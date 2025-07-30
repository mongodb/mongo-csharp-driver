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
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.TestHelpers.Logging;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Specifications.socks5_support;

[Trait("Category", "Integration")]
public class Socks5SupportProseTests(ITestOutputHelper testOutputHelper) : LoggableTestClass(testOutputHelper)
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

        foreach (var (connectionString, expectedResult) in testCases)
        {
            foreach (var isAsync in new[] { true, false })
            {
                foreach (var useTls in new[] { true, false })
                {
                    yield return [connectionString, expectedResult, useTls, isAsync];
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(GetTestCombinations))]
    public async Task TestConnectionStrings(string connectionString, bool expectedResult, bool useTls, bool async)
    {
        //Requires server versions > 5.0 according to spec tests, not sure why

        connectionString = connectionString.Replace("<mappedhost>", "localhost:27017").Replace("<replicaset>", "localhost:27017");
        var mongoClientSettings = MongoClientSettings.FromConnectionString(connectionString);

        if (useTls)
        {
            mongoClientSettings.UseTls = true;
            var certificate = new X509Certificate2("/Users/papafe/dataTlsEnabled/certs/mycert.pfx");
            mongoClientSettings.UseTls = true;
            mongoClientSettings.SslSettings = new SslSettings
            {
                ClientCertificates = [certificate],
                CheckCertificateRevocation = false,
                ServerCertificateValidationCallback = (sender, cert, chain, errors) => true,
            };
        }

        mongoClientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(1.5);
        var client = new MongoClient(mongoClientSettings);

        var database = client.GetDatabase("admin");
        var command = new BsonDocument("hello", 1);

        if (expectedResult)
        {
            var result = async
                ? await database.RunCommandAsync<BsonDocument>(command)
                : database.RunCommand<BsonDocument>(command);

            Assert.NotEmpty(result);
        }
        else
        {
            var exception = async
                ? await Record.ExceptionAsync(() => database.RunCommandAsync<BsonDocument>(command))
                : Record.Exception(() => database.RunCommand<BsonDocument>(command));

            Assert.IsType<TimeoutException>(exception);
        }
    }
}