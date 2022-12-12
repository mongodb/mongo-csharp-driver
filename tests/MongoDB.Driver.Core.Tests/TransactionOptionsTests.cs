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

using FluentAssertions;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver
{
    public class TransactionOptionsTests
    {
        [Fact]
        public void constructor_with_no_arguments_should_initialize_instance()
        {
            var result = new TransactionOptions();

            result.ReadConcern.Should().BeNull();
            result.WriteConcern.Should().BeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_just_readConcern_should_initialize_instance(
            [Values(false, true)] bool nullReadConcern)
        {
            var readConcern = nullReadConcern ? null : new ReadConcern();

            var result = new TransactionOptions(readConcern: readConcern);

            result.ReadConcern.Should().BeSameAs(readConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_just_writeConcern_should_initialize_instance(
            [Values(false, true)] bool nullWriteConcern)
        {
            var writeConcern = nullWriteConcern ? null : new WriteConcern();

            var result = new TransactionOptions(writeConcern: writeConcern);

            result.WriteConcern.Should().BeSameAs(writeConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_all_arguments_should_initialize_instance(
            [Values(false, true)] bool nullReadConcern,
            [Values(false, true)] bool nullWriteConcern)
        {
            var readConcern = nullReadConcern ? null : new ReadConcern();
            var writeConcern = nullWriteConcern ? null : new WriteConcern();

            var result = new TransactionOptions(readConcern: readConcern, writeConcern: writeConcern);

            result.ReadConcern.Should().BeSameAs(readConcern);
            result.WriteConcern.Should().BeSameAs(writeConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadConcern_should_return_expected_result(
            [Values(false, true)] bool nullReadConcern)
        {
            var readConcern = nullReadConcern ? null : new ReadConcern();
            var subject = new TransactionOptions(readConcern: readConcern);

            var result = subject.ReadConcern;

            result.Should().BeSameAs(readConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteConcern_should_return_expected_result(
            [Values(false, true)] bool nullWriteConcern)
        {
            var writeConcern = nullWriteConcern ? null : new WriteConcern();
            var subject = new TransactionOptions(writeConcern: writeConcern);

            var result = subject.WriteConcern;

            result.Should().BeSameAs(writeConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void With_with_readConcern_should_return_expected_result(
            [Values(false, true)] bool nullReadConcern)
        {
            var subject = new TransactionOptions(new ReadConcern(), new ReadPreference(ReadPreferenceMode.Primary), new WriteConcern());
            var readConcern = nullReadConcern ? null : new ReadConcern();

            var result = subject.With(readConcern: readConcern);

            result.ReadConcern.Should().BeSameAs(readConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void With_with_writeConcern_should_return_expected_result(
            [Values(false, true)] bool nullWriteConcern)
        {
            var subject = new TransactionOptions(new ReadConcern(), new ReadPreference(ReadPreferenceMode.Primary), new WriteConcern());
            var writeConcern = nullWriteConcern ? null : new WriteConcern();

            var result = subject.With(writeConcern: writeConcern);

            result.WriteConcern.Should().BeSameAs(writeConcern);
        }

        [Theory]
        [ParameterAttributeData]
        public void With_with_all_arguments_should_return_expected_result(
            [Values(false, true)] bool nullReadConcern,
            [Values(false, true)] bool nullWriteConcern)
        {
            var subject = new TransactionOptions(new ReadConcern(), new ReadPreference(ReadPreferenceMode.Primary), new WriteConcern());
            var readConcern = nullReadConcern ? null : new ReadConcern();
            var writeConcern = nullWriteConcern ? null : new WriteConcern();

            var result = subject.With(readConcern: readConcern, writeConcern: writeConcern);

            result.ReadConcern.Should().BeSameAs(readConcern);
            result.WriteConcern.Should().BeSameAs(writeConcern);
        }
    }
}
