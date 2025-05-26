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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators
{
    internal static class ExpressionToExecutableQueryTranslator
    {
        // public static methods
        public static ExecutableQuery<TDocument, IAsyncCursor<TOutput>> Translate<TDocument, TOutput>(
            MongoQueryProvider<TDocument> provider,
            Expression expression,
            ExpressionTranslationOptions translationOptions)
        {
            expression = PartialEvaluator.EvaluatePartially(expression);

            var context = TranslationContext.Create(translationOptions, provider.Collection.Settings.SerializationDomain);
            var pipeline = ExpressionToPipelineTranslator.Translate(context, expression);

            return ExecutableQuery.Create(
                provider,
                pipeline,
                IdentityFinalizer<TOutput>.Instance);
        }

        public static ExecutableQuery<TDocument, TResult> TranslateScalar<TDocument, TResult>(
            MongoQueryProvider<TDocument> provider,
            Expression expression,
            ExpressionTranslationOptions translationOptions)
        {
            expression = PartialEvaluator.EvaluatePartially(expression);

            var context = TranslationContext.Create(translationOptions, provider.Collection.Settings.SerializationDomain);
            var methodCallExpression = (MethodCallExpression)expression;
            switch (methodCallExpression.Method.Name)
            {
                case "All":
                    return AllMethodToExecutableQueryTranslator.Translate(provider, context, methodCallExpression).AsExecutableQuery<TDocument, TResult>();
                case "Any":
                case "AnyAsync":
                    return AnyMethodToExecutableQueryTranslator.Translate(provider, context, methodCallExpression).AsExecutableQuery<TDocument, TResult>();
                case "Average":
                case "AverageAsync":
                    return AverageMethodToExecutableQueryTranslator<TResult>.Translate(provider, context, methodCallExpression);
                case "Contains":
                    return ContainsMethodToExecutableQueryTranslator.Translate(provider, context, methodCallExpression).AsExecutableQuery<TDocument, TResult>();
                case "Count":
                case "CountAsync":
                    return CountMethodToExecutableQueryTranslator.Translate(provider, context, methodCallExpression).AsExecutableQuery<TDocument, TResult>();
                case "ElementAt":
                    return ElementAtMethodToExecutableQueryTranslator<TResult>.Translate(provider, context, methodCallExpression);
                case "First":
                case "FirstAsync":
                case "FirstOrDefault":
                case "FirstOrDefaultAsync":
                    return FirstMethodToExecutableQueryTranslator<TResult>.Translate(provider, context, methodCallExpression);
                case "Last":
                case "LastOrDefault":
                    return LastMethodToExecutableQueryTranslator<TResult>.Translate(provider, context, methodCallExpression);
                case "LongCount":
                case "LongCountAsync":
                    return LongCountMethodToExecutableQueryTranslator.Translate(provider, context, methodCallExpression).AsExecutableQuery<TDocument, TResult>();
                case "Max":
                case "MaxAsync":
                    return MaxMethodToExecutableQueryTranslator<TResult>.Translate(provider, context, methodCallExpression);
                case "Min":
                case "MinAsync":
                    return MinMethodToExecutableQueryTranslator<TResult>.Translate(provider, context, methodCallExpression);
                case "Single":
                case "SingleAsync":
                case "SingleOrDefault":
                case "SingleOrDefaultAsync":
                    return SingleMethodToExecutableQueryTranslator<TResult>.Translate(provider, context, methodCallExpression);
                case "StandardDeviationPopulation":
                case "StandardDeviationPopulationAsync":
                case "StandardDeviationSample":
                case "StandardDeviationSampleAsync":
                    return StandardDeviationMethodToExecutableQueryTranslator<TResult>.Translate(provider, context, methodCallExpression);
                case "Sum":
                case "SumAsync":
                    return SumMethodToExecutableQueryTranslator<TResult>.Translate(provider, context, methodCallExpression);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private class IdentityFinalizer<TOutput> : IExecutableQueryFinalizer<TOutput, IAsyncCursor<TOutput>>
        {
            #region static
            private static readonly IExecutableQueryFinalizer<TOutput, IAsyncCursor<TOutput>> __instance = new IdentityFinalizer<TOutput>();

            public static IExecutableQueryFinalizer<TOutput, IAsyncCursor<TOutput>> Instance => __instance;
            #endregion

            public IAsyncCursor<TOutput> Finalize(IAsyncCursor<TOutput> cursor, CancellationToken cancellationToken)
            {
                return cursor;
            }

            public Task<IAsyncCursor<TOutput>> FinalizeAsync(IAsyncCursor<TOutput> cursor, CancellationToken cancellationToken)
            {
                return Task.FromResult(cursor);
            }
        }
    }
}
