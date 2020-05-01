/* Copyright 2013-present MongoDB Inc.
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
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Misc
{
    public class BatchableSourceTests
    {
        [Theory]
        [ParameterAttributeData]
        public void constructor_with_list_should_initialize_instance(
            [Values(0, 1, 2, 3)] int length)
        {
            var list = new List<int>();
            for (var i = 0; i < length; i++)
            {
                list.Add(i);
            }

            var result = new BatchableSource<int>(list);

            result.CanBeSplit.Should().BeFalse();
            result.Count.Should().Be(length);
            result.Items.Should().Equal(list);
            result.Offset.Should().Be(0);
            result.ProcessedCount.Should().Be(0);
        }

        [Fact]
        public void constructor_with_list_should_throw_when_list_is_null()
        {
            var exception = Record.Exception(() => new BatchableSource<int>((IReadOnlyList<int>)null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("items");
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_list_and_canBeSplit_should_initialize_instance(
            [Values(0, 1, 2, 3)] int length,
            [Values(false, true)] bool canBeSplit)
        {
            var list = new List<int>();
            for (var i = 0; i < length; i++)
            {
                list.Add(i);
            }

            var result = new BatchableSource<int>(list, canBeSplit);

            result.CanBeSplit.Should().Be(canBeSplit);
            result.Count.Should().Be(length);
            result.Items.Should().Equal(list);
            result.Offset.Should().Be(0);
            result.ProcessedCount.Should().Be(0);
        }

        [Fact]
        public void constructor_with_list_and_canBeSplit_should_throw_when_list_is_null()
        {
            var exception = Record.Exception(() => new BatchableSource<int>((IReadOnlyList<int>)null, true));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("items");
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_list_offset_count_and_canBeSplit_should_initialize_instance(
            [Values(3, 4)] int length,
            [Values(0, 1)] int offset,
            [Values(1, 2)] int count,
            [Values(false, true)] bool canBeSplit)
        {
            var list = new List<int>();
            for (var i = 0; i < length; i++)
            {
                list.Add(i);
            }

            var result = new BatchableSource<int>(list, offset, count, canBeSplit);

            result.CanBeSplit.Should().Be(canBeSplit);
            result.Count.Should().Be(count);
            result.Items.Should().Equal(list);
            result.Offset.Should().Be(offset);
            result.ProcessedCount.Should().Be(0);
        }

        [Fact]
        public void constructor_with_list_offset_count_and_canBeSplit_should_throw_when_list_is_null()
        {
            var exception = Record.Exception(() => new BatchableSource<int>((IReadOnlyList<int>)null, 0, 0, true));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("items");
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(0, 1)]
        [InlineData(1, -1)]
        [InlineData(1, 2)]
        [InlineData(2, -1)]
        [InlineData(2, 3)]
        public void constructor_with_list_offset_count_and_canBeSplit_should_throw_when_offset_is_invalid(int length, int offset)
        {
            var list = Enumerable.Range(0, length).ToList();

            var exception = Record.Exception(() => new BatchableSource<int>(list, offset, 0, true));

            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            e.ParamName.Should().Be("offset");
        }
        [Theory]
        [InlineData(0, 0, -1)]
        [InlineData(0, 0, 1)]
        [InlineData(1, 0, -1)]
        [InlineData(1, 0, 2)]
        [InlineData(1, 1, 1)]
        [InlineData(2, 0, -1)]
        [InlineData(2, 0, 3)]
        [InlineData(2, 1, -1)]
        [InlineData(2, 1, 2)]
        [InlineData(2, 2, -1)]
        [InlineData(2, 2, 1)]
        public void constructor_with_list_offset_count_and_canBeSplit_should_throw_when_count_is_invalid(int length, int offset, int count)
        {
            var list = Enumerable.Range(0, length).ToList();

            var exception = Record.Exception(() => new BatchableSource<int>(list, offset, count, true));

            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            e.ParamName.Should().Be("count");
        }

        [Theory]
        [InlineData(0, 0, true)]
        [InlineData(1, 0, false)]
        [InlineData(1, 1, true)]
        [InlineData(2, 0, false)]
        [InlineData(2, 1, false)]
        [InlineData(2, 2, true)]
        [InlineData(3, 0, false)]
        [InlineData(3, 1, false)]
        [InlineData(3, 2, false)]
        [InlineData(3, 3, true)]
        public void AllItemsWereProcessed_should_return_expected_result(int length, int processedCount, bool expectedResult)
        {
            var subject = CreateSubject(length: length);
            subject.SetProcessedCount(processedCount);

            var result = subject.AllItemsWereProcessed;

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CanBeSplit_should_return_expected_result(
            [Values(false, true)] bool value)
        {
            var subject = CreateSubject(canBeSplit: value);

            var result = subject.CanBeSplit;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Count_should_return_expected_result(
            [Values(0, 1, 2, 3)] int value)
        {
            var subject = CreateSubject(count: value);

            var result = subject.Count;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Items_should_return_expected_result(
            [Values(0, 1, 2, 3)] int length)
        {
            var items = Enumerable.Range(0, length).ToList();
            var subject = new BatchableSource<int>(items);

            var result = subject.Items;

            result.Should().BeSameAs(items);
        }

        [Theory]
        [ParameterAttributeData]
        public void Offset_should_return_expected_result(
            [Values(0, 1, 2, 3)] int value)
        {
            var subject = CreateSubject(length: 4, offset: value, count: 1);

            var result = subject.Offset;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void ProcessedCount_should_return_expected_result(
            [Values(0, 1, 2, 3)] int value)
        {
            var subject = CreateSubject();
            subject.SetProcessedCount(value);

            var result = subject.ProcessedCount;

            result.Should().Be(value);
        }

        [Theory]
        [InlineData(0, 0, 0, 0, 0, 0)]
        [InlineData(1, 0, 0, 0, 0, 0)]
        [InlineData(1, 0, 1, 0, 0, 1)]
        [InlineData(1, 0, 1, 1, 1, 0)]
        [InlineData(1, 1, 0, 0, 1, 0)]
        [InlineData(2, 0, 0, 0, 0, 0)]
        [InlineData(2, 0, 1, 0, 0, 1)]
        [InlineData(2, 0, 1, 1, 1, 0)]
        [InlineData(2, 0, 2, 0, 0, 2)]
        [InlineData(2, 0, 2, 1, 1, 1)]
        [InlineData(2, 0, 2, 2, 2, 0)]
        [InlineData(2, 1, 0, 0, 1, 0)]
        [InlineData(2, 1, 1, 0, 1, 1)]
        [InlineData(2, 1, 1, 1, 2, 0)]
        [InlineData(2, 2, 0, 0, 2, 0)]
        public void AdvancePastProcessedItems_should_have_expected_result(int length, int offset, int count, int processedCount, int expectedOffset, int expectedCount)
        {
            var subject = CreateSubject(length: length, offset: offset, count: count, canBeSplit: true);
            subject.SetProcessedCount(processedCount);

            subject.AdvancePastProcessedItems();

            subject.Offset.Should().Be(expectedOffset);
            subject.Count.Should().Be(expectedCount);
            subject.ProcessedCount.Should().Be(0);
        }

        [Theory]
        [InlineData(0, 0, 0, new int[] { })]
        [InlineData(1, 0, 0, new int[] { })]
        [InlineData(1, 0, 1, new int[] { 0 })]
        [InlineData(1, 1, 0, new int[] { })]
        [InlineData(2, 0, 0, new int[] { })]
        [InlineData(2, 0, 1, new int[] { 0 })]
        [InlineData(2, 0, 2, new int[] { 0, 1 })]
        [InlineData(2, 1, 0, new int[] { })]
        [InlineData(2, 1, 1, new int[] { 1 })]
        [InlineData(2, 2, 0, new int[] { })]
        public void GetBatchItems_should_return_expected_result(int length, int offset, int count, int[] expectedResult)
        {
            var subject = CreateSubject(length: length, offset: offset, count: count);

            var result = subject.GetBatchItems();

            result.Should().Equal(expectedResult);
        }

        [Theory]
        [InlineData(0, 0, 0, 0, new int[] { })]
        [InlineData(1, 0, 0, 0, new int[] { })]
        [InlineData(1, 0, 1, 0, new int[] { })]
        [InlineData(1, 0, 1, 1, new int[] { 0 })]
        [InlineData(1, 1, 0, 0, new int[] { })]
        [InlineData(2, 0, 0, 0, new int[] { })]
        [InlineData(2, 0, 1, 0, new int[] { })]
        [InlineData(2, 0, 1, 1, new int[] { 0 })]
        [InlineData(2, 0, 2, 0, new int[] { })]
        [InlineData(2, 0, 2, 1, new int[] { 0 })]
        [InlineData(2, 0, 2, 2, new int[] { 0, 1 })]
        [InlineData(2, 1, 0, 0, new int[] { })]
        [InlineData(2, 1, 1, 0, new int[] { })]
        [InlineData(2, 1, 1, 1, new int[] { 1 })]
        [InlineData(2, 2, 0, 0, new int[] { })]
        public void GetProcessedItems_should_return_expected_result(int length, int offset, int count, int processedCount, int[] expectedResult)
        {
            var subject = CreateSubject(length: length, offset: offset, count: count);
            subject.SetProcessedCount(processedCount);

            var result = subject.GetProcessedItems();

            result.Should().Equal(expectedResult);
        }

        [Theory]
        [InlineData(0, 0, 0, 0, new int[] { })]
        [InlineData(1, 0, 0, 0, new int[] { })]
        [InlineData(1, 0, 1, 0, new int[] { 0 })]
        [InlineData(1, 0, 1, 1, new int[] { })]
        [InlineData(1, 1, 0, 0, new int[] { })]
        [InlineData(2, 0, 0, 0, new int[] { })]
        [InlineData(2, 0, 1, 0, new int[] { 0 })]
        [InlineData(2, 0, 1, 1, new int[] { })]
        [InlineData(2, 0, 2, 0, new int[] { 0, 1 })]
        [InlineData(2, 0, 2, 1, new int[] { 1 })]
        [InlineData(2, 0, 2, 2, new int[] { })]
        [InlineData(2, 1, 0, 0, new int[] { })]
        [InlineData(2, 1, 1, 0, new int[] { 1 })]
        [InlineData(2, 1, 1, 1, new int[] { })]
        [InlineData(2, 2, 0, 0, new int[] { })]
        public void GetUnprocessedItems_should_return_expected_result(int length, int offset, int count, int processedCount, int[] expectedResult)
        {
            var subject = CreateSubject(length: length, offset: offset, count: count);
            subject.SetProcessedCount(processedCount);

            var result = subject.GetUnprocessedItems();

            result.Should().Equal(expectedResult);
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(1, 0, 0, 0)]
        [InlineData(1, 0, 1, 0)]
        [InlineData(1, 0, 1, 1)]
        [InlineData(1, 1, 0, 0)]
        [InlineData(2, 0, 0, 0)]
        [InlineData(2, 1, 0, 0)]
        [InlineData(2, 1, 1, 0)]
        [InlineData(2, 1, 1, 1)]
        [InlineData(2, 2, 0, 0)]
        public void SetProcessedCount_should_have_expected_result(int length, int offset, int count, int value)
        {
            var subject = CreateSubject(length: length, offset: offset, count: count, canBeSplit: true);

            subject.SetProcessedCount(value);

            subject.ProcessedCount.Should().Be(value);
        }

        [Theory]
        [InlineData(2, 0)]
        [InlineData(2, 1)]
        [InlineData(3, 0)]
        [InlineData(3, 1)]
        [InlineData(3, 2)]
        [InlineData(4, 0)]
        [InlineData(4, 1)]
        [InlineData(4, 2)]
        [InlineData(4, 3)]
        public void SetProcessedCount_should_throw_when_batch_cannot_be_split(int length, int value)
        {
            var subject = CreateSubject(length: length, canBeSplit: false);

            var exception = Record.Exception(() => subject.SetProcessedCount(value));

            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Theory]
        [InlineData(0, 0, -1)]
        [InlineData(0, 0, 1)]
        [InlineData(1, 0, -1)]
        [InlineData(1, 0, 1)]
        [InlineData(1, 1, -1)]
        [InlineData(1, 1, 2)]
        [InlineData(2, 0, -1)]
        [InlineData(2, 0, 1)]
        [InlineData(2, 1, -1)]
        [InlineData(2, 1, 2)]
        [InlineData(2, 2, -1)]
        [InlineData(2, 2, 3)]
        public void SetProcessedCount_should_throw_when_value_is_invalid(int length, int count, int value)
        {
            var subject = CreateSubject(length: length, count: count);

            var exception = Record.Exception(() => subject.SetProcessedCount(value));

            var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
            e.ParamName.Should().Be("value");
        }

        // private methods
        private BatchableSource<int> CreateSubject(
            int? length = null,
            int? offset = null,
            int? count = null,
            bool canBeSplit = true)
        {
            var list = Enumerable.Range(0, length ?? 3).ToList();
            offset = offset ?? 0;
            count = count ?? list.Count;
            return new BatchableSource<int>(list, offset.Value, count.Value, canBeSplit);
        }
    }
}
