/* Copyright 2010-present MongoDB Inc.
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
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class WriteConcernHelperTests
    {
        [Theory]
        [InlineData(false, false, null, null)]
        [InlineData(false, false, "{ }", null)]
        [InlineData(false, false, "{ w : 1 }", "{ w : 1 }")]
        [InlineData(false, false, "{ wtimeout : 100 }", "{ wtimeout : 100 }")]
        [InlineData(false, false, "{ w : 1, wtimeout : 100 }", "{ w : 1, wtimeout : 100 }")]
        [InlineData(false, true, null, null)]
        [InlineData(false, true, "{ }", null)]
        [InlineData(false, true, "{ w : 1 }", "{ w : 1 }")]
        [InlineData(false, true, "{ wtimeout : 100 }", null)]
        [InlineData(false, true, "{ w : 1, wtimeout : 100 }", "{ w : 1 }")]

        [InlineData(true, false, null, null)]
        [InlineData(true, false, "{ }", null)]
        [InlineData(true, false, "{ w : 1 }", null)]
        [InlineData(true, false, "{ wtimeout : 100 }", null)]
        [InlineData(true, false, "{ w : 1, wtimeout : 100 }", null)]
        [InlineData(true, true, null, null)]
        [InlineData(true, true, "{ }", null)]
        [InlineData(true, true, "{ w : 1 }", null)]
        [InlineData(true, true, "{ wtimeout : 100 }", null)]
        [InlineData(true, true, "{ w : 1, wtimeout : 100 }", null)]
        public void GetEffectiveWriteConcern_should_return_expected_result(
            bool isInTransaction,
            bool hasOperationTimeout,
            string writeConcernJson,
            string expectedResult)
        {
            var session = CreateSession(isInTransaction: isInTransaction);
            var operationContext = hasOperationTimeout ? new OperationContext(TimeSpan.FromMilliseconds(42), CancellationToken.None) : OperationContext.NoTimeout;
            var writeConcern = writeConcernJson == null ? null : WriteConcern.FromBsonDocument(BsonDocument.Parse(writeConcernJson));

            var result = WriteConcernHelper.GetEffectiveWriteConcern(operationContext, session, writeConcern);

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
