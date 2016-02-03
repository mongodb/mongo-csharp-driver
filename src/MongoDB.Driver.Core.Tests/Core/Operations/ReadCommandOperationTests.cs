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
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class ReadCommandOperationTests
    {
        // public methods
        [Test]
        public void constructor_should_initialize_instance()
        {
            var databaseNamespace = new DatabaseNamespace("databaseName");
            var command = new BsonDocument("command", 1);
            var resultSerializer = BsonDocumentSerializer.Instance;
            var messageEncoderSettings = new MessageEncoderSettings();

            var result = new ReadCommandOperation<BsonDocument>(databaseNamespace, command, resultSerializer, messageEncoderSettings);

            result.AdditionalOptions.Should().BeNull();
            result.Command.Should().BeSameAs(command);
            result.CommandValidator.Should().BeOfType<NoOpElementNameValidator>();
            result.Comment.Should().BeNull();
            result.DatabaseNamespace.Should().BeSameAs(databaseNamespace);
            result.MessageEncoderSettings.Should().BeSameAs(messageEncoderSettings);
            result.ResultSerializer.Should().BeSameAs(resultSerializer);
        }

        [TestCase(ServerType.Standalone, ReadPreferenceMode.Primary, false, false)]
        [TestCase(ServerType.Standalone, ReadPreferenceMode.Primary, false, true)]
        [TestCase(ServerType.Standalone, ReadPreferenceMode.Secondary, true, false)]
        [TestCase(ServerType.Standalone, ReadPreferenceMode.Secondary, true, true)]
        [TestCase(ServerType.Standalone, ReadPreferenceMode.SecondaryPreferred, true, false)]
        [TestCase(ServerType.Standalone, ReadPreferenceMode.SecondaryPreferred, true, true)]
        [TestCase(ServerType.ShardRouter, ReadPreferenceMode.Primary, false, false)]
        [TestCase(ServerType.ShardRouter, ReadPreferenceMode.Primary, false, true)]
        [TestCase(ServerType.ShardRouter, ReadPreferenceMode.SecondaryPreferred, true, false)]
        [TestCase(ServerType.ShardRouter, ReadPreferenceMode.SecondaryPreferred, true, true)]
        public void Execute_should_call_channel_Command_with_unwrapped_command_when_wrapping_is_not_necessary(
            ServerType serverType,
            ReadPreferenceMode readPreferenceMode,
            bool slaveOk,
            bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var readPreference = new ReadPreference(readPreferenceMode);
            var serverDescription = CreateServerDescription(serverType);
            var channel = CreateFakeChannel();
            var channelSource = CreateFakeChannelSource(serverDescription, channel);
            var binding = CreateFakeReadBinding(readPreference, channelSource);
            var cancellationToken = new CancellationTokenSource().Token;

            BsonDocument result;
            if (async)
            {
                result = subject.ExecuteAsync(binding, cancellationToken).GetAwaiter().GetResult();

                channel.Received(1).CommandAsync(
                    subject.DatabaseNamespace,
                    subject.Command,
                    subject.CommandValidator,
                    Arg.Is<Func<CommandResponseHandling>>(f => f() == CommandResponseHandling.Return),
                    slaveOk,
                    subject.ResultSerializer,
                    subject.MessageEncoderSettings,
                    cancellationToken);
            }
            else
            {
                result = subject.Execute(binding, cancellationToken);

                channel.Received(1).Command(
                    subject.DatabaseNamespace,
                    subject.Command,
                    subject.CommandValidator,
                    Arg.Is<Func<CommandResponseHandling>>(f => f() == CommandResponseHandling.Return),
                    slaveOk,
                    subject.ResultSerializer,
                    subject.MessageEncoderSettings,
                    cancellationToken);
            }
        }

        [Test]
        public void Execute_should_call_channel_Command_with_wrapped_command_when_additionalOptions_need_wrapping(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            subject.AdditionalOptions = new BsonDocument("additional", 1);
            var readPreference = ReadPreference.Primary;
            var serverDescription = CreateServerDescription(ServerType.Standalone);
            var channel = CreateFakeChannel();
            var channelSource = CreateFakeChannelSource(serverDescription, channel);
            var binding = CreateFakeReadBinding(readPreference, channelSource);
            var cancellationToken = new CancellationTokenSource().Token;
            var wrappedCommand = BsonDocument.Parse("{ $query : { command : 1 }, additional : 1 }");
            var slaveOk = false;

            BsonDocument result;
            if (async)
            {
                result = subject.ExecuteAsync(binding, cancellationToken).GetAwaiter().GetResult();

                channel.Received(1).CommandAsync(
                    subject.DatabaseNamespace,
                    wrappedCommand,
                    subject.CommandValidator,
                    Arg.Is<Func<CommandResponseHandling>>(f => f() == CommandResponseHandling.Return),
                    slaveOk,
                    subject.ResultSerializer,
                    subject.MessageEncoderSettings,
                    cancellationToken);
            }
            else
            {
                result = subject.Execute(binding, cancellationToken);

                channel.Received(1).Command(
                    subject.DatabaseNamespace,
                    wrappedCommand,
                    subject.CommandValidator,
                    Arg.Is<Func<CommandResponseHandling>>(f => f() == CommandResponseHandling.Return),
                    slaveOk,
                    subject.ResultSerializer,
                    subject.MessageEncoderSettings,
                    cancellationToken);
            }
        }

        [Test]
        public void Execute_should_call_channel_Command_with_wrapped_command_when_comment_needs_wrapping(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            subject.Comment = "comment";
            var readPreference = ReadPreference.Primary;
            var serverDescription = CreateServerDescription(ServerType.Standalone);
            var channel = CreateFakeChannel();
            var channelSource = CreateFakeChannelSource(serverDescription, channel);
            var binding = CreateFakeReadBinding(readPreference, channelSource);
            var cancellationToken = new CancellationTokenSource().Token;
            var wrappedCommand = BsonDocument.Parse("{ $query : { command : 1 }, $comment : \"comment\" }");
            var slaveOk = false;

            BsonDocument result;
            if (async)
            {
                result = subject.ExecuteAsync(binding, cancellationToken).GetAwaiter().GetResult();

                channel.Received(1).CommandAsync(
                    subject.DatabaseNamespace,
                    wrappedCommand,
                    subject.CommandValidator,
                    Arg.Is<Func<CommandResponseHandling>>(f => f() == CommandResponseHandling.Return),
                    slaveOk,
                    subject.ResultSerializer,
                    subject.MessageEncoderSettings,
                    cancellationToken);
            }
            else
            {
                result = subject.Execute(binding, cancellationToken);

                channel.Received(1).Command(
                    subject.DatabaseNamespace,
                    wrappedCommand,
                    subject.CommandValidator,
                    Arg.Is<Func<CommandResponseHandling>>(f => f() == CommandResponseHandling.Return),
                    slaveOk,
                    subject.ResultSerializer,
                    subject.MessageEncoderSettings,
                    cancellationToken);
            }
        }

        [Test]
        public void Execute_should_call_channel_Command_with_wrapped_command_when_readPreference_needs_wrapping(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject<BsonDocument>();
            var readPreference = ReadPreference.Secondary;
            var serverDescription = CreateServerDescription(ServerType.ShardRouter);
            var channel = CreateFakeChannel();
            var channelSource = CreateFakeChannelSource(serverDescription, channel);
            var binding = CreateFakeReadBinding(readPreference, channelSource);
            var cancellationToken = new CancellationTokenSource().Token;
            var wrappedCommand = BsonDocument.Parse("{ $query : { command : 1 }, $readPreference : { mode : \"secondary\"} }");
            var slaveOk = true;

            BsonDocument result;
            if (async)
            {
                result = subject.ExecuteAsync(binding, cancellationToken).GetAwaiter().GetResult();

                channel.Received(1).CommandAsync(
                    subject.DatabaseNamespace,
                    wrappedCommand,
                    subject.CommandValidator,
                    Arg.Is<Func<CommandResponseHandling>>(f => f() == CommandResponseHandling.Return),
                    slaveOk,
                    subject.ResultSerializer,
                    subject.MessageEncoderSettings,
                    cancellationToken);
            }
            else
            {
                result = subject.Execute(binding, cancellationToken);

                channel.Received(1).Command(
                    subject.DatabaseNamespace,
                    wrappedCommand,
                    subject.CommandValidator,
                    Arg.Is<Func<CommandResponseHandling>>(f => f() == CommandResponseHandling.Return),
                    slaveOk,
                    subject.ResultSerializer,
                    subject.MessageEncoderSettings,
                    cancellationToken);
            }
        }

        // private methods
        private IReadBinding CreateFakeReadBinding(ReadPreference readPreference, IChannelSourceHandle channelSource)
        {
            var binding = Substitute.For<IReadBinding>();
            binding.ReadPreference.Returns(readPreference);
            binding.GetReadChannelSource(CancellationToken.None).ReturnsForAnyArgs(channelSource);
            binding.GetReadChannelSourceAsync(CancellationToken.None).ReturnsForAnyArgs(Task.FromResult(channelSource));
            return binding;
        }

        private IChannelHandle CreateFakeChannel()
        {
            var channel = Substitute.For<IChannelHandle>();
            return channel;
        }

        private IChannelSourceHandle CreateFakeChannelSource(ServerDescription serverDescription, IChannelHandle channel)
        {
            var channelSource = Substitute.For<IChannelSourceHandle>();
            channelSource.ServerDescription.Returns(serverDescription);
            channelSource.GetChannel(CancellationToken.None).ReturnsForAnyArgs(channel);
            channelSource.GetChannelAsync(CancellationToken.None).ReturnsForAnyArgs(Task.FromResult(channel));
            return channelSource;
        }

        private ServerDescription CreateServerDescription(ServerType serverType)
        {
            var clusterId = new ClusterId(1);
            var endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(clusterId, endPoint);
            return new ServerDescription(serverId, endPoint, type: serverType);
        }

        private ReadCommandOperation<TCommandResult> CreateSubject<TCommandResult>(
            string databaseName = "databaseName",
            BsonDocument command = null,
            IBsonSerializer<TCommandResult> resultSerializer = null,
            MessageEncoderSettings messageEncoderSettings = null)
        {
            var databaseNamespace = new DatabaseNamespace(databaseName);
            command = command ?? new BsonDocument("command", 1);
            resultSerializer = resultSerializer ?? BsonSerializer.LookupSerializer<TCommandResult>();
            return new ReadCommandOperation<TCommandResult>(databaseNamespace, command, resultSerializer, messageEncoderSettings);
        }
    }
}
