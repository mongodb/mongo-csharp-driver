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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp3731Tests : Linq3IntegrationTest
    {
        static CSharp3731Tests()
        {
            ConventionRegistry.Register(
                "CSharp3731Conventions",
                new ConventionPack { new CamelCaseElementNameConvention() },
                t => t.FullName.Contains("CSharp3731"));
        }

        [Fact]
        public void Element_names_should_use_camel_case()
        {
            var document = new Document { Id = 1, Version = 2, Data = new InstanceData { InstanceName = "one" } };

            document.ToBsonDocument().Should().Be("{ _id : 1, version : 2, data : { instanceName : 'one' } }");
        }

        [Fact]
        public void First_example_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .Select(m => m);

            var stages = Translate(collection, queryable);
            AssertStages(stages, new string[0]);

            var results = queryable.ToList();
            results.Should().HaveCount(2);
            results[0].ShouldBeEquivalentTo(new Document { Id = 1, Version = 1, Data = new InstanceData { InstanceName = "one" } });
            results[1].ShouldBeEquivalentTo(new Document { Id = 2, Version = 2, Data = null });
        }

        [Fact]
        public void Second_example_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .Select(m => new Model { Version = m.Version, Data = m.Data });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { version : '$version', data : '$data', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(2);
            results[0].ShouldBeEquivalentTo(new Document { Id = 1, Version = 1, Data = new InstanceData { InstanceName = "one" } });
            results[1].ShouldBeEquivalentTo(new Document { Id = 2, Version = 2, Data = null });
        }

        [Fact]
        public void Third_example_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .Select(m => new Model { Version = m.Version, Data = m.Data != null ? new InstanceData { InstanceName = m.Data.InstanceName } : default });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { version : '$version', data : { $cond : { if : { $ne : ['$data', null] }, then : { instanceName : '$data.instanceName' }, else : null } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(2);
            results[0].ShouldBeEquivalentTo(new Document { Id = 1, Version = 1, Data = new InstanceData { InstanceName = "one" } });
            results[1].ShouldBeEquivalentTo(new Document { Id = 2, Version = 2, Data = null });
        }

        private IMongoCollection<Document> CreateCollection()
        {
            var collection = GetCollection<Document>();

            CreateCollection(
                collection,
                new Document { Id = 1, Version = 1, Data = new InstanceData { InstanceName = "one" } },
                new Document { Id = 2, Version = 2, Data = null });

            return collection;
        }

        private class Document
        {
            public int Id { get; set; }
            public int Version { get; set; }
            public InstanceData Data { get; set; }
        }

        public class InstanceData
        {
            public string InstanceName { get; set; }
        }

        private class Model
        {
            public int Version { get; set; }
            public InstanceData Data { get; set; }
        }
    }
}
