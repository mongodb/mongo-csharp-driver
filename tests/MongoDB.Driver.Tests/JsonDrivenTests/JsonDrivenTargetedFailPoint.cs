/* Copyright 2019–present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public sealed class JsonDrivenTargetedFailPointTest : JsonDrivenTestRunnerTest
    {
        private BsonDocument _failCommand;


        public JsonDrivenTargetedFailPointTest(IJsonDrivenTestRunner testRunner, Dictionary<string, object> objectMap)
            : base(testRunner, objectMap)
        {
        }

        protected override void CallMethod(CancellationToken cancellationToken)
        {
            var pinnedServer = GetPinnedServer();
            pinnedServer.Should().NotBeNull();
            TestRunner.ConfigureFailPoint(pinnedServer, NoCoreSession.NewHandle(), _failCommand);
        }

        protected override Task CallMethodAsync(CancellationToken cancellationToken)
        {
            var pinnedServer = GetPinnedServer();
            pinnedServer.Should().NotBeNull();
            return TestRunner.ConfigureFailPointAsync(pinnedServer, NoCoreSession.NewHandle(), _failCommand);
        }

        protected override void AssertResult()
        {
            // do nothing
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
