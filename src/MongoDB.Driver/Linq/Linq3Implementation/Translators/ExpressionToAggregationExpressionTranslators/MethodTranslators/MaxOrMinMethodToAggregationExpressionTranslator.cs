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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class MaxOrMinMethodToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.DeclaringType == typeof(Enumerable) && (method.Name == "Max" || method.Name == "Min"))
            {
                var sourceExpression = arguments[0];
                var sourceTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, sourceExpression);
                AstExpression ast;
                IBsonSerializer serializer;
                if (arguments.Count == 1)
                {
                    var array = sourceTranslation.Ast;
                    ast = method.Name == "Max" ? AstExpression.Max(array) : AstExpression.Min(array);
                    serializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);
                }
                else
                {
                    var selectorLambda = (LambdaExpression)arguments[1];
                    var selectorParameter = selectorLambda.Parameters[0];
                    var selectorParameterSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);
                    var selectorContext = context.WithSymbol(selectorParameter, new Misc.Symbol("$" + selectorParameter.Name, selectorParameterSerializer));
                    var selectorTranslation = ExpressionToAggregationExpressionTranslator.Translate(selectorContext, selectorLambda.Body);
                    var mappedArray =
                        AstExpression.Map(
                            input: sourceTranslation.Ast,
                            @as: selectorParameter.Name,
                            @in: selectorTranslation.Ast);
                    ast = method.Name == "Max" ? AstExpression.Max(mappedArray) : AstExpression.Min(mappedArray);
                    serializer = selectorTranslation.Serializer;
                }
                return new AggregationExpression(expression, ast, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
