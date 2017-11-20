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

using FluentAssertions;
using MongoDB.Bson;
using Xunit;

namespace MongoDB.Driver.Core.Bindings
{
    public class NoCoreSessionTests
    {
        [Fact]
        public void Instance_should_return_expected_result()
        {
            var result = NoCoreSession.Instance;

            result.ClusterTime.Should().BeNull();
            result.Id.Should().BeNull();
            result.IsImplicit.Should().BeTrue();
            result.OperationTime.Should().BeNull();
        }

        [Fact]
        public void Instance_should_return_cached_instance()
        {
            var result1 = NoCoreSession.Instance;
            var result2 = NoCoreSession.Instance;

            result2.Should().BeSameAs(result1);
        }

        [Fact]
        public void NewHandle_should_return_expected_result()
        {
            var result = NoCoreSession.NewHandle();

            var handle = result.Should().BeOfType<CoreSessionHandle>().Subject;
            var referenceCounted = handle.Wrapped.Should().BeOfType<ReferenceCountedCoreSession>().Subject;
            referenceCounted.Wrapped.Should().BeSameAs(NoCoreSession.Instance);
        }

        [Fact]
        public void ClusterTime_should_return_expected_result()
        {
            var subject = CreateSubject();

            var result = subject.ClusterTime;

            result.Should().BeNull();
        }

        [Fact]
        public void Id_should_return_expected_result()
        {
            var subject = CreateSubject();

            var result = subject.Id;

            result.Should().BeNull();
        }

        [Fact]
        public void IsImplicit_should_return_expected_result()
        {
            var subject = CreateSubject();

            var result = subject.IsImplicit;

            result.Should().BeTrue();
        }

        [Fact]
        public void AdvanceClusterTime_should_do_nothing()
        {
            var subject = CreateSubject();
            var newClusterTime = CreateClusterTime();

            subject.AdvanceClusterTime(newClusterTime);
        }

        [Fact]
        public void AdvanceOperationTime_should_do_nothing()
        {
            var subject = CreateSubject();
            var newOperationTime = CreateOperationTime();

            subject.AdvanceOperationTime(newOperationTime);
        }

        [Fact]
        public void Dispose_should_do_nothing()
        {
            var subject = CreateSubject();

            subject.Dispose();

            // verify that no ObjectDisposedExceptions are thrown
            var clusterTime = subject.ClusterTime;
            var id = subject.Id;
            var isImplicit = subject.IsImplicit;
            var operationTime = subject.OperationTime;
            subject.AdvanceClusterTime(CreateClusterTime());
            subject.AdvanceOperationTime(CreateOperationTime());
            subject.WasUsed();
        }

        [Fact]
        public void Dispose_can_be_called_more_than_once()
        {
            var subject = CreateSubject();

            subject.Dispose();
            subject.Dispose();
        }

        [Fact]
        public void WasUsed_should_do_nothing()
        {
            var subject = CreateSubject();

            subject.WasUsed();
        }

        // private methods
        private BsonDocument CreateClusterTime()
        {
            return new BsonDocument
            {
                { "xyz", 1 },
                { "clusterTime", new BsonTimestamp(1L) }
            };
        }

        private BsonTimestamp CreateOperationTime()
        {
            return new BsonTimestamp(1L);
        }

        private NoCoreSession CreateSubject()
        {
            return new NoCoreSession();
        }
    }
}
