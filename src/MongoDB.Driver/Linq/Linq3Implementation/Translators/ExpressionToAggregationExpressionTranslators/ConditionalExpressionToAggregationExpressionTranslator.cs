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
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class ConditionalExpressionToAggregationExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, ConditionalExpression expression)
        {
            if (expression.NodeType == ExpressionType.Conditional)
            {
                var testExpression = expression.Test;
                var ifTrueExpression = expression.IfTrue;
                var ifFalseExpression = expression.IfFalse;

                var testTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, testExpression);

                TranslatedExpression ifTrueTranslation;
                TranslatedExpression ifFalseTranslation;
                IBsonSerializer resultSerializer;
                if (ifTrueExpression is ConstantExpression ifTrueConstantExpression)
                {
                    ifFalseTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, ifFalseExpression);
                    resultSerializer = ifFalseTranslation.Serializer;
                    ifTrueTranslation = ConstantExpressionToAggregationExpressionTranslator.Translate(ifTrueConstantExpression, resultSerializer);
                }
                else if (ifFalseExpression is ConstantExpression ifFalseConstantExpression)
                {
                    ifTrueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, ifTrueExpression);
                    resultSerializer = ifTrueTranslation.Serializer;
                    ifFalseTranslation = ConstantExpressionToAggregationExpressionTranslator.Translate(ifFalseConstantExpression, resultSerializer);
                }
                else
                {
                    ifTrueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, ifTrueExpression);
                    ifFalseTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, ifFalseExpression);

                    resultSerializer = ifTrueTranslation.Serializer;
                    if (!ifFalseTranslation.Serializer.Equals(resultSerializer))
                    {
                        throw new ExpressionNotSupportedException(expression, because: "IfTrue and IfFalse expressions have different serializers");
                    }
                }

                var ast = AstExpression.Cond(testTranslation.Ast, ifTrueTranslation.Ast, ifFalseTranslation.Ast);
                return new TranslatedExpression(expression, ast, resultSerializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
