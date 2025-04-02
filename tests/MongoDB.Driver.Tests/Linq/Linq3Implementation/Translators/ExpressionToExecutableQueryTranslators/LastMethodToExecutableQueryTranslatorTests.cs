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
using FluentAssertions;
using MongoDB.Driver.Linq;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators
{
    public class LastMethodToExecutableQueryTranslator : LinqIntegrationTest<LastMethodToExecutableQueryTranslator.ClassFixture>
    {
        public LastMethodToExecutableQueryTranslator(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Last_should_work()
        {
            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable().OrderBy(t => t.Id);

            var result = queryable.Last();
            var stages = queryable.GetMongoQueryProvider().LoggedStages;

            AssertStages(
                stages,
                "{ $sort : { _id : 1 } }",
                "{ $group : { _id : null, _last : { $last : '$$ROOT' } } }",
                "{ $replaceRoot : { newRoot : '$_last' } }");

            result.Id.Should().Be(3);
        }

        [Fact]
        public void Last_with_no_matching_documents_should_throw()
        {
            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable().Where(t => t.Id > 5);

            var exception = Record.Exception(() => queryable.Last());
            var stages = queryable.GetMongoQueryProvider().LoggedStages;

            AssertStages(
                stages,
                "{ $match : { _id : { $gt : 5 } }}",
                "{ $group : { _id : null, _last : { $last : '$$ROOT' } } }",
                "{ $replaceRoot : { newRoot : '$_last' } }");

            exception.Should().BeOfType<InvalidOperationException>();
            exception.Message.Should().Contain("Sequence contains no elements");
        }

        [Fact]
        public void LastOrDefault_should_work()
        {
            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable().OrderBy(t => t.Id);

            var result = queryable.LastOrDefault();
            var stages = queryable.GetMongoQueryProvider().LoggedStages;

            AssertStages(
                stages,
                "{ $sort : { _id : 1 } }",
                "{ $group : { _id : null, _last : { $last : '$$ROOT' } } }",
                "{ $replaceRoot : { newRoot : '$_last' } }");

            result.Should().NotBeNull();
            result.Id.Should().Be(3);
        }

        [Fact]
        public void LastOrDefault_with_no_matching_documents_should_work()
        {
            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable().Where(t => t.Id > 5);

            var result = queryable.LastOrDefault();
            var stages = queryable.GetMongoQueryProvider().LoggedStages;

            AssertStages(
                stages,
                "{ $match : { _id : { $gt : 5 } }}",
                "{ $group : { _id : null, _last : { $last : '$$ROOT' } } }",
                "{ $replaceRoot : { newRoot : '$_last' } }");

            result.Should().BeNull();
        }

        [Fact]
        public void LastOrDefaultWithPredicate_should_work()
        {
            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable().OrderBy(t => t.Id);

            var result = queryable.LastOrDefault(t => t.Id > 1);
            var stages = queryable.GetMongoQueryProvider().LoggedStages;

            AssertStages(
                stages,
                "{ $sort : { _id : 1 } }",
                "{ $match : { _id : { $gt : 1 } }}",
                "{ $group : { _id : null, _last : { $last : '$$ROOT' } } }",
                "{ $replaceRoot : { newRoot : '$_last' } }");

            result.Should().NotBeNull();
            result.Id.Should().Be(3);
        }

        [Fact]
        public void LastOrDefaultWithPredicate_with_no_matching_documents_should_work()
        {
            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable();

            var result = queryable.LastOrDefault(t => t.Id > 4);
            var stages = queryable.GetMongoQueryProvider().LoggedStages;

            AssertStages(
                stages,
                "{ $match : { _id : { $gt : 4 } }}",
                "{ $group : { _id : null, _last : { $last : '$$ROOT' } } }",
                "{ $replaceRoot : { newRoot : '$_last' } }");

            result.Should().BeNull();
        }

        [Fact]
        public void LastWithPredicate_should_work()
        {
            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable().OrderBy(t => t.Id);

            var result = queryable.Last(t => t.Id > 1);
            var stages = queryable.GetMongoQueryProvider().LoggedStages;

            AssertStages(
                stages,
                "{ $sort : { _id : 1 } }",
                "{ $match : { _id : { $gt : 1 } }}",
                "{ $group : { _id : null, _last : { $last : '$$ROOT' } } }",
                "{ $replaceRoot : { newRoot : '$_last' } }");

            result.Id.Should().Be(3);
        }

        [Fact]
        public void LastWithPredicate_with_no_matching_documents_should_throw()
        {
            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable();

            var exception = Record.Exception(() => queryable.Last(t => t.Id > 4));
            var stages = queryable.GetMongoQueryProvider().LoggedStages;

            AssertStages(
                stages,
                "{ $match : { _id : { $gt : 4 } }}",
                "{ $group : { _id : null, _last : { $last : '$$ROOT' } } }",
                "{ $replaceRoot : { newRoot : '$_last' } }");

            exception.Should().BeOfType<InvalidOperationException>();
            exception.Message.Should().Contain("Sequence contains no elements");
        }

        public class TestClass
        {
            public int Id { get; set; }
            public string StringProperty { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<TestClass>
        {
            protected override IEnumerable<TestClass> InitialData { get; } =
            [
                new TestClass { Id = 1, StringProperty = "A" },
                new TestClass { Id = 2, StringProperty = "B" },
                new TestClass { Id = 3, StringProperty = "C" }
            ];
        }
    }
}
