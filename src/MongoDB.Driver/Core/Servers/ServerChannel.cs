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
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Servers
{
    internal sealed class ServerChannel : IChannelHandle
    {
        // fields
        private readonly IConnectionHandle _connection;
        private readonly IServer _server;
        private readonly TimeSpan _roundTripTime;
        private readonly InterlockedInt32 _state;
        private readonly bool _ownConnection;

        // constructors
        public ServerChannel(IServer server, IConnectionHandle connection, TimeSpan roundTripTime, bool ownConnection = true)
        {
            _server = server;
            _connection = connection;
            _roundTripTime = roundTripTime;
            _state = new InterlockedInt32(ChannelState.Initial);
            _ownConnection = ownConnection;
        }

        // properties
        public IConnectionHandle Connection => _connection;

        public ConnectionDescription ConnectionDescription => _connection.Description;

        public TimeSpan RoundTripTimeout => _roundTripTime;

        // methods
        public TResult Command<TResult>(
            OperationContext operationContext,
            ICoreSession session,
            ReadPreference readPreference,
            DatabaseNamespace databaseNamespace,
            BsonDocument command,
            IEnumerable<BatchableCommandMessageSection> commandPayloads,
            IElementNameValidator commandValidator,
            BsonDocument additionalOptions,
            Action<IMessageEncoderPostProcessor> postWriteAction,
            CommandResponseHandling responseHandling,
            IBsonSerializer<TResult> resultSerializer,
            MessageEncoderSettings messageEncoderSettings)
        {
            var protocol = new CommandWireProtocol<TResult>(
                CreateClusterClockAdvancingCoreSession(session),
                readPreference,
                databaseNamespace,
                command,
                commandPayloads,
                commandValidator,
                additionalOptions,
                postWriteAction,
                responseHandling,
                resultSerializer,
                messageEncoderSettings,
                _server.ServerApi,
                _roundTripTime);

            return ExecuteProtocol(operationContext, protocol, session);
        }

        public Task<TResult> CommandAsync<TResult>(
            OperationContext operationContext,
            ICoreSession session,
            ReadPreference readPreference,
            DatabaseNamespace databaseNamespace,
            BsonDocument command,
            IEnumerable<BatchableCommandMessageSection> commandPayloads,
            IElementNameValidator commandValidator,
            BsonDocument additionalOptions,
            Action<IMessageEncoderPostProcessor> postWriteAction,
            CommandResponseHandling responseHandling,
            IBsonSerializer<TResult> resultSerializer,
            MessageEncoderSettings messageEncoderSettings)
        {
            var protocol = new CommandWireProtocol<TResult>(
                CreateClusterClockAdvancingCoreSession(session),
                readPreference,
                databaseNamespace,
                command,
                commandPayloads,
                commandValidator,
                additionalOptions,
                postWriteAction,
                responseHandling,
                resultSerializer,
                messageEncoderSettings,
                _server.ServerApi,
                _roundTripTime);

            return ExecuteProtocolAsync(operationContext, protocol, session);
        }

        public void Dispose()
        {
            if (_state.TryChange(ChannelState.Initial, ChannelState.Disposed))
            {
                if (_ownConnection)
                {
                    _server.ReturnConnection(_connection);
                }

                _connection.Dispose();
            }
        }

        private ICoreSession CreateClusterClockAdvancingCoreSession(ICoreSession session)
        {
            return new ClusterClockAdvancingCoreSession(session, _server.ClusterClock);
        }

        private TResult ExecuteProtocol<TResult>(OperationContext operationContext, IWireProtocol<TResult> protocol, ICoreSession session)
        {
            try
            {
                return protocol.Execute(operationContext, _connection);
            }
            catch (Exception ex)
            {
                MarkSessionDirtyIfNeeded(session, ex);
                _server.HandleChannelException(_connection, ex);
                throw;
            }
        }

        private async Task<TResult> ExecuteProtocolAsync<TResult>(OperationContext operationContext, IWireProtocol<TResult> protocol, ICoreSession session)
        {
            try
            {
                return await protocol.ExecuteAsync(operationContext, _connection).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                MarkSessionDirtyIfNeeded(session, ex);
                _server.HandleChannelException(_connection, ex);
                throw;
            }
        }

        public IChannelHandle Fork()
        {
            ThrowIfDisposed();

            return new ServerChannel(_server, _connection.Fork(), _roundTripTime, false);
        }

        private void MarkSessionDirtyIfNeeded(ICoreSession session, Exception ex)
        {
            if (ex is MongoConnectionException)
            {
                session.MarkDirty();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_state.Value == ChannelState.Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        // nested types
        private static class ChannelState
        {
            public const int Initial = 0;
            public const int Disposed = 1;
        }
    }
}



