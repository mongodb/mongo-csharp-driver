/* Copyright 2013-2014 MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class GroupOperationTests
    {
        [Test]
        public void constructor_with_key_should_initialize_subject()
        {
            var collectionNamespace = new CollectionNamespace("databaseName", "collectionName");
            var key = new BsonDocument("key", 1);
            var initial = new BsonDocument("x", 1);
            var reduceFunction = new BsonJavaScript("reduceFunction");
            var filter = new BsonDocument("y", 1);
            var messageEncoderSettings = new MessageEncoderSettings();

            var subject = new GroupOperation<BsonDocument>(collectionNamespace, key, initial, reduceFunction, filter, messageEncoderSettings);

            subject.CollectionNamespace.Should().Be(collectionNamespace);
            subject.Filter.Should().Be(filter);
            subject.FinalizeFunction.Should().BeNull();
            subject.Initial.Should().Be(initial);
            subject.Key.Should().Be(key);
            subject.KeyFunction.Should().BeNull();
            subject.MaxTime.Should().Be(default(TimeSpan?));
            Assert.That(subject.MessageEncoderSettings, Is.EqualTo(messageEncoderSettings));
            subject.ReduceFunction.Should().Be(reduceFunction);
            subject.ResultSerializer.Should().BeNull();
        }

        [Test]
        public void constructor_with_key_should_throw_when_collectionNamespace_is_null()
        {
            var key = new BsonDocument("key", 1);
            var initial = new BsonDocument("x", 1);
            var reduceFunction = new BsonJavaScript("reduceFunction");
            var filter = new BsonDocument("y", 1);
            var messageEncoderSettings = new MessageEncoderSettings();

            Action action = () => new GroupOperation<BsonDocument>(null, key, initial, reduceFunction, filter, messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void constructor_with_key_should_throw_when_initial_is_null()
        {
            var collectionNamespace = new CollectionNamespace("databaseName", "collectionName");
            var key = new BsonDocument("key", 1);
            var reduceFunction = new BsonJavaScript("reduceFunction");
            var filter = new BsonDocument("y", 1);
            var messageEncoderSettings = new MessageEncoderSettings();

            Action action = () => new GroupOperation<BsonDocument>(collectionNamespace, key, null, reduceFunction, filter, messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void constructor_with_key_should_throw_when_key_is_null()
        {
            var collectionNamespace = new CollectionNamespace("databaseName", "collectionName");
            var initial = new BsonDocument("x", 1);
            var reduceFunction = new BsonJavaScript("reduceFunction");
            var filter = new BsonDocument("y", 1);
            var messageEncoderSettings = new MessageEncoderSettings();

            Action action = () => new GroupOperation<BsonDocument>(collectionNamespace, (BsonDocument)null, initial, reduceFunction, filter, messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void constructor_with_key_should_throw_when_reduceFunction_is_null()
        {
            var collectionNamespace = new CollectionNamespace("databaseName", "collectionName");
            var key = new BsonDocument("key", 1);
            var initial = new BsonDocument("x", 1);
            var filter = new BsonDocument("y", 1);
            var messageEncoderSettings = new MessageEncoderSettings();

            Action action = () => new GroupOperation<BsonDocument>(collectionNamespace, key, initial, null, filter, messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void constructor_with_keyFunction_should_initialize_subject()
        {
            var collectionNamespace = new CollectionNamespace("databaseName", "collectionName");
            var keyFunction = new BsonJavaScript("keyFunction");
            var initial = new BsonDocument("x", 1);
            var reduceFunction = new BsonJavaScript("reduceFunction");
            var filter = new BsonDocument("y", 1);
            var messageEncoderSettings = new MessageEncoderSettings();

            var subject = new GroupOperation<BsonDocument>(collectionNamespace, keyFunction, initial, reduceFunction, filter, messageEncoderSettings);

            subject.CollectionNamespace.Should().Be(collectionNamespace);
            subject.Filter.Should().Be(filter);
            subject.FinalizeFunction.Should().BeNull();
            subject.Initial.Should().Be(initial);
            subject.Key.Should().BeNull();
            subject.KeyFunction.Should().Be(keyFunction);
            subject.MaxTime.Should().Be(default(TimeSpan?));
            Assert.That(subject.MessageEncoderSettings, Is.EqualTo(messageEncoderSettings));
            subject.ReduceFunction.Should().Be(reduceFunction);
            subject.ResultSerializer.Should().BeNull();
        }

        [Test]
        public void constructor_with_keyFunction_should_throw_when_collectionNamespace_is_null()
        {
            var keyFunction = new BsonJavaScript("keyFunction");
            var initial = new BsonDocument("x", 1);
            var reduceFunction = new BsonJavaScript("reduceFunction");
            var filter = new BsonDocument("y", 1);
            var messageEncoderSettings = new MessageEncoderSettings();

            Action action = () => new GroupOperation<BsonDocument>(null, keyFunction, initial, reduceFunction, filter, messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void constructor_with_keyFunction_should_throw_when_keyFunction_is_null()
        {
            var collectionNamespace = new CollectionNamespace("databaseName", "collectionName");
            var initial = new BsonDocument("x", 1);
            var reduceFunction = new BsonJavaScript("reduceFunction");
            var filter = new BsonDocument("y", 1);
            var messageEncoderSettings = new MessageEncoderSettings();

            Action action = () => new GroupOperation<BsonDocument>(collectionNamespace, (BsonJavaScript)null, initial, reduceFunction, filter, messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void constructor_with_keyFunction_should_throw_when_initial_is_null()
        {
            var collectionNamespace = new CollectionNamespace("databaseName", "collectionName");
            var keyFunction = new BsonJavaScript("keyFunction");
            var reduceFunction = new BsonJavaScript("reduceFunction");
            var filter = new BsonDocument("y", 1);
            var messageEncoderSettings = new MessageEncoderSettings();

            Action action = () => new GroupOperation<BsonDocument>(collectionNamespace, keyFunction, null, reduceFunction, filter, messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void constructor_with_keyFunction_should_throw_when_reduceFunction_is_null()
        {
            var collectionNamespace = new CollectionNamespace("databaseName", "collectionName");
            var keyFunction = new BsonJavaScript("keyFunction");
            var initial = new BsonDocument("x", 1);
            var filter = new BsonDocument("y", 1);
            var messageEncoderSettings = new MessageEncoderSettings();

            Action action = () => new GroupOperation<BsonDocument>(collectionNamespace, keyFunction, initial, null, filter, messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void CreateCommand_should_return_expected_result()
        {
            var collectionNamespace = new CollectionNamespace("databaseName", "collectionName");
            var key = new BsonDocument("key", 1);
            var initial = new BsonDocument("x", 1);
            var reduceFunction = new BsonJavaScript("reduceFunction");
            var messageEncoderSettings = new MessageEncoderSettings();
            var subject = new GroupOperation<BsonDocument>(collectionNamespace, key, initial, reduceFunction, null, messageEncoderSettings);
            var expectedResult = new BsonDocument
            {
                { "group", new BsonDocument
                    {
                        { "ns", collectionNamespace.CollectionName },
                        { "key", key },
                        { "$reduce", reduceFunction },
                        { "initial", initial }
                    }
                }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateCommand_should_return_expected_result_when_filter_was_provided()
        {
            var collectionNamespace = new CollectionNamespace("databaseName", "collectionName");
            var key = new BsonDocument("key", 1);
            var initial = new BsonDocument("x", 1);
            var reduceFunction = new BsonJavaScript("reduceFunction");
            var filter = new BsonDocument("y", 1);
            var messageEncoderSettings = new MessageEncoderSettings();
            var subject = new GroupOperation<BsonDocument>(collectionNamespace, key, initial, reduceFunction, filter, messageEncoderSettings);
            var expectedResult = new BsonDocument
            {
                { "group", new BsonDocument
                    {
                        { "ns", collectionNamespace.CollectionName },
                        { "key", key },
                        { "$reduce", reduceFunction },
                        { "initial", initial },
                        { "cond", filter }
                    }
                }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateCommand_should_return_expected_result_when_finalizeFunction_was_provided()
        {
            var collectionNamespace = new CollectionNamespace("databaseName", "collectionName");
            var key = new BsonDocument("key", 1);
            var initial = new BsonDocument("x", 1);
            var reduceFunction = new BsonJavaScript("reduceFunction");
            var messageEncoderSettings = new MessageEncoderSettings();
            var finalizeFunction = new BsonJavaScript("finalizeFunction");
            var subject = new GroupOperation<BsonDocument>(collectionNamespace, key, initial, reduceFunction, null, messageEncoderSettings);
            subject.FinalizeFunction = finalizeFunction;
            var expectedResult = new BsonDocument
            {
                { "group", new BsonDocument
                    {
                        { "ns", collectionNamespace.CollectionName },
                        { "key", key },
                        { "$reduce", reduceFunction },
                        { "initial", initial },
                        { "finalize", finalizeFunction }
                    }
                }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateCommand_should_return_expected_result_when_keyFunction_was_provided()
        {
            var collectionNamespace = new CollectionNamespace("databaseName", "collectionName");
            var keyFunction = new BsonJavaScript("keyFunction");
            var initial = new BsonDocument("x", 1);
            var reduceFunction = new BsonJavaScript("reduceFunction");
            var messageEncoderSettings = new MessageEncoderSettings();
            var subject = new GroupOperation<BsonDocument>(collectionNamespace, keyFunction, initial, reduceFunction, null, messageEncoderSettings);
            var expectedResult = new BsonDocument
            {
                { "group", new BsonDocument
                    {
                        { "ns", collectionNamespace.CollectionName },
                        { "$keyf", keyFunction },
                        { "$reduce", reduceFunction },
                        { "initial", initial }
                    }
                }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateCommand_should_return_expected_result_when_maxTime_was_provided()
        {
            var collectionNamespace = new CollectionNamespace("databaseName", "collectionName");
            var key = new BsonDocument("key", 1);
            var initial = new BsonDocument("x", 1);
            var reduceFunction = new BsonJavaScript("reduceFunction");
            var messageEncoderSettings = new MessageEncoderSettings();
            var subject = new GroupOperation<BsonDocument>(collectionNamespace, key, initial, reduceFunction, null, messageEncoderSettings);
            subject.MaxTime = TimeSpan.FromSeconds(1);
            var expectedResult = new BsonDocument
            {
                { "group", new BsonDocument
                    {
                        { "ns", collectionNamespace.CollectionName },
                        { "key", key },
                        { "$reduce", reduceFunction },
                        { "initial", initial }
                    }
                },
                { "maxTimeMS", 1000 }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void FinalizeFunction_should_work()
        {
            var collectionNamespace = new CollectionNamespace("databaseName", "collectionName");
            var key = new BsonDocument("key", 1);
            var initial = new BsonDocument("x", 1);
            var reduceFunction = new BsonJavaScript("reduceFunction");
            var filter = new BsonDocument("y", 1);
            var messageEncoderSettings = new MessageEncoderSettings();
            var subject = new GroupOperation<BsonDocument>(collectionNamespace, key, initial, reduceFunction, filter, messageEncoderSettings);
            var finalizeFunction = new BsonJavaScript("finalizeFunction");

            subject.FinalizeFunction = finalizeFunction;

            subject.FinalizeFunction.Should().Be(finalizeFunction);
        }

        [Test]
        public void MaxTime_should_work()
        {
            var collectionNamespace = new CollectionNamespace("databaseName", "collectionName");
            var key = new BsonDocument("key", 1);
            var initial = new BsonDocument("x", 1);
            var reduceFunction = new BsonJavaScript("reduceFunction");
            var filter = new BsonDocument("y", 1);
            var messageEncoderSettings = new MessageEncoderSettings();
            var subject = new GroupOperation<BsonDocument>(collectionNamespace, key, initial, reduceFunction, filter, messageEncoderSettings);
            var maxTime = TimeSpan.FromSeconds(1);

            subject.MaxTime = maxTime;

            subject.MaxTime.Should().Be(maxTime);
        }

        [Test]
        public void ResultSerializer_should_work()
        {
            var collectionNamespace = new CollectionNamespace("databaseName", "collectionName");
            var key = new BsonDocument("key", 1);
            var initial = new BsonDocument("x", 1);
            var reduceFunction = new BsonJavaScript("reduceFunction");
            var filter = new BsonDocument("y", 1);
            var messageEncoderSettings = new MessageEncoderSettings();
            var subject = new GroupOperation<BsonDocument>(collectionNamespace, key, initial, reduceFunction, filter, messageEncoderSettings);
            var resultSerializer = Substitute.For<IBsonSerializer<BsonDocument>>();

            subject.ResultSerializer = resultSerializer;

            subject.ResultSerializer.Should().BeSameAs(resultSerializer);
        }
    }
}
