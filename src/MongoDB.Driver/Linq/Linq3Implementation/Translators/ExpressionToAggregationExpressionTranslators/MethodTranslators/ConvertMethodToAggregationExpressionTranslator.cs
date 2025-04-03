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

            var fieldAst = ExpressionToAggregationExpressionTranslator.Translate(context, arguments[0]).Ast;

            ByteOrder? byteOrder;
            string format;
            AstExpression onErrorAst;
            AstExpression onNullAst;
            BsonBinarySubType? subType;

            var optionExpression = arguments[1];
            switch (optionExpression)
            {
                case ConstantExpression constantExpression:
                    ExtractOptionsFromConstantExpression(constantExpression, out byteOrder, out format, out onErrorAst, out onNullAst, out subType);
                    break;
                case MemberInitExpression memberInitExpression:
                    ExtractOptionsFromMemberInitExpression(memberInitExpression, context, out byteOrder, out format, out onErrorAst, out onNullAst, out subType);
                    break;
                default:
                    throw new ExpressionNotSupportedException("The 'Options' argument can be either a constant expression or a member initialization expression");
            }

            var toType = method.GetGenericArguments()[1];
            var toBsonType = GetBsonType(toType).Render();
            var serializer = BsonSerializer.LookupSerializer(toType);

            var ast = AstExpression.Convert(fieldAst, toBsonType, subType: subType, byteOrder: byteOrder, format: format, onError: onErrorAst, onNull: onNullAst);
            return new TranslatedExpression(expression, ast, serializer);
        }

        private static void ExtractOptionsFromConstantExpression(ConstantExpression constantExpression, out ByteOrder? byteOrder, out string format, out AstExpression onErrorAst, out AstExpression onNullAst, out BsonBinarySubType? subType)
        {
            byteOrder = null;
            format = null;
            onErrorAst = null;
            onNullAst = null;
            subType = null;

            var options = (ConvertOptions)constantExpression.Value;

            if (options is null)
            {
                return;
            }

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

        private static void ExtractOptionsFromMemberInitExpression(MemberInitExpression memberInitExpression, TranslationContext context, out ByteOrder? byteOrder, out string format, out AstExpression onErrorAst, out AstExpression onNullAst, out BsonBinarySubType? subType)
        {
            byteOrder = null;
            format = null;
            onErrorAst = null;
            onNullAst = null;
            subType = null;

            foreach (var binding in memberInitExpression.Bindings)
            {
                if (binding is not MemberAssignment memberAssignment) continue;

                var memberName = memberAssignment.Member.Name;
                var expression = memberAssignment.Expression;

                switch (memberName)
                {
                    case nameof(ConvertOptions.ByteOrder):
                        byteOrder = GetConstantValue<ByteOrder?>(expression, nameof(ConvertOptions.ByteOrder));
                        break;
                    case nameof(ConvertOptions.Format):
                        format = GetConstantValue<string>(expression, nameof(ConvertOptions.Format));
                        break;
                    case nameof(ConvertOptions<object>.OnError):
                        onErrorAst = ExpressionToAggregationExpressionTranslator.Translate(context, expression).Ast;
                        break;
                    case nameof(ConvertOptions<object>.OnNull):
                        onNullAst = ExpressionToAggregationExpressionTranslator.Translate(context, expression).Ast;
                        break;
                    case nameof(ConvertOptions.SubType):
                        subType = GetConstantValue<BsonBinarySubType?>(expression, nameof(ConvertOptions.SubType));
                        break;
                }
            }
        }

        private static T GetConstantValue<T>(Expression expression, string fieldName)
        {
            if (expression is not ConstantExpression constantExpression)
            {
                throw new ExpressionNotSupportedException($"The {fieldName} field must be a constant expression.");
            }
            return (T)constantExpression.Value;
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
