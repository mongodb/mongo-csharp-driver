﻿/* Copyright 2015-2016 MongoDB Inc.
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

using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.GridFS
{
    /// <summary>
    /// Represents mutable options for a GridFS instance.
    /// </summary>
    public class GridFSBucketOptions
    {
        // fields
        private string _bucketName;
        private int _chunkSizeBytes;
        private ReadConcern _readConcern;
        private ReadPreference _readPreference;
        private WriteConcern _writeConcern;
        private bool _suppressEnsureIndexes;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GridFSBucketOptions"/> class.
        /// </summary>
        public GridFSBucketOptions()
            : this(ImmutableGridFSBucketOptions.Defaults)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridFSBucketOptions"/> class.
        /// </summary>
        /// <param name="other">The other <see cref="GridFSBucketOptions"/> from which to copy the values.</param>
        public GridFSBucketOptions(GridFSBucketOptions other)
        {
            Ensure.IsNotNull(other, nameof(other));
            _bucketName = other.BucketName;
            _chunkSizeBytes = other.ChunkSizeBytes;
            _readConcern = other.ReadConcern;
            _readPreference = other.ReadPreference;
            _writeConcern = other.WriteConcern;
            _suppressEnsureIndexes = other.SuppressEnsureIndexes;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridFSBucketOptions"/> class.
        /// </summary>
        /// <param name="other">The other <see cref="ImmutableGridFSBucketOptions"/> from which to copy the values.</param>
        public GridFSBucketOptions(ImmutableGridFSBucketOptions other)
        {
            Ensure.IsNotNull(other, nameof(other));
            _bucketName = other.BucketName;
            _chunkSizeBytes = other.ChunkSizeBytes;
            _readConcern = other.ReadConcern;
            _readPreference = other.ReadPreference;
            _writeConcern = other.WriteConcern;
            _suppressEnsureIndexes = other.SuppressEnsureIndexes;
        }

        // properties
        /// <summary>
        /// Gets or sets the bucket name.
        /// </summary>
        /// <value>
        /// The bucket name.
        /// </value>
        public string BucketName
        {
            get { return _bucketName; }
            set
            {
                Ensure.IsNotNullOrEmpty(value, nameof(value));
                _bucketName = value;
            }
        }

        /// <summary>
        /// Gets or sets the chunk size in bytes.
        /// </summary>
        /// <value>
        /// The chunk size in bytes.
        /// </value>
        public int ChunkSizeBytes
        {
            get { return _chunkSizeBytes; }
            set
            {
                Ensure.IsGreaterThanZero(value, nameof(value));
                _chunkSizeBytes = value;
            }
        }

        /// <summary>
        /// Gets or sets the read concern.
        /// </summary>
        /// <value>
        /// The read concern.
        /// </value>
        public ReadConcern ReadConcern
        {
            get { return _readConcern; }
            set { _readConcern = value; }
        }

        /// <summary>
        /// Gets or sets the read preference.
        /// </summary>
        /// <value>
        /// The read preference.
        /// </value>
        public ReadPreference ReadPreference
        {
            get { return _readPreference; }
            set { _readPreference = value; }
        }

        /// <summary>
        /// Gets or sets the write concern.
        /// </summary>
        /// <value>
        /// The write concern.
        /// </value>
        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = value; }
        }

        /// <summary>
        /// Gets or sets the suppress ensure indexes setting
        /// </summary>
        /// <value>
        /// The suppress ensure indexes setting
        /// </value>
        public bool SuppressEnsureIndexes
        {
            get { return _suppressEnsureIndexes; }
            set { _suppressEnsureIndexes = value; }
        }
    }

    /// <summary>
    /// Represents immutable options for a GridFS instance.
    /// </summary>
    public class ImmutableGridFSBucketOptions
    {
        #region static
        // static fields
        private static readonly ImmutableGridFSBucketOptions __defaults = new ImmutableGridFSBucketOptions();

        // static properties
        /// <summary>
        /// Gets the default GridFSBucketOptions.
        /// </summary>
        /// <value>
        /// The default GridFSBucketOptions.
        /// </value>
        public static ImmutableGridFSBucketOptions Defaults
        {
            get { return __defaults; }
        }
        #endregion

        // fields
        private readonly string _bucketName;
        private readonly int _chunkSizeBytes;
        private readonly ReadConcern _readConcern;
        private readonly ReadPreference _readPreference;
        private readonly WriteConcern _writeConcern;
        private readonly bool _suppressEnsureIndexes;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableGridFSBucketOptions"/> class.
        /// </summary>
        public ImmutableGridFSBucketOptions()
        {
            _bucketName = "fs";
            _chunkSizeBytes = 255 * 1024;
            _suppressEnsureIndexes = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableGridFSBucketOptions" /> class.
        /// </summary>
        /// <param name="other">The other <see cref="GridFSBucketOptions"/> from which to copy the values.</param>
        public ImmutableGridFSBucketOptions(GridFSBucketOptions other)
        {
            Ensure.IsNotNull(other, nameof(other));
            _bucketName = other.BucketName;
            _chunkSizeBytes = other.ChunkSizeBytes;
            _readConcern = other.ReadConcern;
            _readPreference = other.ReadPreference;
            _writeConcern = other.WriteConcern;
            _suppressEnsureIndexes = other.SuppressEnsureIndexes;
        }

        // properties
        /// <summary>
        /// Gets the bucket name.
        /// </summary>
        /// <value>
        /// The bucket name.
        /// </value>
        public string BucketName
        {
            get { return _bucketName; }
        }

        /// <summary>
        /// Gets the chunk size in bytes.
        /// </summary>
        /// <value>
        /// The chunk size in bytes.
        /// </value>
        public int ChunkSizeBytes
        {
            get { return _chunkSizeBytes; }
        }

        /// <summary>
        /// Gets the read concern.
        /// </summary>
        /// <value>
        /// The read concern.
        /// </value>
        public ReadConcern ReadConcern
        {
            get { return _readConcern; }
        }

        /// <summary>
        /// Gets the read preference.
        /// </summary>
        /// <value>
        /// The read preference.
        /// </value>
        public ReadPreference ReadPreference
        {
            get { return _readPreference; }
        }

        /// <summary>
        /// Gets the serializer registry.
        /// </summary>
        /// <value>
        /// The serializer registry.
        /// </value>
        public IBsonSerializerRegistry SerializerRegistry
        {
            get { return BsonSerializer.SerializerRegistry; }
        }

        /// <summary>
        /// Gets the write concern.
        /// </summary>
        /// <value>
        /// The write concern.
        /// </value>
        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
        }

        /// <summary>
        /// Gets the suppress ensure indexes setting
        /// </summary>
        /// <value>
        /// The suppress ensure indexes setting
        /// </value>
        public bool SuppressEnsureIndexes
        {
            get { return _suppressEnsureIndexes; }
        }
    }
}
