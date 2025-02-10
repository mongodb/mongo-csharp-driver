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
using MongoDB.Driver.Search;
using Xunit;

namespace MongoDB.Driver.Tests.Search
{
    public class SearchRangeV2BuilderTests
    {
        [Theory]
        [InlineData(0, null, true, false)]
        [InlineData(null, 1, false, true)]
        [InlineData(0, 0, true, true)]
        [InlineData(0, 1, true, true)]
        [InlineData(0, 1, false, true)]
        [InlineData(0, 1, true, false)]
        [InlineData(0, 1, true, true)]
        public void SearchRangeV2Builder_should_return_valid_instance(int? min, int? max, bool minInclusive, bool maxInclusive)
        {
            SearchRangeV2<int>? subject = null;

            if (min != null)
            {
                subject = minInclusive ? SearchRangeV2Builder.Gte(min.Value) : SearchRangeV2Builder.Gt(min.Value);
            }

            if (max != null)
            {
                if (subject != null)
                {
                    subject = maxInclusive ? subject.Value.Lte(max.Value) : subject.Value.Lt(max.Value);
                }
                else
                {
                    subject = maxInclusive ? SearchRangeV2Builder.Lte(max.Value) : SearchRangeV2Builder.Lt(max.Value);
                }
            }

            subject.Value.Min?.Value.Should().Be(min);
            subject.Value.Max?.Value.Should().Be(max);
            subject.Value.Min?.Inclusive.Should().Be(minInclusive);
            subject.Value.Max?.Inclusive.Should().Be(maxInclusive);
        }
    }
}