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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq3.Ast.Stages;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Serializers;
using MongoDB.Driver.Linq3.Translators.PipelineTranslators;
using MongoDB.Driver.Linq3.Translators.QueryTranslators.Finalizers;

namespace MongoDB.Driver.Linq3.Translators.QueryTranslators
{
    public static class LongCountQueryTranslator
    {
        // private static fields
        private static readonly IExecutableQueryFinalizer<long, long> __finalizer = new SingleFinalizer<long>();
        private static readonly IBsonSerializer<long> __outputSerializer = new WrappedValueSerializer<long>(new Int64Serializer());

        // public static methods
        public static ExecutableQuery<TDocument, long> Translate<TDocument>(MongoQueryProvider<TDocument> provider, TranslationContext context, MethodCallExpression expression)
        {
            if (expression.Method.IsOneOf(QueryableMethod.LongCount, QueryableMethod.LongCountWithPredicate))
            {
                var source = expression.Arguments[0];
                if (expression.Method.Is(QueryableMethod.LongCountWithPredicate))
                {
                    var predicate = expression.Arguments[1];
                    var tsource = source.Type.GetGenericArguments()[0];
                    source = Expression.Call(QueryableMethod.MakeWhere(tsource), source, predicate);
                }

                var pipeline = PipelineTranslator.Translate(context, source);

                pipeline.AddStages(
                    __outputSerializer,
                    //new BsonDocument("$count", "_v"));
                    new AstCountStage("_v"));

                return new ExecutableQuery<TDocument, long, long>(
                    provider.Collection,
                    provider.Options,
                    pipeline.ToPipelineDefinition<TDocument, long>(),
                    __finalizer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
