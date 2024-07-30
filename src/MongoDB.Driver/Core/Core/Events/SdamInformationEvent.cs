/* Copyright 2018–present MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Events
{
    /// <summary>
    /// An informational event used for logging Server Discovery and Monitoring (SDAM) events.
    /// </summary>
    public struct SdamInformationEvent : IEvent
    {
        private readonly object _arg0;
        private readonly object[] _args;
        private readonly int _argsCount;

        private readonly string _messageFormat;
        private readonly DateTime _timestamp;

        private string _formattedMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="SdamInformationEvent"/> struct.
        /// </summary>
        /// <param name="messageFormat">Message format.</param>
        /// <param name="arg0">Message argument.</param>
        public SdamInformationEvent(string messageFormat, object arg0) :
            this(messageFormat, 1, arg0, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SdamInformationEvent"/> struct.
        /// </summary>
        /// <param name="messageFormat">Message format.</param>
        /// <param name="args">Message arguments.</param>
        public SdamInformationEvent(string messageFormat, params object[] args) :
            this(messageFormat, -1, null, args)
        {
        }

        private SdamInformationEvent(string messageFormat, int argsCount, object arg0, params object[] args)
        {
            _args = args;
            _arg0 = arg0;
            _argsCount = argsCount;
            _messageFormat = Ensure.IsNotNull(messageFormat, nameof(messageFormat));
            _timestamp = DateTime.UtcNow;
            _formattedMessage = null;
        }

        /// <summary>
        /// Gets the message.
        /// </summary>
        public string Message
        {
            get
            {
                if (_formattedMessage == null)
                {
                    _formattedMessage = _argsCount switch
                    {
                        -1 => string.Format(_messageFormat, _args),
                        1 => string.Format(_messageFormat, _arg0),
                        _ => throw new InvalidOperationException($"Not supported argument count {_argsCount}")
                    };
                }

                return _formattedMessage;
            }
        }

        /// <summary>
        /// Gets the timestamp.
        /// </summary>
        public DateTime Timestamp => _timestamp;

        // explicit interface implementations
        EventType IEvent.Type => EventType.SdamInformation;

        /// <inheritdoc />
        public override string ToString()
        {
            return $@"{{type: ""{GetType().Name}"", message: ""{Message}"" }}";
        }
    }
}
