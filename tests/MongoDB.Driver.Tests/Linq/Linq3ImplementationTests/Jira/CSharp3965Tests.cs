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
using FluentAssertions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp3965Tests : Linq3IntegrationTest
    {
        [Fact]
        public void OrderBy_with_expression_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .OrderBy(x => x.Y + 1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $add : ['$Y', 1] } } }",
                "{ $sort : { _key1 : 1 } }",
                "{ $replaceRoot : { newRoot : '$_document' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2);
        }

        [Fact]
        public void OrderBy_with_expression_and_ThenBy_with_expression_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .OrderBy(x => x.X + 1)
                .ThenBy(x => x.Y + 1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $add : ['$X', 1] }, _key2 : { $add : ['$Y', 1] } } }",
                "{ $sort : { _key1 : 1, _key2 : 1 } }",
                "{ $replaceRoot : { newRoot : '$_document' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2);
        }

        [Fact]
        public void OrderBy_with_expression_and_ThenBy_with_field_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .OrderBy(x => x.X + 1)
                .ThenBy(x => x.Y);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $add : ['$X', 1] } } }",
                "{ $sort : { _key1 : 1, '_document.Y' : 1 } }",
                "{ $replaceRoot : { newRoot : '$_document' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2);
        }

        [Fact]
        public void OrderBy_with_expression_and_ThenByDescending_with_expression_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .OrderBy(x => x.X + 1)
                .ThenByDescending(x => x.Y + 1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $add : ['$X', 1] }, _key2 : { $add : ['$Y', 1] } } }",
                "{ $sort : { _key1 : 1, _key2 : -1 } }",
                "{ $replaceRoot : { newRoot : '$_document' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2, 1);
        }

        [Fact]
        public void OrderBy_with_expression_and_ThenByDescending_with_field_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .OrderBy(x => x.X + 1)
                .ThenByDescending(x => x.Y);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $add : ['$X', 1] } } }",
                "{ $sort : { _key1 : 1, '_document.Y' : -1 } }",
                "{ $replaceRoot : { newRoot : '$_document' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2, 1);
        }

        [Fact]
        public void OrderBy_with_field_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .OrderBy(x => x.Y);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { Y : 1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2);
        }

        [Fact]
        public void OrderBy_with_field_and_ThenBy_with_expression_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .OrderBy(x => x.X)
                .ThenBy(x => x.Y + 1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $add : ['$Y', 1] } } }",
                "{ $sort : { '_document.X' : 1, _key1 : 1 } }",
                "{ $replaceRoot : { newRoot : '$_document' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2);
        }

        [Fact]
        public void OrderBy_with_field_and_ThenBy_with_field_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .OrderBy(x => x.X)
                .ThenBy(x => x.Y);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { X : 1, Y : 1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2);
        }

        [Fact]
        public void OrderBy_with_field_and_ThenByDescending_with_expression_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .OrderBy(x => x.X)
                .ThenByDescending(x => x.Y + 1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $add : ['$Y', 1] } } }",
                "{ $sort : { '_document.X' : 1, _key1 : -1 } }",
                "{ $replaceRoot : { newRoot : '$_document' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2, 1);
        }

        [Fact]
        public void OrderBy_with_field_and_ThenByDescending_with_field_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .OrderBy(x => x.X)
                .ThenByDescending(x => x.Y);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { X : 1, Y : -1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2, 1);
        }

        [Fact]
        public void OrderByDescending_with_expression_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .OrderByDescending(x => x.Y + 1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $add : ['$Y', 1] } } }",
                "{ $sort : { _key1 : -1 } }",
                "{ $replaceRoot : { newRoot : '$_document' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2, 1);
        }

        [Fact]
        public void OrderByDescending_with_expression_and_ThenBy_with_expression_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .OrderByDescending(x => x.X + 1)
                .ThenBy(x => x.Y + 1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $add : ['$X', 1] }, _key2 : { $add : ['$Y', 1] } } }",
                "{ $sort : { _key1 : -1, _key2 : 1 } }",
                "{ $replaceRoot : { newRoot : '$_document' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2);
        }

        [Fact]
        public void OrderByDescending_with_expression_and_ThenBy_with_field_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .OrderByDescending(x => x.X + 1)
                .ThenBy(x => x.Y);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $add : ['$X', 1] } } }",
                "{ $sort : { _key1 : -1, '_document.Y' : 1 } }",
                "{ $replaceRoot : { newRoot : '$_document' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2);
        }

        [Fact]
        public void OrderByDescending_with_expression_and_ThenByDescending_with_expression_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .OrderByDescending(x => x.X + 1)
                .ThenByDescending(x => x.Y + 1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $add : ['$X', 1] }, _key2 : { $add : ['$Y', 1] } } }",
                "{ $sort : { _key1 : -1, _key2 : -1 } }",
                "{ $replaceRoot : { newRoot : '$_document' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2, 1);
        }

        [Fact]
        public void OrderByDescending_with_expression_and_ThenByDescending_with_field_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .OrderByDescending(x => x.X + 1)
                .ThenByDescending(x => x.Y);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $add : ['$X', 1] } } }",
                "{ $sort : { _key1 : -1, '_document.Y' : -1 } }",
                "{ $replaceRoot : { newRoot : '$_document' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2, 1);
        }

        [Fact]
        public void OrderByDescending_with_field_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .OrderByDescending(x => x.Y);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { Y : -1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2, 1);
        }

        [Fact]
        public void OrderByDescending_with_field_and_ThenBy_with_expression_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .OrderByDescending(x => x.X)
                .ThenBy(x => x.Y + 1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $add : ['$Y', 1] } } }",
                "{ $sort : { '_document.X' : -1, _key1 : 1 } }",
                "{ $replaceRoot : { newRoot : '$_document' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2);
        }

        [Fact]
        public void OrderByDescending_with_field_and_ThenBy_with_field_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .OrderByDescending(x => x.X)
                .ThenBy(x => x.Y);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { X : -1, Y : 1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2);
        }

        [Fact]
        public void OrderByDescending_with_field_and_ThenByDescending_with_expression_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .OrderByDescending(x => x.X)
                .ThenByDescending(x => x.Y + 1);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _id : 0, _document : '$$ROOT', _key1 : { $add : ['$Y', 1] } } }",
                "{ $sort : { '_document.X' : -1, _key1 : -1 } }",
                "{ $replaceRoot : { newRoot : '$_document' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2, 1);
        }

        [Fact]
        public void OrderByDescending_with_field_and_ThenByDescending_with_field_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .OrderByDescending(x => x.X)
                .ThenByDescending(x => x.Y);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { X : -1, Y : -1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2, 1);
        }

        private IMongoCollection<C> CreateCollection()
        {
            var collection = GetCollection<C>();

            var documents = new C[]
            {
                new C
                {
                    Id = 1,
                    X = 1,
                    Y = 1,
                },
                new C
                {
                    Id = 2,
                    X = 1,
                    Y = 2
                }

            };
            CreateCollection(collection, documents);

            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
        }
    }
}
