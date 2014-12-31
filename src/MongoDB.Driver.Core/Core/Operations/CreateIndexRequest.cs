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
        private int? _bits;
        private double? _bucketSize;
        private string _defaultLanguage;
        private TimeSpan? _expireAfter;
        private string _languageOverride;
        private readonly BsonDocument _keys;
        private double? _max;
        private double? _min;
        private string _name;
        private bool? _sparse;
        private int? _sphereIndexVersion;
        private BsonDocument _storageEngine;
        private int? _textIndexVersion;
        private bool? _unique;
        private int? _version;
        private BsonDocument _weights;

        // constructors
        public CreateIndexRequest(BsonDocument keys)
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

        public int? Bits
        {
            get { return _bits; }
            set { _bits = value; }
        }

        public double? BucketSize
        {
            get { return _bucketSize; }
            set { _bucketSize = value; }
        }

        public string DefaultLanguage
        {
            get { return _defaultLanguage; }
            set { _defaultLanguage = value; }
        }

        public TimeSpan? ExpireAfter
        {
            get { return _expireAfter; }
            set { _expireAfter = value; }
        }

        public string LanguageOverride
        {
            get { return _languageOverride; }
            set { _languageOverride = value; }
        }

        public BsonDocument Keys
        {
            get { return _keys; }
        }

        public double? Max
        {
            get { return _max; }
            set { _max = value; }
        }

        public double? Min
        {
            get { return _min; }
            set { _min = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public bool? Sparse
        {
            get { return _sparse; }
            set { _sparse = value; }
        }

        public int? SphereIndexVersion
        {
            get { return _sphereIndexVersion; }
            set { _sphereIndexVersion = value; }
        }

        public BsonDocument StorageEngine
        {
            get { return _storageEngine; }
            set { _storageEngine = value; }
        }

        public int? TextIndexVersion
        {
            get { return _textIndexVersion; }
            set { _textIndexVersion = value; }
        }

        public bool? Unique
        {
            get { return _unique; }
            set { _unique = value; }
        }

        public int? Version
        {
            get { return _version; }
            set { _version = value; }
        }

        public BsonDocument Weights
        {
            get { return _weights; }
            set { _weights = value; }
        }

        // methods
        public BsonDocument CreateIndexDocument()
        {
            var additionalOptionsName = _additionalOptions == null ? null : _additionalOptions.GetValue("name", null);
            var name = _name ?? additionalOptionsName ?? IndexNameHelper.GetIndexName(_keys);
            var document = new BsonDocument
            {
                { "key", _keys },
                { "name", name },
                { "background", () => _background.Value, _background.HasValue },
                { "bits", () => _bits.Value, _bits.HasValue },
                { "bucketSize", () => _bucketSize.Value, _bucketSize.HasValue },
                { "default_language", () => _defaultLanguage, _defaultLanguage != null },
                { "expireAfterSeconds", () => _expireAfter.Value.TotalSeconds, _expireAfter.HasValue },
                { "language_override", () => _languageOverride, _languageOverride != null },
                { "max", () => _max.Value, _max.HasValue },
                { "min", () => _min.Value, _min.HasValue },
                { "sparse", () => _sparse.Value, _sparse.HasValue },
                { "2dsphereIndexVersion", () => _sphereIndexVersion.Value, _sphereIndexVersion.HasValue },
                { "storageEngine", () => _storageEngine, _storageEngine != null },
                { "textIndexVersion", () => _textIndexVersion.Value, _textIndexVersion.HasValue },
                { "unique", () => _unique.Value, _unique.HasValue },
                { "v", () => _version.Value, _version.HasValue },
                { "weights", () => _weights, _weights != null }
            };

            if (_additionalOptions != null)
            {
                document.Merge(_additionalOptions, overwriteExistingElements: false);
            }
            return document;
        }
    }
}
