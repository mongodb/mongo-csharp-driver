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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.WireProtocol.Messages
{
    internal sealed class CommandMessage : MongoDBMessage
    {
        // static
        private static readonly HashSet<string> __messagesNotToBeCompressed = new HashSet<string>
        {
            "hello",
            OppressiveLanguageConstants.LegacyHelloCommandName,
            "saslStart",
            "saslContinue",
            "getnonce",
            "authenticate",
            "createUser",
            "updateUser",
            "copydbsaslstart",
            "copydb"
        };

        // fields
        private bool _exhaustAllowed;
        private bool _moreToCome;
        private Action<IMessageEncoderPostProcessor> _postWriteAction;
        private readonly int _requestId;
        private readonly int _responseTo;
        private readonly List<CommandMessageSection> _sections;

        // constructors
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
        public bool ExhaustAllowed
        {
            get { return _exhaustAllowed; }
            set { _exhaustAllowed = value; }
        }

        public override bool MayBeCompressed
        {
            get
            {
                var type0Section = _sections.OfType<Type0CommandMessageSection>().Single();
                var command = (BsonDocument)type0Section.Document; // could be a RawBsonDocument but that's OK
                var commandName = command.GetElement(0).Name;

                if (__messagesNotToBeCompressed.Contains(commandName))
                {
                    return false;
                }

                return true;
            }
        }

        public override MongoDBMessageType MessageType => MongoDBMessageType.Command;

        public bool MoreToCome
        {
            get { return _moreToCome; }
            set { _moreToCome = value; }
        }

        public Action<IMessageEncoderPostProcessor> PostWriteAction
        {
            get { return _postWriteAction; }
            set { _postWriteAction = value; }
        }

        public int RequestId => _requestId;
        public bool ResponseExpected => !_moreToCome;
        public int ResponseTo => _responseTo;
        public IReadOnlyList<CommandMessageSection> Sections => _sections;

        // public methods
        public override IMessageEncoder GetEncoder(IMessageEncoderFactory encoderFactory)
        {
            return encoderFactory.GetCommandMessageEncoder();
        }
    }
}
