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
            _operationExecutor = new MockOperationExecutor();
            _subject = new MongoDatabaseImpl(
                Substitute.For<ICluster>(),
                "foo",
                new MongoDatabaseSettings(),
                _operationExecutor);
        }

        [Test]
        public void DatabaseName_should_be_set()
        {
            _subject.DatabaseName.Should().Be("foo");
        }

        [Test]
        public void Settings_should_be_set()
        {
            _subject.Settings.Should().NotBeNull();
        }

        [Test]
        public async Task DropAsync_should_execute_the_DropDatabaseOperation()
        {
            await _subject.DropAsync(Timeout.InfiniteTimeSpan, CancellationToken.None);

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<DropDatabaseOperation>();
            var op = (DropDatabaseOperation)call.Operation;
            op.DatabaseName.Should().Be("foo");
        }

        [Test]
        public async Task GetCollectionNames_should_execute_the_ListCollectionNamesOperation()
        {
            await _subject.GetCollectionNamesAsync(Timeout.InfiniteTimeSpan, CancellationToken.None);

            var call = _operationExecutor.GetReadCall<IReadOnlyList<string>>();

            call.Operation.Should().BeOfType<ListCollectionNamesOperation>();
            var op = (ListCollectionNamesOperation)call.Operation;
            op.DatabaseName.Should().Be("foo");
        }

        [Test]
        public async Task RunCommand_should_execute_the_ReadCommandOperation()
        {
            var cmd = new BsonDocument("count", "foo");
            await _subject.RunCommandAsync<BsonDocument>(cmd, Timeout.InfiniteTimeSpan, CancellationToken.None);

            var call = _operationExecutor.GetReadCall<BsonDocument>();

            call.Operation.Should().BeOfType<ReadCommandOperation<BsonDocument>>();
            var op = (ReadCommandOperation<BsonDocument>)call.Operation;
            op.DatabaseName.Should().Be("foo");
            op.Command.Should().Be("{count: \"foo\"}");
        }

        [Test]
        public async Task RunCommand_should_run_a_non_read_command()
        {
            var cmd = new BsonDocument("shutdown", 1);
            await _subject.RunCommandAsync<BsonDocument>(cmd, Timeout.InfiniteTimeSpan, CancellationToken.None);

            var call = _operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<WriteCommandOperation<BsonDocument>>();
            var op = (WriteCommandOperation<BsonDocument>)call.Operation;
            op.DatabaseName.Should().Be("foo");
            op.Command.Should().Be(cmd);
        }

        [Test]
        public async Task RunCommand_should_run_a_json_command()
        {
            await _subject.RunCommandAsync<BsonDocument>("{count: \"foo\"}", Timeout.InfiniteTimeSpan, CancellationToken.None);

            var call = _operationExecutor.GetReadCall<BsonDocument>();

            call.Operation.Should().BeOfType<ReadCommandOperation<BsonDocument>>();
            var op = (ReadCommandOperation<BsonDocument>)call.Operation;
            op.DatabaseName.Should().Be("foo");
            op.Command.Should().Be("{count: \"foo\"}");
        }

        [Test]
        public async Task RunCommand_should_run_a_serialized_command()
        {
            var cmd = new CountCommand { Collection = "foo" };
            await _subject.RunCommandAsync<BsonDocument>(cmd, Timeout.InfiniteTimeSpan, CancellationToken.None);

            var call = _operationExecutor.GetReadCall<BsonDocument>();

            call.Operation.Should().BeOfType<ReadCommandOperation<BsonDocument>>();
            var op = (ReadCommandOperation<BsonDocument>)call.Operation;
            op.DatabaseName.Should().Be("foo");
            op.Command.Should().Be("{count: \"foo\"}");
        }

        private class CountCommand
        {
            [BsonElement("count")]
            public string Collection;
        }
    }
}