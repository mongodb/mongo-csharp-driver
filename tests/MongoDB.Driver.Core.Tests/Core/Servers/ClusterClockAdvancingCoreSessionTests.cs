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
using System.Reflection;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Servers
{
    public class ClusterClockAdvancingCoreSessionTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var wrapped = new Mock<ICoreSession>().Object;
            var clusterClock = new Mock<IClusterClock>().Object;

            var result = new ClusterClockAdvancingCoreSession(wrapped, clusterClock);

            result.Wrapped.Should().BeSameAs(wrapped);
            result.IsDisposed().Should().BeFalse();
            result._clusterClock().Should().BeSameAs(clusterClock);
            result._ownsWrapped().Should().BeFalse();
        }

        [Fact]
        public void constructor_should_throw_when_wrapped_is_null()
        {
            var clusterClock = new Mock<IClusterClock>().Object;

            var exception = Record.Exception(() => new ClusterClockAdvancingCoreSession(null, clusterClock));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("wrapped");
        }

        [Fact]
        public void constructor_should_throw_when_clusterClock_is_null()
        {
            var wrapped = new Mock<ICoreSession>().Object;

            var exception = Record.Exception(() => new ClusterClockAdvancingCoreSession(wrapped, null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("clusterClock");
        }

        [Fact]
        public void AdvanceClusterTime_should_advance_both_cluster_clocks()
        {
            var mockWrapped = new Mock<ICoreSession>();
            var mockClusterClock = new Mock<IClusterClock>();
            var subject = new ClusterClockAdvancingCoreSession(mockWrapped.Object, mockClusterClock.Object);
            var newClusterTime = new BsonDocument("clusterTime", new BsonTimestamp(1L));

            subject.AdvanceClusterTime(newClusterTime);

            mockWrapped.Verify(m => m.AdvanceClusterTime(newClusterTime), Times.Once);
            mockClusterClock.Verify(m => m.AdvanceClusterTime(newClusterTime), Times.Once);
        }
    }

    internal static class ClusterClockAdvancingCoreSessionReflector
    {
        public static IClusterClock _clusterClock(this ClusterClockAdvancingCoreSession obj)
        {
            var fieldInfo = typeof(ClusterClockAdvancingCoreSession).GetField("_clusterClock", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IClusterClock)fieldInfo.GetValue(obj);
        }
    }
}
