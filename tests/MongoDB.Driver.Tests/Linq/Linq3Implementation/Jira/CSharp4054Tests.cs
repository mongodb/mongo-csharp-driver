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
using FluentAssertions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4054Tests : Linq3IntegrationTest
    {
        private readonly static Movie __movie21 = new Movie { Id = 21 };
        private readonly static Movie __movie31 = new Movie { Id = 31 };
        private readonly static Movie __movie32 = new Movie { Id = 32 };
        private readonly static Person __person1 = new Person { Id = 1, MovieIds = new int[] { } };
        private readonly static Person __person2 = new Person { Id = 2, MovieIds = new int[] { 21 } };
        private readonly static Person __person3 = new Person { Id = 3, MovieIds = new int[] { 31, 32 } };

        [Fact]
        public void Join_should_work()
        {
            var people = CreatePeopleCollection();
            var movies = CreateMoviesCollection();

            var queryable =
                from person in people.AsQueryable()
                from movieId in person.MovieIds
                join movie in movies.AsQueryable() on movieId equals movie.Id
                select new { person, movie };

            var stages = Translate(people, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : { $map : { input : '$MovieIds', as : 'movieId', in : { person : '$$ROOT', movieId : '$$movieId' } } }, _id : 0 } }",
                "{ $unwind : '$_v' }",
                "{ $project : { _outer : '$_v', _id : 0 } }",
                "{ $lookup : { from : 'movies', localField : '_outer.movieId', foreignField : '_id', as : '_inner' } }",
                "{ $unwind : '$_inner' }",
                "{ $project : { person : '$_outer.person', movie : '$_inner', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(3);
            results[0].person.Should().Be(__person2);
            results[0].movie.Should().Be(__movie21);
            results[1].person.Should().Be(__person3);
            results[1].movie.Should().Be(__movie31);
            results[2].person.Should().Be(__person3);
            results[2].movie.Should().Be(__movie32);
        }

        private IMongoCollection<Person> CreatePeopleCollection()
        {
            var collection = GetCollection<Person>("people");
            CreateCollection(collection, __person1, __person2, __person3);
            return collection;
        }

        private IMongoCollection<Movie> CreateMoviesCollection()
        {
            var collection = GetCollection<Movie>("movies");
            CreateCollection(collection, __movie21, __movie31, __movie32);
            return collection;
        }

        private class Person
        {
            public int Id { get; set; }
            public IEnumerable<int> MovieIds { get; set; }

            public override bool Equals(object obj) =>
                obj is Person other &&
                Id == other.Id &&
                MovieIds.SequenceEqual(other.MovieIds);

            public override int GetHashCode() => 0;
        }

        class Movie
        {
            public int Id { get; set; }

            public override bool Equals(object obj) =>
                obj is Movie other &&
                Id == other.Id;

            public override int GetHashCode() => 0;
        }
    }
}
