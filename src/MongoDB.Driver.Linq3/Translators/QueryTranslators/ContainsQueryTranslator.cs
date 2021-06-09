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

using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Ast.Filters;
using MongoDB.Driver.Linq3.Ast.Stages;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Serializers;
using MongoDB.Driver.Linq3.Translators.PipelineTranslators;

namespace MongoDB.Driver.Linq3.Translators.QueryTranslators
{
    public static class ContainsQueryTranslator
    {
        // private static fields
        private static readonly IExecutableQueryFinalizer<string, bool> __finalizer = new ContainsFinalizer();
        private static readonly IBsonSerializer<BsonNull> __outputSerializer = new WrappedValueSerializer<BsonNull>(BsonNullSerializer.Instance);

        // public static methods
        public static ExecutableQuery<TDocument, bool> Translate<TDocument>(MongoQueryProvider<TDocument> provider, TranslationContext context, MethodCallExpression expression)
        {
            if (expression.Method.Is(QueryableMethod.Contains))
            {
                var source = expression.Arguments[0];
                var item = expression.Arguments[1];

                var pipeline = PipelineTranslator.Translate(context, source);

                if (!(pipeline.OutputSerializer is IWrappedValueSerializer))
                {
                    var valueType = source.Type.GetGenericArguments()[0];
                    var valueSerializer = pipeline.OutputSerializer;
                    var wrappedValueSerializer = WrappedValueSerializer.Create(valueSerializer);
                    pipeline.AddStages(
                        wrappedValueSerializer,
                        //BsonDocument.Parse("{ $project : { _id : 0, _v : \"$$ROOT\" } }"));
                        new AstProjectStage(
                            new AstProjectStageExcludeFieldSpecification("_id"),
                            new AstProjectStageComputedFieldSpecification(new AstComputedField("_v", new AstFieldExpression("$$ROOT")))));
                }

                var itemValue = ((ConstantExpression)item).Value;
                var serializedWrappedValue = SerializationHelper.SerializeValue(pipeline.OutputSerializer, itemValue);

                AstFilter filter = null; // TODO; serializedWrappedValue
                pipeline.AddStages(
                    __outputSerializer,
                    //new BsonDocument("$match", serializedWrappedValue),
                    //new BsonDocument("$limit", 1),
                    //new BsonDocument("$project", new BsonDocument { { "_id", 0 }, { "_v", BsonNull.Value } }));
                    new AstMatchStage(filter),
                    new AstLimitStage(1),
                    new AstProjectStage(
                        new AstProjectStageExcludeFieldSpecification("_id"),
                        new AstProjectStageComputedFieldSpecification(new AstComputedField("_v", BsonNull.Value))));
                return new ExecutableQuery<TDocument, string, bool>(
                    provider.Collection,
                    provider.Options,
                    pipeline.ToPipelineDefinition<TDocument, string>(),
                    __finalizer);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private class ContainsFinalizer : IExecutableQueryFinalizer<string, bool>
        {
            public bool Finalize(IAsyncCursor<string> cursor, CancellationToken cancellationToken)
            {
                var output = cursor.ToList(cancellationToken);
                return output.Any();
            }

            public async Task<bool> FinalizeAsync(IAsyncCursor<string> cursor, CancellationToken cancellationToken)
            {
                var output = await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);
                return output.Any();
            }
        }
    }
}
