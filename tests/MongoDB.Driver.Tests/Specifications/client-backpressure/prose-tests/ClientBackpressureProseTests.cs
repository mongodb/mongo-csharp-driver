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
using System.Diagnostics;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.client_backpressure.prose_tests;

public class ClientBackpressureProseTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task OperationRetryUsesExponentialBackoff()
    {
        var client = DriverTestConfiguration.Client;
        var collection = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
            .GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);
        await ConfigureFailPointAsync(client);

        Stopwatch sw;
        double noBackoffTime;
        double withBackoffTime;

        using (ConfigureJitter(0))
        {
            sw = Stopwatch.StartNew();
            Assert.Throws<MongoCommandException>(() => collection.InsertOne(new BsonDocument("a", 1)));
            sw.Stop();
            noBackoffTime = sw.Elapsed.TotalSeconds;
        }

        using (ConfigureJitter(1))
        {
            sw.Restart();
            Assert.Throws<MongoCommandException>(() => collection.InsertOne(new BsonDocument("a", 1)));
            sw.Stop();
            withBackoffTime = sw.Elapsed.TotalSeconds;
        }

        Assert.True(withBackoffTime - noBackoffTime >= 2.1, $"Expected at least 2.1s difference, got {withBackoffTime - noBackoffTime:F2}s");
    }

    private static IDisposable ConfigureJitter(int value)
    {
        return DefaultRandom.SetDoubleValueForTesting(value);
    }

    private static async Task ConfigureFailPointAsync(IMongoClient client)
    {
        var adminDb = client.GetDatabase("admin");
        var command = new BsonDocument
        {
            { "configureFailPoint", "failCommand" },
            { "mode", "alwaysOn" },
            { "data", new BsonDocument
                {
                    { "failCommands", new BsonArray { "insert" } },
                    { "errorCode", 2 },
                    { "errorLabels", new BsonArray(new[] { "SystemOverloadedError", "RetryableError" }) }
                }
            }
        };
        await adminDb.RunCommandAsync<BsonDocument>(command);
    }
}
