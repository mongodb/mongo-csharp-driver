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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    public class GroupOperation<TResult> : IReadOperation<IEnumerable<TResult>>
    {
        // fields
        private readonly CollectionNamespace _collectionNamespace;
        private readonly BsonDocument _filter;
        private BsonJavaScript _finalizeFunction;
        private readonly BsonDocument _initial;
        private readonly BsonDocument _key;
        private readonly BsonJavaScript _keyFunction;
        private TimeSpan? _maxTime;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly BsonJavaScript _reduceFunction;
        private IBsonSerializer<TResult> _resultSerializer;

        // constructors
        public GroupOperation(CollectionNamespace collectionNamespace, BsonDocument key, BsonDocument initial, BsonJavaScript reduceFunction, BsonDocument filter, MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _key = Ensure.IsNotNull(key, "key");
            _initial = Ensure.IsNotNull(initial, "initial");
            _reduceFunction = Ensure.IsNotNull(reduceFunction, "reduceFunction");
            _filter = filter;
            _messageEncoderSettings = messageEncoderSettings;
        }

        public GroupOperation(CollectionNamespace collectionNamespace, BsonJavaScript keyFunction, BsonDocument initial, BsonJavaScript reduceFunction, BsonDocument filter, MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _keyFunction = Ensure.IsNotNull(keyFunction, "keyFunction");
            _initial = Ensure.IsNotNull(initial, "initial");
            _reduceFunction = Ensure.IsNotNull(reduceFunction, "reduceFunction");
            _filter = filter;
            _messageEncoderSettings = messageEncoderSettings;
        }

        // properties
        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        public BsonDocument Filter
        {
            get { return _filter; }
        }

        public BsonJavaScript FinalizeFunction
        {
            get { return _finalizeFunction; }
            set { _finalizeFunction = value; }
        }

        public BsonDocument Initial
        {
            get { return _initial; }
        }

        public BsonDocument Key
        {
            get { return _key; }
        }

        public BsonJavaScript KeyFunction
        {
            get { return _keyFunction; }
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
            set { _maxTime = value; }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public BsonJavaScript ReduceFunction
        {
            get { return _reduceFunction; }
        }

        public IBsonSerializer<TResult> ResultSerializer
        {
            get { return _resultSerializer; }
            set { _resultSerializer = value; }
        }

        // methods
        public BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "group", new BsonDocument
                    {
                        { "ns", _collectionNamespace.CollectionName },
                        { "key", _key, _key != null },
                        { "$keyf", _keyFunction, _keyFunction != null },
                        { "$reduce", _reduceFunction },
                        { "initial", _initial },
                        { "cond", _filter, _filter != null },
                        { "finalize", _finalizeFunction, _finalizeFunction != null }
                    }
                },
                { "maxTimeMS", () => _maxTime.Value.TotalMilliseconds, _maxTime.HasValue }
           };
        }

        public async Task<IEnumerable<TResult>> ExecuteAsync(IReadBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var resultSerializer = _resultSerializer ?? BsonSerializer.LookupSerializer<TResult>();
            var resultArraySerializer = new ArraySerializer<TResult>(resultSerializer);
            var commandResultSerializer = new ElementDeserializer<TResult[]>("retval", resultArraySerializer);
            var operation = new ReadCommandOperation<TResult[]>(_collectionNamespace.DatabaseNamespace, command, commandResultSerializer, _messageEncoderSettings);
            return await operation.ExecuteAsync(binding, cancellationToken).ConfigureAwait(false);
        }
    }
}
