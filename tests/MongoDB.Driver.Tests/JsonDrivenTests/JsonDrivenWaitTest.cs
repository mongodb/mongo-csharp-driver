/* Copyright 2020-present MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public sealed class JsonDrivenWaitTest : JsonDrivenTestRunnerTest
    {
        private TimeSpan _delay;

        public JsonDrivenWaitTest(IJsonDrivenTestRunner testRunner, Dictionary<string, object> objectMap)
            : base(testRunner, objectMap)
        {
        }

        protected override void CallMethod(CancellationToken cancellationToken)
        {
            Thread.Sleep(_delay);
        }

        protected override Task CallMethodAsync(CancellationToken cancellationToken)
        {
            return Task.Delay(_delay);
        }

        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "ms":
                    _delay = TimeSpan.FromMilliseconds(value.ToInt32());
                    return;
            }

            base.SetArgument(name, value);
        }
    }
}
