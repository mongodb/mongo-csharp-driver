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
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4234Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void AppendStage_should_work([Values(false, true)] bool useResultSerializer)
        {
            var collection = CreateProductsCollection();
            var textStage = "{ $match : { $text : { $search : 'apples' } } }";
            var resultSerializer = useResultSerializer ? collection.DocumentSerializer : null;

            var queryable = collection
                .AsQueryable()
                .AppendStage(BsonDocument.Parse(textStage), resultSerializer);

            var stages = Translate(collection, queryable);
            AssertStages(stages, textStage);

            var results = queryable.ToList();
            results.Select(r => r.Id).Should().Equal(1);
        }

        private IMongoCollection<Product> CreateProductsCollection()
        {
            var collection = GetCollection<Product>("products");
            var database = collection.Database;

            CreateCollection(
                collection,
                new Product { Id = 1, Name = "Apples" },
                new Product { Id = 2, Name = "Oranges" });

            var keys = Builders<Product>.IndexKeys.Text(p => p.Name);
            var model = new CreateIndexModel<Product>(keys);
            collection.Indexes.CreateOne(model);

            return collection;
        }

        public class Product
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
