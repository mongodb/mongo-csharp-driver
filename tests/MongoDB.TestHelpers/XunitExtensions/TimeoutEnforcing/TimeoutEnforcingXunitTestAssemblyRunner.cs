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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace MongoDB.TestHelpers.XunitExtensions.TimeoutEnforcing
{
    [DebuggerStepThrough]
    internal sealed class TimeoutEnforcingXunitTestAssemblyRunner : XunitTestAssemblyRunner
    {
        private readonly UnobservedExceptionTrackingTestCase _unobservedExceptionTrackingTestCase;

        public TimeoutEnforcingXunitTestAssemblyRunner(
            ITestAssembly testAssembly,
            IEnumerable<IXunitTestCase> testCases,
            IMessageSink diagnosticMessageSink,
            IMessageSink executionMessageSink,
            ITestFrameworkExecutionOptions executionOptions)
            : base(testAssembly, testCases.Where(t => t is not UnobservedExceptionTrackingTestCase), diagnosticMessageSink, executionMessageSink, executionOptions)
        {
            _unobservedExceptionTrackingTestCase = (UnobservedExceptionTrackingTestCase)testCases.SingleOrDefault(t => t is UnobservedExceptionTrackingTestCase);
        }

        protected override Task<RunSummary> RunTestCollectionAsync(
            IMessageBus messageBus,
            ITestCollection testCollection,
            IEnumerable<IXunitTestCase> testCases,
            CancellationTokenSource cancellationTokenSource)
        {
            return new TimeoutEnforcingXunitTestCollectionRunner(testCollection, testCases, DiagnosticMessageSink, messageBus, TestCaseOrderer, new ExceptionAggregator(Aggregator), cancellationTokenSource).RunAsync();
        }

        protected override async Task<RunSummary> RunTestCollectionsAsync(IMessageBus messageBus, CancellationTokenSource cancellationTokenSource)
        {
            var baseSummary = await base.RunTestCollectionsAsync(messageBus, cancellationTokenSource);

            if (_unobservedExceptionTrackingTestCase == null)
            {
                return baseSummary;
            }

            var unobservedExceptionTestCaseRunSummary = await RunTestCollectionAsync(
                messageBus,
                _unobservedExceptionTrackingTestCase.TestMethod.TestClass.TestCollection,
                [_unobservedExceptionTrackingTestCase],
                cancellationTokenSource);

            return new RunSummary
            {
                Total = baseSummary.Total + unobservedExceptionTestCaseRunSummary.Total,
                Failed = baseSummary.Failed + unobservedExceptionTestCaseRunSummary.Failed,
                Skipped = baseSummary.Skipped + unobservedExceptionTestCaseRunSummary.Skipped,
                Time = baseSummary.Time + unobservedExceptionTestCaseRunSummary.Time
            };
        }
    }
}
