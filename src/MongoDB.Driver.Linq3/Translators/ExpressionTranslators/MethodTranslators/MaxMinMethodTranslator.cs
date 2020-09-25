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

using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq3.Ast.Expressions;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators.MethodTranslators
{
    public static class MaxMinMethodTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.DeclaringType == typeof(Enumerable) && (method.Name == "Max" || method.Name == "Min"))
            {
                var source = arguments[0];
                var translatedSource = ExpressionTranslator.Translate(context, source);

                AstExpression translation;
                var @operator = method.Name == "Max" ? AstUnaryOperator.Max : AstUnaryOperator.Min;
                if (arguments.Count == 1)
                {
                    translation = new AstUnaryExpression(@operator, translatedSource.Translation);
                }
                else
                {
                    var selector = (LambdaExpression)arguments[1];
                    var selectorParameter = selector.Parameters[0];
                    if (!TryGetSourceItemSerializer(translatedSource.Serializer, out var sourceItemSerializer))
                    {
                        goto notSupported;
                    }
                    var selectorContext = context.WithSymbol(selectorParameter, new Misc.Symbol("$" + selectorParameter.Name, sourceItemSerializer));
                    var translatedSelector = ExpressionTranslator.Translate(selectorContext, selector.Body);

                    translation = new AstUnaryExpression(
                        @operator,
                        new AstMapExpression(
                            input: translatedSource.Translation,
                            @as: selectorParameter.Name,
                            @in: translatedSelector.Translation));
                }
                translation = new AstConvertExpression(translation, expression.Type);

                var serializer = BsonSerializer.LookupSerializer(expression.Type);
                return new TranslatedExpression(expression, translation, serializer);
            }

        notSupported:
            throw new ExpressionNotSupportedException(expression);
        }

        private static bool TryGetSourceItemSerializer(IBsonSerializer sourceSerializer, out IBsonSerializer sourceItemSerializer)
        {
            sourceItemSerializer = null;

            if (sourceSerializer is IBsonArraySerializer arraySerializer && arraySerializer.TryGetItemSerializationInfo(out var sourceItemSerializationInfo))
            {
                sourceItemSerializer = sourceItemSerializationInfo.Serializer;
                return true;
            }

            return false;
        }
    }
}
