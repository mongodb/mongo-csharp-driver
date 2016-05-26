/* Copyright 2015-2016 MongoDB Inc.
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
    public class ReadAheadEnumerableTests
    {
        [Fact]
        public void Should_return_all_items()
        {
            var items = new[] { 1, 2, 3 };
            var subject = new ReadAheadEnumerable<int>(items);

            var list = subject.ToList();
            list.Count.Should().Be(3);
            list.Should().ContainInOrder(1, 2, 3);
        }

        [Fact]
        public void Should_behave_correctly()
        {
            var items = new[] { 1, 2, 3 };
            var subject = new ReadAheadEnumerable<int>(items);

            var enumerator = (ReadAheadEnumerable<int>.ReadAheadEnumerator)subject.GetEnumerator();

            Action act = () => { int temp = enumerator.Current; };
            act.ShouldThrow<InvalidOperationException>();

            enumerator.MoveNext().Should().BeTrue();
            enumerator.Current.Should().Be(1);
            enumerator.HasNext.Should().BeTrue();

            enumerator.MoveNext().Should().BeTrue();
            enumerator.Current.Should().Be(2);
            enumerator.HasNext.Should().BeTrue();

            enumerator.MoveNext().Should().BeTrue();
            enumerator.Current.Should().Be(3);
            enumerator.HasNext.Should().BeFalse();

            enumerator.MoveNext().Should().BeFalse();

            act = () => { int temp = enumerator.Current; };
            act.ShouldThrow<InvalidOperationException>();
        }
    }
}