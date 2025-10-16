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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.connection_monitoring_and_pooling;

[Category("CSOTPendingResponse")]
public class PendingResponseProseTests
{
    public PendingResponseProseTests()
    {
        RequireEnvironment.Check().EnvironmentVariable("ENABLE_CSOT_PENDING_RESPONSE_TESTS");
    }

    [Theory]
    [ParameterAttributeData]
    public async Task RecoverPartiallyReadResponse(
        [Values(true, false)]bool async,
        [Values(2, 4, 10)]int sendBytes)
    {
        var (client, eventCapturer) = CreateClient();
        using (client)
        {
            var database = client.GetDatabase("test");
            // drop collection if present and also pre-heat the connection pool
            database.DropCollection("coll");

            // TODO: CSOT: Implement timeout support for RunCommand and use it instead of MongoDatabaseSettings.
            var testDatabase = client.GetDatabase("test", new MongoDatabaseSettings { Timeout = TimeSpan.FromMilliseconds(200) });
            var testCommand = CreateInsertCommand(42);
            testCommand.Add("proxyTest", CreateTestProxyDocument(sendBytes));

            var exception = async ?
                await Record.ExceptionAsync(() => testDatabase.RunCommandAsync<BsonDocument>(testCommand)) :
                Record.Exception(() => testDatabase.RunCommand<BsonDocument>(testCommand));
            exception.Should().BeOfType<MongoConnectionException>().Subject.InnerException
                .Should().BeOfType<TimeoutException>();

            exception = async ?
                await Record.ExceptionAsync(() => database.RunCommandAsync<BsonDocument>(CreateInsertCommand(43))) :
                Record.Exception(() => database.RunCommand<BsonDocument>(CreateInsertCommand(43)));
            exception.Should().BeNull();

            eventCapturer.Next().Should().BeOfType<ConnectionCreatedEvent>();
            eventCapturer.Next().Should().BeOfType<ConnectionReadingPendingResponseEvent>();
            eventCapturer.Next().Should().BeOfType<ConnectionReadPendingResponseEvent>();
            eventCapturer.Any().Should().BeFalse();
        }
    }

    [Theory]
    [ParameterAttributeData]
    public async Task NonDestructiveAlivenessCheck([Values(true, false)]bool async)
    {
        var (client, eventCapturer) = CreateClient();
        using (client)
        {
            var database = client.GetDatabase("test");
            // drop collection if present and also pre-heat the connection pool
            database.DropCollection("coll");

            // TODO: CSOT: Implement timeout support for RunCommand and use it instead of MongoDatabaseSettings.
            var testDatabase = client.GetDatabase("test", new MongoDatabaseSettings { Timeout = TimeSpan.FromMilliseconds(200) });
            var testCommand = CreateInsertCommand(42);

            testCommand.Add("proxyTest", CreateTestProxyDocument(2));

            var exception = async ?
                await Record.ExceptionAsync(() => testDatabase.RunCommandAsync<BsonDocument>(testCommand)) :
                Record.Exception(() => testDatabase.RunCommand<BsonDocument>(testCommand));
            exception.Should().BeOfType<MongoConnectionException>().Subject.InnerException
                .Should().BeOfType<TimeoutException>();

            await Task.Delay(4000);

            exception = async ?
                await Record.ExceptionAsync(() => database.RunCommandAsync<BsonDocument>(CreateInsertCommand(43))) :
                Record.Exception(() => database.RunCommand<BsonDocument>(CreateInsertCommand(43)));
            exception.Should().BeNull();

            eventCapturer.Next().Should().BeOfType<ConnectionCreatedEvent>();
            eventCapturer.Next().Should().BeOfType<ConnectionReadingPendingResponseEvent>();
            eventCapturer.Next().Should().BeOfType<ConnectionReadPendingResponseEvent>();
            eventCapturer.Any().Should().BeFalse();
        }
    }

    private (IMongoClient client, EventCapturer eventCapturer) CreateClient()
    {
        var eventCapturer = new EventCapturer()
            .Capture<ConnectionReadingPendingResponseEvent>()
            .Capture<ConnectionReadPendingResponseEvent>()
            .Capture<ConnectionReadingPendingResponseFailedEvent>()
            .Capture<ConnectionClosedEvent>()
            .Capture<ConnectionCreatedEvent>();
        var settings = DriverTestConfiguration.GetClientSettings();
        settings.RetryReads = false;
        settings.RetryWrites = false;
        settings.MinConnectionPoolSize = 0;
        settings.MaxConnectionPoolSize = 1;
        settings.ServerMonitoringMode = ServerMonitoringMode.Poll;
        settings.ClusterConfigurator = (builder) => builder.Subscribe(eventCapturer);
        return (DriverTestConfiguration.CreateMongoClient(settings), eventCapturer);
    }

    private BsonDocument CreateInsertCommand(int documentId) =>
        new()
        {
            { "insert", "coll" },
            { "documents", BsonArray.Create(new[] { new BsonDocument { { "_id", documentId } } }) }
        };

    private BsonDocument CreateTestProxyDocument(int sendBytes) =>
        new()
        {
            { "actions", BsonArray.Create(new[]
            {
                new BsonDocument("sendBytes", 2),
                new BsonDocument("delayMs", 400),
                new BsonDocument("sendAll", true)
            }) }
        };

}

