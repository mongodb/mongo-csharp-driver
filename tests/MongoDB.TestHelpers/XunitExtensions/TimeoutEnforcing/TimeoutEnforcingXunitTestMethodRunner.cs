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

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace MongoDB.TestHelpers.XunitExtensions.TimeoutEnforcing
{
    [DebuggerStepThrough]
    internal sealed class TimeoutEnforcingXunitTestMethodRunner : XunitTestMethodRunner
    {
        private readonly object[] _constructorArguments;
        private readonly IMessageSink _diagnosticMessageSink;

        public TimeoutEnforcingXunitTestMethodRunner(ITestMethod testMethod, IReflectionTypeInfo @class, IReflectionMethodInfo method, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageBus messageBus, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource, object[] constructorArguments) : base(testMethod, @class, method, testCases, diagnosticMessageSink, messageBus, aggregator, cancellationTokenSource, constructorArguments)
        {
            _constructorArguments = constructorArguments;
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        protected override async Task<RunSummary> RunTestCaseAsync(IXunitTestCase originalTestCase)
        {
            var messageBusInterceptor = new SkippableTestMessageBus(MessageBus);

            var isTheory = originalTestCase is XunitTheoryTestCase;
            XunitTestCaseRunner testRunner = isTheory ?
                new TimeoutEnforcingXunitTheoryTestCaseRunner(originalTestCase, originalTestCase.DisplayName, originalTestCase.SkipReason, _constructorArguments, _diagnosticMessageSink, messageBusInterceptor, new ExceptionAggregator(Aggregator), CancellationTokenSource) :
                new TimeoutEnforcingXunitTestCaseRunner(originalTestCase, originalTestCase.DisplayName, originalTestCase.SkipReason, _constructorArguments, originalTestCase.TestMethodArguments, messageBusInterceptor, new ExceptionAggregator(Aggregator), CancellationTokenSource);

            var result = await testRunner.RunAsync();

            result.Failed -= messageBusInterceptor.SkippedCount;
            result.Skipped += messageBusInterceptor.SkippedCount;

            return result;
        }
    }
}
