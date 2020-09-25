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
using MongoDB.Bson;
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Ast.Stages;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Serializers;
using MongoDB.Driver.Linq3.Translators.PipelineTranslators;
using MongoDB.Driver.Linq3.Translators.QueryTranslators.Finalizers;

namespace MongoDB.Driver.Linq3.Translators.QueryTranslators
{
    public static class AverageQueryTranslator<TOutput>
    {
        // private static fields
        private static readonly IExecutableQueryFinalizer<TOutput, TOutput> __finalizer = new SingleFinalizer<TOutput>();

        // public static methods
        public static ExecutableQuery<TDocument, TOutput> Translate<TDocument>(MongoQueryProvider<TDocument> provider, TranslationContext context, MethodCallExpression expression)
        {
            if (expression.Method.DeclaringType == typeof(Queryable))
            {
                var source = expression.Arguments[0];
                if (expression.Arguments.Count == 2)
                {
                    var selector = expression.Arguments[1];
                    var tsource = source.Type.GetGenericArguments()[0];
                    var tresult = expression.Type;
                    source = Expression.Call(QueryableMethod.MakeSelect(tsource, tresult), source, selector);
                }

                var pipeline = PipelineTranslator.Translate(context, source);
                Throw.If(!(pipeline.OutputSerializer is IWrappedValueSerializer), "Expected pipeline.OutputSerializer to be an IWrappedValueSerializer.", nameof(pipeline));

                pipeline.AddStages(
                    pipeline.OutputSerializer,
                    //BsonDocument.Parse("{ $group : { _id : null, _v : { $avg : \"$_v\" } } }"),
                    //BsonDocument.Parse("{ $project : { _id : 0 } }"));
                    new AstGroupStage(
                        id: BsonNull.Value,
                        fields: new AstComputedField("_v", new AstUnaryExpression(AstUnaryOperator.Avg, new AstFieldExpression("$_v")))));

                return new ExecutableQuery<TDocument, TOutput, TOutput>(
                    provider.Collection,
                    provider.Options,
                    pipeline.ToPipelineDefinition<TDocument, TOutput>(),
                    __finalizer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
