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

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq
{
    public class MongoQueryProviderTests
    {
        private class C
        {
            public ObjectId Id { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
        }

        private MongoServer _server;
        private MongoCollection _collection;

        public MongoQueryProviderTests()
        {
            _server = LegacyTestConfiguration.Server;
            _server.Connect();
            _collection = LegacyTestConfiguration.Collection;
        }

        [Fact]
        public void TestConstructor()
        {
            new MongoQueryProvider(_collection);
        }

        [Fact]
        public void TestCreateQuery()
        {
            var expression = _collection.AsQueryable<C>().Expression;
            var provider = new MongoQueryProvider(_collection);
            var query = provider.CreateQuery<C>(expression);
            Assert.Same(typeof(C), query.ElementType);
            Assert.Same(provider, query.Provider);
            Assert.Same(expression, query.Expression);
        }

        [Fact]
        public void TestCreateQueryNonGeneric()
        {
            var expression = _collection.AsQueryable<C>().Expression;
            var provider = new MongoQueryProvider(_collection);
            var query = provider.CreateQuery(expression);
            Assert.Same(typeof(C), query.ElementType);
            Assert.Same(provider, query.Provider);
            Assert.Same(expression, query.Expression);
        }
    }
}
