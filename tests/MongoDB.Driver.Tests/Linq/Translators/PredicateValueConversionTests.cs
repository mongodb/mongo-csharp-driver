/* Copyright 2017-present MongoDB Inc.
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
using System.ComponentModel;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Translators;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Translators
{
    public class PredicateValueConversionTests : IntegrationTestBase
    {
        [Fact]
        public void Value_conversion_with_primitive_types()
        {
            Assert(
                x => x.B == (object)10,
                0,
                "{ B : '10' }");
        }

        [Fact]
        public void Value_conversion_with_custom_type_converter()
        {
            TypeDescriptor.AddAttributes(typeof(C), new TypeConverterAttribute(typeof(CExampleTypeConverter)));

            var objectToConvert = (object)"Dexter";
            Assert(
                x => x.C == objectToConvert,
                0,
                "{ C : { Ids : null, D : 'Dexter', E : null, S : null, X : null } }");
        }

        public void Assert(Expression<Func<Root, bool>> filter, int expectedCount, string expectedFilter)
        {
            var serializer = BsonSerializer.SerializerRegistry.GetSerializer<Root>();
            var filterDocument = PredicateTranslator.Translate(filter, serializer, BsonSerializer.SerializerRegistry);

            var list = __collection.FindSync(filterDocument).ToList();

            filterDocument.Should().Be(BsonDocument.Parse(expectedFilter));
            list.Count.Should().Be(expectedCount);
        }

        /// <summary>
        /// Custom type converter for <see cref="C"/> test class.
        /// </summary>
        private class CExampleTypeConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(string)) return true;
                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string) return CreateFromString(value);
                return base.ConvertFrom(context, culture, value);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                return (typeof(C).GetTypeInfo().IsAssignableFrom(destinationType));
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                if (typeof(C).GetTypeInfo().IsAssignableFrom(destinationType))
                {
                    if (value is string)
                    {
                        return CreateFromString(value);
                    }
                }
                return base.ConvertTo(context, culture, value, destinationType);
            }

            /// <summary>
            /// Creates new <see cref="C"/> instance from string.
            /// </summary>
            /// <param name="value">String value used for conversion.</param>
            private C CreateFromString(object value)
            {
                return new C { D = (string)value };
            }
        }
    }
}
