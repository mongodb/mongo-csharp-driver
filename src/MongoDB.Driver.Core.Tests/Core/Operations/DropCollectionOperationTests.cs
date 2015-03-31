﻿/* Copyright 2013-2014 MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class DropCollectionOperationTests
    {
        // fields
        private CollectionNamespace _collectionNamespace;
        private MessageEncoderSettings _messageEncoderSettings;

        // setup methods
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            _collectionNamespace = CoreTestConfiguration.GetCollectionNamespaceForTestFixture();
            _messageEncoderSettings = CoreTestConfiguration.MessageEncoderSettings;
        }

        // test methods
        [Test]
        public void CollectionNamespace_get_should_return_expected_result()
        {
            var subject = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            var result = subject.CollectionNamespace;

            result.Should().BeSameAs(_collectionNamespace);
        }

        [Test]
        public void constructor_should_initialize_subject()
        {
            var subject = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
        }

        [Test]
        public void constructor_should_throw_when_collectionNamespace_is_null()
        {
            Action action = () => { new DropCollectionOperation(null, _messageEncoderSettings); };

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("collectionNamespace");
        }

        [Test]
        public void CreateCommand_should_return_expected_result()
        {
            var subject = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            var expectedResult = new BsonDocument
            {
                { "drop", _collectionNamespace.CollectionName }
            };

            var result = subject.CreateCommand();

            result.Should().Be(expectedResult);
        }

        [Test]
        [RequiresServer]
        public async Task ExecuteAsync_should_not_throw_when_collection_does_not_exist()
        {
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                var subject = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);
                var dropCollectionOperation = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);
                await dropCollectionOperation.ExecuteAsync(binding, CancellationToken.None);
                await subject.ExecuteAsync(binding, CancellationToken.None); // this will throw if we have a problem...
            }
        }

        [Test]
        [RequiresServer]
        public async Task ExecuteAsync_should_return_expected_result()
        {
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                var subject = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);
                var createCollectionOperation = new CreateCollectionOperation(_collectionNamespace, _messageEncoderSettings);
                await createCollectionOperation.ExecuteAsync(binding, CancellationToken.None);

                var result = await subject.ExecuteAsync(binding, CancellationToken.None);

                result["ok"].ToBoolean().Should().BeTrue();
                result["ns"].ToString().Should().Be(_collectionNamespace.FullName);
            }
        }

        [Test]
        public void MessageEncoderSettings_get_should_return_expected_result()
        {
            var subject = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);

            var result = subject.MessageEncoderSettings;

            result.Should().BeSameAs(_messageEncoderSettings);
        }
    }
}
