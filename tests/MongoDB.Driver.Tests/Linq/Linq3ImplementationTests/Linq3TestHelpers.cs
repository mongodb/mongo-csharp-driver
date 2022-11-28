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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests
{
    public static class Linq3TestHelpers
    {
        public static void AssertStages(IEnumerable<BsonDocument> stages, IEnumerable<string> expectedStages)
        {
            stages.Should().Equal(expectedStages.Select(json => BsonDocument.Parse(json)));
        }

        public static IList<BsonDocument> Render<TInput, TOutput>(PipelineDefinition<TInput, TOutput> pipeline, IBsonSerializer<TInput> inputSerializer, LinqProvider linqProvider)
        {
            var rendered = pipeline.Render(inputSerializer, BsonSerializer.SerializerRegistry, linqProvider);
            return rendered.Documents;
        }

        public static BsonDocument Render<TInput, TOutput>(PipelineStageDefinition<TInput, TOutput> stage, IBsonSerializer<TInput> inputSerializer, LinqProvider linqProvider)
        {
            var rendered = stage.Render(inputSerializer, BsonSerializer.SerializerRegistry, linqProvider);
            return rendered.Document;
        }

        public static List<BsonDocument> Translate<TDocument, TResult>(IMongoCollection<TDocument> collection, IAggregateFluent<TResult> aggregate)
        {
            var pipelineDefinition = ((AggregateFluent<TDocument, TResult>)aggregate).Pipeline;
            var documentSerializer = collection.DocumentSerializer;
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var linqProvider = collection.Database.Client.Settings.LinqProvider;
            var renderedPipeline = pipelineDefinition.Render(documentSerializer, serializerRegistry, linqProvider);
            return renderedPipeline.Documents.ToList();
        }

        // in this overload the collection argument is used only to infer the TDocument type
        public static List<BsonDocument> Translate<TDocument, TResult>(IMongoCollection<TDocument> collection, IQueryable<TResult> queryable)
        {
            return Translate<TDocument, TResult>(queryable);
        }

        public static List<BsonDocument> Translate<TDocument, TResult>(IQueryable<TResult> queryable)
        {
            var provider = queryable.Provider;
            if (provider is MongoDB.Driver.Linq.Linq2Implementation.MongoQueryProviderImpl<TDocument> linq2Provider)
            {
                var executionModel = linq2Provider.GetExecutionModel(queryable.Expression);
                var executionModelType = executionModel.GetType();
                var stagesPropertyInfo = executionModelType.GetProperty("Stages");
                var stages = (IEnumerable<BsonDocument>)stagesPropertyInfo.GetValue(executionModel);
                return stages.ToList();
            }
            else
            {
                var linq3Provider = (MongoQueryProvider<TDocument>)provider;
                var executableQuery = ExpressionToExecutableQueryTranslator.Translate<TDocument, TResult>(linq3Provider, queryable.Expression);
                var stages = executableQuery.Pipeline.Stages;
                return stages.Select(s => s.Render().AsBsonDocument).ToList();
            }
        }
    }
}
