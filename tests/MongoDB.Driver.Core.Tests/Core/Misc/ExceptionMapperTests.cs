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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using Xunit;

namespace MongoDB.Driver.Core.Misc
{
    public class ExceptionMapperTests
    {
        private readonly ConnectionId _connectionId = new ConnectionId(new ServerId(new ClusterId(0), new DnsEndPoint("localhost", 27017)), 0);

        [Fact]
        public void TestDoesNotThrowExceptionWhenEverythingIsKosherWithAWriteConcernResult()
        {
            var response = new BsonDocument
            {
                { "n", 1 },
                { "connectionId", 1 },
                { "ok", 1 }
            };

            var writeConcernResult = new WriteConcernResult(response);
            var ex = ExceptionMapper.Map(_connectionId, writeConcernResult);

            Assert.Null(ex);
        }

        [Fact]
        public void TestDoesNotThrowExceptionWhenEverythingIsKosherWithADocument()
        {
            var response = new BsonDocument();

            var ex = ExceptionMapper.Map(_connectionId, response);

            Assert.Null(ex);
        }

        [Theory]
        [InlineData(11000)]
        [InlineData(11001)]
        [InlineData(12582)]
        public void TestThrowsDuplicateKeyExceptionForMongod(int code)
        {
            var response = new BsonDocument
            {
                { "ok", 1 },
                { "err", string.Format("E{0} duplicate key error index: test.foo.$_id_  dup key: {{ : 1.0 }}", code) },
                { "code", code },
                { "n", 0 },
                { "connectionId", 1 }
            };

            var writeConcernResult = new WriteConcernResult(response);
            var ex = ExceptionMapper.Map(_connectionId, writeConcernResult);

            Assert.NotNull(ex);
            Assert.IsType<MongoDuplicateKeyException>(ex);
        }

        [Theory]
        [InlineData(11000)]
        [InlineData(11001)]
        [InlineData(12582)]
        public void TestThrowsDuplicateKeyExceptionForMongos(int code)
        {
            var response = new BsonDocument
            {
                { "ok", 1 },
                { "err", string.Format("E{0} duplicate key error index 1", code) },
                { "errObjects", new BsonArray
                    {
                        new BsonDocument
                        {
                            { "ok", 1 },
                            { "err", string.Format("E{0} duplicate key error index 1", code) },
                            { "code", code }
                        },
                        new BsonDocument
                        {
                            { "ok", 1 },
                            { "err", string.Format("E{0} duplicate key error index 2", code) },
                            { "code", code }
                        },
                    } }
            };

            var writeConcernResult = new WriteConcernResult(response);
            var ex = ExceptionMapper.Map(_connectionId, writeConcernResult);

            Assert.NotNull(ex);
            Assert.IsType<MongoDuplicateKeyException>(ex);
        }

        [Fact]
        public void TestThrowsWriteConcernExceptionWhenNotOk()
        {
            var response = new BsonDocument
            {
                { "err", "oops" },
                { "code", 20 },
                { "n", 0 },
                { "connectionId", 1 },
                { "ok", 0 }
            };

            var writeConcernResult = new WriteConcernResult(response);
            var ex = ExceptionMapper.Map(_connectionId, writeConcernResult);

            Assert.NotNull(ex);
            Assert.IsType<MongoWriteConcernException>(ex);
        }

        [Fact]
        public void TestThrowsWriteConcernExceptionWhenOkButHasLastErrorMessage()
        {
            var response = new BsonDocument
            {
                { "err", "oops" },
                { "code", 20 },
                { "n", 0 },
                { "connectionId", 1 },
                { "ok", 1 }
            };

            var writeConcernResult = new WriteConcernResult(response);
            var ex = ExceptionMapper.Map(_connectionId, writeConcernResult);

            Assert.NotNull(ex);
            Assert.IsType<MongoWriteConcernException>(ex);
        }

        [Theory]
        [InlineData(50)]
        [InlineData(13475)]
        [InlineData(16986)]
        [InlineData(16712)]
        public void TestThrowsExecutionTimeoutExceptionWhenCodeIsSpecified(int code)
        {
            var response = new BsonDocument
            {
                { "err", "timeout" },
                { "code", code },
            };

            var ex = ExceptionMapper.Map(_connectionId, response);

            Assert.NotNull(ex);
            Assert.IsType<MongoExecutionTimeoutException>(ex);
        }

        [Theory]
        [InlineData("exceeded time limit")]
        [InlineData("execution terminated")]
        public void TestThrowsExecutionTimeoutExceptionWhenErrMsgIsSpecified(string errmsg)
        {
            var response = new BsonDocument
            {
                { "errmsg", errmsg }
            };

            var ex = ExceptionMapper.Map(_connectionId, response);

            Assert.NotNull(ex);
            Assert.IsType<MongoExecutionTimeoutException>(ex);
        }


        [Theory]
        [InlineData(ServerErrorCode.NotMaster, typeof(MongoNotPrimaryException))]
        [InlineData(ServerErrorCode.NotMasterNoSlaveOk, typeof(MongoNotPrimaryException))]
        [InlineData(ServerErrorCode.InterruptedAtShutdown, typeof(MongoNodeIsRecoveringException))]
        [InlineData(ServerErrorCode.InterruptedDueToReplStateChange, typeof(MongoNodeIsRecoveringException))]
        [InlineData(ServerErrorCode.NotMasterOrSecondary, typeof(MongoNodeIsRecoveringException))]
        [InlineData(ServerErrorCode.PrimarySteppedDown, typeof(MongoNodeIsRecoveringException))]
        [InlineData(ServerErrorCode.ShutdownInProgress, typeof(MongoNodeIsRecoveringException))]
        [InlineData(-1, null)]
        public void MapNotPrimaryOrNodeIsRecovering_should_return_expected_result_using_code(int code, Type expectedExceptionType)
        {
            var connectionId = CreateConnectionId();
            var response = BsonDocument.Parse("{ ok : 0, code : 0 }");
            response["code"] = code;

            var result = ExceptionMapper.MapNotPrimaryOrNodeIsRecovering(connectionId, response, "errmsg");

            result?.GetType().Should().Be(expectedExceptionType);
        }

        [Theory]
        [InlineData("...not master...", typeof(MongoNotPrimaryException))]
        [InlineData("...NOT MASTER...", typeof(MongoNotPrimaryException))]
        [InlineData("...node is recovering...", typeof(MongoNodeIsRecoveringException))]
        [InlineData("...NODE IS RECOVERING...", typeof(MongoNodeIsRecoveringException))]
        [InlineData("...not master or secondary...", typeof(MongoNodeIsRecoveringException))]
        [InlineData("...NOT MASTER OR SECONDARY...", typeof(MongoNodeIsRecoveringException))]
        [InlineData("", null)]
        public void MapNotPrimaryOrNodeIsRecovering_should_return_expected_result_using_errmsg(string errmsg, Type expectedExceptionType)
        {
            var connectionId = CreateConnectionId();
            var response = BsonDocument.Parse("{ ok : 0, errmsg : '' }");
            response["errmsg"] = errmsg;

            var result = ExceptionMapper.MapNotPrimaryOrNodeIsRecovering(connectionId, response, "errmsg");

            result?.GetType().Should().Be(expectedExceptionType);
        }

        // private methods
        private ConnectionId CreateConnectionId()
        {
            var clusterId = new ClusterId(1);
            var endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(clusterId, endPoint);
            return new ConnectionId(serverId);
        }
    }
}