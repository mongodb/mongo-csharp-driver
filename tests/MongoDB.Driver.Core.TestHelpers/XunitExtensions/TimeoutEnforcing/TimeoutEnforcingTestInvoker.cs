﻿/* Copyright 2021-present MongoDB Inc.
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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.TestHelpers.Logging;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace MongoDB.Driver.Core.TestHelpers.XunitExtensions.TimeoutEnforcing
{
    internal sealed class TimeoutEnforcingTestInvoker : XunitTestInvoker
    {
        public TimeoutEnforcingTestInvoker(
            ITest test,
            IMessageBus messageBus,
            Type testClass,
            object[] constructorArguments,
            MethodInfo testMethod,
            object[] testMethodArguments,
            IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
            : base(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, beforeAfterAttributes, aggregator, cancellationTokenSource)
        {
        }

        private async Task<decimal> InvokeBaseOnTaskScheduler(object testClassInstance)
        {
            await Misc.TaskExtensions.YieldNoContext();

            return await base.InvokeTestMethodAsync(testClassInstance);
        }

        protected override async Task<decimal> InvokeTestMethodAsync(object testClassInstance)
        {
            var xUnitTestCase = Test.TestCase as IXunitTestCase;
            var timeoutMS = xUnitTestCase?.Timeout ?? 0;
            var timeout = timeoutMS <= 0 ? CoreTestConfiguration.DefaultTestTimeout : TimeSpan.FromMilliseconds(timeoutMS);

            var testLoggable = testClassInstance as LoggableTestClass;

            decimal result;
            try
            {
                var baseTask = InvokeBaseOnTaskScheduler(testClassInstance);
                var resultTask = await Task.WhenAny(baseTask, Task.Delay(timeout));

                if (resultTask != baseTask)
                {
                    throw new TestTimeoutException((int)timeout.TotalMilliseconds);
                }

                if (Aggregator.HasExceptions && testLoggable != null)
                {
                    var exception = Aggregator.ToException();

                    if (exception is not SkipException)
                    {
                        testLoggable.OnException(exception);
                    }
                }

                result = await baseTask;
            }
            catch (Exception exception)
            {
                testLoggable?.OnException(exception);

                throw;
            }

            return result;
        }
    }
}
