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
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ExpressionTranslators;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.MethodTranslators;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators
{
    internal static class ExpressionToFilterTranslator
    {
        // public methods
        public static AstFilter Translate(TranslationContext context, Expression expression, bool exprOk = true)
        {
            AstFilter filter;
            try
            {
                filter = TranslateUsingQueryOperators(context, expression);
            }
            catch (ExpressionNotSupportedException)
            {
                filter = TranslateUsingAggregationOperators(context, expression);
            }

            if (filter.UsesExpr && !exprOk)
            {
                throw new ExpressionNotSupportedException(expression);
            }

            return filter;
        }

        public static AstFilter TranslateLambda(
            TranslationContext context,
            LambdaExpression lambdaExpression,
            IBsonSerializer parameterSerializer,
            bool asRoot = false)
        {
            var parameterExpression = lambdaExpression.Parameters.Single();
            if (parameterSerializer.ValueType != parameterExpression.Type)
            {
                throw new ArgumentException($"ValueType '{parameterSerializer.ValueType.FullName}' of parameterSerializer does not match parameter type '{parameterExpression.Type.FullName}'.", nameof(parameterSerializer));
            }

            var parameterSymbol =
                asRoot ?
                    context.CreateSymbolWithVarName(parameterExpression, varName: "ROOT", parameterSerializer, isCurrent: true) :
                    context.CreateSymbol(parameterExpression, parameterSerializer, isCurrent: true);

            var lambdaContext = context.WithSymbol(parameterSymbol);
            return Translate(lambdaContext, lambdaExpression.Body);
        }

        // private methods
        private static AstFilter TranslateUsingAggregationOperators(TranslationContext context, Expression expression)
        {
            var expressionTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, expression);
            return AstFilter.Expr(expressionTranslation.Ast);
        }

        private static AstFilter TranslateUsingQueryOperators(TranslationContext context, Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return AndExpressionToFilterTranslator.Translate(context, (BinaryExpression)expression);

                case ExpressionType.Call:
                    return MethodCallExpressionToFilterTranslator.Translate(context, (MethodCallExpression)expression);

                case ExpressionType.Equal:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.NotEqual:
                    return ComparisonExpressionToFilterTranslator.Translate(context, (BinaryExpression)expression);

                case ExpressionType.MemberAccess:
                    return MemberExpressionToFilterTranslator.Translate(context, (MemberExpression)expression);

                case ExpressionType.Not:
                    return NotExpressionToFilterTranslator.Translate(context, (UnaryExpression)expression);

                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return OrExpressionToFilterTranslator.Translate(context, (BinaryExpression)expression);

                case ExpressionType.Parameter:
                    return ParameterExpressionToFilterTranslator.Translate(context, (ParameterExpression)expression);

                case ExpressionType.TypeIs:
                    return TypeIsExpressionToFilterTranslator.Translate(context, (TypeBinaryExpression)expression);

                case ExpressionType.Constant:
                    return ConstantExpressionToFilterTranslator.Translate(context, (ConstantExpression)expression);
            }

            if (expression.Type == typeof(bool))
            {
                var field = ExpressionToFilterFieldTranslator.Translate(context, expression);
                var serializedTrue = SerializationHelper.SerializeValue(field.Serializer, true);
                return AstFilter.Eq(field, serializedTrue);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
