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
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace MongoDB.Bson.Serialization.Serializers
{
    public class BsonDocumentBackedClassSerializerTests
    {
        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = (BsonDocumentBackedClassSerializer<C>)new ConcreteBsonDocumentBackedClassSerializer<C>();
            var y = new DerivedFromConcreteBsonDocumentBackedClassSerializer<C>();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = (BsonDocumentBackedClassSerializer<C>)new ConcreteBsonDocumentBackedClassSerializer<C>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = (BsonDocumentBackedClassSerializer<C>)new ConcreteBsonDocumentBackedClassSerializer<C>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = (BsonDocumentBackedClassSerializer<C>)new ConcreteBsonDocumentBackedClassSerializer<C>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = (BsonDocumentBackedClassSerializer<C>)new ConcreteBsonDocumentBackedClassSerializer<C>();
            var y = (BsonDocumentBackedClassSerializer<C>)new ConcreteBsonDocumentBackedClassSerializer<C>();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = (BsonDocumentBackedClassSerializer<C>)new ConcreteBsonDocumentBackedClassSerializer<C>();
            var y = (BsonDocumentBackedClassSerializer<C>)new ConcreteBsonDocumentBackedClassSerializer<C>();
            var registerMemberMethod = typeof(BsonDocumentBackedClassSerializer<C>).GetMethod("RegisterMember", BindingFlags.NonPublic | BindingFlags.Instance);
            registerMemberMethod.Invoke(y, new object[] { "Y", "y", Int32Serializer.Instance });

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = (BsonDocumentBackedClassSerializer<C>)new ConcreteBsonDocumentBackedClassSerializer<C>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        private class ConcreteBsonDocumentBackedClassSerializer<TClass> : BsonDocumentBackedClassSerializer<TClass>
            where TClass : BsonDocumentBackedClass
        {
            public ConcreteBsonDocumentBackedClassSerializer()
            {
                RegisterMember("X", "x", Int32Serializer.Instance);
            }

            protected override TClass CreateInstance(BsonDocument backingDocument) => throw new NotImplementedException();
        }

        private class DerivedFromConcreteBsonDocumentBackedClassSerializer<TClass> : ConcreteBsonDocumentBackedClassSerializer<TClass>
            where TClass : BsonDocumentBackedClass
        {
            public DerivedFromConcreteBsonDocumentBackedClassSerializer()
            {
            }
        }

        private class C : BsonDocumentBackedClass
        {
            public C(BsonDocument document) : base(document, new ConcreteBsonDocumentBackedClassSerializer<C>()) { }
        }
    }
}
