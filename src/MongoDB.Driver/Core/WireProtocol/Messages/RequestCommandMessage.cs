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
using System.Threading;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.WireProtocol.Messages
{
    internal sealed class RequestCommandMessage : CommandMessage
    {
        // static
        private static int __requestId;

        // static properties
        public static int CurrentGlobalRequestId => __requestId;

        // static methods
        public static int GetNextRequestId()
        {
            return Interlocked.Increment(ref __requestId);
        }

        // fields

        // constructors
        public RequestCommandMessage(
            int requestId,
            IEnumerable<CommandMessageSection> sections,
            bool moreToCome)
            : base(requestId, sections, moreToCome)
        {
        }

        // public properties
        public bool ExhaustAllowed { get; set; }

        public Action<IMessageEncoderPostProcessor> PostWriteAction { get; set; }

        public bool ResponseExpected => !MoreToCome;

        public bool WasSent { get; set; }
    }
}
