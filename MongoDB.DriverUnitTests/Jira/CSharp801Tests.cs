﻿/* Copyright 2010-2013 10gen Inc.
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

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira
{
    [TestFixture]
    public class CSharp801
    {
        private MongoCollection<C> _collection;

        [SetUp]
        public void SetUp()
        {
            _collection = Configuration.GetTestCollection<C>();
            if (_collection.Exists()) { _collection.Drop(); }
        }

        [Test]
        public void GenerateIdCalledFromInsert()
        {
            _collection.RemoveAll();
            _collection.Insert(new C());
            var c = _collection.FindOne();
            Assert.AreEqual(1, c.Id);
        }

        [Test]
        public void GenerateIdCalledFromSave()
        {
            _collection.RemoveAll();
            _collection.Save(new C());
            var c = _collection.FindOne();
            Assert.AreEqual(1, c.Id);
        }

        // nested classes
        public class C
        {
            [BsonId(IdGenerator = typeof(MyIdGenerator))]
            public int Id { get; set; }
        }

        public class MyIdGenerator : IIdGenerator
        {
            public object GenerateId(object container, object document)
            {
                var collection = (MongoCollection<C>)container; // should not throw an InvalidCastException
                return 1;
            }

            public bool IsEmpty(object id)
            {
                return (int)id == 0;
            }
        }
    }
}