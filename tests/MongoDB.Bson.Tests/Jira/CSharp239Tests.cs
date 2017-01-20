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
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Bson.Tests.Jira.CSharp239
{
    public class CSharp239Tests
    {
        public class Tree
        {
            public string Node;
            [BsonIgnoreIfNull]
            public Tree Left;
            [BsonIgnoreIfNull]
            public Tree Right;
        }

        [Fact]
        public void TestSerialization()
        {
            var obj = new Tree
            {
                Node = "top",
                Left = new Tree { Node = "left" },
                Right = new Tree { Node = "right" }
            };
            var json = obj.ToJson();
            var expected = "{ 'Node' : 'top', 'Left' : { 'Node' : 'left' }, 'Right' : { 'Node' : 'right' } }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = obj.ToBson();
            var rehydrated = BsonSerializer.Deserialize<Tree>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
