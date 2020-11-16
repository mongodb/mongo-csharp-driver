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
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public static class MaxOrMinMethodToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.DeclaringType == typeof(Enumerable) && (method.Name == "Max" || method.Name == "Min"))
            {
                var sourceExpression = arguments[0];

                var @operator = method.Name == "Max" ? AstUnaryOperator.Max : AstUnaryOperator.Min;
                var sourceTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, sourceExpression);
                AstExpression ast;
                if (arguments.Count == 1)
                {
                    ast = new AstUnaryExpression(@operator, sourceTranslation.Ast);
                }
                else
                {
                    var selectorExpression = (LambdaExpression)arguments[1];
                    var selectorParameter = selectorExpression.Parameters[0];
                    var selectorParameterSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);
                    var selectorContext = context.WithSymbol(selectorParameter, new Misc.Symbol("$" + selectorParameter.Name, selectorParameterSerializer));
                    var selectorTranslation = ExpressionToAggregationExpressionTranslator.Translate(selectorContext, selectorExpression.Body);

                    ast = new AstUnaryExpression(
                        @operator,
                        AstMapExpression.Create(
                            input: sourceTranslation.Ast,
                            @as: selectorParameter.Name,
                            @in: selectorTranslation.Ast));
                }
                var serializer = BsonSerializer.LookupSerializer(expression.Type);

                return new AggregationExpression(expression, ast, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
