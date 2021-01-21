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
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public static class StandardDeviationMethodsToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (IsStandardDeviationMethod(method, out var @operator))
            {
                if (arguments.Count == 1 || arguments.Count == 2)
                {
                    var sourceExpression = arguments[0];
                    LambdaExpression selectorExpression = null;
                    if (arguments.Count == 2)
                    {
                        selectorExpression = (LambdaExpression)arguments[1];
                    }

                    var sourceTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, sourceExpression);
                    if (selectorExpression != null)
                    {
                        var selectorParameter = selectorExpression.Parameters[0];
                        var selectorParameterSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);
                        var selectorContext = context.WithSymbol(selectorParameter, new Symbol("$" + selectorParameter.Name, selectorParameterSerializer));
                        var selectorTranslation = ExpressionToAggregationExpressionTranslator.Translate(selectorContext, selectorExpression.Body);
                        var selectorAst = AstMapExpression.Create(
                            input: sourceTranslation.Ast,
                            @as: selectorParameter.Name,
                            @in: selectorTranslation.Ast);
                        var selectorResultSerializer = BsonSerializer.LookupSerializer(selectorExpression.ReturnType);
                        sourceTranslation = new AggregationExpression(selectorExpression, selectorAst, selectorResultSerializer);
                    }
                    var ast = new AstUnaryExpression(@operator, sourceTranslation.Ast);
                    var serializer = BsonSerializer.LookupSerializer(expression.Type);

                    return new AggregationExpression(expression, ast, serializer);
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static bool IsStandardDeviationMethod(MethodInfo methodInfo, out AstUnaryOperator @operator)
        {
            @operator = default;

            if (methodInfo.DeclaringType == typeof(EnumerableExtensions))
            {
                switch (methodInfo.Name)
                {
                    case "StandardDeviationPopulation": @operator = AstUnaryOperator.StdDevPop; return true;
                    case "StandardDeviationSample": @operator = AstUnaryOperator.StdDevSamp; return true;
                }
            }

            return false;
        }
    }
}
