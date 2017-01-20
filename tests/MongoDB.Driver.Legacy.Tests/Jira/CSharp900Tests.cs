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
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp900Tests
    {
        private class B
        {
            public ObjectId Id;
            public object Value;
            public List<C> SubValues;
        }

        private class C
        {
            public object Value;

            public C(object val)
            {
                this.Value = val;
            }
        }

        private MongoCollection<B> _collection;

        public CSharp900Tests()
        {
            _collection = LegacyTestConfiguration.GetCollection<B>();

            _collection.Drop();
            _collection.CreateIndex("Value", "SubValues.Value");

            //numeric
            _collection.Insert(new B { Id = ObjectId.GenerateNewId(), Value = (byte)1, SubValues = new List<C>() { new C(2f), new C(3), new C(4D), new C(5UL) } });
            _collection.Insert(new B { Id = ObjectId.GenerateNewId(), Value = 2f, SubValues = new List<C>() { new C(6f), new C(7), new C(8D), new C(9UL) } });
            //strings
            _collection.Insert(new B { Id = ObjectId.GenerateNewId(), Value = "1", SubValues = new List<C>() { new C("2"), new C("3"), new C("4"), new C("5") } });
        }

        [Fact(Skip = "LINQ Convert")]
        public void TestEqual()
        {
            Assert.Equal(1, _collection.AsQueryable<B>().Where(x => (byte)x.Value == (byte)1).Count());
            Assert.Equal(1, _collection.AsQueryable<B>().Where(x => (float)x.Value == 1f).Count());
            Assert.Equal(1, _collection.AsQueryable<B>().Where(x => (int)x.Value == 1).Count());
            Assert.Equal(1, _collection.AsQueryable<B>().Where(x => (double)x.Value == 1D).Count());
            Assert.Equal(1, _collection.AsQueryable<B>().Where(x => (ulong)x.Value == 1UL).Count());
            Assert.Equal(1, _collection.AsQueryable<B>().Where(x => (string)x.Value == "1").Count());

            Assert.Equal(1, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (byte)y.Value == (byte)2)).Count());
            Assert.Equal(1, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (float)y.Value == 2f)).Count());
            Assert.Equal(1, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (int)y.Value == 2)).Count());
            Assert.Equal(1, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (double)y.Value == 2D)).Count());
            Assert.Equal(1, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (ulong)y.Value == 2UL)).Count());
            Assert.Equal(1, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (string)y.Value == "2")).Count());
        }

        [Fact(Skip = "LINQ Convert")]
        public void TestNotEqual()
        {
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => (byte)x.Value != (byte)1).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => (float)x.Value != 1f).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => (int)x.Value != 1).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => (double)x.Value != 1D).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => (ulong)x.Value != 1UL).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => (string)x.Value != "1").Count());

            Assert.Equal(3, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (byte)y.Value != (byte)2)).Count());
            Assert.Equal(3, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (float)y.Value != 2f)).Count());
            Assert.Equal(3, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (int)y.Value != 2)).Count());
            Assert.Equal(3, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (double)y.Value != 2D)).Count());
            Assert.Equal(3, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (ulong)y.Value != 2UL)).Count());
            Assert.Equal(3, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (string)y.Value != "2")).Count());
        }

        [Fact(Skip = "LINQ Convert")]
        public void TestGreaterThan()
        {
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => (byte)x.Value > (byte)0).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => (float)x.Value > 0f).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => (int)x.Value > 0).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => (double)x.Value > 0D).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => (ulong)x.Value > 0UL).Count());

            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (byte)y.Value > (byte)1)).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (float)y.Value > 1f)).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (int)y.Value > 1)).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (double)y.Value > 1D)).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (ulong)y.Value > 1UL)).Count());
        }

        [Fact(Skip = "LINQ Convert")]
        public void TestGreaterThanOrEqual()
        {
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => (byte)x.Value >= (byte)0).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => (float)x.Value >= 0f).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => (int)x.Value >= 0).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => (double)x.Value >= 0D).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => (ulong)x.Value >= 0UL).Count());

            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (byte)y.Value >= (byte)1)).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (float)y.Value >= 1f)).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (int)y.Value >= 1)).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (double)y.Value >= 1D)).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (ulong)y.Value >= 1UL)).Count());
        }

        [Fact(Skip = "LINQ Convert")]
        public void TestLessThan()
        {
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => (byte)x.Value < (byte)10).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => (float)x.Value < 10f).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => (int)x.Value < 10).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => (double)x.Value < 10D).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => (ulong)x.Value < 10UL).Count());

            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (byte)y.Value < (byte)10)).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (float)y.Value < 10f)).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (int)y.Value < 10)).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (double)y.Value < 10D)).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (ulong)y.Value < 10UL)).Count());
        }

        [Fact(Skip = "LINQ Convert")]
        public void TestLessThanOrEqual()
        {
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => (byte)x.Value <= (byte)10).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => (float)x.Value <= 10f).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => (int)x.Value <= 10).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => (double)x.Value <= 10D).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => (ulong)x.Value <= 10UL).Count());

            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (byte)y.Value <= (byte)10)).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (float)y.Value <= 10f)).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (int)y.Value <= 10)).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (double)y.Value <= 10D)).Count());
            Assert.Equal(2, _collection.AsQueryable<B>().Where(x => x.SubValues.Any(y => (ulong)y.Value <= 10UL)).Count());
        }
    }
}