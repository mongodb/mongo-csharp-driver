using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MongoDB.Bson.ComponentModel
{
    /// <summary>
    /// TypeConverter to convert an ObjectId into a string or DateTime, and from a string into an ObjectId.
    /// </summary>
    public class ObjectIdConverter : TypeConverter
    {
        /// <summary>
        /// Gets a value indicating whether this converter can convert an object in the
        /// given source type to the native type of the converter.
        /// Supports string only.
        /// </summary>
        /// <param name="context">Ignored.</param>
        /// <param name="sourceType">The source type to evaluate.</param>
        /// <returns></returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;
           
            return base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Converts a string into an ObjectId.
        /// </summary>
        /// <param name="context">Ignored.</param>
        /// <param name="culture">Ignored.</param>
        /// <param name="value">The value type to convert. Must be string otherwise passed to base implementation.</param>
        /// <returns>An ObjectId if the value type was string.</returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
                return ObjectId.Parse((string)value);

            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// Gets a value indicating whether this converter can convert an ObjectId to another type.
        /// Only string or DateTime are supported.
        /// </summary>
        /// <param name="context">Ignored.</param>
        /// <param name="destinationType">Only string or DateTime supported.</param>
        /// <returns>If conversion is possible, a string or DateTime.</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string) || destinationType == typeof(DateTime))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// Converts an ObjectId to a string or DateTime.
        /// </summary>
        /// <param name="context">Ignored.</param>
        /// <param name="culture">Ignored.</param>
        /// <param name="value">The value to be evaluated.</param>
        /// <param name="destinationType">Target type, only string or DateTime supported.</param>
        /// <returns>A string or DateTime if conversion was possible.</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return ((ObjectId) value).ToString();
            }
            else if (destinationType == typeof(DateTime))
            {
                return ((ObjectId) value).CreationTime;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
