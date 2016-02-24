/* Copyright 2015-2016 MongoDB Inc.
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
using System.Collections;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Support;

namespace MongoDB.Driver.Linq.Expressions
{
    internal interface ISerializationExpression
    {
        IBsonSerializer Serializer { get; }

        Type Type { get; }
    }

    internal static class ISerializationExpressionExtensions
    {
        public static string AppendFieldName(this ISerializationExpression node, string suffix)
        {
            var field = node as IFieldExpression;
            return CombineFieldNames(field == null ? null : field.FieldName, suffix);
        }

        public static string PrependFieldName(this ISerializationExpression node, string prefix)
        {
            var field = node as IFieldExpression;
            return CombineFieldNames(prefix, field == null ? null : field.FieldName);
        }

        public static BsonValue SerializeValue(this ISerializationExpression field, object value)
        {
            Ensure.IsNotNull(field, nameof(field));

            value = ConvertIfNecessary(field.Serializer.ValueType, value);

            var tempDocument = new BsonDocument();
            using (var bsonWriter = new BsonDocumentWriter(tempDocument))
            {
                var context = BsonSerializationContext.CreateRoot(bsonWriter);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName("value");
                field.Serializer.Serialize(context, value);
                bsonWriter.WriteEndDocument();
                return tempDocument[0];
            }
        }

        public static BsonArray SerializeValues(this ISerializationExpression field, IEnumerable values)
        {
            var tempDocument = new BsonDocument();
            using (var bsonWriter = new BsonDocumentWriter(tempDocument))
            {
                var context = BsonSerializationContext.CreateRoot(bsonWriter);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName("values");
                bsonWriter.WriteStartArray();
                foreach (var value in values)
                {
                    field.Serializer.Serialize(context, ConvertIfNecessary(field.Serializer.ValueType, value));
                }
                bsonWriter.WriteEndArray();
                bsonWriter.WriteEndDocument();

                return (BsonArray)tempDocument[0];
            }
        }

        private static string CombineFieldNames(string prefix, string suffix)
        {
            if (prefix == null)
            {
                return suffix;
            }
            if (suffix == null)
            {
                return prefix;
            }

            return prefix + "." + suffix;
        }

        private static object ConvertIfNecessary(Type targetType, object value)
        {
            if (targetType.GetTypeInfo().IsEnum || targetType.IsNullableEnum())
            {
                if (value != null)
                {
                    if (targetType.IsNullableEnum())
                    {
                        targetType = targetType.GetNullableUnderlyingType();
                    }

                    value = Enum.ToObject(targetType, value);
                }
            }
            else if (targetType != typeof(BsonValue) && !targetType.IsNullable())
            {
                if (value != null && targetType != value.GetType())
                {
                    value = Convert.ChangeType(value, targetType);
                }
            }

            return value;
        }
    }
}