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
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Bson.Tests.Jira.CSharp121
{
    public class C
    {
        [BsonRepresentation(BsonType.String)]
        public Guid PhotoId { get; set; }
    }

    public class CSharp121Tests
    {
        [Fact]
        public void TestGuidStringRepresentation()
        {
            var c = new C { PhotoId = Guid.Empty };
            var json = c.ToJson();
            var expected = "{ 'PhotoId' : #S }";
            expected = expected.Replace("#S", "'00000000-0000-0000-0000-000000000000'");
            expected = expected.Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsType<C>(rehydrated);
            Assert.Equal(c.PhotoId, rehydrated.PhotoId);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
