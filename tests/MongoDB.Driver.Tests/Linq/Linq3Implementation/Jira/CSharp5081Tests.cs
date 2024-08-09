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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp5081Tests : Linq3IntegrationTest
    {
        [Fact]
        public void SelectMany_chained_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .SelectMany(series => series.Books)
                .SelectMany(book => book.Chapters)
                .Select(chapter => chapter.Title);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : '$Books', _id : 0 } }",
                "{ $unwind : '$_v' }",
                "{ $project : { _v : '$_v.Chapters', _id : 0 } }",
                "{ $unwind : '$_v' }",
                "{ $project : { _v : '$_v.Title', _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(
                "Book 1 Chapter 1",
                "Book 1 Chapter 2",
                "Book 2 Chapter 1",
                "Book 2 Chapter 2",
                "Book 3 Chapter 1",
                "Book 3 Chapter 2");
        }

        [Theory]
        [ParameterAttributeData]
        public void SelectMany_nested_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = GetCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().SelectMany(series => series.Books.AsQueryable().SelectMany(book => book.Chapters.Select(chapter => chapter.Title))) :
                collection.AsQueryable().SelectMany(series => series.Books.SelectMany(book => book.Chapters.Select(chapter => chapter.Title)));

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : { $reduce : { input : { $map : { input : '$Books', as : 'book', in : '$$book.Chapters.Title' } }, initialValue : [], in : { $concatArrays : ['$$value', '$$this'] } } }, _id : 0 } }",
                "{ $unwind : '$_v' }");

            var results = queryable.ToList();
            results.Should().Equal(
                "Book 1 Chapter 1",
                "Book 1 Chapter 2",
                "Book 2 Chapter 1",
                "Book 2 Chapter 2",
                "Book 3 Chapter 1",
                "Book 3 Chapter 2");
        }

        [Theory]
        [ParameterAttributeData]
        public void Aggregate_Project_SelectMany_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = GetCollection();

            var aggregate = withNestedAsQueryable ?
                collection.Aggregate().Project(Series => Series.Books.AsQueryable().SelectMany(book => book.Chapters.Select(chapter => chapter.Title)).ToList()) :
                collection.Aggregate().Project(Series => Series.Books.SelectMany(book => book.Chapters.Select(chapter => chapter.Title)).ToList());

            var stages = Translate(collection, aggregate);
            AssertStages(
                stages,
                "{ $project : { _v : { $reduce : { input : { $map : { input : '$Books', as : 'book', in : '$$book.Chapters.Title' } }, initialValue : [], in : { $concatArrays : ['$$value', '$$this'] } } }, _id : 0 } }");

            var results = aggregate.ToList();
            results.Should().HaveCount(2);
            results[0].Should().Equal(
                "Book 1 Chapter 1",
                "Book 1 Chapter 2",
                "Book 2 Chapter 1",
                "Book 2 Chapter 2");
            results[1].Should().Equal(
                "Book 3 Chapter 1",
                "Book 3 Chapter 2");
        }

        private IMongoCollection<Series> GetCollection()
        {
            var collection = GetCollection<Series>("series");
            var document1 = new Series
            {
                Id = 1,
                Books = new List<Book>
                {
                    new Book
                    {
                        Title = "Book1",
                        Chapters = new List<Chapter>
                        {
                            new Chapter { Title = "Book 1 Chapter 1"},
                            new Chapter { Title = "Book 1 Chapter 2"}
                        }
                    },
                    new Book
                    {
                        Title = "Book2",
                        Chapters = new List<Chapter>
                        {
                            new Chapter { Title = "Book 2 Chapter 1"},
                            new Chapter { Title = "Book 2 Chapter 2"}
                        }
                    }
                }
            };
            var document2 = new Series
            {
                Id = 2,
                Books = new List<Book>
                {
                    new Book
                    {
                        Title = "Book3",
                        Chapters = new List<Chapter>
                        {
                            new Chapter { Title = "Book 3 Chapter 1"},
                            new Chapter { Title = "Book 3 Chapter 2"}
                        }
                    }
                }
            };
            CreateCollection(collection, document1, document2);
            return collection;
        }

        public class Series
        {
            public int Id { get; set; }
            public List<Book> Books { get; set; }
        }

        public class Book
        {
            public string Title { get; set; }
            public List<Chapter> Chapters { get; set; }
        }

        public class Chapter
        {
            public string Title { get; set; }
        }
    }
}
