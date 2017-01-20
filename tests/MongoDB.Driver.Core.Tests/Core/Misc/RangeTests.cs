/* Copyright 2013-2016 MongoDB Inc.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Core.Misc
{
    public class RangeTests
    {
        [Fact]
        public void Constructor_should_throw_when_min_is_greater_than_max()
        {
            Action act = () => new Range<int>(2, 1);

            act.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(0, 0, 0, 0, true)]
        [InlineData(0, 1, 0, 1, true)]
        [InlineData(0, 0, 0, 0, true)]
        [InlineData(0, 1, 0, 0, false)]
        [InlineData(0, 0, 0, 1, false)]
        public void Equals_should_return_correct_value(int a1, int a2, int b1, int b2, bool expected)
        {
            var subject = new Range<int>(a1, a2);
            var comparand = new Range<int>(b1, b2);

            subject.Equals(comparand).Should().Be(expected);
        }

        [Theory]
        [InlineData(0, 0, 0, 0, true)]
        [InlineData(0, 1, 0, 0, true)]
        [InlineData(0, 1, 0, 0, true)]
        [InlineData(0, 1, 1, 1, true)]
        [InlineData(0, 0, 0, 1, true)]
        [InlineData(1, 1, 0, 1, true)]
        [InlineData(0, 2, 1, 1, true)]
        [InlineData(0, 2, 2, 3, true)]
        [InlineData(0, 2, 3, 3, false)]
        [InlineData(0, 2, 3, 4, false)]
        [InlineData(3, 3, 0, 2, false)]
        [InlineData(3, 4, 0, 2, false)]
        public void Overlaps_should_return_correct_value(int a1, int a2, int b1, int b2, bool expected)
        {
            var subject = new Range<int>(a1, a2);
            var comparand = new Range<int>(b1, b2);

            subject.Overlaps(comparand).Should().Be(expected);
        }
    }
}