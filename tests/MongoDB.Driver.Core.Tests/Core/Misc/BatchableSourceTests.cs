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
    public class BatchableSourceTests
    {
        [Fact]
        public void ClearBatch_should_clear_batch()
        {
            var items = new List<int> { 1, 2 };
            var subject = new BatchableSource<int>(items.GetEnumerator());
            subject.StartBatch();
            subject.MoveNext();
            var batch = new[] { subject.Current };
            subject.EndBatch(batch);
            subject.Batch.Should().NotBeNull();

            subject.ClearBatch();
            subject.Batch.Should().BeNull();
        }

        [Fact]
        public void Constructor_with_enumerable_argument_should_initialize_instance()
        {
            var items = new List<int> { 1, 2 };
            var subject = new BatchableSource<int>(items);
            subject.Batch.Should().Equal(items);
            subject.HasMore.Should().BeFalse();
        }

        [Fact]
        public void Constructor_with_enumerable_argument_should_throw_if_batch_is_null()
        {
            Action action = () => new BatchableSource<int>((IEnumerable<int>)null);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_with_enumerator_argument_should_initialize_instance()
        {
            var items = new List<int> { 1, 2 };
            var subject = new BatchableSource<int>(items.GetEnumerator());
            subject.Batch.Should().BeNull();
            subject.HasMore.Should().BeTrue();
        }

        [Fact]
        public void Constructor_with_enumerator_argument_should_throw_if_batch_is_null()
        {
            Action action = () => new BatchableSource<int>((IEnumerator<int>)null);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void EndBatch_with_no_overflow_should_set_batch_and_set_HashMore_to_false()
        {
            var subject = new BatchableSource<int>(Enumerable.Empty<int>().GetEnumerator());
            subject.Batch.Should().BeNull();
            subject.HasMore.Should().BeTrue();
            var batch = new int[] { 1, 2 };
            subject.EndBatch(batch);
            subject.Batch.Should().BeSameAs(batch);
            subject.HasMore.Should().BeFalse();
        }

        [Fact]
        public void EndBatch_with_overflow_should_set_batch_and_set_HashMore_to_true()
        {
            var subject = new BatchableSource<int>(Enumerable.Empty<int>().GetEnumerator());
            subject.Batch.Should().BeNull();
            subject.HasMore.Should().BeTrue();
            var batch = new int[] { 1, 2 };
            var overflow = new BatchableSource<int>.Overflow { Item = 3, State = 4 };
            subject.EndBatch(batch, overflow);
            subject.Batch.Should().BeSameAs(batch);
            subject.HasMore.Should().BeTrue();
            subject.ClearBatch();
            subject.StartBatch().Should().BeSameAs(overflow);
        }

        [Fact]
        public void MoveNext_and_Current_should_enumerate_the_items()
        {
            var expectedItems = new List<int> { 1, 2 };
            var subject = new BatchableSource<int>(expectedItems.GetEnumerator());

            var items = new List<int>();
            while (subject.MoveNext())
            {
                items.Add(subject.Current);
            }

            items.Should().Equal(expectedItems);
        }

        [Fact]
        public void StartBatch_should_return_and_clear_any_overflow()
        {
            var subject = new BatchableSource<int>(Enumerable.Empty<int>().GetEnumerator());
            var batch = new int[0];
            var overflow = new BatchableSource<int>.Overflow { Item = 1, State = null };
            subject.EndBatch(batch, overflow);
            subject.ClearBatch();
            subject.StartBatch().Should().BeSameAs(overflow);
            subject.StartBatch().Should().BeNull();
        }
    }
}