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

using System;
using System.Linq.Expressions;
using MongoDB.Driver.Linq;

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc
{
    internal static class ConvertHelper
    {
        public static Expression RemoveConvertToMongoQueryable(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Convert)
            {
                var convertExpression = (UnaryExpression)expression;
                var convertToType = convertExpression.Type;
                if (convertToType.IsGenericType() &&
                    convertToType.GetGenericTypeDefinition() == typeof(IMongoQueryable<>))
                {
                    return convertExpression.Operand;
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        public static Expression RemoveWideningConvert(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Convert)
            {
                var convertExpression = (UnaryExpression)expression;
                var sourceType = convertExpression.Operand.Type;
                var targetType = expression.Type;
                if (IsWideningConvert(sourceType, targetType))
                {
                    return convertExpression.Operand;
                }
            }

            return expression;

            static bool IsWideningConvert(Type sourceType, Type targetType)
            {
                if (sourceType == typeof(int))
                {
                    return targetType == typeof(long) || targetType == typeof(double) || targetType == typeof(decimal);
                }

                if (sourceType == typeof(long))
                {
                    return targetType == typeof(double) || targetType == typeof(decimal);
                }

                if (sourceType == typeof(double))
                {
                    return targetType == typeof(decimal);
                }

                return false;
            }
        }
    }
}
