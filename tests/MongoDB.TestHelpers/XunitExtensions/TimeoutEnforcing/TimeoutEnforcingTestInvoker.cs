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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace MongoDB.TestHelpers.XunitExtensions.TimeoutEnforcing
{
    [DebuggerStepThrough]
    internal sealed class TimeoutEnforcingTestInvoker : XunitTestInvoker
    {
        // This is a copy of MongoDB.Driver.Core.Misc.TaskExtensions.YieldNoContextAwaitable struct
        // Remove this copy when moving TaskExtensions to BSON level.
        private struct YieldNoContextAwaitable
        {
            public YieldNoContextAwaiter GetAwaiter() { return new YieldNoContextAwaiter(); }

            public struct YieldNoContextAwaiter : ICriticalNotifyCompletion
            {
                /// <summary>Gets whether a yield is not required.</summary>
                /// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
                public bool IsCompleted { get { return false; } } // yielding is always required for YieldNoContextAwaiter, hence false

                public void OnCompleted(Action continuation)
                {
                    Task.Factory.StartNew(continuation, default, TaskCreationOptions.PreferFairness, TaskScheduler.Default);
                }

                public void UnsafeOnCompleted(Action continuation)
                {
                    Task.Factory.StartNew(continuation, default, TaskCreationOptions.PreferFairness, TaskScheduler.Default);
                }

                public void GetResult()
                {
                    // no op
                }
            }
        }

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
            await new YieldNoContextAwaitable();

            return await base.InvokeTestMethodAsync(testClassInstance);
        }

        protected override async Task<decimal> InvokeTestMethodAsync(object testClassInstance)
        {
            var xUnitTestCase = Test.TestCase as IXunitTestCase;
            var timeoutMS = xUnitTestCase?.Timeout ?? 0;
            var timeout = Debugger.IsAttached
                ? Timeout.InfiniteTimeSpan // allow more flexible debugging expirience
                : timeoutMS <= 0 ? XunitExtensionsConstants.DefaultTestTimeout : TimeSpan.FromMilliseconds(timeoutMS);


            var testExceptionHandler = testClassInstance as ITestExceptionHandler;

            decimal result;
            try
            {
                var baseTask = InvokeBaseOnTaskScheduler(testClassInstance);
                var resultTask = await Task.WhenAny(baseTask, Task.Delay(timeout));

                if (resultTask != baseTask)
                {
                    throw new TestTimeoutException((int)timeout.TotalMilliseconds);
                }

                if (Aggregator.HasExceptions && testExceptionHandler != null)
                {
                    var exception = Aggregator.ToException();

                    if (exception is not SkipException)
                    {
                        testExceptionHandler.HandleException(exception);
                    }
                }

                result = await baseTask;
            }
            catch (Exception exception)
            {
                testExceptionHandler?.HandleException(exception);

                throw;
            }

            return result;
        }
    }
}
