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

using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal class DeserializeEJsonMethodToAggregationExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (!method.Is(MqlMethod.DeserializeEJson))
            {
                throw new ExpressionNotSupportedException(expression);
            }

            var valueExpression = arguments[0];
            var optionsExpression = arguments[1];

            var valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, valueExpression);
            var outputSerializer = context.GetSerializer(expression);
            var onErrorAst = TranslateOptions(context, expression, optionsExpression, outputSerializer);

            var ast = AstExpression.DeserializeEJson(valueTranslation.Ast, onErrorAst);
            return new TranslatedExpression(expression, ast, outputSerializer);
        }

        private static AstExpression TranslateOptions(
            TranslationContext context,
            Expression expression,
            Expression optionsExpression,
            IBsonSerializer outputSerializer)
        {
            return optionsExpression switch
            {
                ConstantExpression constantExpression => TranslateOptions(constantExpression, outputSerializer),
                MemberInitExpression memberInitExpression => TranslateOptions(context, expression, memberInitExpression, outputSerializer),
                _ => throw new ExpressionNotSupportedException(optionsExpression, containingExpression: expression, because: "the options argument must be either a constant or a member initialization expression.")
            };
        }

        private static AstExpression TranslateOptions(
            ConstantExpression optionsExpression,
            IBsonSerializer outputSerializer)
        {
            var options = (DeserializeEJsonOptions)optionsExpression.Value;

            AstExpression onErrorAst = null;
            if (options != null)
            {
                if (options.OnErrorWasSet(out var onErrorValue))
                {
                    var serializedOnErrorValue = SerializationHelper.SerializeValue(outputSerializer, onErrorValue);
                    onErrorAst = AstExpression.Constant(serializedOnErrorValue);
                }
            }

            return onErrorAst;
        }

        private static AstExpression TranslateOptions(
            TranslationContext context,
            Expression expression,
            MemberInitExpression optionsExpression,
            IBsonSerializer outputSerializer)
        {
            TranslatedExpression onErrorTranslation = null;

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
                    case nameof(DeserializeEJsonOptions<object>.OnError):
                        onErrorTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, memberExpression);
                        SerializationHelper.EnsureSerializerIsCompatible(memberExpression, containingExpression: expression, onErrorTranslation.Serializer, expectedSerializer: outputSerializer);
                        break;
                    default:
                        throw new ExpressionNotSupportedException(memberExpression, because: $"memberName {memberName} is invalid");
                }
            }

            return onErrorTranslation?.Ast;
        }
    }
}
