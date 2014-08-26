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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a write command operation.
    /// </summary>
    public class WriteCommandOperation : WriteCommandOperation<BsonDocument>
    {
        // constructors
        public WriteCommandOperation(string databaseName, BsonDocument command, MessageEncoderSettings messageEncoderSettings)
            : base(databaseName, command, BsonDocumentSerializer.Instance, messageEncoderSettings)
        {
        }
    }

    /// <summary>
    /// Represents a write command operation.
    /// </summary>
    public class WriteCommandOperation<TCommandResult> : CommandOperationBase<TCommandResult>, IWriteOperation<TCommandResult>
    {
        // constructors
        public WriteCommandOperation(string databaseName, BsonDocument command, IBsonSerializer<TCommandResult> resultSerializer, MessageEncoderSettings messageEncoderSettings)
            : base(databaseName, command, resultSerializer, messageEncoderSettings)
        {
        }

        // methods
        public async Task<TCommandResult> ExecuteAsync(IWriteBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var slidingTimeout = new SlidingTimeout(timeout);

            using (var connectionSource = await binding.GetWriteConnectionSourceAsync(slidingTimeout, cancellationToken))
            {
                return await ExecuteCommandAsync(connectionSource, ReadPreference.Primary, slidingTimeout, cancellationToken);
            }
        }
    }
}
