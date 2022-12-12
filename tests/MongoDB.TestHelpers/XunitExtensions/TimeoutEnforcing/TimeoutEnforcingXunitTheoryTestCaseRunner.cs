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
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace MongoDB.TestHelpers.XunitExtensions.TimeoutEnforcing
{
    [DebuggerStepThrough]
    internal sealed class TimeoutEnforcingXunitTheoryTestCaseRunner : XunitTheoryTestCaseRunner
    {
        public TimeoutEnforcingXunitTheoryTestCaseRunner(
            IXunitTestCase testCase,
            string displayName,
            string skipReason,
            object[] constructorArguments,
            IMessageSink messageSink,
            IMessageBus messageBus,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
           : base(testCase, displayName, skipReason, constructorArguments, messageSink, messageBus, aggregator, cancellationTokenSource)
        {
        }

        protected override XunitTestRunner CreateTestRunner(ITest test, IMessageBus messageBus, Type testClass, object[] constructorArguments, MethodInfo testMethod, object[] testMethodArguments, string skipReason, IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource) =>
            new TimeoutEnforcingTestRunner(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, skipReason, beforeAfterAttributes, aggregator, cancellationTokenSource);
    }
}
