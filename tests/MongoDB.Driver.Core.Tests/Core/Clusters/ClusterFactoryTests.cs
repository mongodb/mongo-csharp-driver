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

using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Core.Clusters
{
    public class ClusterFactoryTests : LoggableTestClass
    {
        private const string ExpectedCosmosDBMessage = "You appear to be connected to a CosmosDB cluster. For more information regarding feature compatibility and support please visit https://www.mongodb.com/supportability/cosmosdb";
        private const string ExpectedDocumentDBMessage = "You appear to be connected to a DocumentDB cluster. For more information regarding feature compatibility and support please visit https://www.mongodb.com/supportability/documentdb";

        public ClusterFactoryTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void ClusterFactory_should_create_cluster_when_loggerfactory_is_not_set()
        {
            const string connectionString = "mongodb://a.MONGO.COSMOS.AZURE.COM:19555";
            var subject = CreateSubject(connectionString, null);
            var cluster = subject.CreateCluster();

            cluster.Should().NotBeNull();
        }

        [Theory]
        [InlineData("mongodb://a.MONGO.COSMOS.AZURE.COM:19555", ExpectedCosmosDBMessage)]
        [InlineData("mongodb://a.MONGO.COSMOS.AZURE.COM:19555", ExpectedCosmosDBMessage)]
        [InlineData("mongodb://a.mongo.cosmos.azure.com:19555", ExpectedCosmosDBMessage)]
        [InlineData("mongodb://a.DOCDB-ELASTIC.AMAZONAWS.COM:27017/", ExpectedDocumentDBMessage)]
        [InlineData("mongodb://a.docdb-elastic.amazonaws.com:27017/", ExpectedDocumentDBMessage)]
        [InlineData("mongodb://a.DOCDB.AMAZONAWS.COM", ExpectedDocumentDBMessage)]
        [InlineData("mongodb://a.docdb.amazonaws.com", ExpectedDocumentDBMessage)]
        // SRV matching
        [InlineData("mongodb+srv://A.MONGO.COSMOS.AZURE.COM", ExpectedCosmosDBMessage)]
        [InlineData("mongodb+srv://a.mongo.cosmos.azure.com", ExpectedCosmosDBMessage)]
        [InlineData("mongodb+srv://a.DOCDB.AMAZONAWS.COM/", ExpectedDocumentDBMessage)]
        [InlineData("mongodb+srv://a.docdb.amazonaws.com/", ExpectedDocumentDBMessage)]
        [InlineData("mongodb+srv://a.DOCDB-ELASTIC.AMAZONAWS.COM/", ExpectedDocumentDBMessage)]
        [InlineData("mongodb+srv://a.docdb-elastic.amazonaws.com/", ExpectedDocumentDBMessage)]
        // Mixing internal and external hosts (unlikely in practice)
        [InlineData("mongodb://a.example.com:27017,b.mongo.cosmos.azure.com:19555/", ExpectedCosmosDBMessage)]
        [InlineData("mongodb://a.example.com:27017,b.docdb.amazonaws.com:27017/", ExpectedDocumentDBMessage)]
        [InlineData("mongodb://a.example.com:27017,b.docdb-elastic.amazonaws.com:27017/", ExpectedDocumentDBMessage)]
        // Multiple external hosts
        [InlineData("mongodb://a.docdb-elastic.amazonaws.com:27017,b.mongo.cosmos.azure.com:19555/", ExpectedDocumentDBMessage)]
        [InlineData("mongodb://a.mongo.cosmos.azure.com:19554,b.docdb-elastic.amazonaws.com:27017,c.mongo.cosmos.azure.com:19555/", ExpectedCosmosDBMessage)]
        public void ClusterFactory_should_log_if_external_environment_is_detected(string connectionString, string expectedMessage)
        {
            var subject = CreateSubject(connectionString, LoggerFactory);
            _ = subject.CreateCluster();

            var logs = GetLogs();
            logs.Length.Should().Be(1);
            logs[0].FormattedMessage.Should().Be(expectedMessage);
        }

        [Theory]
        [InlineData("mongodb://a.mongo.cosmos.azure.com.tld:19555")]
        [InlineData("mongodb://a.docdb-elastic.amazonaws.com.t")]
        [InlineData("mongodb://a.docdb-elastic.amazonaws.com.t,b.docdb-elastic.amazonaws.com.t")]
        [InlineData("mongodb+srv://a.example.com")]
        [InlineData("mongodb+srv://a.mongodb.net/")]
        [InlineData("mongodb+srv://a.mongo.cosmos.azure.com.tld/")]
        [InlineData("mongodb+srv://a.docdb-elastic.amazonaws.com.tld/")]
        public void ClusterFactory_should_not_log_if_no_external_environment_is_detected(string connectionString)
        {
            var subject = CreateSubject(connectionString, LoggerFactory);
            _ = subject.CreateCluster();

            var logs = GetLogs();
            logs.Length.Should().Be(0);
        }

        private ClusterFactory CreateSubject(string connectionString, ILoggerFactory loggerFactory)
        {
            var parsedConnectionString = new ConnectionString(connectionString);

            var eventSubscriberMock = Mock.Of<IEventSubscriber>();
            var serverFactoryMock = Mock.Of<IClusterableServerFactory>();

            var clusterSettings = new ClusterSettings(endPoints: Optional.Enumerable(parsedConnectionString.Hosts));
            var clusterFactory = new ClusterFactory(clusterSettings, serverFactoryMock, eventSubscriberMock, loggerFactory);

            return clusterFactory;
        }

        private LogEntry[] GetLogs() => Logs
            .Where(l => l.LogLevel == LogLevel.Information && l.Category == "MongoDB.Client")
            .ToArray();
    }
}
