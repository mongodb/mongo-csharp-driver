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
using MongoDB.Bson;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Bindings
{
    public class CoreSessionOptionsTests
    {
        [Theory]
        [ParameterAttributeData]
        public void constructor_should_initialize_instance(
            [Values(false, true)] bool nullDefaultTransactionOptions,
            [Values(false, true)] bool isCausallyConsistent,
            [Values(false, true)] bool isImplicit)
        {
            var defaultTransactionOptions = nullDefaultTransactionOptions ? null : new TransactionOptions();

            var result = new CoreSessionOptions(
                defaultTransactionOptions: defaultTransactionOptions,
                isCausallyConsistent: isCausallyConsistent,
                isImplicit: isImplicit);

            result.DefaultTransactionOptions.Should().BeSameAs(defaultTransactionOptions);
            result.IsCausallyConsistent.Should().Be(isCausallyConsistent);
            result.IsImplicit.Should().Be(isImplicit);
        }

        [Fact]
        public void constructor_should_initialize_instance_with_default_values()
        {
            var result = new CoreSessionOptions();

            result.DefaultTransactionOptions.Should().BeNull();
            result.IsCausallyConsistent.Should().BeFalse();
            result.IsImplicit.Should().BeFalse();
        }

        [Fact]
        public void constructor_should_throw_when_setting_snapshotTime_without_snapshot()
        {
            var ex = Record.Exception(() => new CoreSessionOptions(isSnapshot: false, snapshotTime: new BsonTimestamp(2)));
            ex.Should().NotBeNull();
            ex.Should().BeOfType<ArgumentException>();
            ex.Message.Should().Be("snapshotTime can only be set if isSnapshot is true.");
        }

        [Theory]
        [ParameterAttributeData]
        public void DefaultTransactionOptions_should_return_expected_result(
            [Values(false, true)] bool nullDefaultTransactionOptions)
        {
            var defaultTransactionOptions = nullDefaultTransactionOptions ? null : new TransactionOptions();
            var subject = new CoreSessionOptions(defaultTransactionOptions: defaultTransactionOptions);

            var result = subject.DefaultTransactionOptions;

            result.Should().BeSameAs(defaultTransactionOptions);
        }

        [Theory]
        [ParameterAttributeData]
        public void IsCausallyConsistent_should_return_expected_result(
            [Values(false, true)] bool value)
        {
            var subject = new CoreSessionOptions(isCausallyConsistent: value);

            var result = subject.IsCausallyConsistent;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void IsImplicit_should_return_expected_result(
            [Values(false, true)] bool value)
        {
            var subject = new CoreSessionOptions(isImplicit: value);

            var result = subject.IsImplicit;

            result.Should().Be(value);
        }
    }
}
