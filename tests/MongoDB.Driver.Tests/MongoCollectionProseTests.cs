/* Copyright 2020-present MongoDB Inc.
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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class MongoCollectionProseTests
    {
        [SkippableFact]
        public void WriteConcernError_errInfo_should_be_propagated()
        {
            var failPointFeature = CoreTestConfiguration.Cluster.Description.Type == ClusterType.Sharded
                ? Feature.FailPointsFailCommandForSharded
                : Feature.FailPointsFailCommand;
            RequireServer.Check().Supports(failPointFeature);

            var failPointCommand = @"
                {
                    configureFailPoint : 'failCommand',
                    data : {
                        failCommands : ['insert'],
                        writeConcernError : {
                            code : 100,
                            codeName : 'UnsatisfiableWriteConcern',
                            errmsg : 'Not enough data-bearing nodes',
                            errInfo : {
                                writeConcern : {
                                    w : 2,
                                    wtimeout : 0,
                                    provenance : 'clientSupplied'
                                }
                            }
                        }
                    },
                    mode: { times: 1 }
                }";

            using (ConfigureFailPoint(failPointCommand))
            {
                var client = DriverTestConfiguration.Client;
                var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
                var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

                var exception = Record.Exception(() => collection.InsertOne(new BsonDocument()));

                exception.Should().NotBeNull();
                var e = exception.InnerException.Should().BeOfType<MongoBulkWriteException<BsonDocument>>().Subject;
                var writeConcernError = e.WriteConcernError;
                writeConcernError.Code.Should().Be(100);
                writeConcernError.CodeName.Should().Be("UnsatisfiableWriteConcern");
                writeConcernError.Details.Should().Be("{ writeConcern : { w : 2, wtimeout : 0, provenance : 'clientSupplied' } }");
                writeConcernError.Message.Should().Be("Not enough data-bearing nodes");
            }
        }

        // private methods
        private FailPoint ConfigureFailPoint(string failpointCommand)
        {
            var cluster = DriverTestConfiguration.Client.Cluster;
            var session = NoCoreSession.NewHandle();
            return FailPoint.Configure(cluster, session, BsonDocument.Parse(failpointCommand));
        }
    }
}
