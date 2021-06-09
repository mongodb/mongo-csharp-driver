﻿/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Driver.Linq3.Translators.ExpressionToExecutableQueryTranslators;
using MongoDB.Driver.Linq3.Translators.ExpressionToPipelineTranslators;
using MongoDB.Driver.Linq3.Translators.ExpressionToExecutableQueryTranslators.Finalizers;
using MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToExecutableQueryTranslators
{
    public static class FirstMethodToExecutableQueryTranslator<TOutput>
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
                    var predicateLambda = ExpressionHelper.Unquote(arguments[1]);
                    var filter = ExpressionToFilterTranslator.Translate(context, predicateLambda, parameterSerializer: pipeline.OutputSerializer);
                    pipeline.AddStages(
                        pipeline.OutputSerializer,
                        new AstMatchStage(filter));
                }

                pipeline.AddStages(
                    pipeline.OutputSerializer,
                    new AstLimitStage(1));

                var finalizer = expression.Method.Name == "FirstOrDefault" ? __firstOrDefaultFinalizer : __firstFinalizer;

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
