/* Copyright 2010-2014 MongoDB Inc.
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

namespace MongoDB.Driver
{
    /// <summary>
    /// Options for creating an index.
    /// </summary>
    public class CreateIndexOptions
    {
        // fields
        private bool? _background;
        private int? _bits;
        private double? _bucketSize;
        private string _defaultLanguage;
        private TimeSpan? _expireAfter;
        private string _languageOverride;
        private double? _max;
        private double? _min;
        private string _name;
        private bool? _sparse;
        private int? _sphereIndexVersion;
        private object _storageOptions;
        private int? _textIndexVersion;
        private bool? _unique;
        private int? _version;
        private object _weights;

        // properties
        /// <summary>
        /// Gets or sets the background.
        /// </summary>
        public bool? Background
        {
            get { return _background; }
            set { _background = value; }
        }

        /// <summary>
        /// Gets or sets the bits.
        /// </summary>
        public int? Bits
        {
            get { return _bits; }
            set { _bits = value; }
        }

        /// <summary>
        /// Gets or sets the size of the bucket.
        /// </summary>
        /// <value>
        /// The size of the bucket.
        /// </value>
        public double? BucketSize
        {
            get { return _bucketSize; }
            set { _bucketSize = value; }
        }

        /// <summary>
        /// Gets or sets the default language.
        /// </summary>
        public string DefaultLanguage
        {
            get { return _defaultLanguage; }
            set { _defaultLanguage = value; }
        }

        /// <summary>
        /// Gets or sets the expire after.
        /// </summary>
        public TimeSpan? ExpireAfter
        {
            get { return _expireAfter; }
            set { _expireAfter = value; }
        }

        /// <summary>
        /// Gets or sets the language override.
        /// </summary>
        public string LanguageOverride
        {
            get { return _languageOverride; }
            set { _languageOverride = value; }
        }

        /// <summary>
        /// Gets or sets the maximum.
        /// </summary>
        public double? Max
        {
            get { return _max; }
            set { _max = value; }
        }

        /// <summary>
        /// Gets or sets the minimum.
        /// </summary>
        public double? Min
        {
            get { return _min; }
            set { _min = value; }
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Gets or sets the sparse.
        /// </summary>
        public bool? Sparse
        {
            get { return _sparse; }
            set { _sparse = value; }
        }

        /// <summary>
        /// Gets or sets the sphere index version.
        /// </summary>
        public int? SphereIndexVersion
        {
            get { return _sphereIndexVersion; }
            set { _sphereIndexVersion = value; }
        }

        /// <summary>
        /// Gets or sets the storage options.
        /// </summary>
        public object StorageOptions
        {
            get { return _storageOptions; }
            set { _storageOptions = value; }
        }

        /// <summary>
        /// Gets or sets the text index version.
        /// </summary>
        public int? TextIndexVersion
        {
            get { return _textIndexVersion; }
            set { _textIndexVersion = value; }
        }

        /// <summary>
        /// Gets or sets the unique.
        /// </summary>
        public bool? Unique
        {
            get { return _unique; }
            set { _unique = value; }
        }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        public int? Version
        {
            get { return _version; }
            set { _version = value; }
        }

        /// <summary>
        /// Gets or sets the weights.
        /// </summary>
        public object Weights
        {
            get { return _weights; }
            set { _weights = value; }
        }
    }
}
