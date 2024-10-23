/* Copyright 2010-present MongoDB Inc.
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
using System.Dynamic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    [Collection(RegisterObjectSerializerFixture.CollectionName)]
    public class ExpandoSerializerTests
    {
        [Fact]
        public void TestRoundTrip()
        {
            dynamic person = new ExpandoObject();

            person.FirstName = "Jack";
            person.LastName = "McJack";
            dynamic hobby1 = new ExpandoObject();
            hobby1.Name = "hiking";
            person.Hobbies = new List<dynamic> { hobby1, 10 };
            person.Spouse = new ExpandoObject();
            person.Spouse.FirstName = "Jane";
            person.Spouse.LastName = "McJane";

            var json = ((ExpandoObject)person).ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ 'FirstName' : 'Jack', 'LastName' : 'McJack', 'Hobbies' : [{ 'Name' : 'hiking' }, 10], 'Spouse' : { 'FirstName' : 'Jane', 'LastName' : 'McJane' } }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = ((ExpandoObject)person).ToBson();
            var rehydrated = BsonSerializer.Deserialize<ExpandoObject>(bson);
            Assert.True(bson.SequenceEqual((rehydrated).ToBson()));
        }

        [Fact]
        public void TestNestedExpandoRoundTrip()
        {
            var document = new NestedExpando {Id = ObjectId.GenerateNewId(), ExtraData = new ExpandoObject()};

            var json = document.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = $"{{ \"_id\" : ObjectId(\"{document.Id}\"), \"ExtraData\" : {{ }} }}";
            Assert.Equal(expected, json);

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<NestedExpando>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Fact]
        public void TestNullNestedExpandoRoundTrip()
        {
            var document = new NestedExpando {Id = ObjectId.GenerateNewId(), ExtraData = null};

            var json = document.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = $"{{ \"_id\" : ObjectId(\"{document.Id}\"), \"ExtraData\" : null }}";
            Assert.Equal(expected, json);

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<NestedExpando>(bson);
            Assert.True(bson.SequenceEqual(rehydrated.ToBson()));
        }

        class NestedExpando
        {
            public ObjectId Id { get; set; }
            public ExpandoObject ExtraData { get; set; }
        }

#if NET472
        [Fact]
        public void TestDeserializingDiscriminatedVersion()
        {
            var oldJson = "{ 'FirstName' : 'Jack', 'LastName' : 'McJack', 'Hobbies' : { '_t' : 'System.Collections.Generic.List`1[System.Object]', '_v' : [{ '_t' : 'System.Dynamic.ExpandoObject', '_v' : { 'Name' : 'hiking' } }, 10] }, 'Spouse' : { '_t' : 'System.Dynamic.ExpandoObject', '_v' : { 'FirstName' : 'Jane', 'LastName' : 'McJane' } } }".Replace("'", "\"");
            var rehydrated = BsonSerializer.Deserialize<ExpandoObject>(oldJson);

            var json = ((ExpandoObject)rehydrated).ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "{ 'FirstName' : 'Jack', 'LastName' : 'McJack', 'Hobbies' : [{ 'Name' : 'hiking' }, 10], 'Spouse' : { 'FirstName' : 'Jane', 'LastName' : 'McJane' } }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }
#endif
    }

    public class ExpandoObjectSerializerTests
    {
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new ExpandoObjectSerializer();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new ExpandoObjectSerializer();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new ExpandoObjectSerializer();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new ExpandoObjectSerializer();
            var y = new ExpandoObjectSerializer();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new ExpandoObjectSerializer();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }
}
