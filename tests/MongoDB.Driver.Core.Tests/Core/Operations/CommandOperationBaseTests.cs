/* Copyright 2016 MongoDB Inc.
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
using System.Net;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class CommandOperationBaseTests
    {
        // public methods
        [Theory]
        [ParameterAttributeData]
        public void AdditionalOptions_get_and_set_should_work(
            [Values(null, "{ additional : 1 }")] string additionalOptionsString)
        {
            var subject = CreateSubject<BsonDocument>();
            var additionalOptions = additionalOptionsString == null ? null : BsonDocument.Parse(additionalOptionsString);

            subject.AdditionalOptions = additionalOptions;
            var result = subject.AdditionalOptions;

            result.Should().BeSameAs(additionalOptions);
        }

        [Fact]
        public void Command_get_should_return_expected_result()
        {
            var command = new BsonDocument("command", 1);
            var subject = CreateSubject<BsonDocument>(command: command);

            var result = subject.Command;

            result.Should().BeSameAs(command);
        }

        [Fact]
        public void CommandValidator_get_and_set_should_work()
        {
            var subject = CreateSubject<BsonDocument>();
            var commandValidator = new Mock<IElementNameValidator>().Object;

            subject.CommandValidator = commandValidator;
            var result = subject.CommandValidator;

            result.Should().BeSameAs(commandValidator);
        }

        [Fact]
        public void CommandValidator_set_should_throw_when_value_is_null()
        {
            var subject = CreateSubject<BsonDocument>();

            Action action = () => subject.CommandValidator = null;

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("value");
        }

        [Theory]
        [ParameterAttributeData]
        public void Comment_get_and_set_should_work(
           [Values(null, "comment")] string comment)
        {
            var subject = CreateSubject<BsonDocument>();

            subject.Comment = comment;
            var result = subject.Comment;

            result.Should().BeSameAs(comment);
        }

        [Fact]
        public void constructor_should_initialize_instance()
        {
            var databaseNamespace = new DatabaseNamespace("databaseName");
            var command = new BsonDocument("command", 1);
            var resultSerializer = new BsonDocumentSerializer();
            var messageEncoderSettings = new MessageEncoderSettings();

            var result = new FakeCommandOperation<BsonDocument>(databaseNamespace, command, resultSerializer, messageEncoderSettings);

            result.AdditionalOptions.Should().BeNull();
            result.Command.Should().BeSameAs(command);
            result.CommandValidator.Should().BeOfType<NoOpElementNameValidator>();
            result.Comment.Should().BeNull();
            result.DatabaseNamespace.Should().BeSameAs(databaseNamespace);
            result.ResultSerializer.Should().BeSameAs(resultSerializer);
            result.MessageEncoderSettings.Should().BeSameAs(messageEncoderSettings);
        }

        [Fact]
        public void constructor_should_initialize_instance_when_messageEncoderSettings_is_null()
        {
            var databaseNamespace = new DatabaseNamespace("databaseName");
            var command = new BsonDocument("command", 1);
            var resultSerializer = new BsonDocumentSerializer();
            MessageEncoderSettings messageEncoderSettings = null;

            var result = new FakeCommandOperation<BsonDocument>(databaseNamespace, command, resultSerializer, messageEncoderSettings);

            result.MessageEncoderSettings.Should().BeNull();
        }

        [Fact]
        public void constructor_should_throw_when_command_is_null()
        {
            var databaseNamespace = new DatabaseNamespace("databaseName");
            BsonDocument command = null;
            var resultSerializer = new BsonDocumentSerializer();
            var messageEncoderSettings = new MessageEncoderSettings();

            Action action = () => new FakeCommandOperation<BsonDocument>(databaseNamespace, command, resultSerializer, messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("command");
        }

        [Fact]
        public void constructor_should_throw_when_databaseNamespace_is_null()
        {
            DatabaseNamespace databaseNamespace = null;
            var command = new BsonDocument("command", 1);
            var resultSerializer = new BsonDocumentSerializer();
            var messageEncoderSettings = new MessageEncoderSettings();

            Action action = () => new FakeCommandOperation<BsonDocument>(databaseNamespace, command, resultSerializer, messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("databaseNamespace");
        }

        [Fact]
        public void constructor_should_throw_when_resultSerializer_is_null()
        {
            var databaseNamespace = new DatabaseNamespace("databaseName");
            var command = new BsonDocument("command", 1);
            BsonDocumentSerializer resultSerializer = null;
            var messageEncoderSettings = new MessageEncoderSettings();

            Action action = () => new FakeCommandOperation<BsonDocument>(databaseNamespace, command, resultSerializer, messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("resultSerializer");
        }

        [Fact]
        public void DatabaseNamespace_get_should_return_expected_result()
        {
            var databaseNamespace = new DatabaseNamespace("databaseName");
            var subject = CreateSubject<BsonDocument>(databaseNamespace: databaseNamespace);

            var result = subject.DatabaseNamespace;

            result.Should().BeSameAs(databaseNamespace);
        }

        [Theory]
        [ParameterAttributeData]
        public void MessageEncoderSettings_get_should_return_expected_result(
           [Values(false, true)] bool useNull)
        {
            var messageEncoderSettings = useNull ? null : new MessageEncoderSettings();
            var subject = CreateSubject<BsonDocument>(messageEncoderSettings: messageEncoderSettings);

            var result = subject.MessageEncoderSettings;

            result.Should().BeSameAs(messageEncoderSettings);
        }

        [Fact]
        public void ResultSerializer_get_should_return_expected_result()
        {
            var resultSerializer = new Mock<IBsonSerializer<BsonDocument>>().Object;
            var subject = CreateSubject<BsonDocument>(resultSerializer: resultSerializer);

            var result = subject.ResultSerializer;

            result.Should().BeSameAs(resultSerializer);
        }

        // private methods
        private ServerDescription CreateServerDescription(ServerType serverType)
        {
            var endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(new ClusterId(), endPoint);
            return new ServerDescription(serverId, endPoint, type: serverType);
        }

        private CommandOperationBase<TCommandResult> CreateSubject<TCommandResult>(
            DatabaseNamespace databaseNamespace = null,
            BsonDocument command = null,
            IBsonSerializer<TCommandResult> resultSerializer = null,
            MessageEncoderSettings messageEncoderSettings = null)
        {
            databaseNamespace = databaseNamespace ?? new DatabaseNamespace("databaseName");
            command = command ?? new BsonDocument("command", 1);
            resultSerializer = resultSerializer ?? BsonSerializer.LookupSerializer<TCommandResult>();
            return new FakeCommandOperation<TCommandResult>(databaseNamespace, command, resultSerializer, messageEncoderSettings);
        }

        // nested types
        private class FakeCommandOperation<TCommandResult> : CommandOperationBase<TCommandResult>
        {
            public FakeCommandOperation(
                DatabaseNamespace databaseNamespace,
                BsonDocument command,
                IBsonSerializer<TCommandResult> resultSerializer,
                MessageEncoderSettings messageEncoderSettings)
                : base (databaseNamespace, command, resultSerializer, messageEncoderSettings)
            {
            }
        }
    }
}
