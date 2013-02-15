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

using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira
{
    [TestFixture]
    public class CSharp672Tests
    {
        readonly MongoCollection<BsonDocument> _collection = Configuration.TestCollection;

        [SetUp]
        public void SetUp()
        {
            _collection.Drop();
        }

        class C1
        {
            public int Id;
            public BsonTimestamp Timestamp;
        }

        class D1 : C1
        {
            public int X1;
            public int X2;
        }

        [Test]
        public void EndToEndTest()
        {
            _collection.Insert(typeof(C1), new D1 { Timestamp = new BsonTimestamp(0) });
            var res = _collection.FindOneAs<D1>();
            Assert.That(res.Timestamp, Is.Not.EqualTo(new BsonTimestamp(0)));
        }
    }
}
