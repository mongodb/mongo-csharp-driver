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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators
{
    internal static class AnyMethodToExecutableQueryTranslator
    {
        // private static fields
        private static readonly IReadOnlyMethodInfoSet __anyOverloads;
        private static readonly IReadOnlyMethodInfoSet __anyWithPredicateOverloads;
        private static readonly IExecutableQueryFinalizer<BsonNull, bool> __finalizer = new AnyFinalizer();
        private static readonly IBsonSerializer<BsonNull> __outputSerializer = new WrappedValueSerializer<BsonNull>("_v", BsonNullSerializer.Instance);

        // static constructors
        static AnyMethodToExecutableQueryTranslator()
        {
            __anyOverloads = MethodInfoSet.Create(
            [
                QueryableMethod.Any,
                QueryableMethod.AnyWithPredicate,
                MongoQueryableMethod.AnyAsync,
                MongoQueryableMethod.AnyWithPredicateAsync
            ]);

            __anyWithPredicateOverloads = MethodInfoSet.Create(
            [
                QueryableMethod.AnyWithPredicate,
                MongoQueryableMethod.AnyWithPredicateAsync
            ]);
        }

        // public static methods
        public static ExecutableQuery<TDocument, bool> Translate<TDocument>(MongoQueryProvider<TDocument> provider, TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__anyOverloads))
            {
                var sourceExpression = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, sourceExpression);

                if (method.IsOneOf(__anyWithPredicateOverloads))
                {
                    ClientSideProjectionHelper.ThrowIfClientSideProjection(expression, pipeline, method, "with a predicate");

                    var predicateLambda = ExpressionHelper.UnquoteLambda(arguments[1]);
                    var predicateFilter = ExpressionToFilterTranslator.TranslateLambda(context, predicateLambda, parameterSerializer: pipeline.OutputSerializer, asRoot: true);

                    pipeline = pipeline.AddStage(
                        AstStage.Match(predicateFilter),
                        pipeline.OutputSerializer);
                }

                pipeline = pipeline.AddStages(
                    AstStage.Limit(1),
                    AstStage.Project(
                        AstProject.ExcludeId(),
                        AstProject.Set("_v", BsonNull.Value)),
                    __outputSerializer);

                return ExecutableQuery.Create(
                    provider,
                    pipeline,
                    __finalizer);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private class AnyFinalizer : IExecutableQueryFinalizer<BsonNull, bool>
        {
            public bool Finalize(IAsyncCursor<BsonNull> cursor, CancellationToken cancellationToken)
            {
                var output = cursor.ToList(cancellationToken);
                return output.Any();
            }

            public async Task<bool> FinalizeAsync(IAsyncCursor<BsonNull> cursor, CancellationToken cancellationToken)
            {
                var output = await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);
                return output.Any();
            }
        }
    }
}
