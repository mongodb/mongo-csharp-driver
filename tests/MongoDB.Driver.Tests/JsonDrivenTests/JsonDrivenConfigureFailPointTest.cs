/* Copyright 2020–present MongoDB Inc.
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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public class JsonDrivenConfigureFailPointTest : JsonDrivenTestRunnerTest
    {
        protected BsonDocument _failCommand;

        public JsonDrivenConfigureFailPointTest(IJsonDrivenTestRunner testRunner, Dictionary<string, object> objectMap)
            : base(testRunner, objectMap)
        {
        }

        protected override void CallMethod(CancellationToken cancellationToken)
        {
            var server = GetFailPointServer();
            TestRunner.ConfigureFailPoint(server, NoCoreSession.NewHandle(), _failCommand);
        }

        protected override async Task CallMethodAsync(CancellationToken cancellationToken)
        {
            var server = await GetFailPointServerAsync().ConfigureAwait(false);
            await TestRunner.ConfigureFailPointAsync(server, NoCoreSession.NewHandle(), _failCommand).ConfigureAwait(false);
        }

        protected virtual IServer GetFailPointServer()
        {
            var cluster = TestRunner.FailPointCluster;
            return cluster.SelectServer(WritableServerSelector.Instance, CancellationToken.None);
        }

        protected async virtual Task<IServer> GetFailPointServerAsync()
        {
            var cluster = TestRunner.FailPointCluster;
            return await cluster.SelectServerAsync(WritableServerSelector.Instance, CancellationToken.None).ConfigureAwait(false);
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "failPoint":
                    _failCommand = (BsonDocument)value;
                    return;
            }

            base.SetArgument(name, value);
        }
    }
}
