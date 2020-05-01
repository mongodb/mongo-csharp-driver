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
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class ClientSessionOptionsTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var result = new ClientSessionOptions();

            result.CausalConsistency.Should().NotHaveValue();
        }

        [Theory]
        [ParameterAttributeData]
        public void CausalConsistency_get_should_return_expected_result(
            [Values(null, false, true)] bool? value)
        {
            var subject = CreateSubject(causalConsistency: value);

            var result = subject.CausalConsistency;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void CausalConsistency_set_should_have_expected_result(
            [Values(null, false, true)] bool? value)
        {
            var subject = CreateSubject();

            subject.CausalConsistency = value;

            subject.CausalConsistency.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void DefaultTransactionOptions_get_should_return_expected_result(
            [Values(false, true)] bool nullValue)
        {
            var value = nullValue ? null : new TransactionOptions();
            var subject = CreateSubject(defaultTransactionOptions: value);

            var result = subject.DefaultTransactionOptions;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void DefaultTransactionOptions_set_should_have_expected_result(
            [Values(false, true)] bool nullValue)
        {
            var subject = CreateSubject();
            var value = nullValue ? null : new TransactionOptions();

            subject.DefaultTransactionOptions = value;

            subject.DefaultTransactionOptions.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void ToCore_should_return_expected_result(
            [Values(null, false, true)] bool? causalConsistency,
            [Values(false, true)] bool isImplicit,
            [Values(false, true)] bool nullDefaultTransactionOptions)
        {
            var defaultTransactionOptions = nullDefaultTransactionOptions ? null : new TransactionOptions();
            var subject = CreateSubject(
                causalConsistency: causalConsistency,
                defaultTransactionOptions: defaultTransactionOptions);

            var result = subject.ToCore(isImplicit: isImplicit);

            result.DefaultTransactionOptions.Should().BeSameAs(defaultTransactionOptions);
            result.IsCausallyConsistent.Should().Be(causalConsistency ?? true);
            result.IsImplicit.Should().Be(isImplicit);
        }

        // private methods
        private ClientSessionOptions CreateSubject(
            bool? causalConsistency = null,
            TransactionOptions defaultTransactionOptions = null)
        {
            return new ClientSessionOptions
            {
                CausalConsistency = causalConsistency,
                DefaultTransactionOptions = defaultTransactionOptions
            };
        }
    }
}
