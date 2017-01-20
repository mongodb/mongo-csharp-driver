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
using System.IO;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.Jira
{
    public class CSharp351Tests
    {
        private class C
        {
            public int _id { get; set; }
            public N N { get; set; }
        }

        private class N
        {
            public int X { get; set; }
        }

        [Fact]
        public void TestErrorMessage()
        {
            var json = "{ _id : 1, N : 'should be a document, not a string' }";
            var ex = Assert.Throws<FormatException>(() => BsonSerializer.Deserialize<C>(json));
            
            Assert.IsType<FormatException>(ex.InnerException);
            var expected = "An error occurred while deserializing the N property of class MongoDB.Bson.Tests.Jira.CSharp351Tests+C: Expected a nested document representing the serialized form of a MongoDB.Bson.Tests.Jira.CSharp351Tests+N value, but found a value of type String instead.";
            Assert.Equal(expected, ex.Message);
        }
    }
}
