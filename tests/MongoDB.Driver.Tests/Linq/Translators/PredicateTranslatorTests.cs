﻿/* Copyright 2015-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Translators;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Translators
{
    public class PredicateTranslatorTests : IntegrationTestBase
    {
        [Fact]
        public void All()
        {
            var local = new[] { "itchy" };

            Assert(
                x => local.All(i => x.C.E.I.Contains(i)),
                1,
                "{'C.E.I': { $all: [ 'itchy' ] } }");
        }

        [Fact]
        public void All_with_a_not()
        {
            var local = new[] { "itchy" };

            Assert(
                x => !local.All(i => x.C.E.I.Contains(i)),
                1,
                "{'C.E.I': { $not: { $all: [ 'itchy' ] } } }");
        }

        [Fact]
        public void Any_without_a_predicate()
        {
            Assert(
                x => x.G.Any(),
                2,
                "{G: {$ne: null, $not: {$size: 0}}}");
        }

        [Fact]
        public void Any_without_a_predicate_equals_true()
        {
            Assert(
                x => x.G.Any() == true,
                2,
                "{G: {$ne: null, $not: {$size: 0}}}");
        }

        [Fact]
        public void Any_without_a_predicate_not_equals_true()
        {
            Assert(
                x => x.G.Any() != true,
                0,
                "{$nor: [{G: {$ne: null, $not: {$size: 0}}}]}");
        }

        [Fact]
        public void Any_without_a_predicate_equals_false()
        {
            Assert(
                x => x.G.Any() == false,
                0,
                "{$nor: [{G: {$ne: null, $not: {$size: 0}}}]}");
        }

        [Fact]
        public void Any_not_without_a_predicate()
        {
            Assert(
                x => !x.G.Any(),
                0,
                "{$nor: [{G: {$ne: null, $not: {$size: 0}}}]}");
        }

        [Fact]
        public void Any_without_a_predicate_not_equals_false()
        {
            Assert(
                x => x.G.Any() != false,
                2,
                "{G: {$ne: null, $not: {$size: 0}}}");
        }

        [Fact]
        public void Any_with_a_predicate_on_documents()
        {
            Assert(
                x => x.G.Any(g => g.D == "Don't"),
                1,
                "{ G : { $elemMatch : { D : \"Don't\" } } }");

            Assert(
                x => x.G.Any(g => g.D == "Don't" && g.E.F == 33),
                1,
                "{ G : { $elemMatch : { D : \"Don't\", 'E.F' : 33 } } }");
        }

        [Fact]
        public void Any_with_a_predicate_on_document_itself()
        {
            Assert(
                x => x.G.Any(g => g != null),
                2,
                "{ 'G' : { '$elemMatch' : { '$ne' : null } } }");

            Assert(
                x => x.G.Any(g => null != g),
                2,
                "{ 'G' : { '$elemMatch' : { '$ne' : null } } }");

            Assert(
                x => x.G.Any(g => g.E.I.Any(i => i == "insecure")),
                1,
                "{ \"G.E.I\" : { '$elemMatch' : { '$eq': 'insecure' } } }");
        }

        [Fact]
        public void Any_with_a_predicate_on_document_itself_and_objectId()
        {
            Assert(
                x => x.G.Any(g => g.Ids.Any(i => i == ObjectId.Parse("111111111111111111111111"))),
                1,
                "{ 'G.Ids' : { '$elemMatch' : { '$eq' : ObjectId('111111111111111111111111') } } }");
        }

        [Fact]
        public void Any_with_a_predicate_on_documents_itself_and_ClassEquals()
        {
            var c1 = new C()
            {
                D = "Dolphin",
                E = new E()
                {
                    F = 55,
                    H = 66,
                    I = new List<string>()
                    {
                        "insecure"
                    }
                }
            };
            Assert(
                x => x.G.Any(g => g == c1),
                1,
                "{ \"G\" : { \"$elemMatch\" : { \"Ids\" : null, \"D\" : \"Dolphin\", \"E\" : { \"F\" : 55, \"H\" : 66, \"I\" : [\"insecure\"], \"C\" : null }, \"S\" : null, \"X\" : null, \"Y\" : null, \"Z\" : null } } }");
        }

        [Fact]
        public void Any_with_a_gte_predicate_on_documents()
        {
            Assert(
                x => x.G.Any(g => g.E.F >= 100),
                1,
                "{ \"G\" : { \"$elemMatch\" : { \"E.F\" : { \"$gte\" : 100 } } } }");
        }

        [Fact]
        public void Any_with_a_ne_and_Equal_predicate_on_documents()
        {
            Assert(
                x => x.G.Any(g => !g.D.Equals("Don't")),
                2,
                "{ \"G\" : { \"$elemMatch\" : { \"D\" : { \"$ne\" : \"Don't\" } } } }");
        }

        [Fact]
        public void Any_with_a_ne_predicate_on_documents()
        {
            Assert(
                x => x.G.Any(g => g.S != null),
                1,
                "{ \"G\" : { \"$elemMatch\" : { \"S\" : { \"$ne\" : null } } } }");
            Assert(
                x => x.G.Any(g => !(g.S == null)),
                1,
                "{ \"G\" : { \"$elemMatch\" : { \"S\" : { \"$ne\" : null } } } }");
        }

        [Fact]
        public void Any_with_a_multi_not_brackets_predicate_on_documents()
        {
            Assert(
                x => x.G.Any(g => !(!(g.D == "Don't"))),
                1,
                "{ \"G\" : { \"$elemMatch\" : { \"D\" : \"Don't\" } } }");

            Assert(
                x => x.G.Any(g => !(!(!(!(g.D == "Don't"))))),
                1,
                "{ \"G\" : { \"$elemMatch\" : { \"D\" : \"Don't\" } } }");

            Assert(
                x => x.G.Any(g => !(g.S == null)),
                1,
                "{ \"G\" : { \"$elemMatch\" : { \"S\" : { \"$ne\" : null } } } }");

            Assert(
                x => x.G.Any(g => !(!(!(g.S == null)))),
                1,
                "{ \"G\" : { \"$elemMatch\" : { \"S\" : { \"$ne\" : null } } } }");
        }

        [Fact]
        public void Any_with_a_multi_conditions_predicate_on_documents()
        {
            Assert(
                x => x.G.Any(g => g.D != "Don't" && g.E.F == 333),
                1,
                "{ \"G\" : { \"$elemMatch\" : { \"D\" : { \"$ne\" : \"Don't\" }, \"E.F\" : 333 } } }");

            Assert(
                x => x.G.Any(g => g.D == "Don't" || g.E.F != 32),
                2,
                "{ \"G\" : { \"$elemMatch\" : { \"$or\" : [{ \"D\" : \"Don't\" }, { \"E.F\" : { \"$ne\" : 32 } }] } } }");
        }

        [Fact]
        public void Any_with_advanced_nested_Anys()
        {
            Assert(
                i => i.G.Any(g => g.Y.S.Any(s => s.Z.Any(z => z.C.E.C.X.Any()))),
                1,
                "{ \"G.Y.S.Z\" : { $elemMatch : { \"C.E.C.X\" : { $ne : null, $not : { $size : 0 } } } } }");

            Assert(
                i => i.G.Any(g => g.Y.S.Any(s => s.Z.Any(z => z.C.X.Any(x => x.F == 4)))),
                1,
                "{ \"G.Y.S.Z.C.X\" : { $elemMatch : { \"F\" : 4 } } }");

            Assert(
                i => i.G.Any(g => g.D == "Don't" && g.S.Any(s => s.Z.Any(x => x.H == 0))),
                1,
                "{ G : { $elemMatch : { \"D\" : \"Don't\", \"S.Z\" : { $elemMatch : { \"H\" : 0 } } } } }");

            Assert(
                i => i.G.Any(g => g.D == "Don't" && g.Y.S.Any(s => s.Z.Any(x => x.H == 0))),
                1,
                "{ G : { $elemMatch : { \"D\" : \"Don't\", \"Y.S.Z\" : { $elemMatch : { \"H\" : 0 } } } } }");

            Assert(
                i => i.G.Any(g => g.D == "Don't" && g.S.Any(s => s.E == null && s.Z.Any(x => x.H == 0))),
                1,
                "{ G : { $elemMatch : { \"D\" : \"Don't\", \"S\" : { $elemMatch : { \"E\" : null, \"Z\" : { $elemMatch : { \"H\" : 0 } } } } } } }");

            Assert(
                i => i.G.Any(g => g.D == "Don't" && g.Y.S.Any(s => s.E == null && s.Z.Any(x => x.H == 0))),
                1,
                "{ G : { $elemMatch : { \"D\" : \"Don't\", \"Y.S\" : { $elemMatch : { \"E\" : null, \"Z\" : { $elemMatch : { \"H\" : 0 } } } } } } }");

            Assert(
                i => i.G.Any(g => g.D == "Don't" && g.Y.S.Any(s => s.E == null && s.Z.Any(z => z.C.X.Any(x => x.F == 4)))),
                1,
                "{ G:  { $elemMatch : { \"D\" : \"Don't\", \"Y.S\" : { $elemMatch : { \"E\" : null, \"Z.C.X\" : { $elemMatch : { \"F\" : 4 } } } } } } }");

            Assert(
                i => i.G.Any(g => g.D == "Don't" && g.Y.S.Any(s => s.E == null && s.Z.Any(z => z.C.X.Any(x => x.F == 4 && x.H == 0)))),
                1,
                "{ G : { $elemMatch : { \"D\" : \"Don't\", \"Y.S\" : { $elemMatch : { \"E\" : null, \"Z.C.X\" : { $elemMatch : { \"F\" : 4, \"H\" : 0 } } } } } } }");

            Assert(
                i => i.G.Any(g => g.D == "Don't" && g.Y.S.Any(s => s.E == null && s.Z.Any(z => z.F == 1 && z.C.X.Any(x => x.F == 4 && x.H == 0)))),
                1,
                "{ G : { $elemMatch : { \"D\" : \"Don't\", \"Y.S\" : { $elemMatch : { \"E\" : null, \"Z\" : { $elemMatch : { \"F\" : 1, \"C.X\" : { $elemMatch : { \"F\" : 4, \"H\" : 0 } } } } } } } } }");

            Assert(
                i => i.G.Any(
                    g => g.D == "Don't" &&
                         g.Y.S.Any(s => s.Z.Any(z => z.C.X.Any(x => x.F == 4))) &&
                         g.S.Any(s => s.D == "Delilah" && s.Z.Any(z => z.F == 1 && z.H == 0))),
                1,
                @"{ G : { $elemMatch : {
                    ""D"" : ""Don't"",
                    ""Y.S.Z.C.X"" : { $elemMatch : { ""F"" : 4 } },
                    ""S"" : { $elemMatch : { ""D"" : ""Delilah"", ""Z"" : { $elemMatch : { ""F"" : 1, ""H"" : 0 } } } }
                } } }");

            Assert(
                i => i.G.Any(
                    g => g.D == "Don't" &&
                         g.Y.S.Any(s => s.E == null && s.Z.Any(z => z.F == 1 && z.C.X.Any(x => x.F == 4 && x.H == 0))) &&
                         g.S.Any(s => s.D == "Delilah" && s.Z.Any(z => z.F == 1 && z.H == 0))),
                1,
                @"{ G : { $elemMatch : {
                    ""D"" : ""Don't"",
                    ""Y.S"" : { $elemMatch : { ""E"" : null, ""Z"" : { $elemMatch : { ""F"" : 1, ""C.X"" : { $elemMatch : { ""F"" : 4, ""H"" : 0 } } } } } },
                    ""S"" : { $elemMatch : { ""D"" : ""Delilah"", ""Z"" : { $elemMatch : { ""F"" : 1, ""H"" : 0 } } } }
                } } }");
        }

        [Fact]
        public void Any_with_a_nested_Any()
        {
            Assert(
                x => x.G.Any(g => g.S.Any()),
                1,
                "{ G : { $elemMatch : { S : { $ne : null, $not : { $size : 0 } } } } }");

            Assert(
                x => x.G.Any(g => g.S.Any(s => s.D == "Delilah")),
                1,
                "{ \"G.S\" : { $elemMatch : { \"D\" : \"Delilah\" } } }");

            Assert(
                x => x.G.Any(g => g.D == "Don't" && g.S.Any(s => s.D == "Delilah")),
                1,
                "{ \"G\" : { \"$elemMatch\" : { \"D\" : \"Don't\", \"S\" : { \"$elemMatch\" : { \"D\" : \"Delilah\" } } } } }");

            Assert(
                x => x.G.Any(g => g.D == "Don't" && g.S.Any(s => s.E == null && s.D == "Delilah")),
                1,
                "{ G : { $elemMatch : { D : \"Don't\", \"S\" : { $elemMatch : { E : null, D : \"Delilah\" } } } } }");
        }

        [Fact]
        public void Any_with_a_not_and_a_predicate_with_not_contains()
        {
            var x = new[] { 1, 2 };

            AssertUsingCustomCollection(
                c => !c.M.Any(a => !x.Contains(a)),
                "{ M : { '$not' : { '$elemMatch' : { '$not' : { '$in' : [1, 2] } } } } }");
        }

        [Fact]
        public void Any_with_a_not_and_a_predicate_with_contains()
        {
            var x = new[] { 1, 2 };

            AssertUsingCustomCollection(
                c => !c.M.Any(a => x.Contains(a)),
                "{ M : { '$not' : { '$elemMatch' : { '$in' : [1, 2] } } } }");
        }

        [Fact]
        public void Any_with_a_predicate_with_contains()
        {
            var x = new[] { 1, 2 };

            AssertUsingCustomCollection(
                c => c.M.Any(a => x.Contains(a)),
                "{ M : { '$elemMatch' : { '$in' : [1, 2] } } }"
                );
        }

        [Fact]
        public void Any_with_a_predicate_with_not_contains()
        {
            var x = new[] { 1, 2 };

            AssertUsingCustomCollection(
                c => c.M.Any(a => !x.Contains(a)),
                "{ M : { '$elemMatch' : { '$not' : { '$in' : [1, 2] } } } }"
            );
        }

        [Fact]
        public void Any_with_a_not()
        {
            Assert(
                x => x.G.Any(g => !g.S.Any()),
                2,
                "{ G : { $elemMatch : { $nor : [{ S : { $ne : null, $not : { $size : 0 } } }] } } }");

            Assert(
                x => x.G.Any(g => !g.S.Any(s => s.D == "Delilah")),
                1,
                "{\"G.S\" : { $not : { $elemMatch : { \"D\" : \"Delilah\" } } } }");
        }

        [Fact]
        public void Any_with_a_predicate_on_scalars_legacy()
        {
            Assert(
                x => x.M.Any(m => m > 5),
                1,
                "{ M : { $elemMatch : { $gt : 5 } } }");

            Assert(
                x => x.M.Any(m => m > 2 && m < 6),
                2,
                "{ M : { $elemMatch : { $gt : 2, $lt : 6 } } }");
        }

        [SkippableFact]
        public void Any_with_a_predicate_on_scalars()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("2.6.0");

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

        [Fact]
        public void Any_with_a_type_is()
        {
            Assert(
                x => x.C.X.Any(y => y is V),
                1,
                "{\"C.X\": {\"$elemMatch\": {\"_t\": \"V\" } } }");
        }

        [Fact]
        public void Any_with_local_contains_on_an_embedded_document()
        {
            var local = new List<string> { "Delilah", "Dolphin" };

            Assert(
                x => x.G.Any(g => local.Contains(g.D)),
                1,
                "{ 'G' : { '$elemMatch' : { 'D' : { $in : ['Delilah', 'Dolphin'] } } } }");
        }

        [Fact]
        public void Any_with_local_contains_on_a_scalar_array()
        {
            var local = new List<string> { "itchy" };

            Assert(
                x => local.Any(i => x.C.E.I.Contains(i)),
                1,
                "{\"C.E.I\": { $in: [\"itchy\" ] } }");
        }

        [Fact]
        public void AsQueryable()
        {
            Expression<Func<C, bool>> filter = x => x.D == "Don't";

            Assert(
                x => x.G.AsQueryable().Any(filter),
                1,
                "{ 'G' : { '$elemMatch' : { 'D' : \"Don't\" } } }");
        }

        [SkippableFact]
        public void BitsAllClear_with_bitwise_operators()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");

            Assert(
                x => (x.C.E.F & 20) == 0,
                1,
                "{'C.E.F': { $bitsAllClear: 20 } }");
        }

        [SkippableFact]
        public void BitsAllSet_with_bitwise_operators()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");

            Assert(
                x => (x.C.E.F & 7) == 7,
                1,
                "{'C.E.F': { $bitsAllSet: 7 } }");
        }

        [SkippableFact]
        public void BitsAllSet_with_HasFlag()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");

            Assert(
                x => x.Q.HasFlag(Q.One),
                1,
                "{Q: { $bitsAllSet: 1 } }");
        }

        [SkippableFact]
        public void BitsAnyClear_with_bitwise_operators()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");

            Assert(
                x => (x.C.E.F & 7) != 7,
                1,
                "{'C.E.F': { $bitsAnyClear: 7 } }");
        }

        [SkippableFact]
        public void BitsAnySet_with_bitwise_operators()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");

            Assert(
                x => (x.C.E.F & 20) != 0,
                1,
                "{'C.E.F': { $bitsAnySet: 20 } }");
        }

        [Fact]
        public void LocalIListContains()
        {
            IList<int> local = new[] { 10, 20, 30 };

            Assert(
                x => local.Contains(x.Id),
                2,
                "{_id: {$in: [10, 20, 30]}}");
        }

        [Fact]
        public void LocalListContains()
        {
            var local = new List<int> { 10, 20, 30 };

            Assert(
                x => local.Contains(x.Id),
                2,
                "{_id: {$in: [10, 20, 30]}}");
        }

        [Fact]
        public void LocalArrayContains()
        {
            var local = new[] { 10, 20, 30 };

            Assert(
                x => local.Contains(x.Id),
                2,
                "{_id: {$in: [10, 20, 30]}}");
        }

        [Fact]
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

        [Fact]
        public void ArrayLengthNotEquals()
        {
            Assert(
                x => x.M.Length != 3,
                0,
                "{M: {$not: {$size: 3}}}");
        }

        [Fact]
        public void NotArrayLengthEquals()
        {
            Assert(
                x => !(x.M.Length == 3),
                0,
                "{M: {$not: {$size: 3}}}");
        }

        [Fact]
        public void NotArrayLengthNotEquals()
        {
            Assert(
                x => !(x.M.Length != 3),
                2,
                "{M: {$size: 3}}");
        }

        [Fact]
        public void ArrayPositionEquals()
        {
            Assert(
                x => x.M[1] == 4,
                1,
                "{'M.1': 4}");
        }

        [Fact]
        public void ArrayPositionNotEquals()
        {
            Assert(
                x => x.M[1] != 4,
                1,
                "{'M.1': {$ne: 4}}");
        }

        [Fact]
        public void ArrayPositionModEqual()
        {
            Assert(
                x => x.M[1] % 2 == 0,
                1,
                "{'M.1': {$mod: [NumberLong(2), NumberLong(0)]}}");
        }

        [Fact]
        public void ArrayPositionModNotEqual()
        {
            Assert(
                x => x.M[1] % 3 != 2,
                1,
                "{'M.1': {$not: {$mod: [NumberLong(3), NumberLong(2)]}}}");
        }

        [Fact]
        public void Boolean()
        {
            Assert(
                x => x.K,
                1,
                "{K: true}");
        }

        [Fact]
        public void BooleanEqualsTrue()
        {
            Assert(
                x => x.K == true,
                1,
                "{K: true}");
        }

        [Fact]
        public void BooleanEqualsMethod()
        {
            Assert(
                x => x.K.Equals(true),
                1,
                "{K: true}");
        }

        [Fact]
        public void BooleanEqualsFalse()
        {
            Assert(
                x => x.K == false,
                1,
                "{K: false}");
        }

        [Fact]
        public void BooleanNotEqualsTrue()
        {
            Assert(
                x => x.K != true,
                1,
                "{K: {$ne: true}}");
        }

        [Fact]
        public void BooleanNotEqualsFalse()
        {
            Assert(
                x => x.K != false,
                1,
                "{K: {$ne: false}}");
        }

        [Fact]
        public void NotBoolean()
        {
            Assert(
                x => !x.K,
                1,
                "{K: {$ne: true}}");
        }

        [Fact]
        public void ClassEquals()
        {
            Assert(
                x => x.C == new C { D = "Dexter" },
                0,
                "{ C : { Ids : null, D : 'Dexter', E : null, S : null, X : null, Y : null, Z : null } }");
        }

        [Fact]
        public void ClassEqualsMethod()
        {
            Assert(
                x => x.C.Equals(new C { D = "Dexter" }),
                0,
                "{ C : { Ids : null, D : 'Dexter', E : null, S : null, X : null, Y : null, Z : null } }");
        }

        [Fact]
        public void ClassNotEquals()
        {
            Assert(
                x => x.C != new C { D = "Dexter" },
                2,
                "{ C : { $ne : { Ids : null, D : 'Dexter', E : null, S : null, X : null, Y : null, Z : null } } }");
        }

        [Fact]
        public void ClassMemberEquals()
        {
            Assert(
                x => x.C.D == "Dexter",
                1,
                "{'C.D': 'Dexter'}");
        }

        [Fact]
        public void ClassMemberNotEquals()
        {
            Assert(
                x => x.C.D != "Dexter",
                1,
                "{'C.D': {$ne: 'Dexter'}}");
        }

        [Fact]
        public void CompareTo_equal()
        {
            Assert(
                x => x.A.CompareTo("Amazing") == 0,
                1,
                "{'A': 'Amazing' }");
        }

        [Fact]
        public void CompareTo_greater_than()
        {
            Assert(
                x => x.A.CompareTo("Around") > 0,
                1,
                "{'A': { $gt: 'Around' } }");
        }

        [Fact]
        public void CompareTo_greater_than_or_equal()
        {
            Assert(
                x => x.A.CompareTo("Around") >= 0,
                1,
                "{'A': { $gte: 'Around' } }");
        }

        [Fact]
        public void CompareTo_less_than()
        {
            Assert(
                x => x.A.CompareTo("Around") < 0,
                1,
                "{'A': { $lt: 'Around' } }");
        }

        [Fact]
        public void CompareTo_less_than_or_equal()
        {
            Assert(
                x => x.A.CompareTo("Around") <= 0,
                1,
                "{'A': { $lte: 'Around' } }");
        }

        [Fact]
        public void CompareTo_not_equal()
        {
            Assert(
                x => x.A.CompareTo("Amazing") != 0,
                1,
                "{'A': { $ne: 'Amazing' } }");
        }

        [Fact]
        public void DictionaryIndexer()
        {
            Assert(
                x => x.T["one"] == 1,
                1,
                "{'T.one': 1}");
        }

        [Fact]
        public void EnumerableCount()
        {
            Assert(
                x => x.G.Count() == 2,
                2,
                "{'G': {$size: 2}}");
        }

        [Fact]
        public void EnumerableElementAtEquals()
        {
            Assert(
                x => x.G.ElementAt(1).D == "Dolphin",
                1,
                "{'G.1.D': 'Dolphin'}");
        }

        [Fact]
        public void Equals_with_byte_based_enum()
        {
            Assert(
                x => x.Q == Q.One,
                1,
                "{'Q': 1}");
        }

        [Fact]
        public void Equals_with_nullable_date_time()
        {
            Assert(
                x => x.R.HasValue && x.R.Value > DateTime.MinValue,
                1,
                "{'R': { $ne: null, $gt: ISODate('0001-01-01T00:00:00Z') } }");
        }

        [Fact]
        public void Equals_with_non_nullable_field_and_nullable_value()
        {
            var value = (int?)null;
            Assert(
                x => x.Id == value,
                0,
                "{ _id : null }");
        }

        [Fact]
        public void HashSetCount()
        {
            Assert(
                x => x.L.Count == 3,
                2,
                "{'L': {$size: 3}}");
        }

        [Fact]
        public void ListCount()
        {
            Assert(
                x => x.O.Count == 3,
                2,
                "{'O': {$size: 3}}");
        }

        [Fact]
        public void ListSubEquals()
        {
            Assert(
                x => x.O[2] == 30,
                1,
                "{'O.2': NumberLong(30)}");
        }

        [Fact]
        public void RegexInstanceMatch()
        {
            var regex = new Regex("^Awe");
            Assert(
                x => regex.IsMatch(x.A),
                1,
                "{A: /^Awe/}");
        }

        [Fact]
        public void RegexStaticMatch()
        {
            Assert(
                x => Regex.IsMatch(x.A, "^Awe"),
                1,
                "{A: /^Awe/}");
        }

        [Fact]
        public void RegexStaticMatch_with_options()
        {
            Assert(
                x => Regex.IsMatch(x.A, "^Awe", RegexOptions.IgnoreCase),
                1,
                "{A: /^Awe/i}");
        }

        [Fact]
        public void StringContains()
        {
            Assert(
                x => x.A.Contains("some"),
                1,
                "{A: /some/s}");
        }

        [Fact]
        public void StringContains_with_dot()
        {
            Assert(
                x => x.A.Contains("."),
                0,
                "{A: /\\./s}");
        }

        [Fact]
        public void StringNotContains()
        {
            Assert(
                x => !x.A.Contains("some"),
                1,
                "{A: {$not: /some/s}}");
        }

        [Fact]
        public void StringEndsWith()
        {
            Assert(
                x => x.A.EndsWith("some"),
                1,
                "{A: /some$/s}");
        }

        [Fact]
        public void StringStartsWith()
        {
            Assert(
                x => x.A.StartsWith("some"),
                0,
                "{A: /^some/s}");
        }

        [Fact]
        public void StringEquals()
        {
            Assert(
                x => x.A == "Awesome",
                1,
                "{A: 'Awesome'}");
        }

        [Fact]
        public void StringEqualsMethod()
        {
            Assert(
                x => x.A.Equals("Awesome"),
                1,
                "{A: 'Awesome'}");
        }

        [Fact]
        public void NotStringEqualsMethod()
        {
            Assert(
                x => !x.A.Equals("Awesome"),
                1,
                "{A: {$ne: 'Awesome'}}");
        }

        [Fact]
        public void OfType()
        {
            Assert(__otherCollection,
                x => x.Children.OfType<OtherChild2>().Any(y => y.Z == 10),
                0,
                "{Children: {$elemMatch: { _t: 'OtherChild2', Z: 10 }}}");
        }

        [Fact]
        public void String_IsNullOrEmpty()
        {
            Assert(
                x => string.IsNullOrEmpty(x.A),
                0,
                "{A: { $in: [null, ''] } }");
        }

        [Fact]
        public void Not_String_IsNullOrEmpty()
        {
            Assert(
                x => !string.IsNullOrEmpty(x.A),
                2,
                "{A: { $nin: [null, ''] } }");
        }

        [Fact]
        public void Binding_through_a_necessary_conversion()
        {
            long id = 10;
            var root = __collection.FindSync(x => x.Id == id).FirstOrDefault();

            root.Should().NotBeNull();
            root.A.Should().Be("Awesome");
        }

        [Fact]
        public void Binding_through_an_unnecessary_conversion()
        {
            var root = FindFirstOrDefault(__collection, 10);

            root.Should().NotBeNull();
            root.A.Should().Be("Awesome");
        }

        [Fact]
        public void Binding_through_an_unnecessary_conversion_with_a_builder()
        {
            var root = FindFirstOrDefaultWithBuilder(__collection, 10);

            root.Should().NotBeNull();
            root.A.Should().Be("Awesome");
        }

        [Fact]
        public void Injecting_a_filter()
        {
            var filter = Builders<Root>.Filter.Eq(x => x.B, "Balloon");
            var root = __collection.FindSync(x => filter.Inject()).Single();

            root.Should().NotBeNull();
            root.A.Should().Be("Awesome");
            root.B.Should().Be("Balloon");
        }

        [Fact]
        public void Injecting_a_filter_with_a_conjunction()
        {
            var filter = Builders<Root>.Filter.Eq(x => x.B, "Balloon");
            var root = __collection.FindSync(x => x.A == "Awesome" && filter.Inject()).Single();

            root.Should().NotBeNull();
            root.A.Should().Be("Awesome");
            root.B.Should().Be("Balloon");
        }

        private T FindFirstOrDefault<T>(IMongoCollection<T> collection, int id) where T : IRoot
        {
            return collection.FindSync(x => x.Id == id).FirstOrDefault();
        }

        private T FindFirstOrDefaultWithBuilder<T>(IMongoCollection<T> collection, int id) where T : IRoot
        {
            return collection.FindSync(Builders<T>.Filter.Eq(x => x.Id, id)).FirstOrDefault();
        }

        public void Assert<T>(IMongoCollection<T> collection, Expression<Func<T, bool>> filter, int expectedCount, string expectedFilter)
        {
            Assert(collection, filter, expectedCount, BsonDocument.Parse(expectedFilter));
        }

        public List<T> Assert<T>(IMongoCollection<T> collection, Expression<Func<T, bool>> filter, int expectedCount, BsonDocument expectedFilter)
        {
            var serializer = BsonSerializer.SerializerRegistry.GetSerializer<T>();
            var filterDocument = PredicateTranslator.Translate(filter, serializer, BsonSerializer.SerializerRegistry);

            var list = collection.FindSync(filterDocument).ToList();

            filterDocument.Should().Be(expectedFilter);
            list.Count.Should().Be(expectedCount);
            return list;
        }

        public void Assert(Expression<Func<Root, bool>> filter, int expectedCount, string expectedFilter)
        {
            Assert(filter, expectedCount, BsonDocument.Parse(expectedFilter));
        }

        public void Assert(Expression<Func<Root, bool>> filter, int expectedCount, BsonDocument expectedFilter)
        {
            Assert(__collection, filter, expectedCount, expectedFilter);
        }

        protected override void FillCustomDocuments(List<Root> customDocuments)
        {
            customDocuments.AddRange(
                new[]
                {
                    new Root { Id = 1, M = new int[0] },
                    new Root { Id = 2, M = new [] { 1 } },
                    new Root { Id = 3, M = new [] { 2 } },
                    new Root { Id = 4, M = new [] { 3 } },
                    new Root { Id = 5, M = new [] { 4 } },
                    new Root { Id = 6, M = new [] { 1, 2 } },
                    new Root { Id = 7, M = new [] { 1, 3 } },
                    new Root { Id = 8, M = new [] { 1, 4 } },
                    new Root { Id = 9, M = new [] { 2, 3 } },
                    new Root { Id = 10, M = new [] { 3, 4} },
                    new Root { Id = 11, M = new [] { 1, 2, 3 } },
                    new Root { Id = 12, M = new [] { 1, 2 ,4 } },
                    new Root { Id = 13, M = new [] { 1, 3, 4 } },
                    new Root { Id = 14, M = new [] { 1, 2, 3, 4 } }
                });
        }

        public void AssertUsingCustomCollection(Expression<Func<Root, bool>> filter, string expectedFilter)
        {
            AssertUsingCustomCollection(filter, BsonDocument.Parse(expectedFilter));
        }

        public void AssertUsingCustomCollection(Expression<Func<Root, bool>> filter, BsonDocument expectedFilter)
        {
            var expectedResult = __customDocuments.Where(filter.Compile()).ToList();

            var queryResult = Assert(__customCollection, filter, expectedResult.Count, expectedFilter);

            queryResult.Select(r => r.Id).Should().Equal(expectedResult.Select(r => r.Id));
        }
    }
}