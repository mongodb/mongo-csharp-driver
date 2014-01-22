using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Exceptions
{
    [TestFixture]
    public class ExceptionMapperTests
    {
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
            var ex = ExceptionMapper.Map(writeConcernResult);

            Assert.IsNull(ex);
        }

        [Test]
        public void TestDoesNotThrowExceptionWhenEverythingIsKosherWithADocument()
        {
            var response = new BsonDocument();

            var ex = ExceptionMapper.Map(response);

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
                { "err", string.Format("E{0} duplicate key error index: test.foo.$_id_  dup key: {{ : 1.0 }}", code) },
                { "code", code },
                { "n", 0 },
                { "connectionId", 1 },
                { "ok", 1 }
            };

            var writeConcernResult = new WriteConcernResult(response);
            var ex = ExceptionMapper.Map(writeConcernResult);

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
            var ex = ExceptionMapper.Map(writeConcernResult);

            Assert.IsNotNull(ex);
            Assert.IsInstanceOf<WriteConcernException>(ex);
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
            var ex = ExceptionMapper.Map(writeConcernResult);

            Assert.IsNotNull(ex);
            Assert.IsInstanceOf<WriteConcernException>(ex);
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

            var ex = ExceptionMapper.Map(response);

            Assert.IsNotNull(ex);
            Assert.IsInstanceOf<ExecutionTimeoutException>(ex);
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

            var ex = ExceptionMapper.Map(response);

            Assert.IsNotNull(ex);
            Assert.IsInstanceOf<ExecutionTimeoutException>(ex);
        }
    }
}