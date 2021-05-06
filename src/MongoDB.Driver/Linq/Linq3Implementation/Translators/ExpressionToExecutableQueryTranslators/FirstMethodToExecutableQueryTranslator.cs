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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators.Finalizers;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators
{
    internal static class FirstMethodToExecutableQueryTranslator<TOutput>
    {
        // private static fields
        private static readonly IExecutableQueryFinalizer<TOutput, TOutput> __firstFinalizer = new FirstFinalizer<TOutput>();
        private static readonly MethodInfo[] __firstMethods;
        private static readonly MethodInfo[] __firstWithPredicateMethods;
        private static readonly IExecutableQueryFinalizer<TOutput, TOutput> __firstOrDefaultFinalizer = new FirstOrDefaultFinalizer<TOutput>();

        // static constructor
        static FirstMethodToExecutableQueryTranslator()
        {
            __firstMethods = new[]
            {
                QueryableMethod.First,
                QueryableMethod.FirstOrDefault,
                QueryableMethod.FirstOrDefaultWithPredicate,
                QueryableMethod.FirstWithPredicate,
                MongoQueryableMethod.FirstAsync,
                MongoQueryableMethod.FirstOrDefaultAsync,
                MongoQueryableMethod.FirstOrDefaultWithPredicateAsync,
                MongoQueryableMethod.FirstWithPredicateAsync
            };

            __firstWithPredicateMethods = new[]
            {
                QueryableMethod.FirstOrDefaultWithPredicate,
                QueryableMethod.FirstWithPredicate,
                MongoQueryableMethod.FirstOrDefaultWithPredicateAsync,
                MongoQueryableMethod.FirstWithPredicateAsync
            };
        }

        // public static methods
        public static ExecutableQuery<TDocument, TOutput> Translate<TDocument>(MongoQueryProvider<TDocument> provider, TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__firstMethods))
            {
                var sourceExpression = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, sourceExpression);

                if (method.IsOneOf(__firstWithPredicateMethods))
                {
                    var predicateLambda = ExpressionHelper.UnquoteLambda(arguments[1]);
                    var filter = ExpressionToFilterTranslator.TranslateLambda(context, predicateLambda, parameterSerializer: pipeline.OutputSerializer);
                    pipeline = pipeline.AddStages(
                        pipeline.OutputSerializer,
                        AstStage.Match(filter));
                }

                pipeline = pipeline.AddStages(
                    pipeline.OutputSerializer,
                    AstStage.Limit(1));

                var finalizer = method.Name == "FirstOrDefault" ? __firstOrDefaultFinalizer : __firstFinalizer;

                return ExecutableQuery.Create(
                    provider.Collection,
                    provider.Options,
                    pipeline,
                    finalizer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
