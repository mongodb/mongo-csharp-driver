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

using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators
{
    internal static class SelectManyMethodToPipelineTranslator
    {
        // private static fields
        private static readonly MethodInfo[] __selectManyMethods;

        // static constructor
        static SelectManyMethodToPipelineTranslator()
        {
            __selectManyMethods = new[]
            {
                QueryableMethod.SelectMany,
                QueryableMethod.SelectManyWithCollectionSelectorAndResultSelector
            };
        }

        // public static methods
        public static AstPipeline Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__selectManyMethods))
            {
                var sourceExpression = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, sourceExpression);

                if (method.Is(QueryableMethod.SelectMany))
                {
                    return TranslateSelectMany(context, pipeline, arguments);
                }

                if (method.Is(QueryableMethod.SelectManyWithCollectionSelectorAndResultSelector))
                {
                    return TranslateSelectManyWithCollectionSelectorAndResultSelector(context, pipeline, arguments);
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static AstPipeline TranslateSelectMany(
            TranslationContext context,
            AstPipeline pipeline,
            ReadOnlyCollection<Expression> arguments)
        {
            var sourceSerializer = pipeline.OutputSerializer;
            var selectorLambda = ExpressionHelper.UnquoteLambda(arguments[1]);
            var selectorTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, selectorLambda, sourceSerializer, asRoot: true);
            var resultValueSerializer = ArraySerializerHelper.GetItemSerializer(selectorTranslation.Serializer);
            var resultWrappedValueSerializer = WrappedValueSerializer.Create("_v", resultValueSerializer);

            pipeline = pipeline.AddStages(
                resultWrappedValueSerializer,
                AstStage.Project(
                    AstProject.Set("_v", selectorTranslation.Ast),
                    AstProject.ExcludeId()),
                AstStage.Unwind("_v"));

            return pipeline;
        }

        private static AstPipeline TranslateSelectManyWithCollectionSelectorAndResultSelector(
            TranslationContext context,
            AstPipeline pipeline,
            ReadOnlyCollection<Expression> arguments)
        {
            var sourceSerializer = pipeline.OutputSerializer;
            var collectionSelectorLambda = ExpressionHelper.UnquoteLambda(arguments[1]);
            var collectionSelectorTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, collectionSelectorLambda, sourceSerializer, asRoot: true);

            var resultSelectorLambda = ExpressionHelper.UnquoteLambda(arguments[2]);
            if (resultSelectorLambda.Body == resultSelectorLambda.Parameters[1])
            {
                // special case identity resultSelector: (x, y) => y
                return TranslateSelectManyWithCollectionSelectorAndIdentityResultSelector(pipeline, collectionSelectorTranslation);
            }
            else
            {
                return TranslateSelectManyWithCollectionSelectorAndNonIdentityResultSelector(context, pipeline, collectionSelectorTranslation, resultSelectorLambda);
            }
        }

        private static AstPipeline TranslateSelectManyWithCollectionSelectorAndIdentityResultSelector(
            AstPipeline pipeline,
            AggregationExpression collectionSelectorTranslation)
        {
            var collectionItemSerializer = ArraySerializerHelper.GetItemSerializer(collectionSelectorTranslation.Serializer);
            var resultValueSerializer = collectionItemSerializer;
            var resultWrappedValueSerializer = WrappedValueSerializer.Create("_v", resultValueSerializer);

            return pipeline.AddStages(
                resultWrappedValueSerializer,
                AstStage.Project(
                    AstProject.Set("_v", collectionSelectorTranslation.Ast),
                    AstProject.ExcludeId()),
                AstStage.Unwind("_v"));
        }

        private static AstPipeline TranslateSelectManyWithCollectionSelectorAndNonIdentityResultSelector(
            TranslationContext context,
            AstPipeline pipeline,
            AggregationExpression collectionSelectorTranslation,
            LambdaExpression resultSelectorLambda)

        {
            var sourceSerializer = pipeline.OutputSerializer;
            var collectionItemSerializer = ArraySerializerHelper.GetItemSerializer(collectionSelectorTranslation.Serializer);

            var resultSelectorSourceParameterExpression = resultSelectorLambda.Parameters[0];
            var resultSelectorCollectionItemParameterExpression = resultSelectorLambda.Parameters[1];
            var resultSelectorSourceParameterSymbol = context.CreateSymbol(resultSelectorSourceParameterExpression, sourceSerializer, isCurrent: true);
            var resultSelectorCollectionItemParameterSymbol = context.CreateSymbol(resultSelectorCollectionItemParameterExpression, collectionItemSerializer);
            var resultSelectorContext = context.WithSymbols(resultSelectorSourceParameterSymbol, resultSelectorCollectionItemParameterSymbol);
            var resultSelectorTranslation = ExpressionToAggregationExpressionTranslator.Translate(resultSelectorContext, resultSelectorLambda.Body);
            var resultValueSerializer = resultSelectorTranslation.Serializer;
            var resultWrappedValueSerializer = WrappedValueSerializer.Create("_v", resultValueSerializer);
            var resultAst = AstExpression.Map(
                input: collectionSelectorTranslation.Ast,
                @as: resultSelectorCollectionItemParameterSymbol.Var,
                @in: resultSelectorTranslation.Ast);

            return pipeline.AddStages(
                resultWrappedValueSerializer,
                AstStage.Project(
                    AstProject.Set("_v", resultAst),
                    AstProject.ExcludeId()),
                AstStage.Unwind("_v"));
        }
    }
}
