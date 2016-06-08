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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver
{
    public class BatchTransformingAsyncCursorTests
    {
        [Theory]
        [ParameterAttributeData]
        public void Should_provide_back_all_results(
            [Values(false, true)]
            bool async)
        {
            var source = Enumerable.Range(0, 15);
            var cursor = new ListBasedAsyncCursor<int>(source, 5);

            var subject = new BatchTransformingAsyncCursor<int, string>(cursor, x => x.Select(y => y.ToString()));

            var result = MoveNext(subject, async);
            result.Should().BeTrue();
            var batch = subject.Current.ToList();
            batch.Should().Equal("0", "1", "2", "3", "4");

            result = MoveNext(subject, async);
            result.Should().BeTrue();
            batch = subject.Current.ToList();
            batch.Should().Equal("5", "6", "7", "8", "9");

            result = MoveNext(subject, async);
            result.Should().BeTrue();
            batch = subject.Current.ToList();
            batch.Should().Equal("10", "11", "12", "13", "14");

            result = MoveNext(subject, async);
            result.Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void Should_provide_back_a_filtered_list(
            [Values(false, true)]
            bool async)
        {
            var source = Enumerable.Range(0, 15);
            var cursor = new ListBasedAsyncCursor<int>(source, 5);

            var subject = new BatchTransformingAsyncCursor<int, int>(cursor, x => x.Where(y => y % 2 == 0));

            var result = MoveNext(subject, async);
            result.Should().BeTrue();
            var batch = subject.Current.ToList();
            batch.Should().Equal(0, 2, 4);

            result = MoveNext(subject, async);
            result.Should().BeTrue();
            batch = subject.Current.ToList();
            batch.Should().Equal(6, 8);

            result = MoveNext(subject, async);
            result.Should().BeTrue();
            batch = subject.Current.ToList();
            batch.Should().Equal(10, 12, 14);

            result = MoveNext(subject, async);
            result.Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void Should_skip_empty_batches(
            [Values(false, true)]
            bool async)
        {
            var source = Enumerable.Range(0, 15);
            var cursor = new ListBasedAsyncCursor<int>(source, 5);

            // skip the second batch
            var subject = new BatchTransformingAsyncCursor<int, int>(cursor, x => x.Where(y => y < 5 || y > 9));

            var result = MoveNext(subject, async);
            result.Should().BeTrue();
            var batch = subject.Current.ToList();
            batch.Should().Equal(0, 1, 2, 3, 4);

            result = MoveNext(subject, async);
            result.Should().BeTrue();
            batch = subject.Current.ToList();
            batch.Should().Equal(10, 11, 12, 13, 14);

            result = MoveNext(subject, async);
            result.Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void Should_return_false_when_all_remaining_batches_are_empty(
            [Values(false, true)]
            bool async)
        {
            var source = Enumerable.Range(0, 15);
            var cursor = new ListBasedAsyncCursor<int>(source, 5);

            var subject = new BatchTransformingAsyncCursor<int, int>(cursor, x => x.Where(y => y < 8));

            var result = MoveNext(subject, async);
            result.Should().BeTrue();
            var batch = subject.Current.ToList();
            batch.Should().Equal(0, 1, 2, 3, 4);

            result = MoveNext(subject, async);
            result.Should().BeTrue();
            batch = subject.Current.ToList();
            batch.Should().Equal(5, 6, 7);

            result = MoveNext(subject, async);
            result.Should().BeFalse();
        }

        private bool MoveNext<T>(IAsyncCursor<T> cursor, bool async)
        {
            if (async)
            {
                return cursor.MoveNextAsync().GetAwaiter().GetResult();
            }
            else
            {
                return cursor.MoveNext();
            }
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

            public bool MoveNext(CancellationToken cancellationToken)
            {
                if (_index >= _full.Count)
                {
                    _current = null;
                    return false;
                }

                var count = _batchSize;
                if (_index + count > _full.Count)
                {
                    count = _full.Count - _index;
                }
                _current = _full.GetRange(_index, count);
                _index += count;
                return true;
            }

            public Task<bool> MoveNextAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(MoveNext(cancellationToken));
            }

            public void Dispose()
            {
                _current = null;
            }
        }
    }
}
