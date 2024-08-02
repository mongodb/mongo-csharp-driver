/* Copyright 2013-present MongoDB Inc.
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
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Misc
{
    internal static class TaskExtensions
    {
        internal struct YieldNoContextAwaitable
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

        public static void IgnoreExceptions(this Task task)
        {
            task.ContinueWith(t => { var ignored = t.Exception; },
                TaskContinuationOptions.OnlyOnFaulted |
                TaskContinuationOptions.ExecuteSynchronously);
        }

        public static YieldNoContextAwaitable YieldNoContext() => new YieldNoContextAwaitable();
    }
}
