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
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators.Finalizers;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators
{
    internal static class CountMethodToExecutableQueryTranslator
    {
        // private static fields
        private static readonly IReadOnlyMethodInfoSet __countOverloads;
        private static readonly IReadOnlyMethodInfoSet __countWithPredicateOverloads;
        private static readonly IExecutableQueryFinalizer<int, int> _finalizer = new SingleOrDefaultFinalizer<int>();
        private static readonly IBsonSerializer<int> __wrappedInt32Serializer = new WrappedValueSerializer<int>("_v", new Int32Serializer());

        // static constructor
        static CountMethodToExecutableQueryTranslator()
        {
            __countOverloads = MethodInfoSet.Create(
            [
                QueryableMethod.CountOverloads,
                MongoQueryableMethod.CountOverloads
            ]);

            __countWithPredicateOverloads = MethodInfoSet.Create(
            [
                QueryableMethod.CountWithPredicate,
                MongoQueryableMethod.CountWithPredicateAsync
            ]);
        }

        // public static methods
        public static ExecutableQuery<TDocument, int> Translate<TDocument>(MongoQueryProvider<TDocument> provider, TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__countOverloads))
            {
                var sourceExpression = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, sourceExpression);

                if (method.IsOneOf(__countWithPredicateOverloads))
                {
                    ClientSideProjectionHelper.ThrowIfClientSideProjection(expression, pipeline, method, "with a predicate");

                    var predicateLambda = ExpressionHelper.UnquoteLambda(arguments[1]);
                    var predicateFilter = ExpressionToFilterTranslator.TranslateLambda(context, predicateLambda, parameterSerializer: pipeline.OutputSerializer, asRoot: true);

                    pipeline = pipeline.AddStage(
                        AstStage.Match(predicateFilter),
                        pipeline.OutputSerializer);
                }

                pipeline = pipeline.AddStage(
                    AstStage.Count("_v"),
                    __wrappedInt32Serializer);

                return ExecutableQuery.Create(
                    provider,
                    pipeline,
                    _finalizer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
