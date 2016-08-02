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
using System.Linq.Expressions;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Translators;
using Xunit;
using System.Globalization;

namespace MongoDB.Driver.Tests.Linq.Translators
{
    public class AggregateProjectTranslatorTests : IntegrationTestBase
    {
        [Fact]
        public void Should_translate_using_non_anonymous_type_with_default_constructor()
        {
            var result = Project(x => new RootView { Property = x.A, Field = x.B });

            result.Projection.Should().Be("{ Property: \"$A\", Field: \"$B\", _id: 0 }");

            result.Value.Property.Should().Be("Awesome");
            result.Value.Field.Should().Be("Balloon");
        }

        [Fact]
        public void Should_translate_using_non_anonymous_type_with_parameterized_constructor()
        {
            var result = Project(x => new RootView(x.A) { Field = x.B });

            result.Projection.Should().Be("{ Field: \"$B\", Property: \"$A\", _id: 0 }");

            result.Value.Property.Should().Be("Awesome");
            result.Value.Field.Should().Be("Balloon");
        }

        [SkippableFact]
        public void Should_translate_abs()
        {
            RequireServer.Where(minimumVersion: "3.1.6");
            var result = Project(x => new { Result = Math.Abs(x.C.E.F) });

            result.Projection.Should().Be("{ Result: { \"$abs\": \"$C.E.F\" }, _id: 0 }");

            result.Value.Result.Should().Be(11);
        }

        [Fact]
        public void Should_translate_add()
        {
            var result = Project(x => new { Result = x.C.E.F + x.C.E.H });

            result.Projection.Should().Be("{ Result: { \"$add\": [\"$C.E.F\", \"$C.E.H\"] }, _id: 0 }");

            result.Value.Result.Should().Be(33);
        }

        [Fact]
        public void Should_translate_add_flattened()
        {
            var result = Project(x => new { Result = x.Id + x.C.E.F + x.C.E.H });

            result.Projection.Should().Be("{ Result: { \"$add\": [\"$_id\", \"$C.E.F\", \"$C.E.H\"] }, _id: 0 }");

            result.Value.Result.Should().Be(43);
        }

        [SkippableFact]
        public void Should_translate_allElementsTrue()
        {
            RequireServer.Where(minimumVersion: "2.6.0");

            var result = Project(x => new { Result = x.G.All(g => g.E.F > 30) });

            result.Projection.Should().Be("{ Result: { \"$allElementsTrue\" : { \"$map\": { input: \"$G\", as: \"g\", in: { \"$gt\": [\"$$g.E.F\", 30 ] } } } }, _id: 0 }");

            result.Value.Result.Should().BeTrue();
        }

        [SkippableFact]
        public void Should_translate_anyElementTrue()
        {
            RequireServer.Where(minimumVersion: "2.6.0");

            var result = Project(x => new { Result = x.G.Any() });

            result.Projection.Should().Be("{ Result: { \"$gt\" : [{ \"$size\" : \"$G\" }, 0] }, _id: 0 }");

            result.Value.Result.Should().BeTrue();
        }

        [SkippableFact]
        public void Should_translate_anyElementTrue_with_predicate()
        {
            RequireServer.Where(minimumVersion: "2.6.0");

            var result = Project(x => new { Result = x.G.Any(g => g.E.F > 40) });

            result.Projection.Should().Be("{ Result: { \"$anyElementTrue\" : { \"$map\": { input: \"$G\", as: \"g\", in: { \"$gt\": [\"$$g.E.F\", 40 ] } } } }, _id: 0 }");

            result.Value.Result.Should().BeTrue();
        }

        [SkippableFact]
        public void Should_translate_anyElementTrue_using_Contains()
        {
            RequireServer.Where(minimumVersion: "2.6.0");

            var result = Project(x => new { Result = x.L.Contains(5) });

            result.Projection.Should().Be("{ Result: { \"$anyElementTrue\": { $map: { input: \"$L\", as: \"x\", in: { $eq: [\"$$x\", 5 ] } } } }, _id: 0 }");

            result.Value.Result.Should().BeTrue();
        }

        [SkippableFact]
        public void Should_translate_anyElementTrue_using_Contains_on_a_local_collection()
        {
            RequireServer.Where(minimumVersion: "2.6.0");

            var local = new[] { 11, 33, 55 };
            var result = Project(x => new { Result = local.Contains(x.C.E.F) });

            result.Projection.Should().Be("{ Result: { \"$anyElementTrue\": { $map: { input: [11, 33, 55], as: \"x\", in: { $eq: [\"$$x\", \"$C.E.F\" ] } } } }, _id: 0 }");

            result.Value.Result.Should().BeTrue();
        }

        [Fact]
        public void Should_translate_and()
        {
            var result = Project(x => new { Result = x.A == "yes" && x.B == "no" });

            result.Projection.Should().Be("{ Result: { \"$and\": [{ \"$eq\": [\"$A\", \"yes\"] }, { \"$eq\": [\"$B\", \"no\"] }] }, _id: 0 }");

            result.Value.Result.Should().BeFalse();
        }

        [Fact]
        public void Should_translate_and_flattened()
        {
            var result = Project(x => new { Result = x.A == "yes" && x.B == "no" && x.C.D == "maybe" });

            result.Projection.Should().Be("{ Result: { \"$and\": [{ \"$eq\": [\"$A\", \"yes\"] }, { \"$eq\": [\"$B\", \"no\"] }, { \"$eq\" : [\"$C.D\", \"maybe\"] } ] }, _id: 0 }");

            result.Value.Result.Should().BeFalse();
        }

        [SkippableFact]
        public void Should_translate_arrayElemAt_using_a_constant_ElementAt()
        {
            RequireServer.Where(minimumVersion: "3.1.7");

            var result = Project(x => new { Result = x.M.ElementAt(1) });

            result.Projection.Should().Be("{ Result: { $arrayElemAt: [\"$M\", 1] }, _id: 0 }");

            result.Value.Result.Should().Be(4);
        }

        [SkippableFact]
        public void Should_translate_arrayElemAt_using_a_constant_indexer()
        {
            RequireServer.Where(minimumVersion: "3.1.7");

            var result = Project(x => new { Result = x.M[1] });

            result.Projection.Should().Be("{ Result: { $arrayElemAt: [\"$M\", 1] }, _id: 0 }");

            result.Value.Result.Should().Be(4);
        }

        [SkippableFact]
        public void Should_translate_arrayElemAt_using_a_constant_get_Item()
        {
            RequireServer.Where(minimumVersion: "3.1.7");

            var result = Project(x => new { Result = x.O[1] });

            result.Projection.Should().Be("{ Result: { $arrayElemAt: [\"$O\", 1] }, _id: 0 }");

            result.Value.Result.Should().Be(20);
        }

        [SkippableFact]
        public void Should_translate_arrayElemAt_using_a_variable_ElementAt()
        {
            RequireServer.Where(minimumVersion: "3.1.7");

            var result = Project(x => new { Result = (int?)x.M.ElementAt(x.T["one"]) });

            result.Projection.Should().Be("{ Result: { $arrayElemAt: [\"$M\", \"$T.one\"] }, _id: 0 }");

            result.Value.Result.Should().Be(4);
        }

        [SkippableFact]
        public void Should_translate_arrayElemAt_using_a_variable_indexer()
        {
            RequireServer.Where(minimumVersion: "3.1.7");

            var result = Project(x => new { Result = (int?)x.M[x.T["one"]] });

            result.Projection.Should().Be("{ Result: { $arrayElemAt: [\"$M\", \"$T.one\"] }, _id: 0 }");

            result.Value.Result.Should().Be(4);
        }

        [SkippableFact]
        public void Should_translate_arrayElemAt_using_a_variable_get_Item()
        {
            RequireServer.Where(minimumVersion: "3.1.7");

            var result = Project(x => new { Result = (long?)x.O[x.T["one"]] });

            result.Projection.Should().Be("{ Result: { $arrayElemAt: [\"$O\", \"$T.one\"] }, _id: 0 }");

            result.Value.Result.Should().Be(20);
        }

        [SkippableFact]
        public void Should_translate_arrayElemAt_using_a_constant_ElementAt_followed_by_a_field()
        {
            RequireServer.Where(minimumVersion: "3.1.7");

            var result = Project(x => new { Result = x.G.ElementAt(1).D });

            result.Projection.Should().Be("{ Result: { $let: { vars: { item: { \"$arrayElemAt\": [\"$G\", 1] } }, in: \"$$item.D\" } }, _id: 0 }");

            result.Value.Result.Should().Be("Dolphin");
        }

        [SkippableFact]
        public void Should_translate_arrayElemAt_using_a_variable_ElementAt_followed_by_a_field()
        {
            RequireServer.Where(minimumVersion: "3.1.7");

            var result = Project(x => new { Result = x.G.ElementAt(x.T["one"]).D });

            result.Projection.Should().Be("{ Result: { $let: { vars: { item: { \"$arrayElemAt\": [\"$G\", \"$T.one\"] } }, in: \"$$item.D\" } }, _id: 0 }");

            result.Value.Result.Should().Be("Dolphin");
        }

        [SkippableFact]
        public void Should_translate_arrayElemAt_using_First()
        {
            RequireServer.Where(minimumVersion: "3.1.7");

            var result = Project(x => new { Result = x.M.First() });

            result.Projection.Should().Be("{ Result: { \"$arrayElemAt\": [\"$M\", 0] }, _id: 0 }");

            result.Value.Result.Should().Be(2);
        }

        [SkippableFact]
        public void Should_translate_arrayElemAt_using_First_followed_by_a_field()
        {
            RequireServer.Where(minimumVersion: "3.1.7");

            var result = Project(x => new { Result = x.G.First().D });

            result.Projection.Should().Be("{ Result: { $arrayElemAt: [\"$G.D\", 0] }, _id: 0 }");

            result.Value.Result.Should().Be("Don't");
        }

        [SkippableFact]
        public void Should_translate_arrayElemAt_using_Last()
        {
            RequireServer.Where(minimumVersion: "3.1.7");

            var result = Project(x => new { Result = x.M.Last() });

            result.Projection.Should().Be("{ Result: { \"$arrayElemAt\": [\"$M\", -1] }, _id: 0 }");

            result.Value.Result.Should().Be(5);
        }

        [SkippableFact]
        public void Should_translate_arrayElemAt_using_Last_followed_by_a_field()
        {
            RequireServer.Where(minimumVersion: "3.1.7");

            var result = Project(x => new { Result = x.G.Last().D });

            result.Projection.Should().Be("{ Result: { \"$arrayElemAt\": [\"$G.D\", -1] }, _id: 0 }");

            result.Value.Result.Should().Be("Dolphin");
        }

        [SkippableFact]
        public void Should_translate_avg()
        {
            RequireServer.Where(minimumVersion: "3.1.7");

            var result = Project(x => new { Result = x.M.Average() });

            result.Projection.Should().Be("{ Result: { \"$avg\": \"$M\" }, _id: 0 }");

            result.Value.Result.Should().BeApproximately(3.66666667, .0001);
        }

        [SkippableFact]
        public void Should_translate_avg_with_selector()
        {
            RequireServer.Where(minimumVersion: "3.1.7");

            var result = Project(x => new { Result = x.G.Average(g => g.E.F) });

            result.Projection.Should().Be("{ Result: { \"$avg\": \"$G.E.F\" }, _id: 0 }");

            result.Value.Result.Should().Be(44);
        }

        [SkippableFact]
        public void Should_translate_ceil()
        {
            RequireServer.Where(minimumVersion: "3.1.6");

            var result = Project(x => new { Result = Math.Ceiling(x.U) });

            result.Projection.Should().Be("{ Result: { \"$ceil\": \"$U\" }, _id: 0 }");

            result.Value.Result.Should().Be(2);
        }

        [Fact]
        public void Should_translate_coalesce()
        {
            var result = Project(x => new { Result = x.A ?? "funny" });

            result.Projection.Should().Be("{ Result: { \"$ifNull\": [\"$A\", \"funny\"] }, _id: 0 }");

            result.Value.Result.Should().Be("Awesome");
        }

        [Fact]
        public void Should_translate_compare()
        {
            var result = Project(x => new { Result = x.A.CompareTo("Awesome") });

            result.Projection.Should().Be("{ Result: { \"$cmp\": [\"$A\", \"Awesome\"] }, _id: 0 }");

            result.Value.Result.Should().Be(0);
        }

        [SkippableFact]
        public void Should_translate_concat()
        {
            RequireServer.Where(minimumVersion: "2.4.0");

            var result = Project(x => new { Result = x.A + x.B });

            result.Projection.Should().Be("{ Result: { \"$concat\": [\"$A\", \"$B\"] }, _id: 0 }");

            result.Value.Result.Should().Be("AwesomeBalloon");
        }

        [SkippableFact]
        public void Should_translate_concat_flattened()
        {
            RequireServer.Where(minimumVersion: "2.4.0");

            var result = Project(x => new { Result = x.A + " " + x.B });

            result.Projection.Should().Be("{ Result: { \"$concat\": [\"$A\", \" \", \"$B\"] }, _id: 0 }");

            result.Value.Result.Should().Be("Awesome Balloon");
        }

        [SkippableFact]
        public void Should_translate_concatArrays()
        {
            RequireServer.Where(minimumVersion: "3.1.6");

            var result = Project(x => new { Result = x.M.Concat(x.L) });

            result.Projection.Should().Be("{ Result: { \"$concatArrays\": [\"$M\", \"$L\"] }, _id: 0 }");

            result.Value.Result.Should().BeEquivalentTo(2, 4, 5, 1, 3, 5);
        }

        [Fact]
        public void Should_translate_condition()
        {
            var result = Project(x => new { Result = x.A == "funny" ? "a" : "b" });

            result.Projection.Should().Be("{ Result: { \"$cond\": [{ \"$eq\": [\"$A\", \"funny\"] }, \"a\", \"b\"] }, _id: 0 }");

            result.Value.Result.Should().Be("b");
        }

        [Fact]
        public void Should_translate_day_of_month()
        {
            var result = Project(x => new { Result = x.J.Day });

            result.Projection.Should().Be("{ Result: { \"$dayOfMonth\": \"$J\" }, _id: 0 }");

            result.Value.Result.Should().Be(1);
        }

        [Fact]
        public void Should_translate_day_of_week()
        {
            var result = Project(x => new { Result = x.J.DayOfWeek });

            result.Projection.Should().Be("{ Result: { \"$subtract\" : [{ \"$dayOfWeek\": \"$J\" }, 1] }, _id: 0 }");

            result.Value.Result.Should().Be(DayOfWeek.Saturday);
        }

        [Fact]
        public void Should_translate_day_of_year()
        {
            var result = Project(x => new { Result = x.J.DayOfYear });

            result.Projection.Should().Be("{ Result: { \"$dayOfYear\": \"$J\" }, _id: 0 }");

            result.Value.Result.Should().Be(336);
        }

        [Fact]
        public void Should_translate_datetime_add_xxx()
        {
            var result = Project(x => new { J = x.J, Result = x.J.AddDays(2.5) });

            result.Projection.Should().Be("{ J: \"$J\", Result: { \"$add\": [\"$J\", NumberLong(216000000)] }, _id: 0 }");

            result.Value.Result.Should().Be(result.Value.J.AddDays(2.5));
        }

        [Fact]
        public void Should_translate_datetime_add_xxx_negative()
        {
            var result = Project(x => new { J = x.J, Result = x.J.AddMinutes(-3) });

            result.Projection.Should().Be("{ J: \"$J\", Result: { \"$add\": [\"$J\", NumberLong(-180000)] }, _id: 0 }");

            result.Value.Result.Should().Be(result.Value.J.AddMinutes(-3));
        }

        [Fact]
        public void Should_translate_datetime_add_xxx_from_variable()
        {
            var result = Project(x => new { J = x.J, N = x.C.E.F, Result = x.J.AddHours(x.C.E.F) });

            result.Projection.Should().Be("{ J: \"$J\", N: \"$C.E.F\", Result: { \"$add\": [\"$J\", { \"$multiply\": [NumberLong(3600000), \"$C.E.F\"] } ] }, _id: 0 }");

            result.Value.Result.Should().Be(result.Value.J.AddHours(result.Value.N));
        }

        [Fact]
        public void Should_translate_datetime_add_xxx_negative_from_variable()
        {
            var result = Project(x => new { J = x.J, N = x.C.E.H, Result = x.J.AddHours(-x.C.E.H) });

            result.Projection.Should().Be("{ J: \"$J\", N: \"$C.E.H\", Result: { \"$add\": [\"$J\", { \"$multiply\": [NumberLong(3600000), { \"$subtract\": [ 0, \"$C.E.H\" ] } ] } ] }, _id: 0 }");

            result.Value.Result.Should().Be(result.Value.J.AddHours(-result.Value.N));
        }

        [SkippableFact]
        public void Should_translate_datetime_to_string()
        {
            RequireServer.Where(minimumVersion: "3.0.0"); // ??
            
            var result = Project(x => new { J = x.J, Result = x.J.ToString() });

            result.Projection.Should().Be("{ J: \"$J\", Result: { \"$dateToString\": { \"format\": \"%Y-%m-%d %H:%M\", \"date\": \"$J\" } }, _id: 0 }");

            result.Value.Result.Should().Be(result.Value.J.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
        }

        [SkippableFact]
        public void Should_translate_datetime_to_string_with_format()
        {
            RequireServer.Where(minimumVersion: "3.0.0"); // ??

            var result = Project(x => new { J = x.J, Result = x.J.ToString("%d.%m.%Y") });

            result.Projection.Should().Be("{ J: \"$J\", Result: { \"$dateToString\": { \"format\": \"%d.%m.%Y\", \"date\": \"$J\" } }, _id: 0 }");

            result.Value.Result.Should().Be(result.Value.J.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture));
        }

        [SkippableFact]
        public void Should_translate_datetime_to_string_with_format_and_culture()
        {
            RequireServer.Where(minimumVersion: "3.0.0"); // ??

            var result = Project(x => new { J = x.J, Result = x.J.ToString("%Y-%m-%dT%H:%M:%S.%LZ", CultureInfo.InvariantCulture) });

            result.Projection.Should().Be("{ J: \"$J\", Result: { \"$dateToString\": { \"format\": \"%Y-%m-%dT%H:%M:%S.%LZ\", \"date\": \"$J\" } }, _id: 0 }");

            result.Value.Result.Should().Be(result.Value.J.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Should_translate_divide()
        {
            var result = Project(x => new { Result = (double)x.C.E.F / x.C.E.H });

            result.Projection.Should().Be("{ Result: { \"$divide\": [\"$C.E.F\", \"$C.E.H\"] }, _id: 0 }");

            result.Value.Result.Should().Be(0.5);
        }

        [Fact]
        public void Should_translate_divide_3_numbers()
        {
            var result = Project(x => new { Result = (double)x.Id / x.C.E.F / x.C.E.H });

            result.Projection.Should().Be("{ Result: { \"$divide\": [{ \"$divide\": [\"$_id\", \"$C.E.F\"] }, \"$C.E.H\"] }, _id: 0 }");

            result.Value.Result.Should().BeApproximately(0.04, .01);
        }

        [Fact]
        public void Should_translate_equals()
        {
            var result = Project(x => new { Result = x.C.E.F == 5 });

            result.Projection.Should().Be("{ Result: { \"$eq\": [\"$C.E.F\", 5] }, _id: 0 }");

            result.Value.Result.Should().BeFalse();
        }

        [Fact]
        public void Should_translate_equals_as_a_method_call()
        {
            var result = Project(x => new { Result = x.C.E.F.Equals(5) });

            result.Projection.Should().Be("{ Result: { \"$eq\": [\"$C.E.F\", 5] }, _id: 0 }");

            result.Value.Result.Should().BeFalse();
        }

        [SkippableFact]
        public void Should_translate_exp()
        {
            RequireServer.Where(minimumVersion: "3.1.6");

            var result = Project(x => new { Result = Math.Exp(x.C.E.F) });

            result.Projection.Should().Be("{ Result: { \"$exp\": [\"$C.E.F\"] }, _id: 0 }");

            result.Value.Result.Should().BeApproximately(59874.1417151978, .0001);
        }

        [SkippableFact]
        public void Should_translate_floor()
        {
            RequireServer.Where(minimumVersion: "3.1.6");

            var result = Project(x => new { Result = Math.Floor(x.U) });

            result.Projection.Should().Be("{ Result: { \"$floor\": \"$U\" }, _id: 0 }");

            result.Value.Result.Should().Be(1);
        }

        [Fact]
        public void Should_translate_greater_than()
        {
            var result = Project(x => new { Result = x.C.E.F > 5 });

            result.Projection.Should().Be("{ Result: { \"$gt\": [\"$C.E.F\", 5] }, _id: 0 }");

            result.Value.Result.Should().BeTrue();
        }

        [Fact]
        public void Should_translate_greater_than_or_equal()
        {
            var result = Project(x => new { Result = x.C.E.F >= 5 });

            result.Projection.Should().Be("{ Result: { \"$gte\": [\"$C.E.F\", 5] }, _id: 0 }");

            result.Value.Result.Should().BeTrue();
        }

        [Fact]
        public void Should_translate_hour()
        {
            var result = Project(x => new { Result = x.J.Hour });

            result.Projection.Should().Be("{ Result: { \"$hour\": \"$J\" }, _id: 0 }");

            result.Value.Result.Should().Be(13);
        }

        [Fact]
        public void Should_translate_less_than()
        {
            var result = Project(x => new { Result = x.C.E.F < 5 });

            result.Projection.Should().Be("{ Result: { \"$lt\": [\"$C.E.F\", 5] }, _id: 0 }");

            result.Value.Result.Should().BeFalse();
        }

        [Fact]
        public void Should_translate_less_than_or_equal()
        {
            var result = Project(x => new { Result = x.C.E.F <= 5 });

            result.Projection.Should().Be("{ Result: { \"$lte\": [\"$C.E.F\", 5] }, _id: 0 }");

            result.Value.Result.Should().BeFalse();
        }

        [SkippableFact]
        public void Should_translate_literal_when_a_constant_strings_begins_with_a_dollar()
        {
            RequireServer.Where(minimumVersion: "2.6.0");

            var result = Project(x => new { Result = x.A == "$1" });

            result.Projection.Should().Be("{ Result: { \"$eq\": [\"$A\", { \"$literal\": \"$1\" }] }, _id: 0 }");

            result.Value.Result.Should().BeFalse();
        }

        [SkippableFact]
        public void Should_translate_ln()
        {
            RequireServer.Where(minimumVersion: "3.1.6");

            var result = Project(x => new { Result = Math.Log(x.C.E.F) });

            result.Projection.Should().Be("{ Result: { \"$ln\": [\"$C.E.F\"] }, _id: 0 }");

            result.Value.Result.Should().BeApproximately(2.39789527279837, .0001);
        }

        [SkippableFact]
        public void Should_translate_log()
        {
            RequireServer.Where(minimumVersion: "3.1.6");

            var result = Project(x => new { Result = Math.Log(x.C.E.F, 11) });

            result.Projection.Should().Be("{ Result: { \"$log\": [\"$C.E.F\", 11.0] }, _id: 0 }");

            result.Value.Result.Should().Be(1);
        }

        [SkippableFact]
        public void Should_translate_log10()
        {
            RequireServer.Where(minimumVersion: "3.1.6");

            var result = Project(x => new { Result = Math.Log10(x.C.E.F) });

            result.Projection.Should().Be("{ Result: { \"$log10\": [\"$C.E.F\"] }, _id: 0 }");

            result.Value.Result.Should().BeApproximately(1.0413928515823, .0001);
        }

        [SkippableFact]
        public void Should_translate_map_with_document()
        {
            RequireServer.Where(minimumVersion: "2.6.0");

            var result = Project(x => new { Result = x.G.Select(g => g.D + "0") });

            result.Projection.Should().Be("{ Result: { \"$map\": { input: \"$G\", as: \"g\", in: { \"$concat\": [\"$$g.D\", \"0\"] } } }, _id: 0 }");

            result.Value.Result.Should().Equal("Don't0", "Dolphin0");
        }

        [SkippableFact]
        public void Should_translate_map_with_value()
        {
            RequireServer.Where(minimumVersion: "2.6.0");

            var result = Project(x => new { Result = x.C.E.I.Select(i => i + "0") });

            result.Projection.Should().Be("{ Result: { \"$map\": { input: \"$C.E.I\", as: \"i\", in: { \"$concat\": [\"$$i\", \"0\"] } } }, _id: 0 }");

            result.Value.Result.Should().Equal("it0", "icky0");
        }

        [SkippableFact]
        public void Should_translate_max()
        {
            RequireServer.Where(minimumVersion: "3.1.7");

            var result = Project(x => new { Result = x.M.Max() });

            result.Projection.Should().Be("{ Result: { \"$max\": \"$M\" }, _id: 0 }");

            result.Value.Result.Should().Be(5);
        }

        [SkippableFact]
        public void Should_translate_max_with_selector()
        {
            RequireServer.Where(minimumVersion: "3.1.7");

            var result = Project(x => new { Result = x.G.Max(g => g.E.F) });

            result.Projection.Should().Be("{ Result: { \"$max\": \"$G.E.F\" }, _id: 0 }");

            result.Value.Result.Should().Be(55);
        }

        [SkippableFact]
        public void Should_translate_millisecond()
        {
            RequireServer.Where(minimumVersion: "2.4.0");

            var result = Project(x => new { Result = x.J.Millisecond });

            result.Projection.Should().Be("{ Result: { \"$millisecond\": \"$J\" }, _id: 0 }");

            result.Value.Result.Should().Be(16);
        }

        [SkippableFact]
        public void Should_translate_min()
        {
            RequireServer.Where(minimumVersion: "3.1.7");

            var result = Project(x => new { Result = x.M.Min() });

            result.Projection.Should().Be("{ Result: { \"$min\": \"$M\" }, _id: 0 }");

            result.Value.Result.Should().Be(2);
        }

        [SkippableFact]
        public void Should_translate_min_with_selector()
        {
            RequireServer.Where(minimumVersion: "3.1.7");

            var result = Project(x => new { Result = x.G.Min(g => g.E.F) });

            result.Projection.Should().Be("{ Result: { \"$min\": \"$G.E.F\" }, _id: 0 }");

            result.Value.Result.Should().Be(33);
        }

        [Fact]
        public void Should_translate_minute()
        {
            var result = Project(x => new { Result = x.J.Minute });

            result.Projection.Should().Be("{ Result: { \"$minute\": \"$J\" }, _id: 0 }");

            result.Value.Result.Should().Be(14);
        }

        [Fact]
        public void Should_translate_modulo()
        {
            var result = Project(x => new { Result = x.C.E.F % 5 });

            result.Projection.Should().Be("{ Result: { \"$mod\": [\"$C.E.F\", 5] }, _id: 0 }");

            result.Value.Result.Should().Be(1);
        }

        [Fact]
        public void Should_translate_month()
        {
            var result = Project(x => new { Result = x.J.Month });

            result.Projection.Should().Be("{ Result: { \"$month\": \"$J\" }, _id: 0 }");

            result.Value.Result.Should().Be(12);
        }

        [Fact]
        public void Should_translate_multiply()
        {
            var result = Project(x => new { Result = x.C.E.F * x.C.E.H });

            result.Projection.Should().Be("{ Result: { \"$multiply\": [\"$C.E.F\", \"$C.E.H\"] }, _id: 0 }");

            result.Value.Result.Should().Be(242);
        }

        [Fact]
        public void Should_translate_multiply_flattened()
        {
            var result = Project(x => new { Result = x.Id * x.C.E.F * x.C.E.H });

            result.Projection.Should().Be("{ Result: { \"$multiply\": [\"$_id\", \"$C.E.F\", \"$C.E.H\"] }, _id: 0 }");

            result.Value.Result.Should().Be(2420);
        }
        
        [Fact]
        public void Should_translate_negation()
        {
            var result = Project(x => new { Result = -x.C.E.F });

            result.Projection.Should().Be("{ Result: { \"$subtract\": [0, \"$C.E.F\"] }, _id: 0 }");

            result.Value.Result.Should().Be(-11);
        }

        [Fact]
        public void Should_translate_not()
        {
            var result = Project(x => new { Result = !x.K });

            result.Projection.Should().Be("{ Result: { \"$not\": [\"$K\"] }, _id: 0 }");

            result.Value.Result.Should().BeFalse();
        }

        [Fact]
        public void Should_translate_not_with_comparison()
        {
            var result = Project(x => new { Result = !(x.C.E.F < 3) });

            result.Projection.Should().Be("{ Result: { \"$not\": [{ \"$lt\": [\"$C.E.F\", 3] }] }, _id: 0 }");

            result.Value.Result.Should().BeTrue();
        }

        [Fact]
        public void Should_translate_not_equals()
        {
            var result = Project(x => new { Result = x.C.E.F != 5 });

            result.Projection.Should().Be("{ Result: { \"$ne\": [\"$C.E.F\", 5] }, _id: 0 }");

            result.Value.Result.Should().BeTrue();
        }

        [Fact]
        public void Should_translate_or()
        {
            var result = Project(x => new { Result = x.A == "yes" || x.B == "no" });

            result.Projection.Should().Be("{ Result: { \"$or\": [{ \"$eq\": [\"$A\", \"yes\"] }, { \"$eq\": [\"$B\", \"no\"] }] }, _id: 0 }");

            result.Value.Result.Should().BeFalse();
        }

        [Fact]
        public void Should_translate_or_flattened()
        {
            var result = Project(x => new { Result = x.A == "yes" || x.B == "no" || x.C.D == "maybe" });

            result.Projection.Should().Be("{ Result: { \"$or\": [{ \"$eq\": [\"$A\", \"yes\"] }, { \"$eq\": [\"$B\", \"no\"] }, { \"$eq\" : [\"$C.D\", \"maybe\"] } ] }, _id: 0 }");

            result.Value.Result.Should().BeFalse();
        }

        [SkippableFact]
        public void Should_translate_pow()
        {
            RequireServer.Where(minimumVersion: "3.1.6");

            var result = Project(x => new { Result = Math.Pow(x.C.E.F, 5) });

            result.Projection.Should().Be("{ Result: { \"$pow\": [\"$C.E.F\", 5.0] }, _id: 0 }");

            result.Value.Result.Should().Be(161051);
        }

        [Fact]
        public void Should_translate_second()
        {
            var result = Project(x => new { Result = x.J.Second });

            result.Projection.Should().Be("{ Result: { \"$second\": \"$J\" }, _id: 0 }");

            result.Value.Result.Should().Be(15);
        }

        [SkippableFact]
        public void Should_translate_size_greater_than_zero_from_any()
        {
            RequireServer.Where(minimumVersion: "2.6.0");

            var result = Project(x => new { Result = x.M.Any() });

            result.Projection.Should().Be("{ Result: { \"$gt\": [{ \"$size\": \"$M\" }, 0] }, _id: 0 }");

            result.Value.Result.Should().BeTrue();
        }

        [SkippableFact]
        public void Should_translate_size_from_an_array()
        {
            RequireServer.Where(minimumVersion: "2.6.0");

            var result = Project(x => new { Result = x.M.Length });

            result.Projection.Should().Be("{ Result: { \"$size\": \"$M\" }, _id: 0 }");

            result.Value.Result.Should().Be(3);
        }

        [SkippableFact]
        public void Should_translate_size_from_Count_extension_method()
        {
            RequireServer.Where(minimumVersion: "2.6.0");

            var result = Project(x => new { Result = x.M.Count() });

            result.Projection.Should().Be("{ Result: { \"$size\": \"$M\" }, _id: 0 }");

            result.Value.Result.Should().Be(3);
        }

        [SkippableFact]
        public void Should_translate_size_from_LongCount_extension_method()
        {
            RequireServer.Where(minimumVersion: "2.6.0");

            var result = Project(x => new { Result = x.M.LongCount() });

            result.Projection.Should().Be("{ Result: { \"$size\": \"$M\" }, _id: 0 }");

            result.Value.Result.Should().Be(3);
        }

        [SkippableFact]
        public void Should_translate_size_from_Count_property_on_Generic_ICollection()
        {
            RequireServer.Where(minimumVersion: "2.6.0");

            var result = Project(x => new { Result = x.L.Count });

            result.Projection.Should().Be("{ Result: { \"$size\": \"$L\" }, _id: 0 }");

            result.Value.Result.Should().Be(3);
        }

        [SkippableFact]
        public void Should_translate_set_difference()
        {
            RequireServer.Where(minimumVersion: "2.6.0");

            var result = Project(x => new { Result = x.C.E.I.Except(new[] { "it", "not in here" }) });

            result.Projection.Should().Be("{ Result: { \"$setDifference\": [\"$C.E.I\", [\"it\", \"not in here\"] ] }, _id: 0 }");

            result.Value.Result.Should().Equal("icky");
        }

        [SkippableFact]
        public void Should_translate_set_difference_reversed()
        {
            RequireServer.Where(minimumVersion: "2.6.0");

            var result = Project(x => new { Result = new[] { "it", "not in here" }.Except(x.C.E.I) });

            result.Projection.Should().Be("{ Result: { \"$setDifference\": [[\"it\", \"not in here\"], \"$C.E.I\"] }, _id: 0 }");

            result.Value.Result.Should().Equal("not in here");
        }

        [SkippableFact]
        public void Should_translate_set_equals()
        {
            RequireServer.Where(minimumVersion: "2.6.0");

            var result = Project(x => new { Result = x.L.SetEquals(new[] { 1, 3, 5 }) });

            result.Projection.Should().Be("{ Result: { \"$setEquals\": [\"$L\", [1, 3, 5]] }, _id: 0 }");

            result.Value.Result.Should().BeTrue();
        }

        [SkippableFact]
        public void Should_translate_set_equals_reversed()
        {
            RequireServer.Where(minimumVersion: "2.6.0");
            var set = new HashSet<int>(new[] { 1, 3, 5 });

            var result = Project(x => new { Result = set.SetEquals(x.L) });

            result.Projection.Should().Be("{ Result: { \"$setEquals\": [[1, 3, 5], \"$L\"] }, _id: 0 }");

            result.Value.Result.Should().BeTrue();
        }

        [SkippableFact]
        public void Should_translate_set_intersection()
        {
            RequireServer.Where(minimumVersion: "2.6.0");

            var result = Project(x => new { Result = x.C.E.I.Intersect(new[] { "it", "not in here" }) });

            result.Projection.Should().Be("{ Result: { \"$setIntersection\": [\"$C.E.I\", [\"it\", \"not in here\"] ] }, _id: 0 }");

            result.Value.Result.Should().Equal("it");
        }

        [SkippableFact]
        public void Should_translate_set_intersection_reversed()
        {
            RequireServer.Where(minimumVersion: "2.6.0");

            var result = Project(x => new { Result = new[] { "it", "not in here" }.Intersect(x.C.E.I) });

            result.Projection.Should().Be("{ Result: { \"$setIntersection\": [[\"it\", \"not in here\"], \"$C.E.I\"] }, _id: 0 }");

            result.Value.Result.Should().Equal("it");
        }

        [SkippableFact]
        public void Should_translate_set_is_subset()
        {
            RequireServer.Where(minimumVersion: "2.6.0");

            var result = Project(x => new { Result = x.L.IsSubsetOf(new[] { 1, 3, 5 }) });

            result.Projection.Should().Be("{ Result: { \"$setIsSubset\": [\"$L\", [1, 3, 5]] }, _id: 0 }");

            result.Value.Result.Should().BeTrue();
        }

        [SkippableFact]
        public void Should_translate_set_is_subset_reversed()
        {
            RequireServer.Where(minimumVersion: "2.6.0");
            var set = new HashSet<int>(new[] { 1, 3, 5 });

            var result = Project(x => new { Result = set.IsSubsetOf(x.L) });

            result.Projection.Should().Be("{ Result: { \"$setIsSubset\": [[1, 3, 5], \"$L\"] }, _id: 0 }");

            result.Value.Result.Should().BeTrue();
        }

        [SkippableFact]
        public void Should_translate_set_union()
        {
            RequireServer.Where(minimumVersion: "2.6.0");

            var result = Project(x => new { Result = x.C.E.I.Union(new[] { "it", "not in here" }) });

            result.Projection.Should().Be("{ Result: { \"$setUnion\": [\"$C.E.I\", [\"it\", \"not in here\"] ] }, _id: 0 }");

            result.Value.Result.Should().BeEquivalentTo("it", "icky", "not in here");
        }

        [SkippableFact]
        public void Should_translate_set_union_reversed()
        {
            RequireServer.Where(minimumVersion: "2.6.0");

            var result = Project(x => new { Result = new[] { "it", "not in here" }.Union(x.C.E.I) });

            result.Projection.Should().Be("{ Result: { \"$setUnion\": [[\"it\", \"not in here\"], \"$C.E.I\"] }, _id: 0 }");

            result.Value.Result.Should().BeEquivalentTo("it", "icky", "not in here");
        }

        [SkippableFact]
        public void Should_translate_sqrt()
        {
            RequireServer.Where(minimumVersion: "3.1.6");

            var result = Project(x => new { Result = Math.Sqrt(x.C.E.F) });

            result.Projection.Should().Be("{ Result: { \"$sqrt\": [\"$C.E.F\"] }, _id: 0 }");

            result.Value.Result.Should().BeApproximately(3.31662479, .0001);
        }

        [SkippableFact]
        public void Should_translate_trunc()
        {
            RequireServer.Where(minimumVersion: "3.1.6");

            var result = Project(x => new { Result = Math.Truncate(x.U) });

            result.Projection.Should().Be("{ Result: { \"$trunc\": \"$U\" }, _id: 0 }");

            result.Value.Result.Should().Be(1);
        }

        [SkippableFact]
        public void Should_translate_where_to_filter()
        {
            RequireServer.Where(minimumVersion: "3.1.3");

            var result = Project(x => new { Result = x.G.Where(c => c.E.F == 33) });

            result.Projection.Should().Be("{ Result: { \"$filter\": { \"input\": \"$G\", \"as\": \"c\", \"cond\": { \"$eq\": [\"$$c.E.F\", 33] } } }, _id: 0 }");

            result.Value.Result.Should().HaveCount(1);
            result.Value.Result.Single().D.Should().Be("Don't");
        }

        [SkippableFact]
        public void Should_translate_where_then_select_to_filter_then_map()
        {
            RequireServer.Where(minimumVersion: "3.1.3");

            var result = Project(x => new { Result = x.G.Where(c => c.E.F == 33).Select(c => c.D) });

            result.Projection.Should().Be("{ Result: { \"$map\": { \"input\": { \"$filter\": { \"input\": \"$G\", \"as\": \"c\", \"cond\": { \"$eq\": [\"$$c.E.F\", 33] } } }, \"as\": \"c\", \"in\": \"$$c.D\" } }, _id: 0 }");

            result.Value.Result.Should().HaveCount(1);
            result.Value.Result.Single().Should().Be("Don't");
        }

        [SkippableFact]
        public void Should_translate_select_then_where_to_map_then_filter()
        {
            RequireServer.Where(minimumVersion: "3.1.3");

            var result = Project(x => new { Result = x.G.Select(c => c.D).Where(c => c == "Don't") });

            result.Projection.Should().Be("{ Result: { \"$filter\": { \"input\": \"$G.D\", \"as\": \"c\", \"cond\": { \"$eq\": [\"$$c\", \"Don't\"] } } }, _id: 0 }");

            result.Value.Result.Should().HaveCount(1);
            result.Value.Result.Single().Should().Be("Don't");
        }

        [SkippableFact]
        public void Should_translate_select_with_an_anonymous_type_then_where_to_map_then_filter()
        {
            RequireServer.Where(minimumVersion: "3.1.3");

            var result = Project(x => new { Result = x.G.Select(c => new { c.D, c.E.F }).Where(c => c.F == 33) });

            result.Projection.Should().Be("{ Result: { \"$filter\": { \"input\": { \"$map\" : { \"input\": \"$G\", \"as\": \"c\", \"in\": { \"D\": \"$$c.D\", \"F\": \"$$c.E.F\" } } }, \"as\": \"c\", \"cond\": { \"$eq\": [\"$$c.F\", 33] } } }, _id: 0 }");

            result.Value.Result.Should().HaveCount(1);
            result.Value.Result.Single().D.Should().Be("Don't");
            result.Value.Result.Single().F.Should().Be(33);
        }

        [SkippableFact]
        public void Should_translate_stdDevPop()
        {
            RequireServer.Where(minimumVersion: "3.1.7");

            var result = Project(x => new { Result = x.M.StandardDeviationPopulation() });

            result.Projection.Should().Be("{ Result: { \"$stdDevPop\": \"$M\" }, _id: 0 }");

            result.Value.Result.Should().BeApproximately(1.247219128924647, .0001);
        }

        [SkippableFact]
        public void Should_translate_stdDevPop_with_selector()
        {
            RequireServer.Where(minimumVersion: "3.1.7");

            var result = Project(x => new { Result = x.G.StandardDeviationPopulation(g => g.E.F) });

            result.Projection.Should().Be("{ Result: { \"$stdDevPop\": \"$G.E.F\" }, _id: 0 }");

            result.Value.Result.Should().Be(11);
        }

        [SkippableFact]
        public void Should_translate_stdDevSamp()
        {
            RequireServer.Where(minimumVersion: "3.1.7");

            var result = Project(x => new { Result = x.M.StandardDeviationSample() });

            result.Projection.Should().Be("{ Result: { \"$stdDevSamp\": \"$M\" }, _id: 0 }");

            result.Value.Result.Should().BeApproximately(1.5275252316519468, .0001);
        }

        [SkippableFact]
        public void Should_translate_stdDevSamp_with_selector()
        {
            RequireServer.Where(minimumVersion: "3.1.7");

            var result = Project(x => new { Result = x.G.StandardDeviationSample(g => g.E.F) });

            result.Projection.Should().Be("{ Result: { \"$stdDevSamp\": \"$G.E.F\" }, _id: 0 }");

            result.Value.Result.Should().BeApproximately(15.556349186104045, .0001);
        }

        [Fact]
        public void Should_translate_string_equals()
        {
            var result = Project(x => new { Result = x.B.Equals("Balloon") });

            result.Projection.Should().Be("{ Result: { \"$eq\": [\"$B\", \"Balloon\"] }, _id: 0 }");

            result.Value.Result.Should().BeTrue();
        }
        
        [Fact]
        public void Should_translate_string_equals_using_comparison()
        {
            var result = Project(x => new { Result = x.B.Equals("Balloon", StringComparison.Ordinal) });

            result.Projection.Should().Be("{ Result: { \"$eq\": [\"$B\", \"Balloon\"] }, _id: 0 }");

            result.Value.Result.Should().BeTrue();
        }
        
        [Fact]
        public void Should_translate_string_case_insensitive_equals()
        {
            var result = Project(x => new { Result = x.B.Equals("balloon", StringComparison.OrdinalIgnoreCase) });

            result.Projection.Should().Be("{ Result: { \"$eq\": [{ \"$strcasecmp\": [\"$B\", \"balloon\"] }, 0] }, _id: 0 }");

            result.Value.Result.Should().BeTrue();
        }

        [Theory]
        [InlineData(StringComparison.CurrentCulture)]
        [InlineData(StringComparison.CurrentCultureIgnoreCase)]
#if NET45
        [InlineData(StringComparison.InvariantCulture)]
        [InlineData(StringComparison.InvariantCultureIgnoreCase)]
#endif
        public void Should_throw_for_a_not_supported_string_comparison_type(StringComparison comparison)
        {
            Action act = () => Project(x => new { Result = x.B.Equals("balloon", comparison) });

            act.ShouldThrow<NotSupportedException>();
        }

        [Fact]
        public void Should_translate_static_string_equals()
        {
            var result = Project(x => new { Result = string.Equals(x.B, "Balloon") });

            result.Projection.Should().Be("{ Result: { \"$eq\": [\"$B\", \"Balloon\"] }, _id: 0 }");

            result.Value.Result.Should().BeTrue();
        }

        [Fact]
        public void Should_translate_static_string_equals_using_comparison()
        {
            var result = Project(x => new { Result = string.Equals(x.B, "Balloon", StringComparison.Ordinal) });

            result.Projection.Should().Be("{ Result: { \"$eq\": [\"$B\", \"Balloon\"] }, _id: 0 }");

            result.Value.Result.Should().BeTrue();
        }

        [Fact]
        public void Should_translate_static_string_case_insensitive_equals()
        {
            var result = Project(x => new { Result = string.Equals(x.B, "balloon", StringComparison.OrdinalIgnoreCase) });

            result.Projection.Should().Be("{ Result: { \"$eq\": [{ \"$strcasecmp\": [\"$B\", \"balloon\"] }, 0] }, _id: 0 }");

            result.Value.Result.Should().BeTrue();
        }

        [Theory]
        [InlineData(StringComparison.CurrentCulture)]
        [InlineData(StringComparison.CurrentCultureIgnoreCase)]
#if NET45
        [InlineData(StringComparison.InvariantCulture)]
        [InlineData(StringComparison.InvariantCultureIgnoreCase)]
#endif
        public void Should_throw_for_a_not_supported_static_string_comparison_type(StringComparison comparison)
        {
            Action act = () => Project(x => new { Result = string.Equals(x.B, "balloon", comparison) });

            act.ShouldThrow<NotSupportedException>();
        }

        [Fact]
        public void Should_translate_string_is_null_or_empty()
        {
            var result = Project(x => new { Result = string.IsNullOrEmpty(x.B) });

            result.Projection.Should().Be("{ Result: { \"$or\": [{ $eq: [\"$B\", null] }, { $eq: [\"$B\", \"\"] } ] }, _id: 0 }");

            result.Value.Result.Should().BeFalse();
        }
        
        [Fact]
        public void Should_translate_string_concat_with_2_strings()
        {
            var result = Project(x => new { Result = string.Concat(x.A, x.B) });

            result.Projection.Should().Be("{ Result: { \"$concat\": [\"$A\", \"$B\"] }, _id: 0 }");

            result.Value.Result.Should().Be("AwesomeBalloon");
        }

        [Fact]
        public void Should_translate_string_concat_with_3_strings()
        {
            var result = Project(x => new { Result = string.Concat(x.B, "&", x.B) });

            result.Projection.Should().Be("{ Result: { \"$concat\": [\"$B\", \"&\", \"$B\"] }, _id: 0 }");

            result.Value.Result.Should().Be("Balloon&Balloon");
        }

        [Fact]
        public void Should_translate_string_concat_with_4_strings()
        {
            var result = Project(x => new { Result = string.Concat(x.A, " ", x.B, "!") });

            result.Projection.Should().Be("{ Result: { \"$concat\": [\"$A\", \" \", \"$B\", \"!\"] }, _id: 0 }");

            result.Value.Result.Should().Be("Awesome Balloon!");
        }

        [Fact]
        public void Should_translate_string_concat_with_params_string_array()
        {
            var result = Project(x => new { Result = string.Concat(x.A, "+", x.B, "+", x.C.D) });

            result.Projection.Should().Be("{ Result: { \"$concat\": [\"$A\", \"+\", \"$B\", \"+\", \"$C.D\"] }, _id: 0 }");

            result.Value.Result.Should().Be("Awesome+Balloon+Dexter");
        }

        [Fact]
        public void Should_throw_for_a_not_supported_string_concat_with_enumerable_of_string()
        {
            Action act = () => Project(x => new { Result = string.Concat(x.C.E.I) });

            act.ShouldThrow<NotSupportedException>();
        }
        
        [Fact]
        public void Should_throw_for_a_not_supported_string_concat_with_1_object()
        {
            Action act = () => Project(x => new { Result = string.Concat((object)x.A) });

            act.ShouldThrow<NotSupportedException>();
        }

        [Fact]
        public void Should_throw_for_a_not_supported_string_concat_with_2_objects()
        {
            Action act = () => Project(x => new { Result = string.Concat(x.A, x.C) });

            act.ShouldThrow<NotSupportedException>();
        }
        
        [Fact]
        public void Should_throw_for_a_not_supported_string_concat_with_3_objects()
        {
            Action act = () => Project(x => new { Result = string.Concat(x.C, x.J, x.K) });

            act.ShouldThrow<NotSupportedException>();
        }

        [Fact]
        public void Should_throw_for_a_not_supported_string_concat_with_4_objects()
        {
            Action act = () => Project(x => new { Result = string.Concat(x.C, x.B, x.J, x.K) });

            act.ShouldThrow<NotSupportedException>();
        }

        [Fact]
        public void Should_throw_for_a_not_supported_string_concat_with_params_object_array()
        {
            Action act = () => Project(x => new { Result = string.Concat(x.A, 1, x.C, false, x.G) });

            act.ShouldThrow<NotSupportedException>();
        }

        [Fact]
        public void Should_throw_for_a_not_supported_string_concat_with_generic_enumerable()
        {
            Action act = () => Project(x => new { Result = string.Concat(x.G) });

            act.ShouldThrow<NotSupportedException>();
        }
        
        [Fact]
        public void Should_translate_substring()
        {
            var result = Project(x => new { Result = x.B.Substring(3, 20) });

            result.Projection.Should().Be("{ Result: { \"$substr\": [\"$B\",3, 20] }, _id: 0 }");

            result.Value.Result.Should().Be("loon");
        }

        [Fact]
        public void Should_translate_substring_with_1_arg()
        {
            var result = Project(x => new { Result = x.B.Substring(3) });

            result.Projection.Should().Be("{ Result: { \"$substr\": [\"$B\", 3, -1] }, _id: 0 }");

            result.Value.Result.Should().Be("loon");
        }

        [Fact]
        public void Should_translate_subtract()
        {
            var result = Project(x => new { Result = x.C.E.F - x.C.E.H });

            result.Projection.Should().Be("{ Result: { \"$subtract\": [\"$C.E.F\", \"$C.E.H\"] }, _id: 0 }");

            result.Value.Result.Should().Be(-11);
        }

        [Fact]
        public void Should_translate_subtract_3_numbers()
        {
            var result = Project(x => new { Result = x.Id - x.C.E.F - x.C.E.H });

            result.Projection.Should().Be("{ Result: { \"$subtract\": [{ \"$subtract\": [\"$_id\", \"$C.E.F\"] }, \"$C.E.H\"] }, _id: 0 }");

            result.Value.Result.Should().Be(-23);
        }

        [SkippableFact]
        public void Should_translate_slice_with_2_arguments()
        {
            RequireServer.Where(minimumVersion: "3.1.7");

            var result = Project(x => new { Result = x.M.Take(2) });

            result.Projection.Should().Be("{ Result: { \"$slice\": [\"$M\", 2] }, _id: 0 }");

            result.Value.Result.Should().BeEquivalentTo(2, 4);
        }

        [SkippableFact]
        public void Should_translate_slice_with_3_arguments()
        {
            RequireServer.Where(minimumVersion: "3.1.7");

            var result = Project(x => new { Result = x.M.Skip(1).Take(2) });

            result.Projection.Should().Be("{ Result: { \"$slice\": [\"$M\", 1, 2] }, _id: 0 }");

            result.Value.Result.Should().BeEquivalentTo(4, 5);
        }

        [SkippableFact]
        public void Should_translate_sum()
        {
            RequireServer.Where(minimumVersion: "3.1.7");

            var result = Project(x => new { Result = x.M.Sum() });

            result.Projection.Should().Be("{ Result: { \"$sum\": \"$M\" }, _id: 0 }");

            result.Value.Result.Should().Be(11);
        }

        [SkippableFact]
        public void Should_translate_sum_with_selector()
        {
            RequireServer.Where(minimumVersion: "3.1.7");

            var result = Project(x => new { Result = x.G.Sum(g => g.E.F) });

            result.Projection.Should().Be("{ Result: { \"$sum\": \"$G.E.F\" }, _id: 0 }");

            result.Value.Result.Should().Be(88);
        }

        [Fact]
        public void Should_translate_to_lower()
        {
            var result = Project(x => new { Result = x.B.ToLower() });

            result.Projection.Should().Be("{ Result: { \"$toLower\": \"$B\" }, _id: 0 }");

            result.Value.Result.Should().Be("balloon");
        }

        [Fact]
        public void Should_translate_to_lower_invariant()
        {
            var result = Project(x => new { Result = x.B.ToLowerInvariant() });

            result.Projection.Should().Be("{ Result: { \"$toLower\": \"$B\" }, _id: 0 }");

            result.Value.Result.Should().Be("balloon");
        }

        [Fact]
        public void Should_translate_to_upper()
        {
            var result = Project(x => new { Result = x.B.ToUpper() });

            result.Projection.Should().Be("{ Result: { \"$toUpper\": \"$B\" }, _id: 0 }");

            result.Value.Result.Should().Be("BALLOON");
        }

        [Fact]
        public void Should_translate_to_upper_invariant()
        {
            var result = Project(x => new { Result = x.B.ToUpperInvariant() });

            result.Projection.Should().Be("{ Result: { \"$toUpper\": \"$B\" }, _id: 0 }");

            result.Value.Result.Should().Be("BALLOON");
        }

        [Fact]
        public void Should_translate_year()
        {
            var result = Project(x => new { Result = x.J.Year });

            result.Projection.Should().Be("{ Result: { \"$year\": \"$J\" }, _id: 0 }");

            result.Value.Result.Should().Be(2012);
        }

        [Fact]
        public void Should_translate_array_projection()
        {
            var result = Project(x => new { Result = x.G.Select(y => y.E.F) });

            result.Projection.Should().Be("{ Result: \"$G.E.F\", _id: 0 }");

            result.Value.Result.Should().BeEquivalentTo(33, 55);
        }

        [Fact]
        public void Should_translate_a_derived_class_projection()
        {
            var result = Project(x => new DerivedRootView { Property = x.A, DerivedProperty = x.B });

            result.Projection.Should().Be("{ Property: \"$A\", DerivedProperty: \"$B\", _id: 0 }");

            result.Value.Property.Should().Be("Awesome");
            result.Value.DerivedProperty.Should().Be("Balloon");
        }

        [SkippableFact]
        public void Should_translate_array_projection_complex()
        {
            RequireServer.Where(minimumVersion: "2.6.0");

            var result = Project(x => new { Result = x.G.Select(y => new { y.E.F, y.E.H }) });

            result.Projection.Should().Be("{ Result : { $map: { input: \"$G\", as: \"y\", in: { F : \"$$y.E.F\", H : \"$$y.E.H\" } } }, _id : 0 }");

            result.Value.Result.First().F.Should().Be(33);
            result.Value.Result.First().H.Should().Be(44);
            result.Value.Result.Last().F.Should().Be(55);
            result.Value.Result.Last().H.Should().Be(66);
        }

        private ProjectedResult<TResult> Project<TResult>(Expression<Func<Root, TResult>> projector)
        {
            var serializer = BsonSerializer.SerializerRegistry.GetSerializer<Root>();
            var projectionInfo = AggregateProjectTranslator.Translate(projector, serializer, BsonSerializer.SerializerRegistry);

            var pipelineOperator = new BsonDocument("$project", projectionInfo.Document);
            var result = __collection.Aggregate()
                .Project(new BsonDocumentProjectionDefinition<Root, TResult>(projectionInfo.Document, projectionInfo.ProjectionSerializer))
                .First();

            return new ProjectedResult<TResult>
            {
                Projection = projectionInfo.Document,
                Value = result
            };
        }

        private class ProjectedResult<T>
        {
            public BsonDocument Projection;
            public T Value;
        }
    }
}
