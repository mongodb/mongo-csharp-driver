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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Jira
{
    [TestFixture]
    public class CSharp446Tests
    {
        [Test]
        public void TestGetDocumentId()
        {
            var document = new BsonDocument { { "_id", 1 }, { "x", "abc" } };
            object id;
            Type nominalType;
            IIdGenerator idGenerator;
            Assert.IsTrue(((IBsonIdProvider)BsonDocumentSerializer.Instance).GetDocumentId(document, out id, out nominalType, out idGenerator));
            Assert.IsInstanceOf<BsonInt32>(id);
            Assert.AreEqual(new BsonInt32(1), id);
            Assert.AreEqual(typeof(BsonValue), nominalType);
            Assert.IsNull(idGenerator);
        }

        [Test]
        public void TestSetDocumentIdBsonValue()
        {
            var document = new BsonDocument { { "x", "abc" } };
            var id = new BsonInt32(1);
            ((IBsonIdProvider)BsonDocumentSerializer.Instance).SetDocumentId(document, id);
            Assert.IsTrue(document["_id"].IsInt32);
            Assert.AreEqual(1, document["_id"].AsInt32);
        }

        [Test]
        public void TestSetDocumentIdInt32()
        {
            var document = new BsonDocument { { "x", "abc" } };
            ((IBsonIdProvider)BsonDocumentSerializer.Instance).SetDocumentId(document, 1); // 1 will be converted to a BsonInt32
            Assert.IsTrue(document["_id"].IsInt32);
            Assert.AreEqual(1, document["_id"].AsInt32);
        }
    }
}
