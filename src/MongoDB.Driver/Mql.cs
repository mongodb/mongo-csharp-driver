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

        /// <summary>
        ///
        /// </summary>
        /// <param name="field"></param>
        /// <param name="subtype"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static BsonBinaryData ConvertToBinData(string field, BsonBinarySubType subtype, ConvertBinDataFormat format)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="field"></param>
        /// <param name="subtype"></param>
        /// <param name="format"></param>
        /// <param name="onNull"></param>
        /// <param name="onError"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static BsonBinaryData ConvertToBinData(string field, BsonBinarySubType subtype, ConvertBinDataFormat format,
            BsonBinaryData onError, BsonBinaryData onNull)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="field"></param>
        /// <param name="subtype"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static BsonBinaryData ConvertToBinData(int field, BsonBinarySubType subtype, ConvertBinDataFormat format)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="field"></param>
        /// <param name="subtype"></param>
        /// <param name="format"></param>
        /// <param name="onNull"></param>
        /// <param name="onError"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static BsonBinaryData ConvertToBinData(int field, BsonBinarySubType subtype, ConvertBinDataFormat format,
            BsonBinaryData onError, BsonBinaryData onNull)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="field"></param>
        /// <param name="subtype"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static BsonBinaryData ConvertToBinData(long field, BsonBinarySubType subtype, ConvertBinDataFormat format)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="field"></param>
        /// <param name="subtype"></param>
        /// <param name="format"></param>
        /// <param name="onNull"></param>
        /// <param name="onError"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static BsonBinaryData ConvertToBinData(long field, BsonBinarySubType subtype, ConvertBinDataFormat format,
            BsonBinaryData onError, BsonBinaryData onNull)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="field"></param>
        /// <param name="subtype"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static BsonBinaryData ConvertToBinData(double field, BsonBinarySubType subtype, ConvertBinDataFormat format)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="field"></param>
        /// <param name="subtype"></param>
        /// <param name="format"></param>
        /// <param name="onNull"></param>
        /// <param name="onError"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static BsonBinaryData ConvertToBinData(double field, BsonBinarySubType subtype, ConvertBinDataFormat format,
            BsonBinaryData onError, BsonBinaryData onNull)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="field"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string ConvertToString(BsonBinaryData field, ConvertBinDataFormat format)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="field"></param>
        /// <param name="format"></param>
        /// <param name="onError"></param>
        /// <param name="onNull"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string ConvertToString(BsonBinaryData field, ConvertBinDataFormat format, string onError, string onNull)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="field"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static int? ConvertToInt(BsonBinaryData field, ConvertBinDataFormat format)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="field"></param>
        /// <param name="format"></param>
        /// <param name="onError"></param>
        /// <param name="onNull"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static int? ToInt(BsonBinaryData field, ConvertBinDataFormat format, int?  onError, int? onNull)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="field"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static long? ConvertToLong(BsonBinaryData field, ConvertBinDataFormat format)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="field"></param>
        /// <param name="format"></param>
        /// <param name="onError"></param>
        /// <param name="onNull"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static long? ConvertToLong(BsonBinaryData field, ConvertBinDataFormat format, long?  onError, long? onNull)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="field"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static double? ToDouble(BsonBinaryData field, ConvertBinDataFormat format)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="field"></param>
        /// <param name="format"></param>
        /// <param name="onError"></param>
        /// <param name="onNull"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static double? ToDouble(BsonBinaryData field, ConvertBinDataFormat format, double? onError, double? onNull)
        {
            throw CustomLinqExtensionMethodHelper.CreateNotSupportedException();
        }

        /// <summary>
        ///
        /// </summary>
        public enum ConvertBinDataFormat
        //TODO Decide: if to use, location, naming
        {
            /// <summary>
            ///
            /// </summary>
            base64,
            /// <summary>
            ///
            /// </summary>
            base64url,
            /// <summary>
            ///
            /// </summary>
            utf8,
            /// <summary>
            ///
            /// </summary>
            hex,
            /// <summary>
            ///
            /// </summary>
            uuid,
        }
    }
}
