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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public sealed class JsonDrivenAssertSessionPinnedTest : JsonDrivenTestRunnerTest
    {
        public JsonDrivenAssertSessionPinnedTest(IJsonDrivenTestRunner testRunner, Dictionary<string, object> objectMap)
            : base(testRunner, objectMap)
        {
        }

        public override void Act(CancellationToken cancellationToken)
        {
        }

        public override void Assert()
        {
            GetPinnedServer().Should().NotBeNull();
        }
    }
}
