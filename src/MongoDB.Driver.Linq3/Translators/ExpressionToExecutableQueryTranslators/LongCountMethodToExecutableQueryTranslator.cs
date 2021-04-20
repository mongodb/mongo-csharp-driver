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
using MongoDB.Driver.Linq3.Ast.Stages;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Reflection;
using MongoDB.Driver.Linq3.Serializers;
using MongoDB.Driver.Linq3.Translators.ExpressionToExecutableQueryTranslators.Finalizers;
using MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators;
using MongoDB.Driver.Linq3.Translators.ExpressionToPipelineTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToExecutableQueryTranslators
{
    public static class LongCountMethodToExecutableQueryTranslator
    {
        // private static fields
        private static readonly MethodInfo[] __longCountMethods;
        private static readonly MethodInfo[] __longCountWithPredicateMethods;
        private static readonly IExecutableQueryFinalizer<long, long> _finalizer = new SingleOrDefaultFinalizer<long>();
        private static readonly IBsonSerializer<long> __wrappedInt64Serializer = new WrappedValueSerializer<long>(new Int64Serializer());

        // static constructor
        static LongCountMethodToExecutableQueryTranslator()
        {
            __longCountMethods = new[]
            {
                QueryableMethod.LongCount,
                QueryableMethod.LongCountWithPredicate,
                MongoQueryableMethod.LongCountAsync,
                MongoQueryableMethod.LongCountWithPredicateAsync
            };

            __longCountWithPredicateMethods = new[]
            {
                QueryableMethod.LongCountWithPredicate,
                MongoQueryableMethod.LongCountWithPredicateAsync
            };
        }

        // public static methods
        public static ExecutableQuery<TDocument, long> Translate<TDocument>(MongoQueryProvider<TDocument> provider, TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__longCountMethods))
            {
                var sourceExpression = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, sourceExpression);

                if (method.IsOneOf(__longCountWithPredicateMethods))
                {
                    var predicateLambda = ExpressionHelper.UnquoteLambda(arguments[1]);
                    var filter = ExpressionToFilterTranslator.TranslateLambda(context, predicateLambda, parameterSerializer: pipeline.OutputSerializer);
                    pipeline = pipeline.AddStages(
                        pipeline.OutputSerializer,
                        AstStage.Match(filter));
                }

                pipeline = pipeline.AddStages(
                    __wrappedInt64Serializer,
                    AstStage.Count("_v"));

                return new ExecutableQuery<TDocument, long, long>(
                    provider.Collection,
                    provider.Options,
                    pipeline,
                    _finalizer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
