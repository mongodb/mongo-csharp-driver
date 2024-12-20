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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation
{
    public static class Linq3TestHelpers
    {
        public static void AssertStages(IEnumerable<BsonDocument> stages, IEnumerable<string> expectedStages)
        {
            stages.Should().Equal(expectedStages.Select(json => BsonDocument.Parse(json)));
        }

        public static IList<BsonDocument> Render<TInput, TOutput>(PipelineDefinition<TInput, TOutput> pipeline, IBsonSerializer<TInput> inputSerializer, ExpressionTranslationOptions translationOptions)
        {
            var rendered = pipeline.Render(new(inputSerializer, BsonSerializer.SerializerRegistry, translationOptions: translationOptions));
            return rendered.Documents;
        }

        public static IReadOnlyList<BsonDocument> Render<TInput, TOutput>(PipelineStageDefinition<TInput, TOutput> stage, IBsonSerializer<TInput> inputSerializer, ExpressionTranslationOptions translationOptions)
        {
            var rendered = stage.Render(new(inputSerializer, BsonSerializer.SerializerRegistry, translationOptions: translationOptions));
            return rendered.Documents;
        }

        public static List<BsonDocument> Translate<TDocument, TResult>(IMongoCollection<TDocument> collection, IAggregateFluent<TResult> aggregate)
        {
            var pipelineDefinition = ((AggregateFluent<TDocument, TResult>)aggregate).Pipeline;
            var documentSerializer = collection.DocumentSerializer;
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var renderedPipeline = pipelineDefinition.Render(new(documentSerializer, serializerRegistry));
            return renderedPipeline.Documents.ToList();
        }

        // in this overload the collection argument is used only to infer the TDocument type
        public static List<BsonDocument> Translate<TDocument, TResult>(IMongoCollection<TDocument> collection, IQueryable<TResult> queryable)
        {
            return Translate<TDocument, TResult>(queryable);
        }

        public static List<BsonDocument> Translate<TDocument, TResult>(IQueryable<TResult> queryable)
        {
            var provider = (MongoQueryProvider<TDocument>)queryable.Provider;
            var translationOptions = provider.GetTranslationOptions();
            var executableQuery = ExpressionToExecutableQueryTranslator.Translate<TDocument, TResult>(provider, queryable.Expression, translationOptions);
            var stages = executableQuery.Pipeline.AstStages;
            return stages.Select(s => s.Render().AsBsonDocument).ToList();
        }
    }
}
