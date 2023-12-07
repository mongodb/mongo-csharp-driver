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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ExpressionTranslators
{
    internal static class ArrayLengthComparisonExpressionToFilterTranslator
    {
        // caller is responsible for ensuring constant is on the right
        public static bool CanTranslate(Expression leftExpression, Expression rightExpression, out UnaryExpression arrayLengthExpression, out Expression sizeExpression)
        {
            if (leftExpression.NodeType == ExpressionType.ArrayLength)
            {
                arrayLengthExpression = (UnaryExpression)leftExpression;
                sizeExpression = rightExpression;
                return true;
            }

            arrayLengthExpression = null;
            sizeExpression = null;
            return false;
        }

        public static AstFilter Translate(TranslationContext context, BinaryExpression expression, AstComparisonFilterOperator comparisonOperator, UnaryExpression arrayLengthExpression, Expression sizeExpression)
        {
            if (arrayLengthExpression.NodeType == ExpressionType.ArrayLength)
            {
                var arrayExpression = arrayLengthExpression.Operand;
                var arrayField = ExpressionToFilterFieldTranslator.Translate(context, arrayExpression);
                var size = sizeExpression.GetConstantValue<int>(containingExpression: expression);

                switch (comparisonOperator)
                {
                    case AstComparisonFilterOperator.Eq:
                        return AstFilter.Size(arrayField, size);

                    case AstComparisonFilterOperator.Gt:
                        return AstFilter.Exists(ItemField(arrayField, size));

                    case AstComparisonFilterOperator.Gte:
                        return AstFilter.Exists(ItemField(arrayField, size - 1));

                    case AstComparisonFilterOperator.Lt:
                        return AstFilter.NotExists(ItemField(arrayField, size - 1));

                    case AstComparisonFilterOperator.Lte:
                        return AstFilter.NotExists(ItemField(arrayField, size));

                    case AstComparisonFilterOperator.Ne:
                        return AstFilter.Not(AstFilter.Size(arrayField, size));
                }

            }

            throw new ExpressionNotSupportedException(expression);

            static AstFilterField ItemField(AstFilterField field, int index) => field.SubField(index.ToString(), BsonValueSerializer.Instance);
        }
    }
}
