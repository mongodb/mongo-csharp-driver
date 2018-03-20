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
        public void ToCore_should_return_expected_result(
            [Values(null, false, true)] bool? causalConsistency,
            [Values(false, true)] bool isImplicit)
        {
            var subject = CreateSubject(causalConsistency: causalConsistency);

            var result = subject.ToCore(isImplicit: isImplicit);

            result.IsCausallyConsistent.Should().Be(causalConsistency ?? true);
            result.IsImplicit.Should().Be(isImplicit);
        }
    
        // private methods
        private ClientSessionOptions CreateSubject(bool? causalConsistency = null)
        {
            return new ClientSessionOptions
            {
                CausalConsistency = causalConsistency
            };
        }
    }
}
