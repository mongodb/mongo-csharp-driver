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
using MongoDB.Bson;
using MongoDB.Driver.Linq3.Ast.Filters;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ExpressionTranslators
{
    public static class CountComparisonExpressionToFilterTranslator
    {
        public static bool CanTranslate(Expression leftExpression, Expression rightExpression, out Expression enumerableExpression, out Expression sizeExpression)
        {
            if (leftExpression.NodeType == ExpressionType.MemberAccess)
            {
                var leftMemberExpression = (MemberExpression)leftExpression;
                if (leftMemberExpression.Expression != null)
                {
                    var member = leftMemberExpression.Member;
                    if (member.MemberType == MemberTypes.Property &&
                        member.Name == "Count")
                    {
                        enumerableExpression = leftMemberExpression.Expression;
                        sizeExpression = rightExpression;
                        return true;
                    }
                }
            }

            if (leftExpression.NodeType == ExpressionType.Call)
            {
                var leftMethodCallExpression = (MethodCallExpression)leftExpression;
                var method = leftMethodCallExpression.Method;
                if (method.IsOneOf(EnumerableMethod.Count, EnumerableMethod.LongCount))
                {
                    var sourceExpression = leftMethodCallExpression.Arguments[0];
                    if (sourceExpression.Type != typeof(string))
                    {
                        enumerableExpression = leftMethodCallExpression.Arguments[0];
                        sizeExpression = rightExpression;
                        return true;
                    }
                }
            }

            enumerableExpression = null;
            sizeExpression = null;
            return false;
        }

        public static AstFilter Translate(TranslationContext context, BinaryExpression expression, Expression enumerableExpression, Expression sizeExpression)
        {
            var field = ExpressionToFilterFieldTranslator.Translate(context, enumerableExpression);

            if (TryConvertSizeExpressionToBsonValue(sizeExpression, out var size))
            {
                var compareCountFilter = new AstSizeFilter(field, size);
                switch (expression.NodeType)
                {
                    case ExpressionType.Equal: return compareCountFilter;
                    case ExpressionType.NotEqual: return new AstNorFilter(compareCountFilter);
                }
            }

            throw new ExpressionNotSupportedException(expression);
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
