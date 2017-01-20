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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.Jira
{
    public class CSharp83Tests
    {
        private class Student
        {
            public ObjectId Id { get; set; }
            public List<int> Scores { get; set; }
        }

        [Fact]
        public void TestSerialization()
        {
            var student = new Student { Id = ObjectId.Empty, Scores = new List<int> { 1, 2 } };
            var json = student.ToJson();
            var expected = "{ '_id' : ObjectId('000000000000000000000000'), 'Scores' : [1, 2] }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = student.ToBson();
            var rehydrated = BsonSerializer.Deserialize<Student>(bson);
            Assert.IsType<List<int>>(rehydrated.Scores);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
