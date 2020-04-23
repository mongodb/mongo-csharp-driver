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
using FluentAssertions;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.Tests;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class QueryHelperTests
    {
        [Theory]
        [InlineData(null, null, 0)]
        [InlineData(null, 20, 20)]
        [InlineData(20, null, 20)]
        [InlineData(10, 20, 10)]
        [InlineData(20, 10, 10)]
        [InlineData(-20, 10, -20)]
        public void CalculateFirstBatchSize_should_return_the_correct_result(int? limit, int? batchSize, int expectedResult)
        {
            var result = QueryHelper.CalculateFirstBatchSize(limit, batchSize);

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateReadPreferenceDocument_should_return_null_when_the_serverType_is_not_a_shard_router()
        {
            var result = QueryHelper.CreateReadPreferenceDocument(ServerType.ReplicaSetSecondary, ReadPreference.PrimaryPreferred, out var slaveOk);

            result.Should().BeNull();
            slaveOk.Should().BeTrue();
        }

        [Fact]
        public void CreateReadPreferenceDocument_should_return_null_when_the_readPreference_is_Primary_with_no_additional_options()
        {
            var result = QueryHelper.CreateReadPreferenceDocument(ServerType.ShardRouter, ReadPreference.Primary, out var slaveOk);

            result.Should().BeNull();
            slaveOk.Should().BeFalse();
        }

        [Fact]
        public void CreateReadPreferenceDocument_should_return_null_when_the_readPreference_is_SecondaryPreferred_with_no_additional_options()
        {
            var result = QueryHelper.CreateReadPreferenceDocument(ServerType.ShardRouter, ReadPreference.SecondaryPreferred, out var slaveOk);

            result.Should().BeNull();
            slaveOk.Should().BeTrue();
        }

        [Fact]
        public void CreateReadPreferenceDocument_should_return_a_document_when_their_are_tag_sets()
        {
            var rp = ReadPreference.Secondary.With(tagSets: new[] { new TagSet(new[] { new Tag("dc", "tx") }) });

            var result = QueryHelper.CreateReadPreferenceDocument(ServerType.ShardRouter, rp, out var slaveOk);

            result.Should().Be("{mode: \"secondary\", tags: [{dc: \"tx\"}]}");
            slaveOk.Should().BeTrue();
        }

        [Fact]
        public void CreateReadPreferenceDocument_should_return_a_document_when_maxStaleness_is_used()
        {
            var readPreference = ReadPreference.Secondary.With(maxStaleness: TimeSpan.FromSeconds(1));

            var result = QueryHelper.CreateReadPreferenceDocument(ServerType.ShardRouter, readPreference, out var slaveOk);

            result.Should().Be("{ mode : \"secondary\", maxStalenessSeconds : 1.0 }");
            slaveOk.Should().BeTrue();
        }

        [Fact]
        public void CreateReadPreferenceDocument_should_return_a_document_when_the_mode_is_not_Primary_or_SecondaryPreferred()
        {
            var result = QueryHelper.CreateReadPreferenceDocument(ServerType.ShardRouter, ReadPreference.PrimaryPreferred, out var slaveOk);

            result.Should().Be("{mode: \"primaryPreferred\"}");
            slaveOk.Should().BeTrue();
        }

        [Theory]
        [InlineData(null, "{ mode : 'secondary' }")]
        [InlineData(false, "{ mode : 'secondary', hedge : { enabled : false } }")]
        [InlineData(true, "{ mode : 'secondary', hedge : { enabled : true }  }")]
        public void CreateReadPreferenceDocument_should_return_expected_result_when_hedge_is_used(
            bool? isEnabled,
            string expectedResult)
        {
            var hedge = isEnabled.HasValue ? new ReadPreferenceHedge(isEnabled.Value) : null;
            var readPreference = ReadPreference.Secondary.With(hedge: hedge);

            var result = QueryHelper.CreateReadPreferenceDocument(ServerType.ShardRouter, readPreference, out var slaveOk);

            result.Should().Be(expectedResult);
            slaveOk.Should().BeTrue();
        }
    }
}
