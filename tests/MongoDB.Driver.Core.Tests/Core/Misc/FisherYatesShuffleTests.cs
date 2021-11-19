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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Misc
{
    public class FisherYatesShuffleTests
    {
        [Fact]
        public void Shuffling_null_should_throw_ArgumentNullException()
        {
            IList<int> list = null;

            var exception = Record.Exception(() => FisherYatesShuffle.Shuffle(list));

            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Shuffling_empty_list_should_do_nothing()
        {
            var empty = Array.Empty<int>();
            FisherYatesShuffle.Shuffle(empty);
            empty.Should().BeEmpty();
        }

        [Fact]
        public void Shuffling_single_element_list_should_do_nothing()
        {
            var list = new List<int> { 42 };
            FisherYatesShuffle.Shuffle(list);
            list.Should().ContainSingle(x => x == 42);
        }

        [Fact]
        public void Shuffling_a_large_list_should_shuffle_order()
        {
            var list = Enumerable.Range(0, 100).ToList();
            var elementsInOriginalOrder = new List<int>(list);
            FisherYatesShuffle.Shuffle(list);
            // Verify that all the elements are present in some order.
            list.Should().Contain(elementsInOriginalOrder);
            // Verify that the elements are not in the original order.
            // NOTE: There is a 1 in N! chance that the shuffled element order
            //       will match the original element order. This is statistically
            //       extremely unlikely when N = 100.
            list.Should().NotEqual(elementsInOriginalOrder);
        }
    }
}
