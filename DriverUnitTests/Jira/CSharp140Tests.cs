/* Copyright 2010 10gen Inc.
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
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.DefaultSerializer;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace MongoDB.DriverUnitTests.Jira.CSharp140 {
    [TestFixture]
    public class CSharp140Tests {
        private class C {
            public int X;
        }

        [Test]
        public void TestSerializeAnonymousClass() {
            object a = new { X = 1 };
            var json = a.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestSerializeCommandWrapped() {
            object c = new C { X = 1 };
            object w = CommandWrapper.Create(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestSerializeFieldsWrapped() {
            object c = new C { X = 1 };
            object w = Fields.Wrap(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestSerializeGeoNearOptionsWrapped() {
            object c = new C { X = 1 };
            object w = GeoNearOptions.Wrap(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestSerializeGroupByWrapped() {
            object c = new C { X = 1 };
            object w = GroupBy.Wrap(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestSerializeIndexKeysWrapped() {
            object c = new C { X = 1 };
            object w = IndexKeys.Wrap(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestSerializeIndexOptionsWrapped() {
            object c = new C { X = 1 };
            object w = IndexOptions.Wrap(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestSerializeMapReduceOptionsWrapped() {
            object c = new C { X = 1 };
            object w = MapReduceOptions.Wrap(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestSerializeQueryWrapped() {
            object c = new C { X = 1 };
            object w = Query.Wrap(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestSerializeScopeWrapped() {
            object c = new C { X = 1 };
            object w = ScopeWrapper.Create(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestSerializeSortByWrapped() {
            object c = new C { X = 1 };
            object w = SortBy.Wrap(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestSerializeUpdateWrapped() {
            object c = new C { X = 1 };
            object w = Update.Wrap(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestSerializeUpdateReplace() {
            object c = new C { X = 1 };
            object w = Update.Replace<object>(c);
            var json = w.ToJson();
            var expected = "{ '_t' : 'C', 'X' : 1 }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }
    }
}
