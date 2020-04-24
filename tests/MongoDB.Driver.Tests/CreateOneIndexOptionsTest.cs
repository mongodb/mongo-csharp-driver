/* Copyright 2018-present MongoDB Inc.
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
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class CreateOneIndexOptionsTest
    {
        [Theory]
        [ParameterAttributeData]
        public void CommitQuorum_get_should_return_expected_result(
            [Values(null, 1, 2)] int? w)
        {
            var commitQuorum = w.HasValue ? CreateIndexCommitQuorum.Create(w.Value) : null;
            var subject = new CreateOneIndexOptions() { CommitQuorum = commitQuorum };

            var result = subject.CommitQuorum;

            result.Should().BeSameAs(commitQuorum);
        }

        [Theory]
        [ParameterAttributeData]
        public void CommitQuorum_set_should_have_expected_result(
            [Values(null, 1, 2)] int? w)
        {
            var commitQuorum = w.HasValue ? CreateIndexCommitQuorum.Create(w.Value) : null;
            var subject = new CreateOneIndexOptions() { CommitQuorum = commitQuorum };

            subject.CommitQuorum = commitQuorum;

            subject.CommitQuorum.Should().Be(commitQuorum);
        }

        [Fact]
        public void MaxTime_get_should_return_expected_result()
        {
            var subject = new CreateOneIndexOptions { MaxTime = TimeSpan.FromSeconds(123) };

            var result = subject.MaxTime;

            result.Should().Be(TimeSpan.FromSeconds(123));
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_set_should_have_expected_result(
            [Values(null, -10000, 0, 1, 9999, 10000, 10001)] int? maxTimeTicks)
        {
            var subject = new CreateOneIndexOptions();
            var maxTime = maxTimeTicks == null ? (TimeSpan?)null : TimeSpan.FromTicks(maxTimeTicks.Value);

            subject.MaxTime = maxTime;

            subject.MaxTime.Should().Be(maxTime);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxTime_set_should_throw_when_value_is_invalid(
            [Values(-10001, -9999, -1)] long maxTimeTicks)
        {
            var subject = new CreateOneIndexOptions();
            var value = TimeSpan.FromTicks(maxTimeTicks);

            var exception = Record.Exception(() => subject.MaxTime = value);

            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            e.ParamName.Should().Be("value");
        }

    }
}

