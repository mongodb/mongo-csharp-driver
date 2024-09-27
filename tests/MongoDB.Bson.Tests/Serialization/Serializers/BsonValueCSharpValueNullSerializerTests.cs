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
using Xunit;

namespace MongoDB.Bson.Serialization.Serializers
{
    public class BsonValueCSharpNullSerializerTests
    {
        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = new BsonValueCSharpNullSerializer<BsonValue>(BsonValueSerializer.Instance);
            var y = new DerivedFromBsonValueCSharpNullSerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new BsonValueCSharpNullSerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new BsonValueCSharpNullSerializer<BsonValue>(BsonValueSerializer.Instance);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new BsonValueCSharpNullSerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new BsonValueCSharpNullSerializer<BsonValue>(BsonValueSerializer.Instance);
            var y = new BsonValueCSharpNullSerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = new BsonValueCSharpNullSerializer<BsonValue>(new BsonValueSerializer1());
            var y = new BsonValueCSharpNullSerializer<BsonValue>(new BsonValueSerializer2());

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new BsonValueCSharpNullSerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        public class DerivedFromBsonValueCSharpNullSerializer<TBsonValue> : BsonValueCSharpNullSerializer<TBsonValue>
            where TBsonValue : BsonValue
        {
            public DerivedFromBsonValueCSharpNullSerializer(IBsonSerializer<TBsonValue> wrappedSerializer)
                : base(wrappedSerializer)
            {
            }
        }
    }

    public class BsonValueCSharpNullArrayAndDocumentSerializerTests
    {
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new BsonValueCSharpNullArrayAndDocumentSerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new BsonValueCSharpNullArrayAndDocumentSerializer<BsonValue>(BsonValueSerializer.Instance);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new BsonValueCSharpNullArrayAndDocumentSerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new BsonValueCSharpNullArrayAndDocumentSerializer<BsonValue>(BsonValueSerializer.Instance);
            var y = new BsonValueCSharpNullArrayAndDocumentSerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = new BsonValueCSharpNullArrayAndDocumentSerializer<BsonValue>(new BsonValueSerializer1());
            var y = new BsonValueCSharpNullArrayAndDocumentSerializer<BsonValue>(new BsonValueSerializer2());

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new BsonValueCSharpNullArrayAndDocumentSerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }

    public class BsonValueCSharpNullArraySerializerTests
    {
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new BsonValueCSharpNullArraySerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new BsonValueCSharpNullArraySerializer<BsonValue>(BsonValueSerializer.Instance);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new BsonValueCSharpNullArraySerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new BsonValueCSharpNullArraySerializer<BsonValue>(BsonValueSerializer.Instance);
            var y = new BsonValueCSharpNullArraySerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = new BsonValueCSharpNullArraySerializer<BsonValue>(new BsonValueSerializer1());
            var y = new BsonValueCSharpNullArraySerializer<BsonValue>(new BsonValueSerializer2());

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new BsonValueCSharpNullArraySerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }

    public class BsonValueCSharpNullDocumentSerializerTests
    {
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new BsonValueCSharpNullDocumentSerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new BsonValueCSharpNullDocumentSerializer<BsonValue>(BsonValueSerializer.Instance);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new BsonValueCSharpNullDocumentSerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new BsonValueCSharpNullDocumentSerializer<BsonValue>(BsonValueSerializer.Instance);
            var y = new BsonValueCSharpNullDocumentSerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = new BsonValueCSharpNullDocumentSerializer<BsonValue>(new BsonValueSerializer1());
            var y = new BsonValueCSharpNullDocumentSerializer<BsonValue>(new BsonValueSerializer2());

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new BsonValueCSharpNullDocumentSerializer<BsonValue>(BsonValueSerializer.Instance);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }

    public class BsonValueSerializer1 : SerializerBase<BsonValue> { }

    public class BsonValueSerializer2 : SerializerBase<BsonValue> { }
}
