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

using FluentAssertions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class NoServerSessionTests
    {
        [Fact]
        public void Instance_should_return_expected_result()
        {
            var result = NoServerSession.Instance;

            result.Id.Should().BeNull();
            result.LastUsedAt.Should().NotHaveValue();
        }

        [Fact]
        public void Instance_should_return_cached_instance()
        {
            var result1 = NoServerSession.Instance;
            var result2 = NoServerSession.Instance;

            result2.Should().BeSameAs(result1);
        }

        [Fact]
        public void Id_should_return_expected_result()
        {
            var subject = NoServerSession.Instance;

            var result = subject.Id;

            result.Should().BeNull();
        }

        [Fact]
        public void LastUsedAt_should_return_expected_result()
        {
            var subject = NoServerSession.Instance;

            var result = subject.LastUsedAt;

            result.Should().NotHaveValue();
        }

        [Fact]
        public void Dispose_should_do_nothing()
        {
            var subject = NoServerSession.Instance;

            subject.Dispose();

            var id = subject.Id; // no exception
        }

        [Fact]
        public void WasUsed_should_do_nothing()
        {
            var subject = NoServerSession.Instance;

            subject.WasUsed();
        }
    }
}
