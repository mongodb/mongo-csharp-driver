/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Core.Tests
{
    public class FeatureTests
    {
        [Theory]
        [InlineData(7, 8, 12, false)]
        [InlineData(14, 8, 12, false)]
        [InlineData(7, 0, 12, true)]
        [InlineData(6, 5, 6, false)]
        [InlineData(6, 6, 7, true)]
        public void IsSupported_should_return_correct_result(
            int maxSupportedWireVersion,
            int featureIsAddedWireVersion,
            int featureIsRemovedWireVersion,
            bool isSupported)
        {
            var feature = new Feature("test", featureIsAddedWireVersion, featureIsRemovedWireVersion);

            var result = feature.IsSupported(maxSupportedWireVersion);

            result.Should().Be(isSupported);
        }
    }
}
