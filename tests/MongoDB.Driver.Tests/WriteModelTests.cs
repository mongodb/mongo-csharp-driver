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
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using MongoDB.Bson;
using MongoDB.Driver.Core.Operations;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class WriteModelTests
    {
        [Fact]
        public void Should_convert_from_InsertRequest_to_BsonDocument()
        {
            var document = BsonDocument.Parse("{a:1}");
            var request = new InsertRequest(new BsonDocumentWrapper(document));

            var result = WriteModel<BsonDocument>.FromCore(request);

            result.ShouldBeOfType<InsertOneModel<BsonDocument>>();
            var insertModel = (InsertOneModel<BsonDocument>)result;
            insertModel.Document.ShouldBeSameAs(document);
        }

        [Fact]
        public void Should_convert_from_InsertRequest_to_Class()
        {
            var document = new TestClass { a = 1 };
            var request = new InsertRequest(new BsonDocumentWrapper(document));

            var result = WriteModel<TestClass>.FromCore(request);

            result.ShouldBeOfType<InsertOneModel<TestClass>>();
            var model = (InsertOneModel<TestClass>)result;
            model.Document.ShouldBeSameAs(document);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Should_convert_from_UpdateRequest_to_ReplaceOne_with_BsonDocument(bool isUpsert)
        {
            var filter = new BsonDocument("a", 1);
            var replacement = BsonDocument.Parse("{a:2}");
            var request = new UpdateRequest(UpdateType.Update,
                new BsonDocumentWrapper(filter),
                new BsonDocumentWrapper(replacement))
            {
                IsMulti = false,
                IsUpsert = isUpsert
            };

            var result = WriteModel<BsonDocument>.FromCore(request);

            result.ShouldBeOfType<ReplaceOneModel<BsonDocument>>();
            var model = (ReplaceOneModel<BsonDocument>)result;
            ((BsonDocumentFilterDefinition<BsonDocument>)model.Filter).Document.ShouldBe(filter);
            model.Replacement.ShouldBeSameAs(replacement);
            model.IsUpsert.ShouldBe(isUpsert);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Should_convert_from_UpdateRequest_to_ReplaceOne_with_Class(bool isUpsert)
        {
            var filter = new BsonDocument("a", 1);
            var replacement = new TestClass { a = 2 };
            var request = new UpdateRequest(UpdateType.Replacement,
                new BsonDocumentWrapper(filter),
                new BsonDocumentWrapper(replacement))
            {
                IsMulti = false,
                IsUpsert = isUpsert
            };

            var result = WriteModel<TestClass>.FromCore(request);

            result.ShouldBeOfType<ReplaceOneModel<TestClass>>();
            var model = (ReplaceOneModel<TestClass>)result;
            ((BsonDocumentFilterDefinition<TestClass>)model.Filter).Document.ShouldBe(filter);
            model.Replacement.ShouldBeSameAs(replacement);
            model.IsUpsert.ShouldBe(isUpsert);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Should_convert_from_UpdateRequest_to_UpdateMany(bool isUpsert)
        {
            var filter = BsonDocument.Parse("{a:1}");
            var update = BsonDocument.Parse("{$set: {a:2}}");
            var request = new UpdateRequest(UpdateType.Update,
                new BsonDocumentWrapper(filter),
                new BsonDocumentWrapper(update))
            {
                IsMulti = true,
                IsUpsert = isUpsert
            };

            var result = WriteModel<BsonDocument>.FromCore(request);

            result.ShouldBeOfType<UpdateManyModel<BsonDocument>>();
            var model = (UpdateManyModel<BsonDocument>)result;
            ((BsonDocumentFilterDefinition<BsonDocument>)model.Filter).Document.ShouldBeSameAs(filter);
            ((BsonDocumentUpdateDefinition<BsonDocument>)model.Update).Document.ShouldBeSameAs(update);
            model.IsUpsert.ShouldBe(isUpsert);
        }

        private class TestClass
        {
            public int a;
        }
    }
}
