/* Copyright 2019-present MongoDB Inc.
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
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.TestHelpers;

namespace MongoDB.Driver.Tests
{
    internal interface IJsonDrivenTestRunner
    {
        IClusterInternal FailPointCluster { get; }
        void ConfigureFailPoint(IServerSelector serverSelector, BsonDocument failCommand);
        Task ConfigureFailPointAsync(IServerSelector serverSelector, BsonDocument failCommand);
    }

    internal sealed class JsonDrivenTestRunner : IJsonDrivenTestRunner, IDisposable
    {
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public IClusterInternal FailPointCluster
        {
            get
            {
                var regularClient = DriverTestConfiguration.Client;
                var client = regularClient.Cluster.Description.Type == ClusterType.Sharded
                    ? DriverTestConfiguration.ClientWithMultipleShardRouters
                    : regularClient;
                return client.GetClusterInternal();
            }
        }

        public void ConfigureFailPoint(IServerSelector serverSelector, BsonDocument failCommand)
        {
            var failPoint = FailPoint.Configure(serverSelector, failCommand, withAsync: false, cluster: FailPointCluster);
            _disposables.Add(failPoint);
        }

        public async Task ConfigureFailPointAsync(IServerSelector serverSelector, BsonDocument failCommand)
        {
            var failPoint = await Task.Run(() => FailPoint.Configure(serverSelector, failCommand, withAsync: true, cluster: FailPointCluster)).ConfigureAwait(false);
            _disposables.Add(failPoint);
        }

        public void Dispose() => _disposables.ForEach(x => x.Dispose());
    }
}
