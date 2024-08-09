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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Tests.Linq.Linq3Implementation;
using Xunit;

namespace MongoDB.Driver.Tests.Jira.CSharp624
{
    public class CSharp4646Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Watch_client_filtering_on_database_name()
        {
            RequireServer.Check().ClusterTypes(ClusterType.ReplicaSet, ClusterType.Sharded).Supports(Feature.ChangeStreamAllChangesForCluster);
            var client = DriverTestConfiguration.Client;

            var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
                .Match(x => x.DatabaseNamespace.DatabaseName.StartsWith("MyPrefix"));

            var stages = RenderPipeline(pipeline);
            AssertStages(stages, "{ $match : { 'ns.db' : /^MyPrefix/s  } }");

            using var changeStream = client.Watch(pipeline);
        }

        [Fact]
        public void Watch_client_filtering_on_collection_name()
        {
            RequireServer.Check().ClusterTypes(ClusterType.ReplicaSet, ClusterType.Sharded).Supports(Feature.ChangeStreamAllChangesForCluster);
            var client = DriverTestConfiguration.Client;

            var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
                .Match(x => x.CollectionNamespace.CollectionName.StartsWith("MyPrefix"));

            var stages = RenderPipeline(pipeline);
            AssertStages(stages, "{ $match : { 'ns.coll' : /^MyPrefix/s  } }");

            using var changeStream = client.Watch(pipeline);
        }

        [Fact]
        public void Watch_database_filtering_on_collection_name()
        {
            RequireServer.Check().ClusterTypes(ClusterType.ReplicaSet, ClusterType.Sharded).Supports(Feature.ChangeStreamForDatabase);
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("test");

            var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
                .Match(x => x.CollectionNamespace.CollectionName.StartsWith("MyPrefix"));

            var stages = RenderPipeline(pipeline);
            AssertStages(stages, "{ $match : { 'ns.coll' : /^MyPrefix/s  } }");

            // some older versions of the server require the database to exist before you can watch it
            CreateDatabase(database);
             
            using var changeStream = database.Watch(pipeline);
        }

        private void CreateDatabase(IMongoDatabase database)
        {
            // the easiest way to create a database is to create a collection by inserting a document
            var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);
            collection.InsertOne(new BsonDocument("_id", 1));
        }

        private IList<BsonDocument> RenderPipeline(PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>> pipeline)
        {
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = BsonDocumentSerializer.Instance;
            var changeStreamDocumentSerializer = new ChangeStreamDocumentSerializer<BsonDocument>(documentSerializer);
            var renderedPipeline = pipeline.Render(new(changeStreamDocumentSerializer, serializerRegistry));
            return renderedPipeline.Documents;
        }
    }
}
