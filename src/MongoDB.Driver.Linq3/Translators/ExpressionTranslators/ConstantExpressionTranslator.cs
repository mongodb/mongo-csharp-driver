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
using System.Collections;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators
{
    public static class ConstantExpressionTranslator
    {
        public static ExpressionTranslation Translate(TranslationContext context, ConstantExpression expression)
        {
            var value = expression.Value;
            
            if (value == null)
            {
                return new ExpressionTranslation(expression, BsonNull.Value, new BsonNullSerializer());
            }

            BsonValue translatedValue = null;
            switch (Type.GetTypeCode(expression.Type))
            {
                case TypeCode.Boolean: translatedValue = (BsonBoolean)(bool)value; break;
                case TypeCode.DateTime: translatedValue = (BsonDateTime)(DateTime)value; break;
                case TypeCode.Decimal: translatedValue = (BsonDecimal128)(Decimal)value; break;
                case TypeCode.Double: translatedValue = (BsonDouble)(double)value; break;
                case TypeCode.Int16: translatedValue = (BsonInt32)(int)(short)value; break;
                case TypeCode.Int32: translatedValue = (BsonInt32)(int)value; break;
                case TypeCode.Int64: translatedValue = (BsonInt64)(long)value; break;
                case TypeCode.String: translatedValue = (BsonString)(string)value; break;
            }
            if (translatedValue != null)
            {
                var serializer = BsonSerializer.LookupSerializer(translatedValue.GetType());
                return new ExpressionTranslation(expression, translatedValue, serializer);
            }

            var valueType = value.GetType();
            if (typeof(IEnumerable).IsAssignableFrom(valueType))
            {
                var ienumerableInterface = valueType.GetIEnumerableGenericInterface();
                var itemType = ienumerableInterface.GetGenericArguments()[0];
                var itemSerializer = BsonSerializer.LookupSerializer(itemType);
                translatedValue = SerializationHelper.SerializeValues(itemSerializer, (IEnumerable)value);
                var serializer = BsonSerializer.LookupSerializer(translatedValue.GetType());

                return new ExpressionTranslation(expression, translatedValue, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
