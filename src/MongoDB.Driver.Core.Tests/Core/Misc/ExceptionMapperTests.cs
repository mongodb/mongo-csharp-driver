/* Copyright 2010-2014 MongoDB Inc.
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

using System.Net;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Misc
{
    [TestFixture]
    public class ExceptionMapperTests
    {
        private readonly ConnectionId _connectionId = new ConnectionId(new ServerId(new ClusterId(0), new DnsEndPoint("localhost", 27017)), 0);

        [Test]
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

            Assert.IsNull(ex);
        }

        [Test]
        public void TestDoesNotThrowExceptionWhenEverythingIsKosherWithADocument()
        {
            var response = new BsonDocument();

            var ex = ExceptionMapper.Map(_connectionId, response);

            Assert.IsNull(ex);
        }

        [Test]
        [TestCase(11000)]
        [TestCase(11001)]
        [TestCase(12582)]
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

            Assert.IsNotNull(ex);
            Assert.IsInstanceOf<MongoDuplicateKeyException>(ex);
        }

        [Test]
        [TestCase(11000)]
        [TestCase(11001)]
        [TestCase(12582)]
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

            Assert.IsNotNull(ex);
            Assert.IsInstanceOf<MongoDuplicateKeyException>(ex);
        }

        [Test]
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

            Assert.IsNotNull(ex);
            Assert.IsInstanceOf<MongoWriteConcernException>(ex);
        }

        [Test]
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

            Assert.IsNotNull(ex);
            Assert.IsInstanceOf<MongoWriteConcernException>(ex);
        }

        [Test]
        [TestCase(50)]
        [TestCase(13475)]
        [TestCase(16986)]
        [TestCase(16712)]
        public void TestThrowsExecutionTimeoutExceptionWhenCodeIsSpecified(int code)
        {
            var response = new BsonDocument
            {
                { "err", "timeout" },
                { "code", code },
            };

            var ex = ExceptionMapper.Map(_connectionId, response);

            Assert.IsNotNull(ex);
            Assert.IsInstanceOf<MongoExecutionTimeoutException>(ex);
        }

        [Test]
        [TestCase("exceeded time limit")]
        [TestCase("execution terminated")]
        public void TestThrowsExecutionTimeoutExceptionWhenErrMsgIsSpecified(string errmsg)
        {
            var response = new BsonDocument
            {
                { "errmsg", errmsg }
            };

            var ex = ExceptionMapper.Map(_connectionId, response);

            Assert.IsNotNull(ex);
            Assert.IsInstanceOf<MongoExecutionTimeoutException>(ex);
        }
    }
}