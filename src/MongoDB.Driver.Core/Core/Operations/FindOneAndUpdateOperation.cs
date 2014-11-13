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
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations.ElementNameValidators;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    public class FindOneAndUpdateOperation<TResult> : FindAndModifyOperationBase<TResult>
    {
        // fields
        private readonly BsonDocument _criteria;
        private bool _isUpsert;
        private TimeSpan? _maxTime;
        private BsonDocument _projection;
        private bool _returnOriginal;
        private BsonDocument _sort;
        private readonly BsonDocument _update;

        // constructors
        public FindOneAndUpdateOperation(CollectionNamespace collectionNamespace, BsonDocument criteria, BsonDocument update, IBsonSerializer<TResult> resultSerializer, MessageEncoderSettings messageEncoderSettings)
            : base(collectionNamespace, resultSerializer, messageEncoderSettings)
        {
            _criteria = Ensure.IsNotNull(criteria, "criteria");
            _update = Ensure.IsNotNull(update, "update");
            _returnOriginal = true;
        }

        // properties
        public BsonDocument Criteria
        {
            get { return _criteria; }
        }

        public bool IsUpsert
        {
            get { return _isUpsert; }
            set { _isUpsert = value; }
        }

        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = value; }
        }

        public BsonDocument Projection
        {
            get { return _projection; }
            set { _projection = value; }
        }

        public bool ReturnOriginal
        {
            get { return _returnOriginal; }
            set { _returnOriginal = value; }
        }

        public BsonDocument Sort
        {
            get { return _sort; }
            set { _sort = value; }
        }

        public BsonDocument Update
        {
            get { return _update; }
        }

        // methods
        public override BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "findAndModify", CollectionNamespace.CollectionName },
                { "query", _criteria },
                { "sort", _sort, _sort != null },
                { "update", _update },
                { "new", !_returnOriginal },
                { "fields", _projection, _projection != null },
                { "upsert", _isUpsert },
                { "maxTimeMS", () => _maxTime.Value.TotalMilliseconds, _maxTime.HasValue }
            };
        }

        protected override IElementNameValidator GetCommandValidator()
        {
            return Validator.Instance;
        }

        private class Validator : IElementNameValidator
        {
            public readonly static Validator Instance = new Validator();

            public IElementNameValidator GetValidatorForChildContent(string elementName)
            {
                if(elementName == "update")
                {
                    return UpdateElementNameValidator.Instance;
                }

                return NoOpElementNameValidator.Instance;
            }

            public bool IsValidElementName(string elementName)
            {
                return true;
            }
        }
    }
}
