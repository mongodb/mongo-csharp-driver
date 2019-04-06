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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp2564Tests
    {
        [SkippableFact]
        public async Task Misbehaved_async_method_should_not_deadlock_server_selection()
        {
            RequireServer.Check().ClusterType(ClusterType.ReplicaSet).VersionGreaterThanOrEqualTo("3.6");

            // note: the code below deadlocks because await StartSessionAsync continues when UpdateClusterDescription in Cluster called TrySetResult after finding the secondary
            // but then the sync call to RunCommand blocks waiting for a primary and the call to TrySetResult never returns
            // which in turn prevents SDAM from unwinding back to process the next queued heartbeat event so the primary is never found

            var primary = CoreTestConfiguration.Cluster.Description.Servers.Where(s => s.Type == ServerType.ReplicaSetPrimary).Single();
            void clusterConfigurator(ClusterBuilder builder)
            {
                builder.Subscribe((ServerHeartbeatSucceededEvent heartbeatEvent) =>
                {
                    // ensure that the primary heartbeat is the last to be processed by introducing a small artificial delay
                    if (EndPointHelper.Equals(heartbeatEvent.ConnectionId.ServerId.EndPoint, primary.EndPoint))
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    }
                });
            }

            using (var client = DriverTestConfiguration.CreateDisposableClient(clusterConfigurator))
            {
                using (var session = await client.StartSessionAsync().ConfigureAwait(false))
                {
                    var adminDatabase = client.GetDatabase("admin");
                    adminDatabase.RunCommand<BsonDocument>(session, "{ ping : 1 }"); // this async method is misbehaving by calling a blocking sync method
                }
            }
        }
    }
}
