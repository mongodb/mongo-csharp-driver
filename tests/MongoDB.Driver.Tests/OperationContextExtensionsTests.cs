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
using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class OperationContextExtensionsTests
    {
        [Fact]
        public void IsRootContextTimeoutConfigured_should_throw_on_null()
        {
            OperationContext context = null;
            var exception = Record.Exception(() => context.IsRootContextTimeoutConfigured());

            exception.Should().BeOfType<ArgumentNullException>().Subject
                .ParamName.Should().Be("operationContext");
        }

        [Theory]
        [InlineData(false, null)]
        [InlineData(true, 0)]
        [InlineData(true, Timeout.Infinite)]
        [InlineData(true, 5)]
        public void IsRootContextTimeoutConfigured_should_return_expected_result(bool expectedResult, int? timeoutMs)
        {
            TimeSpan? timeout = timeoutMs.HasValue ? TimeSpan.FromMilliseconds(timeoutMs.Value) : null;
            var subject = new OperationContext(timeout, CancellationToken.None);

            var result = subject.IsRootContextTimeoutConfigured();

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void RemainingTimeoutOrDefault_should_throw_on_null()
        {
            OperationContext context = null;
            var exception = Record.Exception(() => context.RemainingTimeoutOrDefault(TimeSpan.Zero));

            exception.Should().BeOfType<ArgumentNullException>().Subject
                .ParamName.Should().Be("operationContext");
        }

        [Theory]
        [InlineData(10, null, 10)]
        [InlineData(0, 0, 10)]
        [InlineData(Timeout.Infinite, Timeout.Infinite, 10)]
        [InlineData(5, 5, 10)]
        public void RemainingTimeoutOrDefault_should_return_expected_result(int expectedResultMs, int? timeoutMs, int defaultValueMs)
        {
            var clock = new FrozenClock(DateTime.UtcNow);
            TimeSpan? timeout = timeoutMs.HasValue ? TimeSpan.FromMilliseconds(timeoutMs.Value) : null;
            var defaultValue = TimeSpan.FromMilliseconds(defaultValueMs);
            var subject = new OperationContext(clock, timeout, CancellationToken.None);

            var result = subject.RemainingTimeoutOrDefault(defaultValue);

            result.Should().Be(TimeSpan.FromMilliseconds(expectedResultMs));
        }
    }
}

