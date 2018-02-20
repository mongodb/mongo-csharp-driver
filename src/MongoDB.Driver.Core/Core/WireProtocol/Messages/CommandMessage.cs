/* Copyright 2018-present MongoDB Inc.
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
using System.Linq;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.WireProtocol.Messages
{
    /// <summary>
    /// Represents a command message.
    /// </summary>
    /// <seealso cref="MongoDB.Driver.Core.WireProtocol.Messages.MongoDBMessage" />
    public sealed class CommandMessage : MongoDBMessage
    {
        // fields
        private readonly bool _moreToCome;
        private readonly int _requestId;
        private readonly int _responseTo;
        private readonly List<CommandMessageSection> _sections;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandMessage" /> class.
        /// </summary>
        /// <param name="requestId">The request identifier.</param>
        /// <param name="responseTo">The response to.</param>
        /// <param name="sections">The sections.</param>
        /// <param name="moreToCome">if set to <c>true</c> [more to come].</param>
        public CommandMessage(
            int requestId, 
            int responseTo, 
            IEnumerable<CommandMessageSection> sections,
            bool moreToCome)
        {
            _requestId = requestId;
            _responseTo = responseTo;
            _sections = Ensure.IsNotNull(sections, nameof(sections)).ToList();
            _moreToCome = moreToCome;

            if (_sections.Count(s => s.PayloadType == PayloadType.Type0) != 1)
            {
                throw new ArgumentException("There must be exactly one type 0 payload.", nameof(sections));
            }
        }

        // public properties
        /// <inheritdoc />
        public override MongoDBMessageType MessageType => MongoDBMessageType.Command;

        /// <summary>
        /// Gets a value indicating whether another message immediately follows this one.
        /// </summary>
        /// <value>
        ///   <c>true</c> if another message immediately follows this one; otherwise, <c>false</c>.
        /// </value>
        public bool MoreToCome => _moreToCome;

        /// <summary>
        /// Gets the request identifier.
        /// </summary>
        /// <value>
        /// The request identifier.
        /// </value>
        public int RequestId => _requestId;

        /// <summary>
        /// Gets the response to.
        /// </summary>
        /// <value>
        /// The response to.
        /// </value>
        public int ResponseTo => _responseTo;

        /// <summary>
        /// Gets the sections.
        /// </summary>
        /// <value>
        /// The sections.
        /// </value>
        public IReadOnlyList<CommandMessageSection> Sections => _sections;

        // public methods
        /// <inheritdoc />
        public override IMessageEncoder GetEncoder(IMessageEncoderFactory encoderFactory)
        {
            return encoderFactory.GetCommandMessageEncoder();
        }
    }
}
