/* Copyright 2010-2014 MongoDB Inc.
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

using System.Threading;
using MongoDB.Bson;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Tests
{
    [TestFixture]
    public class MongoCollectionExtensionsTests
    {
        private IMongoCollection<Person> _collection;

        [SetUp]
        public void SetUp()
        {
            var settings = new MongoCollectionSettings();
            _collection = Substitute.For<IMongoCollection<Person>>();
            _collection.Settings.Returns(settings);
        }

        [Test]
        public void CountAsync_with_an_expression_should_call_collection_with_the_correct_filter()
        {
            var result = _collection.CountAsync(x => x.FirstName == "Jack");

            var expectedFilter = new BsonDocument("FirstName", "Jack");

            _collection.Received().CountAsync(expectedFilter, null, default(CancellationToken));
        }

        [Test]
        public void DeleteManyAsync_with_an_expression_should_call_collection_with_the_correct_filter()
        {
            var result = _collection.DeleteManyAsync(x => x.FirstName == "Jack");

            var expectedFilter = new BsonDocument("FirstName", "Jack");

            _collection.Received().DeleteManyAsync(expectedFilter, default(CancellationToken));
        }

        [Test]
        public void DeleteOneAsync_with_an_expression_should_call_collection_with_the_correct_filter()
        {
            var result = _collection.DeleteOneAsync(x => x.FirstName == "Jack");

            var expectedFilter = new BsonDocument("FirstName", "Jack");

            _collection.Received().DeleteOneAsync(expectedFilter, default(CancellationToken));
        }

        [Test]
        public void DistinctAsync_with_an_expression_should_call_collection_with_the_correct_filter()
        {
            var result = _collection.DistinctAsync(x => x.LastName, x => x.FirstName == "Jack");

            var expectedFieldName = "LastName";
            var expectedFilter = new BsonDocument("FirstName", "Jack");

            _collection.Received().DistinctAsync(
                expectedFieldName,
                expectedFilter,
                Arg.Is<DistinctOptions<string>>(opt => opt.ResultSerializer != null),
                default(CancellationToken));
        }

        [Test]
        public void Find_with_an_expression_should_create_the_correct_find_fluent()
        {
            var fluent = _collection.Find(x => x.FirstName == "Jack");

            var expectedFilter = new BsonDocument("FirstName", "Jack");

            Assert.AreEqual(expectedFilter, fluent.Filter);
        }

        [Test]
        public void FindOneAndDeleteAsync_with_an_expression_should_call_collection_with_the_correct_filter()
        {
            var result = _collection.FindOneAndDeleteAsync(x => x.FirstName == "Jack");

            var expectedFilter = new BsonDocument("FirstName", "Jack");

            _collection.Received().FindOneAndDeleteAsync(expectedFilter, null, default(CancellationToken));
        }

        [Test]
        public void FindOneAndDeleteAsync_with_an_expression_and_result_options_should_call_collection_with_the_correct_filter()
        {
            var options = new FindOneAndDeleteOptions<BsonDocument>();
            var result = _collection.FindOneAndDeleteAsync(x => x.FirstName == "Jack", options);

            var expectedFilter = new BsonDocument("FirstName", "Jack");

            _collection.Received().FindOneAndDeleteAsync<BsonDocument>(expectedFilter, options, default(CancellationToken));
        }

        [Test]
        public void FindOneAndReplaceAsync_with_an_expression_should_call_collection_with_the_correct_filter()
        {
            var replacement = new Person();
            var result = _collection.FindOneAndReplaceAsync(x => x.FirstName == "Jack", replacement);

            var expectedFilter = new BsonDocument("FirstName", "Jack");

            _collection.Received().FindOneAndReplaceAsync(expectedFilter, replacement, null, default(CancellationToken));
        }

        [Test]
        public void FindOneAndReplaceAsync_with_an_expression_and_result_options_should_call_collection_with_the_correct_filter()
        {
            var replacement = new Person();
            var options = new FindOneAndReplaceOptions<BsonDocument>();
            var result = _collection.FindOneAndReplaceAsync(x => x.FirstName == "Jack", replacement, options);

            var expectedFilter = new BsonDocument("FirstName", "Jack");

            _collection.Received().FindOneAndReplaceAsync<BsonDocument>(expectedFilter, replacement, options, default(CancellationToken));
        }

        [Test]
        public void FindOneAndUpdateAsync_with_an_expression_should_call_collection_with_the_correct_filter()
        {
            var update = new BsonDocument();
            var result = _collection.FindOneAndUpdateAsync(x => x.FirstName == "Jack", update);

            var expectedFilter = new BsonDocument("FirstName", "Jack");

            _collection.Received().FindOneAndUpdateAsync(expectedFilter, update, null, default(CancellationToken));
        }

        [Test]
        public void FindOneAndUpdateAsync_with_an_expression_and_result_options_should_call_collection_with_the_correct_filter()
        {
            var update = new BsonDocument();
            var options = new FindOneAndUpdateOptions<BsonDocument>();
            var result = _collection.FindOneAndUpdateAsync(x => x.FirstName == "Jack", update, options);

            var expectedFilter = new BsonDocument("FirstName", "Jack");

            _collection.Received().FindOneAndUpdateAsync<BsonDocument>(expectedFilter, update, options, default(CancellationToken));
        }

        [Test]
        public void ReplaceOneAsync_with_an_expression_should_call_collection_with_the_correct_filter()
        {
            var replacement = new Person();
            var result = _collection.ReplaceOneAsync(x => x.FirstName == "Jack", replacement);

            var expectedFilter = new BsonDocument("FirstName", "Jack");

            _collection.Received().ReplaceOneAsync(expectedFilter, replacement, null, default(CancellationToken));
        }

        [Test]
        public void UpdateManyAsync_with_an_expression_should_call_collection_with_the_correct_filter()
        {
            var update = new BsonDocument();
            var result = _collection.UpdateManyAsync(x => x.FirstName == "Jack", update);

            var expectedFilter = new BsonDocument("FirstName", "Jack");

            _collection.Received().UpdateManyAsync(expectedFilter, update, null, default(CancellationToken));
        }

        [Test]
        public void UpdateOneAsync_with_an_expression_should_call_collection_with_the_correct_filter()
        {
            var update = new BsonDocument();
            var result = _collection.UpdateOneAsync(x => x.FirstName == "Jack", update);

            var expectedFilter = new BsonDocument("FirstName", "Jack");

            _collection.Received().UpdateOneAsync(expectedFilter, update, null, default(CancellationToken));
        }

        private IMongoCollection<TDocument> CreateCollection<TDocument>()
        {
            var settings = new MongoCollectionSettings();
            var subject = Substitute.For<IMongoCollection<TDocument>>();
            subject.Settings.Returns(settings);

            return subject;
        }

        public class Person
        {
            public string FirstName;
            public string LastName;
        }
    }
}
