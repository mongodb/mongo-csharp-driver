/* Copyright 2010-2016 MongoDB Inc.
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
using Xunit;

namespace MongoDB.Driver.Tests.Jira.CSharp258
{
    public class CSharp258Tests
    {
        public class C
        {
            public ObjectId Id { get; set; }
            public DateTime DateTime { get; set; }
        }

        private MongoCollection<C> _collection;

        public CSharp258Tests()
        {
            _collection = LegacyTestConfiguration.GetCollection<C>();
        }

        [Fact]
        public void TestDateTimePropertyWithNewMaxDateTimeRepresentation()
        {
            _collection.RemoveAll();
            _collection.Insert(
                new BsonDocument
                {
                    { "_id", ObjectId.GenerateNewId() },
                    { "DateTime", new BsonDateTime(253402300799999) }
                });

            var c = _collection.FindOne();
            Assert.Equal(DateTime.MaxValue, c.DateTime);
        }

        [Fact]
        public void TestDateTimePropertyWithOldMaxDateTimeRepresentation()
        {
            _collection.RemoveAll();
            _collection.Insert(
                new BsonDocument
                {
                    { "_id", ObjectId.GenerateNewId() },
                    { "DateTime", new BsonDateTime(253402300800000) }
                });

            var c = _collection.FindOne();
            Assert.Equal(DateTime.MaxValue, c.DateTime);
        }

        [Fact]
        public void TestDocumentWithNewMaxDateTimeRepresentation()
        {
            _collection.RemoveAll();
            _collection.Insert(
                new BsonDocument
                {
                    { "_id", ObjectId.GenerateNewId() },
                    { "DateTime", new BsonDateTime(253402300799999) }
                });

            var document = _collection.FindOneAs<BsonDocument>();
            Assert.Equal(DateTime.MaxValue, document["DateTime"].ToUniversalTime());
            Assert.Equal(253402300799999, document["DateTime"].AsBsonDateTime.MillisecondsSinceEpoch);
        }

        [Fact]
        public void TestDocumentWithOldMaxDateTimeRepresentation()
        {
            _collection.RemoveAll();
            _collection.Insert(
                new BsonDocument
                {
                    { "_id", ObjectId.GenerateNewId() },
                    { "DateTime", new BsonDateTime(253402300800000) }
                });

            var document = _collection.FindOneAs<BsonDocument>();
            Assert.Equal(DateTime.MaxValue, document["DateTime"].ToUniversalTime());
            Assert.Equal(253402300799999, document["DateTime"].AsBsonDateTime.MillisecondsSinceEpoch);
        }
    }
}
