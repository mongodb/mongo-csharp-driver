/* Copyright 2010-2017 MongoDB Inc.
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
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Operations;
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
        public void ListDatabases_should_invoke_the_correct_operation(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var operationExecutor = new MockOperationExecutor();
            var subject = new MongoClient(operationExecutor, DriverTestConfiguration.GetClientSettings());
            var session = CreateClientSession();
            var cancellationToken = new CancellationTokenSource().Token;

            if (usingSession)
            {
                if (async)
                {
                    subject.ListDatabasesAsync(session, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.ListDatabases(session, cancellationToken);
                }
            }
            else
            {
                if (async)
                {
                    subject.ListDatabasesAsync(cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.ListDatabases(cancellationToken);
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

            call.Operation.Should().BeOfType<ListDatabasesOperation>();
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

        // private methods
        private IClientSessionHandle CreateClientSession()
        {
            var client = new Mock<IMongoClient>().Object;
            var options = new ClientSessionOptions();
            var serverSession = new ServerSession();
            var clientSession = new ClientSession(client, options, serverSession, isImplicit: false);
            return new ClientSessionHandle(clientSession);
        }
    }
}
