/* Copyright 2020-present MongoDB Inc.
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
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Tests
{
    public class CreateIndexCommitQuorumTests
    {
        [Fact]
        public void Majority_returns_expected_result()
        {
            var subject = CreateIndexCommitQuorum.Majority;

            var s = subject.Should().BeOfType<CreateIndexCommitQuorumWithMode>().Subject;
            s.Mode.Should().Be("majority");
        }

        [Fact]
        public void VotingMembers_returns_expected_result()
        {
            var subject = CreateIndexCommitQuorum.VotingMembers;

            var s = subject.Should().BeOfType<CreateIndexCommitQuorumWithMode>().Subject;
            s.Mode.Should().Be("votingMembers");
        }

        [Theory]
        [ParameterAttributeData]
        public void Create_with_mode_should_return_expected_result(
            [Values("abc", "def")] string mode)
        {
            var subject = CreateIndexCommitQuorum.Create(mode);

            var s = subject.Should().BeOfType<CreateIndexCommitQuorumWithMode>().Subject;
            s.Mode.Should().Be(mode);
        }

        [Theory]
        [ParameterAttributeData]
        public void Create_with_w_should_return_expected_result(
            [Values(1, 2, 3)] int w)
        {
            var subject = CreateIndexCommitQuorum.Create(w);

            var s = subject.Should().BeOfType<CreateIndexCommitQuorumWithW>().Subject;
            s.W.Should().Be(w);
        }
    }

    public class CreateIndexCommitQuorumWithModeTests
    {
        [Theory]
        [ParameterAttributeData]
        public void constructor_should_initialize_instance(
            [Values("abc", "def")] string mode)
        {
            var subject = new CreateIndexCommitQuorumWithMode(mode);

            subject.Mode.Should().Be(mode);
        }

        [Fact]
        public void constructor_should_throw_when_mode_is_null()
        {
            var exception = Record.Exception(() => new CreateIndexCommitQuorumWithMode(null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("mode");
        }

        [Fact]
        public void constructor_should_throw_when_mode_is_empty_string()
        {
            var exception = Record.Exception(() => new CreateIndexCommitQuorumWithMode(""));

            var e = exception.Should().BeOfType<ArgumentException>().Subject;
            e.ParamName.Should().Be("mode");
        }

        [Theory]
        [ParameterAttributeData]
        public void ToBsonValue_should_return_expected_result(
            [Values("abc", "def")] string mode)
        {
            var subject = new CreateIndexCommitQuorumWithMode(mode);

            var result = subject.ToBsonValue();

            var r = result.Should().BeOfType<BsonString>().Subject;
            r.Value.Should().Be(mode);
        }
    }

    public class CreateIndexCommitQuorumWithWTests
    {
        [Theory]
        [ParameterAttributeData]
        public void constructor_should_initialize_instance(
            [Values(0, 1, 2, int.MaxValue)] int w)
        {
            var subject = new CreateIndexCommitQuorumWithW(w);

            subject.W.Should().Be(w);
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_should_throw_when_mode_is_null(
            [Values(int.MinValue, -1)] int w)
        {
            var exception = Record.Exception(() => new CreateIndexCommitQuorumWithW(w));

            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            e.ParamName.Should().Be("w");
        }

        [Theory]
        [ParameterAttributeData]
        public void ToBsonValue_should_return_expected_result(
            [Values(1, 2, 3)] int w)
        {
            var subject = new CreateIndexCommitQuorumWithW(w);

            var result = subject.ToBsonValue();

            var r = result.Should().BeOfType<BsonInt32>().Subject;
            r.Value.Should().Be(w);
        }
    }
}
