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

using System;
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

        protected IMongoClient GetClient(LinqProvider linqProvider)
        {
            return linqProvider switch
            {
                LinqProvider.V2 => DriverTestConfiguration.Client,
                LinqProvider.V3 => DriverTestConfiguration.Linq3Client,
                _ => throw new ArgumentException($"Invalid linqProvider: {linqProvider}.", nameof(linqProvider))
            };
        }

        protected IMongoCollection<TDocument> GetCollection<TDocument>(string collectionName = null)
        {
            var databaseName = DriverTestConfiguration.DatabaseNamespace.DatabaseName;
            return GetCollection<TDocument>(databaseName, collectionName);
        }

        protected IMongoCollection<TDocument> GetCollection<TDocument>(string databaseName, string collectionName, LinqProvider linqProvider = LinqProvider.V3)
        {
            databaseName ??= DriverTestConfiguration.DatabaseNamespace.DatabaseName;
            collectionName ??= DriverTestConfiguration.CollectionNamespace.CollectionName;
            var client = GetClient(linqProvider);
            var database = client.GetDatabase(databaseName);
            return database.GetCollection<TDocument>(collectionName);
        }

        protected static List<BsonDocument> Translate<TDocument, TResult>(IMongoCollection<TDocument> collection, IAggregateFluent<TResult> aggregate)
        {
            var pipelineDefinition = ((AggregateFluent<TDocument, TResult>)aggregate).Pipeline;
            var documentSerializer = collection.DocumentSerializer;
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var linqProvider = collection.Database.Client.Settings.LinqProvider;
            var renderedPipeline = pipelineDefinition.Render(documentSerializer, serializerRegistry, linqProvider);
            return renderedPipeline.Documents.ToList();
        }

        // in this overload the collection argument is used only to infer the TDocument type
        protected List<BsonDocument> Translate<TDocument, TResult>(IMongoCollection<TDocument> collection, IQueryable<TResult> queryable)
        {
            return Translate<TDocument, TResult>(queryable);
        }

        protected List<BsonDocument> Translate<TDocument, TResult>(IQueryable<TResult> queryable)
        {
            var provider = (MongoQueryProvider<TDocument>)queryable.Provider;
            var executableQuery = ExpressionToExecutableQueryTranslator.Translate<TDocument, TResult>(provider, queryable.Expression);
            var stages = executableQuery.Pipeline.Stages;
            return stages.Select(s => s.Render().AsBsonDocument).ToList();
        }

        protected BsonDocument TranslateFilter<TDocument>(IMongoCollection<TDocument> collection, IFindFluent<TDocument, TDocument> find)
        {
            var filterDefinition = find.Filter;
            var documentSerializer = collection.DocumentSerializer;
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var linqProvider = collection.Database.Client.Settings.LinqProvider;
            return filterDefinition.Render(documentSerializer, serializerRegistry, linqProvider);
        }
    }
}
