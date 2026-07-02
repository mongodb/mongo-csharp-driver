/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class CommandOperationBaseTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var databaseNamespace = new DatabaseNamespace("databaseName");
            var command = new BsonDocument("command", 1);
            var resultSerializer = new BsonDocumentSerializer();
            var messageEncoderSettings = new MessageEncoderSettings();

            var result = new FakeCommandOperation<BsonDocument>(databaseNamespace, resultSerializer, messageEncoderSettings);

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

            var result = new FakeCommandOperation<BsonDocument>(databaseNamespace, resultSerializer, messageEncoderSettings);

            result.MessageEncoderSettings.Should().BeNull();
        }

        [Fact]
        public void constructor_should_throw_when_databaseNamespace_is_null()
        {
            DatabaseNamespace databaseNamespace = null;
            var command = new BsonDocument("command", 1);
            var resultSerializer = new BsonDocumentSerializer();
            var messageEncoderSettings = new MessageEncoderSettings();

            Action action = () => new FakeCommandOperation<BsonDocument>(databaseNamespace, resultSerializer, messageEncoderSettings);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("databaseNamespace");
        }

        [Fact]
        public void constructor_should_throw_when_resultSerializer_is_null()
        {
            var databaseNamespace = new DatabaseNamespace("databaseName");
            var command = new BsonDocument("command", 1);
            BsonDocumentSerializer resultSerializer = null;
            var messageEncoderSettings = new MessageEncoderSettings();

            Action action = () => new FakeCommandOperation<BsonDocument>(databaseNamespace, resultSerializer, messageEncoderSettings);

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
            IBsonSerializer<TCommandResult> resultSerializer = null,
            MessageEncoderSettings messageEncoderSettings = null)
        {
            databaseNamespace = databaseNamespace ?? new DatabaseNamespace("databaseName");
            resultSerializer = resultSerializer ?? BsonSerializer.LookupSerializer<TCommandResult>();
            return new FakeCommandOperation<TCommandResult>(databaseNamespace, resultSerializer, messageEncoderSettings);
        }

        // nested types
        private class FakeCommandOperation<TCommandResult> : CommandOperationBase<TCommandResult>
        {
            public FakeCommandOperation(
                DatabaseNamespace databaseNamespace,
                IBsonSerializer<TCommandResult> resultSerializer,
                MessageEncoderSettings messageEncoderSettings)
                : base(databaseNamespace, resultSerializer, messageEncoderSettings)
            {
            }
        }
    }
}
