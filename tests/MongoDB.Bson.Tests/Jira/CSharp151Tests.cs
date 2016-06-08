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

using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.Jira.CSharp151
{
    public class CSharp151Tests
    {
        public class Doc
        {
            public decimal Value { get; set; }
        }

        [Fact]
        public void TestDeserializeDouble()
        {
            var json = "{ 'Value' : 1.23 }".Replace("'", "\"");
            var doc = BsonSerializer.Deserialize<Doc>(json);
            Assert.Equal(1.23m, doc.Value);
        }

        [Fact]
        public void TestDeserializeInt32()
        {
            var json = "{ 'Value' : 123 }".Replace("'", "\"");
            var doc = BsonSerializer.Deserialize<Doc>(json);
            Assert.Equal(123m, doc.Value);
        }

        [Fact]
        public void TestDeserializeInt64()
        {
            var json = "{ 'Value' : 12345678900 }".Replace("'", "\"");
            var doc = BsonSerializer.Deserialize<Doc>(json);
            Assert.Equal(12345678900m, doc.Value);
        }
    }
}
