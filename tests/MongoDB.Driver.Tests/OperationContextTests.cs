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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Driver.Core.Misc;
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
            var clock = new FrozenClock(DateTime.UtcNow);
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            using var operationContext = new OperationContext(clock, timeout, cancellationToken);

            operationContext.Timeout.Should().Be(timeout);
            operationContext.RemainingTimeout.Should().Be(timeout);
            operationContext.CancellationToken.Should().Be(cancellationToken);
            operationContext.RootContext.Should().Be(operationContext);
        }

        [Fact]
        public void Constructor_should_throw_on_negative_timeout()
        {
            var exception = Record.Exception(() => new OperationContext(TimeSpan.FromSeconds(-5), CancellationToken.None));

            exception.Should().BeOfType<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(Timeout.Infinite, 0)]
        [InlineData(Timeout.Infinite, 100)]
        [InlineData(100, 0)]
        [InlineData(100, 100)]
        [InlineData(100, 1000)]
        public void Elapsed_should_return_expected_result(int timeoutMs, int elapsedMs)
        {
            var expected = TimeSpan.FromMilliseconds(elapsedMs);
            using var subject = CreateSubject(TimeSpan.FromMilliseconds(timeoutMs), expected, CancellationToken.None);

            subject.Elapsed.Should().Be(expected);
        }

        [Theory]
        [InlineData(Timeout.Infinite, 0)]
        [InlineData(Timeout.Infinite, 25)]
        [InlineData(100, 0)]
        [InlineData(100, 25)]
        public void Fork_should_copy_context(int timeoutMs, int elapsedMs)
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            using var originalContext = CreateSubject(TimeSpan.FromMilliseconds(timeoutMs), TimeSpan.FromMilliseconds(elapsedMs), cancellationTokenSource.Token);

            using var forkedContext = originalContext.Fork();

            forkedContext.Should().NotBe(originalContext);
            forkedContext.CancellationToken.Should().Be(originalContext.CancellationToken);
            forkedContext.Timeout.Should().Be(originalContext.Timeout);
            forkedContext.Elapsed.Should().Be(originalContext.Elapsed);
            forkedContext.RootContext.Should().Be(originalContext.RootContext);
        }

        [Fact]
        public void RemainingTimeout_should_return_expected_result()
        {
            var timeout = TimeSpan.FromMilliseconds(500);
            var elapsed = TimeSpan.FromMilliseconds(10);
            using var subject = CreateSubject(timeout, elapsed, CancellationToken.None);

            subject.RemainingTimeout.Should().Be(timeout - elapsed);
        }

        [Fact]
        public void RemainingTimeout_should_return_infinite_for_infinite_timeout()
        {
            using var subject = CreateSubject(timeout: Timeout.InfiniteTimeSpan, elapsed: TimeSpan.FromMilliseconds(10));

            subject.RemainingTimeout.Should().Be(Timeout.InfiniteTimeSpan);
        }

        [Fact]
        public void RemainingTimeout_should_return_zero_for_timeout_context()
        {
            using var subject = CreateSubject(timeout: TimeSpan.FromMilliseconds(5), elapsed: TimeSpan.FromMilliseconds(10));

            subject.RemainingTimeout.Should().Be(TimeSpan.Zero);
        }

        [Theory]
        [MemberData(nameof(IsTimedOut_test_cases))]
        public void IsTimedOut_should_return_expected_result(bool expected, TimeSpan timeout, TimeSpan elapsed)
        {
            using var subject = CreateSubject(timeout, elapsed);

            var result = subject.IsTimedOut();

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
            using var subject = CreateSubject(timeout: TimeSpan.FromMilliseconds(20), elapsed: TimeSpan.FromMilliseconds(10));

            var exception = Record.Exception(() => subject.ThrowIfTimedOutOrCanceled());

            exception.Should().BeNull();
        }

        [Fact]
        public void ThrowIfTimedOutOrCanceled_should_throw_on_timeout()
        {
            using var subject = CreateSubject(timeout: TimeSpan.FromMilliseconds(10), elapsed: TimeSpan.FromMilliseconds(20));

            var exception = Record.Exception(() => subject.ThrowIfTimedOutOrCanceled());

            exception.Should().BeOfType<TimeoutException>();
        }

        [Fact]
        public void ThrowIfTimedOutOrCanceled_should_throw_on_cancellation()
        {
            using var cancellationSource = new CancellationTokenSource();
            using var subject = CreateSubject(timeout: Timeout.InfiniteTimeSpan, elapsed: TimeSpan.Zero, cancellationSource.Token);
            cancellationSource.Cancel();

            var exception = Record.Exception(() => subject.ThrowIfTimedOutOrCanceled());

            exception.Should().BeOfType<OperationCanceledException>();
        }

        [Fact]
        public void ThrowIfTimedOutOrCanceled_should_throw_CancelledException_when_timedout_and_cancelled()
        {
            using var cancellationSource = new CancellationTokenSource();
            using var subject = CreateSubject(timeout: TimeSpan.FromMilliseconds(10), elapsed: TimeSpan.FromMilliseconds(20), cancellationSource.Token);
            cancellationSource.Cancel();

            var exception = Record.Exception(() => subject.ThrowIfTimedOutOrCanceled());

            exception.Should().BeOfType<OperationCanceledException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Wait_should_throw_if_context_is_timedout([Values(true, false)] bool async)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            using var subject = CreateSubject(timeout: TimeSpan.FromMilliseconds(10), elapsed: TimeSpan.FromMilliseconds(20));

            var exception = async ?
                await Record.ExceptionAsync(() => subject.WaitTaskAsync(taskCompletionSource.Task)) :
                Record.Exception(() => subject.WaitTask(taskCompletionSource.Task));

            exception.Should().BeOfType<TimeoutException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Wait_should_throw_if_context_is_cancelled([Values(true, false)] bool async)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            using var subject = CreateSubject(timeout: Timeout.InfiniteTimeSpan, elapsed: TimeSpan.Zero, cancellationTokenSource.Token);

            var exception = async ?
                await Record.ExceptionAsync(() => subject.WaitTaskAsync(taskCompletionSource.Task)) :
                Record.Exception(() => subject.WaitTask(taskCompletionSource.Task));

            exception.Should().BeOfType<OperationCanceledException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Wait_should_rethrow_on_failed_task([Values(true, false)] bool async)
        {
            var ex = new InvalidOperationException();
            var task = Task.FromException(ex);
            using var subject = CreateSubject(timeout: Timeout.InfiniteTimeSpan, elapsed: TimeSpan.FromMilliseconds(20));

            var exception = async ?
                await Record.ExceptionAsync(() => subject.WaitTaskAsync(task)) :
                Record.Exception(() => subject.WaitTask(task));

            exception.Should().Be(ex);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Wait_should_rethrow_on_failed_promise_task([Values(true, false)] bool async)
        {
            var ex = new InvalidOperationException("Ups!");
            var taskCompletionSource = new TaskCompletionSource<bool>();
            using var subject = CreateSubject(timeout: Timeout.InfiniteTimeSpan, elapsed: TimeSpan.Zero);

            var task = Task.Run(async () =>
            {
                if (async)
                {
                    await subject.WaitTaskAsync(taskCompletionSource.Task);
                }
                else
                {
                    subject.WaitTask(taskCompletionSource.Task);
                }
            });
            Thread.Sleep(10);
            taskCompletionSource.SetException(ex);

            var exception = await Record.ExceptionAsync(() => task);
            exception.Should().Be(ex);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Wait_should_throw_on_timeout([Values(true, false)] bool async)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            using var subject = CreateSubject(timeout: TimeSpan.FromMilliseconds(10), elapsed: TimeSpan.FromMilliseconds(20));

            var exception = async ?
                await Record.ExceptionAsync(() => subject.WaitTaskAsync(taskCompletionSource.Task)) :
                Record.Exception(() => subject.WaitTask(taskCompletionSource.Task));

            exception.Should().BeOfType<TimeoutException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Wait_should_not_throw_on_resolved_task_with_timedout_context([Values(true, false)] bool async)
        {
            var task = Task.FromResult(42);
            using var subject = CreateSubject(timeout: TimeSpan.FromMilliseconds(10), elapsed: TimeSpan.FromMilliseconds(20));

            var exception = async ?
                await Record.ExceptionAsync(() => subject.WaitTaskAsync(task)) :
                Record.Exception(() => subject.WaitTask(task));

            exception.Should().BeNull();
        }


        [Theory]
        [MemberData(nameof(WithTimeout_test_cases))]
        public void WithTimeout_should_calculate_proper_timeout(TimeSpan expected, TimeSpan originalTimeout, TimeSpan newTimeout)
        {
            using var subject = CreateSubject(timeout: originalTimeout, elapsed: TimeSpan.Zero);

            var resultContext = subject.WithTimeout(newTimeout);

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
        public void WithTimeout_should_set_RootContext()
        {
            using var rootContext = CreateSubject(timeout: Timeout.InfiniteTimeSpan, elapsed: TimeSpan.Zero);
            using var resultContext = rootContext.WithTimeout(TimeSpan.FromSeconds(10));

            resultContext.RootContext.Should().Be(rootContext);
        }

        [Fact]
        public void WithTimeout_should_preserve_operation_metadata()
        {
            using var originalContext = CreateSubject(timeout: TimeSpan.FromSeconds(100), elapsed: TimeSpan.Zero);
            using var contextWithMetadata = originalContext.WithOperationMetadata("find", "testdb", "testcol", true);

            using var resultContext = contextWithMetadata.WithTimeout(TimeSpan.FromSeconds(50));

            resultContext.OperationName.Should().Be("find");
            resultContext.DatabaseName.Should().Be("testdb");
            resultContext.CollectionName.Should().Be("testcol");
            resultContext.IsTracingEnabled.Should().BeTrue();
        }

        [Fact]
        public void WithTimeout_should_preserve_RootContext()
        {
            using var rootContext = CreateSubject(timeout: Timeout.InfiniteTimeSpan, elapsed: TimeSpan.Zero);

            using var intermediateContext = rootContext.WithTimeout(TimeSpan.FromSeconds(200));
            using var resultContext = intermediateContext.WithTimeout(TimeSpan.FromSeconds(10));

            resultContext.RootContext.Should().Be(rootContext);
        }

        [Fact]
        public void WithTimeout_should_create_timed_out_context_on_timed_out_context()
        {
            using var rootContext = CreateSubject(timeout: TimeSpan.FromMilliseconds(5), elapsed: TimeSpan.FromMilliseconds(10));
            rootContext.IsTimedOut().Should().BeTrue();

            using var resultContext = rootContext.WithTimeout(TimeSpan.FromSeconds(7));

            resultContext.IsTimedOut().Should().BeTrue();
        }

        [Fact]
        public void WithTimeout_should_throw_on_negative_timeout()
        {
            using var rootContext = CreateSubject(timeout: Timeout.InfiniteTimeSpan, elapsed: TimeSpan.Zero);

            var exception = Record.Exception(() => rootContext.WithTimeout(TimeSpan.FromSeconds(-5)));

            exception.Should().BeOfType<ArgumentOutOfRangeException>()
                .Subject.ParamName.Should().Be("timeout");
        }

        [Fact]
        public void WithOperationMetadata_should_preserve_RootContext()
        {
            using var rootContext = CreateSubject(timeout: Timeout.InfiniteTimeSpan, elapsed: TimeSpan.Zero);

            using var resultContext = rootContext.WithOperationMetadata("insert", "mydb", "mycol", false);

            resultContext.RootContext.Should().Be(rootContext);
        }

        [Fact]
        public void WithOperationMetadata_should_set_metadata_properties()
        {
            using var originalContext = CreateSubject(timeout: TimeSpan.FromSeconds(10), elapsed: TimeSpan.Zero);

            using var resultContext = originalContext.WithOperationMetadata("find", "testdb", "testcollection", true);

            resultContext.OperationName.Should().Be("find");
            resultContext.DatabaseName.Should().Be("testdb");
            resultContext.CollectionName.Should().Be("testcollection");
            resultContext.IsTracingEnabled.Should().BeTrue();
        }

        [Fact]
        public void WithOperationMetadata_should_use_RemainingTimeout()
        {
            var originalTimeout = TimeSpan.FromSeconds(10);
            var elapsed = TimeSpan.FromSeconds(3);
            using var originalContext = CreateSubject(timeout: originalTimeout, elapsed: elapsed);

            using var resultContext = originalContext.WithOperationMetadata("update", "db", "col", true);

            resultContext.Timeout.Should().Be(originalTimeout - elapsed);
        }

        private static OperationContext CreateSubject(TimeSpan? timeout, TimeSpan elapsed = default, CancellationToken cancellationToken = default)
        {
            var clock = new FrozenClock(DateTime.UtcNow);
            var result = new OperationContext(clock, timeout, cancellationToken);

            if (elapsed != TimeSpan.Zero)
            {
                clock.AdvanceCurrentTime(elapsed);
            }

            return result;
        }
    }
}

