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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class OperationContextTests
    {
        [Fact]
        public void Constructor_should_initialize_properties()
        {
            var timeout = TimeSpan.FromSeconds(42);
            var stopwatch = new Stopwatch();
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var operationContext = new OperationContext(stopwatch, timeout, cancellationToken);

            operationContext.Timeout.Should().Be(timeout);
            operationContext.RemainingTimeout.Should().Be(timeout);
            operationContext.CancellationToken.Should().Be(cancellationToken);
            operationContext.ParentContext.Should().BeNull();
        }

        [Fact]
        public void RemainingTimeout_should_calculate()
        {
            var timeout = TimeSpan.FromMilliseconds(500);
            var stopwatch = Stopwatch.StartNew();
            Thread.Sleep(10);
            stopwatch.Stop();

            var operationContext = new OperationContext(stopwatch, timeout, CancellationToken.None);

            operationContext.RemainingTimeout.Should().Be(timeout - stopwatch.Elapsed);
        }

        [Fact]
        public void RemainingTimeout_should_return_infinite_for_infinite_timeout()
        {
            var stopwatch = Stopwatch.StartNew();
            Thread.Sleep(10);
            stopwatch.Stop();

            var operationContext = new OperationContext(stopwatch, Timeout.InfiniteTimeSpan, CancellationToken.None);

            operationContext.RemainingTimeout.Should().Be(Timeout.InfiniteTimeSpan);
        }

        [Fact]
        public void RemainingTimeout_should_return_zero_for_timeout_context()
        {
            var operationContext = new OperationContext(TimeSpan.FromMilliseconds(5), CancellationToken.None);
            Thread.Sleep(10);

            operationContext.RemainingTimeout.Should().Be(TimeSpan.Zero);
        }

        [Theory]
        [MemberData(nameof(IsTimedOut_test_cases))]
        public void IsTimedOut_should_return_expected_result(bool expected, TimeSpan timeout, TimeSpan waitTime)
        {
            var stopwatch = Stopwatch.StartNew();
            Thread.Sleep(waitTime);
            stopwatch.Stop();

            var operationContext = new OperationContext(stopwatch, timeout, CancellationToken.None);
            var result = operationContext.IsTimedOut();

            result.Should().Be(expected);
        }

        public static IEnumerable<object[]> IsTimedOut_test_cases =
        [
            [false, Timeout.InfiniteTimeSpan, TimeSpan.FromMilliseconds(5)],
            [false, TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(5)],
            [true, TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(10)],
        ];

        [Fact]
        public void ThrowIfTimedOutOrCanceled_should_not_throw_if_no_timeout_and_no_cancellation()
        {
            var operationContext = new OperationContext(Timeout.InfiniteTimeSpan, CancellationToken.None);

            var exception = Record.Exception(() => operationContext.ThrowIfTimedOutOrCanceled());

            exception.Should().BeNull();
        }

        [Fact]
        public void ThrowIfTimedOutOrCanceled_throws_on_timeout()
        {
            var operationContext = new OperationContext(TimeSpan.FromMilliseconds(10), CancellationToken.None);
            Thread.Sleep(20);

            var exception = Record.Exception(() => operationContext.ThrowIfTimedOutOrCanceled());

            exception.Should().BeOfType<TimeoutException>();
        }

        [Fact]
        public void ThrowIfTimedOutOrCanceled_throws_on_cancellation()
        {
            using var cancellationSource = new CancellationTokenSource();
            var operationContext = new OperationContext(Timeout.InfiniteTimeSpan, cancellationSource.Token);
            cancellationSource.Cancel();

            var exception = Record.Exception(() => operationContext.ThrowIfTimedOutOrCanceled());

            exception.Should().BeOfType<OperationCanceledException>();
        }

        [Fact]
        public void ThrowIfTimedOutOrCanceled_throws_CancelledException_when_timedout_and_cancelled()
        {
            using var cancellationSource = new CancellationTokenSource();
            var operationContext = new OperationContext(TimeSpan.FromMilliseconds(10), cancellationSource.Token);
            Thread.Sleep(20);
            cancellationSource.Cancel();

            var exception = Record.Exception(() => operationContext.ThrowIfTimedOutOrCanceled());

            exception.Should().BeOfType<OperationCanceledException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Wait_should_throw_if_context_is_timedout([Values(true, false)] bool async)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            var operationContext = new OperationContext(TimeSpan.FromMilliseconds(10), CancellationToken.None);
            Thread.Sleep(20);

            var exception = async ?
                await Record.ExceptionAsync(() => operationContext.WaitTaskAsync(taskCompletionSource.Task)) :
                Record.Exception(() => operationContext.WaitTask(taskCompletionSource.Task));

            exception.Should().BeOfType<TimeoutException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Wait_should_throw_if_context_is_cancelled([Values(true, false)] bool async)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            var operationContext = new OperationContext(Timeout.InfiniteTimeSpan, cancellationTokenSource.Token);

            var exception = async ?
                await Record.ExceptionAsync(() => operationContext.WaitTaskAsync(taskCompletionSource.Task)) :
                Record.Exception(() => operationContext.WaitTask(taskCompletionSource.Task));

            exception.Should().BeOfType<OperationCanceledException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Wait_should_rethrow_on_failed_task([Values(true, false)] bool async)
        {
            var ex = new InvalidOperationException();
            var task = Task.FromException(ex);
            var operationContext = new OperationContext(Timeout.InfiniteTimeSpan, CancellationToken.None);

            var exception = async ?
                await Record.ExceptionAsync(() => operationContext.WaitTaskAsync(task)) :
                Record.Exception(() => operationContext.WaitTask(task));

            exception.Should().Be(ex);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Wait_should_rethrow_on_failed_promise_task([Values(true, false)] bool async)
        {
            var ex = new InvalidOperationException("Ups!");
            var taskCompletionSource = new TaskCompletionSource<bool>();
            var operationContext = new OperationContext(Timeout.InfiniteTimeSpan, CancellationToken.None);

            var task = Task.Run(async () =>
            {
                if (async)
                {
                    await operationContext.WaitTaskAsync(taskCompletionSource.Task);
                }
                else
                {
                    operationContext.WaitTask(taskCompletionSource.Task);
                }
            });
            Thread.Sleep(20);
            taskCompletionSource.SetException(ex);

            var exception = await Record.ExceptionAsync(() => task);
            exception.Should().Be(ex);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Wait_should_throw_on_timeout([Values(true, false)] bool async)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            var operationContext = new OperationContext(TimeSpan.FromMilliseconds(20), CancellationToken.None);

            var exception = async ?
                await Record.ExceptionAsync(() => operationContext.WaitTaskAsync(taskCompletionSource.Task)) :
                Record.Exception(() => operationContext.WaitTask(taskCompletionSource.Task));

            exception.Should().BeOfType<TimeoutException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Wait_should_not_throw_on_resolved_task_with_timedout_context([Values(true, false)] bool async)
        {
            var task = Task.FromResult(42);
            var operationContext = new OperationContext(TimeSpan.FromMilliseconds(10), CancellationToken.None);
            Thread.Sleep(20);

            var exception = async ?
                await Record.ExceptionAsync(() => operationContext.WaitTaskAsync(task)) :
                Record.Exception(() => operationContext.WaitTask(task));

            exception.Should().BeNull();
        }


        [Theory]
        [MemberData(nameof(WithTimeout_test_cases))]
        public void WithTimeout_should_calculate_proper_timeout(TimeSpan expected, TimeSpan originalTimeout, TimeSpan newTimeout)
        {
            var operationContext = new OperationContext(new Stopwatch(), originalTimeout, CancellationToken.None);
            var resultContext = operationContext.WithTimeout(newTimeout);

            resultContext.Timeout.Should().Be(expected);
        }

        public static IEnumerable<object[]> WithTimeout_test_cases =
        [
            [Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan],
            [TimeSpan.FromMilliseconds(5), Timeout.InfiniteTimeSpan, TimeSpan.FromMilliseconds(5)],
            [TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(5), Timeout.InfiniteTimeSpan],
            [TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(10)],
            [TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(5)],
        ];

        [Fact]
        public void WithTimeout_should_set_ParentContext()
        {
            var operationContext = new OperationContext(new Stopwatch(), Timeout.InfiniteTimeSpan, CancellationToken.None);
            var resultContext = operationContext.WithTimeout(TimeSpan.FromSeconds(10));

            resultContext.ParentContext.Should().Be(operationContext);
        }

        [Fact]
        public void WithTimeout_should_create_timed_out_context_on_timed_out_context()
        {
            var operationContext = new OperationContext(TimeSpan.FromMilliseconds(5), CancellationToken.None);
            Thread.Sleep(10);
            operationContext.IsTimedOut().Should().BeTrue();

            var resultContext = operationContext.WithTimeout(TimeSpan.FromSeconds(10));

            resultContext.IsTimedOut().Should().BeTrue();
        }

        [Fact]
        public void WithTimeout_throws_on_negative_timeout()
        {
            var operationContext = new OperationContext(Timeout.InfiniteTimeSpan, CancellationToken.None);

            var exception = Record.Exception(() => operationContext.WithTimeout(TimeSpan.FromSeconds(-5)));

            exception.Should().BeOfType<ArgumentOutOfRangeException>()
                .Subject.ParamName.Should().Be("timeout");
        }
    }
}

