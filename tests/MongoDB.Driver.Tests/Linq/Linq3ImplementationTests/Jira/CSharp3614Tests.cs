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
using System.Linq;
using FluentAssertions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp3614Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Test()
        {
            var collection = CreateBooksCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new BookDto
                {
                    Id = x.Id,
                    PageCount = x.PageCount,
                    Author = x.Author == null
                        ? null
                        : new AuthorDto
                        {
                            Id = x.Author.Id,
                            Name = x.Author.Name
                        }
                });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _id : '$_id', PageCount : '$PageCount', Author : { $cond : { if : { $eq : ['$Author', null] }, then : null, else : { _id : '$Author._id', Name : '$Author.Name' } } } } }");

            var results = queryable.ToList().OrderBy(r => r.Id).ToList();
            results.Should().HaveCount(2);
            results[0].Should().BeOfType<BookDto>();
            results[0].Id.Should().Be(1);
            results[0].PageCount.Should().Be(1);
            results[0].Author.Should().BeNull();
            results[1].Should().BeOfType<BookDto>();
            results[1].Id.Should().Be(2);
            results[1].PageCount.Should().Be(2);
            results[1].Author.Should().BeOfType<AuthorDto>();
            results[1].Author.ShouldBeEquivalentTo(new AuthorDto { Id = 2, Name = "Two" });
        }

        private IMongoCollection<Book> CreateBooksCollection()
        {
            var collection = GetCollection<Book>();

            var documents = new[]
            {
                new Book { Id = 1, PageCount = 1, Author = null },
                new Book { Id = 2, PageCount = 2, Author = new Author { Id = 2, Name = "Two" } }
            };
            CreateCollection(collection, documents);

            return collection;
        }

        private class BookDto
        {
            public int Id { get; set; }
            public int PageCount { get; set; }
            public AuthorDto Author { get; set; }
        }

        private class AuthorDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        private class Author : IEquatable<Author>
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool Equals(Author other) => Id == other.Id && Name == other.Name;
        }

        private class Book
        {
            public int Id { get; set; }
            public int PageCount { get; set; }
            public Author Author { get; set; }
        }
    }
}
