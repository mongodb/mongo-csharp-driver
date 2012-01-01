/* Copyright 2010-2012 10gen Inc.
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
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace MongoDB.DriverOnlineTests.Jira.CSharp358
{
    [TestFixture]
    public class CSharp358Tests
    {
        [Test]
        public void TestInsertUpdateAndSaveWithElementNameStartingWithDollarSign()
        {
            var server = Configuration.TestServer;
            var database = Configuration.TestDatabase;
            var collection = Configuration.TestCollection;
            collection.Drop();

            var document = new BsonDocument
            {
                { "_id", 1 },
                { "v", new BsonDocument("$x", 1) } // server doesn't allow "$" at top level
            };
            var insertOptions = new MongoInsertOptions { CheckElementNames = false };
            collection.Insert(document, insertOptions);
            document = collection.FindOne();
            Assert.AreEqual(1, document["v"].AsBsonDocument["$x"].AsInt32);

            document["v"].AsBsonDocument["$x"] = 2;
            var query = Query.EQ("_id", 1);
            var update = Update.Replace(document);
            var updateOptions = new MongoUpdateOptions { CheckElementNames = false };
            collection.Update(query, update, updateOptions);
            document = collection.FindOne();
            Assert.AreEqual(2, document["v"].AsBsonDocument["$x"].AsInt32);

            document["v"].AsBsonDocument["$x"] = 3;
            collection.Save(document, insertOptions);
            document = collection.FindOne();
            Assert.AreEqual(3, document["v"].AsBsonDocument["$x"].AsInt32);
        }
    }
}
