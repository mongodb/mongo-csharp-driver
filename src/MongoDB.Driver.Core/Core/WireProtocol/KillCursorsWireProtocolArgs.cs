/* Copyright 2013-2014 MongoDB Inc.
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
using System.Linq;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.WireProtocol
{
    public class KillCursorsWireProtocolArgs
    {
        // fields
        private readonly IReadOnlyList<long> _cursorIds;
        private readonly MessageEncoderSettings _messageEncoderSettings;

        // constructors
        public KillCursorsWireProtocolArgs(IEnumerable<long> cursorIds, MessageEncoderSettings messageEncoderSettings)
        {
            Ensure.IsNotNull(cursorIds, "cursorIds");
            _cursorIds = (cursorIds as IReadOnlyList<long>) ?? cursorIds.ToList();
            _messageEncoderSettings = messageEncoderSettings;
        }

        // properties
        public IReadOnlyList<long> CursorIds
        {
            get { return _cursorIds; }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }
    }
}
