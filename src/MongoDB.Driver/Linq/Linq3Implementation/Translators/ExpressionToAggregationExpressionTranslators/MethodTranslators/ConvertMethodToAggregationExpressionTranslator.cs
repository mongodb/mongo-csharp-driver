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
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal class ConvertMethodToAggregationExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (!method.Is(MqlMethod.Convert))
            {
                throw new ExpressionNotSupportedException(expression);
            }

            ByteOrder? byteOrder = null;
            string format = null;
            AstExpression onErrorAst = null;
            AstExpression onNullAst = null;
            BsonBinarySubType? subType = null;

            var fieldAst = ExpressionToAggregationExpressionTranslator.Translate(context, arguments[0]).Ast;

            var optionExpression = arguments[1];

            if (optionExpression is ConstantExpression constantExpression)
            {
                var options = (ConvertOptions)constantExpression.Value;

                if (options.OnErrorWasSet)
                {
                    onErrorAst = options.GetOnError();
                }

                if (options.OnNullWasSet)
                {
                    onNullAst = options.GetOnNull();
                }

                subType = options.SubType;
                format = options.Format;
                byteOrder = options.ByteOrder;
            }
            else if (optionExpression is MemberInitExpression memberInitExpression)
            {
                foreach (var binding in memberInitExpression.Bindings)
                {
                    if (binding is not MemberAssignment memberAssignment) continue;

                    var memberName = memberAssignment.Member.Name;

                    switch (memberName)
                    {
                        case nameof(ConvertOptions.ByteOrder):
                        {
                            if (memberAssignment.Expression is not ConstantExpression byteOrderExpression)
                            {
                                throw new ExpressionNotSupportedException($"The {nameof(ConvertOptions.ByteOrder)} field must be a constant expression");  //TODO Improve message?
                            }

                            byteOrder = (ByteOrder?)byteOrderExpression.Value;
                            break;
                        }
                        case nameof(ConvertOptions.Format):
                        {
                            if (memberAssignment.Expression is not ConstantExpression formatExpression)
                            {
                                throw new ExpressionNotSupportedException($"The {nameof(ConvertOptions.Format)} field must be a constant expression");  //TODO Improve message?
                            }

                            format = (string)formatExpression.Value;
                            break;
                        }
                        case nameof(ConvertOptions<object>.OnError):
                        {
                            onErrorAst = ExpressionToAggregationExpressionTranslator.Translate(context, memberAssignment.Expression).Ast;
                            break;
                        }
                        case nameof(ConvertOptions<object>.OnNull):
                        {
                            onNullAst = ExpressionToAggregationExpressionTranslator.Translate(context, memberAssignment.Expression).Ast;
                            break;
                        }
                        case nameof(ConvertOptions.SubType):
                        {
                            if (memberAssignment.Expression is not ConstantExpression subTypeExpression)
                            {
                                throw new ExpressionNotSupportedException($"The {nameof(ConvertOptions.SubType)} field must be a constant expression");  //TODO Improve message?
                            }

                            subType = (BsonBinarySubType?)subTypeExpression.Value;
                            break;
                        }
                    }
                }
            }
            else
            {
                throw new ExpressionNotSupportedException("The 'Options' argument can be either a constant expression or a member initialization expression");  //TODO Improve message?
            }

            var toType = method.GetGenericArguments()[1];
            var toBsonType = GetBsonType(toType).Render();
            var serializer = BsonSerializer.LookupSerializer(toType);

            var ast = AstExpression.Convert(fieldAst, toBsonType, subType: subType, byteOrder: byteOrder, format: format, onError: onErrorAst, onNull: onNullAst);
            return new TranslatedExpression(expression, ast, serializer);
        }

        private static BsonType GetBsonType(Type type)  //TODO Do we have this kind of info somewhere else...?
        {
            return Type.GetTypeCode(Nullable.GetUnderlyingType(type) ?? type) switch
            {
                TypeCode.Boolean => BsonType.Boolean,
                TypeCode.Byte => BsonType.Int32,
                TypeCode.SByte => BsonType.Int32,
                TypeCode.Int16 => BsonType.Int32,
                TypeCode.UInt16 => BsonType.Int32,
                TypeCode.Int32 => BsonType.Int32,
                TypeCode.UInt32 => BsonType.Int64,
                TypeCode.Int64 => BsonType.Int64,
                TypeCode.UInt64 => BsonType.Decimal128,
                TypeCode.Single => BsonType.Double,
                TypeCode.Double => BsonType.Double,
                TypeCode.Decimal => BsonType.Decimal128,
                TypeCode.String => BsonType.String,
                TypeCode.Char => BsonType.String,
                TypeCode.DateTime => BsonType.DateTime,
                TypeCode.Object when type == typeof(byte[]) => BsonType.Binary,
                TypeCode.Object when type == typeof(BsonBinaryData) => BsonType.Binary,
                _ when type == typeof(Guid) => BsonType.Binary,
                _ when type == typeof(ObjectId) => BsonType.ObjectId,
                _ => BsonType.Document
            };
        }
    }
}
