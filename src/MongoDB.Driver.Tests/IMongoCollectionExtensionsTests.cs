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

using System;
using System.Linq.Expressions;
using System.Threading;
using MongoDB.Bson;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Tests
{
    [TestFixture]
    public class IMongoCollectionExtensionsTests
    {
        [Test]
        public void CountAsync_with_an_expression_should_call_collection_with_the_correct_filter()
        {
            var subject = CreateSubject();
            subject.CountAsync(x => x.FirstName == "Jack");

            subject.Received().CountAsync(Arg.Any<object>(), null, default(CancellationToken));
        }

        [Test]
        public void DeleteManyAsync_with_an_expression_should_call_collection_with_the_correct_filter()
        {
            var subject = CreateSubject();
            subject.DeleteManyAsync(x => x.FirstName == "Jack");

            subject.Received().DeleteManyAsync(Arg.Any<object>(), default(CancellationToken));
        }

        [Test]
        public void DeleteOneAsync_with_an_expression_should_call_collection_with_the_correct_filter()
        {
            var subject = CreateSubject();
            subject.DeleteOneAsync(x => x.FirstName == "Jack");

            subject.Received().DeleteOneAsync(Arg.Any<object>(), default(CancellationToken));
        }

        [Test]
        public void DistinctAsync_with_an_expression_should_call_collection_with_the_correct_filter()
        {
            var subject = CreateSubject();
            subject.DistinctAsync(x => x.LastName, x => x.FirstName == "Jack");

            var expectedFieldName = "LastName";

            subject.Received().DistinctAsync(
                expectedFieldName,
                Arg.Any<object>(),
                Arg.Is<DistinctOptions<string>>(opt => opt.ResultSerializer != null),
                default(CancellationToken));
        }

        [Test]
        public void FindOneAndDeleteAsync_with_an_expression_should_call_collection_with_the_correct_filter()
        {
            var subject = CreateSubject();
            subject.FindOneAndDeleteAsync(x => x.FirstName == "Jack");

            subject.Received().FindOneAndDeleteAsync(Arg.Any<object>(), null, default(CancellationToken));
        }

        [Test]
        public void FindOneAndDeleteAsync_with_an_expression_and_result_options_should_call_collection_with_the_correct_filter()
        {
            var subject = CreateSubject();
            var options = new FindOneAndDeleteOptions<BsonDocument>();
            subject.FindOneAndDeleteAsync(x => x.FirstName == "Jack", options);

            subject.Received().FindOneAndDeleteAsync<BsonDocument>(Arg.Any<object>(), options, default(CancellationToken));
        }

        [Test]
        public void FindOneAndReplaceAsync_with_an_expression_should_call_collection_with_the_correct_filter()
        {
            var subject = CreateSubject();
            var replacement = new Person();
            subject.FindOneAndReplaceAsync(x => x.FirstName == "Jack", replacement);

            subject.Received().FindOneAndReplaceAsync(Arg.Any<object>(), replacement, null, default(CancellationToken));
        }

        [Test]
        public void FindOneAndReplaceAsync_with_an_expression_and_result_options_should_call_collection_with_the_correct_filter()
        {
            var subject = CreateSubject();
            var replacement = new Person();
            var options = new FindOneAndReplaceOptions<BsonDocument>();
            subject.FindOneAndReplaceAsync(x => x.FirstName == "Jack", replacement, options);

            subject.Received().FindOneAndReplaceAsync<BsonDocument>(Arg.Any<object>(), replacement, options, default(CancellationToken));
        }

        [Test]
        public void FindOneAndUpdateAsync_with_an_expression_should_call_collection_with_the_correct_filter()
        {
            var subject = CreateSubject();
            var update = new BsonDocument();
            subject.FindOneAndUpdateAsync(x => x.FirstName == "Jack", update);

            subject.Received().FindOneAndUpdateAsync(Arg.Any<object>(), update, null, default(CancellationToken));
        }

        [Test]
        public void FindOneAndUpdateAsync_with_an_expression_and_result_options_should_call_collection_with_the_correct_filter()
        {
            var subject = CreateSubject();
            var update = new BsonDocument();
            var options = new FindOneAndUpdateOptions<BsonDocument>();
            subject.FindOneAndUpdateAsync(x => x.FirstName == "Jack", update, options);

            subject.Received().FindOneAndUpdateAsync<BsonDocument>(Arg.Any<object>(), update, options, default(CancellationToken));
        }

        [Test]
        public void ReplaceOneAsync_with_an_expression_should_call_collection_with_the_correct_filter()
        {
            var subject = CreateSubject();
            var replacement = new Person();
            subject.ReplaceOneAsync(x => x.FirstName == "Jack", replacement);

            subject.Received().ReplaceOneAsync(Arg.Any<object>(), replacement, null, default(CancellationToken));
        }

        [Test]
        public void UpdateManyAsync_with_an_expression_should_call_collection_with_the_correct_filter()
        {
            var subject = CreateSubject();
            var update = new BsonDocument();
            subject.UpdateManyAsync(x => x.FirstName == "Jack", update);


            subject.Received().UpdateManyAsync(Arg.Any<object>(), update, null, default(CancellationToken));
        }

        [Test]
        public void UpdateOneAsync_with_an_expression_should_call_collection_with_the_correct_filter()
        {
            var subject = CreateSubject();
            var update = new BsonDocument();
            subject.UpdateOneAsync(x => x.FirstName == "Jack", update);

            subject.Received().UpdateOneAsync(Arg.Any<object>(), update, null, default(CancellationToken));
        }

        private bool Matches(object o, BsonDocument doc)
        {
            return o.ToBsonDocument().Equals(doc);
        }

        private IMongoCollection<Person> CreateSubject()
        {
            var settings = new MongoCollectionSettings();
            var subject = Substitute.For<IMongoCollection<Person>>();
            subject.Settings.Returns(settings);

            return subject;
        }

        public class Person
        {
            public string FirstName;
            public string LastName;
            public int Age;
        }
    }
}
