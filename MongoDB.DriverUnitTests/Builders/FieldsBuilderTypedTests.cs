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
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Builders
{
    [TestFixture]
    public class FieldsBuilderTypedTests
    {
        public class TestClass
        {
            public int[] a;
            public SubClass[] a2;
            public int x;
        }

        public class SubClass
        {
            public int b;
        }

        [Test]
        public void TestElemMatch()
        {
            var fields = Fields<TestClass>.ElemMatch(tc => tc.a2, qb => qb.EQ(tc => tc.b, 10));
            string expected = "{ \"a2\" : { \"$elemMatch\" : { \"b\" : 10 } } }";
            Assert.AreEqual(expected, fields.ToJson());
        }

        [Test]
        public void TestIncludeElemMatch()
        {
            var fields = Fields<TestClass>.Include(tc => tc.x).ElemMatch(tc => tc.a2, qb => qb.EQ(tc => tc.b, 10));
            string expected = "{ \"x\" : 1, \"a2\" : { \"$elemMatch\" : { \"b\" : 10 } } }";
            Assert.AreEqual(expected, fields.ToJson());
        }

        [Test]
        public void TestInclude()
        {
            var fields = Fields<TestClass>.Include(tc => tc.a);
            string expected = "{ \"a\" : 1 }";
            Assert.AreEqual(expected, fields.ToJson());
        }

        [Test]
        public void TestExclude()
        {
            var fields = Fields<TestClass>.Exclude(tc => tc.a);
            string expected = "{ \"a\" : 0 }";
            Assert.AreEqual(expected, fields.ToJson());
        }

        [Test]
        public void TestSliceNameSize()
        {
            var fields = Fields<TestClass>.Slice(tc => tc.a, 10);
            string expected = "{ \"a\" : { \"$slice\" : 10 } }";
            Assert.AreEqual(expected, fields.ToJson());
        }

        [Test]
        public void TestSliceNameSkipLimit()
        {
            var fields = Fields<TestClass>.Slice(tc => tc.a, 10, 20);
            string expected = "{ \"a\" : { \"$slice\" : [10, 20] } }";
            Assert.AreEqual(expected, fields.ToJson());
        }
        [Test]
        public void TestIncludeInclude()
        {
            var fields = Fields<TestClass>.Include(tc => tc.x).Include(tc => tc.a);
            string expected = "{ \"x\" : 1, \"a\" : 1 }";
            Assert.AreEqual(expected, fields.ToJson());
        }

        [Test]
        public void TesIncludetExclude()
        {
            var fields = Fields<TestClass>.Include(tc => tc.x).Exclude(tc => tc.a);
            string expected = "{ \"x\" : 1, \"a\" : 0 }";
            Assert.AreEqual(expected, fields.ToJson());
        }

        [Test]
        public void TestIncludeSliceNameSize()
        {
            var fields = Fields<TestClass>.Include(tc => tc.x).Slice(tc => tc.a, 10);
            string expected = "{ \"x\" : 1, \"a\" : { \"$slice\" : 10 } }";
            Assert.AreEqual(expected, fields.ToJson());
        }

        [Test]
        public void TestIncludeSliceNameSkipLimit()
        {
            var fields = Fields<TestClass>.Include(tc => tc.x).Slice(tc => tc.a, 10, 20);
            string expected = "{ \"x\" : 1, \"a\" : { \"$slice\" : [10, 20] } }";
            Assert.AreEqual(expected, fields.ToJson());
        }
    }
}