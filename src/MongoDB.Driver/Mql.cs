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
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver
{
    /// <summary>
    /// Contains methods that can be used to access MongoDB specific functionality in LINQ queries.
    /// </summary>
    public static class Mql
    {
        /// <summary>
        /// Use this method in a MongoDB LINQ query when you need to specify how a constant should be serialized.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The serializer.</param>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <returns>The value.</returns>
        public static TValue Constant<TValue>(TValue value, IBsonSerializer<TValue> serializer)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Use this method in a MongoDB LINQ query when you need to specify how a constant should be serialized.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="representaion">The representation.</param>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <returns>The value</returns>
        public static TValue Constant<TValue>(TValue value, BsonType representaion)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Converts a string to a BsonBinaryData using the $convert aggregation operator.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="subtype">The BsonBinaryData subtype of the result value.</param>
        /// <param name="format">The format string.</param>
        /// <returns>The converted value.</returns>
        public static BsonValue ToBsonBinaryData(string value, BsonBinarySubType subtype, string format)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Converts a string to BsonBinaryData using the $convert aggregation operator.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="subtype">The BsonBinaryData subtype of the result value.</param>
        /// <param name="format">The format string.</param>
        /// <param name="options">The convert options.</param>
        /// <returns>The converted value.</returns>
        public static BsonValue ToBsonBinaryData(string value, BsonBinarySubType subtype, string format, ConvertOptions<BsonValue> options)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Converts an int to BsonBinaryData using the $convert aggregation operator.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="subtype">The BsonBinaryData subtype of the result value.</param>
        /// <param name="byteOrder">The byte order of BsonBinaryData.</param>
        /// <returns>The converted value.</returns>
        public static BsonValue ToBsonBinaryData(int? value, BsonBinarySubType subtype, ByteOrder byteOrder)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Converts an int? to a BsonBinaryData using the $convert aggregation operator.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="subtype">The BsonBinaryData subtype of the result value.</param>
        /// <param name="byteOrder">The byte ordering of BsonBinaryData.</param>
        /// <param name="options">The convert options.</param>
        /// <returns>The converted value.</returns>
        /// <exception cref="Exception"></exception>
        public static BsonValue ToBsonBinaryData(int? value, BsonBinarySubType subtype, ByteOrder byteOrder, ConvertOptions<BsonValue> options)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Converts a long? to a BsonBinaryData using the $convert aggregation operator.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="subtype">The BsonBinaryData subtype of the result value.</param>
        /// <param name="byteOrder">The byte ordering of BsonBinaryData.</param>
        /// <returns>The converted value.</returns>
        public static BsonValue ToBsonBinaryData(long? value, BsonBinarySubType subtype, ByteOrder byteOrder)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Converts a long? to a BsonBinaryData using the $convert aggregation operator.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="subtype">The BsonBinaryData subtype of the result value.</param>
        /// <param name="byteOrder">The byte ordering of BsonBinaryData.</param>
        /// <param name="options">The convert options.</param>
        /// <returns>The converted value.</returns>
        /// <exception cref="Exception"></exception>
        public static BsonValue ToBsonBinaryData(long? value, BsonBinarySubType subtype, ByteOrder byteOrder, ConvertOptions<BsonValue> options)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Converts a double? to a BsonBinaryData using the $convert aggregation operator.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="subtype">The BsonBinaryData subtype of the result value.</param>
        /// <param name="byteOrder">The byte ordering of BsonBinaryData.</param>
        /// <returns>The converted value.</returns>
        public static BsonValue ToBsonBinaryData(double? value, BsonBinarySubType subtype, ByteOrder byteOrder)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Converts a double? to a BsonBinaryData using the $convert aggregation operator.
        /// </summary>
        /// <param name="value">The field.</param>
        /// <param name="subtype">The BsonBinaryData subtype of the result value.</param>
        /// <param name="byteOrder">The byte ordering of BsonBinaryData.</param>
        /// <param name="options">The convert options.</param>
        /// <returns>The converted value.</returns>
        /// <exception cref="Exception"></exception>
        public static BsonValue ToBsonBinaryData(double? value, BsonBinarySubType subtype, ByteOrder byteOrder, ConvertOptions<BsonValue> options)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Converts a BsonBinaryData to a string using the $convert aggregation operator.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The format string.</param>
        /// <returns>The converted value.</returns>
        public static string ToString(BsonBinaryData value, string format)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Converts a BsonBinaryData to a string using the $convert aggregation operator.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The format string.</param>
        /// <param name="options">The convert options.</param>
        /// <returns>The converted value.</returns>
        public static string ToString(BsonBinaryData value, string format, ConvertOptions<string> options)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Converts a BsonBinaryData to int? using the $convert aggregation operator.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="byteOrder">The byte ordering of BsonBinaryData.</param>
        /// <returns>The converted value.</returns>
        public static int? ToNullableInt(BsonBinaryData value, ByteOrder byteOrder)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Converts a BsonBinaryData to int? using the $convert aggregation operator.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="byteOrder">The byte ordering of BsonBinaryData.</param>
        /// <param name="options">The convert options.</param>
        /// <returns>The converted value.</returns>
        public static int? ToNullableInt(BsonBinaryData value, ByteOrder byteOrder, ConvertOptions<int?> options)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Converts a BsonBinaryData to long? using the $convert aggregation operator.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="byteOrder">The byte ordering of BsonBinaryData.</param>
        /// <returns>The converted value.</returns>
        public static long? ToNullableLong(BsonBinaryData value, ByteOrder byteOrder)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Converts a BsonBinaryData to long? using the $convert aggregation operator.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="byteOrder">The byte ordering of BsonBinaryData.</param>
        /// <param name="options">The convert options.</param>
        /// <returns>The converted value.</returns>
        public static long? ToNullableLong(BsonBinaryData value, ByteOrder byteOrder, ConvertOptions<long?> options)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Converts a BsonBinaryData to double? using the $convert aggregation operator.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="byteOrder">The byte ordering of BsonBinaryData.</param>
        /// <returns>The converted value.</returns>
        public static double? ToNullableDouble(BsonBinaryData value, ByteOrder byteOrder)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Converts a BsonBinaryData to string using the $convert aggregation operator.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="byteOrder">The byte ordering of BsonBinaryData.</param>
        /// <param name="options">The convert options.</param>
        /// <returns>The converted value.</returns>
        public static double? ToNullableDouble(BsonBinaryData value, ByteOrder byteOrder, ConvertOptions<double?> options)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Converts a string to a DateTime using the $dateFromString aggregation operator.
        /// </summary>
        /// <param name="dateString">The string.</param>
        /// <returns>A DateTime.</returns>
        public static DateTime DateFromString(string dateString)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Converts a string to a DateTime using the $dateFromString aggregation operator.
        /// </summary>
        /// <param name="dateString">The string.</param>
        /// <param name="format">The format string.</param>
        /// <returns>A DateTime.</returns>
        public static DateTime DateFromString(
            string dateString,
            string format)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Converts a string to a DateTime using the $dateFromString aggregation operator.
        /// </summary>
        /// <param name="dateString">The string.</param>
        /// <param name="format">The format string.</param>
        /// <param name="timezone">The time zone.</param>
        /// <returns>A DateTime.</returns>
        public static DateTime DateFromString(
            string dateString,
            string format,
            string timezone)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Converts a string to a DateTime using the $dateFromString aggregation operator.
        /// </summary>
        /// <param name="dateString">The string.</param>
        /// <param name="format">The format string.</param>
        /// <param name="timezone">The time zone.</param>
        /// <param name="onError">The onError value.</param>
        /// <param name="onNull">The onNull value.</param>
        /// <returns>A DateTime.</returns>
        public static DateTime? DateFromString(
            string dateString,
            string format,
            string timezone,
            DateTime? onError,
            DateTime? onNull)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Tests whether a field exists.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="field">The field.</param>
        /// <returns><c>true</c> if the field exists.</returns>
        public static bool Exists<TField>(TField field)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Gets the value of a field in a document.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="document">The document.</param>
        /// <param name="fieldName">The field name.</param>
        /// <param name="fieldSerializer">The field serializer.</param>
        /// <returns>The value of the field.</returns>
        public static TField Field<TDocument, TField>(TDocument document, string fieldName, IBsonSerializer<TField> fieldSerializer)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Tests whether a field is missing.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="field">The field.</param>
        /// <returns><c>true</c> if the field is missing.</returns>
        public static bool IsMissing<TField>(TField field)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        /// Tests whether a field is null or missing.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="field">The field.</param>
        /// <returns><c>true</c> if the field is null or missing.</returns>
        public static bool IsNullOrMissing<TField>(TField field)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }
    }

    /// <summary>
    /// Represents the byte order of binData when converting to/from numerical types.
    /// </summary>
    public enum ByteOrder
    {
        /// <summary>
        /// Big endian order.
        /// </summary>
        BigEndian,
        /// <summary>
        /// Little endian order.
        /// </summary>
        LittleEndian,
    }

    //TODO Add docs
    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public class ConvertOptions<TResult>
    {
        private TResult _onError;
        private bool _onErrorWasSet;
        private TResult _onNull;
        private bool _onNullWasSet;

        /// <summary>
        ///
        /// </summary>
        public TResult OnError
        {
            get => _onError;
            set
            {
                _onError = value;
                _onErrorWasSet = true;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public TResult OnNull
        {
            get => _onNull;
            set
            {
                _onNull = value;
                _onNullWasSet = true;
            }
        }

        internal bool OnErrorWasSet => _onErrorWasSet;
        internal bool OnNullWasSet => _onNullWasSet;
    }
}
