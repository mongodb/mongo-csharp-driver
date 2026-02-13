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
using System.Threading.Tasks;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Connections
{
    internal interface IConnection : IDisposable
    {
        ConnectionId ConnectionId { get; }
        ConnectionDescription Description { get; }
        EndPoint EndPoint { get; }
        int Generation { get; }
        bool IsExpired { get; }
        ConnectionSettings Settings { get; }

        void Open(OperationContext operationContext);
        Task OpenAsync(OperationContext operationContext);
        void Reauthenticate(OperationContext operationContext);
        Task ReauthenticateAsync(OperationContext operationContext);
        ResponseMessage ReceiveMessage(OperationContext operationContext, int responseTo, IMessageEncoderSelector encoderSelector, MessageEncoderSettings messageEncoderSettings);
        Task<ResponseMessage> ReceiveMessageAsync(OperationContext operationContext, int responseTo, IMessageEncoderSelector encoderSelector, MessageEncoderSettings messageEncoderSettings);
        void SendMessage(OperationContext operationContext, RequestMessage message, MessageEncoderSettings messageEncoderSettings);
        Task SendMessageAsync(OperationContext operationContext, RequestMessage message, MessageEncoderSettings messageEncoderSettings);

        void CompleteCommandWithException(Exception exception);
    }

    internal interface IConnectionHandle : IConnection
    {
        IConnectionHandle Fork();
    }
}
