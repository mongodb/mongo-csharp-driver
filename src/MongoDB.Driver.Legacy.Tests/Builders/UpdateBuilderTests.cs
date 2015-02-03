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
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace MongoDB.Driver.Tests.Builders
{
    [TestFixture]
    public class UpdateBuilderTests
    {
        private class Test
        {
            public int Id = 0;

            [BsonElement("x")]
            public int X = 0;

            [BsonElement("xl")]
            public long XL = 0;

            [BsonElement("xd")]
            public double XD = 0;

            [BsonElement("y")]
            public int[] Y { get; set; }

            [BsonElement("b")]
            public List<B> B { get; set; }

            [BsonElement("dAsDateTime")]
            public DateTime DAsDateTime { get; set; }

            [BsonElement("dAsInt64")]
            [BsonDateTimeOptions(Representation=BsonType.Int64)]
            public DateTime DAsInt64 { get; set; }

            [BsonElement("bdt")]
            public BsonDateTime BsonDateTime { get; set; }

            [BsonElement("bts")]
            public BsonTimestamp BsonTimestamp { get; set; }
        }

        private class B
        {
            [BsonElement("c")]
            public int C = 0;
        }

        private class C
        {
            public int X = 0;
        }

        private MongoCollection<BsonDocument> _collection;

        private C _a = new C { X = 1 };
        private C _b = new C { X = 2 };
        private BsonDocument _docA1 = new BsonDocument("a", 1);
        private BsonDocument _docA2 = new BsonDocument("a", 2);

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            _collection = LegacyTestConfiguration.Collection;
        }

 
        [Test]
        public void TestReplaceWithInvalidFieldName()
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument { { "_id", 1 }, { "x", 1 } });

            var query = Query.EQ("_id", 1);
            var update = Update.Replace(new BsonDocument { { "_id", 1 }, { "$x", 1 } });
            Assert.Throws<BsonSerializationException>(() => { _collection.Update(query, update); });
        }
    }
}
