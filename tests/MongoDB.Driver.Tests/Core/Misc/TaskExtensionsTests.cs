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
#if !NET6_0_OR_GREATER
using System.Reflection;
#endif
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Misc
{
    public class TaskExtensionsTests
    {
        [Theory]
        [ParameterAttributeData]
        public async Task Wait_should_throw_on_negative_timeout([Values(true, false)] bool isPromiseTask, [Values(true, false)] bool async)
        {
            var task = CreateSubject(isPromiseTask);

            var exception = async ?
                await Record.ExceptionAsync(() => task.WaitAsync(TimeSpan.FromSeconds(-42), CancellationToken.None)) :
                Record.Exception(() => task.WaitTask(TimeSpan.FromSeconds(-42), CancellationToken.None));

            exception.Should().BeOfType<ArgumentOutOfRangeException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Wait_should_work_for_task([Values(true, false)] bool isPromiseTask, [Values(true, false)] bool async)
        {
            var task = CreateSubject(isPromiseTask);

            if (async)
            {
                await task.WaitAsync(Timeout.InfiniteTimeSpan, CancellationToken.None);
            }
            else
            {
                task.WaitTask(Timeout.InfiniteTimeSpan, CancellationToken.None);
            }

            task.IsCompleted.Should().BeTrue();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Wait_should_rethrow_for_failed_task([Values(true, false)] bool isPromiseTask, [Values(true, false)] bool async)
        {
            var ex = new InvalidOperationException();
            var task = CreateSubject(isPromiseTask, ex);

            var exception = async ?
                await Record.ExceptionAsync(() => task.WaitAsync(Timeout.InfiniteTimeSpan, CancellationToken.None)) :
                Record.Exception(() => task.WaitTask(Timeout.InfiniteTimeSpan, CancellationToken.None));

            exception.Should().Be(ex);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Wait_should_throw_on_cancellation([Values(true, false)] bool async)
        {
            var task = CreateSubject(true);
            using var cts = new CancellationTokenSource(5);

            var exception = async ?
                await Record.ExceptionAsync(() => task.WaitAsync(Timeout.InfiniteTimeSpan, cts.Token)) :
                Record.Exception(() => task.WaitTask(Timeout.InfiniteTimeSpan, cts.Token));

            task.IsCompleted.Should().BeFalse();
            if (async)
            {
                exception.Should().BeOfType<TaskCanceledException>();
            }
            else
            {
                exception.Should().BeOfType<OperationCanceledException>();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Wait_should_throw_on_timeout([Values(true, false)] bool async)
        {
            var task = CreateSubject(true);

            var exception = async ?
                await Record.ExceptionAsync(() => task.WaitAsync(TimeSpan.FromMilliseconds(5), CancellationToken.None)) :
                Record.Exception(() => task.WaitTask(TimeSpan.FromMilliseconds(5), CancellationToken.None));

            task.IsCompleted.Should().BeFalse();
            exception.Should().BeOfType<TimeoutException>();
        }

#if !NET6_0_OR_GREATER
        [Theory]
        [ParameterAttributeData]
        public async Task WaitAsync_should_release_cancellation_token_registration_when_task_completes([Values(true, false)] bool generic)
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            var taskCompletionSource = new TaskCompletionSource<int>();
            Task waitTask = generic ?
                taskCompletionSource.Task.WaitAsync(Timeout.InfiniteTimeSpan, cancellationTokenSource.Token) :
                ((Task)taskCompletionSource.Task).WaitAsync(Timeout.InfiniteTimeSpan, cancellationTokenSource.Token);

            GetCancellationCallbackCount(cancellationTokenSource).Should().Be(1);

            taskCompletionSource.SetResult(42);
            await waitTask;

            GetCancellationCallbackCount(cancellationTokenSource).Should().Be(0);
        }
#endif

        [Theory]
        [ParameterAttributeData]
        public async Task WaitAsyncTResult_should_throw_on_negative_timeout([Values(true, false)] bool isPromiseTask)
        {
            var task = CreateSubject(42, isPromiseTask);

            var exception = await Record.ExceptionAsync(() => task.WaitAsync(TimeSpan.FromSeconds(-42), CancellationToken.None));

            exception.Should().BeOfType<ArgumentOutOfRangeException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task WaitAsyncTResult_should_not_throw_on_infinite_timeout([Values(true, false)] bool isPromiseTask)
        {
            var task = CreateSubject(42, isPromiseTask);

            await task.WaitAsync(Timeout.InfiniteTimeSpan, CancellationToken.None);

            task.IsCompleted.Should().BeTrue();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task WaitAsyncTResult_should_work_for_task([Values(true, false)] bool isPromiseTask)
        {
            var task = CreateSubject(42, isPromiseTask);

            var result = await task.WaitAsync(Timeout.InfiniteTimeSpan, CancellationToken.None);

            result.Should().Be(42);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task WaitAsyncTResult_should_rethrow_for_failed_task([Values(true, false)] bool isPromiseTask)
        {
            var ex = new InvalidOperationException();
            var task = CreateSubject(42, isPromiseTask, ex);

            var exception = await Record.ExceptionAsync(() => task.WaitAsync(Timeout.InfiniteTimeSpan, CancellationToken.None));

            exception.Should().Be(ex);
        }

        [Fact]
        public async Task WaitAsyncTResult_should_throw_on_cancellation()
        {
            var task = CreateSubject(42, true);
            using var cts = new CancellationTokenSource(5);

            var exception = await Record.ExceptionAsync(() => task.WaitAsync(Timeout.InfiniteTimeSpan, cts.Token));

            task.IsCompleted.Should().BeFalse();
            exception.Should().BeOfType<TaskCanceledException>();
        }

        [Fact]
        public async Task WaitAsyncTResult_should_throw_on_timeout()
        {
            var task = CreateSubject(42, true);

            var exception = await Record.ExceptionAsync(() => task.WaitAsync(TimeSpan.FromMilliseconds(5), CancellationToken.None));

            task.IsCompleted.Should().BeFalse();
            exception.Should().BeOfType<TimeoutException>();
        }

        private Task CreateSubject(bool isPromise, Exception exception = null)
        {
            if (exception == null)
            {
                return isPromise ? Task.Delay(50) : Task.CompletedTask;
            }

            return isPromise ?
                    Task.Delay(50).ContinueWith(_ => throw exception) :
                    Task.FromException(exception);
        }

        private Task<TResult> CreateSubject<TResult>(TResult result, bool isPromise, Exception exception = null)
        {
            var tcs = new TaskCompletionSource<TResult>();
            if (isPromise)
            {
                Task.Delay(50).ContinueWith(_ =>
                {
                    if (exception == null)
                    {
                        tcs.TrySetResult(result);
                    }
                    else
                    {
                        tcs.SetException(exception);
                    }
                });
            }

            if (exception == null)
            {
                return isPromise ? tcs.Task : Task.FromResult(result);
            }

            return isPromise ? tcs.Task : Task.FromException<TResult>(exception);
        }

#if !NET6_0_OR_GREATER
        private static int GetCancellationCallbackCount(CancellationTokenSource cancellationTokenSource)
        {
            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            var callbackListsField = typeof(CancellationTokenSource).GetField("m_registeredCallbacksLists", bindingFlags);
            var callbackLists = (Array)callbackListsField.GetValue(cancellationTokenSource);
            if (callbackLists == null)
            {
                return 0;
            }

            var count = 0;
            foreach (var callbackList in callbackLists)
            {
                if (callbackList == null)
                {
                    continue;
                }

                var fragment = callbackList.GetType().GetField("m_head", bindingFlags).GetValue(callbackList);
                while (fragment != null)
                {
                    var fragmentType = fragment.GetType();
                    var callbacks = (Array)fragmentType.GetField("m_elements", bindingFlags).GetValue(fragment);
                    foreach (var callback in callbacks)
                    {
                        if (callback != null)
                        {
                            count++;
                        }
                    }

                    fragment = fragmentType.GetField("m_next", bindingFlags).GetValue(fragment);
                }
            }

            return count;
        }
#endif
    }
}

