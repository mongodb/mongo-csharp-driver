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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Tests;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver
{
    public class MongoDatabaseImplTests
    {
        private MockOperationExecutor _operationExecutor;
        private MongoDatabaseImpl _subject;

        [SetUp]
        public void Setup()
        {
            var settings = new MongoDatabaseSettings();
            settings.ApplyDefaultValues(new MongoClientSettings());
            _operationExecutor = new MockOperationExecutor();
            _subject = new MongoDatabaseImpl(
                Substitute.For<IMongoClient>(),
                new DatabaseNamespace("foo"),
                settings,
                Substitute.For<ICluster>(),
                _operationExecutor);
        }

        [Test]
        public void Client_should_be_set()
        {
            _subject.Client.Should().NotBeNull();
        }

        [Test]
        public void DatabaseName_should_be_set()
        {
            _subject.DatabaseNamespace.DatabaseName.Should().Be("foo");
        }

        [Test]
        public void Settings_should_be_set()
        {
            _subject.Settings.Should().NotBeNull();
        }

        [Test]
        public async Task CreateCollectionAsync_should_execute_the_CreateCollectionOperation()
        {
            var storageEngine = new BsonDocument("awesome", true);
            var options = new CreateCollectionOptions
            {
                AutoIndexId = false,
                Capped = true,
                MaxDocuments = 10,
                MaxSize = 11,
                StorageEngine = storageEngine,
                UsePowerOf2Sizes = false
            };
            await _subject.CreateCollectionAsync("bar", options, CancellationToken.None);

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<CreateCollectionOperation>();
            var op = (CreateCollectionOperation)call.Operation;
            op.CollectionNamespace.Should().Be(new CollectionNamespace(new DatabaseNamespace("foo"), "bar"));
            op.AutoIndexId.Should().Be(options.AutoIndexId);
            op.Capped.Should().Be(options.Capped);
            op.MaxDocuments.Should().Be(options.MaxDocuments);
            op.MaxSize.Should().Be(options.MaxSize);
            op.StorageEngine.Should().Be(storageEngine);
            op.UsePowerOf2Sizes.Should().Be(options.UsePowerOf2Sizes);
        }

        [Test]
        public async Task DropCollectionAsync_should_execute_the_DropCollectionOperation()
        {
            await _subject.DropCollectionAsync("bar", CancellationToken.None);

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<DropCollectionOperation>();
            var op = (DropCollectionOperation)call.Operation;
            op.CollectionNamespace.Should().Be(new CollectionNamespace(new DatabaseNamespace("foo"), "bar"));
        }

        [Test]
        public async Task ListCollections_should_execute_the_ListCollectionsOperation()
        {
            var result = Substitute.For<IAsyncCursor<BsonDocument>>();
            _operationExecutor.EnqueueResult<IAsyncCursor<BsonDocument>>(result);
            var filter = new BsonDocument("name", "awesome");

            await _subject.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter }, CancellationToken.None);

            var call = _operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();
            call.Operation.Should().BeOfType<ListCollectionsOperation>();
            var op = (ListCollectionsOperation)call.Operation;
            op.DatabaseNamespace.DatabaseName.Should().Be("foo");
            op.Filter.Should().Be(filter);
        }

        [Test]
        public async Task RenameCollectionAsync_should_execute_the_RenameCollectionOperation()
        {
            var options = new RenameCollectionOptions
            {
                DropTarget = false,
            };
            await _subject.RenameCollectionAsync("bar", "baz", options, CancellationToken.None);

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<RenameCollectionOperation>();
            var op = (RenameCollectionOperation)call.Operation;
            op.CollectionNamespace.Should().Be(new CollectionNamespace(new DatabaseNamespace("foo"), "bar"));
            op.NewCollectionNamespace.Should().Be(new CollectionNamespace(new DatabaseNamespace("foo"), "baz"));
            op.DropTarget.Should().Be(options.DropTarget);
        }

        [Test]
        public async Task RunCommand_should_default_to_ReadPreference_primary()
        {
            var cmd = new BsonDocument("count", "foo");
            await _subject.RunCommandAsync<BsonDocument>(cmd);

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Binding.Should().BeOfType<WritableServerBinding>();

            call.Operation.Should().BeOfType<WriteCommandOperation<BsonDocument>>();
            var op = (WriteCommandOperation<BsonDocument>)call.Operation;
            op.DatabaseNamespace.DatabaseName.Should().Be("foo");
            op.Command.Should().Be("{count: \"foo\"}");
        }

        [Test]
        public async Task RunCommand_should_use_the_provided_ReadPreference()
        {
            var cmd = new BsonDocument("count", "foo");
            await _subject.RunCommandAsync<BsonDocument>(cmd, ReadPreference.Secondary, CancellationToken.None);

            var call = _operationExecutor.GetReadCall<BsonDocument>();

            call.Binding.Should().BeOfType<ReadPreferenceBinding>();
            var binding = (ReadPreferenceBinding)call.Binding;
            binding.ReadPreference.Should().Be(ReadPreference.Secondary);

            call.Operation.Should().BeOfType<ReadCommandOperation<BsonDocument>>();
            var op = (ReadCommandOperation<BsonDocument>)call.Operation;
            op.DatabaseNamespace.DatabaseName.Should().Be("foo");
            op.Command.Should().Be("{count: \"foo\"}");
        }

        [Test]
        public async Task RunCommand_should_run_a_non_read_command()
        {
            var cmd = new BsonDocument("shutdown", 1);
            await _subject.RunCommandAsync<BsonDocument>(cmd);

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<WriteCommandOperation<BsonDocument>>();
            var op = (WriteCommandOperation<BsonDocument>)call.Operation;
            op.DatabaseNamespace.DatabaseName.Should().Be("foo");
            op.Command.Should().Be(cmd);
        }

        [Test]
        public async Task RunCommand_should_run_a_json_command()
        {
            await _subject.RunCommandAsync<BsonDocument>("{count: \"foo\"}");

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<WriteCommandOperation<BsonDocument>>();
            var op = (WriteCommandOperation<BsonDocument>)call.Operation;
            op.DatabaseNamespace.DatabaseName.Should().Be("foo");
            op.Command.Should().Be("{count: \"foo\"}");
        }

        [Test]
        public async Task RunCommand_should_run_a_serialized_command()
        {
            var cmd = new CountCommand { Collection = "foo" };
            await _subject.RunCommandAsync(new ObjectCommand<BsonDocument>(cmd));

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<WriteCommandOperation<BsonDocument>>();
            var op = (WriteCommandOperation<BsonDocument>)call.Operation;
            op.DatabaseNamespace.DatabaseName.Should().Be("foo");
            op.Command.Should().Be("{count: \"foo\"}");
        }

        private class CountCommand
        {
            [BsonElement("count")]
            public string Collection;
        }
    }
}