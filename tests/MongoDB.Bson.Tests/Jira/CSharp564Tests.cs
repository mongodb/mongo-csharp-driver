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

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.Jira.CSharp564
{
    public class CSharp564Tests
    {
        interface IWhatever { }
        class Whatever : IWhatever { }

        class Person
        {
            public IWhatever Whatever { get; set; }
        }

        [Fact]
        public void TestPersonWhateverNull()
        {
            var p = new Person();
            var json = p.ToJson();
            var expected = "{ 'Whatever' : null }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var r = BsonSerializer.Deserialize<Person>(json);
            Assert.Equal(null, r.Whatever);
        }

        [Fact]
        public void TestPersonWhateverNotNull()
        {
            var p = new Person() { Whatever = new Whatever() };
            var json = p.ToJson();
            var expected = "{ 'Whatever' : { '_t' : 'Whatever' } }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var r = BsonSerializer.Deserialize<Person>(json);
            Assert.IsType<Whatever>(r.Whatever);
        }
    }
}
