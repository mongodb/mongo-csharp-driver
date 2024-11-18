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

using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4053Tests : Linq3IntegrationTest
    {
        [Fact]
        public void SelectMany_Where_Select_should_work()
        {
            var people = GetPersonCollection();
            var movies = GetMovieCollection();

            var queryable =
                from movie in movies.AsQueryable()
                from person in people.AsQueryable(new AggregateOptions())
                where person.MovieIds.Contains(movie.Id)
                select new { movie, person };

            var stages = Translate(movies, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : { $map : { input : [{ _id : 1, MovieIds : [1, 2] }, { _id : 2, MovieIds : [2, 3] }], as : 'person', in : { movie : '$$ROOT', person : '$$person' } } }, _id : 0 }  }",
                "{ $unwind : '$_v' }",
                "{ $match : { $expr : { $in : ['$_v.movie._id', '$_v.person.MovieIds'] } } }",
                "{ $project : { movie : '$_v.movie', person : '$_v.person', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(4);
            results[0].ToJson().Should().Be("""{ "movie" : { "_id" : 1 }, "person" : { "_id" : 1, "MovieIds" : [1, 2] } }""");
            results[1].ToJson().Should().Be("""{ "movie" : { "_id" : 2 }, "person" : { "_id" : 1, "MovieIds" : [1, 2] } }""");
            results[2].ToJson().Should().Be("""{ "movie" : { "_id" : 2 }, "person" : { "_id" : 2, "MovieIds" : [2, 3] } }""");
            results[3].ToJson().Should().Be("""{ "movie" : { "_id" : 3 }, "person" : { "_id" : 2, "MovieIds" : [2, 3] } }""");
        }

        private IMongoCollection<Person> GetPersonCollection()
        {
            var collection = GetCollection<Person>("people");
            CreateCollection(
                collection,
                new Person { Id = 1, MovieIds = [1, 2] },
                new Person { Id = 2, MovieIds = [2, 3] });
            return collection;
        }

        private IMongoCollection<Movie> GetMovieCollection()
        {
            var collection = GetCollection<Movie>("movies");
            CreateCollection(
                collection,
                new Movie { Id = 1 },
            new Movie { Id = 2 },
                new Movie { Id = 3 });
            return collection;
        }

        private class Person
        {
            public int Id { get; set; }
            public int[] MovieIds { get; set; }
        }

        private class Movie
        {
            public int Id { get; set; }
        }
    }
}
