﻿/* Copyright 2010-2015 MongoDB Inc.
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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Driver.Linq;
using NUnit.Framework;

namespace MongoDB.Driver.Tests.Linq
{
    [TestFixture]
    public class MongoQueryableTests : IntegrationTestBase
    {
        [Test]
        public void Any()
        {
            var result = CreateQuery().Any();

            result.Should().BeTrue();
        }

        [Test]
        public void Any_with_predicate()
        {
            var result = CreateQuery().Any(x => x.C.E.F == 234124);
            result.Should().BeFalse();

            result = CreateQuery().Any(x => x.C.E.F == 11);
            result.Should().BeTrue();
        }

        [Test]
        public async Task AnyAsync()
        {
            var result = await CreateQuery().AnyAsync();

            result.Should().BeTrue();
        }

        [Test]
        public async Task AnyAsync_with_predicate()
        {
            var result = await CreateQuery().AnyAsync(x => x.C.E.F == 234124);
            result.Should().BeFalse();

            result = await CreateQuery().AnyAsync(x => x.C.E.F == 11);
            result.Should().BeTrue();
        }

        [Test]
        public void Average()
        {
            var result = CreateQuery().Select(x => x.C.E.F + 1).Average();

            result.Should().Be(62);
        }

        [Test]
        public void Average_with_select_and_where()
        {
            var result = CreateQuery()
                .Select(x => x.C.E.F)
                .Where(x => x > 20)
                .Average();

            result.Should().Be(111);
        }

        [Test]
        public void Average_with_selector()
        {
            var result = CreateQuery().Average(x => x.C.E.F + 1);

            result.Should().Be(62);
        }

        [Test]
        public async Task AverageAsync()
        {
            var result = await CreateQuery().Select(x => x.C.E.F).AverageAsync();

            result.Should().Be(61);
        }

        [Test]
        public async Task AverageAsync_with_selector()
        {
            var result = await CreateQuery().AverageAsync(x => x.C.E.F);

            result.Should().Be(61);
        }

        [Test]
        public void Count()
        {
            var result = CreateQuery().Count();

            result.Should().Be(2);
        }

        [Test]
        public void Count_with_predicate()
        {
            var result = CreateQuery().Count(x => x.C.E.F == 11);

            result.Should().Be(1);
        }

        [Test]
        public void Count_with_no_matches()
        {
            var result = CreateQuery().Count(x => x.C.E.F == 13151235);

            result.Should().Be(0);
        }

        [Test]
        public async Task CountAsync()
        {
            var result = await CreateQuery().CountAsync();

            result.Should().Be(2);
        }

        [Test]
        public async Task CountAsync_with_predicate()
        {
            var result = await CreateQuery().CountAsync(x => x.C.E.F == 11);

            result.Should().Be(1);
        }

        [Test]
        public async Task CountAsync_with_no_matches()
        {
            var result = await CreateQuery().CountAsync(x => x.C.E.F == 123412523);

            result.Should().Be(0);
        }

        [Test]
        [RequiresServer(MinimumVersion = "2.6.0")]
        public void Distinct_followed_by_where()
        {
            var query = CreateQuery()
                .Distinct()
                .Where(x => x.A == "Awesome");

            Assert(query,
                1,
                "{ $group: { _id: '$$ROOT' } }",
                "{ $match: { '_id.A': 'Awesome' } }");
        }

        [Test]
        public void First()
        {
            var result = CreateQuery().Select(x => x.C.E.F).First();

            result.Should().Be(11);
        }

        [Test]
        public void First_with_predicate()
        {
            var result = CreateQuery().Select(x => x.C.E.F).First(x => x == 11);

            result.Should().Be(11);
        }

        [Test]
        public async Task FirstAsync()
        {
            var result = await CreateQuery().Select(x => x.C.E.F).FirstAsync();

            result.Should().Be(11);
        }

        [Test]
        public async Task FirstAsync_with_predicate()
        {
            var result = await CreateQuery().Select(x => x.C.E.F).FirstAsync(x => x == 11);

            result.Should().Be(11);
        }

        [Test]
        public void FirstOrDefault()
        {
            var result = CreateQuery().Select(x => x.C.E.F).FirstOrDefault();

            result.Should().Be(11);
        }

        [Test]
        public void FirstOrDefault_with_predicate()
        {
            var result = CreateQuery().Select(x => x.C.E.F).FirstOrDefault(x => x == 11);

            result.Should().Be(11);
        }

        [Test]
        public async Task FirstOrDefaultAsync()
        {
            var result = await CreateQuery().Select(x => x.C.E.F).FirstOrDefaultAsync();

            result.Should().Be(11);
        }

        [Test]
        public async Task FirstOrDefaultAsync_with_predicate()
        {
            var result = await CreateQuery().Select(x => x.C.E.F).FirstOrDefaultAsync(x => x == 11);

            result.Should().Be(11);
        }

        [Test]
        public void Enumerable_foreach()
        {
            var query = from x in CreateQuery()
                        select x.M;

            int sum = 0;

            foreach (var item in query)
            {
                sum += item.Sum();
            }

            foreach (var item in query)
            {
                sum += item.Sum();
            }

            sum.Should().Be(50);
        }

        [Test]
        public void GroupBy_method()
        {
            var query = CreateQuery()
                .GroupBy(x => x.A);

            Assert(query,
                2,
                "{ $group: { _id: '$A' } }");
        }

        [Test]
        public void Group_method_using_select()
        {
            var query = CreateQuery()
                .GroupBy(x => x.A)
                .Select(x => new { A = x.Key, Count = x.Count(), Min = x.Min(y => y.U) });

            Assert(query,
                2,
                "{ $group: { _id: '$A', __agg0: { $sum: 1 }, __agg1: { $min: '$U' } } }",
                "{ $project: { A: '$_id', Count: '$__agg0', Min: '$__agg1', _id: 0 } }");
        }

        [Test]
        public void GroupBy_groupby_method()
        {
            var query = CreateQuery()
                .GroupBy(x => x.A)
                .GroupBy(g => g.First().B);

            Assert(query,
                2,
                "{ $group: { _id: '$A', __agg0: { $first: '$B'} } }",
                "{ $group: { _id: '$__agg0' } }");
        }

        [Test]
        public void GroupBy_select_anonymous_type_method()
        {
            var query = CreateQuery()
                .GroupBy(x => x.A)
                .Select(g => new { Key = g.Key, FirstB = g.First().B });

            Assert(query,
                2,
                "{ $group: { _id: '$A', __agg0: { $first: '$B'} } }",
                "{ $project: { Key: '$_id', FirstB: '$__agg0', _id: 0 } }");
        }
#if !MONO
        [Test]
        public void GroupBy_select_anonymous_type_syntax()
        {
            var query = from p in CreateQuery()
                        group p by p.A into g
                        select new { g.Key, FirstB = g.First().B };

            Assert(query,
                2,
                "{ $group: { _id: '$A', __agg0: { $first: '$B'} } }",
                "{ $project: { Key: '$_id', FirstB: '$__agg0', _id: 0 } }");
        }
#endif
        [Test]
        public void GroupBy_where_method()
        {
            var query = CreateQuery()
                .GroupBy(x => x.A)
                .Where(g => g.Key == "Awesome");

            Assert(query,
                1,
                "{ $group: { _id: '$A' } }",
                "{ $match: { _id: 'Awesome' } }");
        }

        [Test]
        public void GroupBy_where_with_accumulator_method()
        {
            var query = CreateQuery()
                .GroupBy(x => x.A)
                .Where(g => g.First().B == "Balloon");

            Assert(query,
                1,
                "{ $group: { _id: '$A', __agg0: { $first: '$B' } } }",
                "{ $match: { __agg0: 'Balloon' } }");
        }

        [Test]
        public void GroupBy_where_select_anonymous_type_with_duplicate_accumulators_method()
        {
            var query = CreateQuery()
                .GroupBy(x => x.A)
                .Where(g => g.First().B == "Balloon")
                .Select(x => new { x.Key, FirstB = x.First().B });

            Assert(query,
                1,
                "{ $group: { _id: '$A', __agg0: { $first: '$B'} } }",
                "{ $match: { __agg0: 'Balloon' } }",
                "{ $project: { Key: '$_id', FirstB: '$__agg0', _id: 0 } }");
        }

#if !MONO
        [Test]
        public void GroupBy_where_select_anonymous_type_with_duplicate_accumulators_syntax()
        {
            var query = from p in CreateQuery()
                        group p by p.A into g
                        where g.First().B == "Balloon"
                        select new { g.Key, FirstB = g.First().B };

            Assert(query,
                1,
                "{ $group: { _id: '$A', __agg0: { $first: '$B'} } }",
                "{ $match: { __agg0: 'Balloon' } }",
                "{ $project: { Key: '$_id', FirstB: '$__agg0', _id: 0 } }");
        }
#endif
        [Test]
        public void GroupBy_with_resultSelector_anonymous_type_method()
        {
            var query = CreateQuery()
                .GroupBy(x => x.A, (k, s) => new { Key = k, FirstB = s.First().B });

            Assert(query,
                2,
                "{ $group: { _id: '$A', FirstB: { $first: '$B'} } }");
        }

        [Test]
        [RequiresServer(MinimumVersion = "3.1.9", Modules = "enterprise")]
        public void GroupJoin_method()
        {
            var query = CreateQuery()
                .GroupJoin(
                    CreateOtherQuery(),
                    p => p.Id,
                    o => o.Id,
                    (p, o) => new { p, o });

            Assert(query,
                2,
                "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: '_id', as: 'o' } }");
        }

        [Test]
        [RequiresServer(MinimumVersion = "3.1.9", Modules = "enterprise")]
        public void GroupJoinForeignField_method()
        {
            var query = CreateQuery()
                .GroupJoin(
                    CreateOtherQuery(),
                    p => p.Id,
                    o => o.CEF,
                    (p, o) => new { p, o });

            Assert(query,
                2,
                "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: 'CEF', as: 'o' } }");
        }

        [Test]
        [RequiresServer(MinimumVersion = "3.1.9", Modules = "enterprise")]
        public void GroupJoin_syntax()
        {
            var query = from p in CreateQuery()
                        join o in CreateOtherQuery() on p.Id equals o.Id into joined
                        select new { A = p.A, SumCEF = joined.Sum(x => x.CEF) };

            Assert(query,
                2,
                "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: '_id', as: 'joined' } }",
                "{ $project: { A: '$A', SumCEF: { $sum: '$joined.CEF' }, _id: 0 } }");
        }

        [Test]
        [RequiresServer(MinimumVersion = "3.1.9", Modules = "enterprise")]
        public void GroupJoin_syntax_with_a_transparent_identifier()
        {
            var query = from p in CreateQuery()
                        join o in CreateOtherQuery() on p.Id equals o.Id into joined
                        orderby p.B
                        select new { A = p.A, Joined = joined };

            Assert(query,
                2,
                "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: '_id', as: 'joined' } }",
                "{ $sort: { B: 1 } }",
                "{ $project: { A: '$A', Joined: '$joined', _id: 0 } }");
        }

        [Test]
        [RequiresServer(MinimumVersion = "3.1.9", Modules = "enterprise")]
        public void GroupJoin_syntax_with_select_many()
        {
            var query = from p in CreateQuery()
                        join o in _otherCollection on p.Id equals o.Id into joined
                        from subo in joined
                        select new { A = p.A, CEF = subo.CEF };

            Assert(query,
                1,
                "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: '_id', as: 'joined' } }",
                "{ $unwind: '$joined' }",
                "{ $project: { A: '$A', CEF: '$joined.CEF', _id: 0 } }");
        }

        [Test]
        [RequiresServer(MinimumVersion = "3.1.9", Modules = "enterprise")]
        public void GroupJoin_syntax_with_select_many_and_DefaultIfEmpty()
        {
            var query = from p in CreateQuery()
                        join o in _otherCollection on p.Id equals o.Id into joined
                        from subo in joined.DefaultIfEmpty()
                        select new { A = p.A, CEF = (int?)subo.CEF ?? null };

            Assert(query,
                2,
                "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: '_id', as: 'joined' } }",
                "{ $unwind: { path: '$joined', preserveNullAndEmptyArrays: true } }",
                "{ $project: { A: '$A', CEF: { $ifNull: ['$joined.CEF', null] }, _id: 0 } }");
        }

        [Test]
        [RequiresServer(MinimumVersion = "3.1.9", Modules = "enterprise")]
        public void Join_method()
        {
            var query = CreateQuery()
                .Join(
                    CreateOtherQuery(),
                    p => p.Id,
                    o => o.Id,
                    (p, o) => new { p, o });

            Assert(query,
                1,
                "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: '_id', as: 'o' } }",
                "{ $unwind: '$o' }");
        }

        [Test]
        [RequiresServer(MinimumVersion = "3.1.9", Modules = "enterprise")]
        public void Join_syntax()
        {
            var query = from p in CreateQuery()
                        join o in CreateOtherQuery() on p.Id equals o.Id
                        select new { A = p.A, CEF = o.CEF };

            Assert(query,
                1,
                "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: '_id', as: 'o' } }",
                "{ $unwind: '$o' }",
                "{ $project: { A: '$A', CEF: '$o.CEF', _id: 0 } }");
        }

        [Test]
        [RequiresServer(MinimumVersion = "3.1.9", Modules = "enterprise")]
        public void Join_syntax_with_a_transparent_identifier()
        {
            var query = from p in CreateQuery()
                        join o in CreateOtherQuery() on p.Id equals o.Id
                        orderby p.B, o.Id
                        select new { A = p.A, CEF = o.CEF };

            Assert(query,
                1,
                "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: '_id', as: 'o' } }",
                "{ $unwind: '$o' }",
                "{ $sort: { B: 1, 'o._id': 1 } }",
                "{ $project: { A: '$A', CEF: '$o.CEF', _id: 0 } }");
        }

        [Test]
        public void LongCount()
        {
            var result = CreateQuery().LongCount();

            result.Should().Be(2);
        }

        [Test]
        public void LongCount_with_predicate()
        {
            var result = CreateQuery().LongCount(x => x.C.E.F == 11);

            result.Should().Be(1);
        }

        [Test]
        public void LongCount_with_no_results()
        {
            var result = CreateQuery().LongCount(x => x.C.E.F == 123452135);

            result.Should().Be(0);
        }

        [Test]
        public async Task LongCountAsync()
        {
            var result = await CreateQuery().LongCountAsync();

            result.Should().Be(2);
        }

        [Test]
        public async Task LongCountAsync_with_predicate()
        {
            var result = await CreateQuery().LongCountAsync(x => x.C.E.F == 11);

            result.Should().Be(1);
        }

        [Test]
        public async Task LongCountAsync_with_no_results()
        {
            var result = await CreateQuery().LongCountAsync(x => x.C.E.F == 12351235);

            result.Should().Be(0);
        }

        [Test]
        public void Max()
        {
            var result = CreateQuery().Select(x => x.C.E.F).Max();

            result.Should().Be(111);
        }

        [Test]
        public void Max_with_selector()
        {
            var result = CreateQuery().Max(x => x.C.E.F);

            result.Should().Be(111);
        }

        [Test]
        public async Task MaxAsync()
        {
            var result = await CreateQuery().Select(x => x.C.E.F).MaxAsync();

            result.Should().Be(111);
        }

        [Test]
        public async Task MaxAsync_with_selector()
        {
            var result = await CreateQuery().MaxAsync(x => x.C.E.F);

            result.Should().Be(111);
        }

        [Test]
        public void Min()
        {
            var result = CreateQuery().Select(x => x.C.E.F).Min();

            result.Should().Be(11);
        }

        [Test]
        public void Min_with_selector()
        {
            var result = CreateQuery().Min(x => x.C.E.F);

            result.Should().Be(11);
        }

        [Test]
        public async Task MinAsync()
        {
            var result = await CreateQuery().Select(x => x.C.E.F).MinAsync();

            result.Should().Be(11);
        }

        [Test]
        public async Task MinAsync_with_selector()
        {
            var result = await CreateQuery().MinAsync(x => x.C.E.F);

            result.Should().Be(11);
        }

        [Test]
        public void OfType()
        {
            var query = CreateQuery().OfType<RootDescended>();

            Assert(query,
                1,
                "{ $match: { _t: 'RootDescended' } }");
        }

        [Test]
        public void OfType_with_a_field()
        {
            var query = CreateQuery()
                .Select(x => x.C.E)
                .OfType<V>()
                .Where(v => v.W == 1111);

            Assert(query,
                1,
                "{ $project: { E: '$C.E', _id: 0 } }",
                "{ $match: { 'E._t': 'V' } }",
                "{ $match: { 'E.W': 1111 } }");
        }

        [Test]
        public void OrderBy_method()
        {
            var query = CreateQuery()
                .OrderBy(x => x.A);

            Assert(query,
                2,
                "{ $sort: { A: 1 } }");
        }

        [Test]
        public void OrderBy_syntax()
        {
            var query = from x in CreateQuery()
                        orderby x.A
                        select x;

            Assert(query,
                2,
                "{ $sort: { A: 1 } }");
        }

        [Test]
        public void OrderByDescending_method()
        {
            var query = CreateQuery()
                .OrderByDescending(x => x.A);

            Assert(query,
                2,
                "{ $sort: { A: -1 } }");
        }

        [Test]
        public void OrderByDescending_syntax()
        {
            var query = from x in CreateQuery()
                        orderby x.A descending
                        select x;

            Assert(query,
                2,
                "{ $sort: { A: -1 } }");
        }

        [Test]
        public void OrderBy_ThenBy_ThenByDescending_method()
        {
            var query = CreateQuery()
                .OrderBy(x => x.A)
                .ThenBy(x => x.B)
                .ThenByDescending(x => x.C);

            Assert(query,
                2,
                "{ $sort: { A: 1, B: 1, C: -1 } }");
        }

        [Test]
        public void OrderBy_ThenBy_ThenByDescending_syntax()
        {
            var query = from x in CreateQuery()
                        orderby x.A, x.B, x.C descending
                        select x;

            Assert(query,
                2,
                "{ $sort: { A: 1, B: 1, C: -1 } }");
        }

        [Test]
        public void OrderBy_ThenBy_ThenByDescending_with_redundant_fields_method()
        {
            var query = CreateQuery()
                .OrderBy(x => x.A)
                .ThenBy(x => x.B)
                .ThenBy(x => x.A);

            Action act = () => query.ToList();
            act.ShouldThrow<NotSupportedException>();
        }

        [Test]
        public void OrderBy_ThenBy_ThenByDescending_with_redundant_fields_in_different_directions_method()
        {
            var query = CreateQuery()
                .OrderBy(x => x.A)
                .ThenBy(x => x.B)
                .ThenByDescending(x => x.A);

            Action act = () => query.ToList();
            act.ShouldThrow<NotSupportedException>();
        }

        [Test]
        [RequiresServer(MinimumVersion = "3.1.9")]
        public void Sample()
        {
            var query = CreateQuery().Sample(100);

            Assert(query,
                2,
                "{ $sample: { size: 100 } }");
        }

        [Test]
        public void Select_identity()
        {
            var query = CreateQuery().Select(x => x);

            Assert(query, 2);
        }

        [Test]
        public void Select_method_computed_scalar_followed_by_distinct_followed_by_where()
        {
            var query = CreateQuery()
                .Select(x => x.A + " " + x.B)
                .Distinct()
                .Where(x => x == "Awesome Balloon");

            Assert(query,
                1,
                "{ $group: { _id: { $concat: ['$A', ' ', '$B'] } } }",
                "{ $match: { _id: 'Awesome Balloon' } }");
        }

        [Test]
        public void Select_method_computed_scalar_followed_by_where()
        {
            var query = CreateQuery()
                .Select(x => x.A + " " + x.B)
                .Where(x => x == "Awesome Balloon");

            Assert(query,
                1,
                "{ $project: { __fld0: { $concat: ['$A', ' ', '$B'] }, _id: 0 } }",
                "{ $match: { __fld0: 'Awesome Balloon' } }");
        }

        [Test]
        [RequiresServer(MinimumVersion = "2.6.0")]
        public void Select_method_with_predicated_any()
        {
            var query = CreateQuery()
                .Select(x => x.G.Any(g => g.D == "Don't"));

            Assert(query,
                2,
                "{ $project: { __fld0: { $anyElementTrue: { $map: { input: '$G', as: 'g', 'in': { $eq: ['$$g.D', \"Don't\"] } } } }, _id: 0 } }");
        }

        [Test]
        public void Select_anonymous_type_where_method()
        {
            var query = CreateQuery()
                .Select(x => new { Yeah = x.A })
                .Where(x => x.Yeah == "Awesome");

            Assert(query,
                1,
                "{ $project: { Yeah: '$A', _id: 0 } }",
                "{ $match: { Yeah: 'Awesome' } }");
        }

        [Test]
        public void Select_scalar_where_method()
        {
            var query = CreateQuery()
                .Select(x => x.A)
                .Where(x => x == "Awesome");

            Assert(query,
                1,
                "{ $project: { A: '$A', _id: 0 } }",
                "{ $match: { A: 'Awesome' } }");
        }

        [Test]
        public void Select_anonymous_type_method()
        {
            var query = CreateQuery().Select(x => new { Yeah = x.A });

            Assert(query,
                2,
                "{ $project: { Yeah: '$A', _id: 0 } }");
        }

        [Test]
        public void Select_anonymous_type_syntax()
        {
            var query = from x in CreateQuery()
                        select new { Yeah = x.A };

            Assert(query,
                2,
                "{ $project: { Yeah: '$A', _id: 0 } }");
        }

        [Test]
        public void Select_method_scalar()
        {
            var query = CreateQuery().Select(x => x.A);

            Assert(query,
                2,
                "{ $project: { A: '$A', _id: 0 } }");
        }

        [Test]
        public void Select_syntax_scalar()
        {
            var query = from x in CreateQuery()
                        select x.A;

            Assert(query,
                2,
                "{ $project: { A: '$A', _id: 0 } }");
        }

        [Test]
        public void Select_method_computed_scalar()
        {
            var query = CreateQuery().Select(x => x.A + " " + x.B);

            Assert(query,
                2,
                "{ $project: { __fld0: { $concat: ['$A', ' ', '$B'] }, _id: 0 } }");
        }

        [Test]
        public void Select_syntax_computed_scalar()
        {
            var query = from x in CreateQuery()
                        select x.A + " " + x.B;

            Assert(query,
                2,
                "{ $project: { __fld0: { $concat: ['$A', ' ', '$B'] }, _id: 0 } }");
        }

        [Test]
        public void Select_method_array()
        {
            var query = CreateQuery().Select(x => x.M);

            Assert(query,
                2,
                "{ $project: { M: '$M', _id: 0 } }");
        }

        [Test]
        public void Select_syntax_array()
        {
            var query = from x in CreateQuery()
                        select x.M;

            Assert(query,
                2,
                "{ $project: { M: '$M', _id: 0 } }");
        }

        [Test]
        [RequiresServer(MinimumVersion = "3.1.9")]
        public void Select_method_array_index()
        {
            var query = CreateQuery().Select(x => x.M[0]);

            Assert(query,
                2,
                "{ $project: { __fld0: { $arrayElemAt: ['$M', 0] }, _id: 0 } }");
        }

        [Test]
        [RequiresServer(MinimumVersion = "3.1.9")]
        public void Select_syntax_array_index()
        {
            var query = from x in CreateQuery()
                        select x.M[0];

            Assert(query,
                2,
                "{ $project: { __fld0: { $arrayElemAt: ['$M', 0] }, _id: 0 } }");
        }

        [Test]
        [RequiresServer(MinimumVersion = "3.1.9")]
        public void Select_method_embedded_pipeline()
        {
            var query = CreateQuery().Select(x => x.M.First());

            Assert(query,
                2,
                "{ $project: { __fld0: { $arrayElemAt: ['$M', 0] }, _id: 0 } }");
        }

        [Test]
        [RequiresServer(MinimumVersion = "2.6.0")]
        public void Select_method_computed_array()
        {
            var query = CreateQuery()
                .Select(x => x.M.Select(i => i + 1));

            Assert(query,
                2,
                "{ $project: { __fld0: { $map: { input: '$M', as: 'i', in: { $add: ['$$i', 1] } } }, _id: 0 } }");
        }

        [Test]
        [RequiresServer(MinimumVersion = "2.6.0")]
        public void Select_syntax_computed_array()
        {
            var query = from x in CreateQuery()
                        select x.M.Select(i => i + 1);

            Assert(query,
                2,
                "{ $project: { __fld0: { $map: { input: '$M', as: 'i', in: { $add: ['$$i', 1] } } }, _id: 0 } }");
        }

        [Test]
        public void SelectMany_with_only_resultSelector()
        {
            var query = CreateQuery()
                .SelectMany(x => x.G);

            Assert(query,
                4,
                "{ $unwind: '$G' }",
                "{ $project: { G: '$G', _id: 0 } }");
        }

        [Test]
        public void SelectMany_with_collection_selector_method_simple_scalar()
        {
            var query = CreateQuery()
                .SelectMany(x => x.G, (x, c) => c);

            Assert(query,
                4,
                "{ $unwind: '$G' }",
                "{ $project: { G: '$G', _id: 0 } }");
        }

        [Test]
        public void SelectMany_with_collection_selector_syntax_simple_scalar()
        {
            var query = from x in CreateQuery()
                        from y in x.G
                        select y;

            Assert(query,
                4,
                "{ $unwind: '$G' }",
                "{ $project: { G: '$G', _id: 0 } }");
        }

        [Test]
        public void SelectMany_with_collection_selector_method_computed_scalar()
        {
            var query = CreateQuery()
                .SelectMany(x => x.G, (x, c) => x.C.E.F + c.E.F + c.E.H);

            Assert(query,
                4,
                "{ $unwind: '$G' }",
                "{ $project: { __fld0: { $add: ['$C.E.F', '$G.E.F', '$G.E.H'] }, _id: 0 } }");
        }

        [Test]
        public void SelectMany_with_collection_selector_syntax_computed_scalar()
        {
            var query = from x in CreateQuery()
                        from y in x.G
                        select x.C.E.F + y.E.F + y.E.H;

            Assert(query,
                4,
                "{ $unwind: '$G' }",
                "{ $project: { __fld0: { $add: ['$C.E.F', '$G.E.F', '$G.E.H'] }, _id: 0 } }");
        }

        [Test]
        public void SelectMany_with_collection_selector_method_anonymous_type()
        {
            var query = CreateQuery()
                .SelectMany(x => x.G, (x, c) => new { x.C.E.F, Other = c.D });

            Assert(query,
                4,
                "{ $unwind: '$G' }",
                "{ $project: { F: '$C.E.F', Other: '$G.D', _id: 0 } }");
        }

        [Test]
        public void SelectMany_with_collection_selector_syntax_anonymous_type()
        {
            var query = from x in CreateQuery()
                        from y in x.G
                        select new { x.C.E.F, Other = y.D };

            Assert(query,
                4,
                "{ $unwind: '$G' }",
                "{ $project: { F: '$C.E.F', Other: '$G.D', _id: 0 } }");
        }

        [Test]
        public void SelectMany_followed_by_a_group()
        {
            var first = from x in CreateQuery()
                        from y in x.G
                        select y;

            var query = from f in first
                        group f by f.D into g
                        select new
                        {
                            g.Key,
                            SumF = g.Sum(x => x.E.F)
                        };

            Assert(query,
                4,
                "{ $unwind: '$G' }",
                "{ $project: { G: '$G', _id: 0 } }",
                "{ $group: { _id: '$G.D', __agg0: { $sum : '$G.E.F' } } }",
                "{ $project: { Key: '$_id', SumF: '$__agg0', _id: 0 } }");
        }

        [Test]
        public void Single()
        {
            var result = CreateQuery().Where(x => x.Id == 10).Select(x => x.C.E.F).Single();

            result.Should().Be(11);
        }

        [Test]
        public void Single_with_predicate()
        {
            var result = CreateQuery().Select(x => x.C.E.F).Single(x => x == 11);

            result.Should().Be(11);
        }

        [Test]
        public async Task SingleAsync()
        {
            var result = await CreateQuery().Where(x => x.Id == 10).Select(x => x.C.E.F).SingleAsync();

            result.Should().Be(11);
        }

        [Test]
        public async Task SingleAsync_with_predicate()
        {
            var result = await CreateQuery().Select(x => x.C.E.F).SingleAsync(x => x == 11);

            result.Should().Be(11);
        }

        [Test]
        public void SingleOrDefault()
        {
            var result = CreateQuery().Where(x => x.Id == 10).Select(x => x.C.E.F).SingleOrDefault();

            result.Should().Be(11);
        }

        [Test]
        public void SingleOrDefault_with_predicate()
        {
            var result = CreateQuery().Select(x => x.C.E.F).SingleOrDefault(x => x == 11);

            result.Should().Be(11);
        }

        [Test]
        public async Task SingleOrDefaultAsync()
        {
            var result = await CreateQuery().Where(x => x.Id == 10).Select(x => x.C.E.F).SingleOrDefaultAsync();

            result.Should().Be(11);
        }

        [Test]
        public async Task SingleOrDefaultAsync_with_predicate()
        {
            var result = await CreateQuery().Select(x => x.C.E.F).SingleOrDefaultAsync(x => x == 11);

            result.Should().Be(11);
        }

        [Test]
        public void Skip()
        {
            var query = CreateQuery().Skip(10);

            Assert(query,
                0,
                "{ $skip: 10 }");
        }

        [Test]
        [RequiresServer(MinimumVersion = "3.1.9")]
        public void StandardDeviationPopulation()
        {
            var result = CreateQuery().Select(x => x.C.E.F).StandardDeviationPopulation();

            result.Should().Be(50);
        }

        [Test]
        [RequiresServer(MinimumVersion = "3.1.9")]
        public void StandardDeviationPopulation_with_selector()
        {
            var result = CreateQuery().StandardDeviationPopulation(x => x.C.E.F);

            result.Should().Be(50);
        }

        [Test]
        [RequiresServer(MinimumVersion = "3.1.9")]
        public async Task StandardDeviationPopulationAsync()
        {
            var result = await CreateQuery().Select(x => x.C.E.F).StandardDeviationPopulationAsync();

            result.Should().Be(50);
        }

        [Test]
        [RequiresServer(MinimumVersion = "3.1.9")]
        public async Task StandardDeviationPopulationAsync_with_selector()
        {
            var result = await CreateQuery().StandardDeviationPopulationAsync(x => x.C.E.F);

            result.Should().Be(50);
        }

        [Test]
        [RequiresServer(MinimumVersion = "3.1.9")]
        public void StandardDeviationSample()
        {
            var result = CreateQuery().Select(x => x.C.E.F).StandardDeviationSample();

            result.Should().BeApproximately(70.7106781186548, .0001);
        }

        [Test]
        [RequiresServer(MinimumVersion = "3.1.9")]
        public void StandardDeviationSample_with_selector()
        {
            var result = CreateQuery().StandardDeviationSample(x => x.C.E.F);

            result.Should().BeApproximately(70.7106781186548, .0001);
        }

        [Test]
        [RequiresServer(MinimumVersion = "3.1.9")]
        public async Task StandardDeviationSampleAsync()
        {
            var result = await CreateQuery().Select(x => x.C.E.F).StandardDeviationSampleAsync();

            result.Should().BeApproximately(70.7106781186548, .0001);
        }

        [Test]
        [RequiresServer(MinimumVersion = "3.1.9")]
        public async Task StandardDeviationSampleAsync_with_selector()
        {
            var result = await CreateQuery().StandardDeviationSampleAsync(x => x.C.E.F);

            result.Should().BeApproximately(70.7106781186548, .0001);
        }

        [Test]
        public void Sum()
        {
            var result = CreateQuery().Select(x => x.C.E.F).Sum();

            result.Should().Be(122);
        }

        [Test]
        public void Sum_with_selector()
        {
            var result = CreateQuery().Sum(x => x.C.E.F);

            result.Should().Be(122);
        }

        [Test]
        public void Sum_with_no_results()
        {
            var result = CreateQuery().Where(x => x.C.E.F == 12341235).Sum(x => x.C.E.F);

            result.Should().Be(0);
        }

        [Test]
        public async Task SumAsync()
        {
            var result = await CreateQuery().Select(x => x.C.E.F).SumAsync();

            result.Should().Be(122);
        }

        [Test]
        public async Task SumAsync_with_selector()
        {
            var result = await CreateQuery().SumAsync(x => x.C.E.F);

            result.Should().Be(122);
        }

        [Test]
        public async Task SumAsync_with_no_results()
        {
            var result = await CreateQuery().Where(x => x.C.E.F == 21341235).SumAsync(x => x.C.E.F);

            result.Should().Be(0);
        }

        [Test]
        public void Take()
        {
            var query = CreateQuery().Take(1);

            Assert(query,
                1,
                "{ $limit: 1 }");
        }

        [Test]
        public void Where_method()
        {
            var query = CreateQuery()
                .Where(x => x.A == "Awesome");

            Assert(query,
                1,
                "{ $match: { A: 'Awesome' } }");
        }

        [Test]
        public void Where_syntax()
        {
            var query = from x in CreateQuery()
                        where x.A == "Awesome"
                        select x;

            Assert(query,
                1,
                "{ $match: { A: 'Awesome' } }");
        }

        [Test]
        public void Where_method_with_predicated_any()
        {
            var query = CreateQuery()
                .Where(x => x.G.Any(g => g.D == "Don't"));

            Assert(query,
                1,
                "{ $match: { 'G.D': \"Don't\" } }");
        }

        private List<T> Assert<T>(IMongoQueryable<T> queryable, int resultCount, params string[] expectedStages)
        {
            var executionModel = (AggregateQueryableExecutionModel<T>)queryable.GetExecutionModel();

            CollectionAssert.AreEqual(expectedStages.Select(x => BsonDocument.Parse(x)).ToList(), executionModel.Stages);

            // async
            var results = queryable.ToListAsync().GetAwaiter().GetResult();
            results.Count.Should().Be(resultCount);

            // sync
            results = queryable.ToList();
            results.Count.Should().Be(resultCount);

            return results;
        }

        private IMongoQueryable<Root> CreateQuery()
        {
            return _collection.AsQueryable();
        }

        private IMongoQueryable<Other> CreateOtherQuery()
        {
            return _otherCollection.AsQueryable();
        }
    }
}
