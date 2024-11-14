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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ExpressionTranslators
{
    public class GetTypeComparisonExpressionToFilterTranslatorTests : Linq3IntegrationTest
    {
        [Fact]
        public void Hierarchical_documents_should_be_serialized_as_expected()
        {
            var collection = GetHierarchicalCollection();

            var documents = collection.AsQueryable().As(BsonDocumentSerializer.Instance).ToArray();

            documents.Should().HaveCount(3);
            documents[0].Should().Be("{ _id : 1, _t : 'HierarchicalBaseClass' }");
            documents[1].Should().Be("{ _id : 2, _t : ['HierarchicalBaseClass', 'HierarchicalInheritedClass1'] }");
            documents[2].Should().Be("{ _id : 3, _t : ['HierarchicalBaseClass', 'HierarchicalInheritedClass2'] }");
        }

        [Fact]
        public void Hierarchical_Where_GetType_Equals_HierarchicalBaseClass_should_work()
        {
            var collection = GetHierarchicalCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.GetType() == typeof(HierarchicalBaseClass));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { '_t.0' : { $exists : false }, _t : 'HierarchicalBaseClass' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Hierarchical_Where_GetType_Equals_HierarchicalInheritedClass1_should_work()
        {
            var collection = GetHierarchicalCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.GetType() == typeof(HierarchicalInheritedClass1));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _t : ['HierarchicalBaseClass', 'HierarchicalInheritedClass1'] } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2);
        }

        [Fact]
        public void Hierarchical_Where_GetType_NotEquals_HierarchicalBaseClass_should_work()
        {
            var collection = GetHierarchicalCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.GetType() != typeof(HierarchicalBaseClass));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { $nor : [{'_t.0' : { $exists : false }, _t : 'HierarchicalBaseClass' }] } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2, 3);
        }

        [Fact]
        public void Hierarchical_Where_GetType_NotEquals_HierarchicalInheritedClass1_should_work()
        {
            var collection = GetHierarchicalCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.GetType() != typeof(HierarchicalInheritedClass1));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _t : { $ne : ['HierarchicalBaseClass', 'HierarchicalInheritedClass1'] } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 3);
        }

        [Fact]
        public void Scalar_documents_should_be_serialized_as_expected()
        {
            var collection = GetScalarCollection();

            var documents = collection.AsQueryable().As(BsonDocumentSerializer.Instance).ToArray();

            documents.Should().HaveCount(3);
            documents[0].Should().Be("{ _id : 1 }");
            documents[1].Should().Be("{ _id : 2, _t : 'ScalarInheritedClass1' }");
            documents[2].Should().Be("{ _id : 3, _t : 'ScalarInheritedClass2' }");
        }

        [Fact]
        public void Scalar_Where_GetType_Equals_ScalarBaseClass_should_work()
        {
            var collection = GetScalarCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.GetType() == typeof(ScalarBaseClass));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _t : { $exists : false } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Scalar_Where_GetType_Equals_ScalarInheritedClass1_should_work()
        {
            var collection = GetScalarCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.GetType() == typeof(ScalarInheritedClass1));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _t : 'ScalarInheritedClass1' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2);
        }

        [Fact]
        public void Scalar_Where_GetType_NotEquals_ScalarBaseClass_should_work()
        {
            var collection = GetScalarCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.GetType() != typeof(ScalarBaseClass));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _t : { $exists : true } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2, 3);
        }

        [Fact]
        public void Scalar_Where_GetType_NotEquals_ScalarInheritedClass1_should_work()
        {
            var collection = GetScalarCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.GetType() != typeof(ScalarInheritedClass1));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _t : { $ne : 'ScalarInheritedClass1' } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 3);
        }

        private IMongoCollection<HierarchicalBaseClass> GetHierarchicalCollection()
        {
            var collection = GetCollection<HierarchicalBaseClass>("test");
            CreateCollection(
                collection,
                new HierarchicalBaseClass { Id = 1 },
                new HierarchicalInheritedClass1 { Id = 2 },
                new HierarchicalInheritedClass2 { Id = 3 });
            return collection;
        }

        private IMongoCollection<ScalarBaseClass> GetScalarCollection()
        {
            var collection = GetCollection<ScalarBaseClass>("test");
            CreateCollection(
                collection,
                new ScalarBaseClass { Id = 1 },
                new ScalarInheritedClass1 { Id = 2 },
                new ScalarInheritedClass2 { Id = 3 });
            return collection;
        }

        [BsonDiscriminator(RootClass = true)]
        [BsonKnownTypes(typeof(HierarchicalInheritedClass1), typeof(HierarchicalInheritedClass2))]
        private class HierarchicalBaseClass
        {
            public int Id { get; set; }
        }

        private class HierarchicalInheritedClass1 : HierarchicalBaseClass
        {
        }

        private class HierarchicalInheritedClass2 : HierarchicalBaseClass
        {
        }

        private class ScalarBaseClass
        {
            public int Id { get; set; }
        }

        private class ScalarInheritedClass1 : ScalarBaseClass
        {
        }

        private class ScalarInheritedClass2 : ScalarBaseClass
        {
        }
    }
}
