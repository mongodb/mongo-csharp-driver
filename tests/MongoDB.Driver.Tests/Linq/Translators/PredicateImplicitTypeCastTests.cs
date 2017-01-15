using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Translators;
using System.ComponentModel;
using System.Linq.Expressions;
using MongoDB.Bson;
using System.Globalization;

namespace MongoDB.Driver.Tests.Linq.Translators
{
    public class PredicateImplicitTypeCastTests : IntegrationTestBase
    {
        [Fact]
        public void Implicit_Type_Casting_Primitive_Types()
        {
            TypeDescriptor.AddAttributes(typeof(C), new TypeConverterAttribute(typeof(CExampleTypeCoverter)));
            Assert(
                x => x.B == (object)10,
                0,
                "{B: '10'}");
        }

        [Fact]
        public void Implicit_Type_Casting_With_Custom_TypeConverter()
        {
            //register custom type converter for C type
            TypeDescriptor.AddAttributes(typeof(C), new TypeConverterAttribute(typeof(CExampleTypeCoverter)));
            var objectToCast = (object)"Dexter";
            Assert(
                x => x.C == objectToCast,
                0,
                "{C: { '_t' : 'C', D: 'Dexter', E: null, S: null, X: null}}");
        }

        public void Assert(Expression<Func<Root, bool>> filter, int expectedCount, string expectedFilter)
        {
            var serializer = BsonSerializer.SerializerRegistry.GetSerializer<Root>();
            var filterDocument = PredicateTranslator.Translate(filter, serializer, BsonSerializer.SerializerRegistry);

            var list = __collection.FindSync(filterDocument).ToList();

            filterDocument.Should().Be(BsonDocument.Parse(expectedFilter));
            list.Count.Should().Be(expectedCount);
        }

        public class CExampleTypeCoverter : TypeConverter
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
                return (typeof(C).IsAssignableFrom(destinationType));
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                if (typeof(C).IsAssignableFrom(destinationType))
                {
                    if (value is string)
                    {
                        return CreateFromString(value);
                    }
                }
                return base.ConvertTo(context, culture, value, destinationType);
            }

            private C CreateFromString(object value)
            {
                return new C { D = (string)value };
            }
        }
    }
}
