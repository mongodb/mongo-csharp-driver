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
using MongoDB.Bson;
using MongoDB.Driver.Core.TestHelpers.Logging;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Specifications.socks5_support
{
    [Trait("Category", "Integration")]
    public class Socks5SupportProseTests(ITestOutputHelper testOutputHelper) : LoggableTestClass(testOutputHelper)
    {
        [Theory]
        [InlineData("mongodb://<mappedhost>/?proxyHost=localhost&proxyPort=1080&directConnection=true", false, false)]
        [InlineData("mongodb://<mappedhost>/?proxyHost=localhost&proxyPort=1080&directConnection=true", false, true)]
        [InlineData("mongodb://<mappedhost>/?proxyHost=localhost&proxyPort=1081&directConnection=true", true, false)]
        [InlineData("mongodb://<mappedhost>/?proxyHost=localhost&proxyPort=1081&directConnection=true", true, true)]
        [InlineData("mongodb://<replicaset>/?proxyHost=localhost&proxyPort=1080", false, false)]
        [InlineData("mongodb://<replicaset>/?proxyHost=localhost&proxyPort=1080", false, true)]
        [InlineData("mongodb://<replicaset>/?proxyHost=localhost&proxyPort=1081", true, false)]
        [InlineData("mongodb://<replicaset>/?proxyHost=localhost&proxyPort=1081", true, true)]
        [InlineData("mongodb://<mappedhost>/?proxyHost=localhost&proxyPort=1080&proxyUsername=nonexistentuser&proxyPassword=badauth&directConnection=true", false, false)]
        [InlineData("mongodb://<mappedhost>/?proxyHost=localhost&proxyPort=1080&proxyUsername=nonexistentuser&proxyPassword=badauth&directConnection=true", false, true)]
        [InlineData("mongodb://<mappedhost>/?proxyHost=localhost&proxyPort=1081&proxyUsername=nonexistentuser&proxyPassword=badauth&directConnection=true", true, false)]
        [InlineData("mongodb://<mappedhost>/?proxyHost=localhost&proxyPort=1081&proxyUsername=nonexistentuser&proxyPassword=badauth&directConnection=true", true, true)]
        [InlineData("mongodb://<replicaset>/?proxyHost=localhost&proxyPort=1081&proxyUsername=nonexistentuser&proxyPassword=badauth", true, false)]
        [InlineData("mongodb://<replicaset>/?proxyHost=localhost&proxyPort=1081&proxyUsername=nonexistentuser&proxyPassword=badauth", true, true)]
        [InlineData("mongodb://<mappedhost>/?proxyHost=localhost&proxyPort=1080&proxyUsername=username&proxyPassword=p4ssw0rd&directConnection=true", true, false)]
        [InlineData("mongodb://<mappedhost>/?proxyHost=localhost&proxyPort=1080&proxyUsername=username&proxyPassword=p4ssw0rd&directConnection=true", true, true)]
        [InlineData("mongodb://<mappedhost>/?proxyHost=localhost&proxyPort=1081&directConnection=true", true, false)]
        [InlineData("mongodb://<mappedhost>/?proxyHost=localhost&proxyPort=1081&directConnection=true", true, true)]
        [InlineData("mongodb://<replicaset>/?proxyHost=localhost&proxyPort=1080&proxyUsername=username&proxyPassword=p4ssw0rd", true, false)]
        [InlineData("mongodb://<replicaset>/?proxyHost=localhost&proxyPort=1080&proxyUsername=username&proxyPassword=p4ssw0rd", true, true)]
        [InlineData("mongodb://<replicaset>/?proxyHost=localhost&proxyPort=1081", true, false)]
        [InlineData("mongodb://<replicaset>/?proxyHost=localhost&proxyPort=1081", true, true)]
        public async Task TestConnectionStrings(string connectionString, bool expectedResult, bool async)
        {
            connectionString = connectionString.Replace("<mappedhost>", "localhost:27017").Replace("<replicaset>", "localhost:27017");
            var mongoClientSettings = MongoClientSettings.FromConnectionString(connectionString);
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
}