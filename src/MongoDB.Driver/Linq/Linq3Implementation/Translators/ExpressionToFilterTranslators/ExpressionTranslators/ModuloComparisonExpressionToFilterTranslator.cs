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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ExpressionTranslators
{
    internal static class ModuloComparisonExpressionToFilterTranslator
    {
        // caller is responsible for ensuring constant is on the right
        public static bool CanTranslate(Expression leftExpression, Expression rightExpression, out BinaryExpression moduloExpression, out Expression remainderExpression)
        {
            if (leftExpression.NodeType == ExpressionType.Modulo)
            {
                moduloExpression = (BinaryExpression)leftExpression;
                remainderExpression = rightExpression;
                return true;
            }

            moduloExpression = null;
            remainderExpression = null;
            return false;
        }

        public static AstFilter Translate(TranslationContext context, BinaryExpression expression, BinaryExpression moduloExpression, Expression remainderExpression)
        {
            var fieldExpression = moduloExpression.Left;
            var fieldTranslation = ExpressionToFilterFieldTranslator.Translate(context, fieldExpression);

            var divisorExpression = moduloExpression.Right;
            BsonValue divisor;
            BsonValue remainder;
            if (divisorExpression.Type == typeof(decimal) && remainderExpression.Type == typeof(decimal))
            {
                divisor = divisorExpression.GetConstantValue<decimal>(containingExpression: moduloExpression);
                remainder = remainderExpression.GetConstantValue<decimal>(containingExpression: expression);
            }
            else if (divisorExpression.Type == typeof(double) && remainderExpression.Type == typeof(double))
            {
                divisor = divisorExpression.GetConstantValue<double>(containingExpression: moduloExpression);
                remainder = remainderExpression.GetConstantValue<double>(containingExpression: expression);
            }
            else if (divisorExpression.Type == typeof(float) && remainderExpression.Type == typeof(float))
            {
                divisor = divisorExpression.GetConstantValue<float>(containingExpression: moduloExpression);
                remainder = remainderExpression.GetConstantValue<float>(containingExpression: expression);
            }
            else if (divisorExpression.Type == typeof(int) && remainderExpression.Type == typeof(int))
            {
                divisor = divisorExpression.GetConstantValue<int>(containingExpression: moduloExpression);
                remainder = remainderExpression.GetConstantValue<int>(containingExpression: expression);
            }
            else if (divisorExpression.Type == typeof(long) && remainderExpression.Type == typeof(long))
            {
                divisor = divisorExpression.GetConstantValue<long>(containingExpression: moduloExpression);
                remainder = remainderExpression.GetConstantValue<long>(containingExpression: expression);
            }
            else if (divisorExpression.Type == typeof(uint) && remainderExpression.Type == typeof(uint))
            {
                divisor = divisorExpression.GetConstantValue<uint>(containingExpression: moduloExpression);
                remainder = remainderExpression.GetConstantValue<uint>(containingExpression: expression);
            }
            else if (divisorExpression.Type == typeof(ulong) && remainderExpression.Type == typeof(ulong))
            {
                divisor = (long)divisorExpression.GetConstantValue<ulong>(containingExpression: moduloExpression);
                remainder = (long)remainderExpression.GetConstantValue<ulong>(containingExpression: expression);
            }
            else
            {
                throw new ExpressionNotSupportedException(expression);
            }

            var moduloComparisonAst = AstFilter.Mod(fieldTranslation.AstField, divisor, remainder);
            switch (expression.NodeType)
            {
                case ExpressionType.Equal:
                    return moduloComparisonAst;

                case ExpressionType.NotEqual:
                    return AstFilter.Not(moduloComparisonAst);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
