﻿/* Copyright 2010-2014 MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Core.Operations;
using NUnit.Framework;

namespace MongoDB.Driver.Tests
{
    [TestFixture]
    public class WriteModelTests
    {
        [Test]
        public void Should_convert_from_InsertRequest_to_BsonDocument()
        {
            var document = BsonDocument.Parse("{a:1}");
            var request = new InsertRequest(new BsonDocumentWrapper(document));

            var result = WriteModel<BsonDocument>.FromCore(request);

            result.Should().BeOfType<InsertOneModel<BsonDocument>>();
            var insertModel = (InsertOneModel<BsonDocument>)result;
            insertModel.Document.Should().BeSameAs(document);
        }

        [Test]
        public void Should_convert_from_InsertRequest_to_Class()
        {
            var document = new TestClass { a = 1 };
            var request = new InsertRequest(new BsonDocumentWrapper(document));

            var result = WriteModel<TestClass>.FromCore(request);

            result.Should().BeOfType<InsertOneModel<TestClass>>();
            var model = (InsertOneModel<TestClass>)result;
            model.Document.Should().BeSameAs(document);
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void Should_convert_from_UpdateRequest_to_ReplaceOne_with_BsonDocument(bool isUpsert)
        {
            var criteria = Query.EQ("a", 1);
            var replacement = BsonDocument.Parse("{a:2}");
            var request = new UpdateRequest(UpdateType.Update,
                new BsonDocumentWrapper(criteria),
                new BsonDocumentWrapper(replacement))
            {
                IsMulti = false,
                IsUpsert = isUpsert
            };

            var result = WriteModel<BsonDocument>.FromCore(request);

            result.Should().BeOfType<ReplaceOneModel<BsonDocument>>();
            var model = (ReplaceOneModel<BsonDocument>)result;
            model.Criteria.Should().BeSameAs(criteria);
            model.Replacement.Should().BeSameAs(replacement);
            model.IsUpsert.Should().Be(isUpsert);
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void Should_convert_from_UpdateRequest_to_ReplaceOne_with_Class(bool isUpsert)
        {
            var criteria = Query.EQ("a", 1);
            var replacement = new TestClass { a = 2 };
            var request = new UpdateRequest(UpdateType.Replacement,
                new BsonDocumentWrapper(criteria),
                new BsonDocumentWrapper(replacement))
            {
                IsMulti = false,
                IsUpsert = isUpsert
            };

            var result = WriteModel<TestClass>.FromCore(request);

            result.Should().BeOfType<ReplaceOneModel<TestClass>>();
            var model = (ReplaceOneModel<TestClass>)result;
            model.Criteria.Should().BeSameAs(criteria);
            model.Replacement.Should().BeSameAs(replacement);
            model.IsUpsert.Should().Be(isUpsert);
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void Should_convert_from_UpdateRequest_to_UpdateOne_with_wrappers(bool isUpsert)
        {
            var criteria = Query.EQ("a", 1);
            var update = Update.Set("a", 2);
            var request = new UpdateRequest(UpdateType.Update,
                new BsonDocumentWrapper(criteria),
                new BsonDocumentWrapper(update))
            {
                IsMulti = false,
                IsUpsert = isUpsert
            };

            var result = WriteModel<BsonDocument>.FromCore(request);

            result.Should().BeOfType<UpdateOneModel<BsonDocument>>();
            var model = (UpdateOneModel<BsonDocument>)result;
            model.Criteria.Should().BeSameAs(criteria);
            model.Update.Should().BeSameAs(update);
            model.IsUpsert.Should().Be(isUpsert);
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void Should_convert_from_UpdateRequest_to_UpdateMany(bool isUpsert)
        {
            var criteria = BsonDocument.Parse("{a:1}");
            var update = BsonDocument.Parse("{$set: {a:2}}");
            var request = new UpdateRequest(UpdateType.Update,
                new BsonDocumentWrapper(criteria),
                new BsonDocumentWrapper(update))
            {
                IsMulti = true,
                IsUpsert = isUpsert
            };

            var result = WriteModel<BsonDocument>.FromCore(request);

            result.Should().BeOfType<UpdateManyModel<BsonDocument>>();
            var model = (UpdateManyModel<BsonDocument>)result;
            model.Criteria.Should().BeSameAs(criteria);
            model.Update.Should().BeSameAs(update);
            model.IsUpsert.Should().Be(isUpsert);
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void Should_convert_from_UpdateRequest_to_UpdateMany_with_wrappers(bool isUpsert)
        {
            var criteria = Query.EQ("a", 1);
            var update = Update.Set("a", 2);
            var request = new UpdateRequest(UpdateType.Update,
                new BsonDocumentWrapper(criteria),
                new BsonDocumentWrapper(update))
            {
                IsMulti = true,
                IsUpsert = isUpsert
            };

            var result = WriteModel<BsonDocument>.FromCore(request);

            result.Should().BeOfType<UpdateManyModel<BsonDocument>>();
            var model = (UpdateManyModel<BsonDocument>)result;
            model.Criteria.Should().BeSameAs(criteria);
            model.Update.Should().BeSameAs(update);
            model.IsUpsert.Should().Be(isUpsert);
        }

        private class TestClass
        {
            public int a;
        }
    }
}
