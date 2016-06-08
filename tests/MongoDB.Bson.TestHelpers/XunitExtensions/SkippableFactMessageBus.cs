/* Copyright 2016 MongoDB Inc.
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
using Xunit.Abstractions;
using Xunit.Sdk;

namespace MongoDB.Bson.TestHelpers.XunitExtensions
{
    internal class SkippableFactMessageBus : IMessageBus
    {
        readonly IMessageBus _innerBus;

        public SkippableFactMessageBus(IMessageBus innerBus)
        {
            _innerBus = innerBus;
        }

        public int DynamicallySkippedTestCount { get; private set; }

        public void Dispose()
        {
        }

        public bool QueueMessage(IMessageSinkMessage message)
        {
            var testFailed = message as ITestFailed;
            if (testFailed != null)
            {
                var exceptionType = testFailed.ExceptionTypes.FirstOrDefault();
                if (exceptionType == typeof(SkipTestException).FullName)
                {
                    DynamicallySkippedTestCount++;
                    return _innerBus.QueueMessage(new TestSkipped(testFailed.Test, testFailed.Messages.FirstOrDefault()));
                }
            }

            // Nothing we care about, send it on its way
            return _innerBus.QueueMessage(message);
        }
    }
}
