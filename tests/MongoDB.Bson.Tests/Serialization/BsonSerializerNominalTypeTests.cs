/* Copyright 2019-present MongoDB Inc.
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

using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using System;
using System.IO;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class BsonSerializerNominalTypeTests
    {
        [Fact]
        public void Serialize_generic_should_default_args_nominal_type_to_TNominalType()
        {
            var c = new C { X = 1 };

            using (var stringWriter = new StringWriter())
            using (var writer = new JsonWriter(stringWriter))
            {
                BsonSerializer.Serialize(writer, c);
                var json = stringWriter.ToString();

                json.Should().Be("{ \"X\" : 1 }");
            }
        }

        [Fact]
        public void Serialize_generic_should_throw_when_args_nominal_type_is_not_TNominalType()
        {
            var c = new C { X = 1 };

            using (var stringWriter = new StringWriter())
            using (var writer = new JsonWriter(stringWriter))
            {
                var args = new BsonSerializationArgs { NominalType = typeof(object) };

                var exception = Record.Exception(() => BsonSerializer.Serialize(writer, c, args: args));

                var e = exception.Should().BeOfType<ArgumentException>().Subject;
                e.ParamName.Should().Be("args");
                e.Message.Should().StartWith("args.NominalType must be equal to <TNominalType>."); ;
            }
        }

        [Fact]
        public void Serialize_nongeneric_should_default_args_nominal_type_to_TNominalType()
        {
            var c = new C { X = 1 };

            using (var stringWriter = new StringWriter())
            using (var writer = new JsonWriter(stringWriter))
            {
                BsonSerializer.Serialize(writer, typeof(C), c);
                var json = stringWriter.ToString();

                json.Should().Be("{ \"X\" : 1 }");
            }
        }

        [Fact]
        public void Serialize_nongeneric_should_throw_when_args_nominal_type_is_not_TNominalType()
        {
            var c = new C { X = 1 };

            using (var stringWriter = new StringWriter())
            using (var writer = new JsonWriter(stringWriter))
            {
                var args = new BsonSerializationArgs { NominalType = typeof(object) };

                var exception = Record.Exception(() => BsonSerializer.Serialize(writer, typeof(C), c, args: args));

                var e = exception.Should().BeOfType<ArgumentException>().Subject;
                e.ParamName.Should().Be("args");
                e.Message.Should().StartWith("args.NominalType must be equal to nominalType."); ;
            }
        }

        // nested types
        private class C
        {
            public int X { get; set; }
        }
    }
}
