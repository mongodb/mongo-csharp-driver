/* Copyright 2017-present MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Options for a change stream operation.
    /// </summary>
    public class ChangeStreamOptions
    {
        // private fields
        private int? _batchSize;
        private Collation _collation;
        private BsonValue _comment;
        private ChangeStreamFullDocumentOption _fullDocument = ChangeStreamFullDocumentOption.Default;
        private ChangeStreamFullDocumentBeforeChangeOption _fullDocumentBeforeChange = ChangeStreamFullDocumentBeforeChangeOption.Default;
        private TimeSpan? _maxAwaitTime;
        private BsonDocument _resumeAfter;
        private bool? _showExpandedEvents;
        private BsonDocument _startAfter;
        private BsonTimestamp _startAtOperationTime;

        // public properties
        /// <summary>
        /// Gets or sets the size of the batch.
        /// </summary>
        /// <value>
        /// The size of the batch.
        /// </value>
        public int? BatchSize
        {
            get { return _batchSize; }
            set { _batchSize = Ensure.IsNullOrGreaterThanZero(value, nameof(value)); }
        }

        /// <summary>
        /// Gets or sets the collation.
        /// </summary>
        /// <value>
        /// The collation.
        /// </value>
        public Collation Collation
        {
            get { return _collation; }
            set { _collation = value; }
        }

        /// <summary>
        /// Gets or sets the comment.
        /// </summary>
        /// <value>
        /// The comment.
        /// </value>
        public BsonValue Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }

        /// <summary>
        /// Gets or sets the full document.
        /// </summary>
        /// <value>
        /// The full document.
        /// </value>
        public ChangeStreamFullDocumentOption FullDocument
        {
            get { return _fullDocument; }
            set { _fullDocument = value; }
        }

        /// <summary>
        /// Gets or sets the full document before change.
        /// </summary>
        /// <value>
        /// The full document before change.
        /// </value>
        public ChangeStreamFullDocumentBeforeChangeOption FullDocumentBeforeChange
        {
            get { return _fullDocumentBeforeChange; }
            set { _fullDocumentBeforeChange = value; }
        }

        /// <summary>
        /// Gets or sets the maximum await time.
        /// </summary>
        /// <value>
        /// The maximum await time.
        /// </value>
        public TimeSpan? MaxAwaitTime
        {
            get { return _maxAwaitTime; }
            set { _maxAwaitTime = Ensure.IsNullOrGreaterThanZero(value, nameof(value)); }
        }

        /// <summary>
        /// Gets or sets the resume after.
        /// </summary>
        /// <value>
        /// The resume after.
        /// </value>
        public BsonDocument ResumeAfter
        {
            get { return _resumeAfter; }
            set { _resumeAfter = value; }
        }

        /// <summary>
        /// Gets or sets whether the change stream should show expanded events (MongoDB 6.0 and later).
        /// Expanded change stream events include:
        /// <list type="bullet">
        ///     <item><description><see cref="ChangeStreamOperationType.Create"/></description></item>
        ///     <item><description><see cref="ChangeStreamOperationType.CreateIndexes"/></description></item>
        ///     <item><description><see cref="ChangeStreamOperationType.DropIndexes"/></description></item>
        ///     <item><description><see cref="ChangeStreamOperationType.Modify"/></description></item>
        ///     <item><description><see cref="ChangeStreamOperationType.RefineCollectionShardKey"/></description></item>
        ///     <item><description><see cref="ChangeStreamOperationType.ReshardCollection"/></description></item>
        ///     <item><description><see cref="ChangeStreamOperationType.ShardCollection"/></description></item>
        /// </list>
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public bool? ShowExpandedEvents
        {
            get { return _showExpandedEvents; }
            set { _showExpandedEvents = value; }
        }

        /// <summary>
        /// Gets or sets the start after.
        /// </summary>
        /// <value>
        /// The start after.
        /// </value>
        public BsonDocument StartAfter
        {
            get { return _startAfter; }
            set { _startAfter = value; }
        }

        /// <summary>
        /// Gets or sets the start at operation time.
        /// </summary>
        /// <value>
        /// The start at operation time.
        /// </value>
        public BsonTimestamp StartAtOperationTime
        {
            get { return _startAtOperationTime; }
            set { _startAtOperationTime = value; }
        }
    }
}
