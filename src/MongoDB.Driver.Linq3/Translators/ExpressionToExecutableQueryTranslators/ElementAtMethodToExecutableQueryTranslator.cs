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
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Translators.ExpressionToPipelineTranslators;
using MongoDB.Driver.Linq3.Translators.ExpressionToExecutableQueryTranslators.Finalizers;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToExecutableQueryTranslators
{
    public static class ElementAtMethodToExecutableQueryTranslator<TOutput>
    {
        // private static fields
        private static readonly IExecutableQueryFinalizer<TOutput, TOutput> __finalizer = new SingleFinalizer<TOutput>();

        // public static methods
        public static ExecutableQuery<TDocument, TOutput> Translate<TDocument>(MongoQueryProvider<TDocument> provider, TranslationContext context, MethodCallExpression expression)
        {
            if (expression.Method.Is(QueryableMethod.ElementAt))
            {
                var source = expression.Arguments[0];
                var index = expression.Arguments[1];

                var pipeline = ExpressionToPipelineTranslator.Translate(context, source);

                var indexValue = (int)((ConstantExpression)index).Value;

                pipeline = pipeline.AddStages(
                    pipeline.OutputSerializer,
                    //new BsonDocument("$skip", indexValue),
                    //new BsonDocument("$limit", 1));
                    AstStage.Skip(indexValue),
                    AstStage.Limit(1));

                return new ExecutableQuery<TDocument, TOutput, TOutput>(
                    provider.Collection,
                    provider.Options,
                    pipeline,
                    __finalizer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
