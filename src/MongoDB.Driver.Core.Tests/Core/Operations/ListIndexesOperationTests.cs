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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class ListIndexesOperationTests
    {
        // fields
        private CollectionNamespace _collectionNamespace;
        private MessageEncoderSettings _messageEncoderSettings;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var databaseNamespace = CoreTestConfiguration.GetDatabaseNamespaceForTestFixture();
            _collectionNamespace = new CollectionNamespace(databaseNamespace, "ListIndexesOperationTests");
            _messageEncoderSettings = CoreTestConfiguration.MessageEncoderSettings;
        }

        // test methods
        [Test]
        public void CollectionNamespace_get_should_return_expected_result()
        {
            var subject = new ListIndexesOperation(_collectionNamespace, _messageEncoderSettings);

            var result = subject.CollectionNamespace;

            result.Should().BeSameAs(_collectionNamespace);
        }

        [Test]
        public void constructor_should_initialize_subject()
        {
            var subject = new ListIndexesOperation(_collectionNamespace, _messageEncoderSettings);

            subject.CollectionNamespace.Should().BeSameAs(_collectionNamespace);
            subject.MessageEncoderSettings.Should().BeSameAs(_messageEncoderSettings);
        }

        [Test]
        public void constructor_should_throw_when_collectionNamespace_is_null()
        {
            Action action = () => new ListIndexesOperation(null, _messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("collectionNamespace");
        }

        [Test]
        [RequiresServer]
        public async Task ExecuteAsync_should_return_expected_result()
        {
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                var subject = new ListIndexesOperation(_collectionNamespace, _messageEncoderSettings);
                await EnsureCollectionExistsAsync(binding);
                var expectedNames = new[] { "_id_" };

                var result = await subject.ExecuteAsync(binding, CancellationToken.None);
                var list = await result.ToListAsync();

                list.Select(index => index["name"].AsString).Should().BeEquivalentTo(expectedNames);
            }
        }

        [Test]
        [RequiresServer]
        public async Task ExecuteAsync_should_return_expected_result_when_collection_does_not_exist()
        {
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                var subject = new ListIndexesOperation(_collectionNamespace, _messageEncoderSettings);
                await DropCollectionAsync(binding);

                var result = await subject.ExecuteAsync(binding, CancellationToken.None);
                var list = await result.ToListAsync();

                list.Count.Should().Be(0);
            }
        }

        [Test]
        [RequiresServer]
        public async Task ExecuteAsync_should_return_expected_result_when_database_does_not_exist()
        {
            using (var binding = CoreTestConfiguration.GetReadWriteBinding())
            {
                var subject = new ListIndexesOperation(_collectionNamespace, _messageEncoderSettings);
                await DropDatabaseAsync(binding);

                var result = await subject.ExecuteAsync(binding, CancellationToken.None);
                var list = await result.ToListAsync();

                list.Count.Should().Be(0);
            }
        }

        [Test]
        public void ExecuteAsync_should_throw_when_binding_is_null()
        {
            var subject = new ListIndexesOperation(_collectionNamespace, _messageEncoderSettings);

            Func<Task> action = () => subject.ExecuteAsync(null, CancellationToken.None);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("binding");
        }

        [Test]
        public void MessageEncoderSettings_get_should_return_expected_result()
        {
            var subject = new ListIndexesOperation(_collectionNamespace, _messageEncoderSettings);

            var result = subject.MessageEncoderSettings;

            result.Should().BeSameAs(_messageEncoderSettings);
        }

        // helper methods
        public Task DropCollectionAsync(IWriteBinding binding)
        {
            var operation = new DropCollectionOperation(_collectionNamespace, _messageEncoderSettings);
            return operation.ExecuteAsync(binding, CancellationToken.None);
        }

        public Task DropDatabaseAsync(IWriteBinding binding)
        {
            var operation = new DropDatabaseOperation(_collectionNamespace.DatabaseNamespace, _messageEncoderSettings);
            return operation.ExecuteAsync(binding, CancellationToken.None);
        }

        public Task EnsureCollectionExistsAsync(IWriteBinding binding)
        {
            var requests = new[] { new InsertRequest(new BsonDocument("_id", 1)) };
            var operation = new BulkInsertOperation(_collectionNamespace, requests, _messageEncoderSettings);
            return operation.ExecuteAsync(binding, CancellationToken.None);
        }
    }
}
