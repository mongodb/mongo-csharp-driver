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
using MongoDB.Bson.Serialization.Conventions;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators
{
    public static class ExpressionTranslator
    {
        // public static methods
        public static ExpressionTranslation Translate(TranslationContext context, Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Convert:
                case ExpressionType.Not:
                    return UnaryExpressionTranslator.Translate(context, (UnaryExpression)expression);

                case ExpressionType.Add:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Coalesce:
                case ExpressionType.Divide:
                case ExpressionType.Equal:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.Modulo:
                case ExpressionType.Multiply:
                case ExpressionType.NotEqual:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.Power:
                case ExpressionType.Subtract:
                    return BinaryExpressionTranslator.Translate(context, (BinaryExpression)expression);

                case ExpressionType.ArrayIndex:
                    return ArrayIndexTranslator.Translate(context, (BinaryExpression)expression);
                case ExpressionType.ArrayLength:
                    return ArrayLengthTranslator.Translate(context, (UnaryExpression)expression);
                case ExpressionType.Call:
                    return MethodCallExpressionTranslator.Translate(context, (MethodCallExpression)expression);
                case ExpressionType.Constant:
                    return ConstantExpressionTranslator.Translate(context, (ConstantExpression)expression);
                case ExpressionType.MemberAccess:
                    return MemberExpressionTranslator.Translate(context, (MemberExpression)expression);
                case ExpressionType.MemberInit:
                    return MemberInitExpressionTranslator.Translate(context, (MemberInitExpression)expression);
                case ExpressionType.New:
                    return NewExpressionTranslator.Translate(context, (NewExpression)expression);
                case ExpressionType.Parameter:
                    return ParameterExpressionTranslator.Translate(context, (ParameterExpression)expression);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
