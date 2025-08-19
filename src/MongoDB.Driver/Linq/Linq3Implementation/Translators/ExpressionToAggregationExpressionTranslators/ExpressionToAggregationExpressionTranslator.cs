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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class ExpressionToAggregationExpressionTranslator
    {
        // public static methods
        public static TranslatedExpression Translate(TranslationContext context, Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Convert:
                case ExpressionType.TypeAs:
                    return ConvertExpressionToAggregationExpressionTranslator.Translate(context, (UnaryExpression)expression);

                case ExpressionType.Not:
                    return NotExpressionToAggregationExpressionTranslator.Translate(context, (UnaryExpression)expression);

                case ExpressionType.Add:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Coalesce:
                case ExpressionType.Divide:
                case ExpressionType.Equal:
                case ExpressionType.ExclusiveOr:
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
                case ExpressionType.Index:
                    return IndexExpressionToAggregationExpressionTranslator.Translate(context, (IndexExpression)expression);
                case ExpressionType.ListInit:
                    return ListInitExpressionToAggregationExpressionTranslator.Translate(context, (ListInitExpression)expression);
                case ExpressionType.MemberAccess:
                    return MemberExpressionToAggregationExpressionTranslator.Translate(context, (MemberExpression)expression);
                case ExpressionType.MemberInit:
                    return MemberInitExpressionToAggregationExpressionTranslator.Translate(context, (MemberInitExpression)expression);
                case ExpressionType.Negate:
                    return NegateExpressionToAggregationExpressionTranslator.Translate(context, (UnaryExpression)expression);
                case ExpressionType.New:
                    return NewExpressionToAggregationExpressionTranslator.Translate(context, (NewExpression)expression);
                case ExpressionType.NewArrayInit:
                    return NewArrayInitExpressionToAggregationExpressionTranslator.Translate(context, (NewArrayExpression)expression);
                case ExpressionType.Parameter:
                    return ParameterExpressionToAggregationExpressionTranslator.Translate(context, (ParameterExpression)expression);
                case ExpressionType.TypeIs:
                    return TypeIsExpressionToAggregationExpressionTranslator.Translate(context, (TypeBinaryExpression)expression);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        public static TranslatedExpression TranslateEnumerable(TranslationContext context, Expression expression)
        {
            var aggregateExpression = Translate(context, expression);

            var serializer = aggregateExpression.Serializer;
            if (serializer is IWrappedEnumerableSerializer wrappedEnumerableSerializer)
            {
                var enumerableFieldName = wrappedEnumerableSerializer.EnumerableFieldName;
                var enumerableElementSerializer = wrappedEnumerableSerializer.EnumerableElementSerializer;
                var enumerableSerializer = IEnumerableSerializer.Create(enumerableElementSerializer);
                var ast = AstExpression.GetField(aggregateExpression.Ast, enumerableFieldName);

                return new TranslatedExpression(aggregateExpression.Expression, ast, enumerableSerializer);
            }

            return aggregateExpression;
        }

        public static TranslatedExpression TranslateLambdaBody(
            TranslationContext context,
            LambdaExpression lambdaExpression,
            IBsonSerializer parameterSerializer,
            bool asRoot)
        {
            var parameterExpression = lambdaExpression.Parameters.Single();
            if (parameterSerializer.ValueType != parameterExpression.Type)
            {
                throw new ArgumentException($"ValueType '{parameterSerializer.ValueType.FullName}' of parameterSerializer does not match parameter type '{parameterExpression.Type.FullName}'.", nameof(parameterSerializer));
            }
            var parameterSymbol =
                asRoot ?
                    context.CreateRootSymbol(parameterExpression, parameterSerializer) :
                    context.CreateSymbol(parameterExpression, parameterSerializer, isCurrent: false);

            return TranslateLambdaBody(context, lambdaExpression, parameterSymbol);
        }

        public static TranslatedExpression TranslateLambdaBody(
            TranslationContext context,
            LambdaExpression lambdaExpression,
            Symbol parameterSymbol)
        {
            var lambdaContext = context.WithSymbol(parameterSymbol);
            var translatedBody = Translate(lambdaContext, lambdaExpression.Body);

            var lambdaReturnType = lambdaExpression.ReturnType;
            var bodySerializer = translatedBody.Serializer;
            var bodyType = bodySerializer.ValueType;
            if (bodyType != lambdaReturnType)
            {
                if (lambdaReturnType.IsAssignableFrom(bodyType))
                {
                    var downcastingSerializer = DowncastingSerializer.Create(baseType: lambdaReturnType, derivedType: bodyType, derivedTypeSerializer: bodySerializer);
                    translatedBody = new TranslatedExpression(translatedBody.Expression, translatedBody.Ast, downcastingSerializer);
                }
                else
                {
                    throw new ExpressionNotSupportedException(lambdaExpression, because: "lambda body type is not convertible to lambda return type");
                }
            }

            return translatedBody;
        }
    }
}
