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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class PickMethodToAggregationExpressionTranslator
    {
        private static readonly MethodInfo[] __pickMethods = new[]
        {
            EnumerableMethod.Bottom,
            EnumerableMethod.BottomN,
            EnumerableMethod.BottomNWithComputedN,
            EnumerableMethod.FirstN,
            EnumerableMethod.FirstNWithComputedN,
            EnumerableMethod.LastN,
            EnumerableMethod.LastNWithComputedN,
            EnumerableMethod.MaxN,
            EnumerableMethod.MaxNWithComputedN,
            EnumerableMethod.MinN,
            EnumerableMethod.MinNWithComputedN,
            EnumerableMethod.Top,
            EnumerableMethod.TopN,
            EnumerableMethod.TopNWithComputedN
        };

        private static readonly MethodInfo[] __withNMethods = new[]
        {
            EnumerableMethod.BottomN,
            EnumerableMethod.FirstN,
            EnumerableMethod.LastN,
            EnumerableMethod.MaxN,
            EnumerableMethod.MinN,
            EnumerableMethod.TopN
        };

        private static readonly MethodInfo[] __withComputedNMethods = new[]
        {
            EnumerableMethod.BottomNWithComputedN,
            EnumerableMethod.FirstNWithComputedN,
            EnumerableMethod.LastNWithComputedN,
            EnumerableMethod.MaxNWithComputedN,
            EnumerableMethod.MinNWithComputedN,
            EnumerableMethod.TopNWithComputedN
        };

        private static readonly MethodInfo[] __withSortByMethods = new[]
        {
            EnumerableMethod.Bottom,
            EnumerableMethod.BottomN,
            EnumerableMethod.BottomNWithComputedN,
            EnumerableMethod.Top,
            EnumerableMethod.TopN,
            EnumerableMethod.TopNWithComputedN
        };

        private static readonly MethodInfo[] __accumulatorOnlyMethods = new[]
        {
            EnumerableMethod.Bottom,
            EnumerableMethod.BottomN,
            EnumerableMethod.BottomNWithComputedN,
            EnumerableMethod.FirstNWithComputedN,
            EnumerableMethod.LastNWithComputedN,
            EnumerableMethod.MaxNWithComputedN,
            EnumerableMethod.MinNWithComputedN,
            EnumerableMethod.Top,
            EnumerableMethod.TopN,
            EnumerableMethod.TopNWithComputedN
        };

        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments.ToArray();

            if (method.IsOneOf(__pickMethods))
            {
                var sourceExpression = arguments[0];
                var sourceTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, sourceExpression);
                NestedAsQueryableHelper.EnsureQueryableMethodHasNestedAsQueryableSource(expression, sourceTranslation);
                var itemSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);

                if (method.IsOneOf(__accumulatorOnlyMethods) && !IsGroupingSource(sourceTranslation.Ast))
                {
                    throw new ExpressionNotSupportedException(expression, because: $"{method.Name} can only be used as an accumulator with GroupBy");
                }

                AstSortFields sortBy = null;
                if (method.IsOneOf(__withSortByMethods))
                {
                    var sortByExpression = arguments[1];
                    var sortByDefinition = GetSortByDefinition(sortByExpression, expression);
                    sortBy = TranslateSortByDefinition(context, expression, sortByExpression, sortByDefinition, itemSerializer);
                }

                var selectorLambda = (LambdaExpression)GetSelectorArgument(method, arguments);
                var selectorParameter = selectorLambda.Parameters.Single();
                var selectorParameterSymbol = context.CreateSymbol(selectorParameter, itemSerializer);
                var selectorContext = context.WithSymbol(selectorParameterSymbol);
                var selectorTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(selectorContext, selectorLambda, itemSerializer, asRoot: false);

                TranslatedExpression nTranslation = null;
                IBsonSerializer resultSerializer;
                if (method.IsOneOf(__withNMethods, __withComputedNMethods))
                {
                    var nExpression = arguments.Last();
                    if (method.IsOneOf(__withNMethods))
                    {
                        nTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, nExpression);
                    }
                    else
                    {
                        var keyExpression = arguments[arguments.Length - 2];
                        var keyTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, keyExpression);
                        if (!IsValidKey(keyTranslation))
                        {
                            throw new ExpressionNotSupportedException(keyExpression, expression, because: "key must be a reference the _id field");
                        }

                        var nLambda = (LambdaExpression)nExpression;
                        var keyParameter = nLambda.Parameters.Single();
                        var keySymbol = context.CreateSymbol(keyParameter, keyTranslation.Serializer, isCurrent: true);
                        var nContext = context.WithSingleSymbol(keySymbol);
                        nTranslation = ExpressionToAggregationExpressionTranslator.Translate(nContext, nLambda.Body);
                    }
                    resultSerializer = IEnumerableSerializer.Create(selectorTranslation.Serializer);
                }
                else
                {
                    resultSerializer = selectorTranslation.Serializer;
                }

                AstPickOperator @operator;
                var sourceAst = sourceTranslation.Ast;
                var @as = selectorParameterSymbol.Var;
                var selectorAst = selectorTranslation.Ast;
                if (IsGroupingSource(sourceTranslation.Ast))
                {
                    @operator = GetPlaceholderOperator(method);
                }
                else
                {
                    @operator = GetArrayOperator(method);
                    sourceAst = AstExpression.Map(
                        input: sourceAst,
                        @as: selectorParameterSymbol.Var,
                        @in: selectorAst);
                    @as = null;
                    selectorAst = null;
                }

                var ast = AstExpression.PickExpression(
                    @operator,
                    sourceAst,
                    sortBy,
                    @as,
                    selectorAst,
                    nTranslation?.Ast);

                return new TranslatedExpression(expression, ast, resultSerializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static AstPickOperator GetPlaceholderOperator(MethodInfo method)
        {
            return method.Name switch
            {
                "Bottom" => AstPickOperator.BottomPlaceholder,
                "BottomN" => AstPickOperator.BottomNPlaceholder,
                "FirstN" => AstPickOperator.FirstNPlaceholder,
                "LastN" => AstPickOperator.LastNPlaceholder,
                "MaxN" => AstPickOperator.MaxNPlaceholder,
                "MinN" => AstPickOperator.MinNPlaceholder,
                "Top" => AstPickOperator.TopPlaceholder,
                "TopN" => AstPickOperator.TopNPlaceholder,
                _ => throw new InvalidOperationException($"Invalid method name: {method.Name}.")
            };
        }

        private static AstPickOperator GetArrayOperator(MethodInfo method)
        {
            return method.Name switch
            {
                "FirstN" => AstPickOperator.FirstNArray,
                "LastN" => AstPickOperator.LastNArray,
                "MaxN" => AstPickOperator.MaxNArray,
                "MinN" => AstPickOperator.MinNArray,
                _ => throw new InvalidOperationException($"Invalid method name: {method.Name}.")
            };
        }

        private static Expression GetSelectorArgument(MethodInfo method, Expression[] arguments)
        {
            switch (method.Name)
            {
                case "FirstN":
                case "LastN":
                case "MaxN":
                case "MinN":
                    return arguments[1];

                case "Bottom":
                case "BottomN":
                case "Top":
                case "TopN":
                    return arguments[2];

                default:
                    throw new InvalidOperationException($"Method {method.Name} does not have a sortBy argument.");
            }
        }

        private static object GetSortByDefinition(Expression sortByExpression, Expression expression)
        {
            if (sortByExpression.NodeType == ExpressionType.Constant)
            {
                return sortByExpression.GetConstantValue<object>(sortByExpression);
            }

            // we get here when the PartialEvaluator couldn't fully evalute the SortDefinition
            try
            {
                LambdaExpression lambda = Expression.Lambda(sortByExpression);
                Delegate @delegate = lambda.Compile();
                return @delegate.DynamicInvoke(null);
            }
            catch (Exception ex)
            {
                throw new ExpressionNotSupportedException(sortByExpression, expression, because: $"attempting to evaluate the sortBy expression failed: {ex.Message}");
            }
        }

        private static bool IsGroupingSource(AstExpression source)
        {
            return
                source is AstGetFieldExpression getFieldExpression &&
                getFieldExpression.Input.IsRootVar() &&
                getFieldExpression.FieldName.IsStringConstant("_elements");
        }

        private static bool IsValidKey(TranslatedExpression keyTranslation)
        {
            return
                keyTranslation.Ast is AstGetFieldExpression getFieldExpression &&
                getFieldExpression.Input.IsRootVar() &&
                getFieldExpression.FieldName.IsStringConstant("_id");
        }

        private static AstSortFields TranslateSortByDefinition(
            TranslationContext context,
            Expression expression,
            Expression sortByExpression,
            object sortByDefinition,
            IBsonSerializer documentSerializer)
        {
            var methodInfoDefinition = typeof(PickMethodToAggregationExpressionTranslator).GetMethod(nameof(TranslateSortByDefinitionGeneric), BindingFlags.Static | BindingFlags.NonPublic);
            var documentType = documentSerializer.ValueType;
            var methodInfo = methodInfoDefinition.MakeGenericMethod(documentType);
            return (AstSortFields)methodInfo.Invoke(null, new object[] { context, expression, sortByExpression, sortByDefinition, documentSerializer });
        }

        private static AstSortFields TranslateSortByDefinitionGeneric<TDocument>(
            TranslationContext context,
            Expression expression,
            Expression sortByExpression,
            SortDefinition<TDocument> sortByDefinition,
            IBsonSerializer<TDocument> documentSerializer)
        {
            var serializerRegistry = context.SerializationDomain.SerializerRegistry;
            var sortDocument = sortByDefinition.Render(new(documentSerializer, serializerRegistry, translationOptions: context.TranslationOptions));
            var fields = new List<AstSortField>();
            foreach (var element in sortDocument)
            {
                var order = element.Value switch
                {
                    BsonInt32 v when v.Value == 1 => AstSortOrder.Ascending,
                    BsonInt32 v when v.Value == -1 => AstSortOrder.Descending,
                    BsonString s when element.Name == "$meta" && s.Value == "textScore" => AstSortOrder.MetaTextScore,
                    _ => throw new ExpressionNotSupportedException(sortByExpression, expression, because: $"sort order is not supported: {{ {element.Name} : {element.Value} }}")
                };
                var field = AstSort.Field(element.Name, order);
                fields.Add(field);
            }

            return new AstSortFields(fields);
        }
    }
}
