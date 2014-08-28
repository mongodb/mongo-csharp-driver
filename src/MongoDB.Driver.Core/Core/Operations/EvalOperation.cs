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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    public class EvalOperation : IReadOperation<BsonValue>
    {
        // fields
        private DatabaseNamespace _databaseNamespace;
        private BsonJavaScript _javaScript;
        private TimeSpan? _maxTime;
        private MessageEncoderSettings _messageEncoderSettings;
        private bool? _nolock;

        // constructors
        public EvalOperation(
            DatabaseNamespace databaseNamespace,
            BsonJavaScript javaScript,
            MessageEncoderSettings messageEncoderSettings)
        {
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, "databaseNamespace");
            _javaScript = Ensure.IsNotNull(javaScript, "javaScript");
            _messageEncoderSettings = messageEncoderSettings;
        }

        // properties
        public DatabaseNamespace DatabaseNamespace
        {
            get { return _databaseNamespace; }
            set { _databaseNamespace = Ensure.IsNotNull(value, "value"); }
        }

        public BsonJavaScript JavaScript
        {
            get { return _javaScript; }
            set { _javaScript = Ensure.IsNotNull(value, "value"); }
        }

        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = Ensure.IsNullOrInfiniteOrGreaterThanOrEqualToZero(value, "value"); }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
            set { _messageEncoderSettings = value; }
        }

        public bool? Nolock
        {
            get { return _nolock; }
            set { _nolock = value; }
        }

        // methods
        public BsonDocument CreateCommand()
        {
            var javaScriptWithScope = _javaScript as BsonJavaScriptWithScope;
            return new BsonDocument
            {
                { "$eval", _javaScript.Code },
                { "args", () => javaScriptWithScope.Scope, javaScriptWithScope != null },
                { "nolock", () => _nolock.Value, _nolock.HasValue },
                { "maxTimeMS", () => _maxTime.Value.TotalMilliseconds, _maxTime.HasValue }
            };
        }

        public async Task<BsonValue> ExecuteAsync(IReadBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var operation = new ReadCommandOperation(_databaseNamespace, command, _messageEncoderSettings);
            var result = await operation.ExecuteAsync(binding, timeout, cancellationToken);
            return result["retval"];
        }
    }
}
