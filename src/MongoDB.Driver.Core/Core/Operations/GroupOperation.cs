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
    public class GroupOperation : IReadOperation<IEnumerable<BsonDocument>>
    {
        // fields
        private string _collectionName;
        private string _databaseName;
        private BsonJavaScript _finalizeFunction;
        private BsonDocument _initial;
        private BsonDocument _key;
        private BsonJavaScript _keyFunction;
        private TimeSpan? _maxTime;
        private MessageEncoderSettings _messageEncoderSettings;
        private BsonDocument _query;
        private BsonJavaScript _reduceFunction;

        // constructors
        public GroupOperation(string databaseName, string collectionName, BsonDocument key, BsonDocument initial, BsonJavaScript reduceFunction, BsonDocument query, MessageEncoderSettings messageEncoderSettings)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _key = Ensure.IsNotNull(key, "key");
            _initial = Ensure.IsNotNull(initial, "initial");
            _reduceFunction = Ensure.IsNotNull(reduceFunction, "reduceFunction");
            _query = query;
            _messageEncoderSettings = messageEncoderSettings;
        }

        public GroupOperation(string databaseName, string collectionName, BsonJavaScript keyFunction, BsonDocument initial, BsonJavaScript reduceFunction, BsonDocument query)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _keyFunction = Ensure.IsNotNull(keyFunction, "keyFunction");
            _initial = Ensure.IsNotNull(initial, "initial");
            _reduceFunction = Ensure.IsNotNull(reduceFunction, "reduceFunction");
            _query = query;
        }

        // properties
        public string CollectionName
        {
            get { return _collectionName; }
            set { _collectionName = Ensure.IsNotNullOrEmpty(value, "value"); }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
            set { _databaseName = Ensure.IsNotNullOrEmpty(value, "value"); }
        }

        public BsonJavaScript FinalizeFunction
        {
            get { return _finalizeFunction; }
            set { _finalizeFunction = value; }
        }

        public BsonDocument Initial
        {
            get { return _initial; }
            set { _initial = Ensure.IsNotNull(value, "value"); }
        }

        public BsonDocument Key
        {
            get { return _key; }
            set
            {
                _key = Ensure.IsNotNull(value, "value");
                _keyFunction = null;
            }
        }

        public BsonJavaScript KeyFunction
        {
            get { return _keyFunction; }
            set
            {
                _keyFunction = Ensure.IsNotNull(value, "value");
                _key = null;
            }
        }

        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = value; }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
            set { _messageEncoderSettings = value; }
        }

        public BsonDocument Query
        {
            get { return _query; }
            set { _query = value; }
        }

        public BsonJavaScript ReduceFunction
        {
            get { return _reduceFunction; }
            set { _reduceFunction = value; }
        }

        // methods
        public BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "group", new BsonDocument
                    {
                        { "ns", _collectionName },
                        { "key", _key, _key != null },
                        { "$keyf", _keyFunction, _keyFunction != null },
                        { "$reduce", _reduceFunction },
                        { "initial", _initial },
                        { "cond", _query, _query != null },
                        { "finalize", _finalizeFunction, _finalizeFunction != null }
                    }
                },
                { "maxTimeMS", () => _maxTime.Value.TotalMilliseconds, _maxTime.HasValue }
           };
        }

        public async Task<IEnumerable<BsonDocument>> ExecuteAsync(IReadBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var document = await ExecuteCommandAsync(binding, timeout, cancellationToken);
            return document["retval"].AsBsonArray.Cast<BsonDocument>();
        }

        public async Task<BsonDocument> ExecuteCommandAsync(IReadBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var operation = new ReadCommandOperation(_databaseName, command, _messageEncoderSettings);
            return await operation.ExecuteAsync(binding, timeout, cancellationToken);
        }
    }
}
