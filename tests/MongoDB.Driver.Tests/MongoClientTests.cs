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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class MongoClientTests
    {
        [Fact]
        public void constructor_with_settings_should_throw_when_settings_is_null()
        {
            var exception = Record.Exception(() => new MongoClient((MongoClientSettings)null));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("settings");
        }

        [Fact]
        public void UsesSameClusterForIdenticalSettings()
        {
            var client1 = new MongoClient("mongodb://localhost");
            var cluster1 = client1.Cluster;

            var client2 = new MongoClient("mongodb://localhost");
            var cluster2 = client2.Cluster;

            Assert.Same(cluster1, cluster2);
        }

        [Fact]
        public void UsesSameClusterWhenReadPreferenceTagsAreTheSame()
        {
            var client1 = new MongoClient("mongodb://localhost/?readPreference=secondary;readPreferenceTags=dc:ny");
            var cluster1 = client1.Cluster;

            var client2 = new MongoClient("mongodb://localhost/?readPreference=secondary;readPreferenceTags=dc:ny");
            var cluster2 = client2.Cluster;

            Assert.Same(cluster1, cluster2);
        }

        [Theory]
        [ParameterAttributeData]
        public void DropDatabase_should_invoke_the_correct_operation(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var operationExecutor = new MockOperationExecutor();
            var writeConcern = new WriteConcern(1);
            var subject = new MongoClient(operationExecutor, DriverTestConfiguration.GetClientSettings()).WithWriteConcern(writeConcern);
            var session = CreateClientSession();
            var cancellationToken = new CancellationTokenSource().Token;

            if (usingSession)
            {
                if (async)
                {
                    subject.DropDatabaseAsync(session, "awesome", cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.DropDatabase(session, "awesome", cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.DropDatabaseAsync("awesome", cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.DropDatabase("awesome", cancellationToken);
                }
            }

            var call = operationExecutor.GetWriteCall<BsonDocument>();
            if (usingSession)
            {
                call.SessionId.Should().BeSameAs(session.ServerSession.Id);
            }
            else
            {
                call.UsedImplicitSession.Should().BeTrue();
            }
            call.CancellationToken.Should().Be(cancellationToken);

            var dropDatabaseOperation = call.Operation.Should().BeOfType<DropDatabaseOperation>().Subject;
            dropDatabaseOperation.DatabaseNamespace.Should().Be(new DatabaseNamespace("awesome"));
            dropDatabaseOperation.WriteConcern.Should().BeSameAs(writeConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void ListDatabaseNames_should_invoke_the_correct_operation(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var operationExecutor = new MockOperationExecutor();
            var subject = new MongoClient(operationExecutor, DriverTestConfiguration.GetClientSettings());
            var session = CreateClientSession();
            var cancellationToken = new CancellationTokenSource().Token;
            var listDatabaseNamesResult = @"
            {
            	""databases"" : [
            		{
            			""name"" : ""admin"",
            			""sizeOnDisk"" : 131072,
            			""empty"" : false
            		},
            		{
            			""name"" : ""blog"",
            			""sizeOnDisk"" : 11669504,
            			""empty"" : false
            		},
            		{
            			""name"" : ""test-chambers"",
            			""sizeOnDisk"" : 222883840,
            			""empty"" : false
            		},
            		{
            			""name"" : ""recipes"",
            			""sizeOnDisk"" : 73728,
            			""empty"" : false
            		},
            		{
            			""name"" : ""employees"",
            			""sizeOnDisk"" : 225280,
            			""empty"" : false
            		}
            	],
            	""totalSize"" : 252534784,
            	""ok"" : 1
            }";
            var operationResult = BsonDocument.Parse(listDatabaseNamesResult);
            operationExecutor.EnqueueResult(CreateListDatabasesOperationCursor(operationResult));
            
            IList<string> databaseNames;
            if (async)
            {
                if (usingSession)
                {
                    databaseNames = subject.ListDatabaseNamesAsync(session, cancellationToken).GetAwaiter().GetResult().ToList();
                }
                else
                {
                    databaseNames = subject.ListDatabaseNamesAsync(cancellationToken).GetAwaiter().GetResult().ToList();
                }
            }
            else
            {
                if (usingSession)
                {
                    databaseNames = subject.ListDatabaseNames(session, cancellationToken).ToList();
                }
                else
                {
                    databaseNames = subject.ListDatabaseNames(cancellationToken).ToList();
                }
            }

            var call = operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();

            if (usingSession)
            {
                call.SessionId.Should().BeSameAs(session.ServerSession.Id);
            }
            else
            {
                call.UsedImplicitSession.Should().BeTrue();
            }

            call.CancellationToken.Should().Be(cancellationToken);

            var operation = call.Operation.Should().BeOfType<ListDatabasesOperation>().Subject;
            operation.NameOnly.Should().Be(true);
            databaseNames.Should().Equal(operationResult["databases"].AsBsonArray.Select(record => record["name"].AsString));
        }

        private IAsyncCursor<BsonDocument> CreateListDatabasesOperationCursor(BsonDocument reply)
        {
            var databases = reply["databases"].AsBsonArray.OfType<BsonDocument>();
            return new SingleBatchAsyncCursor<BsonDocument>(databases.ToList());
        }

        [Theory]
        [ParameterAttributeData]
        public void ListDatabases_should_invoke_the_correct_operation(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var operationExecutor = new MockOperationExecutor();
            var subject = new MongoClient(operationExecutor, DriverTestConfiguration.GetClientSettings());
            var session = CreateClientSession();
            var cancellationToken = new CancellationTokenSource().Token;
            var filterDocument = BsonDocument.Parse("{ name : \"awesome\" }");
            var filterDefinition = (FilterDefinition<BsonDocument>)filterDocument;
            var nameOnly = true;
            var options = new ListDatabasesOptions
            {
                Filter = filterDefinition,
                NameOnly = nameOnly
            };
            
            if (usingSession)
            {
                if (async)
                {
                    subject.ListDatabasesAsync(session, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.ListDatabases(session, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.ListDatabasesAsync(options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.ListDatabases(options, cancellationToken);
                }
            }

            var call = operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();
            if (usingSession)
            {
                call.SessionId.Should().BeSameAs(session.ServerSession.Id);
            }
            else
            {
                call.UsedImplicitSession.Should().BeTrue();
            }
            call.CancellationToken.Should().Be(cancellationToken);

            var operation = call.Operation.Should().BeOfType<ListDatabasesOperation>().Subject;
            operation.Filter.Should().Be(filterDocument);
            operation.NameOnly.Should().Be(nameOnly);
        }

        [Theory]
        [ParameterAttributeData]
        public void Watch_should_invoke_the_correct_operation(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var operationExecutor = new MockOperationExecutor();
            var clientSettings = DriverTestConfiguration.GetClientSettings();
            var subject = new MongoClient(operationExecutor, clientSettings);
            var session = usingSession ? CreateClientSession() : null;
            var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>().Limit(1);
            var options = new ChangeStreamOptions
            {
                BatchSize = 123,
                Collation = new Collation("en-us"),
                FullDocument = ChangeStreamFullDocumentOption.UpdateLookup,
                MaxAwaitTime = TimeSpan.FromSeconds(123),
                ResumeAfter = new BsonDocument(),
                StartAfter = new BsonDocument(),
                StartAtOperationTime = new BsonTimestamp(1, 2)
            };
            var cancellationToken = new CancellationTokenSource().Token;
            var renderedPipeline = new[] { BsonDocument.Parse("{ $limit : 1 }") };      

            if (usingSession)
            {
                if (async)
                {
                    subject.WatchAsync(session, pipeline, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.Watch(session, pipeline, options, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.WatchAsync(pipeline, options, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.Watch(pipeline, options, cancellationToken);
                }
            }

            var call = operationExecutor.GetReadCall<IAsyncCursor<ChangeStreamDocument<BsonDocument>>>();
            if (usingSession)
            {
                call.SessionId.Should().BeSameAs(session.ServerSession.Id);
            }
            else
            {
                call.UsedImplicitSession.Should().BeTrue();
            }
            call.CancellationToken.Should().Be(cancellationToken);

            var changeStreamOperation = call.Operation.Should().BeOfType<ChangeStreamOperation<ChangeStreamDocument<BsonDocument>>>().Subject;
            changeStreamOperation.BatchSize.Should().Be(options.BatchSize);
            changeStreamOperation.Collation.Should().BeSameAs(options.Collation);
            changeStreamOperation.CollectionNamespace.Should().BeNull();
            changeStreamOperation.DatabaseNamespace.Should().BeNull();
            changeStreamOperation.FullDocument.Should().Be(options.FullDocument);
            changeStreamOperation.MaxAwaitTime.Should().Be(options.MaxAwaitTime);
            changeStreamOperation.MessageEncoderSettings.Should().NotBeNull();
            changeStreamOperation.Pipeline.Should().Equal(renderedPipeline);
            changeStreamOperation.ReadConcern.Should().Be(clientSettings.ReadConcern);
            changeStreamOperation.ResultSerializer.Should().BeOfType<ChangeStreamDocumentSerializer<BsonDocument>>();
            changeStreamOperation.ResumeAfter.Should().Be(options.ResumeAfter);
            changeStreamOperation.StartAfter.Should().Be(options.StartAfter);
            changeStreamOperation.StartAtOperationTime.Should().Be(options.StartAtOperationTime);
        }

        [Fact]
        public void WithReadConcern_should_return_expected_result()
        {
            var originalReadConcern = new ReadConcern(ReadConcernLevel.Linearizable);
            var subject = new MongoClient().WithReadConcern(originalReadConcern);
            var newReadConcern = new ReadConcern(ReadConcernLevel.Majority);

            var result = subject.WithReadConcern(newReadConcern);

            subject.Settings.ReadConcern.Should().BeSameAs(originalReadConcern);
            result.Settings.ReadConcern.Should().BeSameAs(newReadConcern);
            result.WithReadConcern(originalReadConcern).Settings.Should().Be(subject.Settings);
        }

        [Fact]
        public void WithReadPreference_should_return_expected_result()
        {
            var originalReadPreference = new ReadPreference(ReadPreferenceMode.Secondary);
            var subject = new MongoClient().WithReadPreference(originalReadPreference);
            var newReadPreference = new ReadPreference(ReadPreferenceMode.SecondaryPreferred);

            var result = subject.WithReadPreference(newReadPreference);

            subject.Settings.ReadPreference.Should().BeSameAs(originalReadPreference);
            result.Settings.ReadPreference.Should().BeSameAs(newReadPreference);
            result.WithReadPreference(originalReadPreference).Settings.Should().Be(subject.Settings);
        }

        [Fact]
        public void WithWriteConcern_should_return_expected_result()
        {
            var originalWriteConcern = new WriteConcern(2);
            var subject = new MongoClient().WithWriteConcern(originalWriteConcern);
            var newWriteConcern = new WriteConcern(3);

            var result = subject.WithWriteConcern(newWriteConcern);

            subject.Settings.WriteConcern.Should().BeSameAs(originalWriteConcern);
            result.Settings.WriteConcern.Should().BeSameAs(newWriteConcern);
            result.WithWriteConcern(originalWriteConcern).Settings.Should().Be(subject.Settings);
        }

        [Theory]
        [InlineData("{ connectionMode : 'Automatic', clusterType : 'Standalone', servers : [ { state : 'Disconnected', type : 'Unknown' } ] }", null)]
        [InlineData("{ connectionMode : 'Automatic', clusterType : 'Standalone', servers : [ { state : 'Connected', type : 'Standalone' } ] }", false)]
        [InlineData("{ connectionMode : 'Automatic', clusterType : 'Standalone', servers : [ { state : 'Connected', type : 'Standalone', logicalSessionTimeoutMinutes : 30 } ] }", true)]
        [InlineData("{ connectionMode : 'Automatic', clusterType : 'ReplicaSet', servers : [ { state : 'Disconnected', type : 'Unknown' }, { state : 'Disconnected', type : 'Unknown' } ] }", null)]
        [InlineData("{ connectionMode : 'Automatic', clusterType : 'ReplicaSet', servers : [ { state : 'Disconnected', type : 'Unknown' }, { state : 'Connected', type : 'ReplicaSetPrimary' } ] }", false)]
        [InlineData("{ connectionMode : 'Automatic', clusterType : 'ReplicaSet', servers : [ { state : 'Disconnected', type : 'Unknown' }, { state : 'Connected', type : 'ReplicaSetSecondary' } ] }", false)]
        [InlineData("{ connectionMode : 'Automatic', clusterType : 'ReplicaSet', servers : [ { state : 'Disconnected', type : 'Unknown' }, { state : 'Connected', type : 'ReplicaSetArbiter' } ] }", null)]
        [InlineData("{ connectionMode : 'Automatic', clusterType : 'ReplicaSet', servers : [ { state : 'Disconnected', type : 'Unknown' }, { state : 'Connected', type : 'ReplicaSetPrimary', logicalSessionTimeoutMinutes : 30 } ] }", true)]
        [InlineData("{ connectionMode : 'Automatic', clusterType : 'ReplicaSet', servers : [ { state : 'Disconnected', type : 'Unknown' }, { state : 'Connected', type : 'ReplicaSetSecondary', logicalSessionTimeoutMinutes : 30 } ] }", true)]
        [InlineData("{ connectionMode : 'Automatic', clusterType : 'ReplicaSet', servers : [ { state : 'Disconnected', type : 'Unknown' }, { state : 'Connected', type : 'ReplicaSetArbiter', logicalSessionTimeoutMinutes : 30 } ] }", null)]
        [InlineData("{ connectionMode : 'Automatic', clusterType : 'Sharded', servers : [ { state : 'Disconnected', type : 'Unknown' }, { state : 'Disconnected', type : 'Unknown' } ] }", null)]
        [InlineData("{ connectionMode : 'Automatic', clusterType : 'Sharded', servers : [ { state : 'Disconnected', type : 'Unknown' }, { state : 'Connected', type : 'ShardRouter' } ] }", false)]
        [InlineData("{ connectionMode : 'Automatic', clusterType : 'Sharded', servers : [ { state : 'Disconnected', type : 'Unknown' }, { state : 'Connected', type : 'ShardRouter', logicalSessionTimeoutMinutes : 30 } ] }", true)]
        [InlineData("{ connectionMode : 'Direct', clusterType : 'ReplicaSet', servers : [ { state : 'Disconnected', type : 'Unknown' } ] }", null)]
        [InlineData("{ connectionMode : 'Direct', clusterType : 'ReplicaSet', servers : [ { state : 'Connected', type : 'ReplicaSetOther' } ] }", false)]
        [InlineData("{ connectionMode : 'Direct', clusterType : 'ReplicaSet', servers : [ { state : 'Connected', type : 'ReplicaSetOther', logicalSessionTimeoutMinutes : 30 } ] }", true)]
        [InlineData("{ connectionMode : 'Direct', clusterType : 'ReplicaSet', servers : [ { state : 'Connected', type : 'ReplicaSetPrimary' } ] }", false)]
        [InlineData("{ connectionMode : 'Direct', clusterType : 'ReplicaSet', servers : [ { state : 'Connected', type : 'ReplicaSetPrimary', logicalSessionTimeoutMinutes : 30 } ] }", true)]
        public void AreSessionsSupported_should_return_expected_result(string clusterDescriptionJson, bool? expectedResult)
        {
            var subject = new MongoClient("mongodb://localhost");
            var clusterDescription = ClusterDescriptionParser.Parse(clusterDescriptionJson);

            var result = subject.AreSessionsSupported(clusterDescription);

            result.Should().Be(expectedResult);
        }

        // private methods
        private IClientSessionHandle CreateClientSession()
        {
            var client = new Mock<IMongoClient>().Object;
            var options = new ClientSessionOptions();
            var cluster = Mock.Of<ICluster>();
            var coreServerSession = new CoreServerSession();
            var coreSession = new CoreSession(cluster, coreServerSession, options.ToCore());
            var coreSessionHandle = new CoreSessionHandle(coreSession);
            return new ClientSessionHandle(client, options, coreSessionHandle);
        }
    }

    public class AreSessionsSupportedServerSelectorTests
    {
        [Theory]
        [InlineData("{ clusterType : 'Standalone', servers : [ { state : 'Disconnected', type : 'Unknown' } ]}")]
        public void SelectServers_should_set_ClusterDescription(string clusterDescriptionJson)
        {
            var subject = CreateSubject();
            var cluster = ClusterDescriptionParser.Parse(clusterDescriptionJson);
            var connectedServers = cluster.Servers.Where(s => s.State == ServerState.Connected);

            var result = subject.SelectServers(cluster, connectedServers);

            AreSessionsSupportedServerSelectorReflector.ClusterDescription(subject).Should().BeSameAs(cluster);
        }

        [Theory]
        [InlineData("{ connectionMode : 'Direct', clusterType : 'ReplicaSet', servers : [ { state : 'Connected', type : 'ReplicaSetArbiter' } ]}")]
        public void SelectServers_should_return_all_servers_when_connection_mode_is_direct(string clusterDescriptionJson)
        {
            var subject = CreateSubject();
            var cluster = ClusterDescriptionParser.Parse(clusterDescriptionJson);
            var connectedServers = cluster.Servers.Where(s => s.State == ServerState.Connected).ToList();

            var result = subject.SelectServers(cluster, connectedServers);

            result.Should().Equal(connectedServers);
        }

        [Theory]
        [InlineData("{ clusterType : 'ReplicaSet', servers : [ { state : 'Connected', type : 'ReplicaSetArbiter' }, { state : 'Connected', type : 'ReplicaSetPrimary' } ]}")]
        public void SelectServers_should_return_data_bearing_servers_when_connection_mode_is__not_direct(string clusterDescriptionJson)
        {
            var subject = CreateSubject();
            var cluster = ClusterDescriptionParser.Parse(clusterDescriptionJson);
            var connectedServers = cluster.Servers.Where(s => s.State == ServerState.Connected).ToList();
            var dataBearingServers = connectedServers.Skip(1).Take(1);

            var result = subject.SelectServers(cluster, connectedServers);

            result.Should().Equal(dataBearingServers);
        }

        // private methods
        private IServerSelector CreateSubject()
        {
            return AreSessionsSupportedServerSelectorReflector.CreateInstance();
        }
    }

    public static class MongoClientReflector
    {
        public static bool? AreSessionsSupported(this MongoClient obj, ClusterDescription clusterDescription) => (bool?)Reflector.Invoke(obj, nameof(AreSessionsSupported), clusterDescription);
    }

    public static class AreSessionsSupportedServerSelectorReflector
    {
        public static IServerSelector CreateInstance()
        {
            var type = typeof(MongoClient).GetTypeInfo().Assembly.GetType("MongoDB.Driver.MongoClient+AreSessionsSupportedServerSelector");
            return (IServerSelector)Activator.CreateInstance(type);
        }

        public static ClusterDescription ClusterDescription(IServerSelector obj) => (ClusterDescription)Reflector.GetFieldValue(obj, nameof(ClusterDescription), BindingFlags.Public | BindingFlags.Instance);
    }
}
