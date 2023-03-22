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

using System;
using FluentAssertions;
using MongoDB.Driver.Search;
using Xunit;

namespace MongoDB.Driver.Tests.Search
{
    public class SearchRangeBuilderTests
    {
        [Theory]
        [InlineData(1, 0, true, true)]
        public void SearchRange_ctor_should_throw_on_invalid_arguments(int min, int max, bool minInclusive, bool maxInclusive)
        {
            Record.Exception(() => new SearchRange<int>(min, max, minInclusive, maxInclusive))
                .Should()
                .BeOfType<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(0, null, true, true)]
        [InlineData(null, 1, true, true)]
        [InlineData(0, 0, true, true)]
        [InlineData(0, 1, true, true)]
        [InlineData(0, 1, false, true)]
        [InlineData(0, 1, true, false)]
        [InlineData(0, 1, true, true)]
        public void SearchRange_ctor_should_construct_valid_instance(int? min, int? max, bool minInclusive, bool maxInclusive)
        {
            var subject = new SearchRange<int>(min, max, minInclusive, maxInclusive);
            subject.Min.Should().Be(min);
            subject.Max.Should().Be(max);
            subject.IsMinInclusive.Should().Be(minInclusive);
            subject.IsMaxInclusive.Should().Be(maxInclusive);
        }

        [Theory]
        [InlineData(1, 0, true, true)]
        [InlineData(100, -1, false, false)]
        public void SearchRangeBuilder_should_throw_on_invalid_arguments(int min, int max, bool minInclusive, bool maxInclusive)
        {
            SearchRangeBuilder.Lt(min).Gt(max);
            Record.Exception(() => new SearchRange<int>(min, max, minInclusive, maxInclusive))
                .Should()
                .BeOfType<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(0, null, true, false)]
        [InlineData(null, 1, false, true)]
        [InlineData(0, 0, true, true)]
        [InlineData(0, 1, true, true)]
        [InlineData(0, 1, false, true)]
        [InlineData(0, 1, true, false)]
        [InlineData(0, 1, true, true)]
        public void SearchRangeBuilder_should_return_valid_instance(int? min, int? max, bool minInclusive, bool maxInclusive)
        {
            SearchRange<int>? subject = null;

            if (min != null)
            {
                subject = minInclusive ? SearchRangeBuilder.Gte(min.Value) : SearchRangeBuilder.Gt(min.Value);
            }

            if (max != null)
            {
                if (subject != null)
                {
                    subject = maxInclusive ? subject.Value.Lte(max.Value) : subject.Value.Lt(max.Value);
                }
                else
                {
                    subject = maxInclusive ? SearchRangeBuilder.Lte(max.Value) : SearchRangeBuilder.Lt(max.Value);
                }
            }

            subject.Value.Min.Should().Be(min);
            subject.Value.Max.Should().Be(max);
            subject.Value.IsMinInclusive.Should().Be(minInclusive);
            subject.Value.IsMaxInclusive.Should().Be(maxInclusive);
        }
    }
}
