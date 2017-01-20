/* Copyright 2010-2015 MongoDB Inc.
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
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp801
    {
        private MongoCollection<C> _collection;

        public CSharp801()
        {
            _collection = LegacyTestConfiguration.GetCollection<C>();
            if (_collection.Exists()) { _collection.Drop(); }
        }

        [Fact]
        public void GenerateIdCalledFromInsert()
        {
            _collection.RemoveAll();
            _collection.Insert(new C());
            var c = _collection.FindOne();
            Assert.Equal(1, c.Id);
        }

        [Fact]
        public void GenerateIdCalledFromSave()
        {
            _collection.RemoveAll();
            _collection.Save(new C());
            var c = _collection.FindOne();
            Assert.Equal(1, c.Id);
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
#pragma warning disable 219
                var collection = (MongoCollection<C>)container; // should not throw an InvalidCastException
#pragma warning restore
                return 1;
            }

            public bool IsEmpty(object id)
            {
                return (int)id == 0;
            }
        }
    }
}