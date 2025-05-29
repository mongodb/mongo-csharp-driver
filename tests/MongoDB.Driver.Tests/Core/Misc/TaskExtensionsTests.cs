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
        public async Task WaitAsync_should_throw_on_negative_timeout([Values(true, false)] bool isPromiseTask)
        {
            var task = CreateSubject(isPromiseTask);

            var exception = await Record.ExceptionAsync(() => task.WaitAsync(TimeSpan.FromSeconds(-42), CancellationToken.None));

            exception.Should().BeOfType<ArgumentOutOfRangeException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task WaitAsync_should_work_for_task([Values(true, false)] bool isPromiseTask)
        {
            var task = CreateSubject(isPromiseTask);

            await task.WaitAsync(Timeout.InfiniteTimeSpan, CancellationToken.None);

            task.IsCompleted.Should().BeTrue();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task WaitAsync_should_rethrow_for_failed_task([Values(true, false)] bool isPromiseTask)
        {
            var ex = new InvalidOperationException();
            var task = CreateSubject(isPromiseTask, ex);

            var exception = await Record.ExceptionAsync(() => task.WaitAsync(Timeout.InfiniteTimeSpan, CancellationToken.None));

            exception.Should().Be(ex);
        }

        [Fact]
        public async Task WaitAsync_should_throw_on_cancellation()
        {
            var task = CreateSubject(true);
            using var cts = new CancellationTokenSource(5);

            var exception = await Record.ExceptionAsync(() => task.WaitAsync(Timeout.InfiniteTimeSpan, cts.Token));

            task.IsCompleted.Should().BeFalse();
            exception.Should().BeOfType<TaskCanceledException>();
        }

        [Fact]
        public async Task WaitAsync_should_throw_on_timeout()
        {
            var task = CreateSubject(true);

            var exception = await Record.ExceptionAsync(() => task.WaitAsync(TimeSpan.FromMilliseconds(5), CancellationToken.None));

            task.IsCompleted.Should().BeFalse();
            exception.Should().BeOfType<TimeoutException>();
        }

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
    }
}

