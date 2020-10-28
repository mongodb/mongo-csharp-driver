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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Ast.Stages;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Serializers;
using MongoDB.Driver.Linq3.Translators.ExpressionTranslators;

namespace MongoDB.Driver.Linq3.Translators.PipelineTranslators
{
    public static class GroupByStageTranslator
    {
        // private static fields
        private static readonly MethodInfo[] __groupByMethods;

        // static constructor
        static GroupByStageTranslator()
        {
            __groupByMethods = new[]
            {
                QueryableMethod.GroupByWithKeySelector,
                QueryableMethod.GroupByWithKeySelectorAndElementSelector,
                QueryableMethod.GroupByWithKeySelectorAndResultSelector,
                QueryableMethod.GroupByWithKeySelectorElementSelectorAndResultSelector
            };
        }

        // public static methods
        public static TranslatedPipeline Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__groupByMethods))
            {
                var sourceExpression = arguments[0];
                var pipeline = PipelineTranslator.Translate(context, sourceExpression);

                var keySelectorLambdaExpression = ExpressionHelper.Unquote(arguments[1]);
                var keySelectorContext = context.WithSymbolAsCurrent(keySelectorLambdaExpression.Parameters[0], new Symbol("$ROOT", pipeline.OutputSerializer));
                var keySelectorTranslation = ExpressionTranslator.Translate(keySelectorContext, keySelectorLambdaExpression.Body);
                var keySerializer = keySelectorTranslation.Serializer;

                if (method.Is(QueryableMethod.GroupByWithKeySelector))
                {
                    var elementSerializer = pipeline.OutputSerializer;
                    var groupingSerializer = IGroupingSerializer.Create(keySerializer, elementSerializer);

                    pipeline.AddStages(
                        groupingSerializer,
                        new AstGroupStage(
                            keySelectorTranslation.Ast,
                            new[] { new AstComputedField("_elements", new AstUnaryExpression(AstUnaryOperator.Push, new AstFieldExpression("$$ROOT"))) }));

                    return pipeline;
                }

                if (method.Is(QueryableMethod.GroupByWithKeySelectorAndElementSelector))
                {
                    var elementSelectorLambdaExpression = ExpressionHelper.Unquote(arguments[2]);
                    var elementSelectorContext = context.WithSymbolAsCurrent(elementSelectorLambdaExpression.Parameters[0], new Symbol("$ROOT", pipeline.OutputSerializer));
                    var elementSelectorTranslation = ExpressionTranslator.Translate(elementSelectorContext, elementSelectorLambdaExpression.Body);
                    var elementSerializer = elementSelectorTranslation.Serializer;
                    var groupingSerializer = IGroupingSerializer.Create(keySerializer, elementSerializer);

                    pipeline.AddStages(
                        groupingSerializer,
                        new AstGroupStage(
                            keySelectorTranslation.Ast,
                            new[] { new AstComputedField("_elements", new AstUnaryExpression(AstUnaryOperator.Push, elementSelectorTranslation.Ast)) }));

                    return pipeline;
                }

                if (method.Is(QueryableMethod.GroupByWithKeySelectorAndResultSelector))
                {
                    var keyValueSerializer = AddKeyValueStage(context, pipeline, keySelectorTranslation);

                    var resultSelectorLambdaExpression = ExpressionHelper.Unquote(arguments[2]);
                    var keyParameter = resultSelectorLambdaExpression.Parameters[0];
                    var accumulatorFields = TranslateAccumulatorFields(context, resultSelectorLambdaExpression, keyParameter, keyValueSerializer, out var outputSerializer);

                    pipeline.AddStages(
                        outputSerializer,
                        new AstGroupStage(
                            id: new AstFieldExpression("$_key"),
                            accumulatorFields),
                        new AstProjectStage(new AstProjectStageExcludeIdSpecification()));

                    return pipeline;
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        public static TranslatedPipeline TranslateGroupByAndSelectTogether(TranslationContext context, MethodCallExpression groupByExpression, MethodCallExpression selectExpression)
        {
            if (groupByExpression.Method.Is(QueryableMethod.GroupByWithKeySelector) && selectExpression.Method.Is(QueryableMethod.Select))
            {
                var sourceExpression = groupByExpression.Arguments[0];
                var pipeline = PipelineTranslator.Translate(context, sourceExpression);

                var keySelectorLambdaExpression = ExpressionHelper.Unquote(groupByExpression.Arguments[1]);
                var keyValueSerializer = AddKeyValueStage(context, pipeline, keySelectorLambdaExpression);

                var selectorLambdaExpression = ExpressionHelper.Unquote(selectExpression.Arguments[1]);
                var accumulatorFields = TranslateAccumulatorFields(context, selectorLambdaExpression, keyParameterExpression: null, keyValueSerializer, out var outputSerializer);

                pipeline.AddStages(
                    outputSerializer,
                    new AstGroupStage(
                        id: new AstFieldExpression("$_key"),
                        accumulatorFields),
                    new AstProjectStage(new AstProjectStageExcludeIdSpecification()));

                return pipeline;
            }

            throw new ExpressionNotSupportedException(selectExpression);
        }

        // private methods
        private static IGroupByKeyValueSerializer AddKeyValueStage(TranslationContext context, TranslatedPipeline pipeline, LambdaExpression keySelectorLambda)
        {
            var keySelectorTranslation = ExpressionTranslator.Translate(context, keySelectorLambda, pipeline.OutputSerializer);
            return AddKeyValueStage(context, pipeline, keySelectorTranslation);
        }

        private static IGroupByKeyValueSerializer AddKeyValueStage(TranslationContext context, TranslatedPipeline pipeline, ExpressionTranslation keySelectorTranslation)
        {
            var valueAst = new AstFieldExpression("$$ROOT");
            var valueSerializer = pipeline.OutputSerializer;
            if (valueSerializer is IWrappedValueSerializer wrappedValueSerializer)
            {
                valueAst = new AstFieldExpression("$_v");
                valueSerializer = wrappedValueSerializer.ValueSerializer;
            }

            var groupByKeyValueSerializer = GroupByKeyValueSerializer.Create(keySelectorTranslation.Serializer, valueSerializer);
            pipeline.AddStages(
                groupByKeyValueSerializer,
                new AstProjectStage(
                    new AstProjectStageComputedFieldSpecification(new AstComputedField("_key", keySelectorTranslation.Ast)),
                    new AstProjectStageComputedFieldSpecification(new AstComputedField("_v", valueAst)),
                    new AstProjectStageExcludeIdSpecification()));

            return groupByKeyValueSerializer;
        }

        private static List<AstComputedField> TranslateAccumulatorFields(TranslationContext context, LambdaExpression selectorLambda, ParameterExpression keyParameterExpression, IGroupByKeyValueSerializer keyValueSerializer, out IBsonSerializer outputSerializer)
        {
            var body = selectorLambda.Body;

            if (body is NewExpression newExpression)
            {
                return TranslateSelectorNewExpression(context, newExpression, keyParameterExpression, keyValueSerializer, out outputSerializer);
            }

            throw new ExpressionNotSupportedException(selectorLambda);
        }

        private static List<AstComputedField> TranslateSelectorNewExpression(TranslationContext context, NewExpression newExpression, ParameterExpression keyParameterExpression, IGroupByKeyValueSerializer keyValueSerializer, out IBsonSerializer outputSerializer)
        {
            var accumulatorFields = new List<AstComputedField>();
            var classMap = new BsonClassMap(newExpression.Type);

            for (var i = 0; i < newExpression.Members.Count; i++)
            {
                var member = newExpression.Members[i];
                var accumulatorExpression = newExpression.Arguments[i];
                var accumulatorTranslation = TranslateAccumulatorExpression(context, accumulatorExpression, keyParameterExpression, keyValueSerializer);
                var accumulatorComputedField = new AstComputedField(member.Name, accumulatorTranslation.Ast);
                accumulatorFields.Add(accumulatorComputedField);
                classMap.MapMember(member).SetSerializer(accumulatorTranslation.Serializer);
            }
            classMap.Freeze();

            var outputSerializerType = typeof(BsonClassMapSerializer<>).MakeGenericType(newExpression.Type);
            outputSerializer = (IBsonSerializer)Activator.CreateInstance(outputSerializerType, classMap);

            return accumulatorFields;
        }

        private static ExpressionTranslation TranslateAccumulatorExpression(TranslationContext context, Expression accumulatorExpression, ParameterExpression keyParameterExpression, IGroupByKeyValueSerializer keyValueSerializer)
        {
            if (accumulatorExpression == keyParameterExpression)
            {
                var ast = new AstUnaryExpression(AstUnaryOperator.First, new AstFieldExpression("$_key"));
                return new ExpressionTranslation(accumulatorExpression, ast, keyValueSerializer.KeySerializer);
            }

            if (accumulatorExpression is MemberExpression memberExpression)
            {
                var memberDeclaringType = memberExpression.Member.DeclaringType;
                if (memberDeclaringType.IsConstructedGenericType && memberDeclaringType.GetGenericTypeDefinition() == typeof(IGrouping<,>))
                {
                    if (memberExpression.Member.Name == "Key")
                    {
                        var ast = new AstUnaryExpression(AstUnaryOperator.First, new AstFieldExpression("$_key"));
                        return new ExpressionTranslation(accumulatorExpression, ast, keyValueSerializer.KeySerializer);
                    }
                }
            }

            if (accumulatorExpression is MethodCallExpression methodCallExpression)
            {
                var method = methodCallExpression.Method;
                var arguments = methodCallExpression.Arguments;

                var sourceExpression = arguments[0];
                var sourceType = sourceExpression.Type;
                if (sourceType.IsConstructedGenericType)
                {
                    var sourceTypeDefinition = sourceType.GetGenericTypeDefinition();
                    if (sourceTypeDefinition == typeof(IEnumerable<>) || sourceTypeDefinition == typeof(IGrouping<,>))
                    {
                        if (method.Is(EnumerableMethod.Count))
                        {
                            var ast = new AstUnaryExpression(AstUnaryOperator.Sum, 1);
                            var serializer = new Int32Serializer();
                            return new ExpressionTranslation(accumulatorExpression, ast, serializer);
                        }

                        if (method.Name == "Min" && method.DeclaringType == typeof(Enumerable))
                        {
                            var selectorLambda = (LambdaExpression)arguments[1];
                            var wrappedValueSerializer = WrappedValueSerializer.Create(keyValueSerializer.ValueSerializer);
                            var selectorTranslation = ExpressionTranslator.Translate(context, selectorLambda, wrappedValueSerializer);
                            var minAst = new AstUnaryExpression(AstUnaryOperator.Min, selectorTranslation.Ast);
                            return new ExpressionTranslation(accumulatorExpression, minAst, selectorTranslation.Serializer);
                        }
                    }
                }
            }

            throw new ExpressionNotSupportedException(accumulatorExpression);
        }
    }
}
