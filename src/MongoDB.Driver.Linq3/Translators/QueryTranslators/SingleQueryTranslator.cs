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
using MongoDB.Driver.Linq3.Ast.Stages;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Translators.PipelineTranslators;
using MongoDB.Driver.Linq3.Translators.QueryTranslators.Finalizers;

namespace MongoDB.Driver.Linq3.Translators.QueryTranslators
{
    public static class SingleQueryTranslator<TOutput>
    {
        // private static fields
        private static readonly IExecutableQueryFinalizer<TOutput, TOutput> __singleFinalizer = new SingleFinalizer<TOutput>();
        private static readonly IExecutableQueryFinalizer<TOutput, TOutput> __singleOrDefaultFinalizer = new SingleOrDefaultFinalizer<TOutput>();

        // public static methods
        public static ExecutableQuery<TDocument, TOutput> Translate<TDocument>(MongoQueryProvider<TDocument> provider, TranslationContext context, MethodCallExpression expression)
        {
            if (expression.Method.IsOneOf(QueryableMethod.Single, QueryableMethod.SingleWithPredicate, QueryableMethod.SingleOrDefault, QueryableMethod.SingleOrDefaultWithPredicate))
            {
                var source = expression.Arguments[0];
                if (expression.Method.IsOneOf(QueryableMethod.SingleWithPredicate, QueryableMethod.SingleOrDefaultWithPredicate))
                {
                    var predicate = expression.Arguments[1];
                    var tsource = source.Type.GetGenericArguments()[0];
                    source = Expression.Call(QueryableMethod.MakeWhere(tsource), source, predicate);
                }

                var pipeline = PipelineTranslator.Translate(context, source);

                pipeline.AddStages(
                    pipeline.OutputSerializer,
                    //new BsonDocument("$limit", 2));
                    new AstLimitStage(2));

                var finalizer = expression.Method.Name == "SingleOrDefault" ? __singleOrDefaultFinalizer : __singleFinalizer;

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
