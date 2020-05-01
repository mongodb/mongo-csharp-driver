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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public class JsonDrivenAssertSessionTransactionStateTest : JsonDrivenTestRunnerTest
    {
        private CoreTransactionState _state;

        public JsonDrivenAssertSessionTransactionStateTest(IJsonDrivenTestRunner testRunner, Dictionary<string, object> objectMap)
            : base(testRunner, objectMap)
        {

        }

        // public methods
        public override void Act(CancellationToken cancellationToken)
        {
            // do nothing
        }

        public override Task ActAsync(CancellationToken cancellationToken)
        {
            // do nothing
            return Task.FromResult(true);
        }

        public override void Assert()
        {
            CoreSession.CurrentTransaction.State.Should().Be(_state);
        }

        // protected methods
        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "state":
                    _state = MapTransactionState(value.ToString());
                    break;
                default:
                    base.SetArgument(name, value);
                    break;
            }
        }

        // private methods
        private CoreTransactionState MapTransactionState(string value)
        {
            switch (value)
            {
                case "in_progress": return CoreTransactionState.InProgress;
                default:
                    return (CoreTransactionState)Enum.Parse(typeof(CoreTransactionState), value, true);
            }
        }
    }
}
