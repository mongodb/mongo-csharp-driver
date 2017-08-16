/* Copyright 2017 MongoDB Inc.
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

using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a change stream.
    /// </summary>
    /// <typeparam name="TOutput">The type of the output documents.</typeparam>
    /// <seealso cref="MongoDB.Driver.IAsyncCursor{TOutput}" />
    public sealed class ChangeStream<TOutput> : IAsyncCursor<TOutput>
    {
        // private fields
        private IAsyncCursor<TOutput> _cursor;
        private readonly ChangeStreamOptions _options;
        private readonly IReadOnlyList<BsonDocument> _pipeline;
        private readonly ReadPreference _readPreference;
        private BsonDocument _resumeToken;

        // public properties
        /// <inheritdoc/>
        public IEnumerable<TOutput> Current => null;

        // constructors
        internal ChangeStream(
            IAsyncCursor<TOutput> cursor,
            IReadOnlyList<BsonDocument> pipeline,
            ChangeStreamOptions options,
            ReadPreference readPreference)
        {
            _cursor = Ensure.IsNotNull(cursor, nameof(cursor));
            _pipeline = pipeline;
            _options = options;
            _readPreference = readPreference;
        }

        // public methods
        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public bool MoveNext(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
