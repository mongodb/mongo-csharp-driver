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

using System.Collections;
using MongoDB.Bson;
using Xunit;

namespace MongoDB.Bson.Tests.Jira
{
    public class CSharp276Tests
    {
        [Fact]
        public void TestConstructorWithNonGenericIEnumerable()
        {
            IEnumerable values = new object[] { 1, "a" };
            var array = new BsonArray(values);
            Assert.Equal(2, array.Count);
            Assert.Equal(BsonType.Int32, array[0].BsonType);
            Assert.Equal(BsonType.String, array[1].BsonType);
            Assert.Equal(1, array[0].AsInt32);
            Assert.Equal("a", array[1].AsString);
        }

        [Fact]
        public void TestCreateWithNonGenericIEnumerable()
        {
            IEnumerable values = new object[] { 1, "a" };
            var array = new BsonArray(values);
            Assert.Equal(2, array.Count);
            Assert.Equal(BsonType.Int32, array[0].BsonType);
            Assert.Equal(BsonType.String, array[1].BsonType);
            Assert.Equal(1, array[0].AsInt32);
            Assert.Equal("a", array[1].AsString);
        }
    }
}
