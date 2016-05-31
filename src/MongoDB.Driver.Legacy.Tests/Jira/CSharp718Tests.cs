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

using System.Linq;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp718
    {
        public class C
        {
            public int Id;
            public int[] Foo;
        }

        private MongoCollection<C> _collection;

        public CSharp718()
        {
            _collection = LegacyTestConfiguration.GetCollection<C>();
            TestSetup();
        }

        [Fact]
        public void TestLinqNullEquality()
        {
            var postsWithFoo = (from d in _collection.AsQueryable<C>()
                where d.Foo == null
                select d).Count();
            Assert.Equal(2, postsWithFoo);
        }

        [Fact]
        public void TestLinqNullInequality()
        {
            var postsWithFoo = (from d in _collection.AsQueryable<C>()
                where d.Foo != null
                select d).Count();
            Assert.Equal(3, postsWithFoo);
        }

        private void TestSetup()
        {
            _collection.RemoveAll();
            _collection.Insert(new C() { Id = 1});
            _collection.Insert(new C() { Id = 2, Foo = null});
            _collection.Insert(new C() { Id = 3, Foo = new int[] {1}});
            _collection.Insert(new C() { Id = 4, Foo = new int[] { 1, 2 } });
            _collection.Insert(new C() { Id = 5, Foo = new int[] { 1, 2, 3 } });
        }
    }
}