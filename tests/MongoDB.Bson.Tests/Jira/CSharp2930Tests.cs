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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Bson.Tests.Jira
{
    public class CSharp2930Tests
    {
        [Fact]
        public void Deserialize_Guid_property_with_undefined_representation_should_throw()
        {
            var json = "{ \"_id\" : 1, \"G\" : UUID(\"01020304-0506-0708-090a-0b0c0d0e0f10\") }";

            var exception = Record.Exception(() => BsonSerializer.Deserialize<C1>(json));

            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Contain("GuidSerializer cannot deserialize a Guid when GuidRepresentation is Unspecified");
        }

        [Fact]
        public void Deserialize_Guid_property_with_defined_representation_should_work()
        {
            var json = "{ \"_id\" : 1, \"G\" : UUID(\"01020304-0506-0708-090a-0b0c0d0e0f10\") }";

            var c = BsonSerializer.Deserialize<C2>(json);

            c.Id.Should().Be(1);
            c.G.Should().Be(Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10"));
        }

        [Fact]
        public void Serialize_Guid_property_with_undefined_representation_should_throw()
        {
            var c = new C1 { Id = 1, G = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10") };

            var exception = Record.Exception(() => c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell }));

            exception.Should().BeOfType<BsonSerializationException>();
            exception.Message.Should().Contain("GuidSerializer cannot serialize a Guid when GuidRepresentation is Unspecified");
        }

        [Fact]
        public void Serialize_Guid_property_with_defined_representation_should_work()
        {
            var c = new C2 { Id = 1, G = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10") };

            var json = c.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });

            json.Should().Be("{ \"_id\" : 1, \"G\" : UUID(\"01020304-0506-0708-090a-0b0c0d0e0f10\") }");
        }

        public class C1
        {
            public int Id { get; set; }
            public Guid G { get; set; }
        }

        public class C2
        {
            public int Id { get; set; }
            [BsonGuidRepresentation(GuidRepresentation.Standard)]
            public Guid G { get; set; }
        }
    }
}
