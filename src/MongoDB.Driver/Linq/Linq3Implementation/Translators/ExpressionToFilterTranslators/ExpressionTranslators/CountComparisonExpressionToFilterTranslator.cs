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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ExpressionTranslators
{
    internal static class CountComparisonExpressionToFilterTranslator
    {
        // caller is responsible for ensuring constant is on the right
        public static bool CanTranslate(Expression leftExpression, Expression rightExpression, out Expression enumerableExpression, out Expression sizeExpression)
        {
            if (leftExpression is MemberExpression leftMemberExpression &&
                EnumerableProperty.IsCountProperty(leftMemberExpression))
            {
                enumerableExpression = leftMemberExpression.Expression;
                sizeExpression = rightExpression;
                return true;
            }

            if (leftExpression.NodeType == ExpressionType.Call)
            {
                var leftMethodCallExpression = (MethodCallExpression)leftExpression;
                var method = leftMethodCallExpression.Method;
                var arguments = leftMethodCallExpression.Arguments;

                if (method.IsOneOf(EnumerableMethod.Count, EnumerableMethod.LongCount))
                {
                    var sourceExpression = arguments[0];
                    if (sourceExpression.Type != typeof(string))
                    {
                        enumerableExpression = arguments[0];
                        sizeExpression = rightExpression;
                        return true;
                    }
                }
            }

            enumerableExpression = null;
            sizeExpression = null;
            return false;
        }

        public static AstFilter Translate(TranslationContext context, BinaryExpression expression, AstComparisonFilterOperator comparisonOperator, Expression enumerableExpression, Expression sizeExpression)
        {
            var fieldTranslation = ExpressionToFilterFieldTranslator.Translate(context, enumerableExpression);
            SerializationHelper.EnsureRepresentationIsArray(enumerableExpression, fieldTranslation.Serializer);

            if (TryConvertSizeExpressionToBsonValue(sizeExpression, out var size))
            {
                switch (comparisonOperator)
                {
                    case AstComparisonFilterOperator.Eq:
                        return AstFilter.Size(fieldTranslation.AstField, size);

                    case AstComparisonFilterOperator.Gt:
                        return AstFilter.Exists(ItemField(fieldTranslation.AstField, size.ToInt64()));

                    case AstComparisonFilterOperator.Gte:
                        return AstFilter.Exists(ItemField(fieldTranslation.AstField, size.ToInt64() - 1));

                    case AstComparisonFilterOperator.Lt:
                        return AstFilter.NotExists(ItemField(fieldTranslation.AstField, size.ToInt64() - 1));

                    case AstComparisonFilterOperator.Lte:
                        return AstFilter.NotExists(ItemField(fieldTranslation.AstField, size.ToInt64()));

                    case AstComparisonFilterOperator.Ne:
                        return AstFilter.Not(AstFilter.Size(fieldTranslation.AstField, size));
                }
            }

            throw new ExpressionNotSupportedException(expression);

            static AstFilterField ItemField(AstFilterField field, long index) => field.SubField(index.ToString());
        }

        private static bool TryConvertSizeExpressionToBsonValue(Expression sizeExpression, out BsonValue size)
        {
            if (sizeExpression is ConstantExpression sizeConstantExpression)
            {
                if (sizeConstantExpression.Type == typeof(int))
                {
                    size = (int)sizeConstantExpression.Value;
                    return true;
                }
                else if (sizeConstantExpression.Type == typeof(long))
                {
                    size = (long)sizeConstantExpression.Value;
                    return true;
                }
            }

            size = null;
            return false;
        }
    }
}
