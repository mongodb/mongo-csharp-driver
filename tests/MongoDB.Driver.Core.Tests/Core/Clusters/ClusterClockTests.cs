/* Copyright 2017 MongoDB Inc.
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
using MongoDB.Bson;
using Xunit;

namespace MongoDB.Driver.Core.Clusters
{
    public class ClusterClockTests
    {
        [Theory]
        [InlineData(null, null, null)]
        [InlineData(null, 1L, 1L)]
        [InlineData(1L, null, 1L)]
        [InlineData(1L, 1L, 1L)]
        [InlineData(1L, 2L, 2L)]
        [InlineData(2L, 1L, 2L)]
        public void GreaterClusterTime_should_return_expected_result(long? timestamp1, long? timestamp2, long? expectedTimestamp)
        {
            var x = CreateClusterTime(timestamp1);
            var y = CreateClusterTime(timestamp2);
            var expectedResult = CreateClusterTime(expectedTimestamp);

            var result = ClusterClock.GreaterClusterTime(x, y);

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void constructor_should_initialize_instance()
        {
            var result = new ClusterClock();

            result.ClusterTime.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData(1L)]
        [InlineData(2L)]
        public void ClusterTime_should_return_expected_result(long? timestamp)
        {
            var subject = CreateSubject();
            var clusterTime = CreateClusterTime(timestamp);
            if (clusterTime != null)
            {
                subject.AdvanceClusterTime(clusterTime);
            }

            var result = subject.ClusterTime;

            result.Should().Be(clusterTime);
        }

        [Theory]
        [InlineData(null, 1L, 1L)]
        [InlineData(1L, 1L, 1L)]
        [InlineData(1L, 2L, 2L)]
        [InlineData(2L, 1L, 2L)]
        public void AdvanceClusterTime_should_only_advance_cluster_time_when_new_cluster_time_is_greater(long? timestamp1, long? timestamp2, long? expectedTimestamp)
        {
            var clusterTime1 = CreateClusterTime(timestamp1);
            var clusterTime2 = CreateClusterTime(timestamp2);
            var expectedResult = CreateClusterTime(expectedTimestamp);
            var subject = CreateSubject();
            if (clusterTime1 != null)
            {
                subject.AdvanceClusterTime(clusterTime1);
            }

            subject.AdvanceClusterTime(clusterTime2);

            subject.ClusterTime.Should().Be(expectedResult);
        }

        [Fact]
        public void AdvanceClusterTime_should_throw_when_newClusterTime_is_null()
        {
            var subject = CreateSubject();

            var exception = Record.Exception(() => subject.AdvanceClusterTime(null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("newClusterTime");
        }

        // private methods
        private BsonDocument CreateClusterTime(long timestamp)
        {
            return new BsonDocument
            {
                { "xyz", 1 },
                { "clusterTime", new BsonTimestamp(timestamp) }
            };
        }

        private BsonDocument CreateClusterTime(long? timestamp)
        {
            return timestamp.HasValue ? CreateClusterTime(timestamp.Value) : null;
        }

        private ClusterClock CreateSubject()
        {
            return new ClusterClock();
        }
    }

    public class NoClusterClockTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var result = new NoClusterClock();

            result.ClusterTime.Should().BeNull();
        }

        [Fact]
        public void AdvanceClusterTime_should_do_nothing()
        {
            var subject = new NoClusterClock();
            var newClusterTime = new BsonDocument
            {
                { "xyz", 1 },
                { "clusterTime", new BsonTimestamp(1L) }
            };

            subject.AdvanceClusterTime(newClusterTime);

            subject.ClusterTime.Should().BeNull();
        }
    }
}
