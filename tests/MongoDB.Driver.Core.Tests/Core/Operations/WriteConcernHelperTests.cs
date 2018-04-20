/* Copyright 2018-present MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class WriteConcernHelperTests
    {
        [Theory]
        [InlineData(false, "{ }", false, null)]
        [InlineData(false, "{ }", true, null)]
        [InlineData(false, "{ w : 1 }", false, null)]
        [InlineData(false, "{ w : 1 }", true, "{ w : 1 }")]
        [InlineData(true, "{ }", false, null)]
        [InlineData(true, "{ }", true, null)]
        [InlineData(true, "{ w : 1 }", false, null)]
        [InlineData(true, "{ w : 1 }", true, null)]
        public void GetWriteConcernForCommand_should_return_expected_result(
            bool isInTransaction,
            string writeConcernJson,
            bool featureIsSupported,
            string expectedResult)
        {
            var session = CreateSession(isInTransaction: isInTransaction);
            var writeConcern = writeConcernJson == null ? null : WriteConcern.FromBsonDocument(BsonDocument.Parse(writeConcernJson));
            var requiredFeature = Feature.CommandsThatWriteAcceptWriteConcern;
            var serverVersion = requiredFeature.SupportedOrNotSupportedVersion(featureIsSupported);

            var result = WriteConcernHelper.GetWriteConcernForCommand(session, writeConcern, serverVersion, requiredFeature);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(false, "{ }", false, null)]
        [InlineData(false, "{ }", true, null)]
        [InlineData(false, "{ w : 1 }", false, null)]
        [InlineData(false, "{ w : 1 }", true, "{ w : 1 }")]
        [InlineData(true, "{ }", false, null)]
        [InlineData(true, "{ }", true, null)]
        [InlineData(true, "{ w : 1 }", false, null)]
        [InlineData(true, "{ w : 1 }", true, null)]
        public void GetWriteConcernForCommandThatWrites_should_return_expected_result(
            bool isInTransaction,
            string writeConcernJson,
            bool featureIsSupported,
            string expectedResult)
        {
            var session = CreateSession(isInTransaction: isInTransaction);
            var writeConcern = writeConcernJson == null ? null : WriteConcern.FromBsonDocument(BsonDocument.Parse(writeConcernJson));
            var serverVersion = Feature.CommandsThatWriteAcceptWriteConcern.SupportedOrNotSupportedVersion(featureIsSupported);

            var result = WriteConcernHelper.GetWriteConcernForCommandThatWrites(session, writeConcern, serverVersion);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(false, "{ }", null)]
        [InlineData(false, "{ w : 1 }", "{ w : 1 }")]
        [InlineData(true, "{ }", null)]
        [InlineData(true, "{ w : 1 }", null)]
        public void GetWriteConcernForWriteCommand_should_return_expected_result(
            bool isInTransaction,
            string writeConcernJson,
            string expectedResult)
        {
            var session = CreateSession(isInTransaction: isInTransaction);
            var writeConcern = writeConcernJson == null ? null : WriteConcern.FromBsonDocument(BsonDocument.Parse(writeConcernJson));

            var result = WriteConcernHelper.GetWriteConcernForWriteCommand(session, writeConcern);

            result.Should().Be(expectedResult);
        }

        // private methods
        private ICoreSession CreateSession(
            bool isInTransaction)
        {
            var mockSession = new Mock<ICoreSession>();
            mockSession.SetupGet(m => m.IsInTransaction).Returns(isInTransaction);
            return mockSession.Object;
        }
    }
}
