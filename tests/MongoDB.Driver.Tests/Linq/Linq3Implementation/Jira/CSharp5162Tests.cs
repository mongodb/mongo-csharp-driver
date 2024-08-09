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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp5162Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Builders_Projection_Expression_with_camel_casing_should_work()
        {
            var collection = GetCamelCollection();

            var projection = Builders<CamelDocument>.Projection.Expression(x => new CamelDocument { Id = x.Id, Name = x.Name });
            var aggregate = collection.Aggregate().Project(projection);

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $project : { _id : '$_id', name : '$name' } }");

            var result = aggregate.ToList().Single();
            result.Id.Should().Be(1);
            result.Name.Should().Be("John Doe");
        }

        [Fact]
        public void Builders_Projection_Expression_with_pascal_casing_should_work()
        {
            var collection = GetPascalCollection();

            var projection = Builders<PascalDocument>.Projection.Expression(x => new PascalDocument { Id = x.Id, Name = x.Name });
            var aggregate = collection.Aggregate().Project(projection);

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $project : { _id : '$_id', Name : '$Name' } }");

            var result = aggregate.ToList().Single();
            result.Id.Should().Be(1);
            result.Name.Should().Be("John Doe");
        }

        [Fact]
        public void FindExpressionDefinition_with_camel_casing_should_work()
        {
            var collection = GetCamelCollection();

            var projection = new FindExpressionProjectionDefinition<CamelDocument, CamelDocument>(x => new CamelDocument { Id = x.Id, Name = x.Name });
            var aggregate = collection.Aggregate().Project(projection);

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $project : { _id : '$_id', name : '$name' } }");

            var result = aggregate.ToList().Single();
            result.Id.Should().Be(1);
            result.Name.Should().Be("John Doe");
        }

        [Fact]
        public void FindExpressionDefinition_with_pascal_casing_should_work()
        {
            var collection = GetPascalCollection();

            var projection = new FindExpressionProjectionDefinition<PascalDocument, PascalDocument>(x => new PascalDocument { Id = x.Id, Name = x.Name });
            var aggregate = collection.Aggregate().Project(projection);

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $project : { _id : '$_id', Name : '$Name' } }");

            var result = aggregate.ToList().Single();
            result.Id.Should().Be(1);
            result.Name.Should().Be("John Doe");
        }

        private IMongoCollection<CamelDocument> GetCamelCollection()
        {
            var collection = GetCollection<CamelDocument>("test");
            var document = new CamelDocument { Id = 1, Name = "John Doe" };
            CreateCollection(collection, document);
            return collection;
        }

        private IMongoCollection<PascalDocument> GetPascalCollection()
        {
            var collection = GetCollection<PascalDocument>("test");
            var document = new PascalDocument { Id = 1, Name = "John Doe" };
            CreateCollection(collection, document);
            return collection;
        }

        private List<BsonDocument> NormalizeProjectFieldOrder(List<BsonDocument> stages)
        {
            if (stages.Count == 1 &&
                stages[0] is BsonDocument projectStage &&
                projectStage.ElementCount == 1 &&
                projectStage.GetElement(0).Name == "$project" &&
                projectStage[0] is BsonDocument projection &&
                projection.ElementCount == 2 &&
                projection.Names.SequenceEqual(["Name", "_id"]))
            {
                stages[0]["$project"] = new BsonDocument
                {
                    { "_id", projection["_id"] },
                    { "Name", projection["Name"] }
                };
            }

            return stages;
        }

        private class CamelDocument
        {
            public int Id { get; set; }
            [BsonElement("name")] public string Name { get; set; }
            [BsonElement("activeSince")] public DateTime ActiveSince { get; set; }
            [BsonElement("isActive")] public bool IsActive { get; set; }
        }

        private class PascalDocument
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public DateTime ActiveSince { get; set; }
            public bool IsActive { get; set; }
        }
    }
}
