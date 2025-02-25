/* Copyright 2010-present MongoDB Inc.
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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.GeoJsonObjectModel;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class AggregateGeoNearTests : IntegrationTest<AggregateGeoNearTests.ClassFixture>
    {
        public AggregateGeoNearTests(ClassFixture fixture)
            : base(fixture)
        {
        }
        
        [Fact]
        public void GeoNear_omitting_distanceField_should_return_expected_result()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("8.1.0");
            
            var collection = Fixture.GeoCollection;
            
            collection.Indexes.CreateOne(new CreateIndexModel<Place>(Builders<Place>.IndexKeys.Geo2DSphere(p => p.GeoJsonPointLocation)));
            
            var result = collection
                .Aggregate()
                .GeoNear(
                    GeoJson.Point(GeoJson.Geographic(-73.99279, 40.719296)),
                    new GeoNearOptions<Place>
                    {
                        MaxDistance = 2,
                        Key = "GeoJsonPointLocation",
                        Query = Builders<Place>.Filter.Eq(p => p.Category,
                            "Parks"),
                        Spherical = true
                    })
                .ToList();
            
            result.Count.Should().Be(1);
            result[0].Name.Should().Be("Sara D. Roosevelt Park");
        }
        
        [Fact]
        public void GeoNear_using_pipeline_should_return_expected_result()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("8.1.0");
            
            var collection = Fixture.GeoCollection;
            
            collection.Indexes.CreateOne(new CreateIndexModel<Place>(Builders<Place>.IndexKeys.Geo2D(p => p.LegacyCoordinateLocation)));
            
            var pipeline = new EmptyPipelineDefinition<Place>()
                .GeoNear(
                    new[] { -73.99279, 40.719296 },
                    new GeoNearOptions<Place>
                    {
                        MaxDistance = 0.000313917534,
                        Key = "LegacyCoordinateLocation",
                        Query = Builders<Place>.Filter.Eq(p => p.Category,
                            "Parks"),
                        Spherical = true
                    });
            
            var result = collection.Aggregate(pipeline).ToList();
            
            result.Count.Should().Be(1);
            result[0].Name.Should().Be("Sara D. Roosevelt Park");
        }
        
        [Theory]
        [MemberData(nameof(GeoTestData))]
        public void GeoNear_with_legacyCoordinates_should_return_expected_result<TPoint>(TPoint point, bool usingLegacyCoord) where TPoint : class
        {
            var collection = Fixture.GeoCollection;
            
            var result = collection
                .Aggregate()
                .GeoNear(
                    point,
                    new GeoNearOptions<PlaceResult>
                    {
                        DistanceField = "Distance",
                        MaxDistance = usingLegacyCoord ? 0.000313917534 : 2,
                        Key = usingLegacyCoord ? "LegacyCoordinateLocation" : "GeoJsonPointLocation",
                        Query = Builders<PlaceResult>.Filter.Eq(p => p.Category,
                            "Parks"),
                        Spherical = true
                    })
                .ToList();
            
            result.Count.Should().Be(1);
            result[0].Name.Should().Be("Sara D. Roosevelt Park");
        }
        
        public static object[][] GeoTestData =>
        [
            [new[] { -73.99279, 40.719296 }, true],
            [new BsonDocument {{"long", -73.99279}, {"lat", 40.719296}}, true],
            [GeoJson.Point(GeoJson.Geographic(-73.99279, 40.719296)), false]
        ];
        
        public class Place
        {
            public ObjectId Id { get; set; }
            public string Name { get; set; }
            public GeoJsonPoint<GeoJson2DGeographicCoordinates> GeoJsonPointLocation { get; set; }
            public double[] LegacyCoordinateLocation { get; set; }
            public string Category { get; set; }
        }
        
        public class PlaceResult : Place
        {
            public double Distance { get; set; }
        }
        
        public sealed class ClassFixture : MongoDatabaseFixture
        { 
            public IMongoCollection<Place> GeoCollection { get; private set; }
            
            protected override void InitializeFixture()
            {
                GeoCollection = CreateCollection<Place>("geoCollection");
                GeoCollection.InsertMany([
                    new()
                    {
                        Name = "Central Park",
                        GeoJsonPointLocation = GeoJson.Point(GeoJson.Geographic(-73.97, 40.77)),
                        LegacyCoordinateLocation = [-73.97, 40.77],
                        Category = "Parks"
                    },
                    new()
                    {
                        Name = "Sara D. Roosevelt Park",
                        GeoJsonPointLocation = GeoJson.Point(GeoJson.Geographic(-73.9928, 40.7193)),
                        LegacyCoordinateLocation = [-73.9928, 40.7193],
                        Category = "Parks"
                    },
                    new()
                    {
                        Name = "Polo Grounds",
                        GeoJsonPointLocation = GeoJson.Point(GeoJson.Geographic(-73.9375, 40.8303)),
                        LegacyCoordinateLocation = [-73.9375, 40.8303],
                        Category = "Stadiums"
                    }
                ]);
                
                GeoCollection.Indexes.CreateOne(
                    new CreateIndexModel<Place>(Builders<Place>.IndexKeys.Geo2DSphere(p => p.GeoJsonPointLocation)));
                GeoCollection.Indexes.CreateOne(
                    new CreateIndexModel<Place>(Builders<Place>.IndexKeys.Geo2D(p => p.LegacyCoordinateLocation)));
            }
        }
    }
}