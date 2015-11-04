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

using System;
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;
using NUnit.Framework;

namespace MongoDB.Driver.Tests
{
    [TestFixture]
    public class FilterDefinitionBuilderTests
    {
        [Test]
        public void All()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.All("x", new[] { 10, 20 }), "{x: {$all: [10,20]}}");
        }

        [Test]
        public void All_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.All(x => x.FavoriteColors, new[] { "blue", "green" }), "{colors: {$all: ['blue','green']}}");
            Assert(subject.All("FavoriteColors", new[] { "blue", "green" }), "{colors: {$all: ['blue','green']}}");
        }

        [Test]
        public void And()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.And(
                subject.Eq("a", 1),
                subject.Eq("b", 2));

            Assert(filter, "{a: 1, b: 2}");
        }

        [Test]
        public void And_with_clashing_keys_should_get_promoted_to_dollar_form()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.And(
                subject.Eq("a", 1),
                subject.Eq("a", 2));

            Assert(filter, "{$and: [{a: 1}, {a: 2}]}");
        }

        [Test]
        public void And_with_clashing_keys_but_different_operators_should_get_merged()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.And(
                subject.Gt("a", 1),
                subject.Lt("a", 10));

            Assert(filter, "{a: {$gt: 1, $lt: 10}}");
        }

        [Test]
        public void And_with_an_empty_filter()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.And(
                "{}",
                subject.Eq("a", 10));

            Assert(filter, "{a: 10}");
        }

        [Test]
        public void And_with_a_nested_and_should_get_flattened()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.And(
                subject.And("{a: 1}", new BsonDocument("b", 2)),
                subject.Eq("c", 3));

            Assert(filter, "{a: 1, b: 2, c: 3}");
        }

        [Test]
        public void And_with_a_nested_and_and_clashing_keys()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.And(
                subject.And(subject.Eq("a", 1), subject.Eq("a", 2)),
                subject.Eq("c", 3));

            Assert(filter, "{$and: [{a: 1}, {a: 2}, {c: 3}]}");
        }

        [Test]
        public void And_with_a_nested_and_and_clashing_operators_on_the_same_key()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.Lt("a", 1) & subject.Lt("a", 2);

            Assert(filter, "{$and: [{a: {$lt: 1}}, {a: {$lt: 2}}]}");
        }

        [Test]
        public void And_with_a_nested_and_and_clashing_keys_using_ampersand()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.Eq("a", 1) & "{a: 2}" & new BsonDocument("c", 3);

            Assert(filter, "{$and: [{a: 1}, {a: 2}, {c: 3}]}");
        }

        [Test]
        public void BitsAllClear()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.BitsAllClear("a", 43), "{a: {$bitsAllClear: 43}}");
        }

        [Test]
        public void BitsAllClear_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.BitsAllClear(x => x.Age, 43), "{age: {$bitsAllClear: 43}}");
        }

        [Test]
        public void BitsAllSet()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.BitsAllSet("a", 43), "{a: {$bitsAllSet: 43}}");
        }

        [Test]
        public void BitsAllSet_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.BitsAllSet(x => x.Age, 43), "{age: {$bitsAllSet: 43}}");
        }

        [Test]
        public void BitsAnyClear()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.BitsAnyClear("a", 43), "{a: {$bitsAnyClear: 43}}");
        }

        [Test]
        public void BitsAnyClear_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.BitsAnyClear(x => x.Age, 43), "{age: {$bitsAnyClear: 43}}");
        }

        [Test]
        public void BitsAnySet()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.BitsAnySet("a", 43), "{a: {$bitsAnySet: 43}}");
        }

        [Test]
        public void BitsAnySet_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.BitsAnySet(x => x.Age, 43), "{age: {$bitsAnySet: 43}}");
        }

        [Test]
        public void ElemMatch()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.ElemMatch<BsonDocument>("a", "{b: 1}"), "{a: {$elemMatch: {b: 1}}}");
        }

        [Test]
        public void ElemMatch_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.ElemMatch<Animal>("Pets", "{Name: 'Fluffy'}"), "{pets: {$elemMatch: {Name: 'Fluffy'}}}");
            Assert(subject.ElemMatch(x => x.Pets, "{Name: 'Fluffy'}"), "{pets: {$elemMatch: {Name: 'Fluffy'}}}");
            Assert(subject.ElemMatch(x => x.Pets, x => x.Name == "Fluffy"), "{pets: {$elemMatch: {name: 'Fluffy'}}}");
        }

        [Test]
        public void Empty()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Empty, "{}");
        }

        [Test]
        public void Empty_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Empty, "{}");
        }

        [Test]
        public void Eq()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Eq("x", 10), "{x: 10}");
            Assert(subject.AnyEq("x", 10), "{x: 10}");
        }

        [Test]
        public void Eq_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.Eq(x => x.FirstName, "Jack"), "{fn: 'Jack'}");
            Assert(subject.Eq("FirstName", "Jim"), "{fn: 'Jim'}");
            Assert(subject.Eq("firstName", "Jim"), "{firstName: 'Jim'}");
            Assert(subject.Eq(x => x.FavoriteColors, new[] { "yellow", "green" }), "{colors: ['yellow', 'green']}");
            Assert(subject.Eq("FavoriteColors", new[] { "yellow", "green" }), "{colors: ['yellow', 'green']}");

            Assert(subject.AnyEq(x => x.FavoriteColors, "yellow"), "{colors: 'yellow'}");
            Assert(subject.AnyEq("FavoriteColors", "yellow"), "{colors: 'yellow'}");
        }

        [Test]
        public void Exists()
        {
            var subject = CreateSubject<BsonDocument>();
            Assert(subject.Exists("x"), "{x: {$exists: true}}");
            Assert(subject.Exists("x", false), "{x: {$exists: false}}");
        }

        [Test]
        public void Exists_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.Exists(x => x.FirstName), "{fn: {$exists: true}}");
            Assert(subject.Exists("FirstName", false), "{fn: {$exists: false}}");
        }

        [Test]
        public void Expression()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.Where(x => x.FirstName == "Jack" && x.Age > 10), "{fn: 'Jack', age: {$gt: 10}}");
        }

        [Test]
        public void GeoIntersects()
        {
            var subject = CreateSubject<BsonDocument>();
            var poly = GeoJson.Polygon(
                GeoJson.Geographic(40, 18),
                GeoJson.Geographic(40, 19),
                GeoJson.Geographic(41, 19),
                GeoJson.Geographic(40, 18));

            Assert(subject.GeoIntersects("x", poly), "{x: {$geoIntersects: {$geometry: {type: 'Polygon', coordinates: [[[40.0, 18.0], [40.0, 19.0], [41.0, 19.0], [40.0, 18.0]]]}}}}");
        }

        [Test]
        public void GeoIntersects_Typed()
        {
            var subject = CreateSubject<Person>();
            var poly = GeoJson.Polygon(
                GeoJson.Geographic(40, 18),
                GeoJson.Geographic(40, 19),
                GeoJson.Geographic(41, 19),
                GeoJson.Geographic(40, 18));

            Assert(subject.GeoIntersects(x => x.Location, poly), "{loc: {$geoIntersects: {$geometry: {type: 'Polygon', coordinates: [[[40.0, 18.0], [40.0, 19.0], [41.0, 19.0], [40.0, 18.0]]]}}}}");
            Assert(subject.GeoIntersects("Location", poly), "{loc: {$geoIntersects: {$geometry: {type: 'Polygon', coordinates: [[[40.0, 18.0], [40.0, 19.0], [41.0, 19.0], [40.0, 18.0]]]}}}}");
        }

        [Test]
        public void GeoIntersects_Typed_with_GeoJson()
        {
            var subject = CreateSubject<Person>();
            var poly = GeoJson.Polygon(
                GeoJson.Geographic(40, 18),
                GeoJson.Geographic(40, 19),
                GeoJson.Geographic(41, 19),
                GeoJson.Geographic(40, 18));

            Assert(subject.GeoIntersects(x => x.Location, poly), "{loc: {$geoIntersects: {$geometry: {type: 'Polygon', coordinates: [[[40.0, 18.0], [40.0, 19.0], [41.0, 19.0], [40.0, 18.0]]]}}}}");
            Assert(subject.GeoIntersects("Location", poly), "{loc: {$geoIntersects: {$geometry: {type: 'Polygon', coordinates: [[[40.0, 18.0], [40.0, 19.0], [41.0, 19.0], [40.0, 18.0]]]}}}}");
        }

        [Test]
        public void GeoWithin()
        {
            var subject = CreateSubject<BsonDocument>();
            var poly = GeoJson.Polygon(
                GeoJson.Geographic(40, 18),
                GeoJson.Geographic(40, 19),
                GeoJson.Geographic(41, 19),
                GeoJson.Geographic(40, 18));


            Assert(subject.GeoWithin("x", poly), "{x: {$geoWithin: {$geometry: {type: 'Polygon', coordinates: [[[40.0, 18.0], [40.0, 19.0], [41.0, 19.0], [40.0, 18.0]]]}}}}");
        }

        [Test]
        public void GeoWithin_Typed()
        {
            var subject = CreateSubject<Person>();
            var poly = GeoJson.Polygon(
                GeoJson.Geographic(40, 18),
                GeoJson.Geographic(40, 19),
                GeoJson.Geographic(41, 19),
                GeoJson.Geographic(40, 18));

            Assert(subject.GeoWithin(x => x.Location, poly), "{loc: {$geoWithin: {$geometry: {type: 'Polygon', coordinates: [[[40.0, 18.0], [40.0, 19.0], [41.0, 19.0], [40.0, 18.0]]]}}}}");
            Assert(subject.GeoWithin("Location", poly), "{loc: {$geoWithin: {$geometry: {type: 'Polygon', coordinates: [[[40.0, 18.0], [40.0, 19.0], [41.0, 19.0], [40.0, 18.0]]]}}}}");
        }

        [Test]
        public void GeoWithinBox()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.GeoWithinBox("x", 10, 20, 30, 40), "{x: {$geoWithin: {$box: [[10.0, 20.0], [30.0, 40.0]]}}}");
        }

        [Test]
        public void GeoWithinBox_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.GeoWithinBox(x => x.Location, 10, 20, 30, 40), "{loc: {$geoWithin: {$box: [[10.0, 20.0], [30.0, 40.0]]}}}");
            Assert(subject.GeoWithinBox("Location", 10, 20, 30, 40), "{loc: {$geoWithin: {$box: [[10.0, 20.0], [30.0, 40.0]]}}}");
        }

        [Test]
        public void GeoWithinCenter()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.GeoWithinCenter("x", 10, 20, 30), "{x: {$geoWithin: {$center: [[10.0, 20.0], 30.0]}}}");
        }

        [Test]
        public void GeoWithinCenter_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.GeoWithinCenter(x => x.Location, 10, 20, 30), "{loc: {$geoWithin: {$center: [[10.0, 20.0], 30.0]}}}");
            Assert(subject.GeoWithinCenter("Location", 10, 20, 30), "{loc: {$geoWithin: {$center: [[10.0, 20.0], 30.0]}}}");
        }

        [Test]
        public void GeoWithinCenterSphere()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.GeoWithinCenterSphere("x", 10, 20, 30), "{x: {$geoWithin: {$centerSphere: [[10.0, 20.0], 30.0]}}}");
        }

        [Test]
        public void GeoWithinCenterSphere_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.GeoWithinCenterSphere(x => x.Location, 10, 20, 30), "{loc: {$geoWithin: {$centerSphere: [[10.0, 20.0], 30.0]}}}");
            Assert(subject.GeoWithinCenterSphere("Location", 10, 20, 30), "{loc: {$geoWithin: {$centerSphere: [[10.0, 20.0], 30.0]}}}");
        }

        [Test]
        public void GeoWithinPolygon()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.GeoWithinPolygon("x", new[,] { { 1d, 2d }, { 3d, 4d } }), "{x: {$geoWithin: {$polygon: [[1.0, 2.0], [3.0, 4.0]]}}}");
        }

        [Test]
        public void GeoWithinPolygon_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.GeoWithinPolygon(x => x.Location, new[,] { { 1d, 2d }, { 3d, 4d } }), "{loc: {$geoWithin: {$polygon: [[1.0, 2.0], [3.0, 4.0]]}}}");
            Assert(subject.GeoWithinPolygon("Location", new[,] { { 1d, 2d }, { 3d, 4d } }), "{loc: {$geoWithin: {$polygon: [[1.0, 2.0], [3.0, 4.0]]}}}");
        }

        [Test]
        public void GreaterThan()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Gt("x", 10), "{x: {$gt: 10}}");
            Assert(subject.AnyGt("x", 10), "{x: {$gt: 10}}");
        }

        [Test]
        public void GreaterThan_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.Gt(x => x.Age, 10), "{age: {$gt: 10}}");
            Assert(subject.Gt("Age", 10), "{age: {$gt: 10}}");

            Assert(subject.AnyGt(x => x.FavoriteColors, "green"), "{colors: {$gt: 'green'}}");
            Assert(subject.AnyGt("FavoriteColors", "green"), "{colors: {$gt: 'green'}}");
        }

        [Test]
        public void GreaterThanOrEqual()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Gte("x", 10), "{x: {$gte: 10}}");
            Assert(subject.AnyGte("x", 10), "{x: {$gte: 10}}");
        }

        [Test]
        public void GreaterThanOrEqual_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.Gte(x => x.Age, 10), "{age: {$gte: 10}}");
            Assert(subject.Gte("Age", 10), "{age: {$gte: 10}}");

            Assert(subject.AnyGte(x => x.FavoriteColors, "green"), "{colors: {$gte: 'green'}}");
            Assert(subject.AnyGte("FavoriteColors", "green"), "{colors: {$gte: 'green'}}");
        }

        [Test]
        public void In()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.In("x", new[] { 10, 20 }), "{x: {$in: [10,20]}}");
            Assert(subject.AnyIn("x", new[] { 10, 20 }), "{x: {$in: [10,20]}}");
        }

        [Test]
        public void In_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.In(x => x.Age, new[] { 10, 20 }), "{age: {$in: [10, 20]}}");
            Assert(subject.In("Age", new[] { 10, 20 }), "{age: {$in: [10, 20]}}");

            Assert(subject.AnyIn(x => x.FavoriteColors, new[] { "blue", "green" }), "{colors: {$in: ['blue','green']}}");
            Assert(subject.AnyIn("FavoriteColors", new[] { "blue", "green" }), "{colors: {$in: ['blue','green']}}");
        }

        [Test]
        public void Lt()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Lt("x", 10), "{x: {$lt: 10}}");
            Assert(subject.AnyLt("x", 10), "{x: {$lt: 10}}");
        }

        [Test]
        public void Lt_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.Lt(x => x.Age, 10), "{age: {$lt: 10}}");
            Assert(subject.Lt("Age", 10), "{age: {$lt: 10}}");

            Assert(subject.AnyLt(x => x.FavoriteColors, "green"), "{colors: {$lt: 'green'}}");
            Assert(subject.AnyLt("FavoriteColors", "green"), "{colors: {$lt: 'green'}}");
        }

        [Test]
        public void Lte()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Lte("x", 10), "{x: {$lte: 10}}");
            Assert(subject.AnyLte("x", 10), "{x: {$lte: 10}}");
        }

        [Test]
        public void Lte_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.Lte(x => x.Age, 10), "{age: {$lte: 10}}");
            Assert(subject.Lte("Age", 10), "{age: {$lte: 10}}");

            Assert(subject.AnyLte(x => x.FavoriteColors, "green"), "{colors: {$lte: 'green'}}");
            Assert(subject.AnyLte("FavoriteColors", "green"), "{colors: {$lte: 'green'}}");
        }

        [Test]
        public void Mod()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Mod("x", 10, 4), "{x: {$mod: [NumberLong(10), NumberLong(4)]}}");
        }

        [Test]
        public void Mod_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.Mod(x => x.Age, 10, 4), "{age: {$mod: [NumberLong(10), NumberLong(4)]}}");
            Assert(subject.Mod("Age", 10, 4), "{age: {$mod: [NumberLong(10), NumberLong(4)]}}");

            Assert(subject.Mod(x => x.FavoriteColors, 10, 4), "{colors: {$mod: [NumberLong(10), NumberLong(4)]}}");
            Assert(subject.Mod("FavoriteColors", 10, 4), "{colors: {$mod: [NumberLong(10), NumberLong(4)]}}");
        }

        [Test]
        public void Ne()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Ne("x", 10), "{x: {$ne: 10}}");
            Assert(subject.AnyNe("x", 10), "{x: {$ne: 10}}");
        }

        [Test]
        public void Ne_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.Ne(x => x.Age, 10), "{age: {$ne: 10}}");
            Assert(subject.Ne("Age", 10), "{age: {$ne: 10}}");

            Assert(subject.AnyNe(x => x.FavoriteColors, "green"), "{colors: {$ne: 'green'}}");
            Assert(subject.AnyNe("FavoriteColors", "green"), "{colors: {$ne: 'green'}}");
        }

        [Test]
        public void Near(
            [Values(null, 10d)] double? maxDistance,
            [Values(null, 20d)] double? minDistance)
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.Near("x", 40, 18, maxDistance, minDistance);

            var expectedNear = BsonDocument.Parse("{$near: [40.0, 18.0]}");
            if (maxDistance.HasValue)
            {
                expectedNear.Add("$maxDistance", maxDistance.Value);
            }
            if (minDistance.HasValue)
            {
                expectedNear.Add("$minDistance", minDistance.Value);
            }
            var expected = new BsonDocument("x", expectedNear);
            Assert(filter, expected);
        }

        [Test]
        public void Near_with_GeoJson(
            [Values(null, 10d)] double? maxDistance,
            [Values(null, 20d)] double? minDistance)
        {
            var subject = CreateSubject<BsonDocument>();
            var point = GeoJson.Point(GeoJson.Geographic(40, 18));
            var filter = subject.Near("x", point, maxDistance, minDistance);

            var expectedNearCondition = BsonDocument.Parse("{$geometry: {type: 'Point', coordinates: [40.0, 18.0]}}");
            if (maxDistance.HasValue)
            {
                expectedNearCondition.Add("$maxDistance", maxDistance.Value);
            }
            if (minDistance.HasValue)
            {
                expectedNearCondition.Add("$minDistance", minDistance.Value);
            }
            var expectedNear = new BsonDocument("$near", expectedNearCondition);
            var expected = new BsonDocument("x", expectedNear);
            Assert(filter, expected);
        }

        [Test]
        public void NearSphere(
            [Values(null, 10d)] double? maxDistance,
            [Values(null, 20d)] double? minDistance)
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.NearSphere("x", 40, 18, maxDistance, minDistance);

            var expectedNear = BsonDocument.Parse("{$nearSphere: [40.0, 18.0]}");
            if (maxDistance.HasValue)
            {
                expectedNear.Add("$maxDistance", maxDistance.Value);
            }
            if (minDistance.HasValue)
            {
                expectedNear.Add("$minDistance", minDistance.Value);
            }
            var expected = new BsonDocument("x", expectedNear);
            Assert(filter, expected);
        }

        [Test]
        public void NearSphere_with_GeoJson(
            [Values(null, 10d)] double? maxDistance,
            [Values(null, 20d)] double? minDistance)
        {
            var subject = CreateSubject<BsonDocument>();
            var point = GeoJson.Point(GeoJson.Geographic(40, 18));
            var filter = subject.NearSphere("x", point, maxDistance, minDistance);

            var expectedNearCondition = BsonDocument.Parse("{$geometry: {type: 'Point', coordinates: [40.0, 18.0]}}");
            if (maxDistance.HasValue)
            {
                expectedNearCondition.Add("$maxDistance", maxDistance.Value);
            }
            if (minDistance.HasValue)
            {
                expectedNearCondition.Add("$minDistance", minDistance.Value);
            }
            var expectedNear = new BsonDocument("$nearSphere", expectedNearCondition);
            var expected = new BsonDocument("x", expectedNear);
            Assert(filter, expected);
        }

        [Test]
        public void Nin()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Nin("x", new[] { 10, 20 }), "{x: {$nin: [10,20]}}");
            Assert(subject.AnyNin("x", new[] { 10, 20 }), "{x: {$nin: [10,20]}}");
        }

        [Test]
        public void Nin_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.Nin(x => x.Age, new[] { 10, 20 }), "{age: {$nin: [10, 20]}}");
            Assert(subject.Nin("Age", new[] { 10, 20 }), "{age: {$nin: [10, 20]}}");

            Assert(subject.AnyNin(x => x.FavoriteColors, new[] { "blue", "green" }), "{colors: {$nin: ['blue','green']}}");
            Assert(subject.AnyNin("FavoriteColors", new[] { "blue", "green" }), "{colors: {$nin: ['blue','green']}}");
        }

        [Test]
        public void Not_with_and()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.Not("{$and: [{a: 1}, {b: 2}]}");

            Assert(filter, "{$nor: [{$and: [{a: 1}, {b: 2}]}]}");
        }

        [Test]
        public void Not_with_equal()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.Not("{a: 1}");

            Assert(filter, "{a: {$ne: 1}}");
        }

        [Test]
        public void Not_with_exists()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.Not(subject.Exists("a"));

            Assert(filter, "{a: {$exists: false}}");

            var filter2 = subject.Not(subject.Exists("a", false));

            Assert(filter2, "{a: {$exists: true}}");
        }

        [Test]
        public void Not_with_in()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.Not(subject.In("a", new[] { 10, 20 }));

            Assert(filter, "{a: {$nin: [10, 20]}}");
        }

        [Test]
        public void Not_with_not()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.Not(subject.Not("{a: 1}"));

            Assert(filter, "{a: 1}");
        }

        [Test]
        public void Not_with_not_equal()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.Not("{a: {$ne: 1}}");

            Assert(filter, "{a: 1}");
        }

        [Test]
        public void Not_with_not_in()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.Not(subject.AnyNin("a", new[] { 10, 20 }));

            Assert(filter, "{a: {$in: [10, 20]}}");
        }

        [Test]
        public void Not_with_not_or()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.Not("{$nor: [{a: 1}, {b: 2}]}");

            Assert(filter, "{$or: [{a: 1}, {b: 2}]}");
        }

        [Test]
        public void Not_with_or()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.Not("{$or: [{a: 1}, {b: 2}]}");

            Assert(filter, "{$nor: [{a: 1}, {b: 2}]}");
        }

        [Test]
        public void Not_with_or_using_bang_operator()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = !(subject.Eq("a", 1) | "{b: 2}");

            Assert(filter, "{$nor: [{a: 1}, {b: 2}]}");
        }

        [Test]
        public void OfType_Typed()
        {
            var subject = CreateSubject<Person>();

            // test OfType overloads that apply to the document as a whole
            Assert(subject.OfType<Twin>(), "{ _t : \"Twin\" }");
            Assert(subject.OfType<Twin>(Builders<Twin>.Filter.Eq(p => p.WasBornFirst, true)), "{ _t : \"Twin\", wasBornFirst : true }");
            Assert(subject.OfType<Twin>("{ wasBornFirst : true }"), "{ _t : \"Twin\", wasBornFirst : true }");
            Assert(subject.OfType<Twin>(BsonDocument.Parse("{ wasBornFirst : true }")), "{ _t : \"Twin\", wasBornFirst : true }");
            Assert(subject.OfType<Twin>(p => p.WasBornFirst), "{ _t : \"Twin\", wasBornFirst : true }");

            // test multiple OfType filters against same document
            var personFilter = Builders<Person>.Filter.Or(
                subject.OfType<Twin>(p => p.WasBornFirst),
                subject.OfType<Triplet>(p => p.BirthOrder == 1));
            Assert(personFilter, "{ $or : [{ _t : \"Twin\", wasBornFirst : true }, { _t : \"Triplet\", birthOrder : 1 }] }");

            // test OfType overloads that apply to a field of the document
            Assert(subject.OfType<Animal, Cat>("favoritePet"), "{ \"favoritePet._t\" : \"Cat\" }");
            Assert(subject.OfType<Animal, Cat>("favoritePet", Builders<Cat>.Filter.Eq(c => c.LivesLeft, 9)), "{ \"favoritePet._t\" : \"Cat\", \"favoritePet.livesLeft\" : 9 }");
            Assert(subject.OfType<Animal, Cat>("favoritePet", "{ livesLeft : 9 }"), "{ \"favoritePet._t\" : \"Cat\", \"favoritePet.livesLeft\" : 9 }");
            Assert(subject.OfType<Animal, Cat>("favoritePet", BsonDocument.Parse("{ livesLeft : 9 }")), "{ \"favoritePet._t\" : \"Cat\", \"favoritePet.livesLeft\" : 9 }");
            Assert(subject.OfType<Animal, Cat>(p => p.FavoritePet), "{ \"favoritePet._t\" : \"Cat\" }");
            Assert(subject.OfType<Animal, Cat>(p => p.FavoritePet, c => c.LivesLeft == 9), "{ \"favoritePet._t\" : \"Cat\", \"favoritePet.livesLeft\" : 9 }");

            // test multiple OfType filters against same field
            var animalFilter = Builders<Person>.Filter.Or(
                subject.OfType<Animal, Cat>(p => p.FavoritePet, c => c.LivesLeft == 9),
                subject.OfType<Animal, Dog>(p => p.FavoritePet, d => d.IsLapDog));
            Assert(animalFilter, "{ $or : [{ \"favoritePet._t\" : \"Cat\", \"favoritePet.livesLeft\" : 9 }, { \"favoritePet._t\" : \"Dog\", \"favoritePet.isLapDog\" : true }] }");
        }

        [Test]
        public void Or()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.Or(
                "{a: 1}",
                new BsonDocument("b", 2));

            Assert(filter, "{$or: [{a: 1}, {b: 2}]}");
        }

        [Test]
        public void Or_should_flatten_nested_ors()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.Or(
                "{$or: [{a: 1}, {b: 2}]}",
                new BsonDocument("c", 3));

            Assert(filter, "{$or: [{a: 1}, {b: 2}, {c: 3}]}");
        }

        [Test]
        public void Or_should_flatten_nested_ors_with_a_pipe()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.Eq("a", 1) | "{b: 2}" | new BsonDocument("c", 3);

            Assert(filter, "{$or: [{a: 1}, {b: 2}, {c: 3}]}");
        }

        [Test]
        public void Regex()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Regex("x", "/abc/"), "{x: /abc/}");
        }

        [Test]
        public void Regex_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.Regex(x => x.FirstName, "/abc/"), "{fn: /abc/}");
            Assert(subject.Regex("FirstName", "/abc/"), "{fn: /abc/}");

            Assert(subject.Regex(x => x.FavoriteColors, "/abc/"), "{colors: /abc/}");
            Assert(subject.Regex("FavoriteColors", "/abc/"), "{colors: /abc/}");
        }

        [Test]
        public void Size()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Size("x", 10), "{x: {$size: 10}}");
        }

        [Test]
        public void Size_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.Size(x => x.FavoriteColors, 10), "{colors: {$size: 10}}");
            Assert(subject.Size("FavoriteColors", 10), "{colors: {$size: 10}}");
        }

        [Test]
        public void SizeGt()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.SizeGt("x", 10), "{'x.10': {$exists: true}}");
        }

        [Test]
        public void SizeGt_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.SizeGt(x => x.FavoriteColors, 10), "{'colors.10': {$exists: true}}");
            Assert(subject.SizeGt("FavoriteColors", 10), "{'colors.10': {$exists: true}}");
        }

        [Test]
        public void Text()
        {
            var subject = CreateSubject<BsonDocument>();
            Assert(subject.Text("funny"), "{$text: {$search: 'funny'}}");
            Assert(subject.Text("funny", "en"), "{$text: {$search: 'funny', $language: 'en'}}");
            Assert(subject.Text("funny", new TextSearchOptions { Language = "en" }), "{$text: {$search: 'funny', $language: 'en'}}");
            Assert(subject.Text("funny", new TextSearchOptions { CaseSensitive = true }), "{$text: {$search: 'funny', $caseSensitive: true}}");
            Assert(subject.Text("funny", new TextSearchOptions { DiacriticSensitive = true }), "{$text: {$search: 'funny', $diacriticSensitive: true}}");
        }

        [Test]
        public void Type()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Type("x", BsonType.String), "{x: {$type: 2}}");
        }

        [Test]
        public void Type_string()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Type("x", "string"), "{x: {$type: \"string\"}}");
        }

        [Test]
        public void Type_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.Type(x => x.FirstName, BsonType.String), "{fn: {$type: 2}}");
            Assert(subject.Type("FirstName", BsonType.String), "{fn: {$type: 2}}");
        }

        [Test]
        public void Type_Typed_string()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.Type(x => x.FirstName, "string"), "{fn: {$type: \"string\"}}");
            Assert(subject.Type("FirstName", "string"), "{fn: {$type: \"string\"}}");
        }

        [Test]
        public void Generic_type_constraint_causing_base_class_conversion()
        {
            var filter = TypeConstrainedFilter<Twin>(21);

            Assert(filter, "{ age: 21 }");
        }

        private FilterDefinition<T> TypeConstrainedFilter<T>(int age) where T : Person
        {
            return CreateSubject<T>().Eq(x => x.Age, age);
        }

        private void Assert<TDocument>(FilterDefinition<TDocument> filter, string expected)
        {
            Assert(filter, BsonDocument.Parse(expected));
        }

        private void Assert<TDocument>(FilterDefinition<TDocument> filter, BsonDocument expected)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
            var renderedFilter = filter.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            renderedFilter.Should().Be(expected);
        }

        private FilterDefinitionBuilder<TDocument> CreateSubject<TDocument>()
        {
            return new FilterDefinitionBuilder<TDocument>();
        }

        private class Person
        {
            [BsonElement("fn")]
            public string FirstName { get; set; }

            [BsonElement("colors")]
            public string[] FavoriteColors { get; set; }

            [BsonElement("age")]
            public int Age { get; set; }

            [BsonElement("favoritePet")]
            public Animal FavoritePet { get; set; }

            [BsonElement("pets")]
            public Animal[] Pets { get; set; }

            [BsonElement("loc")]
            public int[] Location { get; set; }
        }

        private class Twin : Person
        {
            [BsonElement("wasBornFirst")]
            public bool WasBornFirst { get; set; }
        }

        private class Triplet : Person
        {
            [BsonElement("birthOrder")]
            public int BirthOrder { get; set; }
        }

        private abstract class Animal
        {
            [BsonElement("name")]
            public string Name { get; set; }
        }

        private abstract class Mammal : Animal
        {
        }

        private class Cat : Mammal
        {
            [BsonElement("livesLeft")]
            public int LivesLeft { get; set; }
        }

        private class Dog : Mammal
        {
            [BsonElement("isLapDog")]
            public bool IsLapDog { get; set; }
        }
    }
}
