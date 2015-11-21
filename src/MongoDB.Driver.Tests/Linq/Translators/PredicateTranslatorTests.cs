/* Copyright 2015 MongoDB Inc.
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
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core;
using MongoDB.Driver.Linq.Translators;
using NUnit.Framework;

namespace MongoDB.Driver.Tests.Linq.Translators
{
    [TestFixture]
    public class PredicateTranslatorTests : IntegrationTestBase
    {
        [Test]
        public void All()
        {
            var local = new[] { "itchy" };

            Assert(
                x => local.All(i => x.C.E.I.Contains(i)),
                1,
                "{'C.E.I': { $all: [ 'itchy' ] } }");
        }

        [Test]
        public void All_with_a_not()
        {
            var local = new[] { "itchy" };

            Assert(
                x => !local.All(i => x.C.E.I.Contains(i)),
                1,
                "{'C.E.I': { $not: { $all: [ 'itchy' ] } } }");
        }

        [Test]
        public void Any_without_a_predicate()
        {
            Assert(
                x => x.G.Any(),
                2,
                "{G: {$ne: null, $not: {$size: 0}}}");
        }

        [Test]
        public void Any_with_a_predicate_on_documents()
        {
            Assert(
                x => x.G.Any(g => g.D == "Don't"),
                1,
                "{\"G.D\": \"Don't\"}");

            Assert(
                x => x.G.Any(g => g.D == "Don't" && g.E.F == 33),
                1,
                "{G: {$elemMatch: {D: \"Don't\", 'E.F': 33}}}");
        }

        [Test]
        public void Any_with_a_nested_Any()
        {
            Assert(
                x => x.G.Any(g => g.S.Any()),
                1,
                "{G: {$elemMatch: {S: {$ne: null, $not: {$size: 0}}}}}");

            Assert(
                x => x.G.Any(g => g.S.Any(s => s.D == "Delilah")),
                1,
                "{\"G.S.D\": \"Delilah\"}");
        }

        [Test]
        public void Any_with_a_not()
        {
            Assert(
                x => x.G.Any(g => !g.S.Any()),
                2,
                "{G: {$elemMatch: {$nor: [{S: {$ne: null, $not: {$size: 0}}}]}}}");

            Assert(
                x => x.G.Any(g => !g.S.Any(s => s.D == "Delilah")),
                1,
                "{\"G.S.D\": {$ne: \"Delilah\"}}}");
        }

        [Test]
        public void Any_with_a_predicate_on_scalars_legacy()
        {
            Assert(
                x => x.M.Any(m => m > 5),
                1,
                "{M: {$gt: 5}}");

            Assert(
                x => x.M.Any(m => m > 2 && m < 6),
                2,
                "{M: {$elemMatch: {$gt: 2, $lt: 6}}}");
        }

        [Test]
        [RequiresServer(MinimumVersion = "2.6.0")]
        public void Any_with_a_predicate_on_scalars()
        {
            Assert(
                x => x.C.E.I.Any(i => i.StartsWith("ick")),
                1,
                "{\"C.E.I\": /^ick/s}");

            // this isn't a legal query, as in, there isn't any 
            // way to render this legally for the server...
            //Assert(
            //    x => x.C.E.I.Any(i => i.StartsWith("ick") && i == "Jack"),
            //    1,
            //    new BsonDocument(
            //        "C.E.I",
            //        new BsonDocument(
            //            "$elemMatch",
            //            new BsonDocument
            //            {
            //                { "$regex", new BsonRegularExpression("^ick", "s") },
            //                { "$eq", "Jack" }
            //            })));
        }

        [Test]
        public void Any_with_local_contains_on_an_embedded_document()
        {
            var local = new List<string> { "Delilah", "Dolphin" };

            Assert(
                x => x.G.Any(g => local.Contains(g.D)),
                1,
                "{\"G.D\": { $in: [\"Delilah\", \"Dolphin\" ] } }");
        }

        [Test]
        public void Any_with_local_contains_on_a_scalar_array()
        {
            var local = new List<string> { "itchy" };

            Assert(
                x => local.Any(i => x.C.E.I.Contains(i)),
                1,
                "{\"C.E.I\": { $in: [\"itchy\" ] } }");
        }

        [Test]
        [RequiresServer(MinimumVersion = "3.1.9")]
        public void BitsAllClear_with_bitwise_operators()
        {
            Assert(
                x => (x.C.E.F & 20) == 0,
                1,
                "{'C.E.F': { $bitsAllClear: 20 } }");
        }

        [Test]
        [RequiresServer(MinimumVersion = "3.1.9")]
        public void BitsAllSet_with_bitwise_operators()
        {
            Assert(
                x => (x.C.E.F & 7) == 7,
                1,
                "{'C.E.F': { $bitsAllSet: 7 } }");
        }

        [Test]
        [RequiresServer(MinimumVersion = "3.1.9")]
        public void BitsAllSet_with_HasFlag()
        {
            Assert(
                x => x.Q.HasFlag(Q.One),
                1,
                "{Q: { $bitsAllSet: 1 } }");
        }

        [Test]
        [RequiresServer(MinimumVersion = "3.1.9")]
        public void BitsAnyClear_with_bitwise_operators()
        {
            Assert(
                x => (x.C.E.F & 7) != 7,
                1,
                "{'C.E.F': { $bitsAnyClear: 7 } }");
        }

        [Test]
        [RequiresServer(MinimumVersion = "3.1.9")]
        public void BitsAnySet_with_bitwise_operators()
        {
            Assert(
                x => (x.C.E.F & 20) != 0,
                1,
                "{'C.E.F': { $bitsAnySet: 20 } }");
        }

        [Test]
        public void LocalIListContains()
        {
            IList<int> local = new[] { 10, 20, 30 };

            Assert(
                x => local.Contains(x.Id),
                2,
                "{_id: {$in: [10, 20, 30]}}");
        }

        [Test]
        public void LocalListContains()
        {
            var local = new List<int> { 10, 20, 30 };

            Assert(
                x => local.Contains(x.Id),
                2,
                "{_id: {$in: [10, 20, 30]}}");
        }

        [Test]
        public void LocalArrayContains()
        {
            var local = new[] { 10, 20, 30 };

            Assert(
                x => local.Contains(x.Id),
                2,
                "{_id: {$in: [10, 20, 30]}}");
        }

        [Test]
        public void ArrayLengthEquals()
        {
            Assert(
                x => x.M.Length == 3,
                2,
                "{M: {$size: 3}}");

            Assert(
                x => 3 == x.M.Length,
                2,
                "{M: {$size: 3}}");
        }

        [Test]
        public void ArrayLengthNotEquals()
        {
            Assert(
                x => x.M.Length != 3,
                0,
                "{M: {$not: {$size: 3}}}");
        }

        [Test]
        public void NotArrayLengthEquals()
        {
            Assert(
                x => !(x.M.Length == 3),
                0,
                "{M: {$not: {$size: 3}}}");
        }

        [Test]
        public void NotArrayLengthNotEquals()
        {
            Assert(
                x => !(x.M.Length != 3),
                2,
                "{M: {$size: 3}}");
        }

        [Test]
        public void ArrayPositionEquals()
        {
            Assert(
                x => x.M[1] == 4,
                1,
                "{'M.1': 4}");
        }

        [Test]
        public void ArrayPositionNotEquals()
        {
            Assert(
                x => x.M[1] != 4,
                1,
                "{'M.1': {$ne: 4}}");
        }

        [Test]
        public void ArrayPositionModEqual()
        {
            Assert(
                x => x.M[1] % 2 == 0,
                1,
                "{'M.1': {$mod: [NumberLong(2), NumberLong(0)]}}");
        }

        [Test]
        public void ArrayPositionModNotEqual()
        {
            Assert(
                x => x.M[1] % 3 != 2,
                1,
                "{'M.1': {$not: {$mod: [NumberLong(3), NumberLong(2)]}}}");
        }

        [Test]
        public void Boolean()
        {
            Assert(
                x => x.K,
                1,
                "{K: true}");
        }

        [Test]
        public void BooleanEqualsTrue()
        {
            Assert(
                x => x.K == true,
                1,
                "{K: true}");
        }

        [Test]
        public void BooleanEqualsMethod()
        {
            Assert(
                x => x.K.Equals(true),
                1,
                "{K: true}");
        }

        [Test]
        public void BooleanEqualsFalse()
        {
            Assert(
                x => x.K == false,
                1,
                "{K: false}");
        }

        [Test]
        public void BooleanNotEqualsTrue()
        {
            Assert(
                x => x.K != true,
                1,
                "{K: {$ne: true}}");
        }

        [Test]
        public void BooleanNotEqualsFalse()
        {
            Assert(
                x => x.K != false,
                1,
                "{K: {$ne: false}}");
        }

        [Test]
        public void NotBoolean()
        {
            Assert(
                x => !x.K,
                1,
                "{K: {$ne: true}}");
        }

        [Test]
        public void ClassEquals()
        {
            Assert(
                x => x.C == new C { D = "Dexter" },
                0,
                "{C: {D: 'Dexter', E: null, S: null}}");
        }

        [Test]
        public void ClassEqualsMethod()
        {
            Assert(
                x => x.C.Equals(new C { D = "Dexter" }),
                0,
                "{C: {D: 'Dexter', E: null, S: null}}");
        }

        [Test]
        public void ClassNotEquals()
        {
            Assert(
                x => x.C != new C { D = "Dexter" },
                2,
                "{C: {$ne: {D: 'Dexter', E: null, S: null}}}");
        }

        [Test]
        public void ClassMemberEquals()
        {
            Assert(
                x => x.C.D == "Dexter",
                1,
                "{'C.D': 'Dexter'}");
        }

        [Test]
        public void ClassMemberNotEquals()
        {
            Assert(
                x => x.C.D != "Dexter",
                1,
                "{'C.D': {$ne: 'Dexter'}}");
        }

        [Test]
        public void CompareTo_equal()
        {
            Assert(
                x => x.A.CompareTo("Amazing") == 0,
                1,
                "{'A': 'Amazing' }");
        }

        [Test]
        public void CompareTo_greater_than()
        {
            Assert(
                x => x.A.CompareTo("Around") > 0,
                1,
                "{'A': { $gt: 'Around' } }");
        }

        [Test]
        public void CompareTo_greater_than_or_equal()
        {
            Assert(
                x => x.A.CompareTo("Around") >= 0,
                1,
                "{'A': { $gte: 'Around' } }");
        }

        [Test]
        public void CompareTo_less_than()
        {
            Assert(
                x => x.A.CompareTo("Around") < 0,
                1,
                "{'A': { $lt: 'Around' } }");
        }

        [Test]
        public void CompareTo_less_than_or_equal()
        {
            Assert(
                x => x.A.CompareTo("Around") <= 0,
                1,
                "{'A': { $lte: 'Around' } }");
        }

        [Test]
        public void CompareTo_not_equal()
        {
            Assert(
                x => x.A.CompareTo("Amazing") != 0,
                1,
                "{'A': { $ne: 'Amazing' } }");
        }

        [Test]
        public void DictionaryIndexer()
        {
            Assert(
                x => x.T["one"] == 1,
                1,
                "{'T.one': 1}");
        }

        [Test]
        public void EnumerableCount()
        {
            Assert(
                x => x.G.Count() == 2,
                2,
                "{'G': {$size: 2}}");
        }

        [Test]
        public void EnumerableElementAtEquals()
        {
            Assert(
                x => x.G.ElementAt(1).D == "Dolphin",
                1,
                "{'G.1.D': 'Dolphin'}");
        }

        [Test]
        public void Equals_with_byte_based_enum()
        {
            Assert(
                x => x.Q == Q.One,
                1,
                "{'Q': 1}");
        }

        [Test]
        public void Equals_with_nullable_date_time()
        {
            Assert(
                x => x.R.HasValue && x.R.Value > DateTime.MinValue,
                1,
                "{'R': { $ne: null, $gt: ISODate('0001-01-01T00:00:00Z') } }");
        }

        [Test]
        public void HashSetCount()
        {
            Assert(
                x => x.L.Count == 3,
                2,
                "{'L': {$size: 3}}");
        }

        [Test]
        public void ListCount()
        {
            Assert(
                x => x.O.Count == 3,
                2,
                "{'O': {$size: 3}}");
        }

        [Test]
        public void ListSubEquals()
        {
            Assert(
                x => x.O[2] == 30,
                1,
                "{'O.2': NumberLong(30)}");
        }

        [Test]
        public void RegexInstanceMatch()
        {
            var regex = new Regex("^Awe");
            Assert(
                x => regex.IsMatch(x.A),
                1,
                "{A: /^Awe/}");
        }

        [Test]
        public void RegexStaticMatch()
        {
            Assert(
                x => Regex.IsMatch(x.A, "^Awe"),
                1,
                "{A: /^Awe/}");
        }

        [Test]
        public void RegexStaticMatch_with_options()
        {
            Assert(
                x => Regex.IsMatch(x.A, "^Awe", RegexOptions.IgnoreCase),
                1,
                "{A: /^Awe/i}");
        }

        [Test]
        public void StringContains()
        {
            Assert(
                x => x.A.Contains("some"),
                1,
                "{A: /some/s}");
        }

        [Test]
        public void StringContains_with_dot()
        {
            Assert(
                x => x.A.Contains("."),
                0,
                "{A: /\\./s}");
        }

        [Test]
        public void StringNotContains()
        {
            Assert(
                x => !x.A.Contains("some"),
                1,
                "{A: {$not: /some/s}}");
        }

        [Test]
        public void StringEndsWith()
        {
            Assert(
                x => x.A.EndsWith("some"),
                1,
                "{A: /some$/s}");
        }

        [Test]
        public void StringStartsWith()
        {
            Assert(
                x => x.A.StartsWith("some"),
                0,
                "{A: /^some/s}");
        }

        [Test]
        public void StringEquals()
        {
            Assert(
                x => x.A == "Awesome",
                1,
                "{A: 'Awesome'}");
        }

        [Test]
        public void StringEqualsMethod()
        {
            Assert(
                x => x.A.Equals("Awesome"),
                1,
                "{A: 'Awesome'}");
        }

        [Test]
        public void NotStringEqualsMethod()
        {
            Assert(
                x => !x.A.Equals("Awesome"),
                1,
                "{A: {$ne: 'Awesome'}}");
        }

        [Test]
        public void String_IsNullOrEmpty()
        {
            Assert(
                x => string.IsNullOrEmpty(x.A),
                0,
                "{A: { $in: [null, ''] } }");
        }

        [Test]
        public void Not_String_IsNullOrEmpty()
        {
            Assert(
                x => !string.IsNullOrEmpty(x.A),
                2,
                "{A: { $nin: [null, ''] } }");
        }

        [Test]
        public void Binding_through_an_unnecessary_conversion()
        {
            var root = FindFirstOrDefault(_collection, 10);

            root.Should().NotBeNull();
            root.A.Should().Be("Awesome");
        }

        [Test]
        public void Binding_through_an_unnecessary_conversion_with_a_builder()
        {
            var root = FindFirstOrDefaultWithBuilder(_collection, 10);

            root.Should().NotBeNull();
            root.A.Should().Be("Awesome");
        }

        private T FindFirstOrDefault<T>(IMongoCollection<T> collection, int id) where T : IRoot
        {
            return collection.FindSync(x => x.Id == id).FirstOrDefault();
        }

        private T FindFirstOrDefaultWithBuilder<T>(IMongoCollection<T> collection, int id) where T : IRoot
        {
            return collection.FindSync(Builders<T>.Filter.Eq(x => x.Id, id)).FirstOrDefault();
        }

        public void Assert(Expression<Func<Root, bool>> filter, int expectedCount, string expectedFilter)
        {
            Assert(filter, expectedCount, BsonDocument.Parse(expectedFilter));
        }

        public void Assert(Expression<Func<Root, bool>> filter, int expectedCount, BsonDocument expectedFilter)
        {
            var serializer = BsonSerializer.SerializerRegistry.GetSerializer<Root>();
            var filterDocument = PredicateTranslator.Translate(filter, serializer, BsonSerializer.SerializerRegistry);

            var list = _collection.FindSync(filterDocument).ToList();

            filterDocument.Should().Be(expectedFilter);
            list.Count.Should().Be(expectedCount);
        }
    }
}
