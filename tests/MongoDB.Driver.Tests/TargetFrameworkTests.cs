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

using FluentAssertions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class TargetFrameworkTests
    {
        [Fact]
        public void TargetFramework_should_be_valid()
        {
            var actualFramework = MongoDB.Driver.TargetFramework.Moniker;
            var expectedFramework = GetExpectedTargetFramework();
            actualFramework.Should().Be(expectedFramework);
        }

        // private methods
        private string GetExpectedTargetFramework()
        {
#if NETCOREAPP1_1
            return "netstandard15";
#elif NETCOREAPP2_1
            return "netstandard20";
#elif NET452
            return "net452";
#endif
        }
    }
}
