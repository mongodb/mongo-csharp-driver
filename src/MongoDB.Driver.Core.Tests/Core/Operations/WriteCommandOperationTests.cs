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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class WriteCommandOperationTests
    {
        // public methods
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var databaseNamespace = new DatabaseNamespace("databaseName");
            var command = new BsonDocument("command", 1);
            var resultSerializer = BsonDocumentSerializer.Instance;
            var messageEncoderSettings = new MessageEncoderSettings();

            var result = new WriteCommandOperation<BsonDocument>(databaseNamespace, command, resultSerializer, messageEncoderSettings);

            result.AdditionalOptions.Should().BeNull();
            result.Command.Should().BeSameAs(command);
            result.CommandValidator.Should().BeOfType<NoOpElementNameValidator>();
            result.Comment.Should().BeNull();
            result.DatabaseNamespace.Should().BeSameAs(databaseNamespace);
            result.MessageEncoderSettings.Should().BeSameAs(messageEncoderSettings);
            result.ResultSerializer.Should().BeSameAs(resultSerializer);
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_call_channel_Command_with_unwrapped_command_when_wrapping_is_not_necessary(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var serverDescription = CreateServerDescription(ServerType.Standalone);
            var mockChannel = CreateMockChannel();
            var channelSource = CreateMockChannelSource(serverDescription, mockChannel.Object).Object;
            var binding = CreateMockWriteBinding(channelSource).Object;
            var cancellationToken = new CancellationTokenSource().Token;
            var slaveOk = false;

            BsonDocument result;
            if (async)
            {
                result = subject.ExecuteAsync(binding, cancellationToken).GetAwaiter().GetResult();

                mockChannel.Verify(
                    c => c.CommandAsync(
                        subject.DatabaseNamespace,
                        subject.Command,
                        subject.CommandValidator,
                        It.Is<Func<CommandResponseHandling>>(f => f() == CommandResponseHandling.Return),
                        slaveOk,
                        subject.ResultSerializer,
                        subject.MessageEncoderSettings,
                        cancellationToken),
                    Times.Once);
            }
            else
            {
                result = subject.Execute(binding, cancellationToken);

                mockChannel.Verify(
                    c => c.Command(
                        subject.DatabaseNamespace,
                        subject.Command,
                        subject.CommandValidator,
                        It.Is<Func<CommandResponseHandling>>(f => f() == CommandResponseHandling.Return),
                        slaveOk,
                        subject.ResultSerializer,
                        subject.MessageEncoderSettings,
                        cancellationToken),
                    Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_call_channel_Command_with_wrapped_command_when_additionalOptions_need_wrapping(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            subject.AdditionalOptions = new BsonDocument("additional", 1);
            var serverDescription = CreateServerDescription(ServerType.Standalone);
            var mockChannel = CreateMockChannel();
            var channelSource = CreateMockChannelSource(serverDescription, mockChannel.Object).Object;
            var binding = CreateMockWriteBinding(channelSource).Object;
            var cancellationToken = new CancellationTokenSource().Token;
            var wrappedCommand = BsonDocument.Parse("{ $query : { command : 1 }, additional : 1 }");
            var slaveOk = false;

            BsonDocument result;
            if (async)
            {
                result = subject.ExecuteAsync(binding, cancellationToken).GetAwaiter().GetResult();

                mockChannel.Verify(
                    c => c.CommandAsync(
                        subject.DatabaseNamespace,
                        wrappedCommand,
                        subject.CommandValidator,
                        It.Is<Func<CommandResponseHandling>>(f => f() == CommandResponseHandling.Return),
                        slaveOk,
                        subject.ResultSerializer,
                        subject.MessageEncoderSettings,
                        cancellationToken),
                    Times.Once);
            }
            else
            {
                result = subject.Execute(binding, cancellationToken);

                mockChannel.Verify(
                    c => c.Command(
                        subject.DatabaseNamespace,
                        wrappedCommand,
                        subject.CommandValidator,
                        It.Is<Func<CommandResponseHandling>>(f => f() == CommandResponseHandling.Return),
                        slaveOk,
                        subject.ResultSerializer,
                        subject.MessageEncoderSettings,
                        cancellationToken),
                    Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Execute_should_call_channel_Command_with_wrapped_command_when_comment_needs_wrapping(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            subject.Comment = "comment";
            var serverDescription = CreateServerDescription(ServerType.Standalone);
            var mockChannel = CreateMockChannel();
            var channelSource = CreateMockChannelSource(serverDescription, mockChannel.Object).Object;
            var binding = CreateMockWriteBinding(channelSource).Object;
            var cancellationToken = new CancellationTokenSource().Token;
            var wrappedCommand = BsonDocument.Parse("{ $query : { command : 1 }, $comment : \"comment\" }");
            var slaveOk = false;

            BsonDocument result;
            if (async)
            {
                result = subject.ExecuteAsync(binding, cancellationToken).GetAwaiter().GetResult();

                mockChannel.Verify(
                    c => c.CommandAsync(
                        subject.DatabaseNamespace,
                        wrappedCommand,
                        subject.CommandValidator,
                        It.Is<Func<CommandResponseHandling>>(f => f() == CommandResponseHandling.Return),
                        slaveOk,
                        subject.ResultSerializer,
                        subject.MessageEncoderSettings,
                        cancellationToken),
                    Times.Once);
            }
            else
            {
                result = subject.Execute(binding, cancellationToken);

                mockChannel.Verify(
                    c => c.Command(
                        subject.DatabaseNamespace,
                        wrappedCommand,
                        subject.CommandValidator,
                        It.Is<Func<CommandResponseHandling>>(f => f() == CommandResponseHandling.Return),
                        slaveOk,
                        subject.ResultSerializer,
                        subject.MessageEncoderSettings,
                        cancellationToken),
                    Times.Once);
            }
        }

        // private methods
        private Mock<IWriteBinding> CreateMockWriteBinding(IChannelSourceHandle channelSource)
        {
            var mockBinding = new Mock<IWriteBinding>();
            mockBinding.Setup(b => b.GetWriteChannelSource(It.IsAny<CancellationToken>())).Returns(channelSource);
            mockBinding.Setup(b => b.GetWriteChannelSourceAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(channelSource));
            return mockBinding;
        }

        private Mock<IChannelHandle> CreateMockChannel()
        {
            var mockChannel = new Mock<IChannelHandle>();
            return mockChannel;
        }

        private Mock<IChannelSourceHandle> CreateMockChannelSource(ServerDescription serverDescription, IChannelHandle channel)
        {
            var mockChannelSource = new Mock<IChannelSourceHandle>();
            mockChannelSource.SetupGet(s => s.ServerDescription).Returns(serverDescription);
            mockChannelSource.Setup(s => s.GetChannel(It.IsAny<CancellationToken>())).Returns(channel);
            mockChannelSource.Setup(s => s.GetChannelAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(channel));
            return mockChannelSource;
        }

        private ServerDescription CreateServerDescription(ServerType serverType)
        {
            var clusterId = new ClusterId(1);
            var endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(clusterId, endPoint);
            return new ServerDescription(serverId, endPoint, type: serverType);
        }

        private WriteCommandOperation<TCommandResult> CreateSubject<TCommandResult>(
            string databaseName = "databaseName",
            BsonDocument command = null,
            IBsonSerializer<TCommandResult> resultSerializer = null,
            MessageEncoderSettings messageEncoderSettings = null)
        {
            var databaseNamespace = new DatabaseNamespace(databaseName);
            command = command ?? new BsonDocument("command", 1);
            resultSerializer = resultSerializer ?? BsonSerializer.LookupSerializer<TCommandResult>();
            return new WriteCommandOperation<TCommandResult>(databaseNamespace, command, resultSerializer, messageEncoderSettings);
        }
    }
}
