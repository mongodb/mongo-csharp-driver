/* Copyright 2015-2016 MongoDB Inc.
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver
{
    [Trait("Category", "ReadConcern")]
    public class ReadConcernTests
    {
        [Fact]
        public void Default_should_return_expected_result()
        {
            var result = ReadConcern.Default;

            result.Level.Should().BeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void Constructor_with_level_should_initialize_instance(
            [Values(ReadConcernLevel.Local, ReadConcernLevel.Majority, null)]
            ReadConcernLevel? level)
        {
            var result = new ReadConcern(level);

            result.Level.Should().Be(level);
        }

        [Fact]
        public void Equals_should_return_false_when_level_is_not_equal()
        {
            ReadConcern.Default.Should().NotBe(ReadConcern.Local);
            ReadConcern.Default.Should().NotBe(ReadConcern.Majority);
            ReadConcern.Local.Should().NotBe(ReadConcern.Majority);
            ReadConcern.Linearizable.Should().NotBe(ReadConcern.Local);
            ReadConcern.Linearizable.Should().NotBe(ReadConcern.Majority);
        }

        [Fact]
        public void Equals_should_return_true_when_level_is_equal()
        {
            ReadConcern.Default.Should().Be(ReadConcern.Default);
            new ReadConcern().Should().Be(ReadConcern.Default);
            ReadConcern.Linearizable.Should().Be(ReadConcern.Linearizable);
            new ReadConcern(ReadConcernLevel.Linearizable).Should().Be(ReadConcern.Linearizable);
            ReadConcern.Local.Should().Be(ReadConcern.Local);
            new ReadConcern(ReadConcernLevel.Local).Should().Be(ReadConcern.Local);
            ReadConcern.Majority.Should().Be(ReadConcern.Majority);
            new ReadConcern(ReadConcernLevel.Majority).Should().Be(ReadConcern.Majority);
        }

        [Fact]
        public void ThrowIfNotSupported_should_not_throw_when_default()
        {
            var serverVersion = new SemanticVersion(3, 0, 2);
            var subject = ReadConcern.Default;

            Action act = () => subject.ThrowIfNotSupported(serverVersion);

            act.ShouldNotThrow<MongoClientException>();
        }

        [Fact]
        public void ThrowIfNotSupported_should_not_throw_when_the_serverVersion_is_greater_than_317()
        {
            var serverVersion = new SemanticVersion(3, 2, 0);
            var subject = ReadConcern.Majority;

            Action act = () => subject.ThrowIfNotSupported(serverVersion);

            act.ShouldNotThrow<MongoClientException>();
        }

        [Fact]
        public void ThrowIfNotSupported_should_throw_when_the_serverVersion_is_less_than_317_and_readConcern_is_majority()
        {
            var serverVersion = new SemanticVersion(3, 0, 2);
            var subject = ReadConcern.Majority;

            Action act = () => subject.ThrowIfNotSupported(serverVersion);

            act.ShouldThrow<MongoClientException>();
        }

        [Theory]
        [InlineData(ReadConcernLevel.Linearizable, "{ level: 'linearizable' }")]
        [InlineData(ReadConcernLevel.Local, "{ level: 'local' }")]
        [InlineData(ReadConcernLevel.Majority, "{ level: 'majority' }")]
        [InlineData(null, "{ }")]
        public void ToBsonDocument_should_return_expected_result(ReadConcernLevel? level, string json)
        {
            var subject = new ReadConcern(level);
            var result = subject.ToBsonDocument();

            result.Should().Be(json);
        }

        [Fact]
        public void With_should_return_new_instance_when_level_is_not_the_same()
        {
            var subject = new ReadConcern(null);

            var result = subject.With(ReadConcernLevel.Majority);

            result.Should().NotBeSameAs(subject);
            result.Level.Should().Be(ReadConcernLevel.Majority);
        }

        [Fact]
        public void With_should_return_same_instance_when_all_values_are_equal()
        {
            var subject = new ReadConcern(ReadConcernLevel.Local);

            var result = subject.With(ReadConcernLevel.Local);

            result.Should().BeSameAs(subject);
        }

        [Fact]
        public void With_should_return_same_instance_when_no_values_are_provided()
        {
            var subject = new ReadConcern();

            var result = subject.With();

            result.Should().BeSameAs(subject);
        }
    }
}
