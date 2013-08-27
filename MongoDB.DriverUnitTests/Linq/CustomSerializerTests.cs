/* Copyright 2010-2013 10gen Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Linq
{
    [TestFixture]
    public class CustomSerializerTests
    {
        [Test]
        public void TestStringIndexers()
        {
            var query = Query<MongoDocument>.Where(x => x["Name"] == "awesome");
            var expected = "{ \"Name\" : \"awesome\" }";
            Assert.AreEqual(expected, query.ToJson());
        }

        [Test]
        public void TestThrowsExceptionWhenMemberDoesNotExist()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Query<MongoDocument>.Where(x => x["ThrowMe"] == 42));
        }

        [Serializable]
        [BsonSerializer(typeof(MongoDocumentClassSerializer))]
        public class MongoDocument : BsonDocumentBackedClass
        {
            public MongoDocument()
                : base(new MongoDocumentClassSerializer())
            {
            }

            public MongoDocument(BsonDocument backingDocument)
                : base(backingDocument, new MongoDocumentClassSerializer())
            {
            }

            public MongoDocument(BsonDocument backingDocument, IBsonDocumentSerializer serializer)
                : base(backingDocument, serializer)
            {

            }

            [BsonId]
            public ObjectId Id { get; set; }

            public BsonValue this[string fieldname]
            {
                get { return this.BackingDocument[fieldname]; }
                set { this.BackingDocument[fieldname] = value; }
            }

            public static implicit operator BsonDocument(MongoDocument document)
            {
                return document.BackingDocument;
            }
        }

        public class MongoDocumentClassSerializer : BsonDocumentBackedClassSerializer<MongoDocument>
        {
            public MongoDocumentClassSerializer()
            {
                this.RegisterMember("Id", "_id", new ObjectIdSerializer(), typeof(ObjectId), null);
                this.RegisterMember("Name", "name", new StringSerializer(), typeof(string), null);
            }

            protected override MongoDocument CreateInstance(BsonDocument backingDocument)
            {
                return new MongoDocument(backingDocument, this);
            }
        }
    }
}