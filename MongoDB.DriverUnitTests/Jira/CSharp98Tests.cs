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

using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira.CSharp98
{
    [TestFixture]
    public class CSharp98Tests
    {
        private class A
        {
            [BsonId]
            public ObjectId Id { get; set; }
            public int PA { get; set; }
        }

        private class B : A
        {
            public int PB { get; set; }
        }

        [Test]
        public void TestDeserializationOfTwoBs()
        {
            var server = Configuration.TestServer;
            var database = Configuration.TestDatabase;
            var collection = Configuration.GetTestCollection<A>();

            collection.RemoveAll();
            var b1 = new B { PA = 1, PB = 2 };
            var b2 = new B { PA = 3, PB = 4 };
            collection.Insert<A>(b1);
            collection.Insert<A>(b2);

            var docs = collection.FindAll().ToList();
        }
    }
}
