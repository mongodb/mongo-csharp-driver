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
using MongoDB.Bson;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators.Finalizers;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators
{
    internal static class LastMethodToExecutableQueryTranslator<TOutput>
    {
        // private static fields
        private static readonly MethodInfo[] __lastMethods;
        private static readonly MethodInfo[] __lastWithPredicateMethods;
        private static readonly IExecutableQueryFinalizer<TOutput, TOutput> __singleFinalizer = new SingleFinalizer<TOutput>();
        private static readonly IExecutableQueryFinalizer<TOutput, TOutput> __singleOrDefaultFinalizer = new SingleOrDefaultFinalizer<TOutput>();

        // static constructor
        static LastMethodToExecutableQueryTranslator()
        {
            __lastMethods = new[]
            {
                QueryableMethod.Last,
                QueryableMethod.LastWithPredicate,
                QueryableMethod.LastOrDefault,
                QueryableMethod.LastOrDefaultWithPredicate
            };

            __lastWithPredicateMethods = new[]
            {
                QueryableMethod.LastWithPredicate,
                QueryableMethod.LastOrDefaultWithPredicate
            };
        }

        // public methods
        public static ExecutableQuery<TDocument, TOutput> Translate<TDocument>(MongoQueryProvider<TDocument> provider, TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__lastMethods))
            {
                var sourceExpression = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, sourceExpression);

                if (method.IsOneOf(__lastWithPredicateMethods))
                {
                    ClientSideProjectionHelper.ThrowIfClientSideProjection(expression, pipeline, method, "with a predicate");

                    var predicateLambda = ExpressionHelper.UnquoteLambda(arguments[1]);
                    var predicateFilter = ExpressionToFilterTranslator.TranslateLambda(context, predicateLambda, parameterSerializer: pipeline.OutputSerializer, asRoot: true);

                    pipeline = pipeline.AddStages(
                        pipeline.OutputSerializer,
                        AstStage.Match(predicateFilter));
                }

                pipeline = pipeline.AddStages(
                    pipeline.OutputSerializer,
                    AstStage.Group(
                        id: BsonNull.Value,
                        fields: AstExpression.AccumulatorField("_last", AstUnaryAccumulatorOperator.Last, AstExpression.Var("ROOT"))));

                var finalizer = method.Name == "LastOrDefault" ? __singleOrDefaultFinalizer : __singleFinalizer;

                return ExecutableQuery.Create(
                    provider,
                    pipeline,
                    finalizer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
