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
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Specifications.socks5_support;

[Trait("Category", "Integration")]
[Trait("Category", "Socks5Proxy")]
public class Socks5SupportProseTests(ITestOutputHelper testOutputHelper)
    : LoggableTestClass(testOutputHelper)
{
    public static IEnumerable<object[]> GetTestCombinations()
    {
        var testCases = new (string ConnectionString, bool ExpectedResult)[]
        {
            ("mongodb://<mappedhost>/?proxyHost=localhost&proxyPort=1080&directConnection=true", false),
            ("mongodb://<mappedhost>/?proxyHost=localhost&proxyPort=1081&directConnection=true", true),
            ("mongodb://<replicaset>/?proxyHost=localhost&proxyPort=1080", false),
            ("mongodb://<replicaset>/?proxyHost=localhost&proxyPort=1081", true),
            ("mongodb://<mappedhost>/?proxyHost=localhost&proxyPort=1080&proxyUsername=nonexistentuser&proxyPassword=badauth&directConnection=true", false),
            ("mongodb://<mappedhost>/?proxyHost=localhost&proxyPort=1081&proxyUsername=nonexistentuser&proxyPassword=badauth&directConnection=true", true),
            ("mongodb://<replicaset>/?proxyHost=localhost&proxyPort=1081&proxyUsername=nonexistentuser&proxyPassword=badauth", true),
            ("mongodb://<mappedhost>/?proxyHost=localhost&proxyPort=1080&proxyUsername=username&proxyPassword=p4ssw0rd&directConnection=true", true),
            ("mongodb://<mappedhost>/?proxyHost=localhost&proxyPort=1081&directConnection=true", true),
            ("mongodb://<replicaset>/?proxyHost=localhost&proxyPort=1080&proxyUsername=username&proxyPassword=p4ssw0rd", true),
            ("mongodb://<replicaset>/?proxyHost=localhost&proxyPort=1081", true)
        };

        var index = 0;
        foreach (var (connectionString, expectedResult) in testCases)
        {
            foreach (var useTls in new[] { true, false })
            {
                foreach (var isAsync in new[] { true, false })
                {
                    var id = $"{index++}_{(useTls ? "Tls" : "NoTls")}_{(isAsync ? "Async" : "Sync")}";
                    yield return [id, connectionString, expectedResult, useTls, isAsync];
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(GetTestCombinations))]
    public async Task TestConnectionStrings(string id, string connectionString, bool expectedResult, bool useTls, bool async)
    {
        RequireServer.Check().Tls(useTls);
        RequireEnvironment.Check().EnvironmentVariable("SOCKS5_PROXY_SERVERS_ENABLED");

        var isMappedHost = connectionString.Contains("<mappedhost>");

        List<(string Host, int Port)> actualHosts;

        if (isMappedHost)
        {
            connectionString = connectionString.Replace("<mappedhost>", "localhost:12345");
            actualHosts = [("localhost", 12345)];
        }
        else
        {
            //Convert the hosts to a format that can be used in the connection string (host:port), and join them into a string.
            actualHosts = CoreTestConfiguration.ConnectionString.Hosts.Select(h => h.GetHostAndPort()).ToList();
            var stringHosts = string.Join(",", actualHosts.Select( h => $"{h.Host}:{h.Port}"));
            connectionString = connectionString.Replace("<replicaset>", stringHosts);
        }

        var eventList = new List<CommandStartedEvent>();
        var mongoClientSettings = MongoClientSettings.FromConnectionString(connectionString);
        mongoClientSettings.UseTls = useTls;
        mongoClientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(1.5);
        mongoClientSettings.ClusterConfigurator = cb =>
        {
            cb.Subscribe<CommandStartedEvent>(eventList.Add);
        };

        var client = new MongoClient(mongoClientSettings);
        var database = client.GetDatabase("admin");

        var command = new BsonDocument("hello", 1);

        if (expectedResult)
        {
            var result = async
                ? await database.RunCommandAsync<BsonDocument>(command)
                : database.RunCommand<BsonDocument>(command);

            Assert.NotEmpty(result);
            AssertEventListDoesNotContainSocks5Proxy(actualHosts, eventList);
        }
        else
        {
            var exception = async
                ? await Record.ExceptionAsync(() => database.RunCommandAsync<BsonDocument>(command))
                : Record.Exception(() => database.RunCommand<BsonDocument>(command));

            Assert.IsType<TimeoutException>(exception);
        }
    }

    private static void AssertEventListDoesNotContainSocks5Proxy(List<(string Host, int Port)> hosts, List<CommandStartedEvent> eventList)
    {
        var proxyHosts = new List<(string Host, int Port)>
        {
            ("localhost", 1080),
            ("localhost", 1081)
        };

        var endPointsSeen = eventList
            .Select(e => e.ConnectionId.ServerId.EndPoint.GetHostAndPort())
            .ToList();

        Assert.DoesNotContain(endPointsSeen, proxyHosts.Contains);
        Assert.Contains(endPointsSeen, hosts.Contains);
    }
}