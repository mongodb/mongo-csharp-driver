/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Driver;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Tests;
using MongoDB.Driver.Tests.Linq;
using Xunit;

namespace Tests.MongoDB.Driver.Linq
{
    public class MongoQueryableSessionTests : IntegrationTestBase
    {
        [Fact]
        public void Any()
        {
            Execute(session =>
            {
                CleanCollection(session);

                var result_in_transaction = CreateQuery(session).Any();

                result_in_transaction.Should().BeFalse();

                var result_not_in_transaction = CreateQuery().Any();

                result_not_in_transaction.Should().BeTrue();
            }, false);
        }

        [Fact]
        public void Any_with_predicate()
        {
            Execute(session =>
            {
                var result_in_transaction = CreateQuery(session).Any(x => x.C.E.F == 234124);
                result_in_transaction.Should().BeFalse();

                result_in_transaction = CreateQuery(session).Any(x => x.C.E.F == 1111);
                result_in_transaction.Should().BeTrue();

                var result_not_in_transaction = CreateQuery().Any(x => x.C.E.F == 1111);
                result_not_in_transaction.Should().BeFalse();
            });
        }

        [Fact]
        public async Task AnyAsync()
        {
            await ExecuteAsync(async session =>
            {
                await CleanCollectionAsync(session);

                var result_in_transaction = await CreateQuery(session).AnyAsync();

                result_in_transaction.Should().BeFalse();

                var result_not_in_transaction = await CreateQuery().AnyAsync();

                result_not_in_transaction.Should().BeTrue();
            }, false);
        }

        [Fact]
        public async Task AnyAsync_with_predicate()
        {
            await ExecuteAsync(async session =>
            {
                var result_in_transaction = await CreateQuery(session).AnyAsync(x => x.C.E.F == 234124);
                result_in_transaction.Should().BeFalse();

                result_in_transaction = await CreateQuery(session).AnyAsync(x => x.C.E.F == 1111);
                result_in_transaction.Should().BeTrue();

                var result_not_in_transaction = await CreateQuery().AnyAsync(x => x.C.E.F == 1111);
                result_not_in_transaction.Should().BeFalse();
            });
        }

        [Fact]
        public void Average()
        {
            Execute(session =>
            {
                var result_in_transaction = CreateQuery(session).Select(x => x.C.E.F + 1).Average();

                result_in_transaction.Should().Be(412);

                var result_not_in_transaction = CreateQuery().Select(x => x.C.E.F + 1).Average();

                result_not_in_transaction.Should().Be(62);
            });
        }

        [Fact]
        public void Average_with_select_and_where()
        {
            Execute(session =>
            {
                var result_in_transaction = CreateQuery(session)
                .Select(x => x.C.E.F + 1)
                .Where(x => x > 20)
                .Average();

                result_in_transaction.Should().Be(612);

                var result_not_in_transaction = CreateQuery()
                .Select(x => x.C.E.F + 1)
                .Where(x => x > 20)
                .Average();

                result_not_in_transaction.Should().Be(112);
            });
        }

        [Fact]
        public void Average_with_selector()
        {
            Execute(session =>
            {
                var result_in_transaction = CreateQuery(session).Average(x => x.C.E.F + 1);

                result_in_transaction.Should().Be(412);

                var result_not_in_transaction = CreateQuery().Average(x => x.C.E.F + 1);

                result_not_in_transaction.Should().Be(62);
            });
        }

        [Fact]
        public async Task AverageAsync()
        {
            await ExecuteAsync(async session =>
            {
                var result_in_transaction = await CreateQuery(session).Select(x => x.C.E.F + 1).AverageAsync();

                result_in_transaction.Should().Be(412);

                var result_not_in_transaction = await CreateQuery().Select(x => x.C.E.F + 1).AverageAsync();

                result_not_in_transaction.Should().Be(62);
            });
        }

        [Fact]
        public async Task AverageAsync_with_selector()
        {
            await ExecuteAsync(async session =>
            {
                var result_in_transaction = await CreateQuery(session).AverageAsync(x => x.C.E.F + 1);

                result_in_transaction.Should().Be(412);

                var result_not_in_transaction = await CreateQuery().AverageAsync(x => x.C.E.F + 1);

                result_not_in_transaction.Should().Be(62);
            });
        }

        [Fact]
        public void Count()
        {
            Execute(session =>
            {
                var result_in_transaction = CreateQuery(session).Count();

                result_in_transaction.Should().Be(3);

                var result_not_in_transaction = CreateQuery().Count();

                result_not_in_transaction.Should().Be(2);
            });
        }

        [Fact]
        public void Count_with_predicate()
        {
            Execute(session =>
            {
                var result_in_transaction = CreateQuery(session).Count(x => x.C.E.F == 1111);

                result_in_transaction.Should().Be(1);

                var result_not_in_transaction = CreateQuery().Count(x => x.C.E.F == 11);

                result_not_in_transaction.Should().Be(1);
            });
        }

        [Fact]
        public void Count_with_no_matches()
        {
            Execute(session =>
            {
                var result_in_transaction = CreateQuery(session).Count(x => x.C.E.F == 13151235);

                result_in_transaction.Should().Be(0);

                var result_not_in_transaction = CreateQuery().Count(x => x.C.E.F == 1111);

                result_not_in_transaction.Should().Be(0);
            });
        }

        [Fact]
        public async Task CountAsync()
        {
            await ExecuteAsync(async session =>
            {
                var result_in_transaction = await CreateQuery(session).CountAsync();

                result_in_transaction.Should().Be(3);

                var result_not_in_transaction = await CreateQuery().CountAsync();

                result_not_in_transaction.Should().Be(2);
            });
        }

        [Fact]
        public async Task CountAsync_with_predicate()
        {
            await ExecuteAsync(async session =>
            {
                var result_in_transaction = await CreateQuery(session).CountAsync(x => x.C.E.F == 1111);

                result_in_transaction.Should().Be(1);

                var result_not_in_transaction = await CreateQuery().CountAsync(x => x.C.E.F == 11);

                result_not_in_transaction.Should().Be(1);
            });
        }

        [Fact]
        public async Task CountAsync_with_no_matches()
        {
            await ExecuteAsync(async session =>
            {
                var result_in_transaction = await CreateQuery(session).CountAsync(x => x.C.E.F == 13151235);

                result_in_transaction.Should().Be(0);

                var result_not_in_transaction = await CreateQuery().CountAsync(x => x.C.E.F == 1111);

                result_not_in_transaction.Should().Be(0);
            });
        }

        [SkippableFact]
        public void Distinct_document_followed_by_where()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("2.6.0");
            RequireServer.Check().VersionLessThan("4.1.0"); // TODO: remove this line when SERVER-37459 is fixed

            Execute(session =>
            {
                var query = CreateQuery(session)
                    .Distinct()
                    .Where(x => x.A == "Astonishing");

                Assert(query,
                    1,
                    "{ $group: { _id: '$$ROOT' } }",
                    "{ $match: { '_id.A': 'Astonishing' } }");
            });
        }

        [SkippableFact]
        public void Distinct_document_preceded_by_select_where()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("2.6.0");

            Execute(session =>
            {
                var query = CreateQuery(session)
                .Select(x => new { x.A, x.B })
                .Where(x => x.A == "Astonishing")
                .Distinct();

                Assert(query,
                    1,
                    "{ $project: { 'A': '$A', 'B': '$B', '_id': 0 } }",
                    "{ $match: { 'A': 'Astonishing' } }",
                    "{ $group: { '_id': '$$ROOT' } }");
            });
        }

        [SkippableFact]
        public void Distinct_document_preceded_by_where_select()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("2.6.0");

            Execute(session =>
            {
                var query = CreateQuery(session)
                .Where(x => x.A == "Astonishing")
                .Select(x => new { x.A, x.B })
                .Distinct();

                Assert(query,
                    1,
                    "{ $match: { 'A': 'Astonishing' } }",
                    "{ $group: { '_id': { 'A': '$A', 'B': '$B' } } }");
            });
        }

        [SkippableFact]
        public void Distinct_field_preceded_by_where_select()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("2.6.0");

            Execute(session =>
            {
                var query = CreateQuery(session)
                .Where(x => x.A == "Astonishing")
                .Select(x => x.A)
                .Distinct();

                Assert(query,
                    1,
                    "{ $match: { 'A': 'Astonishing' } }",
                    "{ $group: { '_id': '$A' } }");
            });
        }

        [SkippableFact]
        public void Distinct_field_preceded_by_select_where()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("2.6.0");

            Execute(session =>
            {
                var query = CreateQuery(session)
                .Select(x => x.A)
                .Where(x => x == "Astonishing")
                .Distinct();

                Assert(query,
                    1,
                    "{ $project: { 'A': '$A', '_id': 0 } }",
                    "{ $match: { 'A': 'Astonishing' } }",
                    "{ $group: { '_id': '$A' } }");
            });
        }

        [Fact]
        public void Enumerable_foreach()
        {
            Execute(session =>
            {
                var query_in_transaction = from x in CreateQuery(session)
                                           select x.M;

                int sum_in_transaction = 0;

                foreach (var item in query_in_transaction)
                {
                    sum_in_transaction += item.Sum();
                }

                foreach (var item in query_in_transaction)
                {
                    sum_in_transaction += item.Sum();
                }

                sum_in_transaction.Should().Be(84);

                var query_not_in_transaction = from x in CreateQuery()
                                               select x.M;

                int sum_not_in_transaction = 0;

                foreach (var item in query_not_in_transaction)
                {
                    sum_not_in_transaction += item.Sum();
                }

                foreach (var item in query_not_in_transaction)
                {
                    sum_not_in_transaction += item.Sum();
                }

                sum_not_in_transaction.Should().Be(50);
            });
        }

        [Fact]
        public void First()
        {
            Execute(session =>
            {
                CleanCollection(session);
                InsertThird(session);

                var result_in_transaction = CreateQuery(session).Select(x => x.C.E.F).First();

                result_in_transaction.Should().Be(1111);

                var result_not_in_transaction = CreateQuery().Select(x => x.C.E.F).First();

                result_not_in_transaction.Should().Be(11);
            }, false);
        }

        [Fact]
        public void First_with_predicate()
        {
            Execute(session =>
            {
                var result_in_transaction = CreateQuery(session).Select(x => x.C.E.F).First(x => x == 1111);

                result_in_transaction.Should().Be(1111);

                var result_not_in_transaction = CreateQuery().Select(x => x.C.E.F).First(x => x == 11);

                result_not_in_transaction.Should().Be(11);
            });
        }

        [Fact]
        public async Task FirstAsync()
        {
            await ExecuteAsync(async session =>
            {
                await CleanCollectionAsync(session);
                await InsertThirdAsync(session);

                var result_in_transaction = await CreateQuery(session).Select(x => x.C.E.F).FirstAsync();

                result_in_transaction.Should().Be(1111);

                var result_not_in_transaction = await CreateQuery().Select(x => x.C.E.F).FirstAsync();

                result_not_in_transaction.Should().Be(11);
            }, false);
        }

        [Fact]
        public async Task FirstAsync_with_predicate()
        {
            await ExecuteAsync(async session =>
            {
                var result_in_transaction = await CreateQuery(session).Select(x => x.C.E.F).FirstAsync(x => x == 1111);

                result_in_transaction.Should().Be(1111);

                var result_not_in_transaction = await CreateQuery().Select(x => x.C.E.F).FirstAsync(x => x == 11);

                result_not_in_transaction.Should().Be(11);
            });
        }

        [Fact]
        public void FirstOrDefault()
        {
            Execute(session =>
            {
                CleanCollection(session);
                InsertThird(session);

                var result_in_transaction = CreateQuery(session).Select(x => x.C.E.F).FirstOrDefault();

                result_in_transaction.Should().Be(1111);

                var result_not_in_transaction = CreateQuery().Select(x => x.C.E.F).FirstOrDefault();

                result_not_in_transaction.Should().Be(11);
            }, false);
        }

        [Fact]
        public void FirstOrDefault_with_predicate()
        {
            Execute(session =>
            {
                var result_in_transaction = CreateQuery(session).Select(x => x.C.E.F).FirstOrDefault(x => x == 1111);

                result_in_transaction.Should().Be(1111);

                var result_not_in_transaction = CreateQuery().Select(x => x.C.E.F).FirstOrDefault(x => x == 11);

                result_not_in_transaction.Should().Be(11);
            });
        }

        [Fact]
        public async Task FirstOrDefaultAsync()
        {
            await ExecuteAsync(async session =>
            {
                await CleanCollectionAsync(session);
                await InsertThirdAsync(session);

                var result_in_transaction = await CreateQuery(session).Select(x => x.C.E.F).FirstOrDefaultAsync();

                result_in_transaction.Should().Be(1111);

                var result_not_in_transaction = await CreateQuery().Select(x => x.C.E.F).FirstOrDefaultAsync();

                result_not_in_transaction.Should().Be(11);
            }, false);
        }

        [Fact]
        public async Task FirstOrDefaultAsync_with_predicate()
        {
            await ExecuteAsync(async session =>
            {
                var result_in_transaction = await CreateQuery(session).Select(x => x.C.E.F).FirstOrDefaultAsync(x => x == 1111);

                result_in_transaction.Should().Be(1111);

                var result_not_in_transaction = await CreateQuery().Select(x => x.C.E.F).FirstOrDefaultAsync(x => x == 11);

                result_not_in_transaction.Should().Be(11);
            });
        }

        [Fact]
        public void GroupBy_method()
        {
            Execute(session =>
            {
                var query_in_transaction = CreateQuery(session)
                .GroupBy(x => x.A);

                Assert(query_in_transaction,
                    3,
                    "{ $group: { _id: '$A' } }");

                var query_not_in_transaction = CreateQuery()
                .GroupBy(x => x.A);

                Assert(query_not_in_transaction,
                    2,
                    "{ $group: { _id: '$A' } }");
            });
        }

        [Fact]
        public void Group_method_using_select()
        {
            Execute(session =>
            {
                var query_in_transaction = CreateQuery(session)
                .GroupBy(x => x.A)
                .Select(x => new { A = x.Key, Count = x.Count(), Min = x.Min(y => y.U) });

                Assert(query_in_transaction,
                    3,
                    "{ $group: { _id: '$A', __agg0: { $sum: 1 }, __agg1: { $min: '$U' } } }",
                    "{ $project: { A: '$_id', Count: '$__agg0', Min: '$__agg1', _id: 0 } }");

                var query_not_in_transaction = CreateQuery()
                .GroupBy(x => x.A)
                .Select(x => new { A = x.Key, Count = x.Count(), Min = x.Min(y => y.U) });

                Assert(query_not_in_transaction,
                    2,
                    "{ $group: { _id: '$A', __agg0: { $sum: 1 }, __agg1: { $min: '$U' } } }",
                    "{ $project: { A: '$_id', Count: '$__agg0', Min: '$__agg1', _id: 0 } }");
            });
        }

        [Fact]
        public void GroupBy_groupby_method()
        {
            Execute(session =>
            {
                var query_in_transaction = CreateQuery(session)
                .GroupBy(x => x.A)
                .GroupBy(g => g.First().B);

                Assert(query_in_transaction,
                    3,
                    "{ $group: { _id: '$A', __agg0: { $first: '$B'} } }",
                    "{ $group: { _id: '$__agg0' } }");

                var query_not_in_transaction = CreateQuery()
                .GroupBy(x => x.A)
                .GroupBy(g => g.First().B);

                Assert(query_not_in_transaction,
                    2,
                    "{ $group: { _id: '$A', __agg0: { $first: '$B'} } }",
                    "{ $group: { _id: '$__agg0' } }");
            });
        }

        [Fact]
        public void GroupBy_select_anonymous_type_method()
        {
            Execute(session =>
            {
                var query_in_transaction = CreateQuery(session)
                .GroupBy(x => x.A)
                .Select(g => new { Key = g.Key, FirstB = g.First().B });

                Assert(query_in_transaction,
                    3,
                    "{ $group: { _id: '$A', __agg0: { $first: '$B'} } }",
                    "{ $project: { Key: '$_id', FirstB: '$__agg0', _id: 0 } }");

                var query_not_in_transaction = CreateQuery()
                .GroupBy(x => x.A)
                .Select(g => new { Key = g.Key, FirstB = g.First().B });

                Assert(query_not_in_transaction,
                    2,
                    "{ $group: { _id: '$A', __agg0: { $first: '$B'} } }",
                    "{ $project: { Key: '$_id', FirstB: '$__agg0', _id: 0 } }");
            });
        }
#if !MONO
        [Fact]
        public void GroupBy_select_anonymous_type_syntax()
        {
            Execute(session =>
            {
                var query_in_transaction = from p in CreateQuery(session)
                                           group p by p.A into g
                                           select new { g.Key, FirstB = g.First().B };

                Assert(query_in_transaction,
                    3,
                    "{ $group: { _id: '$A', __agg0: { $first: '$B'} } }",
                    "{ $project: { Key: '$_id', FirstB: '$__agg0', _id: 0 } }");

                var query_not_in_transaction = from p in CreateQuery()
                                               group p by p.A into g
                                               select new { g.Key, FirstB = g.First().B };

                Assert(query_not_in_transaction,
                    2,
                    "{ $group: { _id: '$A', __agg0: { $first: '$B'} } }",
                    "{ $project: { Key: '$_id', FirstB: '$__agg0', _id: 0 } }");
            });
        }
#endif

        [Fact]
        public void GroupBy_where_method()
        {
            Execute(session =>
            {
                var query_in_transaction = CreateQuery(session)
                .GroupBy(x => x.A)
                .Where(g => g.Key == "Astonishing");

                Assert(query_in_transaction,
                    1,
                    "{ $group: { _id: '$A' } }",
                    "{ $match: { _id: 'Astonishing' } }");

                var query_not_in_transaction = CreateQuery()
                .GroupBy(x => x.A)
                .Where(g => g.Key == "Astonishing");

                Assert(query_not_in_transaction,
                    0,
                    "{ $group: { _id: '$A' } }",
                    "{ $match: { _id: 'Astonishing' } }");
            });
        }

        [Fact]
        public void GroupBy_where_with_accumulator_method()
        {
            Execute(session =>
            {
                var query_in_transaction = CreateQuery(session)
                .GroupBy(x => x.A)
                .Where(g => g.First().B == "Bamboo");

                Assert(query_in_transaction,
                    1,
                    "{ $group: { _id: '$A', __agg0: { $first: '$B' } } }",
                    "{ $match: { __agg0: 'Bamboo' } }");

                var query_not_in_transaction = CreateQuery()
                .GroupBy(x => x.A)
                .Where(g => g.First().B == "Bamboo");

                Assert(query_not_in_transaction,
                    0,
                    "{ $group: { _id: '$A', __agg0: { $first: '$B' } } }",
                    "{ $match: { __agg0: 'Bamboo' } }");
            });
        }

        [Fact]
        public void GroupBy_where_select_anonymous_type_with_duplicate_accumulators_method()
        {
            Execute(session =>
            {
                var query_in_transaction = CreateQuery(session)
                    .GroupBy(x => x.A)
                    .Where(g => g.First().B == "Bamboo")
                    .Select(x => new { x.Key, FirstB = x.First().B });

                Assert(query_in_transaction,
                    1,
                    "{ $group: { _id: '$A', __agg0: { $first: '$B'} } }",
                    "{ $match: { __agg0: 'Bamboo' } }",
                    "{ $project: { Key: '$_id', FirstB: '$__agg0', _id: 0 } }");

                var query_not_in_transaction = CreateQuery()
                    .GroupBy(x => x.A)
                    .Where(g => g.First().B == "Bamboo")
                    .Select(x => new { x.Key, FirstB = x.First().B });

                Assert(query_not_in_transaction,
                    0,
                    "{ $group: { _id: '$A', __agg0: { $first: '$B'} } }",
                    "{ $match: { __agg0: 'Bamboo' } }",
                    "{ $project: { Key: '$_id', FirstB: '$__agg0', _id: 0 } }");
            });
        }

#if !MONO
        [Fact]
        public void GroupBy_where_select_anonymous_type_with_duplicate_accumulators_syntax()
        {
            Execute(session =>
            {
                var query_in_transaction = from p in CreateQuery(session)
                                           group p by p.A into g
                                           where g.First().B == "Bamboo"
                                           select new { g.Key, FirstB = g.First().B };

                Assert(query_in_transaction,
                    1,
                    "{ $group: { _id: '$A', __agg0: { $first: '$B'} } }",
                    "{ $match: { __agg0: 'Bamboo' } }",
                    "{ $project: { Key: '$_id', FirstB: '$__agg0', _id: 0 } }");

                var query_not_in_transaction = from p in CreateQuery()
                                               group p by p.A into g
                                               where g.First().B == "Bamboo"
                                               select new { g.Key, FirstB = g.First().B };

                Assert(query_not_in_transaction,
                    0,
                    "{ $group: { _id: '$A', __agg0: { $first: '$B'} } }",
                    "{ $match: { __agg0: 'Bamboo' } }",
                    "{ $project: { Key: '$_id', FirstB: '$__agg0', _id: 0 } }");
            });
        }
#endif

        [Fact]
        public void GroupBy_with_resultSelector_anonymous_type_method()
        {
            Execute(session =>
            {
                var query_in_transaction = CreateQuery(session)
                .GroupBy(x => x.A, (k, s) => new { Key = k, FirstB = s.First().B });

                Assert(query_in_transaction,
                    3,
                    "{ $group: { _id: '$A', FirstB: { $first: '$B'} } }");

                var query_not_in_transaction = CreateQuery()
                .GroupBy(x => x.A, (k, s) => new { Key = k, FirstB = s.First().B });

                Assert(query_not_in_transaction,
                    2,
                    "{ $group: { _id: '$A', FirstB: { $first: '$B'} } }");
            });
        }

        [Fact]
        public void GroupBy_combined_with_a_previous_embedded_pipeline()
        {
            Execute(session =>
            {
                var bs = new List<string>
                {
                    "Bambo",
                    "Bamboo"
                };

                var query_in_transaction = CreateQuery(session)
                    .Where(x => bs.Contains(x.B))
                    .GroupBy(x => x.A)
                    .Select(x => x.Max(y => y.C));

                Assert(query_in_transaction,
                    1,
                    "{ $match: { 'B': { '$in': ['Bambo', 'Bamboo'] } } }",
                    "{ $group: { '_id': '$A', '__agg0': { '$max': '$C' } } }",
                    "{ $project: { '__fld0': '$__agg0', '_id': 0 } }");

                var query_not_in_transaction = CreateQuery()
                    .Where(x => bs.Contains(x.B))
                    .GroupBy(x => x.A)
                    .Select(x => x.Max(y => y.C));

                Assert(query_not_in_transaction,
                    0,
                    "{ $match: { 'B': { '$in': ['Bambo', 'Bamboo'] } } }",
                    "{ $group: { '_id': '$A', '__agg0': { '$max': '$C' } } }",
                    "{ $project: { '__fld0': '$__agg0', '_id': 0 } }");
            });
        }

        [SkippableFact]
        public void GroupJoin_method()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");

            Execute(session =>
            {
                var query_in_transaction = CreateQuery(session)
                .GroupJoin(
                    CreateOtherQuery(session),
                    p => p.Id,
                    o => o.Id,
                    (p, o) => new { p, o });

                Assert(query_in_transaction,
                    3,
                    "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: '_id', as: 'o' } }");

                var query_not_in_transaction = CreateQuery()
                .GroupJoin(
                    CreateOtherQuery(),
                    p => p.Id,
                    o => o.Id,
                    (p, o) => new { p, o });

                Assert(query_not_in_transaction,
                    2,
                    "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: '_id', as: 'o' } }");
            });
        }

        [SkippableFact]
        public void GroupJoinForeignField_method()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");

            Execute(session =>
            {
                var query_in_transaction = CreateQuery(session)
                .GroupJoin(
                    CreateOtherQuery(session),
                    p => p.Id,
                    o => o.CEF,
                    (p, o) => new { p, o });

                Assert(query_in_transaction,
                    3,
                    "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: 'CEF', as: 'o' } }");

                var query_not_in_transaction = CreateQuery()
                .GroupJoin(
                    CreateOtherQuery(),
                    p => p.Id,
                    o => o.CEF,
                    (p, o) => new { p, o });

                Assert(query_not_in_transaction,
                    2,
                    "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: 'CEF', as: 'o' } }");
            });
        }

        [SkippableFact]
        public void GroupJoin_syntax()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");

            Execute(session =>
            {
                var query_in_transaction = from p in CreateQuery(session)
                                           join o in CreateOtherQuery(session) on p.Id equals o.Id into joined
                                           select new { A = p.A, SumCEF = joined.Sum(x => x.CEF) };

                Assert(query_in_transaction,
                    3,
                    "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: '_id', as: 'joined' } }",
                    "{ $project: { A: '$A', SumCEF: { $sum: '$joined.CEF' }, _id: 0 } }");

                var query_not_in_transaction = from p in CreateQuery()
                                               join o in CreateOtherQuery() on p.Id equals o.Id into joined
                                               select new { A = p.A, SumCEF = joined.Sum(x => x.CEF) };

                Assert(query_not_in_transaction,
                    2,
                    "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: '_id', as: 'joined' } }",
                    "{ $project: { A: '$A', SumCEF: { $sum: '$joined.CEF' }, _id: 0 } }");
            });
        }

        [SkippableFact]
        public void GroupJoin_syntax_with_a_transparent_identifier()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");

            Execute(session =>
            {
                var query_in_transaction = from p in CreateQuery(session)
                                           join o in CreateOtherQuery(session) on p.Id equals o.Id into joined
                                           orderby p.B
                                           select new { A = p.A, Joined = joined };

                Assert(query_in_transaction,
                    3,
                    "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: '_id', as: 'joined' } }",
                    "{ $sort: { B: 1 } }",
                    "{ $project: { A: '$A', Joined: '$joined', _id: 0 } }");

                var query_not_in_transaction = from p in CreateQuery()
                                               join o in CreateOtherQuery() on p.Id equals o.Id into joined
                                               orderby p.B
                                               select new { A = p.A, Joined = joined };

                Assert(query_not_in_transaction,
                    2,
                    "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: '_id', as: 'joined' } }",
                    "{ $sort: { B: 1 } }",
                    "{ $project: { A: '$A', Joined: '$joined', _id: 0 } }");
            });
        }

        [SkippableFact]
        public void GroupJoin_syntax_with_select_many()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");

            Execute(session =>
            {
                InsertJoin(session);

                var query_in_transaction = from p in CreateQuery(session)
                                           join o in __otherCollection on p.Id equals o.Id into joined
                                           from subo in joined
                                           select new { A = p.A, CEF = subo.CEF };

                Assert(query_in_transaction,
                    2,
                    "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: '_id', as: 'joined' } }",
                    "{ $unwind: '$joined' }",
                    "{ $project: { A: '$A', CEF: '$joined.CEF', _id: 0 } }");

                var query_not_in_transaction = from p in CreateQuery()
                                               join o in __otherCollection on p.Id equals o.Id into joined
                                               from subo in joined
                                               select new { A = p.A, CEF = subo.CEF };

                Assert(query_not_in_transaction,
                    1,
                    "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: '_id', as: 'joined' } }",
                    "{ $unwind: '$joined' }",
                    "{ $project: { A: '$A', CEF: '$joined.CEF', _id: 0 } }");
            });
        }

        [SkippableFact]
        public void GroupJoin_syntax_with_select_many_and_DefaultIfEmpty()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");

            Execute(session =>
            {
                var query_in_transaction = from p in CreateQuery(session)
                                           join o in __otherCollection on p.Id equals o.Id into joined
                                           from subo in joined.DefaultIfEmpty()
                                           select new { A = p.A, CEF = (int?)subo.CEF ?? null };

                Assert(query_in_transaction,
                    3,
                    "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: '_id', as: 'joined' } }",
                    "{ $unwind: { path: '$joined', preserveNullAndEmptyArrays: true } }",
                    "{ $project: { A: '$A', CEF: { $ifNull: ['$joined.CEF', null] }, _id: 0 } }");

                var query_not_in_transaction = from p in CreateQuery()
                                               join o in __otherCollection on p.Id equals o.Id into joined
                                               from subo in joined.DefaultIfEmpty()
                                               select new { A = p.A, CEF = (int?)subo.CEF ?? null };

                Assert(query_not_in_transaction,
                    2,
                    "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: '_id', as: 'joined' } }",
                    "{ $unwind: { path: '$joined', preserveNullAndEmptyArrays: true } }",
                    "{ $project: { A: '$A', CEF: { $ifNull: ['$joined.CEF', null] }, _id: 0 } }");
            });
        }

        [SkippableFact]
        public void Join_method()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");

            Execute(session =>
            {
                InsertJoin(session);

                var query_in_transaction = CreateQuery(session)
                .Join(
                    CreateOtherQuery(session),
                    p => p.Id,
                    o => o.Id,
                    (p, o) => new { p, o });

                Assert(query_in_transaction,
                    2,
                    "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: '_id', as: 'o' } }",
                    "{ $unwind: '$o' }");


                var query_not_in_transaction = CreateQuery()
                .Join(
                    CreateOtherQuery(),
                    p => p.Id,
                    o => o.Id,
                    (p, o) => new { p, o });

                Assert(query_not_in_transaction,
                    1,
                    "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: '_id', as: 'o' } }",
                    "{ $unwind: '$o' }");
            });
        }

        [SkippableFact]
        public void JoinForeignField_method()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");
            Execute(session =>
            {
                var query = CreateQuery(session)
                .Join(
                    CreateOtherQuery(session),
                    p => p.Id,
                    o => o.CEF,
                    (p, o) => new { p, o });

                Assert(query,
                    0,
                    "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: 'CEF', as: 'o' } }",
                    "{ $unwind: '$o' }");
            });
        }

        [SkippableFact]
        public void Join_syntax()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");

            Execute(session =>
            {
                InsertJoin(session);

                var query_in_transaction = from p in CreateQuery(session)
                            join o in CreateOtherQuery(session) on p.Id equals o.Id
                            select new { A = p.A, CEF = o.CEF };

                Assert(query_in_transaction,
                    2,
                    "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: '_id', as: 'o' } }",
                    "{ $unwind: '$o' }",
                    "{ $project: { A: '$A', CEF: '$o.CEF', _id: 0 } }");

                var query_not_in_transaction = from p in CreateQuery()
                            join o in CreateOtherQuery() on p.Id equals o.Id
                            select new { A = p.A, CEF = o.CEF };

                Assert(query_not_in_transaction,
                    1,
                    "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: '_id', as: 'o' } }",
                    "{ $unwind: '$o' }",
                    "{ $project: { A: '$A', CEF: '$o.CEF', _id: 0 } }");
            });
        }

        [SkippableFact]
        public void Join_syntax_with_a_transparent_identifier()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");

            Execute(session =>
            {
                InsertJoin(session);

                var query_in_transaction = from p in CreateQuery(session)
                            join o in CreateOtherQuery(session) on p.Id equals o.Id
                            orderby p.B, o.Id
                            select new { A = p.A, CEF = o.CEF };

                Assert(query_in_transaction,
                    2,
                    "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: '_id', as: 'o' } }",
                    "{ $unwind: '$o' }",
                    "{ $sort: { B: 1, 'o._id': 1 } }",
                    "{ $project: { A: '$A', CEF: '$o.CEF', _id: 0 } }");

                var query_not_in_transaction = from p in CreateQuery()
                            join o in CreateOtherQuery() on p.Id equals o.Id
                            orderby p.B, o.Id
                            select new { A = p.A, CEF = o.CEF };

                Assert(query_not_in_transaction,
                    1,
                    "{ $lookup: { from: 'testcollection_other', localField: '_id', foreignField: '_id', as: 'o' } }",
                    "{ $unwind: '$o' }",
                    "{ $sort: { B: 1, 'o._id': 1 } }",
                    "{ $project: { A: '$A', CEF: '$o.CEF', _id: 0 } }");
            });
        }

        [Fact]
        public void LongCount()
        {
            Execute(session =>
            {
                var result_in_transaction = CreateQuery(session).LongCount();

                result_in_transaction.Should().Be(3);

                var result_not_in_transaction = CreateQuery().LongCount();

                result_not_in_transaction.Should().Be(2);
            });
        }

        [Fact]
        public void LongCount_with_predicate()
        {
            Execute(session =>
            {
                var result_in_transaction = CreateQuery(session).LongCount(x => x.C.E.F == 1111);

                result_in_transaction.Should().Be(1);

                var result_not_in_transaction = CreateQuery().LongCount(x => x.C.E.F == 11);

                result_not_in_transaction.Should().Be(1);
            });
        }

        [Fact]
        public void LongCount_with_no_results()
        {
            Execute(session =>
            {
                var result_in_transaction = CreateQuery(session).LongCount(x => x.C.E.F == 13151235);

                result_in_transaction.Should().Be(0);

                var result_not_in_transaction = CreateQuery().LongCount(x => x.C.E.F == 1111);

                result_not_in_transaction.Should().Be(0);
            });
        }

        [Fact]
        public async Task LongCountAsync()
        {
            await ExecuteAsync(async session =>
            {
                var result_in_transaction = await CreateQuery(session).LongCountAsync();

                result_in_transaction.Should().Be(3);

                var result_not_in_transaction = await CreateQuery().LongCountAsync();

                result_not_in_transaction.Should().Be(2);
            });
        }

        [Fact]
        public async Task LongCountAsync_with_predicate()
        {
            await ExecuteAsync(async session =>
            {
                var result_in_transaction = await CreateQuery(session).LongCountAsync(x => x.C.E.F == 1111);

                result_in_transaction.Should().Be(1);

                var result_not_in_transaction = await CreateQuery().LongCountAsync(x => x.C.E.F == 11);

                result_not_in_transaction.Should().Be(1);
            });
        }

        [Fact]
        public async Task LongCountAsync_with_no_results()
        {
            await ExecuteAsync(async session =>
            {
                var result_in_transaction = await CreateQuery(session).LongCountAsync(x => x.C.E.F == 13151235);

                result_in_transaction.Should().Be(0);

                var result_not_in_transaction = await CreateQuery().LongCountAsync(x => x.C.E.F == 1111);

                result_not_in_transaction.Should().Be(0);
            });
        }

        [Fact]
        public void Max()
        {
            Execute(session =>
            {
                var result_in_transaction = CreateQuery(session).Select(x => x.C.E.F).Max();

                result_in_transaction.Should().Be(1111);

                var result_not_in_transaction = CreateQuery().Select(x => x.C.E.F).Max();

                result_not_in_transaction.Should().Be(111);
            });
        }

        [Fact]
        public void Max_with_selector()
        {
            Execute(session =>
            {
                var result_in_transaction = CreateQuery(session).Max(x => x.C.E.F);

                result_in_transaction.Should().Be(1111);

                var result_not_in_transaction = CreateQuery().Max(x => x.C.E.F);

                result_not_in_transaction.Should().Be(111);
            });
        }

        [Fact]
        public async Task MaxAsync()
        {
            await ExecuteAsync(async session =>
            {
                var result_in_transaction = await CreateQuery(session).Select(x => x.C.E.F).MaxAsync();

                result_in_transaction.Should().Be(1111);

                var result_not_in_transaction = await CreateQuery().Select(x => x.C.E.F).MaxAsync();

                result_not_in_transaction.Should().Be(111);
            });
        }

        [Fact]
        public async Task MaxAsync_with_selector()
        {
            await ExecuteAsync(async session =>
            {
                var result_in_transaction = await CreateQuery(session).MaxAsync(x => x.C.E.F);

                result_in_transaction.Should().Be(1111);

                var result_not_in_transaction = await CreateQuery().MaxAsync(x => x.C.E.F);

                result_not_in_transaction.Should().Be(111);
            });
        }

        [Fact]
        public void Min()
        {
            Execute(session =>
            {
                CleanCollection(session);
                InsertThird(session);

                var result_in_transaction = CreateQuery(session).Select(x => x.C.E.F).Min();

                result_in_transaction.Should().Be(1111);

                var result_not_in_transaction = CreateQuery().Select(x => x.C.E.F).Min();

                result_not_in_transaction.Should().Be(11);
            }, false);
        }

        [Fact]
        public void Min_with_selector()
        {
            Execute(session =>
            {
                CleanCollection(session);
                InsertThird(session);

                var result_in_transaction = CreateQuery(session).Min(x => x.C.E.F);

                result_in_transaction.Should().Be(1111);

                var result_not_in_transaction = CreateQuery().Min(x => x.C.E.F);

                result_not_in_transaction.Should().Be(11);
            }, false);
        }

        [Fact]
        public async Task MinAsync()
        {
            await ExecuteAsync(async session =>
            {
                CleanCollection(session);
                InsertThird(session);

                var result_in_transaction = await CreateQuery(session).Select(x => x.C.E.F).MinAsync();

                result_in_transaction.Should().Be(1111);

                var result_not_in_transaction = await CreateQuery().Select(x => x.C.E.F).MinAsync();

                result_not_in_transaction.Should().Be(11);
            }, false);
        }

        [Fact]
        public async Task MinAsync_with_selector()
        {
            await ExecuteAsync(async session =>
            {
                CleanCollection(session);
                InsertThird(session);

                var result_in_transaction = await CreateQuery(session).MinAsync(x => x.C.E.F);

                result_in_transaction.Should().Be(1111);

                var result_not_in_transaction = await CreateQuery().MinAsync(x => x.C.E.F);

                result_not_in_transaction.Should().Be(11);
            }, false);
        }

        [Fact]
        public void OfType()
        {
            Execute(session =>
            {
                CleanCollection(session);
                InsertThird(session);

                var query_in_transaction = CreateQuery(session).OfType<RootDescended>();

                Assert(query_in_transaction,
                    0,
                    "{ $match: { _t: 'RootDescended' } }");

                var query_not_in_transaction = CreateQuery().OfType<RootDescended>();

                Assert(query_not_in_transaction,
                    1,
                    "{ $match: { _t: 'RootDescended' } }");
            }, false);
        }

        [Fact]
        public void OfType_with_a_field()
        {
            Execute(session =>
            {
                var query_in_transaction = CreateQuery(session)
                .Select(x => x.C.E)
                .OfType<V>()
                .Where(v => v.W == 11111);

                Assert(query_in_transaction,
                    1,
                    "{ $project: { E: '$C.E', _id: 0 } }",
                    "{ $match: { 'E._t': 'V' } }",
                    "{ $match: { 'E.W': 11111 } }");

                var query_not_in_transaction = CreateQuery()
                .Select(x => x.C.E)
                .OfType<V>()
                .Where(v => v.W == 11111);

                Assert(query_not_in_transaction,
                    0,
                    "{ $project: { E: '$C.E', _id: 0 } }",
                    "{ $match: { 'E._t': 'V' } }",
                    "{ $match: { 'E.W': 11111 } }");
            });
        }

        [Fact]
        public void OrderBy_method()
        {
            var query = CreateQuery()
                .OrderBy(x => x.A);

            Assert(query,
                2,
                "{ $sort: { A: 1 } }");
        }

        [Fact]
        public void OrderBy_syntax()
        {
            Execute(session =>
            {
                var query = from x in CreateQuery(session)
                            orderby x.A
                            select x;

                Assert(query,
                    3,
                    "{ $sort: { A: 1 } }");
            });
        }

        [Fact]
        public void OrderByDescending_method()
        {
            Execute(session =>
            {
                var query = CreateQuery(session)
                .OrderByDescending(x => x.A);

                Assert(query,
                    3,
                    "{ $sort: { A: -1 } }");
            });
        }

        [Fact]
        public void OrderByDescending_syntax()
        {
            Execute(session =>
            {
                var query = from x in CreateQuery(session)
                            orderby x.A descending
                            select x;

                Assert(query,
                    3,
                    "{ $sort: { A: -1 } }");
            });
        }

        [Fact]
        public void OrderBy_ThenBy_ThenByDescending_method()
        {
            Execute(session =>
            {
                var query = CreateQuery(session)
                .OrderBy(x => x.A)
                .ThenBy(x => x.B)
                .ThenByDescending(x => x.C);

                Assert(query,
                    3,
                    "{ $sort: { A: 1, B: 1, C: -1 } }");
            });
        }

        [Fact]
        public void OrderBy_ThenBy_ThenByDescending_syntax()
        {
            Execute(session =>
            {
                var query = from x in CreateQuery(session)
                            orderby x.A, x.B, x.C descending
                            select x;

                Assert(query,
                    3,
                    "{ $sort: { A: 1, B: 1, C: -1 } }");
            });
        }

        [Fact]
        public void OrderBy_ThenBy_ThenByDescending_with_redundant_fields_method()
        {
            Execute(session =>
            {
                var query = CreateQuery(session)
                .OrderBy(x => x.A)
                .ThenBy(x => x.B)
                .ThenBy(x => x.A);

                Action act = () => query.ToList();
                act.ShouldThrow<NotSupportedException>();
            });
        }

        [Fact]
        public void OrderBy_ThenBy_ThenByDescending_with_redundant_fields_in_different_directions_method()
        {
            Execute(session =>
            {
                var query = CreateQuery(session)
                .OrderBy(x => x.A)
                .ThenBy(x => x.B)
                .ThenByDescending(x => x.A);

                Action act = () => query.ToList();
                act.ShouldThrow<NotSupportedException>();
            });
        }

        [SkippableFact]
        public void Sample()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");

            Execute(session =>
            {
                var query = CreateQuery(session).Sample(100);

                Assert(query,
                    3,
                    "{ $sample: { size: 100 } }");
            });
        }

        [SkippableFact]
        public void Sample_after_another_function()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");

            Execute(session =>
            {
                var query = CreateQuery(session).Select(x => x.A).Sample(100);

                Assert(query,
                    3,
                    "{ $project: { A: '$A', _id: 0 } }",
                    "{ $sample: { size: 100 } }");
            });
        }

        [Fact]
        public void Select_identity()
        {
            Execute(session =>
            {
                var query = CreateQuery(session).Select(x => x);

                Assert(query, 3);
            });
        }

        [Fact]
        public void Select_new_of_same()
        {
            Execute(session =>
            {
                var query = CreateQuery(session).Select(x => new Root { Id = x.Id, A = x.A });

                Assert(query,
                    3,
                    "{ $project: { Id: '$_id', A: '$A', _id: 0} }");
            });
        }

        [Fact]
        public void Select_method_computed_scalar_followed_by_distinct_followed_by_where()
        {
            Execute(session =>
            {
                var query = CreateQuery(session)
                .Select(x => x.A + " " + x.B)
                .Distinct()
                .Where(x => x == "Astonishing Bamboo");

                Assert(query,
                    1,
                    "{ $group: { _id: { $concat: ['$A', ' ', '$B'] } } }",
                    "{ $match: { _id: 'Astonishing Bamboo' } }");
            });
        }

        [Fact]
        public void Select_method_computed_scalar_followed_by_where()
        {
            Execute(session =>
            {
                var query = CreateQuery(session)
                .Select(x => x.A + " " + x.B)
                .Where(x => x == "Astonishing Bamboo");

                Assert(query,
                    1,
                    "{ $project: { __fld0: { $concat: ['$A', ' ', '$B'] }, _id: 0 } }",
                    "{ $match: { __fld0: 'Astonishing Bamboo' } }");
            });
        }

        [SkippableFact]
        public void Select_method_with_predicated_any()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("2.6.0");

            Execute(session =>
            {
                var query = CreateQuery(session)
                    .Select(x => x.G.Any(g => g.D == "Dock"));

                Assert(query,
                    3,
                    "{ $project: { __fld0: { $anyElementTrue: { $map: { input: '$G', as: 'g', 'in': { $eq: ['$$g.D', 'Dock'] } } } }, _id: 0 } }");
            });
        }

        [Fact]
        public void Select_anonymous_type_where_method()
        {
            Execute(session =>
            {
                var query = CreateQuery(session)
                .Select(x => new { Yeah = x.A })
                .Where(x => x.Yeah == "Astonishing");

                Assert(query,
                    1,
                    "{ $project: { Yeah: '$A', _id: 0 } }",
                    "{ $match: { Yeah: 'Astonishing' } }");
            });
        }

        [Fact]
        public void Select_scalar_where_method()
        {
            Execute(session =>
            {
                var query = CreateQuery(session)
                .Select(x => x.A)
                .Where(x => x == "Astonishing");

                Assert(query,
                    1,
                    "{ $project: { A: '$A', _id: 0 } }",
                    "{ $match: { A: 'Astonishing' } }");
            });
        }

        [Fact]
        public void Select_anonymous_type_method()
        {
            Execute(session =>
            {
                var query = CreateQuery(session).Select(x => new { Yeah = x.A });

                Assert(query,
                    3,
                    "{ $project: { Yeah: '$A', _id: 0 } }");
            });
        }

        [Fact]
        public void Select_anonymous_type_syntax()
        {
            Execute(session =>
            {
                var query = from x in CreateQuery(session)
                            select new { Yeah = x.A };

                Assert(query,
                    3,
                    "{ $project: { Yeah: '$A', _id: 0 } }");
            });
        }

        [Fact]
        public void Select_method_scalar()
        {
            Execute(session =>
            {
                var query = CreateQuery(session).Select(x => x.A);

                Assert(query,
                    3,
                    "{ $project: { A: '$A', _id: 0 } }");
            });
        }

        [Fact]
        public void Select_syntax_scalar()
        {
            Execute(session =>
            {
                var query = from x in CreateQuery(session)
                            select x.A;

                Assert(query,
                    3,
                    "{ $project: { A: '$A', _id: 0 } }");
            });
        }

        [Fact]
        public void Select_method_computed_scalar()
        {
            Execute(session =>
            {
                var query = CreateQuery(session).Select(x => x.A + " " + x.B);

                Assert(query,
                    3,
                    "{ $project: { __fld0: { $concat: ['$A', ' ', '$B'] }, _id: 0 } }");
            });
        }

        [Fact]
        public void Select_syntax_computed_scalar()
        {
            Execute(session =>
            {
                var query = from x in CreateQuery(session)
                            select x.A + " " + x.B;

                Assert(query,
                    3,
                    "{ $project: { __fld0: { $concat: ['$A', ' ', '$B'] }, _id: 0 } }");
            });
        }

        [Fact]
        public void Select_method_array()
        {
            Execute(session =>
            {
                var query = CreateQuery(session).Select(x => x.M);

                Assert(query,
                    3,
                    "{ $project: { M: '$M', _id: 0 } }");
            });
        }

        [Fact]
        public void Select_syntax_array()
        {
            Execute(session =>
            {
                var query = from x in CreateQuery(session)
                            select x.M;

                Assert(query,
                    3,
                    "{ $project: { M: '$M', _id: 0 } }");
            });
        }

        [SkippableFact]
        public void Select_method_array_index()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");

            Execute(session =>
            {
                var query = CreateQuery(session).Select(x => x.M[0]);

                Assert(query,
                    3,
                    "{ $project: { __fld0: { $arrayElemAt: ['$M', 0] }, _id: 0 } }");
            });
        }

        [SkippableFact]
        public void Select_syntax_array_index()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");

            Execute(session =>
            {
                var query = from x in CreateQuery(session)
                            select x.M[0];

                Assert(query,
                    3,
                    "{ $project: { __fld0: { $arrayElemAt: ['$M', 0] }, _id: 0 } }");
            });
        }

        [SkippableFact]
        public void Select_method_embedded_pipeline()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");

            Execute(session =>
            {
                var query = CreateQuery(session).Select(x => x.M.First());

                Assert(query,
                    3,
                    "{ $project: { __fld0: { $arrayElemAt: ['$M', 0] }, _id: 0 } }");
            });
        }

        [SkippableFact]
        public void Select_method_computed_array()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("2.6.0");

            Execute(session =>
            {
                var query = CreateQuery(session)
                    .Select(x => x.M.Select(i => i + 1));

                Assert(query,
                    3,
                    "{ $project: { __fld0: { $map: { input: '$M', as: 'i', in: { $add: ['$$i', 1] } } }, _id: 0 } }");
            });
        }

        [SkippableFact]
        public void Select_syntax_computed_array()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("2.6.0");

            Execute(session =>
            {
                var query = from x in CreateQuery(session)
                            select x.M.Select(i => i + 1);

                Assert(query,
                    3,
                    "{ $project: { __fld0: { $map: { input: '$M', as: 'i', in: { $add: ['$$i', 1] } } }, _id: 0 } }");
            });
        }

        [Fact]
        public void Select_followed_by_group()
        {
            Execute(session =>
            {
                var query = CreateQuery(session)
                    .Select(x => new
                    {
                        Id = x.Id,
                        First = x.A,
                        Second = x.B
                    })
                    .GroupBy(x => x.First, (k, s) => new
                    {
                        First = k,
                        Stuff = s.Select(y => new { y.Id, y.Second })
                    });

                Assert(query,
                    3,
                    "{ $project: { Id: '$_id', First: '$A', Second: '$B', _id: 0 } }",
                    "{ $group: { _id: '$First', Stuff: { $push: { Id: '$Id', Second: '$Second' } } } }");
            });
        }

        [Fact]
        public void SelectMany_with_only_resultSelector()
        {
            Execute(session =>
            {
                var query = CreateQuery(session)
                    .SelectMany(x => x.G);

                Assert(query,
                    6,
                    "{ $unwind: '$G' }",
                    "{ $project: { G: '$G', _id: 0 } }");
            });
        }

        [Fact]
        public void SelectMany_with_result_selector_which_has_subobjects()
        {
            Execute(session =>
            {
                var query = CreateQuery(session)
                    .SelectMany(x => x.C.X);

                Assert(query,
                    4,
                    "{ $unwind : '$C.X' }",
                    "{ $project : { X : '$C.X', _id : 0 } }");
            });
        }

        [Fact]
        public void SelectMany_with_result_selector_which_is_called_from_SelectMany()
        {
            Execute(session =>
            {
                var cQuery = CreateQuery(session)
                    .SelectMany(g => g.G)
                    .SelectMany(s => s.S);

                Assert(cQuery,
                    2,
                    "{ $unwind : '$G' }",
                    "{ $project : { G : '$G', _id : 0 } }",
                    "{ $unwind : '$G.S' }",
                    "{ $project : { S : '$G.S', _id : 0 } }");

                var xQuery = CreateQuery(session)
                    .SelectMany(g => g.G)
                    .SelectMany(s => s.S)
                    .SelectMany(x => x.X);

                Assert(xQuery,
                    0,
                    "{ $unwind : '$G' }",
                    "{ $project : { G : '$G', _id : 0 } }",
                    "{ $unwind : '$G.S' }",
                    "{ $project : { S : '$G.S', _id : 0 } }",
                    "{ $unwind : '$S.X' }",
                    "{ $project : { X : '$S.X', _id : 0 } }");
            });
        }

        [Fact]
        public void SelectMany_with_result_selector_which_called_from_where()
        {
            Execute(session =>
            {
                var query = CreateQuery(session)
                    .Where(c => c.K)
                    .SelectMany(x => x.G);

                Assert(query,
                    4,
                    "{ $match : { 'K' : true } }",
                    "{ $unwind : '$G' }",
                    "{ $project : { G : '$G', _id : 0 } }");
            });
        }

        [Fact]
        public void SelectMany_with_collection_selector_method_simple_scalar()
        {
            Execute(session =>
            {
                var query = CreateQuery(session)
                    .SelectMany(x => x.G, (x, c) => c);

                Assert(query,
                    6,
                    "{ $unwind: '$G' }",
                    "{ $project: { G: '$G', _id: 0 } }");
            });
        }

        [Fact]
        public void SelectMany_with_collection_selector_method_simple_scalar_which_is_called_from_SelectMany()
        {
            Execute(session =>
            {
                var cQuery = CreateQuery(session)
                    .SelectMany(g => g.G, (x, c) => c)
                    .SelectMany(s => s.S);

                Assert(cQuery,
                    2,
                    "{ $unwind : '$G' }",
                    "{ $project : { G : '$G', _id : 0 } }",
                    "{ $unwind : '$G.S' }",
                    "{ $project : { S : '$G.S', _id : 0 } }");

                cQuery = CreateQuery(session)
                    .SelectMany(g => g.G)
                    .SelectMany(s => s.S, (x, c) => c);

                Assert(cQuery,
                    2,
                    "{ $unwind : '$G' }",
                    "{ $project : { G : '$G', _id : 0 } }",
                    "{ $unwind : '$G.S' }",
                    "{ $project : { S : '$G.S', _id : 0 } }");

                cQuery = CreateQuery(session)
                    .SelectMany(g => g.G, (x, c) => c)
                    .SelectMany(s => s.S, (x, c) => c);

                Assert(cQuery,
                    2,
                    "{ $unwind : '$G' }",
                    "{ $project : { G : '$G', _id : 0 } }",
                    "{ $unwind : '$G.S' }",
                    "{ $project : { S : '$G.S', _id : 0 } }");

                var xQuery = CreateQuery(session)
                    .SelectMany(g => g.G, (x, c) => c)
                    .SelectMany(s => s.S, (x, c) => c)
                    .SelectMany(x => x.X, (x, c) => c);

                Assert(xQuery,
                    0,
                    "{ $unwind : '$G' }",
                    "{ $project : { G : '$G', _id : 0 } }",
                    "{ $unwind : '$G.S' }",
                    "{ $project : { S : '$G.S', _id : 0 } }",
                    "{ $unwind : '$S.X' }",
                    "{ $project : { X : '$S.X', _id : 0 } }");
            });
        }

        [Fact]
        public void SelectMany_with_collection_selector_syntax_simple_scalar()
        {
            Execute(session =>
            {
                var query = from x in CreateQuery(session)
                            from y in x.G
                            select y;

                Assert(query,
                    6,
                    "{ $unwind: '$G' }",
                    "{ $project: { G: '$G', _id: 0 } }");
            });
        }

        [Fact]
        public void SelectMany_with_collection_selector_syntax_simple_scalar_which_is_called_from_SelectMany()
        {
            Execute(session =>
            {
                var selectMany1 = from x in CreateQuery(session)
                                  from g in x.G
                                  select g;
                var selectMany2 = from g in selectMany1
                                  from s in g.S
                                  select s;

                Assert(selectMany2,
                    2,
                    "{ $unwind : '$G' }",
                    "{ $project : { G : '$G', _id : 0 } }",
                    "{ $unwind : '$G.S' }",
                    "{ $project : { S : '$G.S', _id : 0 } }");
            });
        }

        [Fact]
        public void SelectMany_with_collection_selector_method_computed_scalar()
        {
            Execute(session =>
            {
                var query = CreateQuery(session)
                    .SelectMany(x => x.G, (x, c) => x.C.E.F + c.E.F + c.E.H);

                Assert(query,
                    6,
                    "{ $unwind: '$G' }",
                    "{ $project: { __fld0: { $add: ['$C.E.F', '$G.E.F', '$G.E.H'] }, _id: 0 } }");
            });
        }

        [Fact]
        public void SelectMany_with_collection_selector_method_computed_scalar_which_is_called_from_SelectMany()
        {
            Execute(session =>
            {
                var query = CreateQuery(session)
                    .SelectMany(g => g.G)
                    .SelectMany(s => s.S, (x, c) => (int?)(x.E.F + c.E.F + c.E.H));

                Assert(query,
                    2,
                    "{ $unwind : '$G' }",
                    "{ $project : { G : '$G', _id : 0 } }",
                    "{ $unwind : '$G.S' }",
                    "{ $project : { __fld0 : { $add : ['$G.E.F', '$G.S.E.F', '$G.S.E.H'] }, _id : 0 } }");
            });
        }

        [Fact]
        public void SelectMany_with_collection_selector_syntax_computed_scalar()
        {
            Execute(session =>
            {
                var query = from x in CreateQuery(session)
                            from y in x.G
                            select x.C.E.F + y.E.F + y.E.H;

                Assert(query,
                    6,
                    "{ $unwind: '$G' }",
                    "{ $project: { __fld0: { $add: ['$C.E.F', '$G.E.F', '$G.E.H'] }, _id: 0 } }");
            });
        }

        [Fact]
        public void SelectMany_with_collection_selector_syntax_computed_scalar_which_is_called_from_SelectMany()
        {
            Execute(session =>
            {
                var selectMany1 = from x in CreateQuery(session)
                                  from g in x.G
                                  select g;
                var selectMany2 = from g in selectMany1
                                  from s in g.S
                                  select (int?)(g.E.F + s.E.F + s.E.H);

                Assert(selectMany2,
                    2,
                    "{ $unwind : '$G' }",
                    "{ $project : { G : '$G', _id : 0 } }",
                    "{ $unwind : '$G.S' }",
                    "{ $project: { __fld0 : { $add : ['$G.E.F', '$G.S.E.F', '$G.S.E.H'] }, _id : 0 } }");
            });
        }

        [Fact]
        public void SelectMany_with_collection_selector_method_anonymous_type()
        {
            Execute(session =>
            {
                var query = CreateQuery(session)
                    .SelectMany(x => x.G, (x, c) => new { x.C.E.F, Other = c.D });

                Assert(query,
                    6,
                    "{ $unwind: '$G' }",
                    "{ $project: { F: '$C.E.F', Other: '$G.D', _id: 0 } }");
            });
        }

        [Fact]
        public void SelectMany_with_collection_selector_method_anonymous_type_which_is_called_from_SelectMany()
        {
            Execute(session =>
            {
                var query = CreateQuery(session)
                    .SelectMany(g => g.G)
                    .SelectMany(s => s.S, (x, c) => new { x.E.F, Other = c.D });

                Assert(query,
                    2,
                    "{ $unwind : '$G' }",
                    "{ $project : { G : '$G', _id : 0 } }",
                    "{ $unwind : '$G.S' }",
                    "{ $project : { F : '$G.E.F', Other : '$G.S.D', _id : 0 } }");
            });
        }

        [Fact]
        public void SelectMany_with_collection_selector_syntax_anonymous_type()
        {
            Execute(session =>
            {
                var query = from x in CreateQuery(session)
                            from y in x.G
                            select new { x.C.E.F, Other = y.D };

                Assert(query,
                    6,
                    "{ $unwind: '$G' }",
                    "{ $project: { F: '$C.E.F', Other: '$G.D', _id: 0 } }");
            });
        }

        [Fact]
        public void SelectMany_with_collection_selector_syntax_anonymous_type_which_is_called_from_SelectMany()
        {
            Execute(session =>
            {
                var selectMany1 = from x in CreateQuery(session)
                                  from g in x.G
                                  select g;
                var selectMany2 = from g in selectMany1
                                  from s in g.S
                                  select new { g.E.F, Other = s.D };

                Assert(selectMany2,
                    2,
                    "{ $unwind : '$G' }",
                    "{ $project : { G : '$G', _id : 0 } }",
                    "{ $unwind : '$G.S' }",
                    "{ $project : { F : '$G.E.F', Other : '$G.S.D', _id : 0 } }");
            });
        }

        [Fact]
        public void SelectMany_followed_by_a_group()
        {
            Execute(session =>
            {
                var first = from x in CreateQuery(session)
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
                    6,
                    "{ $unwind: '$G' }",
                    "{ $project: { G: '$G', _id: 0 } }",
                    "{ $group: { _id: '$G.D', __agg0: { $sum : '$G.E.F' } } }",
                    "{ $project: { Key: '$_id', SumF: '$__agg0', _id: 0 } }");
            });
        }

        [Fact]
        public void SelectMany_followed_by_a_group_which_is_called_from_SelectMany()
        {
            Execute(session =>
            {
                var selectMany1 = from x in CreateQuery(session)
                                  from g in x.G
                                  select g;
                var selectMany2 = from g in selectMany1
                                  from s in g.S
                                  select s;
                var query = from s in selectMany2
                            group s by s.D into g
                            select new
                            {
                                g.Key,
                                SumF = g.Sum(x => x.E.F)
                            };

                Assert(query,
                    2,
                    "{ $unwind : '$G' }",
                    "{ $project : { G: '$G', _id : 0 } }",
                    "{ $unwind : '$G.S' }",
                    "{ $project : { 'S' : '$G.S', '_id' : 0 } }",
                    "{ $group : { _id : '$S.D', __agg0 : { $sum : '$S.E.F' } } }",
                    "{ $project : { Key : '$_id', SumF : '$__agg0', _id : 0 } }");
            });
        }

        [Fact]
        public void Single()
        {
            Execute(session =>
            {
                var result = CreateQuery(session).Where(x => x.Id == 30).Select(x => x.C.E.F).Single();

                result.Should().Be(1111);
            });
        }

        [Fact]
        public void Single_with_predicate()
        {
            Execute(session =>
            {
                var result = CreateQuery(session).Select(x => x.C.E.F).Single(x => x == 1111);

                result.Should().Be(1111);
            });
        }

        [Fact]
        public async Task SingleAsync()
        {
            await ExecuteAsync(async session =>
            {
                var result = await CreateQuery(session).Where(x => x.Id == 30).Select(x => x.C.E.F).SingleAsync();

                result.Should().Be(1111);
            });
        }

        [Fact]
        public async Task SingleAsync_with_predicate()
        {
            await ExecuteAsync(async session =>
            {
                var result = await CreateQuery(session).Select(x => x.C.E.F).SingleAsync(x => x == 1111);

                result.Should().Be(1111);
            });
        }

        [Fact]
        public void SingleOrDefault()
        {
            Execute(session =>
            {
                var result = CreateQuery(session).Where(x => x.Id == 30).Select(x => x.C.E.F).SingleOrDefault();

                result.Should().Be(1111);
            });
        }

        [Fact]
        public void SingleOrDefault_with_predicate()
        {
            Execute(session =>
            {
                var result = CreateQuery(session).Select(x => x.C.E.F).SingleOrDefault(x => x == 1111);

                result.Should().Be(1111);
            });
        }

        [Fact]
        public async Task SingleOrDefaultAsync()
        {
            await ExecuteAsync(async session =>
            {
                var result = await CreateQuery(session).Where(x => x.Id == 30).Select(x => x.C.E.F).SingleOrDefaultAsync();

                result.Should().Be(1111);
            });
        }

        [Fact]
        public async Task SingleOrDefaultAsync_with_predicate()
        {
            await ExecuteAsync(async session =>
            {
                var result = await CreateQuery(session).Select(x => x.C.E.F).SingleOrDefaultAsync(x => x == 1111);

                result.Should().Be(1111);
            });
        }

        [Fact]
        public void Skip()
        {
            Execute(session =>
            {
                var query = CreateQuery(session).Skip(2);

                Assert(query,
                    1,
                    "{ $skip: 2 }");
            });
        }

        [SkippableFact]
        public void StandardDeviationPopulation()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");

            Execute(session =>
            {
                CleanCollection(session, x => x.Id == 10);

                var result = CreateQuery(session).Select(x => x.C.E.F).StandardDeviationPopulation();

                result.Should().Be(500);
            });
        }

        [SkippableFact]
        public void StandardDeviationPopulation_with_selector()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");

            Execute(session =>
            {
                CleanCollection(session, x => x.Id == 10);

                var result = CreateQuery(session).StandardDeviationPopulation(x => x.C.E.F);

                result.Should().Be(500);
            });
        }

        [SkippableFact]
        public async Task StandardDeviationPopulationAsync()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");

            await ExecuteAsync(async session =>
            {
                await CleanCollectionAsync(session, x => x.Id == 10);

                var result = await CreateQuery(session).Select(x => x.C.E.F).StandardDeviationPopulationAsync();

                result.Should().Be(500);
            });
        }

        [SkippableFact]
        public async Task StandardDeviationPopulationAsync_with_selector()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");

            await ExecuteAsync(async session =>
            {
                await CleanCollectionAsync(session, x => x.Id == 10);

                var result = await CreateQuery(session).StandardDeviationPopulationAsync(x => x.C.E.F);

                result.Should().Be(500);
            });
        }

        [SkippableFact]
        public void StandardDeviationSample()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");

            Execute(session =>
            {
                var result = CreateQuery(session).Select(x => x.C.E.F).StandardDeviationSample();

                result.Should().BeApproximately(608.276253029822, .0001);
            });
        }

        [SkippableFact]
        public void StandardDeviationSample_with_selector()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");

            Execute(session =>
            {
                var result = CreateQuery(session).StandardDeviationSample(x => x.C.E.F);

                result.Should().BeApproximately(608.276253029822, .0001);
            });
        }

        [SkippableFact]
        public async Task StandardDeviationSampleAsync()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");

            await ExecuteAsync(async session =>
            {
                var result = await CreateQuery(session).Select(x => x.C.E.F).StandardDeviationSampleAsync();

                result.Should().BeApproximately(608.276253029822, .0001);
            });
        }

        [SkippableFact]
        public async Task StandardDeviationSampleAsync_with_selector()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.2.0");

            await ExecuteAsync(async session =>
            {
                var result = await CreateQuery(session).StandardDeviationSampleAsync(x => x.C.E.F);

                result.Should().BeApproximately(608.276253029822, .0001);
            });
        }

        [Fact]
        public void Sum()
        {
            Execute(session =>
            {
                var result = CreateQuery(session).Select(x => x.C.E.F).Sum();

                result.Should().Be(1233);
            });
        }

        [Fact]
        public void Sum_with_selector()
        {
            Execute(session =>
            {
                var result = CreateQuery(session).Sum(x => x.C.E.F);

                result.Should().Be(1233);
            });
        }

        [Fact]
        public void Sum_with_no_results()
        {
            Execute(session =>
            {
                CleanCollection(session, x => x.Id == 10);

                var result = CreateQuery(session).Where(x => x.Id == 10).Sum(x => x.C.E.F);

                result.Should().Be(0);
            });
        }

        [Fact]
        public async Task SumAsync()
        {
            await ExecuteAsync(async session =>
            {
                var result = await CreateQuery(session).Select(x => x.C.E.F).SumAsync();

                result.Should().Be(1233);
            });
        }

        [Fact]
        public async Task SumAsync_with_selector()
        {
            await ExecuteAsync(async session =>
            {
                var result = await CreateQuery(session).SumAsync(x => x.C.E.F);

                result.Should().Be(1233);
            });
        }

        [Fact]
        public async Task SumAsync_with_no_results()
        {
            await ExecuteAsync(async session =>
            {
                await CleanCollectionAsync(session, x => x.Id == 10);

                var result = await CreateQuery(session).Where(x => x.Id == 10).SumAsync(x => x.C.E.F);

                result.Should().Be(0);
            });
        }

        [Fact]
        public void Take()
        {
            Execute(session =>
            {
                var query_in_transaction = CreateQuery(session).Take(3);

                Assert(query_in_transaction,
                    3,
                    "{ $limit: 3 }");

                var query_not_in_transaction = CreateQuery().Take(3);

                Assert(query_not_in_transaction,
                    2,
                    "{ $limit: 3 }");
            });
        }

        [Fact]
        public void Where_method()
        {
            Execute(session =>
            {
                var query = CreateQuery(session)
                .Where(x => x.A == "Astonishing");

                Assert(query,
                    1,
                    "{ $match: { A: 'Astonishing' } }");
            });
        }

        [Fact]
        public void Where_syntax()
        {
            Execute(session =>
            {
                var query = from x in CreateQuery(session)
                            where x.A == "Astonishing"
                            select x;

                Assert(query,
                    1,
                    "{ $match: { A: 'Astonishing' } }");
            });
        }

        [Fact]
        public void Where_method_with_predicated_any()
        {
            Execute(session =>
            {
                var query = CreateQuery(session)
                    .Where(x => x.G.Any(g => g.D == "Dock"));

                Assert(query,
                    1,
                    "{ $match : { 'G' : { '$elemMatch' : { 'D' : 'Dock' } } } }");
            });
        }

        private List<T> Assert<T>(IMongoQueryable<T> queryable, int resultCount, params string[] expectedStages)
        {
            var executionModel = (AggregateQueryableExecutionModel<T>)queryable.GetExecutionModel();

            executionModel.Stages.Should().Equal(expectedStages.Select(x => BsonDocument.Parse(x)));

            // async
            var results = queryable.ToListAsync().GetAwaiter().GetResult();
            results.Count.Should().Be(resultCount);

            // sync
            results = queryable.ToList();
            results.Count.Should().Be(resultCount);

            return results;
        }

        private IMongoQueryable<Root> CreateQuery(IClientSessionHandle session = null)
        {
            return __collection.AsQueryable(session);
        }

        private IMongoQueryable<Other> CreateOtherQuery(IClientSessionHandle session = null)
        {
            return __otherCollection.AsQueryable(session);
        }

        private void Execute(Action<IClientSessionHandle> action, bool insert = true)
        {
            using (var session = DriverTestConfiguration.Client.StartSession())
            {
                session.StartTransaction();
                try
                {
                    if (insert)
                        InsertThird(session);

                    action(session);
                }
                finally
                {
                    session.AbortTransaction();
                }
            }
        }

        private async Task ExecuteAsync(Func<IClientSessionHandle, Task> action, bool insert = true)
        {
            using (var session = DriverTestConfiguration.Client.StartSession())
            {
                session.StartTransaction();
                try
                {
                    if (insert)
                        await InsertThirdAsync(session);

                    await action(session);
                }
                finally
                {
                    session.AbortTransaction();
                }
            }
        }

        private void CleanCollection(IClientSessionHandle session)
        {
            __collection.DeleteMany(session, FilterDefinition<Root>.Empty);
        }

        private void CleanCollection(IClientSessionHandle session, Expression<Func<Root, bool>> filter)
        {
            __collection.DeleteMany(session, filter);
        }

        private async Task CleanCollectionAsync(IClientSessionHandle session)
        {
            await __collection.DeleteManyAsync(session, FilterDefinition<Root>.Empty);
        }

        private async Task CleanCollectionAsync(IClientSessionHandle session, Expression<Func<Root, bool>> filter)
        {
            await __collection.DeleteManyAsync(session, filter);
        }

        private Root CreateThird()
        {
            return new Root
            {
                A = "Astonishing",
                B = "Bamboo",
                C = new C
                {
                    D = "Duke Nukem",
                    E = new V
                    {
                        F = 1111,
                        H = 2222,
                        I = new[] { "item" },
                        W = 11111
                    },
                    X = new List<E> { new E { F = 100 }, new V { W = 123 } }
                },
                G = new[] {
                        new C
                        {
                            D = "Dock",
                            E = new E
                            {
                                F = 3333,
                                H = 4444,
                                I = new [] { "ignite"}
                            },
                            S = new [] {
                                    new C
                                    {
                                        D = "Deborah"
                                    }
                            },
                            Ids = new [] { new ObjectId("222222222222222222222222") }
                        },
                        new C
                        {
                            D = "Dove",
                            E = new E
                            {
                                F = 5555,
                                H = 6666,
                                I = new [] { "impossible"}
                            }
                        }
                },
                Id = 30,
                J = new DateTime(2013, 12, 1, 13, 14, 15, 16, DateTimeKind.Utc),
                K = true,
                L = new HashSet<int>(new[] { 2, 4, 6 }),
                M = new[] { 4, 6, 7 },
                O = new List<long> { 1000, 2000, 3000 },
                Q = Q.One,
                R = new DateTime(2014, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc),
                T = new Dictionary<string, int> { { "three", 3 }, { "four", 4 } },
                U = 0.123456571661743267789m,
                V = "2018-02-08T12:10:40.787"
            };
        }

        private void InsertThird(IClientSessionHandle session)
        {
            var root = CreateThird();
            __collection.InsertOne(session, root);
        }

        private async Task InsertThirdAsync(IClientSessionHandle session)
        {
            var root = CreateThird();
            await __collection.InsertOneAsync(session, root);
        }

        private void InsertJoin(IClientSessionHandle session)
        {
            __otherCollection.InsertOne(session, new Other
            {
                Id = 30 // will join with third
            });
        }
    }
}
