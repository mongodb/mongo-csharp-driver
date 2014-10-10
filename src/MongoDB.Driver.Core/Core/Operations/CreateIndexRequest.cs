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
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
    public class CreateIndexRequest
    {
        // fields
        private BsonDocument _additionalOptions;
        private bool? _background;
        private bool? _dropDups;
        private string _indexName;
        private BsonDocument _keys;
        private bool? _sparse;
        private TimeSpan? _timeToLive;
        private bool? _unique;

        // constructors
        public CreateIndexRequest(
            BsonDocument keys)
        {
            _keys = Ensure.IsNotNull(keys, "keys");
        }

        // properties
        public BsonDocument AdditionalOptions
        {
            get { return _additionalOptions; }
            set { _additionalOptions = value; }
        }

        public bool? Background
        {
            get { return _background; }
            set { _background = value; }
        }

        public bool? DropDups
        {
            get { return _dropDups; }
            set { _dropDups = value; }
        }

        public string IndexName
        {
            get { return _indexName; }
            set { _indexName = Ensure.IsNotNullOrEmpty(value, "value"); }
        }

        public BsonDocument Keys
        {
            get { return _keys; }
            set { _keys = Ensure.IsNotNull(value, "value"); }
        }

        public bool? Sparse
        {
            get { return _sparse; }
            set { _sparse = value; }
        }

        public TimeSpan? TimeToLive
        {
            get { return _timeToLive; }
            set { _timeToLive = Ensure.IsNullOrGreaterThanZero(value, "value"); }
        }

        public bool? Unique
        {
            get { return _unique; }
            set { _unique = value; }
        }

        // methods
        public BsonDocument CreateIndexDocument()
        {
            var additionalOptionsName = _additionalOptions == null ? null : _additionalOptions.GetValue("name", null);
            var name = _indexName ?? additionalOptionsName ?? CreateIndexesOperation.GetIndexName(_keys);
            var document = new BsonDocument
            {
                { "key", _keys },
                { "name", name },
                { "background", () => _background.Value, _background.HasValue },
                { "dropDups", () => _dropDups.Value, _dropDups.HasValue },
                { "sparse", () => _sparse.Value, _sparse.HasValue },
                { "unique", () => _unique.Value, _unique.HasValue },
                { "expireAfterSeconds", () => _timeToLive.Value.TotalSeconds, _timeToLive.HasValue },
            };
            if (_additionalOptions != null)
            {
                document.Merge(_additionalOptions, overwriteExistingElements: false);
            }
            return document;
        }
    }
}
