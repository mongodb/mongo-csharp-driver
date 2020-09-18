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
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators
{
    public static class ConstantExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, ConstantExpression expression)
        {
            var value = expression.Value;
            
            if (value == null)
            {
                return new TranslatedExpression(expression, new AstConstantExpression(BsonNull.Value), null);
            }

            BsonValue translatedConstant = null;
            switch (Type.GetTypeCode(expression.Type))
            {
                case TypeCode.Boolean: translatedConstant = (BsonBoolean)(bool)value; break;
                case TypeCode.DateTime: translatedConstant = (BsonDateTime)(DateTime)value; break;
                case TypeCode.Decimal: translatedConstant = (BsonDecimal128)(Decimal)value; break;
                case TypeCode.Double: translatedConstant = (BsonDouble)(double)value; break;
                case TypeCode.Int16: translatedConstant = (BsonInt32)(int)(short)value; break;
                case TypeCode.Int32: translatedConstant = (BsonInt32)(int)value; break;
                case TypeCode.Int64: translatedConstant = (BsonInt64)(long)value; break;
                case TypeCode.String: translatedConstant = (BsonString)(string)value; break;
            }

            if (translatedConstant != null)
            {
                return new TranslatedExpression(expression, new AstConstantExpression(translatedConstant), null);
            }

            var valueType = value.GetType();
            if (typeof(IEnumerable).IsAssignableFrom(valueType))
            {
                var ienumerableInterface = valueType.GetIEnumerableGenericInterface();
                var itemType = ienumerableInterface.GetGenericArguments()[0];
                var itemSerializer = BsonSerializer.LookupSerializer(itemType);

                translatedConstant = SerializationHelper.SerializeValues(itemSerializer, (IEnumerable)value);
                return new TranslatedExpression(expression, new AstConstantExpression(translatedConstant), null);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
