/* Copyright 2013-present MongoDB Inc.
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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a find one and delete operation.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public class FindOneAndDeleteOperation<TResult> : FindAndModifyOperationBase<TResult>
    {
        // fields
        private readonly BsonDocument _filter;
        private BsonValue _hint;
        private BsonDocument _let;
        private TimeSpan? _maxTime;
        private BsonDocument _projection;
        private BsonDocument _sort;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="FindOneAndDeleteOperation{TResult}"/> class.
        /// </summary>
        /// <param name="collectionNamespace">The collection namespace.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public FindOneAndDeleteOperation(CollectionNamespace collectionNamespace, BsonDocument filter, IBsonSerializer<TResult> resultSerializer, MessageEncoderSettings messageEncoderSettings)
            : base(collectionNamespace, resultSerializer, messageEncoderSettings)
        {
            _filter = Ensure.IsNotNull(filter, nameof(filter));
        }

        // properties
        /// <summary>
        /// Gets the filter.
        /// </summary>
        /// <value>
        /// The filter.
        /// </value>
        public BsonDocument Filter
        {
            get { return _filter; }
        }

        /// <summary>
        /// Gets or sets the hint.
        /// </summary>
        /// <value>
        /// The hint.
        /// </value>
        public BsonValue Hint
        {
            get { return _hint; }
            set { _hint = value; }
        }

        /// <summary>
        /// Gets or sets the let document.
        /// </summary>
        /// <value>
        /// The let document.
        /// </value>
        public BsonDocument Let
        {
            get { return _let; }
            set { _let = value; }
        }

        /// <summary>
        /// Gets or sets the maximum time the server should spend on this operation.
        /// </summary>
        /// <value>
        /// The maximum time the server should spend on this operation.
        /// </value>
        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = Ensure.IsNullOrInfiniteOrGreaterThanOrEqualToZero(value, nameof(value)); }
        }

        /// <summary>
        /// Gets or sets the projection.
        /// </summary>
        /// <value>
        /// The projection.
        /// </value>
        public BsonDocument Projection
        {
            get { return _projection; }
            set { _projection = value; }
        }

        /// <summary>
        /// Gets or sets the sort specification.
        /// </summary>
        /// <value>
        /// The sort specification.
        /// </value>
        public BsonDocument Sort
        {
            get { return _sort; }
            set { _sort = value; }
        }

        // methods
        internal override BsonDocument CreateCommand(ICoreSessionHandle session, ConnectionDescription connectionDescription, long? transactionNumber)
        {
            var wireVersion = connectionDescription.MaxWireVersion;
            FindProjectionChecker.ThrowIfAggregationExpressionIsUsedWhenNotSupported(_projection, wireVersion);

            if (Feature.HintForFindAndModifyFeature.DriverMustThrowIfNotSupported(wireVersion) || (WriteConcern != null && !WriteConcern.IsAcknowledged))
            {
                if (_hint != null)
                {
                    throw new NotSupportedException($"Server version {WireVersion.GetServerVersionForErrorMessage(wireVersion)} does not support hints.");
                }
            }

            var writeConcern = WriteConcernHelper.GetEffectiveWriteConcern(session, WriteConcern);
            return new BsonDocument
            {
                { "findAndModify", CollectionNamespace.CollectionName },
                { "query", _filter },
                { "remove", true },
                { "sort", _sort, _sort != null },
                { "fields", _projection, _projection != null },
                { "maxTimeMS", () => MaxTimeHelper.ToMaxTimeMS(_maxTime.Value), _maxTime.HasValue },
                { "writeConcern", writeConcern, writeConcern != null },
                { "collation", () => Collation.ToBsonDocument(), Collation != null },
                { "comment", Comment, Comment != null },
                { "hint", _hint, _hint != null },
                { "txnNumber", () => transactionNumber, transactionNumber.HasValue },
                { "let", _let, _let != null }
            };
        }

        /// <inheritdoc/>
        protected override IElementNameValidator GetCommandValidator()
        {
            return NoOpElementNameValidator.Instance;
        }
    }
}
