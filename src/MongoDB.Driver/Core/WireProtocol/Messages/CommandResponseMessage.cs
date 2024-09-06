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

using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.WireProtocol.Messages
{
    internal sealed class CommandResponseMessage : ResponseMessage
    {
        // private fields
        private readonly CommandMessage _wrappedMessage;

        // constructors
        public CommandResponseMessage(CommandMessage wrappedMessage)
            : base(Ensure.IsNotNull(wrappedMessage, nameof(wrappedMessage)).RequestId, wrappedMessage.ResponseTo)
        {
            _wrappedMessage = Ensure.IsNotNull(wrappedMessage, nameof(wrappedMessage));
        }

        // public properties
        public override MongoDBMessageType MessageType => _wrappedMessage.MessageType;
        public CommandMessage WrappedMessage => _wrappedMessage;

        // public methods
        public override IMessageEncoder GetEncoder(IMessageEncoderFactory encoderFactory)
        {
            return encoderFactory.GetCommandResponseMessageEncoder();
        }
    }
}
