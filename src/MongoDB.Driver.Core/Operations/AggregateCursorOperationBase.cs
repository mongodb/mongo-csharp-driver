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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;

namespace MongoDB.Driver.Core.Operations
{
    public abstract class AggregateCursorOperationBase : AggregateOperationBase
    {
        // fields
        private readonly int? _batchSize;
        private readonly AggregateResultMode _resultMode;

        // constructors
        protected AggregateCursorOperationBase(string databaseName, string collectionName, IEnumerable<BsonDocument> pipeline)
            : base(databaseName, collectionName, pipeline)
        {
            _resultMode = AggregateResultMode.Cursor;
        }

        protected AggregateCursorOperationBase(
            bool? allowDiskUsage,
            int? batchSize,
            string collectionName,
            string databaseName,
            AggregateResultMode resultMode,
            IReadOnlyList<BsonDocument> pipeline)
            : base(
                allowDiskUsage,
                collectionName,
                databaseName,
                pipeline)
        {
            _batchSize = batchSize;
            _resultMode = resultMode;
        }

        // properties
        public int? BatchSize
        {
            get { return _batchSize; }
        }

        public AggregateResultMode ResultMode
        {
            get { return _resultMode; }
        }

        // methods
        public override BsonDocument CreateCommand()
        {
            var command = base.CreateCommand();
            if (_resultMode == AggregateResultMode.Cursor)
            {
                command["cursor"] = new BsonDocument
                {
                    { "batchSize", () => _batchSize.Value, _batchSize.HasValue }
                };
            }
            return command;
        }

        protected Cursor<BsonDocument> CreateCursor(IConnectionSource connectionSource, BsonDocument command, BsonDocument result, TimeSpan timeout, CancellationToken cancellationToken)
        {
            switch (_resultMode)
            {
                case AggregateResultMode.Cursor: return CreateCursorFromCursorResult(connectionSource, command, result, timeout, cancellationToken);
                case AggregateResultMode.Inline: return CreateCursorFromInlineResult(command, result, timeout, cancellationToken);
                default: throw new ArgumentException(string.Format("Invalid AggregateResultMode: {0}.", _resultMode));
            }
        }

        private Cursor<BsonDocument> CreateCursorFromCursorResult(IConnectionSource connectionSource, BsonDocument command, BsonDocument result, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var firstBatch = ((BsonArray)result["cursor"]["firstBatch"]).Cast<BsonDocument>().ToList();
            var cursorId = result["cursor"]["id"].ToInt64();

            return new Cursor<BsonDocument>(
                connectionSource.Fork(),
                DatabaseName,
                CollectionName,
                command,
                firstBatch,
                cursorId,
                _batchSize ?? 0,
                BsonDocumentSerializer.Instance,
                timeout,
                cancellationToken);
        }

        private Cursor<BsonDocument> CreateCursorFromInlineResult(BsonDocument command, BsonDocument result, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var firstBatch = result["result"].AsBsonArray.Cast<BsonDocument>().ToList();

            return new Cursor<BsonDocument>(
                null,
                DatabaseName,
                CollectionName,
                command,
                firstBatch,
                0,
                0,
                null,
                timeout,
                cancellationToken);
        }
    }
}
