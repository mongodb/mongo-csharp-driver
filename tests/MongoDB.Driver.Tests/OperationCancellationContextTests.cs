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
using FluentAssertions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class OperationCancellationContextTests
    {
        [Fact]
        public void Constructor_should_initialize_properties()
        {
            var timeout = TimeSpan.FromSeconds(42);
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var cancellationContext = new OperationCancellationContext(timeout, cancellationToken);

            cancellationContext.Timeout.Should().Be(timeout);
            cancellationContext.CancellationToken.Should().Be(cancellationToken);
        }

        [Fact]
        public void Elapsed_should_return_elapsed_time()
        {
            var stopwatch = Stopwatch.StartNew();
            Thread.Sleep(10);
            stopwatch.Stop();

            var cancellationContext = new OperationCancellationContext(stopwatch, TimeSpan.Zero, CancellationToken.None);

            cancellationContext.Elapsed.Should().Be(stopwatch.Elapsed);
        }

        [Fact]
        public void RemainingTimeout_should_calculate()
        {
            var timeout = TimeSpan.FromMilliseconds(500);
            var stopwatch = Stopwatch.StartNew();
            Thread.Sleep(10);
            stopwatch.Stop();

            var cancellationContext = new OperationCancellationContext(stopwatch, timeout, CancellationToken.None);

            cancellationContext.RemainingTimeout.Should().Be(timeout - stopwatch.Elapsed);
        }

        [Fact]
        public void RemainingTimeout_should_return_infinite_for_infinite_timeout()
        {
            var stopwatch = Stopwatch.StartNew();
            Thread.Sleep(10);
            stopwatch.Stop();

            var cancellationContext = new OperationCancellationContext(stopwatch, Timeout.InfiniteTimeSpan, CancellationToken.None);

            cancellationContext.RemainingTimeout.Should().Be(Timeout.InfiniteTimeSpan);
        }

        [Fact]
        public void RenainingTimeout_could_be_negative()
        {
            var timeout = TimeSpan.FromMilliseconds(5);
            var stopwatch = Stopwatch.StartNew();
            Thread.Sleep(10);
            stopwatch.Stop();

            var cancellationContext = new OperationCancellationContext(stopwatch, timeout, CancellationToken.None);

            cancellationContext.RemainingTimeout.Should().Be(timeout - stopwatch.Elapsed);
        }

        [Theory]
        [MemberData(nameof(IsTimedOut_test_cases))]
        public void IsTimedOut_should_return_expected_result(bool expected, TimeSpan timeout, TimeSpan waitTime)
        {
            var stopwatch = Stopwatch.StartNew();
            Thread.Sleep(waitTime);
            stopwatch.Stop();

            var cancellationContext = new OperationCancellationContext(stopwatch, timeout, CancellationToken.None);
            var result = cancellationContext.IsTimedOut();

            result.Should().Be(expected);
        }

        public static IEnumerable<object[]> IsTimedOut_test_cases =
        [
            [false, Timeout.InfiniteTimeSpan, TimeSpan.FromMilliseconds(5)],
            [false, TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(5)],
            [true, TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(10)],
        ];

        [Theory]
        [MemberData(nameof(WithTimeout_test_cases))]
        public void WithTimeout_should_calculate_proper_timeout(TimeSpan expected, TimeSpan originalTimeout, TimeSpan newTimeout)
        {
            var cancellationContext = new OperationCancellationContext(new Stopwatch(), originalTimeout, CancellationToken.None);
            var resultContext = cancellationContext.WithTimeout(newTimeout);

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

        // TODO: Add tests for WaitTask and WaitTaskAsync.
    }
}

