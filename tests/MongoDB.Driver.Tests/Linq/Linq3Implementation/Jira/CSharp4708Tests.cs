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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4708Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Select_Dictionary_item_with_string_using_compiler_generated_expression_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => x.Dictionary["a"]);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : '$Dictionary.a', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, 0);
        }

        [Fact]
        public void Select_Dictionary_item_with_string_using_call_to_get_item_should_work()
        {
            var collection = GetCollection();
            var x = Expression.Parameter(typeof(C), "x");
            var body = Expression.Call(
                Expression.Property(x, typeof(C).GetProperty("Dictionary")),
                typeof(IDictionary<string, int>).GetProperty("Item").GetGetMethod(),
                Expression.Constant("a"));
            var parameters = new ParameterExpression[] { x };
            var selector = Expression.Lambda<Func<C, int>>(body, parameters);

            var queryable = collection.AsQueryable()
                .Select(selector);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : '$Dictionary.a', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, 0);
        }

        [Fact]
        public void Select_Dictionary_item_with_string_using_MakeIndex_should_work()
        {
            var collection = GetCollection();
            var x = Expression.Parameter(typeof(C), "x");
            var body = Expression.MakeIndex(
                Expression.Property(x, typeof(C).GetProperty("Dictionary")),
                typeof(IDictionary<string, int>).GetProperty("Item"),
                new Expression[] { Expression.Constant("a") });
            var parameters = new ParameterExpression[] { x };
            var selector = Expression.Lambda<Func<C, int>>(body, parameters);

            var queryable = collection.AsQueryable()
                .Select(selector);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : '$Dictionary.a', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, 0);
        }

        [Fact]
        public void Select_Document_item_with_int_using_compiler_generated_expression_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => x.Document[0]);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $objectToArray : '$Document' }, 0] } }, in : '$$this.v' } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, 2);
        }

        [Fact]
        public void Select_Document_item_with_int_using_call_to_get_item_should_work()
        {
            var collection = GetCollection();
            var x = Expression.Parameter(typeof(C), "x");
            var body = Expression.Call(
                Expression.Property(x, typeof(C).GetProperty("Document")),
                typeof(BsonDocument).GetProperty("Item", new[] { typeof(int) }).GetGetMethod(),
                Expression.Constant(0));
            var parameters = new ParameterExpression[] { x };
            var selector = Expression.Lambda<Func<C, BsonValue>>(body, parameters);

            var queryable = collection.AsQueryable()
                .Select(selector);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $objectToArray : '$Document' }, 0] } }, in : '$$this.v' } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, 2);
        }

        [Fact]
        public void Select_Document_item_with_int_using_MakeIndex_should_work()
        {
            var collection = GetCollection();
            var x = Expression.Parameter(typeof(C), "x");
            var body = Expression.MakeIndex(
                Expression.Property(x, typeof(C).GetProperty("Document")),
                typeof(BsonDocument).GetProperty("Item", new[] { typeof(int) }),
                new Expression[] { Expression.Constant(0) });
            var parameters = new ParameterExpression[] { x };
            var selector = Expression.Lambda<Func<C, BsonValue>>(body, parameters);

            var queryable = collection.AsQueryable()
                .Select(selector);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { this : { $arrayElemAt : [{ $objectToArray : '$Document' }, 0] } }, in : '$$this.v' } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, 2);
        }

        [Fact]
        public void Select_Document_item_with_string_using_compiler_generated_expression_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => x.Document["a"]);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : '$Document.a', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, null);
        }

        [Fact]
        public void Select_Document_item_with_string_using_call_to_get_item_should_work()
        {
            var collection = GetCollection();
            var x = Expression.Parameter(typeof(C), "x");
            var body = Expression.Call(
                Expression.Property(x, typeof(C).GetProperty("Document")),
                typeof(BsonDocument).GetProperty("Item", new[] { typeof(string) }).GetGetMethod(),
                Expression.Constant("a"));
            var parameters = new ParameterExpression[] { x };
            var selector = Expression.Lambda<Func<C, BsonValue>>(body, parameters);

            var queryable = collection.AsQueryable()
                .Select(selector);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : '$Document.a', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, null);
        }

        [Fact]
        public void Select_Document_item_with_string_using_MakeIndex_should_work()
        {
            var collection = GetCollection();
            var x = Expression.Parameter(typeof(C), "x");
            var body = Expression.MakeIndex(
                Expression.Property(x, typeof(C).GetProperty("Document")),
                typeof(BsonDocument).GetProperty("Item", new[] { typeof(string) }),
                new Expression[] { Expression.Constant("a") });
            var parameters = new ParameterExpression[] { x };
            var selector = Expression.Lambda<Func<C, BsonValue>>(body, parameters);

            var queryable = collection.AsQueryable()
                .Select(selector);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : '$Document.a', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, null);
        }

        [Fact]
        public void Select_List_item_with_int_using_compiler_generated_expression_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => x.List[0]);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : ['$List', 0] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, 2);
        }

        [Fact]
        public void Select_List_item_with_int_using_call_to_get_item_should_work()
        {
            var collection = GetCollection();
            var x = Expression.Parameter(typeof(C), "x");
            var body = Expression.Call(
                Expression.Property(x, typeof(C).GetProperty("List")),
                typeof(IList<int>).GetProperty("Item", new[] { typeof(int) }).GetGetMethod(),
                Expression.Constant(0));
            var parameters = new ParameterExpression[] { x };
            var selector = Expression.Lambda<Func<C, int>>(body, parameters);

            var queryable = collection.AsQueryable()
                .Select(selector);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : ['$List', 0] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, 2);
        }

        [Fact]
        public void Select_List_item_with_int_using_MakeIndex_should_work()
        {
            var collection = GetCollection();
            var x = Expression.Parameter(typeof(C), "x");
            var body = Expression.MakeIndex(
                Expression.Property(x, typeof(C).GetProperty("List")),
                typeof(IList<int>).GetProperty("Item", new[] { typeof(int) }),
                new Expression[] { Expression.Constant(0) });
            var parameters = new ParameterExpression[] { x };
            var selector = Expression.Lambda<Func<C, int>>(body, parameters);

            var queryable = collection.AsQueryable()
                .Select(selector);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : ['$List', 0] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(1, 2);
        }

        [Fact]
        public void Where_Dictionary_item_with_string_using_compiler_generated_expression_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.Dictionary["a"] == 1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'Dictionary.a' : 1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Where_Dictionary_item_with_string_using_call_to_get_item_should_work()
        {
            var collection = GetCollection();
            var x = Expression.Parameter(typeof(C), "x");
            var body = Expression.Equal(
                Expression.Call(
                    Expression.Property(x, typeof(C).GetProperty("Dictionary")),
                    typeof(IDictionary<string, int>).GetProperty("Item").GetGetMethod(),
                    Expression.Constant("a")),
                Expression.Constant(1));
            var parameters = new ParameterExpression[] { x };
            var predicate = Expression.Lambda<Func<C, bool>>(body, parameters);

            var queryable = collection.AsQueryable()
                .Where(predicate);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'Dictionary.a' : 1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Where_Dictionary_item_with_string_using_MakeIndex_should_work()
        {
            var collection = GetCollection();
            var x = Expression.Parameter(typeof(C), "x");
            var body = Expression.Equal(
                Expression.MakeIndex(
                    Expression.Property(x, typeof(C).GetProperty("Dictionary")),
                    typeof(IDictionary<string, int>).GetProperty("Item"),
                    new Expression[] { Expression.Constant("a") }),
                Expression.Constant(1));
            var parameters = new ParameterExpression[] { x };
            var predicate = Expression.Lambda<Func<C, bool>>(body, parameters);

            var queryable = collection.AsQueryable()
                .Where(predicate);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'Dictionary.a' : 1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Where_Document_item_with_int_using_compiler_generated_expression_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.Document[0] == 1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { $expr : { $eq : [{ $let : { vars : { this : { $arrayElemAt : [{ $objectToArray : '$Document' }, 0] } }, in : '$$this.v' } }, 1] } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Where_Document_item_with_int_using_call_to_get_item_should_work()
        {
            var collection = GetCollection();
            var x = Expression.Parameter(typeof(C), "x");
            var body = Expression.Equal(
                Expression.Call(
                    Expression.Property(x, typeof(C).GetProperty("Document")),
                    typeof(BsonDocument).GetProperty("Item", new[] { typeof(int) }).GetGetMethod(),
                    Expression.Constant(0)),
                Expression.Constant(BsonValue.Create(1)));
            var parameters = new ParameterExpression[] { x };
            var predicate = Expression.Lambda<Func<C, bool>>(body, parameters);

            var queryable = collection.AsQueryable()
                .Where(predicate);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { $expr : { $eq : [{ $let : { vars : { this : { $arrayElemAt : [{ $objectToArray : '$Document' }, 0] } }, in : '$$this.v' } }, 1] } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Where_Document_item_with_int_using_MakeIndex_should_work()
        {
            var collection = GetCollection();
            var x = Expression.Parameter(typeof(C), "x");
            var body = Expression.Equal(
                Expression.MakeIndex(
                    Expression.Property(x, typeof(C).GetProperty("Document")),
                    typeof(BsonDocument).GetProperty("Item", new[] { typeof(int) }),
                    new Expression[] { Expression.Constant(0) }),
                Expression.Constant(BsonValue.Create(1)));
            var parameters = new ParameterExpression[] { x };
            var predicate = Expression.Lambda<Func<C, bool>>(body, parameters);

            var queryable = collection.AsQueryable()
                .Where(predicate);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { $expr : { $eq : [{ $let : { vars : { this : { $arrayElemAt : [{ $objectToArray : '$Document' }, 0] } }, in : '$$this.v' } }, 1] } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Where_Document_item_with_string_using_compiler_generated_expression_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.Document["a"] == 1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'Document.a' : 1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Where_Document_item_with_string_using_call_to_get_item_should_work()
        {
            var collection = GetCollection();
            var x = Expression.Parameter(typeof(C), "x");
            var body = Expression.Equal(
                Expression.Call(
                    Expression.Property(x, typeof(C).GetProperty("Document")),
                    typeof(BsonDocument).GetProperty("Item", new[] { typeof(string) }).GetGetMethod(),
                    Expression.Constant("a")),
                Expression.Constant(BsonValue.Create(1)));
            var parameters = new ParameterExpression[] { x };
            var predicate = Expression.Lambda<Func<C, bool>>(body, parameters);

            var queryable = collection.AsQueryable()
                .Where(predicate);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'Document.a' : 1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Where_Document_item_with_string_using_MakeIndex_should_work()
        {
            var collection = GetCollection();
            var x = Expression.Parameter(typeof(C), "x");
            var body = Expression.Equal(
                Expression.MakeIndex(
                    Expression.Property(x, typeof(C).GetProperty("Document")),
                    typeof(BsonDocument).GetProperty("Item", new[] { typeof(string) }),
                    new Expression[] { Expression.Constant("a") }),
                Expression.Constant(BsonValue.Create(1)));
            var parameters = new ParameterExpression[] { x };
            var predicate = Expression.Lambda<Func<C, bool>>(body, parameters);

            var queryable = collection.AsQueryable()
                .Where(predicate);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'Document.a' : 1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Where_List_item_with_int_using_compiler_generated_expression_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.List[0] == 1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'List.0' : 1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Where_List_item_with_int_using_call_to_get_item_should_work()
        {
            var collection = GetCollection();
            var x = Expression.Parameter(typeof(C), "x");
            var body = Expression.Equal(
                Expression.Call(
                    Expression.Property(x, typeof(C).GetProperty("List")),
                    typeof(IList<int>).GetProperty("Item", new[] { typeof(int) }).GetGetMethod(),
                    Expression.Constant(0)),
                Expression.Constant(1));
            var parameters = new ParameterExpression[] { x };
            var predicate = Expression.Lambda<Func<C, bool>>(body, parameters);

            var queryable = collection.AsQueryable()
                .Where(predicate);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'List.0' : 1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Where_List_item_with_int_using_MakeIndex_should_work()
        {
            var collection = GetCollection();
            var x = Expression.Parameter(typeof(C), "x");
            var body = Expression.Equal(
                Expression.MakeIndex(
                    Expression.Property(x, typeof(C).GetProperty("List")),
                    typeof(IList<int>).GetProperty("Item", new[] { typeof(int) }),
                    new Expression[] { Expression.Constant(0) }),
                Expression.Constant(1));
            var parameters = new ParameterExpression[] { x };
            var predicate = Expression.Lambda<Func<C, bool>>(body, parameters);

            var queryable = collection.AsQueryable()
                .Where(predicate);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'List.0' : 1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        private IMongoCollection<C> GetCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                collection,
                new C { Id = 1, Dictionary = new Dictionary<string, int> { ["a"] = 1 }, Document = new BsonDocument("a", 1), List = new List<int> { 1 } },
                new C { Id = 2, Dictionary = new Dictionary<string, int> { ["b"] = 2 }, Document = new BsonDocument("b", 2), List = new List<int> { 2 } });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public IDictionary<string, int> Dictionary { get; set; }
            public BsonDocument Document { get; set; }
            public IList<int> List { get; set; }
        }
    }
}
