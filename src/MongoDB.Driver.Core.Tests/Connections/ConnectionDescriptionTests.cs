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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Tests.Connections
{
    [TestFixture]
    public class ConnectionDescriptionTests
    {
        private static readonly IsMasterResult __isMasterResult = new IsMasterResult(BsonDocument.Parse(
            "{ ok: 1, maxWriteBatchSize: 10, maxBsonObjectSize: 20, maxMessageSizeBytes: 30 }"
        ));

        private static readonly BuildInfoResult __buildInfoResult = new BuildInfoResult(BsonDocument.Parse(
            "{ ok: 1, version: \"2.6.3\" }"
        ));

        [Test]
        public void Constructor_should_throw_an_ArgumentNullException_when_isMasterResult_is_null()
        {
            Action act = () => new ConnectionDescription(null, __buildInfoResult);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_an_ArgumentNullException_when_buildInfoResult_is_null()
        {
            Action act = () => new ConnectionDescription(__isMasterResult, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void MaxBatchCount_should_return_isMasterResult_MaxBatchCount()
        {
            var subject = new ConnectionDescription(__isMasterResult, __buildInfoResult);

            subject.MaxBatchCount.Should().Be(__isMasterResult.MaxBatchCount);
        }

        [Test]
        public void MaxDocumentSize_should_return_isMasterResult_MaxDocumentSize()
        {
            var subject = new ConnectionDescription(__isMasterResult, __buildInfoResult);

            subject.MaxDocumentSize.Should().Be(__isMasterResult.MaxDocumentSize);
        }

        [Test]
        public void MaxMessageSize_should_return_isMasterResult_MaxMessageSize()
        {
            var subject = new ConnectionDescription(__isMasterResult, __buildInfoResult);

            subject.MaxMessageSize.Should().Be(__isMasterResult.MaxMessageSize);
        }

        [Test]
        public void ServerVersion_should_return_buildInfoResult_ServerVersion()
        {
            var subject = new ConnectionDescription(__isMasterResult, __buildInfoResult);

            subject.ServerVersion.Should().Be(__buildInfoResult.ServerVersion);
        }
    }
}