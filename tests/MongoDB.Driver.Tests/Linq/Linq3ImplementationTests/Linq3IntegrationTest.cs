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

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests
{
    public abstract class Linq3IntegrationTest
    {
        protected void AssertStages(IEnumerable<BsonDocument> stages, params string[] expectedStages)
        {
            AssertStages(stages, (IEnumerable<string>)expectedStages);
        }

        protected void AssertStages(IEnumerable<BsonDocument> stages, IEnumerable<string> expectedStages)
        {
            stages.Should().Equal(expectedStages.Select(json => BsonDocument.Parse(json)));
        }

        protected void CreateCollection<TDocument>(IMongoCollection<TDocument> collection, IEnumerable<TDocument> documents = null)
        {
            var database = collection.Database;
            var collectionName = collection.CollectionNamespace.CollectionName;
            database.DropCollection(collectionName);

            if (documents != null && documents.Any())
            {
                collection.InsertMany(documents);
            }
            else
            {
                database.CreateCollection(collectionName);
            }
        }

        protected void CreateCollection<TDocument>(IMongoCollection<TDocument> collection, params TDocument[] documents)
        {
            CreateCollection(collection, (IEnumerable<TDocument>)documents); ;
        }

        protected IMongoCollection<TDocument> GetCollection<TDocument>(string collectionName = null, LinqProvider linqProvider = LinqProvider.V3)
        {
            return GetCollection<TDocument>(databaseName: null, collectionName, linqProvider);
        }

        protected IMongoCollection<TDocument> GetCollection<TDocument>(string databaseName, string collectionName, LinqProvider linqProvider = LinqProvider.V3)
        {
            var database = GetDatabase(databaseName, linqProvider);
            return database.GetCollection<TDocument>(collectionName ?? DriverTestConfiguration.CollectionNamespace.CollectionName);
        }

        protected IMongoDatabase GetDatabase(string databaseName = null, LinqProvider linqProvider = LinqProvider.V3)
        {
            var client = DriverTestConfiguration.GetLinqClient(linqProvider);
            return client.GetDatabase(databaseName ?? DriverTestConfiguration.DatabaseNamespace.DatabaseName);
        }

        protected static List<BsonDocument> Translate<TDocument, TResult>(IMongoCollection<TDocument> collection, IAggregateFluent<TResult> aggregate)
        {
            var pipelineDefinition = ((AggregateFluent<TDocument, TResult>)aggregate).Pipeline;
            var documentSerializer = collection.DocumentSerializer;
            var linqProvider = collection.Database.Client.Settings.LinqProvider;
            return Translate(pipelineDefinition, documentSerializer, linqProvider);
        }

        // in this overload the collection argument is used only to infer the TDocument type
        protected List<BsonDocument> Translate<TDocument, TResult>(IMongoCollection<TDocument> collection, IQueryable<TResult> queryable)
        {
            var linqProvider = collection.Database.Client.Settings.LinqProvider;
            return Translate<TDocument, TResult>(queryable, linqProvider);
        }

        // in this overload the collection argument is used only to infer the TDocument type
        protected List<BsonDocument> Translate<TDocument, TResult>(IMongoCollection<TDocument> collection, IQueryable<TResult> queryable, out IBsonSerializer<TResult> outputSerializer)
        {
            return Translate<TDocument, TResult>(queryable, out outputSerializer);
        }

        protected static List<BsonDocument> Translate<TResult>(IMongoDatabase database, IAggregateFluent<TResult> aggregate)
        {
            var pipelineDefinition = ((AggregateFluent<NoPipelineInput, TResult>)aggregate).Pipeline;
            var linqProvider = database.Client.Settings.LinqProvider;
            return Translate(pipelineDefinition, NoPipelineInputSerializer.Instance, linqProvider);
        }

        // in this overload the database argument is used only to infer the NoPipelineInput type
        protected List<BsonDocument> Translate<TResult>(IMongoDatabase database, IQueryable<TResult> queryable)
        {
            return Translate<NoPipelineInput, TResult>(queryable);
        }

        protected List<BsonDocument> Translate<TDocument, TResult>(IQueryable<TResult> queryable, LinqProvider linqProvider = LinqProvider.V3)
        {
            return Translate<TDocument, TResult>(queryable, linqProvider, out _);
        }

        protected List<BsonDocument> Translate<TDocument, TResult>(IQueryable<TResult> queryable, out IBsonSerializer<TResult> outputSerializer)
        {
            return Translate<TDocument, TResult>(queryable, LinqProvider.V3, out outputSerializer);
        }

        protected List<BsonDocument> Translate<TDocument, TResult>(IQueryable<TResult> queryable, LinqProvider linqProvider, out IBsonSerializer<TResult> outputSerializer)
        {
            if (linqProvider == LinqProvider.V2)
            {
                var linq2QueryProvider = (MongoDB.Driver.Linq.Linq2Implementation.MongoQueryProviderImpl<TDocument>)queryable.Provider;
                var executionModel = linq2QueryProvider.GetExecutionModel(queryable.Expression);
                var executionModelType = executionModel.GetType();
                var stagesPropertyInfo = executionModelType.GetProperty("Stages");
                var stages = (IEnumerable<BsonDocument>)stagesPropertyInfo.GetValue(executionModel);
                var outputSerializerPropertyInfo = executionModelType.GetProperty("OutputSerializer");
                outputSerializer = (IBsonSerializer<TResult>)outputSerializerPropertyInfo.GetValue(executionModel);
                return stages.ToList();
            }
            else
            {
                var linq3QueryProvider = (MongoQueryProvider<TDocument>)queryable.Provider;
                var executableQuery = ExpressionToExecutableQueryTranslator.Translate<TDocument, TResult>(linq3QueryProvider, queryable.Expression);
                var stages = executableQuery.Pipeline.Stages;
                outputSerializer = (IBsonSerializer<TResult>)executableQuery.Pipeline.OutputSerializer;
                return stages.Select(s => s.Render().AsBsonDocument).ToList();
            }
        }

        protected static List<BsonDocument> Translate<TDocument, TResult>(
            PipelineDefinition<TDocument, TResult> pipelineDefinition,
            IBsonSerializer<TDocument> documentSerializer = null,
            LinqProvider linqProvider = LinqProvider.V3)
        {
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            documentSerializer ??= serializerRegistry.GetSerializer<TDocument>();
            var renderedPipeline = pipelineDefinition.Render(documentSerializer, serializerRegistry, linqProvider);
            return renderedPipeline.Documents.ToList();
        }

        protected BsonDocument Translate<TDocument>(IMongoCollection<TDocument> collection, FilterDefinition<TDocument> filterDefinition)
        {
            var documentSerializer = collection.DocumentSerializer;
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var linqProvider = collection.Database.Client.Settings.LinqProvider;
            return filterDefinition.Render(documentSerializer, serializerRegistry, linqProvider);
        }

        protected BsonDocument TranslateFindProjection<TDocument, TProjection>(
            IMongoCollection<TDocument> collection,
            IFindFluent<TDocument, TProjection> find,
            LinqProvider linqProvider = LinqProvider.V3)
        {
            var findOptions = ((FindFluent<TDocument, TProjection>)find).Options;
            var projection = findOptions.Projection;
            var documentSerializer = collection.DocumentSerializer;
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var renderedProjection = projection.Render(documentSerializer, serializerRegistry, linqProvider);
            return renderedProjection.Document;
        }
    }
}
