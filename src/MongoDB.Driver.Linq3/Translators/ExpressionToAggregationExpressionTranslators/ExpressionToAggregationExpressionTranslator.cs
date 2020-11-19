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
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Serializers;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators
{
    public static class ExpressionToAggregationExpressionTranslator
    {
        // public static methods
        public static AggregationExpression Translate(TranslationContext context, Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Convert:
                case ExpressionType.Not:
                    return UnaryExpressionToAggregationTranslator.Translate(context, (UnaryExpression)expression);

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
                    return BinaryExpressionToAggregationExpressionTranslator.Translate(context, (BinaryExpression)expression);

                case ExpressionType.ArrayIndex:
                    return ArrayIndexExpressionToAggregationExpressionTranslator.Translate(context, (BinaryExpression)expression);
                case ExpressionType.ArrayLength:
                    return ArrayLengthExpressionToAggregationExpressionTranslator.Translate(context, (UnaryExpression)expression);
                case ExpressionType.Call:
                    return MethodCallExpressionToAggregationExpressionTranslator.Translate(context, (MethodCallExpression)expression);
                case ExpressionType.Conditional:
                    return ConditionalExpressionToAggregationExpressionTranslator.Translate(context, (ConditionalExpression)expression);
                case ExpressionType.Constant:
                    return ConstantExpressionToAggregationExpressionTranslator.Translate(context, (ConstantExpression)expression);
                case ExpressionType.MemberAccess:
                    return MemberExpressionToAggregationExpressionTranslator.Translate(context, (MemberExpression)expression);
                case ExpressionType.MemberInit:
                    return MemberInitExpressionToAggregationExpressionTranslator.Translate(context, (MemberInitExpression)expression);
                case ExpressionType.New:
                    return NewExpressionToAggregationExpressionTranslator.Translate(context, (NewExpression)expression);
                case ExpressionType.NewArrayInit:
                    return NewArrayInitExpressionToAggregationExpressionTranslator.Translate(context, (NewArrayExpression)expression);
                case ExpressionType.Parameter:
                    return ParameterExpressionToAggregationExpressionTranslator.Translate(context, (ParameterExpression)expression);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        public static AggregationExpression TranslateEnumerable(TranslationContext context, Expression expression)
        {
            var aggregateExpression = Translate(context, expression);

            var serializer = aggregateExpression.Serializer;
            if (serializer is IWrappedEnumerableSerializer wrappedEnumerableSerializer)
            {
                var enumerableFieldName = wrappedEnumerableSerializer.EnumerableFieldName;
                var enumerableElementSerializer = wrappedEnumerableSerializer.EnumerableElementSerializer;
                var enumerableSerializer = IEnumerableSerializer.Create(enumerableElementSerializer);
                var ast = CreateFieldReference(aggregateExpression.Ast, enumerableFieldName);
                return new AggregationExpression(aggregateExpression.Expression, ast, enumerableSerializer);
            }

            return aggregateExpression;
        }

        public static AggregationExpression TranslateLambdaBody(TranslationContext context, LambdaExpression lambdaExpression, IBsonSerializer parameterSerializer)
        {
            var parameterExpression = lambdaExpression.Parameters.Single();
            var parameterSymbol = new Symbol(parameterExpression.Name, parameterSerializer);
            var lambdaContext = context.WithSymbolAsCurrent(parameterExpression, parameterSymbol);
            return Translate(lambdaContext, lambdaExpression.Body);
        }

        // TODO: this probably needs to be moved to a helper class so that it can be used in more places
        private static AstExpression CreateFieldReference(AstExpression astExpression, string fieldName)
        {
            if (astExpression is AstFieldExpression astFieldExpression)
            {
                var containerFieldName = astFieldExpression.Field;
                var combinedFieldName = TranslatedFieldHelper.Combine(containerFieldName, fieldName);
                return new AstFieldExpression(combinedFieldName);
            }
            else
            {
                return new AstLetExpression(
                    vars: new[] { new AstComputedField("_container", astExpression) },
                    @in: new AstFieldExpression($"$$_container.{fieldName}"));
            }
        }
    }
}
