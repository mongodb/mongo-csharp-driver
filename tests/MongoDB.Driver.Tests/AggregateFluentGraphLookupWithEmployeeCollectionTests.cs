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
    public class AggregateFluentGraphLookupWithEmployeeCollectionTests
    {
        #region static
        // private static fields
        private static IMongoDatabase __database;
        private static IMongoCollection<Employee> __employeesCollection;
        private static Lazy<bool> __ensureTestData;

        private static Employee __dev, __eliot, __ron, __andrew, __asya, __dan;

        // static constructor
        static AggregateFluentGraphLookupWithEmployeeCollectionTests()
        {
            var databaseNamespace = DriverTestConfiguration.DatabaseNamespace;
            __database = DriverTestConfiguration.Client.GetDatabase(databaseNamespace.DatabaseName);
            __employeesCollection = __database.GetCollection<Employee>("employees");
            __ensureTestData = new Lazy<bool>(CreateTestData);
        }

        // private static methods
        private static bool CreateTestData()
        {
            // test data is from: https://docs.mongodb.com/master/release-notes/3.4-reference/#pipe._S_graphLookup

            __database.DropCollection(__employeesCollection.CollectionNamespace.CollectionName);

            __dev = new Employee { Id = 1, Name = "Dev", ReportsTo = null };
            __eliot = new Employee { Id = 2, Name = "Eliot", ReportsTo = "Dev" };
            __ron = new Employee { Id = 3, Name = "Ron", ReportsTo = "Eliot" };
            __andrew = new Employee { Id = 4, Name = "Andrew", ReportsTo = "Eliot" };
            __asya = new Employee { Id = 5, Name = "Asya", ReportsTo = "Ron" };
            __dan = new Employee { Id = 6, Name = "Dan", ReportsTo = "Andrew" };
            __employeesCollection.InsertMany(new[] { __dev, __eliot, __ron, __andrew, __asya, __dan });

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
            var subject = __employeesCollection.Aggregate();
            var connectFromField = (FieldDefinition<Employee, string>)"ReportsTo";
            var connectToField = (FieldDefinition<Employee, string>)"Name";
            var startWith = (AggregateExpressionDefinition<Employee, string>)"$reportsTo";
            var @as = (FieldDefinition<EmployeeWithReportingHierarchy, IEnumerable<Employee>>)"ReportingHierarchy";

            var result = subject.GraphLookup(__employeesCollection, connectFromField, connectToField, startWith, @as);

            var stage = result.Stages.Single();
            var renderedStage = stage.Render(__employeesCollection.DocumentSerializer, BsonSerializer.SerializerRegistry);
            renderedStage.Document.Should().Be(
                @"{
                    $graphLookup : {
                        from : 'employees',
                        connectFromField : 'reportsTo',
                        connectToField : 'name',
                        startWith : '$reportsTo',
                        as : 'reportingHierarchy'
                    }
                }");
        }

        [SkippableFact]
        public void GraphLookup_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateGraphLookupStage);
            EnsureTestData();
            var subject = __employeesCollection.Aggregate();
            var connectFromField = (FieldDefinition<Employee, string>)"ReportsTo";
            var connectToField = (FieldDefinition<Employee, string>)"Name";
            var startWith = (AggregateExpressionDefinition<Employee, string>)"$reportsTo";
            var @as = (FieldDefinition<EmployeeWithReportingHierarchy, IEnumerable<Employee>>)"ReportingHierarchy";

            var result = subject
                .GraphLookup(__employeesCollection, connectFromField, connectToField, startWith, @as)
                .ToList();

            var comparer = new EmployeeWithReportingHierarchyEqualityComparer();
            result.WithComparer(comparer).Should().Equal(
                new EmployeeWithReportingHierarchy(__dev, new Employee[0]),
                new EmployeeWithReportingHierarchy(__eliot, new[] { __dev }),
                new EmployeeWithReportingHierarchy(__ron, new[] { __dev, __eliot }),
                new EmployeeWithReportingHierarchy(__andrew, new[] { __dev, __eliot }),
                new EmployeeWithReportingHierarchy(__asya, new[] { __dev, __eliot, __ron }),
                new EmployeeWithReportingHierarchy(__dan, new[] { __dev, __eliot, __andrew }));
        }

        [Fact]
        public void GraphLookup_with_restrictSearchWithMatch_should_add_expected_stage()
        {
            var subject = __employeesCollection.Aggregate();
            var connectFromField = (FieldDefinition<Employee, string>)"ReportsTo";
            var connectToField = (FieldDefinition<Employee, string>)"Name";
            var startWith = (AggregateExpressionDefinition<Employee, string>)"$reportsTo";
            var @as = (FieldDefinition<EmployeeWithReportingHierarchy, Employee[]>)"ReportingHierarchy";
            var options = new AggregateGraphLookupOptions<Employee, Employee, EmployeeWithReportingHierarchy>
            {
                RestrictSearchWithMatch = Builders<Employee>.Filter.Ne("Id", 1)
            };

            var result = subject.GraphLookup(__employeesCollection, connectFromField, connectToField, startWith, @as, options);

            var stage = result.Stages.Single();
            var renderedStage = stage.Render(__employeesCollection.DocumentSerializer, BsonSerializer.SerializerRegistry);
            renderedStage.Document.Should().Be(
                @"{
                    $graphLookup : {
                        from : 'employees',
                        connectFromField : 'reportsTo',
                        connectToField : 'name',
                        startWith : '$reportsTo',
                        as : 'reportingHierarchy',
                        restrictSearchWithMatch : { _id : { $ne : 1 } }
                    }
                }");
        }

        [SkippableFact]
        public void GraphLookup_with_restrictSearchWithMatch_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateGraphLookupStage);
            EnsureTestData();
            var subject = __employeesCollection.Aggregate();
            var connectFromField = (FieldDefinition<Employee, string>)"ReportsTo";
            var connectToField = (FieldDefinition<Employee, string>)"Name";
            var startWith = (AggregateExpressionDefinition<Employee, string>)"$reportsTo";
            var @as = (FieldDefinition<EmployeeWithReportingHierarchy, Employee[]>)"ReportingHierarchy";
            var options = new AggregateGraphLookupOptions<Employee, Employee, EmployeeWithReportingHierarchy>
            {
                RestrictSearchWithMatch = Builders<Employee>.Filter.Ne("Id", 1)
            };

            var result = subject
                .GraphLookup(__employeesCollection, connectFromField, connectToField, startWith, @as, options)
                .ToList();

            var comparer = new EmployeeWithReportingHierarchyEqualityComparer();
            result.WithComparer(comparer).Should().Equal(
                new EmployeeWithReportingHierarchy(__dev, new Employee[0]),
                new EmployeeWithReportingHierarchy(__eliot, new Employee[0]),
                new EmployeeWithReportingHierarchy(__ron, new[] { __eliot }),
                new EmployeeWithReportingHierarchy(__andrew, new[] { __eliot }),
                new EmployeeWithReportingHierarchy(__asya, new[] { __eliot, __ron }),
                new EmployeeWithReportingHierarchy(__dan, new[] { __eliot, __andrew }));
        }

        [Fact]
        public void GraphLookup_with_expressions_should_add_expected_stage()
        {
            var subject = __employeesCollection.Aggregate();

            var result = subject.GraphLookup(
                __employeesCollection,
                connectFromField: x => x.ReportsTo,
                connectToField: x => x.Name,
                startWith: x => x.ReportsTo,
                @as: (EmployeeWithReportingHierarchy x) => x.ReportingHierarchy);               

            var stage = result.Stages.Single();
            var renderedStage = stage.Render(__employeesCollection.DocumentSerializer, BsonSerializer.SerializerRegistry);
            renderedStage.Document.Should().Be(
                @"{
                    $graphLookup : {
                        from : 'employees',
                        connectFromField : 'reportsTo',
                        connectToField : 'name',
                        startWith : '$reportsTo',
                        as : 'reportingHierarchy'
                    }
                }");
        }

        [SkippableFact]
        public void GraphLookup_with_expressions_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateGraphLookupStage);
            EnsureTestData();
            var subject = __employeesCollection.Aggregate();

            var result = subject
                .GraphLookup(
                    __employeesCollection,
                    connectFromField: x => x.ReportsTo,
                    connectToField: x => x.Name,
                    startWith: x => x.ReportsTo,
                    @as: (EmployeeWithReportingHierarchy x) => x.ReportingHierarchy)
                .ToList();

            var comparer = new EmployeeWithReportingHierarchyEqualityComparer();
            result.WithComparer(comparer).Should().Equal(
                new EmployeeWithReportingHierarchy(__dev, new Employee[0]),
                new EmployeeWithReportingHierarchy(__eliot, new[] { __dev }),
                new EmployeeWithReportingHierarchy(__ron, new[] { __dev, __eliot }),
                new EmployeeWithReportingHierarchy(__andrew, new[] { __dev, __eliot }),
                new EmployeeWithReportingHierarchy(__asya, new[] { __dev, __eliot, __ron }),
                new EmployeeWithReportingHierarchy(__dan, new[] { __dev, __eliot, __andrew }));
        }

        [Fact]
        public void GraphLookup_untyped_based_should_add_expected_stage()
        {
            var subject = __employeesCollection.Aggregate();

            var result = subject.GraphLookup(__employeesCollection, "reportsTo", "name", "$reportsTo", "reportingHierarchy");

            var stage = result.Stages.Single();
            var renderedStage = stage.Render(__employeesCollection.DocumentSerializer, BsonSerializer.SerializerRegistry);
            renderedStage.Document.Should().Be(
                @"{
                    $graphLookup : {
                        from : 'employees',
                        connectFromField : 'reportsTo',
                        connectToField : 'name',
                        startWith : '$reportsTo',
                        as : 'reportingHierarchy'
                    }
                }");
        }

        [SkippableFact]
        public void GraphLookup_untyped_based_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateGraphLookupStage);
            EnsureTestData();
            var subject = __employeesCollection.Aggregate();

            var result = subject
                .GraphLookup(__employeesCollection, "reportsTo", "name", "$reportsTo", "reportingHierarchy")
                .ToList();

            var comparer = new EmployeeWithReportingHierarchyBsonDocumentEqualityComparer();
            result.WithComparer(comparer).Should().Equal(
                BsonDocument.Parse(@"{ _id : 1, name : 'Dev', reportingHierarchy : [ ] }"),
                BsonDocument.Parse(@"{
                   _id : 2,
                   name : 'Eliot',
                   reportsTo : 'Dev',
                   reportingHierarchy : [
                      { _id : 1, name : 'Dev' }
                   ]
                }"),
                BsonDocument.Parse(@"{
                   _id : 3,
                   name : 'Ron',
                   reportsTo : 'Eliot',
                   reportingHierarchy : [
                      { _id : 1, name : 'Dev' },
                      { _id : 2, name : 'Eliot', reportsTo : 'Dev' }
                   ]
                }"),
                BsonDocument.Parse(@"{
                   _id : 4,
                   name : 'Andrew',
                   reportsTo : 'Eliot',
                   reportingHierarchy : [
                      { _id : 1, name : 'Dev' },
                      { _id : 2, name : 'Eliot', reportsTo : 'Dev' }
                   ]
                }"),
                BsonDocument.Parse(@"{
                   _id : 5,
                   name : 'Asya',
                   reportsTo : 'Ron',
                   reportingHierarchy : [
                      { _id : 1, name : 'Dev' },
                      { _id : 2, name : 'Eliot', reportsTo : 'Dev' },
                      { _id : 3, name : 'Ron', reportsTo : 'Eliot' }
                   ]
                }"),
                BsonDocument.Parse(@"{
                   _id : 6,
                   name : 'Dan',
                   reportsTo : 'Andrew',
                   reportingHierarchy : [
                      { _id : 1, name : 'Dev' },
                      { _id : 2, name : 'Eliot', reportsTo : 'Dev' },
                      { _id : 4, name : 'Andrew', reportsTo : 'Eliot' }
                   ]
                }"));
        }

        // nested types
        private class Employee
        {
            [BsonId]
            public int Id { get; set; }
            [BsonElement("name")]
            public string Name { get; set; }
            [BsonIgnoreIfNull]
            [BsonElement("reportsTo")]
            public string ReportsTo { get; set; }
        }

        private class EmployeeWithReportingHierarchy
        {
            public EmployeeWithReportingHierarchy(Employee employee, Employee[] reportingHierarchy)
            {
                Id = employee.Id;
                Name = employee.Name;
                ReportsTo = employee.ReportsTo;
                ReportingHierarchy = reportingHierarchy;
            }

            [BsonId]
            public int Id { get; set; }
            [BsonElement("name")]
            public string Name { get; set; }
            [BsonIgnoreIfNull]
            [BsonElement("reportsTo")]
            public string ReportsTo { get; set; }
            [BsonElement("reportingHierarchy")]
            public Employee[] ReportingHierarchy { get; set; }
        }

        private class EmployeeEqualityComparer : IEqualityComparer<Employee>
        {
            public bool Equals(Employee x, Employee y)
            {
                return
                    x.Id == y.Id &&
                    x.Name == y.Name &&
                    x.ReportsTo == y.ReportsTo;
            }

            public int GetHashCode(Employee obj)
            {
                throw new NotImplementedException();
            }
        }

        private class EmployeeWithReportingHierarchyEqualityComparer : IEqualityComparer<EmployeeWithReportingHierarchy>
        {
            private IEqualityComparer<Employee[]> _reportingHierarchyComparer = new EnumerableSetEqualityComparer<Employee>(new EmployeeEqualityComparer());

            public bool Equals(EmployeeWithReportingHierarchy x, EmployeeWithReportingHierarchy y)
            {
                return
                    x.Id == y.Id &&
                    x.Name == y.Name &&
                    x.ReportsTo == y.ReportsTo &&
                    _reportingHierarchyComparer.Equals(x.ReportingHierarchy, y.ReportingHierarchy);
            }

            public int GetHashCode(EmployeeWithReportingHierarchy obj)
            {
                throw new NotImplementedException();
            }
        }

        private class EmployeeWithReportingHierarchyBsonDocumentEqualityComparer : IEqualityComparer<BsonDocument>
        {
            private readonly IEqualityComparer<IEnumerable<BsonDocument>> _reportingHierarchyComparer = new EnumerableSetEqualityComparer<BsonDocument>(new EqualsEqualityComparer<BsonDocument>());

            public bool Equals(BsonDocument x, BsonDocument y)
            {
                if (!x.Names.SequenceEqual(y.Names))
                {
                    return false;
                }

                return
                    x["_id"].Equals(y["_id"]) &&
                    x["name"].Equals(y["name"]) &&
                    object.Equals(x.GetValue("reportsTo", null), y.GetValue("reportsTo", null)) &&
                    _reportingHierarchyComparer.Equals(x["reportingHierarchy"].AsBsonArray.Cast<BsonDocument>(), y["reportingHierarchy"].AsBsonArray.Cast<BsonDocument>());
            }

            public int GetHashCode(BsonDocument obj)
            {
                throw new NotImplementedException();
            }
        }
    }
}
