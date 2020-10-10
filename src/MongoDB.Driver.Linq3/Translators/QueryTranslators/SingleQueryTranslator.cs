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
using MongoDB.Driver.Linq3.Ast.Stages;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Translators.FilterTranslators;
using MongoDB.Driver.Linq3.Translators.PipelineTranslators;
using MongoDB.Driver.Linq3.Translators.QueryTranslators.Finalizers;

namespace MongoDB.Driver.Linq3.Translators.QueryTranslators
{
    public static class SingleQueryTranslator<TOutput>
    {
        // private static fields
        private static readonly IExecutableQueryFinalizer<TOutput, TOutput> __singleFinalizer = new SingleFinalizer<TOutput>();
        private static readonly MethodInfo[] __singleMethods;
        private static readonly MethodInfo[] __singleOrDefaultMethods;
        private static readonly MethodInfo[] __singleWithPredicateMethods;
        private static readonly IExecutableQueryFinalizer<TOutput, TOutput> __singleOrDefaultFinalizer = new SingleOrDefaultFinalizer<TOutput>();

        // static constructor
        static SingleQueryTranslator()
        {
            __singleMethods = new[]
            {
                QueryableMethod.Single,
                QueryableMethod.SingleOrDefault,
                QueryableMethod.SingleOrDefaultWithPredicate,
                QueryableMethod.SingleWithPredicate,
                MongoQueryableMethod.SingleAsync,
                MongoQueryableMethod.SingleOrDefaultAsync,
                MongoQueryableMethod.SingleOrDefaultWithPredicateAsync,
                MongoQueryableMethod.SingleWithPredicateAsync
            };

            __singleWithPredicateMethods = new[]
            {
                QueryableMethod.SingleOrDefaultWithPredicate,
                QueryableMethod.SingleWithPredicate,
                MongoQueryableMethod.SingleOrDefaultWithPredicateAsync,
                MongoQueryableMethod.SingleWithPredicateAsync
            };

            __singleOrDefaultMethods = new[]
            {
                QueryableMethod.SingleOrDefault,
                QueryableMethod.SingleOrDefaultWithPredicate,
                MongoQueryableMethod.SingleOrDefaultAsync,
                MongoQueryableMethod.SingleOrDefaultWithPredicateAsync
            };
        }

        // public static methods
        public static ExecutableQuery<TDocument, TOutput> Translate<TDocument>(MongoQueryProvider<TDocument> provider, TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__singleMethods))
            {
                var sourceExpression = arguments[0];
                var pipeline = PipelineTranslator.Translate(context, sourceExpression);

                if (method.IsOneOf(__singleWithPredicateMethods))
                {
                    var predicateLambda = ExpressionHelper.Unquote(arguments[1]);
                    var filter = FilterTranslator.Translate(context, predicateLambda, parameterSerializer: pipeline.OutputSerializer);
                    pipeline.AddStages(
                        pipeline.OutputSerializer,
                        new AstMatchStage(filter));
                }

                pipeline.AddStages(
                    pipeline.OutputSerializer,
                    new AstLimitStage(2));

                var finalizer = method.IsOneOf(__singleOrDefaultMethods) ? __singleOrDefaultFinalizer : __singleFinalizer;

                return new ExecutableQuery<TDocument, TOutput, TOutput>(
                    provider.Collection,
                    provider.Options,
                    pipeline.ToPipelineDefinition<TDocument, TOutput>(),
                    finalizer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
