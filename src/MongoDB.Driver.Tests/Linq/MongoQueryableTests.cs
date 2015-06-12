using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using FluentAssertions;
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
            var result = CreateQuery().Select(x => x.C.E.F).Average();

            result.Should().Be(61);
        }

        [Test]
        public void Average_with_selector()
        {
            var result = CreateQuery().Average(x => x.C.E.F);

            result.Should().Be(61);
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
        public void Distinct()
        {
            var query = CreateQuery().Select(x => x.C.E.F).Distinct();

            Assert(query,
                2,
                "{ $project: { 'C.E.F': 1, _id: 0 } }",
                "{ $group: { _id: '$C.E.F' } }");
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
        public void GroupBy_method()
        {
            var query = CreateQuery()
                .GroupBy(x => x.A);

            Assert(query,
                2,
                "{ $group: { _id: '$A' } }");
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
        [Ignore("Does not work - use a project in the middle")]
        public void GroupBy_groupby_where_with_nested_accumulators_method()
        {
            var query = CreateQuery()
                .GroupBy(x => x.A)
                .GroupBy(g => g.First().B)
                .Where(g2 => g2.Average(g => g.Sum(x => x.C.E.F)) == 10);

            Assert(query,
                1,
                "{ $group: { _id: '$A', __agg0: { $first: '$B' }, __agg1: { $sum: '$C.E.F'} } }",
                "{ $group: { _id: '$__agg0', _agg0: { $avg: '$__agg1' } } }",
                "{ $match: { _agg0: 10 } }");
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

            Assert(query,
                2,
                "{ $sort: { A: 1, B: 1 } }");
        }

        [Test]
        public void OrderBy_ThenBy_ThenByDescending_with_redundant_fields_in_different_directions_method()
        {
            var query = CreateQuery()
                .OrderBy(x => x.A)
                .ThenBy(x => x.B)
                .ThenByDescending(x => x.A);

            Assert(query,
                2,
                "{ $sort: { B: 1, A: -1 } }");
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
                "{ $project: { A: 1, _id: 0 } }",
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
                "{ $project: { A: 1, _id: 0 } }");
        }

        [Test]
        public void Select_syntax_scalar()
        {
            var query = from x in CreateQuery()
                        select x.A;

            Assert(query,
                2,
                "{ $project: { A: 1, _id: 0 } }");
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
                "{ $project: { M: 1, _id: 0 } }");
        }

        [Test]
        public void Select_syntax_array()
        {
            var query = from x in CreateQuery()
                        select x.M;

            Assert(query,
                2,
                "{ $project: { M: 1, _id: 0 } }");
        }

        [Test]
        public void Select_method_computed_array()
        {
            var query = CreateQuery()
                .Select(x => x.M.Select(i => i + 1));

            Assert(query,
                2,
                "{ $project: { __fld0: { $map: { input: '$M', as: 'i', in: { $add: ['$$i', 1] } } }, _id: 0 } }");
        }

        [Test]
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
                "{ $project: { G: 1, _id: 0 } }");
        }

        [Test]
        public void SelectMany_with_collection_selector_method_simple_scalar()
        {
            var query = CreateQuery()
                .SelectMany(x => x.G, (x, c) => c);

            Assert(query,
                4,
                "{ $unwind: '$G' }",
                "{ $project: { G: 1, _id: 0 } }");
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
                "{ $project: { G: 1, _id: 0 } }");
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

        private List<T> Assert<T>(IMongoQueryable<T> queryable, int resultCount, params string[] expectedStages)
        {
            var stages = ((AggregateQueryableExecutionModel<T>)queryable.BuildExecutionModel()).Stages;
            CollectionAssert.AreEqual(expectedStages.Select(x => BsonDocument.Parse(x)).ToList(), stages);

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
    }
}
