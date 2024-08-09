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
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations.ElementNameValidators;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a find one and update operation.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public class FindOneAndUpdateOperation<TResult> : FindAndModifyOperationBase<TResult>
    {
        // fields
        private IEnumerable<BsonDocument> _arrayFilters;
        private bool? _bypassDocumentValidation;
        private readonly BsonDocument _filter;
        private BsonValue _hint;
        private bool _isUpsert;
        private BsonDocument _let;
        private TimeSpan? _maxTime;
        private BsonDocument _projection;
        private ReturnDocument _returnDocument;
        private BsonDocument _sort;
        private readonly BsonValue _update;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="FindOneAndUpdateOperation{TResult}"/> class.
        /// </summary>
        /// <param name="collectionNamespace">The collection namespace.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="update">The update.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public FindOneAndUpdateOperation(CollectionNamespace collectionNamespace, BsonDocument filter, BsonValue update, IBsonSerializer<TResult> resultSerializer, MessageEncoderSettings messageEncoderSettings)
            : base(collectionNamespace, resultSerializer, messageEncoderSettings)
        {
            _filter = Ensure.IsNotNull(filter, nameof(filter));
            _update = EnsureUpdateIsValid(update);
            _returnDocument = ReturnDocument.Before;
        }

        // properties
        /// <summary>
        /// Gets or sets the array filters.
        /// </summary>
        /// <value>
        /// The array filters.
        /// </value>
        public IEnumerable<BsonDocument> ArrayFilters
        {
            get { return _arrayFilters; }
            set { _arrayFilters = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to bypass document validation.
        /// </summary>
        /// <value>
        /// A value indicating whether to bypass document validation.
        /// </value>
        public bool? BypassDocumentValidation
        {
            get { return _bypassDocumentValidation; }
            set { _bypassDocumentValidation = value; }
        }

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
        /// Gets a value indicating whether a document should be inserted if no matching document is found.
        /// </summary>
        /// <value>
        ///   <c>true</c> if a document should be inserted if no matching document is found; otherwise, <c>false</c>.
        /// </value>
        public bool IsUpsert
        {
            get { return _isUpsert; }
            set { _isUpsert = value; }
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
        /// Gets or sets which version of the modified document to return.
        /// </summary>
        /// <value>
        /// Which version of the modified document to return.
        /// </value>
        public ReturnDocument ReturnDocument
        {
            get { return _returnDocument; }
            set { _returnDocument = value; }
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

        /// <summary>
        /// Gets or sets the update specification.
        /// </summary>
        /// <value>
        /// The update specification.
        /// </value>
        public BsonValue Update
        {
            get { return _update; }
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
                { "update", _update },
                { "new", true, _returnDocument == ReturnDocument.After },
                { "sort", _sort, _sort != null },
                { "fields", _projection, _projection != null },
                { "upsert", true, _isUpsert },
                { "maxTimeMS", () => MaxTimeHelper.ToMaxTimeMS(_maxTime.Value), _maxTime.HasValue },
                { "writeConcern", writeConcern, writeConcern != null },
                { "bypassDocumentValidation", () => _bypassDocumentValidation.Value, _bypassDocumentValidation.HasValue },
                { "collation", () => Collation.ToBsonDocument(), Collation != null },
                { "comment", Comment, Comment != null },
                { "hint", _hint, _hint != null },
                { "arrayFilters", () => new BsonArray(_arrayFilters), _arrayFilters != null },
                { "txnNumber", () => transactionNumber, transactionNumber.HasValue },
                { "let", _let, _let != null }
            };
        }

        /// <inheritdoc/>
        protected override IElementNameValidator GetCommandValidator()
        {
            return Validator.Instance;
        }

        // private methods
        private BsonValue EnsureUpdateIsValid(BsonValue update)
        {
            Ensure.IsNotNull(update, nameof(update));

            switch (update)
            {
                case BsonDocument document:
                    {
                        if (document.ElementCount == 0)
                        {
                            throw new ArgumentException("Updates must have at least 1 update operator.", nameof(update));
                        }

                        break;
                    }
                case BsonArray array:
                    {
                        if (array.Count == 0)
                        {
                            throw new ArgumentException("Updates must have at least 1 update operator in a pipeline.", nameof(update));
                        }

                        break;
                    }
                default:
                    throw new ArgumentException("Updates must be BsonDocument or BsonArray.", nameof(update));
            }

            return update;
        }

        private class Validator : IElementNameValidator
        {
            public readonly static Validator Instance = new Validator();

            public IElementNameValidator GetValidatorForChildContent(string elementName)
            {
                if (elementName == "update")
                {
                    return UpdateElementNameValidator.Instance;
                }

                return NoOpElementNameValidator.Instance;
            }

            public bool IsValidElementName(string elementName)
            {
                Ensure.IsNotNull(elementName, nameof(elementName));
                return true;
            }
        }
    }
}
