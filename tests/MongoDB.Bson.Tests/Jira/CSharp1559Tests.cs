/* Copyright 2020-present MongoDB Inc.
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
using System.Linq;
using System.Reflection;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.TestHelpers;
using Xunit;

namespace MongoDB.Bson.Tests.Jira
{
    public class CSharp1559Tests
    {
        [Theory]
        [InlineData(typeof(DerivedWithoutSetter_BaseWithoutSetter), new[] { 1, 2 }, "{ \"X\" : 1, \"Y\" : 2 }")]
        [InlineData(typeof(DerivedWithoutSetter_BaseWithPrivateSetterAndWithProtectedConstructor), new[] { 1, 2 }, "{ \"X\" : 1, \"Y\" : 2 }")]
        [InlineData(typeof(DerivedWithoutSetterAndWithBsonElement_BaseWithoutSetterAndWithProtectedConstructor), new[] { 1, 2 }, "{ \"X\" : 1, \"y\" : 2 }")]
        [InlineData(typeof(DerivedWithoutSetter_BaseWithoutSetterAndWithProtectedInternalConstructor), new[] { 1, 2 }, "{ \"X\" : 1, \"Y\" : 2 }")]
        [InlineData(typeof(DerivedWithoutSetterAndWithBsonElementAndWithPrivateConstructor_BaseWithoutSetter), new[] { 1, 2 }, "{ \"X\" : 1, \"y\" : 2 }")]
        [InlineData(typeof(DerivedWithoutSetterAndWithoutBsonElement_AbstractBaseWithoutSetterAndWithProtectedConstructor), new[] { 1, 2 }, "{ \"X\" : 1, \"Y\" : 2 }")]
        [InlineData(typeof(DerivedWithoutSetterAndWithoutBsonElement_AbstractBaseWithoutSetter), new[] { 1, 2 }, "{ \"X\" : 1, \"Y\" : 2 }")]
        [InlineData(typeof(DerivedWithPrivateSetterAndWithBsonElement_BaseWithoutSetter), new[] { 1, 2 }, "{ \"X\" : 1, \"y\" : 2 }")]
        [InlineData(typeof(DerivedImmutableWithMorePropertiesThanInConstructorAndWithBsonConstructorAttribute_AbstractBaseImmutable), new[] { 2 }, "{ \"Y\" : 2 }")]
        [InlineData(typeof(DerivedImmutableWithMorePropertiesThanInConstructorAndWithBsonConstructorAttribute_AbstractBaseImmutableWithConstructor), new[] { 1, 2 }, "{ \"X\" : 1, \"Y\" : 2  }")]
        [InlineData(typeof(DerivedImmutableWithMorePropertiesThanInConstructorButWithBsonElementAttributeAndWithBsonConstructorAttribute_AbstractBaseImmutable), new[] { 2 }, "{ \"Y\" : 2 }")]
        [InlineData(typeof(DerivedImmutableWithMorePropertiesThanInConstructorButWithBsonElementAttributeAndWithBsonConstructorAttribute_AbstractBaseImmutableWithConstructor), new[] { 1, 2 }, "{ \"X\" : 1, \"Y\" : 2 }")]
        [InlineData(typeof(DerivedImmutableWithMorePropertiesThanInConstructorButWithBsonIgnoreAttribute_AbstractBaseImmutableWithAbstractProperty), new[] { 2 }, "{ \"Y\" : 2 }")]
        [InlineData(typeof(DerivedImmutableWithMorePropertiesThanInConstructorButWithBsonIgnoreAttribute_AbstractBaseImmutableWithConstructor), new[] { 1 }, "{ \"X\" : 1 }")]
        [InlineData(typeof(ClassWithTwoConstructorsWhereTheFirstIsNotFullyMatchedAndTheSecondIsFullyMatched), new[] { 1, 2 }, "{ \"X\" : 1, \"Y\" : 2 }")]
        [InlineData(typeof(ClassWithOneConstructorThatIsNotFullyMatched), new[] { 1, 2 }, "{ \"X\" : 1, \"Y\" : 2 }")]
        public void Serialization_should_return_expected_result(Type testCaseType, int[] arguments, string expectedJson)
        {
            var testCase = Activator.CreateInstance(testCaseType, arguments.Select(a => (object)a).ToArray());

            var json = testCase.ToJson();
            var actualBsonDocument = BsonDocument.Parse(json);
            actualBsonDocument["_t"].ToString().Should().Be(testCaseType.Name);
            actualBsonDocument.Remove("_t");
            actualBsonDocument.Should().Be(BsonDocument.Parse(expectedJson));

            var result = BsonSerializer.Deserialize(json, testCaseType);

            var x = GetPropertyValue(result, "X");
            var y = GetPropertyValue(result, "Y");
            x.Should().Be(1);
            y.Should().Be(2);
        }

        [Fact]
        public void Serialization_with_mismatching_type_between_constructor_argument_and_base_property()
        {
            var testCase = new DerivedWithMismatchedXTypeComparingWithBase(1, 2);

            var exception = Record.Exception(() => testCase.ToJson());
            var e = exception.Should().BeOfType<BsonSerializationException>().Subject;

            e.Message.Should().Be("Creator map for class MongoDB.Bson.Tests.Jira.CSharp1559Tests+DerivedWithMismatchedXTypeComparingWithBase has 2 arguments, but none are configured.");
        }

        [Fact]
        public void Serialization_with_more_than_one_type_in_inheritance_hierarchy()
        {
            var testCase = new DerivedWithoutSetter_IntermediateBaseAndWithProtectedConstructor(1, 2, 3);

            var json = testCase.ToJson();
            var result = BsonSerializer.Deserialize<DerivedWithoutSetter_IntermediateBaseAndWithProtectedConstructor>(json);

            result.X.Should().Be(1);
            result.Y.Should().Be(2);
            result.Z.Should().Be(3);
        }

        // private methods
        private int GetPropertyValue(object value, string propertyName)
        {
            return (int)Reflector.GetPropertyValue(value, propertyName, BindingFlags.Public | BindingFlags.Instance);
        }

        // nested types
        public class BaseWithoutSetter
        {
            public BaseWithoutSetter(int? x)
            {
                X = x;
            }

            public int? X { get; }
        }

        public class DerivedWithMismatchedXTypeComparingWithBase : BaseWithoutSetter
        {
            public DerivedWithMismatchedXTypeComparingWithBase(int x, int y) : base(x)
            {
                Y = y;
            }

            public int Y { get; }
        }

        public class DerivedWithoutSetter_BaseWithoutSetter : BaseWithoutSetter
        {
            private readonly int _y;
            public DerivedWithoutSetter_BaseWithoutSetter(int? x, int y) : base(x)
            {
                _y = y;
            }

            public int Y => _y;
        }

        public class DerivedWithoutSetterAndWithBsonElementAndWithPrivateConstructor_BaseWithoutSetter : BaseWithoutSetter
        {
            private DerivedWithoutSetterAndWithBsonElementAndWithPrivateConstructor_BaseWithoutSetter() : base(null)
            {
            }

            public DerivedWithoutSetterAndWithBsonElementAndWithPrivateConstructor_BaseWithoutSetter(int? x, int y) : base(x)
            {
                Y = y;
            }

            [BsonElement("y")]
            public int Y { get; }
        }

        public class DerivedWithPrivateSetterAndWithBsonElement_BaseWithoutSetter : BaseWithoutSetter
        {
            public DerivedWithPrivateSetterAndWithBsonElement_BaseWithoutSetter(int? x, int y) : base(x)
            {
                Y = y;
            }

            [BsonElement("y")]
            public int Y { get; private set; }
        }

        public abstract class AbstractBaseWithoutSetter
        {
            public AbstractBaseWithoutSetter(int? x)
            {
                X = x;
            }

            public int? X { get; }
        }

        public class DerivedWithoutSetterAndWithoutBsonElement_AbstractBaseWithoutSetter : AbstractBaseWithoutSetter
        {
            public DerivedWithoutSetterAndWithoutBsonElement_AbstractBaseWithoutSetter(int? x, int y) : base(x)
            {
                Y = y;
            }

            public int Y { get; }
        }

        public abstract class AbstractBaseWithoutSetterAndWithProtectedConstructor
        {
            protected AbstractBaseWithoutSetterAndWithProtectedConstructor(int? x)
            {
                X = x;
            }

            public int? X { get; }
        }

        public class DerivedWithoutSetterAndWithoutBsonElement_AbstractBaseWithoutSetterAndWithProtectedConstructor : AbstractBaseWithoutSetterAndWithProtectedConstructor
        {
            public DerivedWithoutSetterAndWithoutBsonElement_AbstractBaseWithoutSetterAndWithProtectedConstructor(int? x, int y) : base(x)
            {
                Y = y;
            }

            public int Y { get; }
        }

        public class BaseWithoutSetterAndWithProtectedConstructor
        {
            protected BaseWithoutSetterAndWithProtectedConstructor(int? x)
            {
                X = x;
            }

            public int? X { get; }
        }

        public class BaseWithoutSetterAndWithProtectedInternalConstructor
        {
            protected internal BaseWithoutSetterAndWithProtectedInternalConstructor(int? x)
            {
                X = x;
            }

            public int? X { get; }
        }

        public class DerivedWithoutSetterAndWithBsonElement_BaseWithoutSetterAndWithProtectedConstructor : BaseWithoutSetterAndWithProtectedConstructor
        {
            public DerivedWithoutSetterAndWithBsonElement_BaseWithoutSetterAndWithProtectedConstructor(int? x, int y) : base(x)
            {
                Y = y;
            }

            [BsonElement("y")]
            public int Y { get; }
        }

        public class DerivedWithoutSetter_BaseWithoutSetterAndWithProtectedInternalConstructor : BaseWithoutSetterAndWithProtectedInternalConstructor
        {
            public DerivedWithoutSetter_BaseWithoutSetterAndWithProtectedInternalConstructor(int? x, int y) : base(x)
            {
                Y = y;
            }

            public int Y { get; }
        }

        public class BaseWithPrivateSetterAndWithProtectedConstructor
        {
            protected BaseWithPrivateSetterAndWithProtectedConstructor(int? x)
            {
                X = x;
            }

            public int? X { get; private set; }
        }

        public class DerivedWithoutSetter_BaseWithPrivateSetterAndWithProtectedConstructor : BaseWithPrivateSetterAndWithProtectedConstructor
        {
            public DerivedWithoutSetter_BaseWithPrivateSetterAndWithProtectedConstructor(int? x, int y) : base(x)
            {
                Y = y;
            }

            public int Y { get; }
        }

        public abstract class IntermediateBaseWithoutSetterAndWithProtectedConstructor : BaseWithPrivateSetterAndWithProtectedConstructor
        {
            protected IntermediateBaseWithoutSetterAndWithProtectedConstructor(int? z, int? x) : base(x)
            {
                Z = z;
            }

            public int? Z { get; }
        }

        public class DerivedWithoutSetter_IntermediateBaseAndWithProtectedConstructor : IntermediateBaseWithoutSetterAndWithProtectedConstructor
        {
            public DerivedWithoutSetter_IntermediateBaseAndWithProtectedConstructor(int? x, int y, int? z) : base(z, x)
            {
                Y = y;
            }

            public int Y { get; }
        }

        public abstract class AbstractBaseImmutable
        {
            public int X { get; } = 1;
        }

        public class DerivedImmutableWithMorePropertiesThanInConstructorAndWithBsonConstructorAttribute_AbstractBaseImmutable : AbstractBaseImmutable
        {
            [BsonConstructor]
            public DerivedImmutableWithMorePropertiesThanInConstructorAndWithBsonConstructorAttribute_AbstractBaseImmutable(int y)
            {
                Y = y;
            }

            public int Y { get; }
        }

        public class DerivedImmutableWithMorePropertiesThanInConstructorButWithBsonElementAttributeAndWithBsonConstructorAttribute_AbstractBaseImmutable : AbstractBaseImmutable
        {
            [BsonConstructor]
            public DerivedImmutableWithMorePropertiesThanInConstructorButWithBsonElementAttributeAndWithBsonConstructorAttribute_AbstractBaseImmutable(int y)
            {
                Y = y;
            }

            [BsonElement]
            public int Y { get; }
        }

        public abstract class AbstractBaseImmutableWithConstructor
        {
            public AbstractBaseImmutableWithConstructor(int x)
            {
                X = x;
            }

            public int X { get; } = 0; // should be overwritten
        }

        public class DerivedImmutableWithMorePropertiesThanInConstructorAndWithBsonConstructorAttribute_AbstractBaseImmutableWithConstructor : AbstractBaseImmutableWithConstructor
        {
            [BsonConstructor]
            public DerivedImmutableWithMorePropertiesThanInConstructorAndWithBsonConstructorAttribute_AbstractBaseImmutableWithConstructor(int x, int y) : base(x)
            {
                Y = y;
            }

            public int Y { get; }
        }

        public class DerivedImmutableWithMorePropertiesThanInConstructorButWithBsonElementAttributeAndWithBsonConstructorAttribute_AbstractBaseImmutableWithConstructor : AbstractBaseImmutableWithConstructor
        {
            [BsonConstructor]
            public DerivedImmutableWithMorePropertiesThanInConstructorButWithBsonElementAttributeAndWithBsonConstructorAttribute_AbstractBaseImmutableWithConstructor(int x, int y) : base(x)
            {
                Y = y;
            }

            [BsonElement]
            public int Y { get; }
        }

        public class DerivedImmutableWithMorePropertiesThanInConstructorButWithBsonIgnoreAttribute_AbstractBaseImmutableWithConstructor : AbstractBaseImmutableWithConstructor
        {
            public DerivedImmutableWithMorePropertiesThanInConstructorButWithBsonIgnoreAttribute_AbstractBaseImmutableWithConstructor(int x) : base(x)
            {
            }

            [BsonIgnore]
            public int Y { get; } = 2;
        }


        public abstract class AbstractBaseImmutableWithAbstractProperty
        {
            [BsonIgnore]
            public abstract int X { get; }
        }

        public class DerivedImmutableWithMorePropertiesThanInConstructorButWithBsonIgnoreAttribute_AbstractBaseImmutableWithAbstractProperty : AbstractBaseImmutableWithAbstractProperty
        {
            public DerivedImmutableWithMorePropertiesThanInConstructorButWithBsonIgnoreAttribute_AbstractBaseImmutableWithAbstractProperty(int y)
            {
                Y = y;
            }

            [BsonIgnore]
            public override int X { get; } = 1;

            public int Y { get; } = 0; // should be overwritten
        }

        public class ClassWithTwoConstructorsWhereTheFirstIsNotFullyMatchedAndTheSecondIsFullyMatched
        {
            public int? X { get; }
            public int? Y { get; }

            public ClassWithTwoConstructorsWhereTheFirstIsNotFullyMatchedAndTheSecondIsFullyMatched(int? x)
                : this(x, null)
            {
            }

            public ClassWithTwoConstructorsWhereTheFirstIsNotFullyMatchedAndTheSecondIsFullyMatched(int? x, int? y)
            {
                X = x;
                Y = y;
            }
        }

        public class ClassWithOneConstructorThatIsNotFullyMatched
        {
            public int? X { get; }
            public int? Y { get; }
            public int? Z { get; }

            public ClassWithOneConstructorThatIsNotFullyMatched(int? x, int? y)
            {
                X = x;
                Y = y;
            }
        }
    }
}
