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

using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Builders
{
    [TestFixture]
    public class UpdateBuilderTests
    {
        private class Test
        {
            public int Id = 0;

            [BsonElement("x")]
            public int X = 0;

            [BsonElement("y")]
            public int[] Y { get; set; }

            [BsonElement("b")]
            public List<B> B { get; set; }
        }

        private class B
        {
            [BsonElement("c")]
            public int C = 0;
        }

        private class C
        {
            public int X = 0;
        }

        private C _a = new C { X = 1 };
        private C _b = new C { X = 2 };

        [Test]
        public void TestAddToSet()
        {
            var update = Update.AddToSet("name", "abc");
            var expected = "{ \"$addToSet\" : { \"name\" : \"abc\" } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestAddToSet_Typed()
        {
            var update = Update<Test>.AddToSet(t => t.Y, 1);
            var expected = "{ \"$addToSet\" : { \"y\" : 1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestAddToSetEach()
        {
            var update = Update.AddToSetEach("name", "abc", "def");
            var expected = "{ \"$addToSet\" : { \"name\" : { \"$each\" : [\"abc\", \"def\"] } } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestAddToSetEach_Typed()
        {
            var update = Update<Test>.AddToSetEach(t => t.Y, new [] { 1, 2 });
            var expected = "{ \"$addToSet\" : { \"y\" : { \"$each\" : [1, 2] } } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestAddToSetEachWrapped()
        {
            var update = Update.AddToSetEachWrapped("name", _a, _b, null);
            var expected = "{ \"$addToSet\" : { \"name\" : { \"$each\" : [{ \"X\" : 1 }, { \"X\" : 2 }, null] } } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestAddToSetEachWrappedNullName()
        {
            Assert.Throws<ArgumentNullException>(() => { var update = Update.AddToSetEachWrapped(null, _a); });
        }

        [Test]
        public void TestAddToSetEachWrappedNullValues()
        {
            Assert.Throws<ArgumentNullException>(() => { var update = Update.AddToSetEachWrapped<C>("name", null); });
        }

        [Test]
        public void TestAddToSetWrapped()
        {
            var update = Update.AddToSetWrapped("name", _a);
            var expected = "{ \"$addToSet\" : { \"name\" : { \"X\" : 1 } } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestAddToSetWrappedNullName()
        {
            Assert.Throws<ArgumentNullException>(() => { var update = Update.AddToSetWrapped(null, _a); });
        }

        [Test]
        public void TestAddToSetWrappedNullValue()
        {
            var update = Update.AddToSetWrapped<C>("name", null);
            var expected = "{ \"$addToSet\" : { \"name\" : null } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestBitwiseAndInt()
        {
            var update = Update.BitwiseAnd("name", 1);
            var expected = "{ '$bit' : { 'name' : { 'and' : 1 } } }".Replace("'", "\"");
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestBitwiseAndInt_Typed()
        {
            var update = Update<Test>.BitwiseAnd(t => t.X, 1);
            var expected = "{ '$bit' : { 'x' : { 'and' : 1 } } }".Replace("'", "\"");
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestBitwiseAndIntTwice()
        {
            var update = Update.BitwiseAnd("x", 1).BitwiseAnd("y", 2);
            var expected = "{ '$bit' : { 'x' : { 'and' : 1 }, 'y' : { 'and' : 2 } } }".Replace("'", "\"");
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestBitwiseAndLong()
        {
            var update = Update.BitwiseAnd("name", 1L);
            var expected = "{ '$bit' : { 'name' : { 'and' : NumberLong(1) } } }".Replace("'", "\"");
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestBitwiseAndLongTwice()
        {
            var update = Update.BitwiseAnd("x", 1L).BitwiseAnd("y", 2L);
            var expected = "{ '$bit' : { 'x' : { 'and' : NumberLong(1) }, 'y' : { 'and' : NumberLong(2) } } }".Replace("'", "\"");
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestBitwiseAndOrInt()
        {
            var update = Update.BitwiseAnd("x", 1L).BitwiseOr("x", 2L);
            var expected = "{ '$bit' : { 'x' : { 'and' : NumberLong(1), 'or' : NumberLong(2) } } }".Replace("'", "\"");
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestBitwiseAndOrInt_Typed()
        {
            var update = Update<Test>.BitwiseAnd(t => t.X, 1).BitwiseOr(t => t.X, 2);
            var expected = "{ '$bit' : { 'x' : { 'and' : 1, 'or' : 2 } } }".Replace("'", "\"");
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestBitwiseOrInt()
        {
            var update = Update.BitwiseOr("name", 1);
            var expected = "{ '$bit' : { 'name' : { 'or' : 1 } } }".Replace("'", "\"");
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestBitwiseOrInt_Typed()
        {
            var update = Update<Test>.BitwiseOr(t => t.X, 1);
            var expected = "{ '$bit' : { 'x' : { 'or' : 1 } } }".Replace("'", "\"");
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestBitwiseOrIntTwice()
        {
            var update = Update.BitwiseOr("x", 1).BitwiseOr("y", 2);
            var expected = "{ '$bit' : { 'x' : { 'or' : 1 }, 'y' : { 'or' : 2 } } }".Replace("'", "\"");
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestBitwiseOrLong()
        {
            var update = Update.BitwiseOr("name", 1L);
            var expected = "{ '$bit' : { 'name' : { 'or' : NumberLong(1) } } }".Replace("'", "\"");
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestBitwiseOrLongTwice()
        {
            var update = Update.BitwiseOr("x", 1L).BitwiseOr("y", 2L);
            var expected = "{ '$bit' : { 'x' : { 'or' : NumberLong(1) }, 'y' : { 'or' : NumberLong(2) } } }".Replace("'", "\"");
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestCombineIncSet()
        {
            var update = Update.Combine(
                Update.Inc("x", 1),
                Update.Set("y", 2)
            );
            var expected = "{ '$inc' : { 'x' : 1 }, '$set' : { 'y' : 2 } }".Replace("'", "\"");
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestCombineIncSet_Typed()
        {
            var update = Update<Test>.Combine(
                Update<Test>.Inc(t => t.X, 1),
                Update<Test>.Set(t => t.Y, new[] { 1, 2 }));

            var expected = "{ '$inc' : { 'x' : 1 }, '$set' : { 'y' : [1, 2] } }".Replace("'", "\"");
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestCombineSetSet()
        {
            var update = Update.Combine(
                Update.Set("x", 1),
                Update.Set("y", 2)
            );
            var expected = "{ '$set' : { 'x' : 1, 'y' : 2 } }".Replace("'", "\"");
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestIncDouble()
        {
            var update = Update.Inc("name", 1.1);
            var expected = "{ \"$inc\" : { \"name\" : 1.1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestIncInt()
        {
            var update = Update.Inc("name", 1);
            var expected = "{ \"$inc\" : { \"name\" : 1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestIncInt_Typed()
        {
            var update = Update<Test>.Inc(t => t.X, 1);
            var expected = "{ \"$inc\" : { \"x\" : 1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestIncLong()
        {
            var update = Update.Inc("name", 1L);
            var expected = "{ \"$inc\" : { \"name\" : NumberLong(1) } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPopFirst()
        {
            var update = Update.PopFirst("name");
            var expected = "{ \"$pop\" : { \"name\" : -1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPopFirst_Typed()
        {
            var update = Update<Test>.PopFirst(t => t.Y);
            var expected = "{ \"$pop\" : { \"y\" : -1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPopLast()
        {
            var update = Update.PopLast("name");
            var expected = "{ \"$pop\" : { \"name\" : 1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPopLast_Typed()
        {
            var update = Update<Test>.PopLast(t => t.Y);
            var expected = "{ \"$pop\" : { \"y\" : 1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPull()
        {
            var update = Update.Pull("name", "abc");
            var expected = "{ \"$pull\" : { \"name\" : \"abc\" } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPull_Typed()
        {
            var update = Update<Test>.Pull(t => t.Y, 3);
            var expected = "{ \"$pull\" : { \"y\" : 3 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPullQuery()
        {
            var update = Update.Pull("name", Query.GT("x", "abc"));
            var expected = "{ \"$pull\" : { \"name\" : { \"x\" : { \"$gt\" : \"abc\" } } } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPullQuery_Typed()
        {
            var update = Update<Test>.Pull(t => t.B, eqb => eqb.GT(b => b.C, 3));
            var expected = "{ \"$pull\" : { \"b\" : { \"c\" : { \"$gt\" : 3 } } } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPullAll()
        {
            var update = Update.PullAll("name", "abc", "def");
            var expected = "{ \"$pullAll\" : { \"name\" : [\"abc\", \"def\"] } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPullAll_Typed()
        {
            var update = Update<Test>.PullAll(t => t.Y, new [] { 1, 2});
            var expected = "{ \"$pullAll\" : { \"y\" : [1, 2] } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPullAllWrapped()
        {
            var update = Update.PullAllWrapped("name", _a, _b, null);
            var expected = "{ \"$pullAll\" : { \"name\" : [{ \"X\" : 1 }, { \"X\" : 2 }, null] } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPullAllWrappedNullName()
        {
            Assert.Throws<ArgumentNullException>(() => { var update = Update.PullAllWrapped(null, _a); });
        }

        [Test]
        public void TestPullAllWrappedNullValues()
        {
            Assert.Throws<ArgumentNullException>(() => { var update = Update.PullAllWrapped<C>("name", null); });
        }

        [Test]
        public void TestPullWrapped()
        {
            var update = Update.PullWrapped("name", _a);
            var expected = "{ \"$pull\" : { \"name\" : { \"X\" : 1 } } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPullWrappedNullName()
        {
            Assert.Throws<ArgumentNullException>(() => { var update = Update.PullWrapped(null, _a); });
        }

        [Test]
        public void TestPullWrappedNullValue()
        {
            var update = Update.PullWrapped<C>("name", null);
            var expected = "{ \"$pull\" : { \"name\" : null } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPush()
        {
            var update = Update.Push("name", "abc");
            var expected = "{ \"$push\" : { \"name\" : \"abc\" } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPush_Typed()
        {
            var update = Update<Test>.Push(t => t.Y, 7);
            var expected = "{ \"$push\" : { \"y\" : 7 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPushAll()
        {
            var update = Update.PushAll("name", "abc", "def");
            var expected = "{ \"$pushAll\" : { \"name\" : [\"abc\", \"def\"] } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPushAll_Typed()
        {
            var update = Update<Test>.PushAll(t => t.Y, new [] { 23, 32 });
            var expected = "{ \"$pushAll\" : { \"y\" : [23, 32] } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPushAllWrapped()
        {
            var update = Update.PushAllWrapped("name", _a, _b, null);
            var expected = "{ \"$pushAll\" : { \"name\" : [{ \"X\" : 1 }, { \"X\" : 2 }, null] } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPushAllWrappedNullName()
        {
            Assert.Throws<ArgumentNullException>(() => { var update = Update.PushAllWrapped(null, _a); });
        }

        [Test]
        public void TestPushAllWrappedNullValue()
        {
            Assert.Throws<ArgumentNullException>(() => { var update = Update.PushAllWrapped<C>("name", null); });
        }

        [Test]
        public void TestPushWrapped()
        {
            var update = Update.PushWrapped("name", _a);
            var expected = "{ \"$push\" : { \"name\" : { \"X\" : 1 } } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPushWrappedNullName()
        {
            Assert.Throws<ArgumentNullException>(() => { var update = Update.PushWrapped(null, _a); });
        }

        [Test]
        public void TestPushWrappedNulLValue()
        {
            var update = Update.PushWrapped<C>("name", null);
            var expected = "{ \"$push\" : { \"name\" : null } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestRename()
        {
            var update = Update.Rename("old", "new");
            var expected = "{ '$rename' : { 'old' : 'new' } }".Replace("'", "\"");
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestReplace()
        {
            var t = new Test { Id = 1, X = 2, Y = null, B = null };
            var update = Update.Replace(t);
            var expected = "{ \"_id\" : 1, \"x\" : 2, \"y\" : null, \"b\" : null }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestReplace_Typed()
        {
            var t = new Test { Id = 1, X = 2, Y = null, B = null };
            var update = Update<Test>.Replace(t);
            var expected = "{ \"_id\" : 1, \"x\" : 2, \"y\" : null, \"b\" : null }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSet()
        {
            var update = Update.Set("name", "abc");
            var expected = "{ \"$set\" : { \"name\" : \"abc\" } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSet_Typed()
        {
            var update = Update<Test>.Set(t => t.X, 42);
            var expected = "{ \"$set\" : { \"x\" : 42 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetWrapped()
        {
            var update = Update.SetWrapped<C>("name", _a);
            var expected = "{ \"$set\" : { \"name\" : { \"X\" : 1 } } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetWrappedNullName()
        {
            Assert.Throws<ArgumentNullException>(() => { var update = Update.SetWrapped(null, _a); });
        }

        [Test]
        public void TestSetWrappedNullValue()
        {
            var update = Update.SetWrapped<C>("name", null);
            var expected = "{ \"$set\" : { \"name\" : null } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestUnset()
        {
            var update = Update.Unset("name");
            var expected = "{ \"$unset\" : { \"name\" : 1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestUnset_Typed()
        {
            var update = Update<Test>.Unset(t => t.X);
            var expected = "{ \"$unset\" : { \"x\" : 1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestAddToSetTwice()
        {
            var update = Update.AddToSet("a", 1).AddToSet("b", 2);
            var expected = "{ \"$addToSet\" : { \"a\" : 1, \"b\" : 2 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestAddToSetEachTwice()
        {
            var update = Update.AddToSetEach("a", 1, 2).AddToSetEach("b", 3, 4);
            var expected = "{ \"$addToSet\" : { \"a\" : { \"$each\" : [1, 2] }, \"b\" : { \"$each\" : [3, 4] } } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestIncDoubleTwice()
        {
            var update = Update.Inc("x", 1.1).Inc("y", 2.2);
            var expected = "{ \"$inc\" : { \"x\" : 1.1, \"y\" : 2.2 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestIncIntTwice()
        {
            var update = Update.Inc("x", 1).Inc("y", 2);
            var expected = "{ \"$inc\" : { \"x\" : 1, \"y\" : 2 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestIncLongTwice()
        {
            var update = Update.Inc("x", 1L).Inc("y", 2L);
            var expected = "{ \"$inc\" : { \"x\" : NumberLong(1), \"y\" : NumberLong(2) } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPopFirstTwice()
        {
            var update = Update.PopFirst("a").PopFirst("b");
            var expected = "{ \"$pop\" : { \"a\" : -1, \"b\" : -1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPopLastTwice()
        {
            var update = Update.PopLast("a").PopLast("b");
            var expected = "{ \"$pop\" : { \"a\" : 1, \"b\" : 1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPullTwice()
        {
            var update = Update.Pull("a", 1).Pull("b", 2);
            var expected = "{ \"$pull\" : { \"a\" : 1, \"b\" : 2 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPullAllTwice()
        {
            var update = Update.PullAll("a", 1, 2).PullAll("b", 3, 4);
            var expected = "{ \"$pullAll\" : { \"a\" : [1, 2], \"b\" : [3, 4] } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPushTwice()
        {
            var update = Update.Push("a", 1).Push("b", 2);
            var expected = "{ \"$push\" : { \"a\" : 1, \"b\" : 2 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestPushAllTwice()
        {
            var update = Update.PushAll("a", 1, 2).PushAll("b", 3, 4);
            var expected = "{ \"$pushAll\" : { \"a\" : [1, 2], \"b\" : [3, 4] } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetTwice()
        {
            var update = Update.Set("a", 1).Set("b", 2);
            var expected = "{ \"$set\" : { \"a\" : 1, \"b\" : 2 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestUnsetTwice()
        {
            var update = Update.Unset("a").Unset("b");
            var expected = "{ \"$unset\" : { \"a\" : 1, \"b\" : 1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetThenAddToSet()
        {
            var update = Update.Set("x", 1).AddToSet("name", "abc");
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$addToSet\" : { \"name\" : \"abc\" } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetThenAddToSetEach()
        {
            var update = Update.Set("x", 1).AddToSetEach("name", "abc", "def");
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$addToSet\" : { \"name\" : { \"$each\" : [\"abc\", \"def\"] } } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetThenIncDouble()
        {
            var update = Update.Set("x", 1).Inc("name", 1.1);
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$inc\" : { \"name\" : 1.1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetThenIncInt()
        {
            var update = Update.Set("x", 1).Inc("name", 1);
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$inc\" : { \"name\" : 1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetThenIncLong()
        {
            var update = Update.Set("x", 1).Inc("name", 1L);
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$inc\" : { \"name\" : NumberLong(1) } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetThenPopFirst()
        {
            var update = Update.Set("x", 1).PopFirst("name");
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$pop\" : { \"name\" : -1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetThenPopLast()
        {
            var update = Update.Set("x", 1).PopLast("name");
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$pop\" : { \"name\" : 1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetThenPull()
        {
            var update = Update.Set("x", 1).Pull("name", "abc");
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$pull\" : { \"name\" : \"abc\" } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetThenPullAll()
        {
            var update = Update.Set("x", 1).PullAll("name", "abc", "def");
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$pullAll\" : { \"name\" : [\"abc\", \"def\"] } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetThenPush()
        {
            var update = Update.Set("x", 1).Push("name", "abc");
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$push\" : { \"name\" : \"abc\" } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetThenPushAll()
        {
            var update = Update.Set("x", 1).PushAll("name", "abc", "def");
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$pushAll\" : { \"name\" : [\"abc\", \"def\"] } }";
            Assert.AreEqual(expected, update.ToJson());
        }

        [Test]
        public void TestSetThenUnset()
        {
            var update = Update.Set("x", 1).Unset("name");
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$unset\" : { \"name\" : 1 } }";
            Assert.AreEqual(expected, update.ToJson());
        }
    }
}
