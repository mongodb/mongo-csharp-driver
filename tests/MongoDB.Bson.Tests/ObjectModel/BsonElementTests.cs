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

using System.Collections.Generic;
using MongoDB.Bson;
using Xunit;

namespace MongoDB.Bson.Tests
{
    public class BsonElementTests
    {
        [Fact]
        public void TestNewBsonArray()
        {
            new BsonArray(new List<int>() { 1, 2, 3 });
            new BsonArray(new int[] { 4, 5, 6 });
        }

        [Fact]
        public void TestStringElement()
        {
            BsonElement element = new BsonElement("abc", "def");
            string value = element.Value.AsString;
            Assert.Equal("abc", element.Name);
            Assert.Equal("def", value);
        }
    }
}
