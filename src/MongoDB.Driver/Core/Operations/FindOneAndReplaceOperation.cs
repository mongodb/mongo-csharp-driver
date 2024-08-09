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
    internal sealed class FindOneAndReplaceOperation<TResult> : FindAndModifyOperationBase<TResult>
    {
        private bool? _bypassDocumentValidation;
        private readonly BsonDocument _filter;
        private BsonValue _hint;
        private bool _isUpsert;
        private BsonDocument _let;
        private TimeSpan? _maxTime;
        private BsonDocument _projection;
        private readonly BsonDocument _replacement;
        private ReturnDocument _returnDocument;
        private BsonDocument _sort;

        public FindOneAndReplaceOperation(CollectionNamespace collectionNamespace, BsonDocument filter, BsonDocument replacement, IBsonSerializer<TResult> resultSerializer, MessageEncoderSettings messageEncoderSettings)
            : base(collectionNamespace, resultSerializer, messageEncoderSettings)
        {
            _filter = Ensure.IsNotNull(filter, nameof(filter));
            _replacement = Ensure.IsNotNull(replacement, nameof(replacement));
            _returnDocument = ReturnDocument.Before;
        }

        public bool? BypassDocumentValidation
        {
            get { return _bypassDocumentValidation; }
            set { _bypassDocumentValidation = value; }
        }

        public BsonDocument Filter
        {
            get { return _filter; }
        }

        public BsonValue Hint
        {
            get { return _hint; }
            set { _hint = value; }
        }

        public bool IsUpsert
        {
            get { return _isUpsert; }
            set { _isUpsert = value; }
        }

        public BsonDocument Let
        {
            get { return _let; }
            set { _let = value; }
        }

        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = Ensure.IsNullOrInfiniteOrGreaterThanOrEqualToZero(value, nameof(value)); }
        }

        public BsonDocument Projection
        {
            get { return _projection; }
            set { _projection = value; }
        }

        public BsonDocument Replacement
        {
            get { return _replacement; }
        }

        public ReturnDocument ReturnDocument
        {
            get { return _returnDocument; }
            set { _returnDocument = value; }
        }

        public BsonDocument Sort
        {
            get { return _sort; }
            set { _sort = value; }
        }

        public override BsonDocument CreateCommand(ICoreSessionHandle session, ConnectionDescription connectionDescription, long? transactionNumber)
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
                { "update", _replacement },
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
                { "txnNumber", () => transactionNumber, transactionNumber.HasValue },
                { "let", _let, _let != null }
            };
        }

        protected override IElementNameValidator GetCommandValidator()
        {
            return NoOpElementNameValidator.Instance;
        }
    }
}
