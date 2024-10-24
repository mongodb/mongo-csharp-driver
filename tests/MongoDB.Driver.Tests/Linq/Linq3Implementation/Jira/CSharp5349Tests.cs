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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp5349Tests : Linq3IntegrationTest
    {
        static CSharp5349Tests()
        {
            BsonSerializer.RegisterDiscriminatorConvention(typeof(BaseNoRoot), new ScalarDiscriminatorConvention("__type"));
            BsonSerializer.RegisterDiscriminatorConvention(typeof(BaseWithRoot), new HierarchicalDiscriminatorConvention("__type"));
        }

        [Fact]
        public void InsertOne_BaseNoRoot_should_use_the_configured_discriminator()
        {
            var collection = GetCollectionOfBaseNoRoot();

            var documents = collection.AsQueryable().As(BsonDocumentSerializer.Instance).ToList();

            documents.Single().Should().Be("{ _id : 1, __type : 'DerivedNoRoot' }");
        }

        [Fact]
        public void InsertOne_BaseWithRoot_should_use_the_configured_discriminator()
        {
            var collection = GetCollectionOfBaseWithRoot();

            var documents = collection.AsQueryable().As(BsonDocumentSerializer.Instance).ToList();

            documents.Single().Should().Be("{ _id : 1, __type : ['BaseWithRoot', 'DerivedWithRoot'] }");
        }

        private IMongoCollection<BaseNoRoot> GetCollectionOfBaseNoRoot()
        {
            var collection = GetCollection<BaseNoRoot>("test");
            CreateCollection(
                collection,
                new DerivedNoRoot { Id = 1 });
            return collection;
        }

        private IMongoCollection<BaseWithRoot> GetCollectionOfBaseWithRoot()
        {
            var collection = GetCollection<BaseWithRoot>("test");
            CreateCollection(
                collection,
                new DerivedWithRoot { Id = 1 });
            return collection;
        }

        private class BaseNoRoot
        {
            public int Id { get; set; }
        }

        private class DerivedNoRoot : BaseNoRoot
        {
        }

        [BsonDiscriminator(RootClass = true)]
        [BsonKnownTypes(typeof(DerivedWithRoot))]
        private class BaseWithRoot
        {
            public int Id { get; set; }
        }

        private class DerivedWithRoot : BaseWithRoot
        {
        }
    }
}
