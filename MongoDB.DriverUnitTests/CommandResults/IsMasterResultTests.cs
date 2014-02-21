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

using System;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.CommandResults
{
    [TestFixture]
    public class IsMasterResultTests
    {
        [Test]
        public void TestMaxMessageLengthWhenServerSupplied()
        {
            var document = new BsonDocument
            {
                { "ok", 1 },
                { "maxMessageSizeBytes", 1000 },
                { "maxBsonObjectSize", 1000 }
            };
            var result = new IsMasterResult(document);

            Assert.AreEqual(1000, result.MaxMessageLength);
        }

        [Test]
        public void TestMaxMessageLengthWhenNotServerSuppliedUsesMongoDefaultsWhenLargerThanMaxBsonObjectSize()
        {
            var document = new BsonDocument
            {
                { "ok", 1 },
                { "maxBsonObjectSize", MongoDefaults.MaxMessageLength - 2048 }
            };
            var result = new IsMasterResult(document);

            Assert.AreEqual(MongoDefaults.MaxMessageLength, result.MaxMessageLength);
        }

        [Test]
        public void TestMaxMessageLengthWhenNotServerSuppliedUsesMaxBsonObjectSizeWhenLargerThanMongoDefaults()
        {
            var document = new BsonDocument
            {
                { "ok", 1 },
                { "maxBsonObjectSize", MongoDefaults.MaxMessageLength }
            };
            var result = new IsMasterResult(document);

            Assert.AreEqual(MongoDefaults.MaxMessageLength + 1024, result.MaxMessageLength);
        }

        [Test]
        public void TestMaxWriteBatchSizeWhenServerSupplied()
        {
            var document = new BsonDocument
            {
                { "ok", 1 },
                { "maxWriteBatchSize", 200 }
            };
            var result = new IsMasterResult(document);

            Assert.AreEqual(200, result.MaxWriteBatchSize);
        }

        [Test]
        public void TestMaxWriteBatchSizeWhenNotServerSupplied()
        {
            var document = new BsonDocument
            {
                { "ok", 1 }
            };
            var result = new IsMasterResult(document);

            Assert.AreEqual(1000, result.MaxWriteBatchSize);
        }
    }
}
