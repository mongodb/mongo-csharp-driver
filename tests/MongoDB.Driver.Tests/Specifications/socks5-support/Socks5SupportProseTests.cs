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
        /* From the Socks5 Proxy Support Prose Tests:
         *
         * Drivers MUST create a MongoClient for each of these connection strings, and attempt to run a hello command using each client.
         * The operation must succeed for table entries marked (succeeds) and fail for table entries marked (fails).
         * The connection strings MUST all be accepted as valid connection strings.
         *
         * Drivers MUST run variants of these tests in which the proxy options are substituted for MongoClient options -- This is not done as it would mostly be a repetition of the connection string tests.
         *
         * Drivers MUST verify for at least one of the connection strings marked (succeeds) that command monitoring events do not reference the SOCKS5 proxy host where the MongoDB service server/port are referenced.
         */
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

        return
            (from tc in testCases
                from useTls in new[] { true, false }
                from isAsync in new[] { true, false }
                select new { tc.ConnectionString, tc.ExpectedResult, useTls, isAsync })
            .Select((x, i) => new object[]
            {
                $"{i}_{(x.useTls ? "Tls" : "NoTls")}_{(x.isAsync ? "Async" : "Sync")}",
                x.ConnectionString,
                x.ExpectedResult,
                x.useTls,
                x.isAsync
            });
    }

    //Prose test: https://github.com/mongodb/specifications/blob/a6dbd208462d97f97c813560cac5cf25925bb0cf/source/socks5-support/tests/README.md
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
            //<mappedhost> is always replaced with localhost:12345, and it's used to verify that the test proxy server is actually used.
            //Internally localhost:12345 is mapped to the actual hosts by the test proxy server.
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
        mongoClientSettings.ClusterSource = DisposingClusterSource.Instance;
        mongoClientSettings.UseTls = useTls;
        mongoClientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(1.5);
        mongoClientSettings.ClusterConfigurator = cb =>
        {
            cb.Subscribe<CommandStartedEvent>(eventList.Add);
        };

        using var client = new MongoClient(mongoClientSettings);
        var database = client.GetDatabase("admin");

        var command = new BsonDocument("hello", 1);

        if (expectedResult)
        {
            var result = async
                ? await database.RunCommandAsync<BsonDocument>(command)
                : database.RunCommand<BsonDocument>(command);

            Assert.NotEmpty(result);
            AssertEventListContainsCorrectEndpoints(actualHosts, eventList);
        }
        else
        {
            var exception = async
                ? await Record.ExceptionAsync(() => database.RunCommandAsync<BsonDocument>(command))
                : Record.Exception(() => database.RunCommand<BsonDocument>(command));

            Assert.IsType<TimeoutException>(exception);
        }
    }

    private static void AssertEventListContainsCorrectEndpoints(List<(string Host, int Port)> hosts, List<CommandStartedEvent> eventList)
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