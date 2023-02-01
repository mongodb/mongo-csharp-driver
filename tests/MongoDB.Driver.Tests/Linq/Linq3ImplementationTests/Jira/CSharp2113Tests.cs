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
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    [Collection(RegisterObjectSerializerFixture.CollectionName)]
    public class CSharp2113Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Query1_should_work()
        {
            var collection = CreateDocumentCollection();

            var queryable = collection
                .AsQueryable()
                .Where(x => x.X == 1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { X : 1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Query2_should_work()
        {
            var collection = CreateDocumentCollection();

            var queryable = collection
                .AsQueryable()
                .Where(x => x.Inner.Y == 1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'Inner.Y' : 1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Query3_should_work()
        {
            var collection = CreateIDocumentCollection();

            var queryable = collection
                .AsQueryable()
                .Where(x => x.X == 1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { X : 1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Query4_should_work()
        {
            var collection = CreateIDocumentCollection();

            var queryable = collection
                .AsQueryable()
                .Where(x => x.Inner.Y == 1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'Inner.Y' : 1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        private IMongoCollection<Document> CreateDocumentCollection()
        {
            var collection = GetCollection<Document>();

            CreateCollection(
                collection,
                new Document { Id = 1, X = 1, Inner = new Inner { Y = 1 } },
                new Document { Id = 2, X = 2, Inner = new Inner { Y = 2 } });

            return collection;
        }

        private IMongoCollection<IDocument> CreateIDocumentCollection()
        {
            var collection = GetCollection<IDocument>();

            CreateCollection(
                collection,
                new DocumentWithInterface { Id = 1, X = 1, Inner = new InnerWithInterface { Y = 1 } },
                new DocumentWithInterface { Id = 2, X = 2, Inner = new InnerWithInterface { Y = 2 } });

            return collection;
        }

        private class Document
        {
            public int Id { get; set; }
            public int X { get; set; }
            public Inner Inner { get; set; }
        }

        private class Inner
        {
            public int Y { get; set; }
        }

        private interface IDocument
        {
            int Id { get; set; }
            int X { get; set; }
            IInner Inner { get; set; }
        }

        private interface IInner
        {
            int Y { get; set; }
        }

        private class DocumentWithInterface : IDocument
        {
            public int Id { get; set; }
            public int X { get; set; }
            public IInner Inner { get; set; }
        }

        public class InnerWithInterface : IInner
        {
            public int Y { get; set; }
        }
    }
}
