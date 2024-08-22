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
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class WindowMethodToAggregationExpressionTranslator
    {
        private static readonly MethodInfo[] __windowMethods =
        {
            WindowMethod.AddToSet,
            WindowMethod.AverageWithDecimal,
            WindowMethod.AverageWithDouble,
            WindowMethod.AverageWithInt32,
            WindowMethod.AverageWithInt64,
            WindowMethod.AverageWithNullableDecimal,
            WindowMethod.AverageWithNullableDouble,
            WindowMethod.AverageWithNullableInt32,
            WindowMethod.AverageWithNullableInt64,
            WindowMethod.AverageWithNullableSingle,
            WindowMethod.AverageWithSingle,
            WindowMethod.Count,
            WindowMethod.CovariancePopulationWithDecimals,
            WindowMethod.CovariancePopulationWithDoubles,
            WindowMethod.CovariancePopulationWithInt32s,
            WindowMethod.CovariancePopulationWithInt64s,
            WindowMethod.CovariancePopulationWithNullableDecimals,
            WindowMethod.CovariancePopulationWithNullableDoubles,
            WindowMethod.CovariancePopulationWithNullableInt32s,
            WindowMethod.CovariancePopulationWithNullableInt64s,
            WindowMethod.CovariancePopulationWithNullableSingles,
            WindowMethod.CovariancePopulationWithSingles,
            WindowMethod.CovarianceSampleWithDecimals,
            WindowMethod.CovarianceSampleWithDoubles,
            WindowMethod.CovarianceSampleWithInt32s,
            WindowMethod.CovarianceSampleWithInt64s,
            WindowMethod.CovarianceSampleWithNullableDecimals,
            WindowMethod.CovarianceSampleWithNullableDoubles,
            WindowMethod.CovarianceSampleWithNullableInt32s,
            WindowMethod.CovarianceSampleWithNullableInt64s,
            WindowMethod.CovarianceSampleWithNullableSingles,
            WindowMethod.CovarianceSampleWithSingles,
            WindowMethod.DenseRank,
            WindowMethod.DerivativeWithDecimal,
            WindowMethod.DerivativeWithDecimalAndUnit,
            WindowMethod.DerivativeWithDouble,
            WindowMethod.DerivativeWithDoubleAndUnit,
            WindowMethod.DerivativeWithInt32,
            WindowMethod.DerivativeWithInt32AndUnit,
            WindowMethod.DerivativeWithInt64,
            WindowMethod.DerivativeWithInt64AndUnit,
            WindowMethod.DerivativeWithSingle,
            WindowMethod.DerivativeWithSingleAndUnit,
            WindowMethod.DocumentNumber,
            WindowMethod.ExponentialMovingAverageWithDecimal,
            WindowMethod.ExponentialMovingAverageWithDouble,
            WindowMethod.ExponentialMovingAverageWithInt32,
            WindowMethod.ExponentialMovingAverageWithInt64,
            WindowMethod.ExponentialMovingAverageWithSingle,
            WindowMethod.First,
            WindowMethod.IntegralWithDecimal,
            WindowMethod.IntegralWithDecimalAndUnit,
            WindowMethod.IntegralWithDouble,
            WindowMethod.IntegralWithDoubleAndUnit,
            WindowMethod.IntegralWithInt32,
            WindowMethod.IntegralWithInt32AndUnit,
            WindowMethod.IntegralWithInt64,
            WindowMethod.IntegralWithInt64AndUnit,
            WindowMethod.IntegralWithSingle,
            WindowMethod.IntegralWithSingleAndUnit,
            WindowMethod.Last,
            WindowMethod.Locf,
            WindowMethod.Max,
            WindowMethod.Min,
            WindowMethod.Push,
            WindowMethod.Rank,
            WindowMethod.Shift,
            WindowMethod.ShiftWithDefaultValue,
            WindowMethod.StandardDeviationPopulationWithDecimal,
            WindowMethod.StandardDeviationPopulationWithDouble,
            WindowMethod.StandardDeviationPopulationWithInt32,
            WindowMethod.StandardDeviationPopulationWithInt64,
            WindowMethod.StandardDeviationPopulationWithNullableDecimal,
            WindowMethod.StandardDeviationPopulationWithNullableDouble,
            WindowMethod.StandardDeviationPopulationWithNullableInt32,
            WindowMethod.StandardDeviationPopulationWithNullableInt64,
            WindowMethod.StandardDeviationPopulationWithNullableSingle,
            WindowMethod.StandardDeviationPopulationWithSingle,
            WindowMethod.StandardDeviationSampleWithDecimal,
            WindowMethod.StandardDeviationSampleWithDouble,
            WindowMethod.StandardDeviationSampleWithInt32,
            WindowMethod.StandardDeviationSampleWithInt64,
            WindowMethod.StandardDeviationSampleWithNullableDecimal,
            WindowMethod.StandardDeviationSampleWithNullableDouble,
            WindowMethod.StandardDeviationSampleWithNullableInt32,
            WindowMethod.StandardDeviationSampleWithNullableInt64,
            WindowMethod.StandardDeviationSampleWithNullableSingle,
            WindowMethod.StandardDeviationSampleWithSingle,
            WindowMethod.SumWithDecimal,
            WindowMethod.SumWithDouble,
            WindowMethod.SumWithInt32,
            WindowMethod.SumWithInt64,
            WindowMethod.SumWithNullableDecimal,
            WindowMethod.SumWithNullableDouble,
            WindowMethod.SumWithNullableInt32,
            WindowMethod.SumWithNullableInt64,
            WindowMethod.SumWithNullableSingle,
            WindowMethod.SumWithSingle
        };

        private static readonly MethodInfo[] __nullaryMethods =
        {
            WindowMethod.Count,
            WindowMethod.DenseRank,
            WindowMethod.DocumentNumber,
            WindowMethod.Rank
        };

        private static readonly MethodInfo[] __unaryMethods =
       {
            WindowMethod.AddToSet,
            WindowMethod.AverageWithDecimal,
            WindowMethod.AverageWithDouble,
            WindowMethod.AverageWithInt32,
            WindowMethod.AverageWithInt64,
            WindowMethod.AverageWithNullableDecimal,
            WindowMethod.AverageWithNullableDouble,
            WindowMethod.AverageWithNullableInt32,
            WindowMethod.AverageWithNullableInt64,
            WindowMethod.AverageWithNullableSingle,
            WindowMethod.AverageWithSingle,
            WindowMethod.First,
            WindowMethod.Last,
            WindowMethod.Locf,
            WindowMethod.Max,
            WindowMethod.Min,
            WindowMethod.Push,
            WindowMethod.StandardDeviationPopulationWithDecimal,
            WindowMethod.StandardDeviationPopulationWithDouble,
            WindowMethod.StandardDeviationPopulationWithInt32,
            WindowMethod.StandardDeviationPopulationWithInt64,
            WindowMethod.StandardDeviationPopulationWithNullableDecimal,
            WindowMethod.StandardDeviationPopulationWithNullableDouble,
            WindowMethod.StandardDeviationPopulationWithNullableInt32,
            WindowMethod.StandardDeviationPopulationWithNullableInt64,
            WindowMethod.StandardDeviationPopulationWithNullableSingle,
            WindowMethod.StandardDeviationPopulationWithSingle,
            WindowMethod.StandardDeviationSampleWithDecimal,
            WindowMethod.StandardDeviationSampleWithDouble,
            WindowMethod.StandardDeviationSampleWithInt32,
            WindowMethod.StandardDeviationSampleWithInt64,
            WindowMethod.StandardDeviationSampleWithNullableDecimal,
            WindowMethod.StandardDeviationSampleWithNullableDouble,
            WindowMethod.StandardDeviationSampleWithNullableInt32,
            WindowMethod.StandardDeviationSampleWithNullableInt64,
            WindowMethod.StandardDeviationSampleWithNullableSingle,
            WindowMethod.StandardDeviationSampleWithSingle,
            WindowMethod.SumWithDecimal,
            WindowMethod.SumWithDouble,
            WindowMethod.SumWithInt32,
            WindowMethod.SumWithInt64,
            WindowMethod.SumWithNullableDecimal,
            WindowMethod.SumWithNullableDouble,
            WindowMethod.SumWithNullableInt32,
            WindowMethod.SumWithNullableInt64,
            WindowMethod.SumWithNullableSingle,
            WindowMethod.SumWithSingle
        };

        private static readonly MethodInfo[] __binaryMethods =
        {
            WindowMethod.CovariancePopulationWithDecimals,
            WindowMethod.CovariancePopulationWithDoubles,
            WindowMethod.CovariancePopulationWithInt32s,
            WindowMethod.CovariancePopulationWithInt64s,
            WindowMethod.CovariancePopulationWithNullableDecimals,
            WindowMethod.CovariancePopulationWithNullableDoubles,
            WindowMethod.CovariancePopulationWithNullableInt32s,
            WindowMethod.CovariancePopulationWithNullableInt64s,
            WindowMethod.CovariancePopulationWithNullableSingles,
            WindowMethod.CovariancePopulationWithSingles,
            WindowMethod.CovarianceSampleWithDecimals,
            WindowMethod.CovarianceSampleWithDoubles,
            WindowMethod.CovarianceSampleWithInt32s,
            WindowMethod.CovarianceSampleWithInt64s,
            WindowMethod.CovarianceSampleWithNullableDecimals,
            WindowMethod.CovarianceSampleWithNullableDoubles,
            WindowMethod.CovarianceSampleWithNullableInt32s,
            WindowMethod.CovarianceSampleWithNullableInt64s,
            WindowMethod.CovarianceSampleWithNullableSingles,
            WindowMethod.CovarianceSampleWithSingles
        };

        private static readonly MethodInfo[] __derivativeOrIntegralMethods =
        {
            WindowMethod.DerivativeWithDecimal,
            WindowMethod.DerivativeWithDecimalAndUnit,
            WindowMethod.DerivativeWithDouble,
            WindowMethod.DerivativeWithDoubleAndUnit,
            WindowMethod.DerivativeWithInt32,
            WindowMethod.DerivativeWithInt32AndUnit,
            WindowMethod.DerivativeWithInt64,
            WindowMethod.DerivativeWithInt64AndUnit,
            WindowMethod.DerivativeWithSingle,
            WindowMethod.DerivativeWithSingleAndUnit,
            WindowMethod.IntegralWithDecimal,
            WindowMethod.IntegralWithDecimalAndUnit,
            WindowMethod.IntegralWithDouble,
            WindowMethod.IntegralWithDoubleAndUnit,
            WindowMethod.IntegralWithInt32,
            WindowMethod.IntegralWithInt32AndUnit,
            WindowMethod.IntegralWithInt64,
            WindowMethod.IntegralWithInt64AndUnit,
            WindowMethod.IntegralWithSingle,
            WindowMethod.IntegralWithSingleAndUnit
        };

        private static readonly MethodInfo[] __exponentialMovingAverageMethods =
        {
            WindowMethod.ExponentialMovingAverageWithDecimal,
            WindowMethod.ExponentialMovingAverageWithDouble,
            WindowMethod.ExponentialMovingAverageWithInt32,
            WindowMethod.ExponentialMovingAverageWithInt64,
            WindowMethod.ExponentialMovingAverageWithSingle
        };

        private static readonly MethodInfo[] __shiftMethods =
        {
            WindowMethod.Shift,
            WindowMethod.ShiftWithDefaultValue
        };

        public static bool CanTranslate(MethodCallExpression expression)
        {
            return expression.Method.IsOneOf(__windowMethods);
        }

        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var parameters = method.GetParameters();
            var arguments = expression.Arguments.ToArray();

            if (method.IsOneOf(__windowMethods))
            {
                var partitionExpression = arguments[0];
                var partitionTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, partitionExpression);
                var partitionSerializer = (ISetWindowFieldsPartitionSerializer)partitionTranslation.Serializer;
                var inputSerializer = partitionSerializer.InputSerializer;

                AstWindow window = null;
                if (HasArgument<Expression>(parameters, "window", arguments, out var windowExpression))
                {
                    window = TranslateWindow(context, expression, windowExpression, inputSerializer);
                }

                if (method.IsOneOf(__nullaryMethods))
                {
                    var @operator = GetNullaryWindowOperator(method);
                    var ast = AstExpression.NullaryWindowExpression(@operator, window);
                    var serializer = BsonSerializer.LookupSerializer(method.ReturnType); // TODO: use correct serializer
                    return new AggregationExpression(expression, ast, serializer);
                }

                AggregationExpression selectorTranslation = null;
                if (HasArgument<LambdaExpression>(parameters, "selector", arguments, out var selectorLambda))
                {
                    selectorTranslation = TranslateSelector(context, selectorLambda, inputSerializer);
                }

                if (method.IsOneOf(__unaryMethods))
                {
                    ThrowIfSelectorTranslationIsNull(selectorTranslation);
                    var @operator = GetUnaryWindowOperator(method);
                    var ast = AstExpression.UnaryWindowExpression(@operator, selectorTranslation.Ast, window);
                    var serializer = BsonSerializer.LookupSerializer(method.ReturnType); // TODO: use correct serializer
                    return new AggregationExpression(expression, ast, serializer);
                }

                if (method.IsOneOf(__binaryMethods))
                {
                    var selector1Lambda = GetArgument<LambdaExpression>(parameters, "selector1", arguments);
                    var selector2Lambda = GetArgument<LambdaExpression>(parameters, "selector2", arguments);
                    var selector1Translation = TranslateSelector(context, selector1Lambda, inputSerializer);
                    var selector2Translation = TranslateSelector(context, selector2Lambda, inputSerializer);

                    var @operator = GetBinaryWindowOperator(method);
                    var ast = AstExpression.BinaryWindowExpression(@operator, selector1Translation.Ast, selector2Translation.Ast, window);
                    var serializer = BsonSerializer.LookupSerializer(method.ReturnType); // TODO: use correct serializer
                    return new AggregationExpression(expression, ast, serializer);
                }

                if (method.IsOneOf(__derivativeOrIntegralMethods))
                {
                    ThrowIfSelectorTranslationIsNull(selectorTranslation);
                    WindowTimeUnit? unit = default;
                    if (HasArgument<Expression>(parameters, "unit", arguments, out var unitExpression))
                    {
                        unit = unitExpression.GetConstantValue<WindowTimeUnit>(expression);
                    }

                    var @operator = GetDerivativeOrIntegralWindowOperator(method);
                    var ast = AstExpression.DerivativeOrIntegralWindowExpression(@operator, selectorTranslation.Ast, unit, window);
                    var serializer = BsonSerializer.LookupSerializer(method.ReturnType); // TODO: use correct serializer
                    return new AggregationExpression(expression, ast, serializer);
                }

                if (method.IsOneOf(__exponentialMovingAverageMethods))
                {
                    ThrowIfSelectorTranslationIsNull(selectorTranslation);
                    var weightingExpression = arguments[2];
                    var weighting = weightingExpression.GetConstantValue<ExponentialMovingAverageWeighting>(expression);

                    var ast = AstExpression.ExponentialMovingAverageWindowExpression(selectorTranslation.Ast, weighting, window);
                    var serializer = BsonSerializer.LookupSerializer(method.ReturnType); // TODO: use correct serializer
                    return new AggregationExpression(expression, ast, serializer);
                }

                if (method.IsOneOf(__shiftMethods))
                {
                    ThrowIfSelectorTranslationIsNull(selectorTranslation);
                    var byExpression = arguments[2];
                    var by = byExpression.GetConstantValue<int>(expression);

                    AstExpression defaultValue = null;
                    if (method.Is(WindowMethod.ShiftWithDefaultValue))
                    {
                        var defaultValueExpression = arguments[3];
                        var defaultValueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, defaultValueExpression);
                        defaultValue = defaultValueTranslation.Ast;
                    }

                    var ast = AstExpression.ShiftWindowExpression(selectorTranslation.Ast, by, defaultValue);
                    var serializer = BsonSerializer.LookupSerializer(method.ReturnType); // TODO: use correct serializer
                    return new AggregationExpression(expression, ast, serializer);
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static TExpression GetArgument<TExpression>(ParameterInfo[] parameters, string parameterName, Expression[] arguments)
            where TExpression : Expression
        {
            if (HasArgument<TExpression>(parameters, parameterName, arguments, out TExpression argument))
            {
                return argument;
            }

            throw new InvalidOperationException($"There is no parameter named {parameterName}.");
        }

        public static AstBinaryWindowOperator GetBinaryWindowOperator(MethodInfo method)
        {
            return method.Name switch
            {
                "CovariancePopulation" => AstBinaryWindowOperator.CovariancePopulation,
                "CovarianceSample" => AstBinaryWindowOperator.CovarianceSample,
                _ => throw new InvalidOperationException($"Invalid method name: {method.Name}.")
            };
        }

        public static AstDerivativeOrIntegralWindowOperator GetDerivativeOrIntegralWindowOperator(MethodInfo method)
        {
            return method.Name switch
            {
                "Derivative" => AstDerivativeOrIntegralWindowOperator.Derivative,
                "Integral" => AstDerivativeOrIntegralWindowOperator.Integral,
                _ => throw new InvalidOperationException($"Invalid method name: {method.Name}.")
            };
        }

        public static AstNullaryWindowOperator GetNullaryWindowOperator(MethodInfo method)
        {
            return method.Name switch
            {
                "Count" => AstNullaryWindowOperator.Count,
                "DenseRank" => AstNullaryWindowOperator.DenseRank,
                "DocumentNumber" => AstNullaryWindowOperator.DocumentNumber,
                "Rank" => AstNullaryWindowOperator.Rank,
                _ => throw new InvalidOperationException($"Invalid method name: {method.Name}.")
            };
        }

        public static AstUnaryWindowOperator GetUnaryWindowOperator(MethodInfo method)
        {
            return method.Name switch
            {
                "AddToSet" => AstUnaryWindowOperator.AddToSet,
                "Average" => AstUnaryWindowOperator.Average,
                "First" => AstUnaryWindowOperator.First,
                "Last" => AstUnaryWindowOperator.Last,
                "Locf" => AstUnaryWindowOperator.Locf,
                "Max" => AstUnaryWindowOperator.Max,
                "Min" => AstUnaryWindowOperator.Min,
                "Push" => AstUnaryWindowOperator.Push,
                "StandardDeviationPopulation" => AstUnaryWindowOperator.StandardDeviationPopulation,
                "StandardDeviationSample" => AstUnaryWindowOperator.StandardDeviationSample,
                "Sum" => AstUnaryWindowOperator.Sum,
                _ => throw new InvalidOperationException($"Invalid method name: {method.Name}.")
            };
        }

        private static bool HasArgument<TExpression>(ParameterInfo[] parameters, string parameterName, Expression[] arguments, out TExpression argument)
            where TExpression : Expression
        {
            for (var i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Name == parameterName)
                {
                    argument = (TExpression)arguments[i];
                    return true;
                }
            }

            argument = null;
            return false;
        }

        private static void ThrowIfSelectorTranslationIsNull(AggregationExpression selectTranslation)
        {
            if (selectTranslation == null)
            {
                throw new Exception("selectorTranslation is unexpectedly null.");
            }
        }

        private static AggregationExpression TranslateSelector(TranslationContext context, LambdaExpression selectorLambda, IBsonSerializer inputSerializer)
        {
            var selectorParameter = selectorLambda.Parameters[0];
            var selectorSymbol = context.CreateSymbol(selectorParameter, inputSerializer, isCurrent: true);
            var selectorContext = context.WithSymbol(selectorSymbol);
            return ExpressionToAggregationExpressionTranslator.Translate(selectorContext, selectorLambda.Body);
        }

        private static AstWindow TranslateWindow(TranslationContext context, Expression expression, Expression windowExpression, IBsonSerializer inputSerializer)
        {
            var windowConstant = windowExpression.GetConstantValue<SetWindowFieldsWindow>(expression);
            var sortBy = context.Data?.GetValueOrDefault<object>("SortBy", null);
            var serializerRegistry = context.Data?.GetValueOrDefault<BsonSerializerRegistry>("SerializerRegistry", null);
            return ToAstWindow(windowConstant, sortBy, inputSerializer, serializerRegistry, context.TranslationOptions);
        }

        private static AstWindow ToAstWindow(
            SetWindowFieldsWindow window,
            object sortBy,
            IBsonSerializer inputSerializer,
            BsonSerializerRegistry serializerRegistry,
            ExpressionTranslationOptions translationOptions)
        {
            if (window == null)
            {
                return null;
            }

            if (window is DocumentsWindow documentsWindow)
            {
                var lowerBoundary = documentsWindow.LowerBoundary;
                var upperBoundary = documentsWindow.UpperBoundary;
                return new AstWindow("documents", lowerBoundary.Render(), upperBoundary.Render(), unit: null);
            }

            if (window is RangeWindow rangeWindow)
            {
                var lowerBoundary = rangeWindow.LowerBoundary;
                var upperBoundary = rangeWindow.UpperBoundary;
                var unit = (lowerBoundary as TimeRangeWindowBoundary)?.Unit ?? (upperBoundary as TimeRangeWindowBoundary)?.Unit;

                var lowerValueBoundary = lowerBoundary as ValueRangeWindowBoundary;
                var upperValueBoundary = upperBoundary as ValueRangeWindowBoundary;
                IBsonSerializer lowerBoundaryValueSerializer = null;
                IBsonSerializer upperBoundaryValueSerializer = null;
                if (lowerValueBoundary != null || upperValueBoundary != null)
                {
                    var sortBySerializer = GetSortBySerializer(sortBy, inputSerializer, serializerRegistry, translationOptions);
                    if (lowerValueBoundary != null)
                    {
                        lowerBoundaryValueSerializer = ValueRangeWindowBoundaryConvertingValueSerializerFactory.Create(lowerValueBoundary, sortBySerializer);
                    }
                    if (upperValueBoundary != null)
                    {
                        if (lowerBoundaryValueSerializer != null && lowerBoundaryValueSerializer.ValueType == upperValueBoundary.ValueType)
                        {
                            upperBoundaryValueSerializer = lowerBoundaryValueSerializer;
                        }
                        else
                        {
                            upperBoundaryValueSerializer = ValueRangeWindowBoundaryConvertingValueSerializerFactory.Create(upperValueBoundary, sortBySerializer);
                        }
                    }
                }

                return new AstWindow("range", lowerBoundary.Render(lowerBoundaryValueSerializer), upperBoundary.Render(upperBoundaryValueSerializer), unit);
            }

            throw new ArgumentException($"Invalid window type: {window.GetType().FullName}.");
        }

        private static IBsonSerializer GetSortBySerializer(
            object sortBy,
            IBsonSerializer inputSerializer,
            BsonSerializerRegistry serializerRegistry,
            ExpressionTranslationOptions translationOptions)
        {
            Ensure.IsNotNull(sortBy, nameof(sortBy));

            // use reflection to call GetSortBySerializerGeneric because we don't know TDocument until runtime
            var sortByType = sortBy.GetType();
            var documentType = sortByType.GetGenericArguments().Single();
            var methodInfoDefinition = typeof(WindowMethodToAggregationExpressionTranslator).GetMethod(
                nameof(GetSortBySerializerGeneric),
                BindingFlags.NonPublic | BindingFlags.Static);
            var methodInfo = methodInfoDefinition.MakeGenericMethod(documentType);
            return (IBsonSerializer)methodInfo.Invoke(null, new object[] { sortBy, inputSerializer, serializerRegistry, translationOptions });
        }

        private static IBsonSerializer GetSortBySerializerGeneric<TDocument>(
            SortDefinition<TDocument> sortBy,
            IBsonSerializer<TDocument> documentSerializer,
            BsonSerializerRegistry serializerRegistry,
            ExpressionTranslationOptions translationOptions)
        {
            var directionalSortBy = sortBy as DirectionalSortDefinition<TDocument>;
            if (directionalSortBy == null)
            {
                throw new InvalidOperationException("SetWindowFields SortBy with range window must be on a single field.");
            }
            if (directionalSortBy.Direction != SortDirection.Ascending)
            {
                throw new InvalidOperationException("SetWindowFields SortBy with range window must be ascending.");
            }

            var field = directionalSortBy.Field;
            var renderedField = field.Render(new(documentSerializer, serializerRegistry, translationOptions: translationOptions));

            return renderedField.FieldSerializer;
        }
    }
}
