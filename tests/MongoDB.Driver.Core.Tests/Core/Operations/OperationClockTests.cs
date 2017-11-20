/* Copyright 2017 MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class OperationClockTests
    {
        [Theory]
        [InlineData(null, null, null)]
        [InlineData(null, 1L, 1L)]
        [InlineData(1L, null, 1L)]
        [InlineData(1L, 1L, 1L)]
        [InlineData(1L, 2L, 2L)]
        [InlineData(2L, 1L, 2L)]
        public void GreaterOperationTime_should_return_expected_result(long? timestamp1, long? timestamp2, long? expectedTimestamp)
        {
            var x = CreateOperationTime(timestamp1);
            var y = CreateOperationTime(timestamp2);
            var expectedResult = CreateOperationTime(expectedTimestamp);

            var result = OperationClock.GreaterOperationTime(x, y);

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void constructor_should_initialize_instance()
        {
            var result = new OperationClock();

            result.OperationTime.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData(1L)]
        [InlineData(2L)]
        public void OperationTime_should_return_expected_result(long? timestamp)
        {
            var subject = CreateSubject();
            var operationTime = CreateOperationTime(timestamp);
            if (operationTime != null)
            {
                subject.AdvanceOperationTime(operationTime);
            }

            var result = subject.OperationTime;

            result.Should().Be(operationTime);
        }

        [Theory]
        [InlineData(null, 1L, 1L)]
        [InlineData(1L, 1L, 1L)]
        [InlineData(1L, 2L, 2L)]
        [InlineData(2L, 1L, 2L)]
        public void AdvanceOperationTime_should_only_advance_operation_time_when_new_operation_time_is_greater(long? timestamp1, long? timestamp2, long? expectedTimestamp)
        {
            var operationTime1 = CreateOperationTime(timestamp1);
            var operationTime2 = CreateOperationTime(timestamp2);
            var expectedResult = CreateOperationTime(expectedTimestamp);
            var subject = CreateSubject();
            if (operationTime1 != null)
            {
                subject.AdvanceOperationTime(operationTime1);
            }

            subject.AdvanceOperationTime(operationTime2);

            subject.OperationTime.Should().Be(expectedResult);
        }

        [Fact]
        public void AdvanceOperationTime_should_throw_when_newOperationTime_is_null()
        {
            var subject = CreateSubject();

            var exception = Record.Exception(() => subject.AdvanceOperationTime(null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("newOperationTime");
        }

        // private methods
        private BsonTimestamp CreateOperationTime(long? timestamp)
        {
            return timestamp.HasValue ? new BsonTimestamp(timestamp.Value) : null;
        }

        private OperationClock CreateSubject()
        {
            return new OperationClock();
        }
    }

    public class NoOperationClockTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var result = new NoOperationClock();

            result.OperationTime.Should().BeNull();
        }

        [Fact]
        public void AdvanceOperationTime_should_do_nothing()
        {
            var subject = new NoOperationClock();
            var newOperationTime = new BsonTimestamp(1);

            subject.AdvanceOperationTime(newOperationTime);

            subject.OperationTime.Should().BeNull();
        }
    }
}
