/* Copyright 2013-2014 MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver
{
    public class BatchTransformingAsyncCursorTests
    {
        [Test]
        public async Task Should_provide_back_all_results()
        {
            var source = Enumerable.Range(0, 15);
            var cursor = new ListBasedAsyncCursor<int>(source, 5);

            var subject = new BatchTransformingAsyncCursor<int, string>(cursor, x => x.Select(y => y.ToString()));

            var result = await subject.MoveNextAsync(CancellationToken.None);
            result.Should().BeTrue();
            var batch = subject.Current.ToList();
            batch.Should().Equal("0", "1", "2", "3", "4");

            result = await subject.MoveNextAsync(CancellationToken.None);
            result.Should().BeTrue();
            batch = subject.Current.ToList();
            batch.Should().Equal("5", "6", "7", "8", "9");

            result = await subject.MoveNextAsync(CancellationToken.None);
            result.Should().BeTrue();
            batch = subject.Current.ToList();
            batch.Should().Equal("10", "11", "12", "13", "14");

            result = await subject.MoveNextAsync(CancellationToken.None);
            result.Should().BeFalse();
        }

        [Test]
        public async Task Should_provide_back_a_filtered_list()
        {
            var source = Enumerable.Range(0, 15);
            var cursor = new ListBasedAsyncCursor<int>(source, 5);

            var subject = new BatchTransformingAsyncCursor<int, int>(cursor, x => x.Where(y => y % 2 == 0));

            var result = await subject.MoveNextAsync(CancellationToken.None);
            result.Should().BeTrue();
            var batch = subject.Current.ToList();
            batch.Should().Equal(0, 2, 4);

            result = await subject.MoveNextAsync(CancellationToken.None);
            result.Should().BeTrue();
            batch = subject.Current.ToList();
            batch.Should().Equal(6, 8);

            result = await subject.MoveNextAsync(CancellationToken.None);
            result.Should().BeTrue();
            batch = subject.Current.ToList();
            batch.Should().Equal(10, 12, 14);

            result = await subject.MoveNextAsync(CancellationToken.None);
            result.Should().BeFalse();
        }

        [Test]
        public async Task Should_skip_empty_batches()
        {
            var source = Enumerable.Range(0, 15);
            var cursor = new ListBasedAsyncCursor<int>(source, 5);

            // skip the second batch
            var subject = new BatchTransformingAsyncCursor<int, int>(cursor, x => x.Where(y => y < 5 || y > 9));

            var result = await subject.MoveNextAsync(CancellationToken.None);
            result.Should().BeTrue();
            var batch = subject.Current.ToList();
            batch.Should().Equal(0, 1, 2, 3, 4);

            result = await subject.MoveNextAsync(CancellationToken.None);
            result.Should().BeTrue();
            batch = subject.Current.ToList();
            batch.Should().Equal(10, 11, 12, 13, 14);

            result = await subject.MoveNextAsync(CancellationToken.None);
            result.Should().BeFalse();
        }

        [Test]
        public async Task Should_return_false_when_all_remaining_batches_are_empty()
        {
            var source = Enumerable.Range(0, 15);
            var cursor = new ListBasedAsyncCursor<int>(source, 5);

            var subject = new BatchTransformingAsyncCursor<int, int>(cursor, x => x.Where(y => y < 8));

            var result = await subject.MoveNextAsync(CancellationToken.None);
            result.Should().BeTrue();
            var batch = subject.Current.ToList();
            batch.Should().Equal(0, 1, 2, 3, 4);

            result = await subject.MoveNextAsync(CancellationToken.None);
            result.Should().BeTrue();
            batch = subject.Current.ToList();
            batch.Should().Equal(5, 6, 7);

            result = await subject.MoveNextAsync(CancellationToken.None);
            result.Should().BeFalse();
        }


        private class ListBasedAsyncCursor<T> : IAsyncCursor<T>
        {
            private readonly List<T> _full;
            private readonly int _batchSize;
            private int _index;
            private List<T> _current;

            public ListBasedAsyncCursor(IEnumerable<T> full, int batchSize)
            {
                _full = full.ToList();
                _batchSize = batchSize;
            }

            public System.Collections.Generic.IEnumerable<T> Current
            {
                get
                {
                    if (_current == null)
                    {
                        throw new Exception("AAAHHHHH");
                    }

                    return _current;
                }
            }

            public Task<bool> MoveNextAsync(CancellationToken cancellationToken)
            {
                if (_index >= _full.Count)
                {
                    _current = null;
                    return Task.FromResult(false);
                }

                var count = _batchSize;
                if (_index + count > _full.Count)
                {
                    count = _full.Count - _index;
                }
                _current = _full.GetRange(_index, count);
                _index += count;
                return Task.FromResult(true);
            }

            public void Dispose()
            {
                _current = null;
            }
        }
    }
}
