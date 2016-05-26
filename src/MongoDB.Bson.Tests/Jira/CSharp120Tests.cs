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

using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.Jira.CSharp120
{
    public abstract class B
    {
        public int X { get; set; }
        public abstract int Y { get; set; }
    }

    public class C : B
    {
        public override int Y { get; set; }
    }

    public class CSharp120Tests
    {
        [Fact]
        public void TestGuidStringRepresentation()
        {
            var c = new C { X = 1, Y = 2 };
            var json = c.ToJson();
            var expected = "{ 'X' : 1, 'Y' : 2 }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.IsType<C>(rehydrated);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
