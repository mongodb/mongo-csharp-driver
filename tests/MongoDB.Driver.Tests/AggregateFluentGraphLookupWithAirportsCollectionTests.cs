/* Copyright 2016 MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.EqualityComparers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class AggregateFluentGraphLookupWithAirportCollectionTests
    {
        #region static
        // private static fields
        private static IMongoCollection<Airport> __airportsCollection;
        private static IMongoDatabase __database;
        private static Lazy<bool> __ensureTestData;
        private static IMongoCollection<Traveler> __travelersCollection;

        private static Airport __jfk, __bos, __ord, __pwm, __lhr;
        private static Traveler __dev, __eliot, __jeff;

        // static constructor
        static AggregateFluentGraphLookupWithAirportCollectionTests()
        {
            var databaseNamespace = DriverTestConfiguration.DatabaseNamespace;
            __database = DriverTestConfiguration.Client.GetDatabase(databaseNamespace.DatabaseName);
            __airportsCollection = __database.GetCollection<Airport>("airports");
            __ensureTestData = new Lazy<bool>(CreateTestData);
            __travelersCollection = __database.GetCollection<Traveler>("travelers");
        }

        // private static methods
        private static bool CreateTestData()
        {
            // test data is from: https://docs.mongodb.com/master/release-notes/3.4-reference/#pipe._S_graphLookup

            __database.DropCollection(__airportsCollection.CollectionNamespace.CollectionName);
            __database.DropCollection(__travelersCollection.CollectionNamespace.CollectionName);

            __jfk= new Airport { Id = 0, Name = "JFK", Connects = new[] { "BOS", "ORD" } };
            __bos = new Airport { Id = 1, Name = "BOS", Connects = new[] { "JFK", "PWM" } };
            __ord = new Airport { Id = 2, Name = "ORD", Connects = new[] { "JFK" } };
            __pwm = new Airport { Id = 3, Name = "PWM", Connects = new[] { "BOS", "LHR" } };
            __lhr = new Airport { Id = 4, Name = "LHR", Connects = new[] { "PWM" } };
            __airportsCollection.InsertMany(new[] { __jfk, __bos, __ord, __pwm, __lhr });

            __dev = new Traveler { Id = 1, Name = "Dev", NearestAirport = "JFK" };
            __eliot = new Traveler { Id = 2, Name = "Eliot", NearestAirport = "JFK" };
            __jeff = new Traveler { Id = 3, Name = "Jeff", NearestAirport = "BOS" };
            __travelersCollection.InsertMany(new[] { __dev, __eliot, __jeff });

            return true;
        }

        private static void EnsureTestData()
        {
            var _ = __ensureTestData.Value;
        }

        #endregion

        // public methods
        [Fact]
        public void GraphLookup_should_add_expected_stage()
        {
            var subject = __travelersCollection.Aggregate();
            var connectFromField = (FieldDefinition<Airport, string[]>)"Connects";
            var connectToField = (FieldDefinition<Airport, string>)"Name";
            var startWith = (AggregateExpressionDefinition<Traveler, string>)"$nearestAirport";
            var @as = (FieldDefinition<TravelerDestinations, Destination[]>)"Destinations";
            var depthField = (FieldDefinition<Destination, int>)"NumConnections";
            var options = new AggregateGraphLookupOptions<Airport, Destination, TravelerDestinations>
            {
                MaxDepth = 2
            };

            var result = subject.GraphLookup(__airportsCollection, connectFromField, connectToField, startWith, @as, depthField, options);

            var stage = result.Stages.Single();
            var renderedStage = stage.Render(__travelersCollection.DocumentSerializer, BsonSerializer.SerializerRegistry);
            renderedStage.Document.Should().Be(
                @"{
                    $graphLookup : {
                        from : 'airports',
                        connectFromField : 'connects',
                        connectToField : 'airport',
                        startWith : '$nearestAirport',
                        as : 'destinations',
                        depthField : 'numConnections',
                        maxDepth : 2
                    }
                }");
        }

        [SkippableFact]
        public void GraphLookup_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateGraphLookupStage);
            EnsureTestData();
            var subject = __travelersCollection.Aggregate();
            var connectFromField = (FieldDefinition<Airport, string[]>)"Connects";
            var connectToField = (FieldDefinition<Airport, string>)"Name";
            var startWith = (AggregateExpressionDefinition<Traveler, string>)"$nearestAirport";
            var @as = (FieldDefinition<TravelerDestinations, Destination[]>)"Destinations";
            var depthField = (FieldDefinition<Destination, int>)"NumConnections";
            var options = new AggregateGraphLookupOptions<Airport, Destination, TravelerDestinations>
            {
                MaxDepth = 2
            };

            var result = subject
                .GraphLookup(__airportsCollection, connectFromField, connectToField, startWith, @as, depthField, options)
                .ToList();

            var comparer = new TravelerDestinationsEqualityComparer();
            result.WithComparer(comparer).Should().Equal(
                new TravelerDestinations(__dev, new[]
                {
                    new Destination(__pwm, 2),
                    new Destination(__ord, 1),
                    new Destination(__bos, 1),
                    new Destination(__jfk, 0)
                }),
                new TravelerDestinations(__eliot, new[]
                {
                    new Destination(__pwm, 2),
                    new Destination(__ord, 1),
                    new Destination(__bos, 1),
                    new Destination(__jfk, 0)
                }),
                new TravelerDestinations(__jeff, new[]
                {
                    new Destination(__ord, 2),
                    new Destination(__pwm, 1),
                    new Destination(__lhr, 2),
                    new Destination(__jfk, 1),
                    new Destination(__bos, 0)
                }));
        }

        [Fact]
        public void GraphLookup_typed_should_add_expected_stage()
        {
            var subject = __travelersCollection.Aggregate();
            var options = new AggregateGraphLookupOptions<Airport, Destination, TravelerDestinations>
            {
                MaxDepth = 2
            };

            var result = subject.GraphLookup(
                from: __airportsCollection,
                connectFromField: x => x.Connects,
                connectToField: x => x.Name,
                startWith: x => x.NearestAirport,
                @as : (TravelerDestinations x) => x.Destinations,
                depthField: (Destination x) => x.NumConnections,
                options: options);

            var stage = result.Stages.Single();
            var renderedStage = stage.Render(__travelersCollection.DocumentSerializer, BsonSerializer.SerializerRegistry);
            renderedStage.Document.Should().Be(
                @"{
                    $graphLookup : {
                        from : 'airports',
                        connectFromField : 'connects',
                        connectToField : 'airport',
                        startWith : '$nearestAirport',
                        as : 'destinations',
                        depthField : 'numConnections',
                        maxDepth : 2
                    }
                }");
        }

        [SkippableFact]
        public void GraphLookup_typed_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateGraphLookupStage);
            EnsureTestData();
            var subject = __travelersCollection.Aggregate();
            var connectFromField = (FieldDefinition<Airport, string[]>)"Connects";
            var connectToField = (FieldDefinition<Airport, string>)"Name";
            var startWith = (AggregateExpressionDefinition<Traveler, string>)"$nearestAirport";
            var @as = (FieldDefinition<TravelerDestinations, Destination[]>)"Destinations";
            var depthField = (FieldDefinition<Destination, int>)"NumConnections";
            var options = new AggregateGraphLookupOptions<Airport, Destination, TravelerDestinations>
            {
                MaxDepth = 2
            };

            var result = subject
                .GraphLookup(__airportsCollection, connectFromField, connectToField, startWith, @as, depthField, options)
                .ToList();

            var comparer = new TravelerDestinationsEqualityComparer();
            result.WithComparer(comparer).Should().Equal(
                new TravelerDestinations(__dev, new[]
                {
                    new Destination(__pwm, 2),
                    new Destination(__ord, 1),
                    new Destination(__bos, 1),
                    new Destination(__jfk, 0)
                }),
                new TravelerDestinations(__eliot, new[]
                {
                    new Destination(__pwm, 2),
                    new Destination(__ord, 1),
                    new Destination(__bos, 1),
                    new Destination(__jfk, 0)
                }),
                new TravelerDestinations(__jeff, new[]
                {
                    new Destination(__ord, 2),
                    new Destination(__pwm, 1),
                    new Destination(__lhr, 2),
                    new Destination(__jfk, 1),
                    new Destination(__bos, 0)
                }));
        }

        [Fact]
        public void GraphLookup_typed_with_array_valued_start_with_should_add_expected_stage()
        {
            var subject = __airportsCollection.Aggregate();
            var options = new AggregateGraphLookupOptions<Airport, Airport, AirportDestinationsIncludingStops>
            {
                MaxDepth = 2
            };

            var result = subject.GraphLookup(
                from: __airportsCollection,
                connectFromField: x => x.Connects,
                connectToField: x => x.Name,
                startWith: x => x.Connects,
                @as: (AirportDestinationsIncludingStops x) => x.Destinations,
                options: options);

            var stage = result.Stages.Single();
            var renderedStage = stage.Render(__airportsCollection.DocumentSerializer, BsonSerializer.SerializerRegistry);
            renderedStage.Document.Should().Be(
                @"{
                    $graphLookup : {
                        from : 'airports',
                        connectFromField : 'connects',
                        connectToField : 'airport',
                        startWith : '$connects',
                        as : 'destinations',
                        maxDepth : 2
                    }
                }");
        }

        [SkippableFact]
        public void GraphLookup_typed_with_array_valued_start_with_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateGraphLookupStage);
            EnsureTestData();
            var subject = __airportsCollection.Aggregate();
            var options = new AggregateGraphLookupOptions<Airport, Airport, AirportDestinationsIncludingStops>
            {
                MaxDepth = 2
            };

            var result = subject
                .GraphLookup(
                    from: __airportsCollection,
                    connectFromField: x => x.Connects,
                    connectToField: x => x.Name,
                    startWith: x => x.Connects,
                    @as: (AirportDestinationsIncludingStops x) => x.Destinations,
                    options: options)
                .ToList();

            var comparer = new AirportDestinationsIncludingStopsEqualityComparer();
            result.WithComparer(comparer).Should().Equal(
                new AirportDestinationsIncludingStops(__jfk, new[] { __jfk, __ord, __bos, __pwm, __lhr }),
                new AirportDestinationsIncludingStops(__bos, new[] { __bos, __ord, __jfk, __pwm, __lhr }),
                new AirportDestinationsIncludingStops(__ord, new[] { __ord, __jfk, __bos, __pwm }),
                new AirportDestinationsIncludingStops(__pwm, new[] { __pwm, __ord, __jfk, __bos, __lhr }),
                new AirportDestinationsIncludingStops(__lhr, new[] { __lhr, __jfk, __bos, __pwm }));
        }

        // nested types
        private class Airport
        {
            public int Id { get; set; }
            [BsonElement("airport")]
            public string Name { get; set; }
            [BsonElement("connects")]
            public string[] Connects { get; set; }
        }

        private class AirportDestinationsIncludingStops
        {
            public AirportDestinationsIncludingStops(Airport airport, Airport[] destinations)
            {
                Id = airport.Id;
                Name = airport.Name;
                Connects = airport.Connects;
                Destinations = destinations;
            }

            public int Id { get; set; }
            [BsonElement("airport")]
            public string Name { get; set; }
            [BsonElement("connects")]
            public string[] Connects { get; set; }
            [BsonElement("destinations")]
            public Airport[] Destinations { get; set; }
        }

        private class Destination
        {
            public Destination(Airport airport, int numConnections)
            {
                Id = airport.Id;
                AirportName = airport.Name;
                Connects = airport.Connects;
                NumConnections = numConnections;
            }

            public int Id { get; set; }
            [BsonElement("airport")]
            public string AirportName { get; set; }
            [BsonElement("connects")]
            public string[] Connects { get; set; }
            [BsonElement("numConnections")]
            public int NumConnections { get; set; }
        }

        private class Traveler
        {
            public int Id { get; set; }
            [BsonElement("name")]
            public string Name { get; set; }
            [BsonElement("nearestAirport")]
            public string NearestAirport { get; set; }
        }

        private class TravelerDestinations
        {
            public TravelerDestinations(Traveler traveler, Destination[] destinations)
            {
                Id = traveler.Id;
                Name = traveler.Name;
                NearestAirport = traveler.NearestAirport;
                Destinations = destinations;
            }

            public int Id { get; set; }
            [BsonElement("name")]
            public string Name { get; set; }
            [BsonElement("nearestAirport")]
            public string NearestAirport { get; set; }
            [BsonElement("destinations")]
            public Destination[] Destinations {get;set;}
        }

        private class AirportEqualityComparer : IEqualityComparer<Airport>
        {
            public bool Equals(Airport x, Airport y)
            {
                return
                    x.Id == y.Id &&
                    x.Name == y.Name &&
                    x.Connects.SequenceEqual(y.Connects);
            }

            public int GetHashCode(Airport obj)
            {
                throw new NotImplementedException();
            }
        }

        private class AirportDestinationsIncludingStopsEqualityComparer : IEqualityComparer<AirportDestinationsIncludingStops>
        {
            private readonly IEqualityComparer<Airport[]> _destinationsEqualityComparer = new EnumerableSetEqualityComparer<Airport>(new AirportEqualityComparer());

            public bool Equals(AirportDestinationsIncludingStops x, AirportDestinationsIncludingStops y)
            {
                return
                    x.Id == y.Id &&
                    x.Name == y.Name &&
                    x.Connects.SequenceEqual(y.Connects) &&
                    _destinationsEqualityComparer.Equals(x.Destinations, y.Destinations);
            }

            public int GetHashCode(AirportDestinationsIncludingStops obj)
            {
                throw new NotImplementedException();
            }
        }

        private class DestinationEqualityComparer : IEqualityComparer<Destination>
        {
            public bool Equals(Destination x, Destination y)
            {
                return
                    x.Id == y.Id &&
                    x.AirportName == y.AirportName &&
                    x.Connects.SequenceEqual(y.Connects) &&
                    x.NumConnections == y.NumConnections;
            }

            public int GetHashCode(Destination obj)
            {
                throw new NotImplementedException();
            }
        }

        private class TravelerDestinationsEqualityComparer : IEqualityComparer<TravelerDestinations>
        {
            private readonly IEqualityComparer<Destination[]> _destinationsComparer = new EnumerableSetEqualityComparer<Destination>(new DestinationEqualityComparer());

            public bool Equals(TravelerDestinations x, TravelerDestinations y)
            {
                return
                    x.Id == y.Id &&
                    x.Name == y.Name &&
                    x.NearestAirport == y.NearestAirport &&
                    _destinationsComparer.Equals(x.Destinations, y.Destinations);
            }

            public int GetHashCode(TravelerDestinations obj)
            {
                throw new NotImplementedException();
            }
        }
    }
}
