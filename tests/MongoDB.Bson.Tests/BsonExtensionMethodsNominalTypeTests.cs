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
using MongoDB.Bson.Serialization;
using System;
using Xunit;

namespace MongoDB.Bson.Tests
{
    public class BsonExtensionMethodsNominalTypeTests
    {
        [Fact]
        public void ToBson_generic_should_default_args_nominal_type_to_TNominalType()
        {
            var c = new C { X = 1 };

            var bytes = c.ToBson();

            var document = BsonSerializer.Deserialize<BsonDocument>(bytes);
            document.Should().Be("{ X : 1 }");
        }

        [Fact]
        public void ToBson_generic_should_throw_when_args_nominal_type_is_not_TNominalType()
        {
            var c = new C { X = 1 };
            var args = new BsonSerializationArgs { NominalType = typeof(object) };

            var exception = Record.Exception(() => c.ToBson(args: args));

            var e = exception.Should().BeOfType<ArgumentException>().Subject;
            e.ParamName.Should().Be("args");
            e.Message.Should().StartWith("args.NominalType must be equal to <TNominalType>.");
        }

        [Fact]
        public void ToBson_nongeneric_should_default_args_nominal_type_to_nominalType()
        {
            var c = new C { X = 1 };

            var bytes = c.ToBson(typeof(C));

            var document = BsonSerializer.Deserialize<BsonDocument>(bytes);
            document.Should().Be("{ X : 1 }");
        }

        [Fact]
        public void ToBson_nongeneric_should_throw_when_args_nominal_type_is_not_nominalType()
        {
            var c = new C { X = 1 };
            var args = new BsonSerializationArgs { NominalType = typeof(object) };

            var exception = Record.Exception(() => c.ToBson(typeof(C), args: args));

            var e = exception.Should().BeOfType<ArgumentException>().Subject;
            e.ParamName.Should().Be("args");
            e.Message.Should().StartWith("args.NominalType must be equal to nominalType.");
        }

        [Fact]
        public void ToBsonDocument_generic_should_default_args_nominal_type_to_TNominalType()
        {
            var c = new C { X = 1 };

            var document = c.ToBsonDocument();

            document.Should().Be("{ X : 1 }");
        }

        [Fact]
        public void ToBsonDocument_generic_should_throw_when_args_nominal_type_is_not_TNominalType()
        {
            var c = new C { X = 1 };
            var args = new BsonSerializationArgs { NominalType = typeof(object) };

            var exception = Record.Exception(() => c.ToBsonDocument(args: args));

            var e = exception.Should().BeOfType<ArgumentException>().Subject;
            e.ParamName.Should().Be("args");
            e.Message.Should().StartWith("args.NominalType must be equal to <TNominalType>.");
        }

        [Fact]
        public void ToBsonDocument_nongeneric_should_default_args_nominal_type_to_nominalType()
        {
            var c = new C { X = 1 };

            var document = c.ToBsonDocument(typeof(C));

            document.Should().Be("{ X : 1 }");
        }

        [Fact]
        public void ToBsonDocument_nongeneric_should_throw_when_args_nominal_type_is_not_nominalType()
        {
            var c = new C { X = 1 };
            var args = new BsonSerializationArgs { NominalType = typeof(object) };

            var exception = Record.Exception(() => c.ToBsonDocument(typeof(C), args: args));

            var e = exception.Should().BeOfType<ArgumentException>().Subject;
            e.ParamName.Should().Be("args");
            e.Message.Should().StartWith("args.NominalType must be equal to nominalType.");
        }

        [Fact]
        public void ToJson_generic_should_default_args_nominal_type_to_TNominalType()
        {
            var c = new C { X = 1 };

            var json = c.ToJson();

            json.Should().Be("{ \"X\" : 1 }");
        }

        [Fact]
        public void ToJson_generic_should_throw_when_args_nominal_type_is_not_TNominalType()
        {
            var c = new C { X = 1 };
            var args = new BsonSerializationArgs { NominalType = typeof(object) };

            var exception = Record.Exception(() => c.ToJson(args: args));

            var e = exception.Should().BeOfType<ArgumentException>().Subject;
            e.ParamName.Should().Be("args");
            e.Message.Should().StartWith("args.NominalType must be equal to <TNominalType>.");
        }

        [Fact]
        public void ToJson_nongeneric_should_default_args_nominal_type_to_nominalType()
        {
            var c = new C { X = 1 };

            var json = c.ToJson(typeof(C));

            json.Should().Be("{ \"X\" : 1 }");
        }

        [Fact]
        public void ToJson_nongeneric_should_throw_when_args_nominal_type_is_not_nominalType()
        {
            var c = new C { X = 1 };
            var args = new BsonSerializationArgs { NominalType = typeof(object) };

            var exception = Record.Exception(() => c.ToJson(typeof(C), args: args));

            var e = exception.Should().BeOfType<ArgumentException>().Subject;
            e.ParamName.Should().Be("args");
            e.Message.Should().StartWith("args.NominalType must be equal to nominalType.");
        }

        // nested types
        private class C
        {
            public int X { get; set; }
        }
    }
}
