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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4734Tests : Linq3IntegrationTest
    {
        [Theory]
        [InlineData("{ X : false }", "{ _id : 1 }")]
        [InlineData("{ X : true }", "{ _id : 1, X : 2 }")]
        [InlineData("{ X : 0 }", "{ _id : 1 }")]
        [InlineData("{ X : 1 }", "{ _id : 1, X : 2 }")]
        [InlineData("{ X : -1 }", "{ _id : 1, X : 2 }")]
        [InlineData("{ X : { $numberLong : 0 } }", "{ _id : 1 }")]
        [InlineData("{ X : { $numberLong : 1 } }", "{ _id : 1, X : 2 }")]
        [InlineData("{ X : { $numberLong : -1 } }", "{ _id : 1, X : 2 }")]
        [InlineData("{ X : 0.0 }", "{ _id : 1 }")]
        [InlineData("{ X : 1.0 }", "{ _id : 1, X : 2 }")]
        [InlineData("{ X : -1.0 }", "{ _id : 1, X : 2 }")]
        public void Find_with_projections_that_should_work_on_all_server_versions(string projection, string expectedResult)
        {
            var collection = GetCollection("{ _id : 1, X : 2 }");

            var find = collection
                .Find("{}")
                .Project(projection);

            var result = find.Single();
            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("{}", "{ A : { $slice : 2 } }", "{ _id : 1, A : [1, 2] }")]
        [InlineData("{}", "{ A : { $slice : -2 } }", "{ _id : 1, A : [2, 3] }")]
        [InlineData("{}", "{ A : { $slice : [1, 2] } }", "{ _id : 1, A : [2, 3] }")]
        [InlineData("{}", "{ A : { $slice : [-3, 2] } }", "{ _id : 1, A : [1, 2] }")]
        [InlineData("{}", "{ A : { $elemMatch : { $gt : 1 } } }", "{ _id : 1, A : [2] }")]
        [InlineData("{ A : { $gt : 1 } }", "{ 'A.$' : true }", "{ _id : 1, A : [2] }")]
        [InlineData("{ A : { $gt : 1 } }", "{ 'A.$' : 1 }", "{ _id : 1, A : [2] }")]
        [InlineData("{ A : { $gt : 1 } }", "{ 'A.$' : -1 }", "{ _id : 1, A : [2] }")]
        [InlineData("{ A : { $gt : 1 } }", "{ 'A.$' : { $numberLong : 1 } }", "{ _id : 1, A : [2] }")]
        [InlineData("{ A : { $gt : 1 } }", "{ 'A.$' : { $numberLong : -1 } }", "{ _id : 1, A : [2] }")]
        [InlineData("{ A : { $gt : 1 } }", "{ 'A.$' : 1.0 }", "{ _id : 1, A : [2] }")]
        [InlineData("{ A : { $gt : 1 } }", "{ 'A.$' : -1.0 }", "{ _id : 1, A : [2] }")]
        public void Find_with_array_projection_that_should_work_on_all_server_versions(string filter, string projection, string expectedResult)
        {
            var collection = GetCollection("{ _id : 1, A : [1, 2, 3] }");

            var find = collection
                .Find(filter)
                .Project(projection);

            var result = find.Single();
            result.Should().BeEquivalentTo(expectedResult); // order of result elements varies by server version
        }

        [Theory]
        [InlineData("{ $text : { $search : 'coffee' } }", "{ score : { $meta : 'textScore' } }", "{ _id : 1, subject : 'coffee', score : 1.0 }")]
        public void Find_with_meta_projection_that_should_work_on_all_server_versions(string filter, string projection, string expectedResult)
        {
            var collection = GetCollection("{ _id : 1, subject : 'coffee' }");
            var keyDefinition = new IndexKeysDefinitionBuilder<BsonDocument>().Text("subject");
            var indexModel = new CreateIndexModel<BsonDocument>(keyDefinition);
            collection.Indexes.CreateOne(indexModel);

            var find = collection
                .Find(filter)
                .Project(projection);

            var result = find.Single();
            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("{ X : 'abc' }", "{ _id : 1, X : 'abc' }")]
        [InlineData("{ X : '$Y' }", "{ _id : 1, X : 3 }")]
        [InlineData("{ X : { $add : ['$X', '$Y'] } }", "{ _id : 1, X : 5 }")]
        [InlineData("{ X : { $literal : true } }", "{ _id : 1, X : true }")]
        [InlineData("{ X : { $literal : 1 } }", "{ _id : 1, X : 1 }")]
        [InlineData("{ X : { $literal : { $numberLong : 1 } } }", "{ _id : 1, X : { $numberLong : 1 } }")]
        [InlineData("{ X : { $literal : 1.0 } }", "{ _id : 1, X : 1.0 }")]
        public void Find_with_projections_that_should_work_only_on_servers_newer_than_44(string projection, string expectedResult)
        {
            var collection = GetCollection("{ _id : 1, X : 2, Y : 3 }");

            var find = collection
                .Find("{}")
                .Project(projection);

            var wireVersion = CoreTestConfiguration.MaxWireVersion;
            if (Feature.FindProjectionExpressions.IsSupported(wireVersion))
            {
                var result = find.Single();
                result.Should().BeEquivalentTo(expectedResult); // order of result elements varies by server version
            }
            else
            {
                var exception = Record.Exception(() => find.Single());
                exception.Should().BeOfType<NotSupportedException>();
                exception.Message.Should().Contain("is not supported with find on servers prior to version 4.4.");
            }
        }

        [Theory]
        [InlineData("{ X : { Y : 1 } }", "{ _id : 1, X : { Y : 2 } }")]
        [InlineData("{ X : { Y : 0 } }", "{ _id : 1, X : { Z : 3 } }")]
        [InlineData("{ X : { Y : { $numberLong : 1 } } }", "{ _id : 1, X : { Y : 2 } }")]
        [InlineData("{ X : { Y : 1.0 } }", "{ _id : 1, X : { Y : 2 } }")]
        public void Find_with_nested_field_projections_that_should_work_only_on_servers_newer_than_44(string projection, string expectedResult)
        {
            var collection = GetCollection("{ _id : 1, X : { Y : 2, Z : 3 } }");

            var find = collection
                .Find("{}")
                .Project(projection);

            var wireVersion = CoreTestConfiguration.MaxWireVersion;
            if (Feature.FindProjectionExpressions.IsSupported(wireVersion))
            {
                var result = find.Single();
                result.Should().BeEquivalentTo(expectedResult); // order of result elements varies by server version
            }
            else
            {
                var exception = Record.Exception(() => find.Single());
                exception.Should().BeOfType<NotSupportedException>();
                exception.Message.Should().Contain("is not supported with find on servers prior to version 4.4.");
            }
        }

        [Fact] // it is sufficient to test only one projection because the rest are tested using Find
        public void FindOneAndDelete_with_projections_that_should_work_only_on_servers_newer_than_44()
        {
            var collection = GetCollection("{ _id : 1, X : 2, Y : 3 }");
            var filter = "{ _id : 1 }";
            var options = new FindOneAndDeleteOptions<BsonDocument> { Projection = "{ Z : '$Y' }" };

            var wireVersion = CoreTestConfiguration.MaxWireVersion;
            if (Feature.FindProjectionExpressions.IsSupported(wireVersion))
            {
                var result = collection.FindOneAndDelete(filter, options);
                result.Should().BeEquivalentTo("{ _id : 1, Z : 3 }"); // order of result elements varies by server version
            }
            else
            {
                var exception = Record.Exception(() => collection.FindOneAndDelete(filter, options));
                exception.Should().BeOfType<NotSupportedException>();
                exception.Message.Should().Contain("is not supported with find on servers prior to version 4.4.");
            }
        }

        [Fact] // it is sufficient to test only one projection because the rest are tested using Find
        public void FindOneAndReplace_with_projections_that_should_work_only_on_servers_newer_than_44()
        {
            var collection = GetCollection("{ _id : 1, X : 2, Y : 3 }");
            var filter = "{ _id : 1 }";
            var replacement = BsonDocument.Parse("{ _id : 1, X : 4, Y : 5 }");
            var options = new FindOneAndReplaceOptions<BsonDocument> { Projection = "{ Z : '$Y' }", ReturnDocument = ReturnDocument.After };

            var wireVersion = CoreTestConfiguration.MaxWireVersion;
            if (Feature.FindProjectionExpressions.IsSupported(wireVersion))
            {
                var result = collection.FindOneAndReplace(filter, replacement, options);
                result.Should().BeEquivalentTo("{ _id : 1, Z : 5 }"); // order of result elements varies by server version
            }
            else
            {
                var exception = Record.Exception(() => collection.FindOneAndReplace(filter, replacement, options));
                exception.Should().BeOfType<NotSupportedException>();
                exception.Message.Should().Contain("is not supported with find on servers prior to version 4.4.");
            }
        }

        [Fact] // it is sufficient to test only one projection because the rest are tested using Find
        public void FindOneAndUpdate_with_projections_that_should_work_only_on_servers_newer_than_44()
        {
            var collection = GetCollection("{ _id : 1, X : 2, Y : 3 }");
            var filter = "{ _id : 1 }";
            var update = "{ $inc : { Y : 1 } }";
            var options = new FindOneAndUpdateOptions<BsonDocument> { Projection = "{ Z : '$Y' }", ReturnDocument = ReturnDocument.After };

            var wireVersion = CoreTestConfiguration.MaxWireVersion;
            if (Feature.FindProjectionExpressions.IsSupported(wireVersion))
            {
                var result = collection.FindOneAndUpdate(filter, update, options);
                result.Should().BeEquivalentTo("{ _id : 1, Z : 4 }"); // order of result elements varies by server version
            }
            else
            {
                var exception = Record.Exception(() => collection.FindOneAndUpdate(filter, update, options));
                exception.Should().BeOfType<NotSupportedException>();
                exception.Message.Should().Contain("is not supported with find on servers prior to version 4.4.");
            }
        }

        private IMongoCollection<BsonDocument> GetCollection(params string[] documents)
        {
            var collection = GetCollection<BsonDocument>("test");
            CreateCollection(
                collection,
                documents.Select(BsonDocument.Parse));
            return collection;
        }
    }
}
