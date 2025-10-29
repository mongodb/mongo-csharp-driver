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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
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

            var toType = method.GetGenericArguments()[1];
            var valueExpression = arguments[0];
            var optionsExpression = arguments[1];

            var toSerializer = context.KnownSerializers.GetSerializer(expression);
            var toBsonType = GetResultRepresentation(expression, toSerializer);
            var valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, valueExpression);
            var (subType, byteOrder, format, onErrorAst, onNullAst) = TranslateOptions(context, expression, optionsExpression, toSerializer);

            var ast = AstExpression.Convert(valueTranslation.Ast, toBsonType.Render(), subType, byteOrder, format, onErrorAst, onNullAst);
            return new TranslatedExpression(expression, ast, toSerializer);
        }

        private static (BsonBinarySubType? subType, ByteOrder? byteOrder, string format, AstExpression onErrorAst, AstExpression onNullAst)
            TranslateOptions(
                TranslationContext context,
                Expression expression,
                Expression optionsExpression,
                IBsonSerializer toSerializer)
        {
            return optionsExpression switch
            {
                ConstantExpression constantExpression => TranslateOptions(constantExpression, toSerializer),
                MemberInitExpression memberInitExpressionExpression => TranslateOptions(context, expression, memberInitExpressionExpression, toSerializer),
                _ => throw new ExpressionNotSupportedException(optionsExpression, containingExpression: expression, because: "the options argument must be either a constant or a member initialization expression.")
            };
        }

        private static (BsonBinarySubType? subType, ByteOrder? byteOrder, string format, AstExpression onErrorAst, AstExpression onNullAst)
            TranslateOptions(
                ConstantExpression optionsExpression,
                IBsonSerializer toSerializer)
        {
            var options = (ConvertOptions)optionsExpression.Value;

            AstExpression onErrorAst = null;
            AstExpression onNullAst = null;
            if (options != null)
            {
                if (options.OnErrorWasSet(out var onErrorValue))
                {
                    var serializedOnErrorValue = SerializationHelper.SerializeValue(toSerializer, onErrorValue);
                    onErrorAst = AstExpression.Constant(serializedOnErrorValue);
                }

                if (options.OnNullWasSet(out var onNullValue))
                {
                    var serializedOnNullValue = SerializationHelper.SerializeValue(toSerializer, onNullValue);
                    onNullAst = AstExpression.Constant(serializedOnNullValue);
                }
            }

            return (options?.SubType, options?.ByteOrder, options?.Format, onErrorAst, onNullAst);
        }

        private static (BsonBinarySubType? subType, ByteOrder? byteOrder, string format, AstExpression onErrorAst, AstExpression onNullAst)
            TranslateOptions(
                TranslationContext context,
                Expression expression,
                MemberInitExpression optionsExpression,
                IBsonSerializer toSerializer
            )
        {
            BsonBinarySubType? subType = null;
            ByteOrder? byteOrder = null;
            string format = null;
            TranslatedExpression onErrorTranslation = null;
            TranslatedExpression onNullTranslation = null;

            foreach (var binding in optionsExpression.Bindings)
            {
                if (binding is not MemberAssignment memberAssignment)
                {
                    throw new ExpressionNotSupportedException(optionsExpression, containingExpression: expression, because: "only member assignment is supported");
                }

                var memberName = memberAssignment.Member.Name;
                var memberExpression = memberAssignment.Expression;

                switch (memberName)
                {
                    case nameof(ConvertOptions.ByteOrder):
                        byteOrder = memberExpression.GetConstantValue<ByteOrder?>(expression);
                        break;
                    case nameof(ConvertOptions.Format):
                        format = memberExpression.GetConstantValue<string>(expression);
                        break;
                    case nameof(ConvertOptions<object>.OnError):
                        onErrorTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, memberExpression);
                        SerializationHelper.EnsureSerializerIsCompatible(memberExpression, containingExpression: expression, onErrorTranslation.Serializer, expectedSerializer: toSerializer);
                        break;
                    case nameof(ConvertOptions<object>.OnNull):
                        onNullTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, memberExpression);
                        SerializationHelper.EnsureSerializerIsCompatible(memberExpression, containingExpression: expression, onNullTranslation.Serializer, expectedSerializer: toSerializer);
                        break;
                    case nameof(ConvertOptions.SubType):
                        subType = memberExpression.GetConstantValue<BsonBinarySubType?>(expression);
                        break;
                    default:
                        throw new ExpressionNotSupportedException(memberExpression, because: $"memberName {memberName} is invalid");
                }
            }

            return (subType, byteOrder, format, onErrorTranslation?.Ast, onNullTranslation?.Ast);
        }

        private static BsonType GetResultRepresentation(Expression expression, IBsonSerializer resultSerializer)
        {
            if (resultSerializer is not IHasRepresentationSerializer hasRepresentationSerializer)
            {
                throw new ExpressionNotSupportedException(expression, because: $"{resultSerializer.GetType().Name} does not implement {nameof(IHasRepresentationSerializer)}");
            }

            return hasRepresentationSerializer.Representation;
        }
    }
}
