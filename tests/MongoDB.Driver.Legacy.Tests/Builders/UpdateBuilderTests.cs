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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Xunit;

namespace MongoDB.Driver.Tests.Builders
{
    public class UpdateBuilderTests
    {
        private class Test
        {
            public int Id = 0;

            [BsonElement("x")]
            public int X = 0;

            [BsonElement("xl")]
            public long XL = 0;

            [BsonElement("xd")]
            public double XD = 0;

            [BsonElement("y")]
            public int[] Y { get; set; }

            [BsonElement("b")]
            public List<B> B { get; set; }

            [BsonElement("dAsDateTime")]
            public DateTime DAsDateTime { get; set; }

            [BsonElement("dAsInt64")]
            [BsonDateTimeOptions(Representation=BsonType.Int64)]
            public DateTime DAsInt64 { get; set; }

            [BsonElement("bdt")]
            public BsonDateTime BsonDateTime { get; set; }

            [BsonElement("bts")]
            public BsonTimestamp BsonTimestamp { get; set; }
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

        private MongoCollection<BsonDocument> _collection;

        private C _a = new C { X = 1 };
        private C _b = new C { X = 2 };
        private BsonDocument _docA1 = new BsonDocument("a", 1);
        private BsonDocument _docA2 = new BsonDocument("a", 2);

        public UpdateBuilderTests()
        {
            _collection = LegacyTestConfiguration.Collection;
        }

        [Fact]
        public void TestAddToSet()
        {
            var update = Update.AddToSet("name", "abc");
            var expected = "{ \"$addToSet\" : { \"name\" : \"abc\" } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestAddToSet_Typed()
        {
            var update = Update<Test>.AddToSet(t => t.Y, 1);
            var expected = "{ \"$addToSet\" : { \"y\" : 1 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestAddToSetEach()
        {
            var update = Update.AddToSetEach("name", "abc", "def");
            var expected = "{ \"$addToSet\" : { \"name\" : { \"$each\" : [\"abc\", \"def\"] } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestAddToSetEach_Typed()
        {
            var update = Update<Test>.AddToSetEach(t => t.Y, new[] { 1, 2 });
            var expected = "{ \"$addToSet\" : { \"y\" : { \"$each\" : [1, 2] } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestAddToSetEachWrapped()
        {
            var update = Update.AddToSetEachWrapped("name", _a, _b, null);
            var expected = "{ \"$addToSet\" : { \"name\" : { \"$each\" : [{ \"X\" : 1 }, { \"X\" : 2 }, null] } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestAddToSetEachWrappedNullName()
        {
            Assert.Throws<ArgumentNullException>(() => Update.AddToSetEachWrapped(null, _a));
        }

        [Fact]
        public void TestAddToSetEachWrappedNullValues()
        {
            Assert.Throws<ArgumentNullException>(() => Update.AddToSetEachWrapped<C>("name", null));
        }

        [Fact]
        public void TestAddToSetWrapped()
        {
            var update = Update.AddToSetWrapped("name", _a);
            var expected = "{ \"$addToSet\" : { \"name\" : { \"X\" : 1 } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestAddToSetWrappedNullName()
        {
            Assert.Throws<ArgumentNullException>(() => Update.AddToSetWrapped(null, _a));
        }

        [Fact]
        public void TestAddToSetWrappedNullValue()
        {
            var update = Update.AddToSetWrapped<C>("name", null);
            var expected = "{ \"$addToSet\" : { \"name\" : null } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestBitwiseAndInt()
        {
            var update = Update.BitwiseAnd("name", 1);
            var expected = "{ '$bit' : { 'name' : { 'and' : 1 } } }".Replace("'", "\"");
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestBitwiseAndInt_Typed()
        {
            var update = Update<Test>.BitwiseAnd(t => t.X, 1);
            var expected = "{ '$bit' : { 'x' : { 'and' : 1 } } }".Replace("'", "\"");
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestBitwiseAndIntTwice()
        {
            var update = Update.BitwiseAnd("x", 1).BitwiseAnd("y", 2);
            var expected = "{ '$bit' : { 'x' : { 'and' : 1 }, 'y' : { 'and' : 2 } } }".Replace("'", "\"");
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestBitwiseAndLong()
        {
            var update = Update.BitwiseAnd("name", 1L);
            var expected = "{ '$bit' : { 'name' : { 'and' : NumberLong(1) } } }".Replace("'", "\"");
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestBitwiseAndLongTwice()
        {
            var update = Update.BitwiseAnd("x", 1L).BitwiseAnd("y", 2L);
            var expected = "{ '$bit' : { 'x' : { 'and' : NumberLong(1) }, 'y' : { 'and' : NumberLong(2) } } }".Replace("'", "\"");
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestBitwiseAndOrInt()
        {
            var update = Update.BitwiseAnd("x", 1L).BitwiseOr("x", 2L);
            var expected = "{ '$bit' : { 'x' : { 'and' : NumberLong(1), 'or' : NumberLong(2) } } }".Replace("'", "\"");
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestBitwiseAndOrInt_Typed()
        {
            var update = Update<Test>.BitwiseAnd(t => t.X, 1).BitwiseOr(t => t.X, 2);
            var expected = "{ '$bit' : { 'x' : { 'and' : 1, 'or' : 2 } } }".Replace("'", "\"");
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestBitwiseOrInt()
        {
            var update = Update.BitwiseOr("name", 1);
            var expected = "{ '$bit' : { 'name' : { 'or' : 1 } } }".Replace("'", "\"");
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestBitwiseOrInt_Typed()
        {
            var update = Update<Test>.BitwiseOr(t => t.X, 1);
            var expected = "{ '$bit' : { 'x' : { 'or' : 1 } } }".Replace("'", "\"");
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestBitwiseOrIntTwice()
        {
            var update = Update.BitwiseOr("x", 1).BitwiseOr("y", 2);
            var expected = "{ '$bit' : { 'x' : { 'or' : 1 }, 'y' : { 'or' : 2 } } }".Replace("'", "\"");
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestBitwiseOrLong()
        {
            var update = Update.BitwiseOr("name", 1L);
            var expected = "{ '$bit' : { 'name' : { 'or' : NumberLong(1) } } }".Replace("'", "\"");
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestBitwiseOrLongTwice()
        {
            var update = Update.BitwiseOr("x", 1L).BitwiseOr("y", 2L);
            var expected = "{ '$bit' : { 'x' : { 'or' : NumberLong(1) }, 'y' : { 'or' : NumberLong(2) } } }".Replace("'", "\"");
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestBitwiseXorInt()
        {
            var update = Update.BitwiseXor("name", 1);
            var expected = "{ '$bit' : { 'name' : { 'xor' : 1 } } }".Replace("'", "\"");
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestBitwiseXorInt_Typed()
        {
            var update = Update<Test>.BitwiseXor(t => t.X, 1);
            var expected = "{ '$bit' : { 'x' : { 'xor' : 1 } } }".Replace("'", "\"");
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestBitwiseXorIntTwice()
        {
            var update = Update.BitwiseXor("x", 1).BitwiseXor("y", 2);
            var expected = "{ '$bit' : { 'x' : { 'xor' : 1 }, 'y' : { 'xor' : 2 } } }".Replace("'", "\"");
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestBitwiseXorLong()
        {
            var update = Update.BitwiseXor("name", 1L);
            var expected = "{ '$bit' : { 'name' : { 'xor' : NumberLong(1) } } }".Replace("'", "\"");
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestBitwiseXorLong_Typed()
        {
            var update = Update<Test>.BitwiseXor(t => t.XL, 1L);
            var expected = "{ '$bit' : { 'xl' : { 'xor' : NumberLong(1) } } }".Replace("'", "\"");
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestBitwiseXorLongTwice()
        {
            var update = Update.BitwiseXor("x", 1L).BitwiseXor("y", 2L);
            var expected = "{ '$bit' : { 'x' : { 'xor' : NumberLong(1) }, 'y' : { 'xor' : NumberLong(2) } } }".Replace("'", "\"");
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestCombineIncSet()
        {
            var update = Update.Combine(
                Update.Inc("x", 1),
                Update.Set("y", 2)
            );
            var expected = "{ '$inc' : { 'x' : 1 }, '$set' : { 'y' : 2 } }".Replace("'", "\"");
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestCombineIncSet_Typed()
        {
            var update = Update<Test>.Combine(
                Update<Test>.Inc(t => t.X, 1),
                Update<Test>.Set(t => t.Y, new[] { 1, 2 }));

            var expected = "{ '$inc' : { 'x' : 1 }, '$set' : { 'y' : [1, 2] } }".Replace("'", "\"");
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestCombineSetSet()
        {
            var update = Update.Combine(
                Update.Set("x", 1),
                Update.Set("y", 2)
            );
            var expected = "{ '$set' : { 'x' : 1, 'y' : 2 } }".Replace("'", "\"");
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestCurrentDate()
        {
            var update = Update.CurrentDate("name");
            var expected = "{ \"$currentDate\" : { \"name\" : true } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestCurrentDate_Typed_AsDateTime()
        {
            var update = Update<Test>.CurrentDate(x => x.DAsDateTime);
            var expected = "{ \"$currentDate\" : { \"dAsDateTime\" : { \"$type\" : \"date\" } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestCurrentDate_Typed_AsInt64()
        {
            Assert.Throws<NotSupportedException>(() => Update<Test>.CurrentDate(x => x.DAsInt64));
        }

        [Fact]
        public void TestCurrentDate_Typed_AsBsonDateTime()
        {
            var update = Update<Test>.CurrentDate(x => x.BsonDateTime);
            var expected = "{ \"$currentDate\" : { \"bdt\" : { \"$type\" : \"date\" } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestCurrentDate_Typed_AsBsonTimestamp()
        {
            var update = Update<Test>.CurrentDate(x => x.BsonTimestamp);
            var expected = "{ \"$currentDate\" : { \"bts\" : { \"$type\" : \"timestamp\" } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestCurrentDateAsDate()
        {
            var update = Update.CurrentDate("name", UpdateCurrentDateType.Date);
            var expected = "{ \"$currentDate\" : { \"name\" : { \"$type\" : \"date\" } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestCurrentDateAsTimestamp()
        {
            var update = Update.CurrentDate("name", UpdateCurrentDateType.Timestamp);
            var expected = "{ \"$currentDate\" : { \"name\" : { \"$type\" : \"timestamp\" } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestIncDouble()
        {
            var update = Update.Inc("name", 1.5);
            var expected = "{ \"$inc\" : { \"name\" : 1.5 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestIncInt()
        {
            var update = Update.Inc("name", 1);
            var expected = "{ \"$inc\" : { \"name\" : 1 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestIncInt_Typed()
        {
            var update = Update<Test>.Inc(t => t.X, 1);
            var expected = "{ \"$inc\" : { \"x\" : 1 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestIncLong()
        {
            var update = Update.Inc("name", 1L);
            var expected = "{ \"$inc\" : { \"name\" : NumberLong(1) } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestMax()
        {
            var update = Update.Max("x", 100);
            var expected = "{ \"$max\" : { \"x\" : 100 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestMax_Twice()
        {
            var update = Update.Max("x", 100).Max("y", 200);
            var expected = "{ \"$max\" : { \"x\" : 100, \"y\" : 200 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestMax_Typed()
        {
            var update = Update<Test>.Max(x => x.X, 100);
            var expected = "{ \"$max\" : { \"x\" : 100 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestMin()
        {
            var update = Update.Min("x", 100);
            var expected = "{ \"$min\" : { \"x\" : 100 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestMin_Twice()
        {
            var update = Update.Min("x", 100).Min("y", 200);
            var expected = "{ \"$min\" : { \"x\" : 100, \"y\" : 200 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestMin_Typed()
        {
            var update = Update<Test>.Min(x => x.X, 100);
            var expected = "{ \"$min\" : { \"x\" : 100 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestMulDouble()
        {
            var update = Update.Mul("name", 1.5);
            var expected = "{ \"$mul\" : { \"name\" : 1.5 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestMulDouble_Typed()
        {
            var update = Update<Test>.Mul(x => x.XD, 1.5);
            var expected = "{ \"$mul\" : { \"xd\" : 1.5 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestMulInt()
        {
            var update = Update.Mul("name", 1);
            var expected = "{ \"$mul\" : { \"name\" : 1 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestMulInt_Typed()
        {
            var update = Update<Test>.Mul(t => t.X, 1);
            var expected = "{ \"$mul\" : { \"x\" : 1 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestMulInt_Twice()
        {
            var update = Update.Mul("name", 1).Mul("name2", 2);
            var expected = "{ \"$mul\" : { \"name\" : 1, \"name2\" : 2 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestMulLong()
        {
            var update = Update.Mul("name", 1L);
            var expected = "{ \"$mul\" : { \"name\" : NumberLong(1) } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestMulLong_Typed()
        {
            var update = Update<Test>.Mul(x => x.XL, 1L);
            var expected = "{ \"$mul\" : { \"xl\" : NumberLong(1) } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPopFirst()
        {
            var update = Update.PopFirst("name");
            var expected = "{ \"$pop\" : { \"name\" : -1 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPopFirst_Typed()
        {
            var update = Update<Test>.PopFirst(t => t.Y);
            var expected = "{ \"$pop\" : { \"y\" : -1 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPopLast()
        {
            var update = Update.PopLast("name");
            var expected = "{ \"$pop\" : { \"name\" : 1 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPopLast_Typed()
        {
            var update = Update<Test>.PopLast(t => t.Y);
            var expected = "{ \"$pop\" : { \"y\" : 1 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPull()
        {
            var update = Update.Pull("name", "abc");
            var expected = "{ \"$pull\" : { \"name\" : \"abc\" } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPull_Typed()
        {
            var update = Update<Test>.Pull(t => t.Y, 3);
            var expected = "{ \"$pull\" : { \"y\" : 3 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPullQuery()
        {
            var update = Update.Pull("name", Query.GT("x", "abc"));
            var expected = "{ \"$pull\" : { \"name\" : { \"x\" : { \"$gt\" : \"abc\" } } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPullQuery_Typed()
        {
            var update = Update<Test>.Pull(t => t.B, eqb => eqb.GT(b => b.C, 3));
            var expected = "{ \"$pull\" : { \"b\" : { \"c\" : { \"$gt\" : 3 } } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPullAll()
        {
            var update = Update.PullAll("name", "abc", "def");
            var expected = "{ \"$pullAll\" : { \"name\" : [\"abc\", \"def\"] } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPullAll_Typed()
        {
            var update = Update<Test>.PullAll(t => t.Y, new[] { 1, 2 });
            var expected = "{ \"$pullAll\" : { \"y\" : [1, 2] } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPullAllWrapped()
        {
            var update = Update.PullAllWrapped("name", _a, _b, null);
            var expected = "{ \"$pullAll\" : { \"name\" : [{ \"X\" : 1 }, { \"X\" : 2 }, null] } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPullAllWrappedNullName()
        {
            Assert.Throws<ArgumentNullException>(() => Update.PullAllWrapped(null, _a));
        }

        [Fact]
        public void TestPullAllWrappedNullValues()
        {
            Assert.Throws<ArgumentNullException>(() => Update.PullAllWrapped<C>("name", null));
        }

        [Fact]
        public void TestPullWrapped()
        {
            var update = Update.PullWrapped("name", _a);
            var expected = "{ \"$pull\" : { \"name\" : { \"X\" : 1 } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPullWrappedNullName()
        {
            Assert.Throws<ArgumentNullException>(() => Update.PullWrapped(null, _a));
        }

        [Fact]
        public void TestPullWrappedNullValue()
        {
            var update = Update.PullWrapped<C>("name", null);
            var expected = "{ \"$pull\" : { \"name\" : null } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPush()
        {
            var update = Update.Push("name", "abc");
            var expected = "{ \"$push\" : { \"name\" : \"abc\" } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPush_Typed()
        {
            var update = Update<Test>.Push(t => t.Y, 7);
            var expected = "{ \"$push\" : { \"y\" : 7 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPushAll()
        {
            var update = Update.PushAll("name", "abc", "def");
            var expected = "{ \"$pushAll\" : { \"name\" : [\"abc\", \"def\"] } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPushAll_Typed()
        {
            var update = Update<Test>.PushAll(t => t.Y, new[] { 23, 32 });
            var expected = "{ \"$pushAll\" : { \"y\" : [23, 32] } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPushAllWrapped()
        {
            var update = Update.PushAllWrapped("name", _a, _b, null);
            var expected = "{ \"$pushAll\" : { \"name\" : [{ \"X\" : 1 }, { \"X\" : 2 }, null] } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPushAllWrappedNullName()
        {
            Assert.Throws<ArgumentNullException>(() => Update.PushAllWrapped(null, _a));
        }

        [Fact]
        public void TestPushAllWrappedNullValue()
        {
            Assert.Throws<ArgumentNullException>(() => Update.PushAllWrapped<C>("name", null));
        }

        [Fact]
        public void TestPushEach()
        {
            var update = Update.PushEach("name", _docA1, _docA2);
            var expected = "{ \"$push\" : { \"name\" : { \"$each\" : [{ \"a\" : 1 }, { \"a\" : 2 }] } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPushEachWithPosition()
        {
            var update = Update.PushEach("name", new PushEachOptions { Position = 10 }, _docA1, _docA2);
            var expected = "{ \"$push\" : { \"name\" : { \"$each\" : [{ \"a\" : 1 }, { \"a\" : 2 }], \"$position\" : 10 } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPushEachWithSlice()
        {
            var update = Update.PushEach("name", new PushEachOptions { Slice = -2 }, _docA1, _docA2);
            var expected = "{ \"$push\" : { \"name\" : { \"$each\" : [{ \"a\" : 1 }, { \"a\" : 2 }], \"$slice\" : -2 } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPushEachWithSort()
        {
            var update = Update.PushEach("name", new PushEachOptions { Sort = SortBy.Ascending("a") }, _docA1, _docA2);
            var expected = "{ \"$push\" : { \"name\" : { \"$each\" : [{ \"a\" : 1 }, { \"a\" : 2 }], \"$sort\" : { \"a\" : 1 } } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPushEachWithPositionAndSortAndSlice()
        {
            var update = Update.PushEach("name", new PushEachOptions { Position = 10, Slice = -3, Sort = SortBy.Descending("a") }, _docA1, _docA2);
            var expected = "{ \"$push\" : { \"name\" : { \"$each\" : [{ \"a\" : 1 }, { \"a\" : 2 }], \"$position\" : 10, \"$slice\" : -3, \"$sort\" : { \"a\" : -1 } } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPushEach_Typed()
        {
            var update = Update<Test>.PushEach(x => x.B, new[] { new B { C = 0 }, new B { C = 1 } });
            var expected = "{ \"$push\" : { \"b\" : { \"$each\" : [{ \"c\" : 0 }, { \"c\" : 1 }] } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPushEachWithPosition_Typed()
        {
            var update = Update<Test>.PushEach(x => x.B, args => args.Position(10), new[] { new B { C = 0 }, new B { C = 1 } });
            var expected = "{ \"$push\" : { \"b\" : { \"$each\" : [{ \"c\" : 0 }, { \"c\" : 1 }], \"$position\" : 10 } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPushEachWithSlice_Typed()
        {
            var update = Update<Test>.PushEach(x => x.B, args => args.Slice(-2), new[] { new B { C = 0 }, new B { C = 1 } });
            var expected = "{ \"$push\" : { \"b\" : { \"$each\" : [{ \"c\" : 0 }, { \"c\" : 1 }], \"$slice\" : -2 } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPushEachWithSort_Typed()
        {
            var update = Update<Test>.PushEach(x => x.B, args => args.SortAscending(x => x.C), new[] { new B { C = 0 }, new B { C = 1 } });
            var expected = "{ \"$push\" : { \"b\" : { \"$each\" : [{ \"c\" : 0 }, { \"c\" : 1 }], \"$sort\" : { \"c\" : 1 } } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPushEachWithPositionAndSortAndSlice_Typed()
        {
            var update = Update<Test>.PushEach(x => x.B, args => args.SortDescending(x => x.C).Slice(-3).Position(10), new[] { new B { C = 0 }, new B { C = 1 } });
            var expected = "{ \"$push\" : { \"b\" : { \"$each\" : [{ \"c\" : 0 }, { \"c\" : 1 }], \"$position\" : 10, \"$slice\" : -3, \"$sort\" : { \"c\" : -1 } } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPushEachWrapped()
        {
            var update = Update.PushEachWrapped("name", _a, _b);
            var expected = "{ \"$push\" : { \"name\" : { \"$each\" : [{ \"X\" : 1 }, { \"X\" : 2 }] } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPushEachWrappedWithSlice()
        {
            var update = Update.PushEachWrapped("name", new PushEachOptions { Slice = -2 }, _a, _b);
            var expected = "{ \"$push\" : { \"name\" : { \"$each\" : [{ \"X\" : 1 }, { \"X\" : 2 }], \"$slice\" : -2 } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPushEachWrappedWithSort()
        {
            var update = Update.PushEachWrapped("name", new PushEachOptions { Sort = SortBy.Ascending("a") }, _a, _b);
            var expected = "{ \"$push\" : { \"name\" : { \"$each\" : [{ \"X\" : 1 }, { \"X\" : 2 }], \"$sort\" : { \"a\" : 1 } } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPushEachWrappedWithSortAndSlice()
        {
            var update = Update.PushEachWrapped("name", new PushEachOptions { Slice = -3, Sort = SortBy.Descending("a") }, _a, _b);
            var expected = "{ \"$push\" : { \"name\" : { \"$each\" : [{ \"X\" : 1 }, { \"X\" : 2 }], \"$slice\" : -3, \"$sort\" : { \"a\" : -1 } } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPushWrapped()
        {
            var update = Update.PushWrapped("name", _a);
            var expected = "{ \"$push\" : { \"name\" : { \"X\" : 1 } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPushWrappedNullName()
        {
            Assert.Throws<ArgumentNullException>(() => Update.PushWrapped(null, _a));
        }

        [Fact]
        public void TestPushWrappedNulLValue()
        {
            var update = Update.PushWrapped<C>("name", null);
            var expected = "{ \"$push\" : { \"name\" : null } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestRename()
        {
            var update = Update.Rename("old", "new");
            var expected = "{ '$rename' : { 'old' : 'new' } }".Replace("'", "\"");
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestReplace()
        {
            var t = new Test { Id = 1, X = 2, Y = null, B = null };
            var update = Update.Replace(t);
            var expected = "{ \"_id\" : 1, \"x\" : 2, \"xl\" : NumberLong(0), \"xd\" : 0.0, \"y\" : null, \"b\" : null, \"dAsDateTime\" : ISODate(\"0001-01-01T00:00:00Z\"), \"dAsInt64\" : NumberLong(0), \"bdt\" : { \"_csharpnull\" : true }, \"bts\" : { \"_csharpnull\" : true } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestReplace_Typed()
        {
            var t = new Test { Id = 1, X = 2, Y = null, B = null };
            var update = Update<Test>.Replace(t);
            var expected = "{ \"_id\" : 1, \"x\" : 2, \"xl\" : NumberLong(0), \"xd\" : 0.0, \"y\" : null, \"b\" : null, \"dAsDateTime\" : ISODate(\"0001-01-01T00:00:00Z\"), \"dAsInt64\" : NumberLong(0), \"bdt\" : { \"_csharpnull\" : true }, \"bts\" : { \"_csharpnull\" : true } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestSet()
        {
            var update = Update.Set("name", "abc");
            var expected = "{ \"$set\" : { \"name\" : \"abc\" } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestSet_Typed()
        {
            var update = Update<Test>.Set(t => t.X, 42);
            var expected = "{ \"$set\" : { \"x\" : 42 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestSetOnInsert()
        {
            var update = Update.SetOnInsert("name", "abc");
            var expected = "{ \"$setOnInsert\" : { \"name\" : \"abc\" } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestSetOnInsert_Typed()
        {
            var update = Update<Test>.SetOnInsert(x => x.X, 42);
            var expected = "{ \"$setOnInsert\" : { \"x\" : 42 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestSetWrapped()
        {
            var update = Update.SetWrapped<C>("name", _a);
            var expected = "{ \"$set\" : { \"name\" : { \"X\" : 1 } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestSetWrappedNullName()
        {
            Assert.Throws<ArgumentNullException>(() => Update.SetWrapped(null, _a));
        }

        [Fact]
        public void TestSetWrappedNullValue()
        {
            var update = Update.SetWrapped<C>("name", null);
            var expected = "{ \"$set\" : { \"name\" : null } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestUnset()
        {
            var update = Update.Unset("name");
            var expected = "{ \"$unset\" : { \"name\" : 1 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestUnset_Typed()
        {
            var update = Update<Test>.Unset(t => t.X);
            var expected = "{ \"$unset\" : { \"x\" : 1 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestAddToSetTwice()
        {
            var update = Update.AddToSet("a", 1).AddToSet("b", 2);
            var expected = "{ \"$addToSet\" : { \"a\" : 1, \"b\" : 2 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestAddToSetEachTwice()
        {
            var update = Update.AddToSetEach("a", 1, 2).AddToSetEach("b", 3, 4);
            var expected = "{ \"$addToSet\" : { \"a\" : { \"$each\" : [1, 2] }, \"b\" : { \"$each\" : [3, 4] } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestIncDoubleTwice()
        {
            var update = Update.Inc("x", 1.5).Inc("y", 2.5);
            var expected = "{ \"$inc\" : { \"x\" : 1.5, \"y\" : 2.5 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestIncIntTwice()
        {
            var update = Update.Inc("x", 1).Inc("y", 2);
            var expected = "{ \"$inc\" : { \"x\" : 1, \"y\" : 2 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestIncLongTwice()
        {
            var update = Update.Inc("x", 1L).Inc("y", 2L);
            var expected = "{ \"$inc\" : { \"x\" : NumberLong(1), \"y\" : NumberLong(2) } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPopFirstTwice()
        {
            var update = Update.PopFirst("a").PopFirst("b");
            var expected = "{ \"$pop\" : { \"a\" : -1, \"b\" : -1 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPopLastTwice()
        {
            var update = Update.PopLast("a").PopLast("b");
            var expected = "{ \"$pop\" : { \"a\" : 1, \"b\" : 1 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPullTwice()
        {
            var update = Update.Pull("a", 1).Pull("b", 2);
            var expected = "{ \"$pull\" : { \"a\" : 1, \"b\" : 2 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPullAllTwice()
        {
            var update = Update.PullAll("a", 1, 2).PullAll("b", 3, 4);
            var expected = "{ \"$pullAll\" : { \"a\" : [1, 2], \"b\" : [3, 4] } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPushTwice()
        {
            var update = Update.Push("a", 1).Push("b", 2);
            var expected = "{ \"$push\" : { \"a\" : 1, \"b\" : 2 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestPushAllTwice()
        {
            var update = Update.PushAll("a", 1, 2).PushAll("b", 3, 4);
            var expected = "{ \"$pushAll\" : { \"a\" : [1, 2], \"b\" : [3, 4] } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestSetTwice()
        {
            var update = Update.Set("a", 1).Set("b", 2);
            var expected = "{ \"$set\" : { \"a\" : 1, \"b\" : 2 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestSetOnInsertTwice()
        {
            var update = Update
                .SetOnInsert("name", "abc")
                .SetOnInsert("two", "cde");
            var expected = "{ \"$setOnInsert\" : { \"name\" : \"abc\", \"two\" : \"cde\" } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestUnsetTwice()
        {
            var update = Update.Unset("a").Unset("b");
            var expected = "{ \"$unset\" : { \"a\" : 1, \"b\" : 1 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestSetThenAddToSet()
        {
            var update = Update.Set("x", 1).AddToSet("name", "abc");
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$addToSet\" : { \"name\" : \"abc\" } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestSetThenAddToSetEach()
        {
            var update = Update.Set("x", 1).AddToSetEach("name", "abc", "def");
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$addToSet\" : { \"name\" : { \"$each\" : [\"abc\", \"def\"] } } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestSetThenIncDouble()
        {
            var update = Update.Set("x", 1).Inc("name", 1.5);
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$inc\" : { \"name\" : 1.5 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestSetThenIncInt()
        {
            var update = Update.Set("x", 1).Inc("name", 1);
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$inc\" : { \"name\" : 1 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestSetThenIncLong()
        {
            var update = Update.Set("x", 1).Inc("name", 1L);
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$inc\" : { \"name\" : NumberLong(1) } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestSetThenPopFirst()
        {
            var update = Update.Set("x", 1).PopFirst("name");
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$pop\" : { \"name\" : -1 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestSetThenPopLast()
        {
            var update = Update.Set("x", 1).PopLast("name");
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$pop\" : { \"name\" : 1 } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestSetThenPull()
        {
            var update = Update.Set("x", 1).Pull("name", "abc");
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$pull\" : { \"name\" : \"abc\" } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestSetThenPullAll()
        {
            var update = Update.Set("x", 1).PullAll("name", "abc", "def");
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$pullAll\" : { \"name\" : [\"abc\", \"def\"] } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestSetThenPush()
        {
            var update = Update.Set("x", 1).Push("name", "abc");
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$push\" : { \"name\" : \"abc\" } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestSetThenPushAll()
        {
            var update = Update.Set("x", 1).PushAll("name", "abc", "def");
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$pushAll\" : { \"name\" : [\"abc\", \"def\"] } }";
            Assert.Equal(expected, update.ToJson());
        }

        [Fact]
        public void TestSetThenUnset()
        {
            var update = Update.Set("x", 1).Unset("name");
            var expected = "{ \"$set\" : { \"x\" : 1 }, \"$unset\" : { \"name\" : 1 } }";
            Assert.Equal(expected, update.ToJson());
        }
 
        [Fact]
        public void TestReplaceWithInvalidFieldName()
        {
            _collection.Drop();
            _collection.Insert(new BsonDocument { { "_id", 1 }, { "x", 1 } });

            var query = Query.EQ("_id", 1);
            var update = Update.Replace(new BsonDocument { { "_id", 1 }, { "$x", 1 } });
            Assert.Throws<BsonSerializationException>(() => { _collection.Update(query, update); });
        }
    }
}
