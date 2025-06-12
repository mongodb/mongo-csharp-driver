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
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class GroupOperation<TResult> : IReadOperation<IEnumerable<TResult>>
    {
        private Collation _collation;
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

        public GroupOperation(CollectionNamespace collectionNamespace, BsonDocument key, BsonDocument initial, BsonJavaScript reduceFunction, BsonDocument filter, MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _key = Ensure.IsNotNull(key, nameof(key));
            _initial = Ensure.IsNotNull(initial, nameof(initial));
            _reduceFunction = Ensure.IsNotNull(reduceFunction, nameof(reduceFunction));
            _filter = filter; // can be null
            _messageEncoderSettings = messageEncoderSettings;
        }

        public GroupOperation(CollectionNamespace collectionNamespace, BsonJavaScript keyFunction, BsonDocument initial, BsonJavaScript reduceFunction, BsonDocument filter, MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _keyFunction = Ensure.IsNotNull(keyFunction, nameof(keyFunction));
            _initial = Ensure.IsNotNull(initial, nameof(initial));
            _reduceFunction = Ensure.IsNotNull(reduceFunction, nameof(reduceFunction));
            _filter = filter;
            _messageEncoderSettings = messageEncoderSettings;
        }

        public Collation Collation
        {
            get { return _collation; }
            set { _collation = value; }
        }

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

        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = Ensure.IsNullOrInfiniteOrGreaterThanOrEqualToZero(value, nameof(value)); }
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
                        { "finalize", _finalizeFunction, _finalizeFunction != null },
                        { "collation", () => _collation.ToBsonDocument(), _collation != null }
                    }
                },
                { "maxTimeMS", () => MaxTimeHelper.ToMaxTimeMS(_maxTime.Value), _maxTime.HasValue }
           };
        }

        public IEnumerable<TResult> Execute(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));
            using (var channelSource = binding.GetReadChannelSource(operationContext))
            using (var channel = channelSource.GetChannel(operationContext))
            using (var channelBinding = new ChannelReadBinding(channelSource.Server, channel, binding.ReadPreference, binding.Session.Fork()))
            {
                var operation = CreateOperation();
                return operation.Execute(operationContext, channelBinding);
            }
        }

        public async Task<IEnumerable<TResult>> ExecuteAsync(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));
            using (var channelSource = await binding.GetReadChannelSourceAsync(operationContext).ConfigureAwait(false))
            using (var channel = await channelSource.GetChannelAsync(operationContext).ConfigureAwait(false))
            using (var channelBinding = new ChannelReadBinding(channelSource.Server, channel, binding.ReadPreference, binding.Session.Fork()))
            {
                var operation = CreateOperation();
                return await operation.ExecuteAsync(operationContext, channelBinding).ConfigureAwait(false);
            }
        }

        private ReadCommandOperation<TResult[]> CreateOperation()
        {
            var command = CreateCommand();
            var resultSerializer = _resultSerializer ?? BsonSerializer.LookupSerializer<TResult>();
            var resultArraySerializer = new ArraySerializer<TResult>(resultSerializer);
            var commandResultSerializer = new ElementDeserializer<TResult[]>("retval", resultArraySerializer);
            return new ReadCommandOperation<TResult[]>(_collectionNamespace.DatabaseNamespace, command, commandResultSerializer, _messageEncoderSettings)
            {
                RetryRequested = false
            };
        }
    }
}
