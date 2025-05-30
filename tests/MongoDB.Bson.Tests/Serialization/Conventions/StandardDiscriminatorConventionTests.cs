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
using MongoDB.Bson.Serialization.Conventions;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Conventions
{
    public class StandardDiscriminatorConventionTests
    {
        [Fact]
        public void TestConstructorThrowsWhenElementNameContainsNulls()
        {
            Assert.Throws<ArgumentException>(() => new ScalarDiscriminatorConvention("a\0b"));
        }

        [Fact]
        public void TestConstructorThrowsWhenElementNameIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ScalarDiscriminatorConvention(null));
        }

        [Fact]
        public void Equals_derived_should_return_false()
        {
            var x = (StandardDiscriminatorConvention)new ConcreteStandardDiscriminatorConvention("_t");
            var y = new DerivedFromConcreteStandardDiscriminatorConvention("_t");

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = (StandardDiscriminatorConvention)new ConcreteStandardDiscriminatorConvention("_t");

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = (StandardDiscriminatorConvention)new ConcreteStandardDiscriminatorConvention("_t");
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = (StandardDiscriminatorConvention)new ConcreteStandardDiscriminatorConvention("_t");

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = (StandardDiscriminatorConvention)new ConcreteStandardDiscriminatorConvention("_t");
            var y = (StandardDiscriminatorConvention)new ConcreteStandardDiscriminatorConvention("_t");

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = (StandardDiscriminatorConvention)new ConcreteStandardDiscriminatorConvention("_t");
            var y = (StandardDiscriminatorConvention)new ConcreteStandardDiscriminatorConvention("_u");

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = (StandardDiscriminatorConvention)new ConcreteStandardDiscriminatorConvention("_t");

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        public class ConcreteStandardDiscriminatorConvention : StandardDiscriminatorConvention
        {
            public ConcreteStandardDiscriminatorConvention(string elementName)
                : base(elementName)
            {
            }

            public override BsonValue GetDiscriminator(Type nominalType, Type actualType) => throw new NotImplementedException();
            public override BsonValue GetDiscriminator(Type nominalType, Type actualType, IBsonSerializationDomain domain)
            {
                throw new NotImplementedException();
            }
        }

        public class DerivedFromConcreteStandardDiscriminatorConvention : ConcreteStandardDiscriminatorConvention
        {
            public DerivedFromConcreteStandardDiscriminatorConvention(string elementName)
                : base(elementName)
            {
            }

            public override BsonValue GetDiscriminator(Type nominalType, Type actualType) => throw new NotImplementedException();
        }
    }
}
