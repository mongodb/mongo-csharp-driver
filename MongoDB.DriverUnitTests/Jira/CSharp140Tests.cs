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
using MongoDB.Driver.Builders;
using MongoDB.Driver.Wrappers;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira.CSharp140
{
    [TestFixture]
    public class CSharp140Tests
    {
        private class C
        {
            public int X;
        }

        [Test]
        public void TestSerializeAnonymousClass()
        {
            object a = new { X = 1 };
            var json = a.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestSerializeCommandWrapped()
        {
            object c = new C { X = 1 };
            object w = CommandWrapper.Create(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestSerializeFieldsWrapped()
        {
            object c = new C { X = 1 };
            object w = FieldsWrapper.Create(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestSerializeGeoNearOptionsWrapped()
        {
            object c = new C { X = 1 };
            object w = GeoNearOptionsWrapper.Create(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestSerializeGroupByWrapped()
        {
            object c = new C { X = 1 };
            object w = GroupByWrapper.Create(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestSerializeIndexKeysWrapped()
        {
            object c = new C { X = 1 };
            object w = IndexKeysWrapper.Create(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestSerializeIndexOptionsWrapped()
        {
            object c = new C { X = 1 };
            object w = IndexOptionsWrapper.Create(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestSerializeMapReduceOptionsWrapped()
        {
            object c = new C { X = 1 };
            object w = MapReduceOptionsWrapper.Create(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestSerializeQueryWrapped()
        {
            object c = new C { X = 1 };
            object w = QueryWrapper.Create(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestSerializeScopeWrapped()
        {
            object c = new C { X = 1 };
            object w = ScopeWrapper.Create(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestSerializeSortByWrapped()
        {
            object c = new C { X = 1 };
            object w = SortByWrapper.Create(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestSerializeUpdateWrapped()
        {
            var c = new C { X = 1 };
            var w = UpdateWrapper.Create(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestSerializeUpdateReplace()
        {
            object c = new C { X = 1 };
            object w = Update.Replace<object>(c);
            var json = w.ToJson();
            var expected = "{ '_t' : 'C', 'X' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }
    }
}
