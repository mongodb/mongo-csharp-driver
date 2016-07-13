/* Copyright 2010-2016 MongoDB Inc.
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
using System.Text;
using MongoDB.Bson;

namespace MongoDB.Driver.GridFS
{
    /// <summary>
    /// Represents setting for GridFS.
    /// </summary>
#if NET45
    [Serializable]
#endif
    public class MongoGridFSSettings : IEquatable<MongoGridFSSettings>
    {
        // private static fields
        private static MongoGridFSSettings __defaults = new MongoGridFSSettings();

        // private fields
        private Setting<int> _chunkSize;
        private Setting<GuidRepresentation> _guidRepresentation;
        private Setting<UTF8Encoding> _readEncoding;
        private Setting<ReadPreference> _readPreference;
        private Setting<string> _root;
        private Setting<bool> _updateMD5;
        private Setting<bool> _verifyMD5;
        private Setting<WriteConcern> _writeConcern;
        private Setting<UTF8Encoding> _writeEncoding;

        private bool _isFrozen;
        private int _frozenHashCode;

        // static constructor
        static MongoGridFSSettings()
        {
            __defaults = new MongoGridFSSettings
            {
                ChunkSize = 255 * 1024, // 255KiB
                Root = "fs",
                UpdateMD5 = true,
                VerifyMD5 = true
            };
            __defaults.Freeze();
        }

        // constructors
        /// <summary>
        /// Initializes a new instance of the MongoGridFSSettings class.
        /// </summary>
        public MongoGridFSSettings()
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoGridFSSettings class.
        /// </summary>
        /// <param name="database">The database from which to inherit some of the settings.</param>
        [Obsolete("Use new MongoGridFSSettings() instead.")]
        public MongoGridFSSettings(MongoDatabase database)
        {
            if (database == null)
            {
                throw new ArgumentNullException("database");
            }

            _chunkSize.Value = __defaults.ChunkSize;
            _root.Value = __defaults.Root;
            _updateMD5.Value = __defaults.UpdateMD5;
            _verifyMD5.Value = __defaults.VerifyMD5;
            _writeConcern.Value = database.Settings.WriteConcern;
        }

        /// <summary>
        /// Initializes a new instance of the MongoGridFSSettings class.
        /// </summary>
        /// <param name="chunkSize">The chunk size.</param>
        /// <param name="root">The root collection name.</param>
        /// <param name="writeConcern">The write concern.</param>
        [Obsolete("Use new MongoGridFSSettings() instead.")]
        public MongoGridFSSettings(int chunkSize, string root, WriteConcern writeConcern)
        {
            if (root == null)
            {
                throw new ArgumentNullException("root");
            }
            if (writeConcern == null)
            {
                throw new ArgumentNullException("writeConcern");
            }

            _chunkSize.Value = chunkSize;
            _root.Value = root;
            _updateMD5.Value = __defaults.UpdateMD5;
            _verifyMD5.Value = __defaults.VerifyMD5;
            _writeConcern.Value = writeConcern;
        }

        // public static properties
        /// <summary>
        /// Gets or sets the default GridFS settings.
        /// </summary>
        public static MongoGridFSSettings Defaults
        {
            get { return __defaults; }
            set { __defaults = value; }
        }

        // public properties
        /// <summary>
        /// Gets the chunks collection name.
        /// </summary>
        [Obsolete("Use Root instead.")]
        public string ChunksCollectionName
        {
            get { return (_root.Value == null) ? null : _root.Value + ".chunks"; }
        }

        /// <summary>
        /// Gets or sets the chunk size.
        /// </summary>
        public int ChunkSize
        {
            get { return _chunkSize.Value; }
            set
            {
                if (_isFrozen) { ThrowFrozen(); }
                _chunkSize.Value = value;
            }
        }

        /// <summary>
        /// Gets the files collection name.
        /// </summary>
        [Obsolete("Use Root instead.")]
        public string FilesCollectionName
        {
            get { return (_root.Value == null) ? null : _root.Value + ".files"; }
        }

        /// <summary>
        /// Gets or sets the GuidRepresentation.
        /// </summary>
        public GuidRepresentation GuidRepresentation
        {
            get { return _guidRepresentation.Value; }
            set
            {
                if (_isFrozen) { ThrowFrozen(); }
                _guidRepresentation.Value = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the settings are frozen.
        /// </summary>
        public bool IsFrozen
        {
            get { return _isFrozen; }
        }

        /// <summary>
        /// Gets or sets the read encoding.
        /// </summary>
        public UTF8Encoding ReadEncoding
        {
            get { return _readEncoding.Value; }
            set
            {
                if (_isFrozen) { ThrowFrozen(); }
                _readEncoding.Value = value;
            }
        }

        /// <summary>
        /// Gets or sets the ReadPreference.
        /// </summary>
        public ReadPreference ReadPreference
        {
            get { return _readPreference.Value; }
            set
            {
                if (_isFrozen) { ThrowFrozen(); }
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _readPreference.Value = value;
            }
        }

        /// <summary>
        /// Gets or sets the root collection name (the files and chunks collection names are derived from the root).
        /// </summary>
        public string Root
        {
            get { return _root.Value; }
            set
            {
                if (_isFrozen) { ThrowFrozen(); }
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _root.Value = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to udpate the MD5 hash on the server when a file is uploaded or modified.
        /// </summary>
        public bool UpdateMD5
        {
            get { return _updateMD5.Value; }
            set {
                if (_isFrozen) { ThrowFrozen(); }
                _updateMD5.Value = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to verify the MD5 hash when a file is uploaded or downloaded.
        /// </summary>
        public bool VerifyMD5
        {
            get { return _verifyMD5.Value; }
            set {
                if (_isFrozen) { ThrowFrozen(); }
                _verifyMD5.Value = value;
            }
        }

        /// <summary>
        /// Gets or sets the WriteConcern.
        /// </summary>
        public WriteConcern WriteConcern
        {
            get { return _writeConcern.Value; }
            set
            {
                if (_isFrozen) { ThrowFrozen(); }
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _writeConcern.Value = value;
            }
        }

        /// <summary>
        /// Gets or sets the write encoding.
        /// </summary>
        public UTF8Encoding WriteEncoding
        {
            get { return _writeEncoding.Value; }
            set
            {
                if (_isFrozen) { ThrowFrozen(); }
                _writeEncoding.Value = value;
            }
        }

        // public operators
        /// <summary>
        /// Compares two MongoGridFSSettings.
        /// </summary>
        /// <param name="lhs">The first MongoGridFSSettings.</param>
        /// <param name="rhs">The other MongoGridFSSettings.</param>
        /// <returns>True if the two MongoGridFSSettings are not equal (or one is null and the other is not).</returns>
        public static bool operator !=(MongoGridFSSettings lhs, MongoGridFSSettings rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Compares two MongoGridFSSettings.
        /// </summary>
        /// <param name="lhs">The first MongoGridFSSettings.</param>
        /// <param name="rhs">The other MongoGridFSSettings.</param>
        /// <returns>True if the two MongoGridFSSettings are equal (or both null).</returns>
        public static bool operator ==(MongoGridFSSettings lhs, MongoGridFSSettings rhs)
        {
            return object.Equals(lhs, rhs);
        }

        // public methods
        /// <summary>
        /// Creates a clone of the settings.
        /// </summary>
        /// <returns>A clone of the settings.</returns>
        public MongoGridFSSettings Clone()
        {
            var clone = new MongoGridFSSettings();
            clone._chunkSize = _chunkSize.Clone();
            clone._guidRepresentation = _guidRepresentation.Clone();
            clone._readEncoding = _readEncoding.Clone();
            clone._readPreference = _readPreference.Clone();
            clone._root = _root.Clone();
            clone._updateMD5 = _updateMD5.Clone();
            clone._verifyMD5 = _verifyMD5.Clone();
            clone._writeConcern = _writeConcern.Clone();
            clone._writeEncoding = _writeEncoding.Clone();
            return clone;
        }

        /// <summary>
        /// Compares this MongoGridFSSettings to another one.
        /// </summary>
        /// <param name="rhs">The other MongoGridFSSettings.</param>
        /// <returns>True if the two settings are equal.</returns>
        public bool Equals(MongoGridFSSettings rhs)
        {
            if (object.ReferenceEquals(rhs, null) || GetType() != rhs.GetType()) { return false; }
            return
                _chunkSize.Value == rhs._chunkSize.Value &&
                _guidRepresentation.Value == rhs._guidRepresentation.Value &&
                object.Equals(_readEncoding.Value, rhs._readEncoding.Value) &&
                _readPreference.Value == rhs._readPreference.Value &&
                _root.Value == rhs._root.Value &&
                _updateMD5.Value == rhs._updateMD5.Value &&
                _verifyMD5.Value == rhs._verifyMD5.Value &&
                _writeConcern.Value == rhs._writeConcern.Value &&
                object.Equals(_writeEncoding.Value, rhs._writeEncoding.Value);
        }

        /// <summary>
        /// Compares this MongoGridFSSettings to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the other objects is a MongoGridFSSettings and is equal to this one.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as MongoGridFSSettings); // works even if obj is null or of a different type
        }

        /// <summary>
        /// Freezes the settings.
        /// </summary>
        /// <returns>The frozen settings.</returns>
        public MongoGridFSSettings Freeze()
        {
            if (!_isFrozen)
            {
                _frozenHashCode = GetHashCode();
                _isFrozen = true;
            }
            return this;
        }

        /// <summary>
        /// Returns a frozen copy of the settings.
        /// </summary>
        /// <returns>A frozen copy of the settings.</returns>
        public MongoGridFSSettings FrozenCopy()
        {
            if (_isFrozen)
            {
                return this;
            }
            else
            {
                return Clone().Freeze();
            }
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            if (_isFrozen)
            {
                return _frozenHashCode;
            }

            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + _chunkSize.Value.GetHashCode();
            hash = 37 * hash + _guidRepresentation.Value.GetHashCode();
            hash = 37 * hash + ((_readEncoding.Value == null) ? 0 : _readEncoding.Value.GetHashCode());
            hash = 37 * hash + ((_readPreference.Value == null) ? 0 : _readPreference.Value.GetHashCode());
            hash = 37 * hash + ((_root.Value == null) ? 0 : _root.Value.GetHashCode());
            hash = 37 * hash + _updateMD5.Value.GetHashCode();
            hash = 37 * hash + _verifyMD5.Value.GetHashCode();
            hash = 37 * hash + ((_writeConcern.Value == null) ? 0 : _writeConcern.Value.GetHashCode());
            hash = 37 * hash + ((_writeEncoding.Value == null) ? 0 : _writeEncoding.Value.GetHashCode());
            return hash;
        }

        // internal methods
        internal void ApplyDefaultValues(MongoDatabaseSettings databaseSettings)
        {
            if (!_chunkSize.HasBeenSet)
            {
                ChunkSize = __defaults.ChunkSize;
            }
            if (!_guidRepresentation.HasBeenSet)
            {
                GuidRepresentation = databaseSettings.GuidRepresentation;
            }
            if (!_readEncoding.HasBeenSet)
            {
                ReadEncoding = databaseSettings.ReadEncoding;
            }
            if (!_readPreference.HasBeenSet)
            {
                ReadPreference = databaseSettings.ReadPreference;
            }
            if (!_root.HasBeenSet)
            {
                Root = __defaults.Root;
            }
            if (!_updateMD5.HasBeenSet)
            {
                UpdateMD5 = __defaults.UpdateMD5;
            }
            if (!_verifyMD5.HasBeenSet)
            {
                VerifyMD5 = __defaults.VerifyMD5;
            }
            if (!_writeConcern.HasBeenSet)
            {
                WriteConcern = databaseSettings.WriteConcern;
            }
            if (!_writeEncoding.HasBeenSet)
            {
                WriteEncoding = databaseSettings.ReadEncoding;
            }
        }

        internal void ApplyDefaultValues(MongoServerSettings serverSettings)
        {
            if (!_chunkSize.HasBeenSet)
            {
                ChunkSize = __defaults.ChunkSize;
            }
            if (!_guidRepresentation.HasBeenSet)
            {
                GuidRepresentation = serverSettings.GuidRepresentation;
            }
            if (!_readEncoding.HasBeenSet)
            {
                ReadEncoding = serverSettings.ReadEncoding;
            }
            if (!_readPreference.HasBeenSet)
            {
                ReadPreference = serverSettings.ReadPreference;
            }
            if (!_root.HasBeenSet)
            {
                Root = __defaults.Root;
            }
            if (!_updateMD5.HasBeenSet)
            {
                UpdateMD5 = __defaults.UpdateMD5;
            }
            if (!_verifyMD5.HasBeenSet)
            {
                VerifyMD5 = __defaults.VerifyMD5;
            }
            if (!_writeConcern.HasBeenSet)
            {
                WriteConcern = serverSettings.WriteConcern;
            }
            if (!_writeEncoding.HasBeenSet)
            {
                WriteEncoding = serverSettings.ReadEncoding;
            }
        }

        internal MongoDatabaseSettings GetDatabaseSettings()
        {
            return new MongoDatabaseSettings
            {
                GuidRepresentation = _guidRepresentation.Value,
                ReadEncoding = _readEncoding.Value,
                ReadPreference = _readPreference.Value,
                WriteConcern = _writeConcern.Value,
                WriteEncoding = _writeEncoding.Value
            };
        }

        // private methods
        private void ThrowFrozen()
        {
            throw new InvalidOperationException("A MongoGridFSSettings object cannot be modified once it has been frozen.");
        }
    }
}
