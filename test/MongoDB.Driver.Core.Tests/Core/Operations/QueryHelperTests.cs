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

using FluentAssertions;
using MongoDB.Driver.Core.Servers;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class QueryHelperTests
    {
        [Test]
        [TestCase(null, null, 0)]
        [TestCase(null, 20, 20)]
        [TestCase(20, null, 20)]
        [TestCase(10, 20, 10)]
        [TestCase(20, 10, 10)]
        [TestCase(-20, 10, -20)]
        public void CalculateFirstBatchSize_should_return_the_correct_result(int? limit, int? batchSize, int expectedResult)
        {
            var result = QueryHelper.CalculateFirstBatchSize(limit, batchSize);

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateReadPreferenceDocument_should_return_null_when_the_serverType_is_not_a_shard_router()
        {
            var result = QueryHelper.CreateReadPreferenceDocument(ServerType.ReplicaSetSecondary, ReadPreference.PrimaryPreferred);

            result.Should().BeNull();
        }

        [Test]
        public void CreateReadPreferenceDocument_should_return_null_when_the_readPreference_is_Primary_with_no_tag_sets()
        {
            var result = QueryHelper.CreateReadPreferenceDocument(ServerType.ShardRouter, ReadPreference.Primary);

            result.Should().BeNull();
        }

        [Test]
        public void CreateReadPreferenceDocument_should_return_null_when_the_readPreference_is_SecondaryPreferred_with_no_tag_sets()
        {
            var result = QueryHelper.CreateReadPreferenceDocument(ServerType.ShardRouter, ReadPreference.SecondaryPreferred);

            result.Should().BeNull();
        }

        [Test]
        public void CreateReadPreferenceDocument_should_return_a_document_when_their_are_tag_sets()
        {
            var rp = ReadPreference.Secondary.With(tagSets: new[] { new TagSet(new[] { new Tag("dc", "tx") }) });

            var result = QueryHelper.CreateReadPreferenceDocument(ServerType.ShardRouter, rp);

            result.Should().Be("{mode: \"secondary\", tags: [{dc: \"tx\"}]}");
        }

        [Test]
        public void CreateReadPreferenceDocument_should_return_a_document_when_the_mode_is_not_Primary_or_SecondaryPreferred()
        {
            var result = QueryHelper.CreateReadPreferenceDocument(ServerType.ShardRouter, ReadPreference.PrimaryPreferred);

            result.Should().Be("{mode: \"primaryPreferred\"}");
        }
    }
}
