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

using System;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Jira
{
    public class CSharp4511Tests
    {
        static CSharp4511Tests()
        {
            var objectSerializerWithAllowedTypes = new ObjectSerializer(
                BsonSerializer.LookupDiscriminatorConvention(typeof(object)),
                GuidRepresentation.Standard,
                t => t == typeof(AllowedToBeDeserialized),
                t => t == typeof(AllowedToBeSerialized));

            BsonClassMap.RegisterClassMap<C>(classMap =>
            {
                classMap.AutoMap();
                classMap.MapProperty(x => x.Object).SetSerializer(objectSerializerWithAllowedTypes);
            });
        }

        [Fact]
        public void Deserialize_should_return_expected_result_when_deserializing_an_allowed_type()
        {
            var json = "{ Object : { _t : 'MongoDB.Bson.Tests.Jira.CSharp4511Tests+AllowedToBeDeserialized, MongoDB.Bson.Tests', X : 1  } }";

            var result = BsonSerializer.Deserialize<C>(json);

            var allowed = result.Object.Should().BeOfType<AllowedToBeDeserialized>().Subject;
            allowed.X.Should().Be(1);
        }

        [Fact]
        public void Deserialize_should_throw_when_deserializing_a_not_allowed_type()
        {
            var json = "{ Object : { _t : 'MongoDB.Bson.Tests.Jira.CSharp4511Tests+NotAllowed, MongoDB.Bson.Tests', X : 1  } }";

            var exception = Record.Exception(() => BsonSerializer.Deserialize<C>(json));

            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Be("An error occurred while deserializing the Object property of class MongoDB.Bson.Tests.Jira.CSharp4511Tests+C: Type MongoDB.Bson.Tests.Jira.CSharp4511Tests+NotAllowed is not configured as a type that is allowed to be deserialized for this instance of ObjectSerializer.");
        }

        [Fact]
        public void Serialize_should_have_expected_result_when_serializing_an_allowed_type()
        {
            var c = new C { Object = new AllowedToBeSerialized { X = 1 } };

            var result = c.ToJson();

            result.Should().Be("{ \"Object\" : { \"_t\" : \"AllowedToBeSerialized\", \"X\" : 1 } }");
        }

        [Fact]
        public void Serialize_should_throw_when_serializing_a_not_allowed_type()
        {
            var c = new C { Object = new NotAllowed { X = 1 } };

            var exception = Record.Exception(() => c.ToJson());

            exception.Should().BeOfType<BsonSerializationException>();
            exception.Message.Should().Be("An error occurred while serializing the Object property of class MongoDB.Bson.Tests.Jira.CSharp4511Tests+C: Type MongoDB.Bson.Tests.Jira.CSharp4511Tests+NotAllowed is not configured as a type that is allowed to be serialized for this instance of ObjectSerializer.");
        }

        public class C
        {
            public object Object { get; set; }
        }

        public class AllowedToBeDeserialized
        {
            public int X { get; set; }
        }

        public class AllowedToBeSerialized
        {
            public int X { get; set; }
        }

        public class NotAllowed
        {
            public int X { get; set; }
        }
    }
}
