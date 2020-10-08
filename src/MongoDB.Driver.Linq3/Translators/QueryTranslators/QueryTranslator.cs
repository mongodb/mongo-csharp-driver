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
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Translators.PipelineTranslators;

namespace MongoDB.Driver.Linq3.Translators.QueryTranslators
{
    public static class QueryTranslator
    {
        // public static methods
        public static ExecutableQuery<TDocument, IAsyncCursor<TOutput>> TranslateMultiValuedQuery<TDocument, TOutput>(MongoQueryProvider<TDocument> provider, Expression expression)
        {
            expression = PartialEvaluator.EvaluatePartially(expression);

            var context = new TranslationContext();
            var pipeline = PipelineTranslator.Translate(context, expression);

            return new ExecutableQuery<TDocument, TOutput, IAsyncCursor<TOutput>>(
                provider.Collection,
                provider.Options,
                pipeline.ToPipelineDefinition<TDocument, TOutput>(),
                MultiValuedQueryFinalizer<TOutput>.Instance);
        }

        public static ExecutableQuery<TDocument, TResult> TranslateSingleValuedQuery<TDocument, TResult>(MongoQueryProvider<TDocument> provider, Expression expression)
        {
            expression = PartialEvaluator.EvaluatePartially(expression);

            var context = new TranslationContext();
            var methodCallExpression = (MethodCallExpression)expression;
            switch (methodCallExpression.Method.Name)
            {
                case "All":
                    return AllQueryTranslator.Translate(provider, context, methodCallExpression).AsExecutableQuery<TDocument, TResult>();
                case "Any":
                case "AnyAsync":
                    return AnyQueryTranslator.Translate(provider, context, methodCallExpression).AsExecutableQuery<TDocument, TResult>();
                case "Average":
                case "AverageAsync":
                    return AverageQueryTranslator<TResult>.Translate(provider, context, methodCallExpression);
                case "Contains":
                    return ContainsQueryTranslator.Translate(provider, context, methodCallExpression).AsExecutableQuery<TDocument, TResult>(); ;
                case "Count":
                case "CountAsync":
                    return CountQueryTranslator.Translate(provider, context, methodCallExpression).AsExecutableQuery<TDocument, TResult>();
                case "ElementAt":
                    return ElementAtQueryTranslator<TResult>.Translate(provider, context, methodCallExpression);
                case "First":
                case "FirstAsync":
                case "FirstOrDefault":
                case "FirstOrDefaultAsync":
                    return FirstQueryTranslator<TResult>.Translate(provider, context, methodCallExpression);
                case "Last":
                case "LastOrDefault":
                    return LastQueryTranslator<TResult>.Translate(provider, context, methodCallExpression);
                case "LongCount":
                case "LongCountAsync":
                    return LongCountQueryTranslator.Translate(provider, context, methodCallExpression).AsExecutableQuery<TDocument, TResult>();
                case "Max":
                case "MaxAsync":
                    return MaxQueryTranslator<TResult>.Translate(provider, context, methodCallExpression);
                case "Min":
                    return MinQueryTranslator<TResult>.Translate(provider, context, methodCallExpression);
                case "Single":
                case "SingleOrDefault":
                    return SingleQueryTranslator<TResult>.Translate(provider, context, methodCallExpression);
                case "Sum":
                    return SumQueryTranslator<TResult>.Translate(provider, context, methodCallExpression);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private class MultiValuedQueryFinalizer<TOutput> : IExecutableQueryFinalizer<TOutput, IAsyncCursor<TOutput>>
        {
            #region static
            private static readonly IExecutableQueryFinalizer<TOutput, IAsyncCursor<TOutput>> __instance = new MultiValuedQueryFinalizer<TOutput>();

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
