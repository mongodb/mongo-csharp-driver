/* Copyright 2010-present MongoDB Inc.
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
using Xunit.Abstractions;
using Xunit.Sdk;

namespace MongoDB.TestHelpers.XunitExtensions.TimeoutEnforcing
{
    internal sealed class SkippableTestMessageBus : IMessageBus
    {
        private readonly static string __skippableExceptionName = typeof(SkipException).FullName;

        private readonly IMessageBus _messageBus;
        private int _skippedCount;

        public SkippableTestMessageBus(IMessageBus messageBus)
        {
            if (messageBus == null)
            {
                throw new ArgumentNullException(nameof(messageBus));
            }

            _messageBus = messageBus;
        }

        public int SkippedCount => _skippedCount;

        public void Dispose()
        {
            _messageBus.Dispose();
        }

        /// <inheritdoc />
        public bool QueueMessage(IMessageSinkMessage message)
        {
            var failed = message as TestFailed;
            if (message is TestFailed testFailed &&
                testFailed.ExceptionTypes.FirstOrDefault() == __skippableExceptionName)
            {
                _skippedCount++;
                return _messageBus.QueueMessage(new TestSkipped(failed.Test, failed.Messages[0]));
            }

            return _messageBus.QueueMessage(message);
        }
    }
}
