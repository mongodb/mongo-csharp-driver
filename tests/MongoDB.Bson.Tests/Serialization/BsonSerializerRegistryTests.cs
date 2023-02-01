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
using System.Collections.Generic;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Moq;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class BsonSerializerRegistryTests
    {
        [Fact]
        public void RegisterSerializer_should_work()
        {
            var subject = new BsonSerializerRegistry();
            var serializer = new ObjectSerializer();

            subject.RegisterSerializer(typeof(object), serializer);

            subject.GetSerializer(typeof(object)).Should().BeSameAs(serializer);
        }

        [Fact]
        public void RegisterSerializer_should_throw_when_type_is_null()
        {
            var subject = new BsonSerializerRegistry();
            var serializer = new ObjectSerializer();

            var exception = Record.Exception(() => subject.RegisterSerializer(type: null, serializer));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("type");
        }

        [Fact]
        public void RegisterSerializer_should_throw_when_type_is_BsonValue()
        {
            var subject = new BsonSerializerRegistry();
            var serializer = BsonValueSerializer.Instance;

            var exception = Record.Exception(() => subject.RegisterSerializer(typeof(BsonValue), serializer));

            exception.Should().BeOfType<BsonSerializationException>();
            exception.Message.Should().Contain("A serializer cannot be registered for type BsonValue because it is a subclass of BsonValue");
        }

        [Fact]
        public void RegisterSerializer_should_throw_when_type_is_not_closed_generic_type()
        {
            var subject = new BsonSerializerRegistry();
            var serializer = Mock.Of<IBsonSerializer<object>>();

            var exception = Record.Exception(() => subject.RegisterSerializer(typeof(List<>), serializer));

            var argumentException = exception.Should().BeOfType<ArgumentException>().Subject;
            argumentException.ParamName.Should().Be("type");
            argumentException.Message.Should().Contain("Generic type List<T> has unassigned type parameters");
        }

        [Fact]
        public void RegisterSerializer_should_throw_when_serializer_is_null()
        {
            var subject = new BsonSerializerRegistry();

            var exception = Record.Exception(() => subject.RegisterSerializer(typeof(object), serializer: null));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("serializer");
        }

        [Fact]
        public void RegisterSerializer_should_throw_when_serializer_is_already_registered()
        {
            var subject = new BsonSerializerRegistry();
            var serializer = new ObjectSerializer();

            subject.RegisterSerializer(typeof(object), serializer);
            var exception = Record.Exception(() => subject.RegisterSerializer(typeof(object), serializer));

            exception.Should().BeOfType<BsonSerializationException>();
            exception.Message.Should().Contain("There is already a serializer registered for type Object");
        }

        [Fact]
        public void TryRegisterSerializer_should_return_true_when_serializer_is_not_already_registered()
        {
            var subject = new BsonSerializerRegistry();
            var serializer = new ObjectSerializer();

            var result = subject.TryRegisterSerializer(typeof(object), serializer);

            result.Should().BeTrue();
            subject.GetSerializer(typeof(object)).Should().BeSameAs(serializer);
        }

        [Fact]
        public void TryRegisterSerializer_should_return_false_when_equivalent_serializer_is_already_registered()
        {
            var subject = new BsonSerializerRegistry();
            var serializer1 = new ObjectSerializer(ObjectSerializer.DefaultAllowedTypes);
            var serializer2 = new ObjectSerializer(ObjectSerializer.DefaultAllowedTypes);

            var result1 = subject.TryRegisterSerializer(typeof(object), serializer1);
            var result2 = subject.TryRegisterSerializer(typeof(object), serializer2);

            result1.Should().BeTrue();
            result2.Should().BeFalse();
            subject.GetSerializer(typeof(object)).Should().BeSameAs(serializer1);
            subject.GetSerializer(typeof(object)).Should().NotBeSameAs(serializer2);
        }

        [Fact]
        public void TryRegisterSerializer_should_throw_when_type_is_null()
        {
            var subject = new BsonSerializerRegistry();
            var serializer = new ObjectSerializer();

            var exception = Record.Exception(() => subject.TryRegisterSerializer(type: null, serializer));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("type");
        }

        [Fact]
        public void TryRegisterSerializer_should_throw_when_type_is_BsonValue()
        {
            var subject = new BsonSerializerRegistry();
            var serializer = BsonValueSerializer.Instance;

            var exception = Record.Exception(() => subject.TryRegisterSerializer(typeof(BsonValue), serializer));

            exception.Should().BeOfType<BsonSerializationException>();
            exception.Message.Should().Contain("A serializer cannot be registered for type BsonValue because it is a subclass of BsonValue");
        }

        [Fact]
        public void TryRegisterSerializer_should_throw_when_type_is_not_closed_generic_type()
        {
            var subject = new BsonSerializerRegistry();
            var serializer = Mock.Of<IBsonSerializer<object>>();

            var exception = Record.Exception(() => subject.TryRegisterSerializer(typeof(List<>), serializer));

            var argumentException = exception.Should().BeOfType<ArgumentException>().Subject;
            argumentException.ParamName.Should().Be("type");
            argumentException.Message.Should().Contain("Generic type List<T> has unassigned type parameters");
        }

        [Fact]
        public void TryRegisterSerializer_should_throw_when_serializer_is_null()
        {
            var subject = new BsonSerializerRegistry();

            var exception = Record.Exception(() => subject.TryRegisterSerializer(typeof(object), serializer: null));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("serializer");
        }

        [Fact]
        public void TryRegisterSerializer_should_throw_when_different_serializer_is_already_registered()
        {
            var subject = new BsonSerializerRegistry();
            var serializer1 = new ObjectSerializer(ObjectSerializer.DefaultAllowedTypes);
            var serializer2 = new ObjectSerializer(ObjectSerializer.AllAllowedTypes);

            var result1 = subject.TryRegisterSerializer(typeof(object), serializer1);
            var exception = Record.Exception(() => subject.TryRegisterSerializer(typeof(object), serializer2));

            result1.Should().BeTrue();
            subject.GetSerializer(typeof(object)).Should().BeSameAs(serializer1);
            subject.GetSerializer(typeof(object)).Should().NotBeSameAs(serializer2);
            exception.Should().BeOfType<BsonSerializationException>();
            exception.Message.Should().Contain("There is already a different serializer registered for type Object");
        }
    }
}
