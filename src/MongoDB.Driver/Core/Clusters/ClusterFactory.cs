/* Copyright 2013-present MongoDB Inc.
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
using System.Linq;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Clusters
{
    internal sealed class ClusterFactory : IClusterFactory
    {
        // fields
        private readonly IEventSubscriber _eventSubscriber;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IClusterableServerFactory _serverFactory;
        private readonly ClusterSettings _settings;

        // constructors
        public ClusterFactory(ClusterSettings settings, IClusterableServerFactory serverFactory, IEventSubscriber eventSubscriber, ILoggerFactory loggerFactory)
        {
            _settings = Ensure.IsNotNull(settings, nameof(settings));
            _serverFactory = Ensure.IsNotNull(serverFactory, nameof(serverFactory));
            _eventSubscriber = Ensure.IsNotNull(eventSubscriber, nameof(eventSubscriber));
            _loggerFactory = loggerFactory;
        }

        // methods
        public IClusterInternal CreateCluster()
        {
            var settings = _settings;

            ProcessClusterEnvironment(settings);

            if (settings.LoadBalanced)
            {
                return CreateLoadBalancedCluster(settings);
            }
            else if (settings.DirectConnection)
            {
                return CreateSingleServerCluster(settings);
            }
            else
            {
                return CreateMultiServerCluster(settings);
            }
        }

        private MultiServerCluster CreateMultiServerCluster(ClusterSettings settings)
        {
            return new MultiServerCluster(settings, _serverFactory, _eventSubscriber, _loggerFactory);
        }

        private SingleServerCluster CreateSingleServerCluster(ClusterSettings settings)
        {
            return new SingleServerCluster(settings, _serverFactory, _eventSubscriber, _loggerFactory);
        }

        private LoadBalancedCluster CreateLoadBalancedCluster(ClusterSettings setting)
        {
            return new LoadBalancedCluster(setting, _serverFactory, _eventSubscriber, _loggerFactory);
        }

        private void ProcessClusterEnvironment(ClusterSettings settings)
        {
            if (_loggerFactory == null)
            {
                return;
            }

            foreach (var (host, _) in  settings.EndPoints.Select(EndPointHelper.GetHostAndPort))
            {
                if (LogIfCosmosDB(host) || LogIfDocumentDB(host))
                {
                    return;
                }
            }

            bool LogIfCosmosDB(string host) =>
                LogIfExternalEnvironment(host, "CosmosDB", "https://www.mongodb.com/supportability/cosmosdb", ".cosmos.azure.com");

            bool LogIfDocumentDB(string host) =>
                LogIfExternalEnvironment(host, "DocumentDB", "https://www.mongodb.com/supportability/documentdb", ".docdb.amazonaws.com", ".docdb-elastic.amazonaws.com");

            bool LogIfExternalEnvironment(string host, string environment, string documentationUrl, params string[] suffixes)
            {
                if (suffixes.Any(s => host.EndsWith(s, StringComparison.InvariantCultureIgnoreCase)))
                {
                    var logger = _loggerFactory.CreateLogger<LogCategories.Client>();
                    logger?.LogInformation("You appear to be connected to a {environment} cluster. For more information regarding feature compatibility and support please visit {url}", environment, documentationUrl);

                    return true;
                }

                return false;
            }
        }
    }
}
