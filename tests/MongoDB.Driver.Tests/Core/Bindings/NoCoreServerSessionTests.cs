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

namespace MongoDB.Driver.Core.Bindings
{
    public class NoCoreServerSessionTests
    {
        [Fact]
        public void Instance_should_return_expected_result()
        {
            var result = NoCoreServerSession.Instance;

            result.Id.Should().BeNull();
            result.LastUsedAt.Should().NotHaveValue();
        }

        [Fact]
        public void Instance_should_return_singleton()
        {
            var result1 = NoCoreServerSession.Instance;
            var result2 = NoCoreServerSession.Instance;

            result2.Should().BeSameAs(result1);
        }

        [Fact]
        public void Id_should_return_null()
        {
            var subject = CreateSubject();

            var result = subject.Id;

            result.Should().BeNull();
        }

        [Fact]
        public void LastUsedAt_should_return_null()
        {
            var subject = CreateSubject();

            var result = subject.LastUsedAt;

            result.Should().NotHaveValue();
        }

        [Fact]
        public void AdvanceTransactionNumber_should_return_minus_one()
        {
            var subject = CreateSubject();

            var result = subject.AdvanceTransactionNumber();

            result.Should().Be(-1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Dispose_should_do_nothing(
            [Values(1, 2)] int timesCalled)
        {
            var subject = CreateSubject();

            for (var i = 0; i < timesCalled; i++)
            {
                subject.Dispose();
            }
        }

        [Fact]
        public void WasUsedshould_do_nothing()
        {
            var subject = CreateSubject();

            subject.WasUsed();
        }

        // private methods
        private NoCoreServerSession CreateSubject()
        {
            return new NoCoreServerSession();
        }
    }
}
