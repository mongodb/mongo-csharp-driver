/* Copyright 2010-2014 MongoDB Inc.
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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using Xunit;

namespace MongoDB.Bson.Tests.Jira
{
    public class CSharp310Tests
    {
        private class C
        {
            public int Id;
            public Guid G = Guid.Empty;
        }

        private class EmptyGuidDefaultValueConvention : IMemberMapConvention
        {
            public string Name
            {
                get { return "EmptyGuidDefaultValue"; }
            }

            public void Apply(BsonMemberMap memberMap)
            {
                if (memberMap.MemberType == typeof(Guid))
                {
                    memberMap.SetDefaultValue(Guid.Empty);
                }
            }
        }

        private static void InitializeSerialization()
        {
            var conventions = new ConventionPack();
            conventions.Add(new EmptyGuidDefaultValueConvention());
            conventions.Add(new IgnoreIfDefaultConvention(true));
            ConventionRegistry.Register("CSharp310", conventions, type => type.FullName.StartsWith("MongoDB.Bson.Tests.Jira.CSharp310Tests", StringComparison.Ordinal));
        }

        [Fact]
        public void TestNeverSerializeDefaultValueConvention()
        {
            InitializeSerialization();

            var c = new C { Id = 1 };
            var json = c.ToJson<C>();
            var expected = "{ '_id' : 1 }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson<C>();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson<C>()));
        }
    }
}
