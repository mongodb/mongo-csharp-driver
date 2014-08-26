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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    public class DatabaseExistsOperation : IReadOperation<bool>
    {
        // fields
        private string _databaseName;
        private MessageEncoderSettings _messageEncoderSettings;

        // constructors
        public DatabaseExistsOperation(string databaseName, MessageEncoderSettings messageEncoderSettings)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _messageEncoderSettings = messageEncoderSettings;
        }

        // properties
        public string DatabaseName
        {
            get { return _databaseName; }
            set { _databaseName = Ensure.IsNotNullOrEmpty(value, "value"); }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
            set { _messageEncoderSettings = value; }
        }

        // methods
        public async Task<bool> ExecuteAsync(IReadBinding binding, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, "binding");
            var operation = new ListDatabaseNamesOperation(_messageEncoderSettings);
            var result = await operation.ExecuteAsync(binding, timeout, cancellationToken);
            return result.Contains(_databaseName);
        }
    }
}
