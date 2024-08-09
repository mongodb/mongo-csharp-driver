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

        protected IMongoCollection<TDocument> GetCollection<TDocument>(string collectionName = null)
        {
            return GetCollection<TDocument>(databaseName: null, collectionName);
        }

        protected IMongoCollection<TDocument> GetCollection<TDocument>(string databaseName, string collectionName)
        {
            var database = GetDatabase(databaseName);
            return database.GetCollection<TDocument>(collectionName ?? DriverTestConfiguration.CollectionNamespace.CollectionName);
        }

        protected IMongoDatabase GetDatabase(string databaseName = null)
        {
            var client = DriverTestConfiguration.Client;
            return client.GetDatabase(databaseName ?? DriverTestConfiguration.DatabaseNamespace.DatabaseName);
        }

        protected static List<BsonDocument> Translate<TDocument, TResult>(IMongoCollection<TDocument> collection, IAggregateFluent<TResult> aggregate)
        {
            var pipelineDefinition = ((AggregateFluent<TDocument, TResult>)aggregate).Pipeline;
            var documentSerializer = collection.DocumentSerializer;
            return Translate(pipelineDefinition, documentSerializer);
        }

        // in this overload the collection argument is used only to infer the TDocument type
        protected List<BsonDocument> Translate<TDocument, TResult>(IMongoCollection<TDocument> collection, IQueryable<TResult> queryable)
        {
            return Translate<TDocument, TResult>(queryable);
        }

        // in this overload the collection argument is used only to infer the TDocument type
        protected List<BsonDocument> Translate<TDocument, TResult>(IMongoCollection<TDocument> collection, IQueryable<TResult> queryable, out IBsonSerializer<TResult> outputSerializer)
        {
            return Translate<TDocument, TResult>(queryable, out outputSerializer);
        }

        protected static List<BsonDocument> Translate<TResult>(IMongoDatabase database, IAggregateFluent<TResult> aggregate)
        {
            var pipelineDefinition = ((AggregateFluent<NoPipelineInput, TResult>)aggregate).Pipeline;
            return Translate(pipelineDefinition, NoPipelineInputSerializer.Instance);
        }

        // in this overload the database argument is used only to infer the NoPipelineInput type
        protected List<BsonDocument> Translate<TResult>(IMongoDatabase database, IQueryable<TResult> queryable)
        {
            return Translate<NoPipelineInput, TResult>(queryable);
        }

        protected List<BsonDocument> Translate<TDocument, TResult>(IQueryable<TResult> queryable)
        {
            return Translate<TDocument, TResult>(queryable, out _);
        }

        protected List<BsonDocument> Translate<TDocument, TResult>(IQueryable<TResult> queryable, out IBsonSerializer<TResult> outputSerializer)
        {
            var provider = (MongoQueryProvider<TDocument>)queryable.Provider;
            var executableQuery = ExpressionToExecutableQueryTranslator.Translate<TDocument, TResult>(provider, queryable.Expression);
            var stages = executableQuery.Pipeline.Stages;
            outputSerializer = (IBsonSerializer<TResult>)executableQuery.Pipeline.OutputSerializer;
            return stages.Select(s => s.Render().AsBsonDocument).ToList();
        }

        protected static List<BsonDocument> Translate<TDocument, TResult>(
            PipelineDefinition<TDocument, TResult> pipelineDefinition,
            IBsonSerializer<TDocument> documentSerializer = null)
        {
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            documentSerializer ??= serializerRegistry.GetSerializer<TDocument>();
            var renderedPipeline = pipelineDefinition.Render(new(documentSerializer, serializerRegistry));
            return renderedPipeline.Documents.ToList();
        }

        protected BsonDocument Translate<TDocument>(IMongoCollection<TDocument> collection, FilterDefinition<TDocument> filterDefinition)
        {
            var documentSerializer = collection.DocumentSerializer;
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            return filterDefinition.Render(new(documentSerializer, serializerRegistry));
        }

        protected BsonDocument TranslateFilter<TDocument>(IMongoCollection<TDocument> collection, FilterDefinition<TDocument> filter)
        {
            var documentSerializer = collection.DocumentSerializer;
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            return filter.Render(new(documentSerializer, serializerRegistry));
        }

        protected BsonDocument TranslateFindFilter<TDocument, TProjection>(IMongoCollection<TDocument> collection, IFindFluent<TDocument, TProjection> find)
        {
            var filterDefinition = ((FindFluent<TDocument, TProjection>)find).Filter;
            return filterDefinition.Render(new(collection.DocumentSerializer, BsonSerializer.SerializerRegistry));
        }

        protected BsonDocument TranslateFindProjection<TDocument, TProjection>(
            IMongoCollection<TDocument> collection,
            IFindFluent<TDocument, TProjection> find)
        {
            var projection = ((FindFluent<TDocument, TProjection>)find).Options.Projection;
            return TranslateFindProjection(collection, projection);
        }

        protected BsonDocument TranslateFindProjection<TDocument, TProjection>(
            IMongoCollection<TDocument> collection,
            ProjectionDefinition<TDocument, TProjection> projection)
        {
            var documentSerializer = collection.DocumentSerializer;
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var renderedProjection = projection.Render(new(documentSerializer, serializerRegistry, renderForFind: true));
            return renderedProjection.Document;
        }
    }
}
