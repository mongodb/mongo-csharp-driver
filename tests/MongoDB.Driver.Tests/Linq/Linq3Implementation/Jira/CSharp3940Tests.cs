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

using System.Linq;
using MongoDB.Bson;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp3940Tests
    {
        [Fact]
        public void Where_comparing_field_name_to_constant_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x["field"] == 0);

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : { field : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Where_comparing_field_name_and_index_to_constant_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x["field"][1] == 0);

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : { 'field.1' : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Where_comparing_nested_field_name_to_constant_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x["field1"]["field2"] == 0);

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : { 'field1.field2' : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Where_comparing_nested_field_name_and_index_to_constant_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x["field1"]["field2"][3] == 0);

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : { 'field1.field2.3' : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Where_comparing_field_name_and_index_and_nested_field_name_to_constant_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x["field1"][2]["field3"] == 0);

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : { 'field1.2.field3' : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Where_comparing_field_name_and_two_indexes_to_constant_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x["field"][1][2] == 0);

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : { 'field.1.2' : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Where_comparing_two_field_names_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x["field1"] == x["field2"]);

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : { $expr : { $eq : ['$field1', '$field2'] } } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Where_comparing_two_field_names_and_indexes_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x["field1"][2] == x["field3"][4]);

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : { $expr : { $eq : [{ $arrayElemAt : ['$field1', 2] }, { $arrayElemAt : ['$field3', 4] }] } } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Where_comparing_two_nested_field_names_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x["field1"]["field2"] == x["field3"]["field4"]);

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : { $expr : { $eq : ['$field1.field2', '$field3.field4'] } } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Where_comparing_two_nested_field_names_and_indexes_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x["field1"]["field2"][3] == x["field4"]["field5"][6]);

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : { $expr : { $eq : [{ $arrayElemAt : ['$field1.field2', 3] }, { $arrayElemAt : ['$field4.field5', 6] }] } } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }


        [Fact]
        public void Where_comparing_two_field_names_and_indexes_and_nested_field_names_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x["field1"][2]["field3"] == x["field4"][5]["field6"]);

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : { $expr : { $eq : [{ $let : { vars : { this : { $arrayElemAt : ['$field1', 2] } }, in : '$$this.field3' } }, { $let : { vars : { this : { $arrayElemAt : ['$field4', 5] } }, in : '$$this.field6' } }] } } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Where_comparing_field_names_and_two_indexes_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x["field1"][2][3] == x["field4"][5][6]);

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : { $expr : { $eq : [{ $arrayElemAt : [{ $arrayElemAt : ['$field1', 2] }, 3] }, { $arrayElemAt : [{ $arrayElemAt : ['$field4', 5] }, 6] }] } } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Select_with_field_name_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new { Field = x["field"] });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { Field : '$field', _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Select_with_field_name_and_index_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new { Field = x["field"][1] });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { Field : { $arrayElemAt : ['$field', 1] }, _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Select_with_nested_field_name_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new { Field = x["field1"]["field2"] });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { Field : '$field1.field2', _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Select_with_nested_field_name_and_index_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new { Field = x["field1"]["field2"][3] });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { Field : { $arrayElemAt : ['$field1.field2', 3] }, _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Select_with_field_name_and_index_and_nested_field_name_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new { Field = x["field1"][2]["field3"] });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { Field : { $let : { vars : { this : { $arrayElemAt : ['$field1', 2] } }, in : '$$this.field3' } }, _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Select_with_field_name_and_two_indexes_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new { Field = x["field1"][2][3] });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { Field : { $arrayElemAt : [{ $arrayElemAt : ['$field1', 2] } , 3] }, _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Select_comparing_field_name_to_constant_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new { IsZero = x["field"] == 0 });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { IsZero : { $eq : ['$field', 0] }, _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }


        [Fact]
        public void Select_comparing_field_name_and_index_to_constant_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new { IsZero = x["field"][1] == 0 });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { IsZero : { $eq : [{ $arrayElemAt : ['$field', 1] }, 0] }, _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Select_comparing_nested_field_name_to_constant_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new { IsZero = x["field1"]["field2"] == 0 });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { IsZero : { $eq : ['$field1.field2', 0] }, _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Select_comparing_nested_field_name_and_index_to_constant_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new { IsZero = x["field1"]["field2"][3] == 0 });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { IsZero : { $eq : [{ $arrayElemAt : ['$field1.field2', 3] }, 0] }, _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Select_comparing_field_name_and_index_and_nested_field_name_to_constant_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new { IsZero = x["field1"][2]["field3"] == 0 });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { IsZero : { $eq : [{ $let : { vars : { this : { $arrayElemAt : ['$field1', 2] } }, in : '$$this.field3' } }, 0] }, _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Select_comparing_field_name_and__two_indexes_to_constant_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new { IsZero = x["field1"][2][3] == 0 });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { IsZero : { $eq : [{ $arrayElemAt : [{ $arrayElemAt : ['$field1', 2] }, 3] }, 0] }, _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Select_comparing_two_field_names_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new { AreEqual = x["field1"] == x["field2"] });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { AreEqual : { $eq : ['$field1', '$field2'] }, _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Select_comparing_two_field_names_and_indexes_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new { AreEqual = x["field1"][2] == x["field3"][4] });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { AreEqual : { $eq : [{ $arrayElemAt : ['$field1', 2] }, { $arrayElemAt : ['$field3', 4] }] }, _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Select_comparing_two_nested_field_names_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new { AreEqual = x["field1"]["field2"] == x["field3"]["field4"] });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { AreEqual : { $eq : ['$field1.field2', '$field3.field4'] }, _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Select_comparing_two_nested_field_names_and_indexes_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new { AreEqual = x["field1"]["field2"][3] == x["field4"]["field5"][6] });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { AreEqual : { $eq : [{ $arrayElemAt : ['$field1.field2', 3] }, { $arrayElemAt : ['$field4.field5', 6] }] }, _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Select_comparing_field_names_and_indexes_and_nested_field_names_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new { AreEqual = x["field1"][2]["field3"] == x["field4"][5]["field6"] });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { AreEqual : { $eq : [{ $let : { vars : { this : { $arrayElemAt : ['$field1', 2] } }, in : '$$this.field3' } }, { $let : { vars : { this : { $arrayElemAt : ['$field4', 5] } }, in : '$$this.field6'  } }] }, _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Select_comparing_field_names_and_two_indexes_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new { AreEqual = x["field1"][2][3] == x["field4"][5][6] });

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { AreEqual : { $eq : [{ $arrayElemAt : [{ $arrayElemAt : ['$field1' , 2] } , 3] }, { $arrayElemAt : [{ $arrayElemAt : ['$field4' , 5] } , 6] }] }, _id : 0 } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        private IMongoCollection<BsonDocument> GetCollection()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("test");
            return database.GetCollection<BsonDocument>("test");
        }
    }
}
