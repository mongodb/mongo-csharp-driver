/* Copyright 2021-present MongoDB Inc.
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
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers;

namespace MongoDB.Driver.TestHelpers
{
    public interface ITestClientsProvider
    {
        CoreEnvironmentConfiguration CoreEnvironmentConfiguration { get; }
        MongoClient CreateClient(Action<MongoClientSettings> clientSettingsConfigurator = null, bool useMultipleShardRouters = false);
        MongoClientSettings GetClientSettings();
    }

    public class TestClientsProvider : ITestClientsProvider
    {
        private readonly ClusterType _clusterType;
        private readonly string _defaultConnectionString;
        private readonly CoreEnvironmentConfiguration _coreEnvironmentConfiguration;

        public TestClientsProvider(CoreEnvironmentConfiguration coreEnvironmentConfiguration, ClusterType clusterType)
        {
            _clusterType = clusterType;
            _coreEnvironmentConfiguration = Ensure.IsNotNull(coreEnvironmentConfiguration, nameof(coreEnvironmentConfiguration));
            _defaultConnectionString = _coreEnvironmentConfiguration.DefaultConnectionString.ToString();
        }

        public CoreEnvironmentConfiguration CoreEnvironmentConfiguration => _coreEnvironmentConfiguration;

        public MongoClientSettings GetClientSettings()
        {
            var connectionString = _defaultConnectionString;
            var clientSettings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));

            clientSettings.ServerSelectionTimeout = TimeSpan.FromMilliseconds(_coreEnvironmentConfiguration.DefaultServerSelection);
            clientSettings.ClusterConfigurator = cb =>
            {
                if (ClusterBuilderHelper.TryCreateTraceSourceIfConfigured(_coreEnvironmentConfiguration, out var traceSource))
                {
                    cb = ClusterBuilderHelper.ConfigureLogging(cb, traceSource);
                }
            };
            clientSettings.ServerApi = _coreEnvironmentConfiguration.ServerApi;

            return clientSettings;
        }

        public MongoClient CreateClient(Action<MongoClientSettings> clientSettingsConfigurator = null, bool useMultipleShardRouters = false)
        {
            if (_clusterType != ClusterType.Sharded && _clusterType != ClusterType.LoadBalanced)
            {
                // This option has no effect for non-sharded/load balanced topologies.
                useMultipleShardRouters = false;
            }

            var connectionString = useMultipleShardRouters
                ? _coreEnvironmentConfiguration.MultipleShardRoutersConnectionString.ToString()
                : _coreEnvironmentConfiguration.DefaultConnectionString.ToString();
            var clientSettings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));
            clientSettings.ServerApi = _coreEnvironmentConfiguration.ServerApi;
            clientSettingsConfigurator?.Invoke(clientSettings);

            return new MongoClient(clientSettings);
        }
    }
}
