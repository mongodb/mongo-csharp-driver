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
using System.Net;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using MongoDB.Bson.TestHelpers.EqualityComparers;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using Xunit;

namespace MongoDB.Driver
{
    public class MongoWriteConcernExceptionTests
    {
        private readonly ConnectionId _connectionId = new ConnectionId(new ServerId(new ClusterId(1), new DnsEndPoint("localhost", 27017)), 2).WithServerValue(3);
        private readonly string _message = "message";
        private readonly WriteConcernResult _writeConcernResult = new WriteConcernResult(new BsonDocument("result", 1));

        [Fact]
        public void constructor_should_initialize_subject()
        {
            var subject = new MongoWriteConcernException(_connectionId, _message, _writeConcernResult);

            subject.Command.Should().BeNull();
            subject.ConnectionId.Should().BeSameAs(_connectionId);
            subject.InnerException.Should().BeNull();
            subject.Message.Should().BeSameAs(_message);
            subject.Result.Should().Be(_writeConcernResult.Response);
            subject.WriteConcernResult.Should().Be(_writeConcernResult);
        }

        [Theory]
        [InlineData(ServerErrorCode.LegacyNotPrimary, typeof(MongoNotPrimaryException))]
        [InlineData(ServerErrorCode.NotWritablePrimary, typeof(MongoNotPrimaryException))]
        [InlineData(ServerErrorCode.NotPrimaryNoSecondaryOk, typeof(MongoNotPrimaryException))]
        [InlineData(OppressiveLanguageConstants.LegacyNotPrimaryErrorMessage, typeof(MongoNotPrimaryException))]
        [InlineData(ServerErrorCode.InterruptedAtShutdown, typeof(MongoNodeIsRecoveringException))] // IsShutdownError
        [InlineData(ServerErrorCode.ShutdownInProgress, typeof(MongoNodeIsRecoveringException))] // IsShutdownError
        [InlineData(ServerErrorCode.InterruptedDueToReplStateChange, typeof(MongoNodeIsRecoveringException))]
        [InlineData(ServerErrorCode.NotPrimaryOrSecondary, typeof(MongoNodeIsRecoveringException))]
        [InlineData(ServerErrorCode.PrimarySteppedDown, typeof(MongoNodeIsRecoveringException))]
        [InlineData(OppressiveLanguageConstants.LegacyNotPrimaryOrSecondaryErrorMessage, typeof(MongoNodeIsRecoveringException))]
        [InlineData("node is recovering", typeof(MongoNodeIsRecoveringException))]
        [InlineData(ServerErrorCode.MaxTimeMSExpired, typeof(MongoExecutionTimeoutException))]
        [InlineData(13475, typeof(MongoExecutionTimeoutException))]
        [InlineData(16986, typeof(MongoExecutionTimeoutException))]
        [InlineData(16712, typeof(MongoExecutionTimeoutException))]
        [InlineData("exceeded time limit", typeof(MongoExecutionTimeoutException))]
        [InlineData("execution terminated", typeof(MongoExecutionTimeoutException))]
        [InlineData(-1, null)]
        [InlineData("test", null)]
        public void constructor_should_should_map_writeConcernResult(object exceptionInfo, Type expectedExceptionType)
        {
            var response = new BsonDocument
            {
                {
                    "writeConcernError",
                    Enum.TryParse<ServerErrorCode>(exceptionInfo.ToString(), out var errorCode)
                        ? new BsonDocument("code", (int)errorCode)
                        : new BsonDocument("errmsg", exceptionInfo.ToString())
                }
            };
            var writeConcernResult = new WriteConcernResult(response);
            var writeConcernException = new MongoWriteConcernException(_connectionId, "dummy", writeConcernResult);

            var result = writeConcernException.MappedWriteConcernResultException;

            if (expectedExceptionType != null)
            {
                result.GetType().Should().Be(expectedExceptionType);
            }
            else
            {
                result.Should().BeNull();
            }
        }

        [Fact]
        public void Serialization_should_work()
        {
            var subject = new MongoWriteConcernException(_connectionId, _message, _writeConcernResult);

            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
#pragma warning disable SYSLIB0011 // BinaryFormatter serialization is obsolete
                formatter.Serialize(stream, subject);
                stream.Position = 0;
                var rehydrated = (MongoWriteConcernException)formatter.Deserialize(stream);
#pragma warning restore SYSLIB0011 // BinaryFormatter serialization is obsolete

                rehydrated.Command.Should().BeNull();
                rehydrated.ConnectionId.Should().Be(subject.ConnectionId);
                rehydrated.InnerException.Should().BeNull();
                rehydrated.Message.Should().Be(subject.Message);
                rehydrated.Result.Should().Be(subject.Result);
                rehydrated.WriteConcernResult.Should().BeUsing(subject.WriteConcernResult, EqualityComparerRegistry.Default);
            }
        }
    }
}
