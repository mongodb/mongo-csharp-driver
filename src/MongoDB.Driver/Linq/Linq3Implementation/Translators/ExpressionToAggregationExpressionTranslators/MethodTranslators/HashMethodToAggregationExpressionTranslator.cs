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
using MongoDB.Bson;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class HashMethodToAggregationExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(MqlMethod.Hash, MqlMethod.HexHash))
            {
                var algorithmParameter = arguments[1];
                var algorithm = algorithmParameter.GetConstantValue<MqlHashAlgorithm>(expression);

                if (algorithm == MqlHashAlgorithm.Undefined)
                {
                    throw new ExpressionNotSupportedException(expression, "hash algorithm should be specified");
                }

                var valueExpression = arguments[0];
                var valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, valueExpression);

                if (valueExpression.Type != typeof(BsonBinaryData))
                {
                    var valueRepresentation = SerializationHelper.GetRepresentation(valueTranslation.Serializer);
                    if (valueRepresentation != BsonType.Binary && valueRepresentation != BsonType.String)
                    {
                        throw new ExpressionNotSupportedException(expression, because: $"{valueExpression} should have BinData or String representation, but found {valueRepresentation} representation.");
                    }
                }

                AstExpression ast;
                if (method.Is(MqlMethod.Hash))
                {
                    ast = AstExpression.HashExpression(valueTranslation.Ast, algorithm);
                }
                else if (method.Is(MqlMethod.HexHash))
                {
                    ast = AstExpression.HexHashExpression(valueTranslation.Ast, algorithm);
                }
                else
                {
                    throw new ExpressionNotSupportedException(expression, $"unsupported hash method: {method}");
                }

                return new TranslatedExpression(expression, ast, context.GetSerializer(expression));
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
