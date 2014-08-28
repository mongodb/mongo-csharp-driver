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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    public class AggregateOperation : AggregateCursorOperationBase, IReadOperation<Cursor<BsonDocument>>
    {
        // constructors
        public AggregateOperation(CollectionNamespace collectionNamespace, IEnumerable<BsonDocument> pipeline, MessageEncoderSettings messageEncoderSettings)
            : base(collectionNamespace, pipeline, messageEncoderSettings)
        {
        }

        // methods
        private void EnsureIsReadOnlyPipeline()
        {
            if (Pipeline.Any(s => s.GetElement(0).Name == "$out"))
            {
                throw new ArgumentException("The pipeline for an AggregateOperation contains a $out operator. Use AggregateOutputToCollectionOperation instead.");
            }
        }

        public async Task<Cursor<BsonDocument>> ExecuteAsync(IReadBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            EnsureIsReadOnlyPipeline();

            var slidingTimeout = new SlidingTimeout(timeout);
            using (var connectionSource = await binding.GetReadConnectionSourceAsync(slidingTimeout, cancellationToken))
            {
                var command = CreateCommand();
                var operation = new ReadCommandOperation(CollectionNamespace.DatabaseNamespace, command, MessageEncoderSettings);
                var result = await operation.ExecuteAsync(connectionSource, binding.ReadPreference, slidingTimeout, cancellationToken);
                return CreateCursor(connectionSource, command, result, timeout, cancellationToken);
            }
        }

        public async Task<BsonDocument> ExplainAsync(IReadBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");

            var command = CreateCommand();
            command["explain"] = true;
            var operation = new ReadCommandOperation(CollectionNamespace.DatabaseNamespace, command, MessageEncoderSettings);
            return await operation.ExecuteAsync(binding, timeout, cancellationToken);
        }
    }
}
