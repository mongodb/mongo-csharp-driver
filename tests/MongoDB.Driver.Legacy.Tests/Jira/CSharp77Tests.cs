/* Copyright 2010-2015 MongoDB Inc.
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

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using Xunit;

namespace MongoDB.Driver.Tests.Jira.CSharp77
{
    public class CSharp77Tests
    {
        private class Foo
        {
            public ObjectId FooId { get; set; }
            public string Name { get; set; }
            public string Summary { get; set; }
        }

        [Fact]
        public void TestSave()
        {
            var server = LegacyTestConfiguration.Server;
            var database = LegacyTestConfiguration.Database;
            var collection = LegacyTestConfiguration.GetCollection<Foo>();

            var conventions = new ConventionPack();
            conventions.Add(new NamedIdMemberConvention(new[] { "FooId" }));
            ConventionRegistry.Register("test", conventions, t => t == typeof(Foo));

            BsonClassMap.RegisterClassMap<Foo>();

            collection.RemoveAll();
            for (int i = 0; i < 10; i++)
            {
                var foo = new Foo
                {
                    FooId = ObjectId.Empty,
                    Name = string.Format("Foo-{0}", i),
                    Summary = string.Format("Summary for Foo-{0}", i)
                };
                collection.Save(foo);
                var count = collection.Count();
                Assert.Equal(i + 1, count);
            }
        }
    }
}
