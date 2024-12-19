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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators
{
    internal static class ContainsMethodToExecutableQueryTranslator
    {
        // private static fields
        private static readonly IExecutableQueryFinalizer<BsonNull, bool> __finalizer = new ContainsFinalizer();
        private static readonly IBsonSerializer<BsonNull> __outputSerializer = new WrappedValueSerializer<BsonNull>("_v", BsonNullSerializer.Instance);

        // public static methods
        public static ExecutableQuery<TDocument, bool> Translate<TDocument>(MongoQueryProvider<TDocument> provider, TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(QueryableMethod.Contains))
            {
                var sourceExpression = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, sourceExpression);
                ClientSideProjectionHelper.ThrowIfClientSideProjection(expression, pipeline, method);

                IBsonSerializer valueSerializer;
                if (pipeline.OutputSerializer is IWrappedValueSerializer wrappedValueSerializer)
                {
                    valueSerializer = wrappedValueSerializer.ValueSerializer;
                }
                else
                {
                    valueSerializer = pipeline.OutputSerializer;
                    wrappedValueSerializer = WrappedValueSerializer.Create("_v", valueSerializer);
                    pipeline = pipeline.AddStages(
                        wrappedValueSerializer,
                        AstStage.Project(
                            AstProject.ExcludeId(),
                            AstProject.Set("_v", AstExpression.Var("ROOT"))));
                }

                var itemExpression = arguments[1];
                var itemValue = itemExpression.GetConstantValue<object>(containingExpression: expression);
                var serializedValue = SerializationHelper.SerializeValue(pipeline.OutputSerializer, itemValue);

                AstFilter filter = AstFilter.Eq(AstFilter.Field("_v"), serializedValue);
                pipeline = pipeline.AddStages(
                    __outputSerializer,
                    AstStage.Match(filter),
                    AstStage.Limit(1),
                    AstStage.Project(
                        AstProject.ExcludeId(),
                        AstProject.Set("_v", BsonNull.Value)));

                return ExecutableQuery.Create(
                    provider,
                    pipeline,
                    __finalizer);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private class ContainsFinalizer : IExecutableQueryFinalizer<BsonNull, bool>
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
