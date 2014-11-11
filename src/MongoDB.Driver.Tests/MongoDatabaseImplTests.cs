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
                new DatabaseNamespace("foo"),
                settings,
                Substitute.For<ICluster>(),
                _operationExecutor);
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
        public async Task DropCollectionAsync_should_execute_the_DropCollectionOperation()
        {
            await _subject.DropCollectionAsync("bar", CancellationToken.None);

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<DropCollectionOperation>();
            var op = (DropCollectionOperation)call.Operation;
            op.CollectionNamespace.Should().Be(new CollectionNamespace(new DatabaseNamespace("foo"), "bar"));
        }

        [Test]
        public async Task GetCollectionNames_should_execute_the_ListCollectionsOperation()
        {
            _operationExecutor.EnqueueResult<IReadOnlyList<BsonDocument>>(new BsonDocument[0]);

            await _subject.GetCollectionNamesAsync(CancellationToken.None);

            var call = _operationExecutor.GetReadCall<IReadOnlyList<BsonDocument>>();
            call.Operation.Should().BeOfType<ListCollectionsOperation>();
            var op = (ListCollectionsOperation)call.Operation;
            op.DatabaseNamespace.DatabaseName.Should().Be("foo");
        }

        [Test]
        public async Task RunCommand_should_execute_the_ReadCommandOperation()
        {
            var cmd = new BsonDocument("count", "foo");
            await _subject.RunCommandAsync<BsonDocument>(cmd, CancellationToken.None);

            var call = _operationExecutor.GetReadCall<BsonDocument>();

            call.Operation.Should().BeOfType<ReadCommandOperation<BsonDocument>>();
            var op = (ReadCommandOperation<BsonDocument>)call.Operation;
            op.DatabaseNamespace.DatabaseName.Should().Be("foo");
            op.Command.Should().Be("{count: \"foo\"}");
        }

        [Test]
        public async Task RunCommand_should_run_a_non_read_command()
        {
            var cmd = new BsonDocument("shutdown", 1);
            await _subject.RunCommandAsync<BsonDocument>(cmd, CancellationToken.None);

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<WriteCommandOperation<BsonDocument>>();
            var op = (WriteCommandOperation<BsonDocument>)call.Operation;
            op.DatabaseNamespace.DatabaseName.Should().Be("foo");
            op.Command.Should().Be(cmd);
        }

        [Test]
        public async Task RunCommand_should_run_a_json_command()
        {
            await _subject.RunCommandAsync<BsonDocument>("{count: \"foo\"}", CancellationToken.None);

            var call = _operationExecutor.GetReadCall<BsonDocument>();

            call.Operation.Should().BeOfType<ReadCommandOperation<BsonDocument>>();
            var op = (ReadCommandOperation<BsonDocument>)call.Operation;
            op.DatabaseNamespace.DatabaseName.Should().Be("foo");
            op.Command.Should().Be("{count: \"foo\"}");
        }

        [Test]
        public async Task RunCommand_should_run_a_serialized_command()
        {
            var cmd = new CountCommand { Collection = "foo" };
            await _subject.RunCommandAsync<BsonDocument>(cmd, CancellationToken.None);

            var call = _operationExecutor.GetReadCall<BsonDocument>();

            call.Operation.Should().BeOfType<ReadCommandOperation<BsonDocument>>();
            var op = (ReadCommandOperation<BsonDocument>)call.Operation;
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