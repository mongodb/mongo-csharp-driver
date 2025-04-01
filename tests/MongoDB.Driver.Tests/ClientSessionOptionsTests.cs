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

using Shouldly;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Bson.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class ClientSessionOptionsTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var result = new ClientSessionOptions();

            result.CausalConsistency.ShouldNotHaveValue();
        }

        [Theory]
        [ParameterAttributeData]
        public void CausalConsistency_get_should_return_expected_result(
            [Values(null, false, true)] bool? value)
        {
            var subject = CreateSubject(causalConsistency: value);

            var result = subject.CausalConsistency;

            result.ShouldBe(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void CausalConsistency_set_should_have_expected_result(
            [Values(null, false, true)] bool? value)
        {
            var subject = CreateSubject();

            subject.CausalConsistency = value;

            subject.CausalConsistency.ShouldBe(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void DefaultTransactionOptions_get_should_return_expected_result(
            [Values(false, true)] bool nullValue)
        {
            var value = nullValue ? null : new TransactionOptions();
            var subject = CreateSubject(defaultTransactionOptions: value);

            var result = subject.DefaultTransactionOptions;

            result.ShouldBeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void DefaultTransactionOptions_set_should_have_expected_result(
            [Values(false, true)] bool nullValue)
        {
            var subject = CreateSubject();
            var value = nullValue ? null : new TransactionOptions();

            subject.DefaultTransactionOptions = value;

            subject.DefaultTransactionOptions.ShouldBeSameAs(value);
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

            result.DefaultTransactionOptions.ShouldBeSameAs(defaultTransactionOptions);
            result.IsCausallyConsistent.ShouldBe(causalConsistency ?? true);
            result.IsImplicit.ShouldBe(isImplicit);
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
