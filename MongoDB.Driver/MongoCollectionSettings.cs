/* Copyright 2010-2013 10gen Inc.
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

namespace MongoDB.Driver
{
    /// <summary>
    /// The settings used to access a collection.
    /// </summary>
    public class MongoCollectionSettings
    {
        // private fields
        private Setting<string> _collectionName;
        private Setting<bool> _assignIdOnInsert;
        private Setting<Type> _defaultDocumentType;
        private Setting<GuidRepresentation> _guidRepresentation;
        private Setting<UTF8Encoding> _readEncoding;
        private Setting<ReadPreference> _readPreference;
        private Setting<WriteConcern> _writeConcern;
        private Setting<UTF8Encoding> _writeEncoding;

        // the following fields are set when Freeze is called
        private bool _isFrozen;
        private int _frozenHashCode;
        private string _frozenStringRepresentation;

        // constructors
        /// <summary>
        /// Initializes a new instance of the MongoCollectionSettings class.
        /// </summary>
        public MongoCollectionSettings()
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoCollectionSettings class.
        /// </summary>
        /// <param name="database">The database that contains the collection (some collection settings will be inherited from the database settings).</param>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="defaultDocumentType">The default document type for the collection.</param>
        [Obsolete("Use MongoCollectionSettings() instead.")]
        protected MongoCollectionSettings(MongoDatabase database, string collectionName, Type defaultDocumentType)
        {
            if (database == null)
            {
                throw new ArgumentNullException("database");
            }
            if (collectionName == null)
            {
                throw new ArgumentNullException("collectionName");
            }
            if (defaultDocumentType == null)
            {
                throw new ArgumentNullException("defaultDocumentType");
            }

            var databaseSettings = database.Settings;
            _collectionName.Value = collectionName;
            _assignIdOnInsert.Value = MongoDefaults.AssignIdOnInsert;
            _defaultDocumentType.Value = defaultDocumentType;
            _guidRepresentation.Value = databaseSettings.GuidRepresentation;
            _readEncoding.Value = databaseSettings.ReadEncoding;
            _readPreference.Value = databaseSettings.ReadPreference;
            _writeConcern.Value = databaseSettings.WriteConcern;
            _writeEncoding.Value = databaseSettings.WriteEncoding;
        }

        /// <summary>
        /// Initializes a new instance of the MongoCollectionSettings class.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="assignIdOnInsert">Whether to automatically assign a value to an empty document Id on insert.</param>
        /// <param name="defaultDocumentType">The default document type for the collection.</param>
        /// <param name="guidRepresentation">The GUID representation to use with this collection.</param>
        /// <param name="readPreference">The read preference.</param>
        /// <param name="writeConcern">The WriteConcern to use with this collection.</param>
        [Obsolete("Only used by the deprecated MongoCollectionSettings<TDefaultDocument> subclass.")]
        protected MongoCollectionSettings(
            string collectionName,
            bool assignIdOnInsert,
            Type defaultDocumentType,
            GuidRepresentation guidRepresentation,
            ReadPreference readPreference,
            WriteConcern writeConcern)
        {
            if (collectionName == null)
            {
                throw new ArgumentNullException("collectionName");
            }
            if (defaultDocumentType == null)
            {
                throw new ArgumentNullException("defaultDocumentType");
            }
            if (readPreference == null)
            {
                throw new ArgumentNullException("readPreference");
            }
            if (writeConcern == null)
            {
                throw new ArgumentNullException("writeConcern");
            }

            _collectionName.Value = collectionName;
            _assignIdOnInsert.Value = assignIdOnInsert;
            _defaultDocumentType.Value = defaultDocumentType;
            _guidRepresentation.Value = guidRepresentation;
            _readPreference.Value = readPreference;
            _writeConcern.Value = writeConcern;
        }

        // public properties
        /// <summary>
        /// Gets or sets whether the driver should assign Id values when missing.
        /// </summary>
        public bool AssignIdOnInsert
        {
            get { return _assignIdOnInsert.Value; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoCollectionSettings is frozen."); }
                _assignIdOnInsert.Value = value;
            }
        }

        /// <summary>
        /// Gets the name of the collection.
        /// </summary>
        [Obsolete("Provide the collection name on the call to GetCollection instead.")]
        public string CollectionName
        {
            get { return _collectionName.Value; }
        }

        /// <summary>
        /// Gets the default document type of the collection.
        /// </summary>
        [Obsolete("Provide the default document type on the call to GetCollection instead.")]
        public Type DefaultDocumentType
        {
            get { return _defaultDocumentType.Value; }
        }

        /// <summary>
        /// Gets or sets the representation used for Guids.
        /// </summary>
        public GuidRepresentation GuidRepresentation
        {
            get { return _guidRepresentation.Value; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoCollectionSettings is frozen."); }
                _guidRepresentation.Value = value;
            }
        }

        /// <summary>
        /// Gets whether the settings have been frozen to prevent further changes.
        /// </summary>
        public bool IsFrozen
        {
            get { return _isFrozen; }
        }

        /// <summary>
        /// Gets or sets the Read Encoding.
        /// </summary>
        public UTF8Encoding ReadEncoding
        {
            get { return _readEncoding.Value; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoCollectionSettings is frozen."); }
                _readEncoding.Value = value;
            }
        }

        /// <summary>
        /// Gets or sets the read preference to use.
        /// </summary>
        public ReadPreference ReadPreference
        {
            get { return _readPreference.Value; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoCollectionSettings is frozen."); }
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _readPreference.Value = value;
            }
        }

        /// <summary>
        /// Gets or sets the SafeMode to use.
        /// </summary>
        [Obsolete("Use WriteConcern instead.")]
        public SafeMode SafeMode
        {
            get { return (_writeConcern.Value == null) ? null : new SafeMode(_writeConcern.Value); }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoCollectionSettings is frozen."); }
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _writeConcern.Value = value.WriteConcern;
            }
        }

        /// <summary>
        /// Gets or sets whether queries should be sent to secondary servers.
        /// </summary>
        [Obsolete("Use ReadPreference instead.")]
        public bool SlaveOk
        {
            get
            {
                return (_readPreference.Value != null) ? _readPreference.Value.ToSlaveOk() : false;
            }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoCollectionSettings is frozen."); }
                _readPreference.Value = ReadPreference.FromSlaveOk(value);
            }
        }

        /// <summary>
        /// Gets or sets the WriteConcern to use.
        /// </summary>
        public WriteConcern WriteConcern
        {
            get { return _writeConcern.Value; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoCollectionSettings is frozen."); }
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _writeConcern.Value = value;
            }
        }

        /// <summary>
        /// Gets or sets the Write Encoding.
        /// </summary>
        public UTF8Encoding WriteEncoding
        {
            get { return _writeEncoding.Value; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoCollectionSettings is frozen."); }
                _writeEncoding.Value = value;
            }
        }

        // public methods
        /// <summary>
        /// Creates a clone of the settings.
        /// </summary>
        /// <returns>A clone of the settings.</returns>
        public virtual MongoCollectionSettings Clone()
        {
            var clone = new MongoCollectionSettings();
            clone._collectionName = _collectionName.Clone();
            clone._assignIdOnInsert = _assignIdOnInsert.Clone();
            clone._defaultDocumentType = _defaultDocumentType.Clone();
            clone._guidRepresentation = _guidRepresentation.Clone();
            clone._readEncoding = _readEncoding.Clone();
            clone._readPreference = _readPreference.Clone();
            clone._writeConcern = _writeConcern.Clone();
            clone._writeEncoding = _writeEncoding.Clone();
            return clone;
        }

        /// <summary>
        /// Compares two MongoCollectionSettings instances.
        /// </summary>
        /// <param name="obj">The other instance.</param>
        /// <returns>True if the two instances are equal.</returns>
        public override bool Equals(object obj)
        {
            var rhs = obj as MongoCollectionSettings;
            if (rhs == null)
            {
                return false;
            }
            else
            {
                if (_isFrozen && rhs._isFrozen)
                {
                    return _frozenStringRepresentation == rhs._frozenStringRepresentation;
                }
                else
                {
                    return
                        _collectionName.Value == rhs._collectionName.Value &&
                        _assignIdOnInsert.Value == rhs._assignIdOnInsert.Value &&
                        _defaultDocumentType.Value == rhs._defaultDocumentType.Value &&
                        _guidRepresentation.Value == rhs._guidRepresentation.Value &&
                        object.Equals(_readEncoding, rhs._readEncoding) &&
                        _readPreference.Value == rhs._readPreference.Value &&
                        _writeConcern.Value == rhs._writeConcern.Value &&
                        object.Equals(_writeEncoding, rhs._writeEncoding);
                }
            }
        }

        /// <summary>
        /// Freezes the settings.
        /// </summary>
        /// <returns>The frozen settings.</returns>
        public MongoCollectionSettings Freeze()
        {
            if (!_isFrozen)
            {
                if (_readPreference.Value != null) { _readPreference.Value = _readPreference.Value.FrozenCopy(); }
                if (_writeConcern.Value != null) { _writeConcern.Value = _writeConcern.Value.FrozenCopy(); }
                _frozenHashCode = GetHashCode();
                _frozenStringRepresentation = ToString();
                _isFrozen = true;
            }
            return this;
        }

        /// <summary>
        /// Returns a frozen copy of the settings.
        /// </summary>
        /// <returns>A frozen copy of the settings.</returns>
        public MongoCollectionSettings FrozenCopy()
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
            hash = 37 * hash + ((_collectionName.Value == null) ? 0 : _collectionName.Value.GetHashCode());
            hash = 37 * hash + _assignIdOnInsert.Value.GetHashCode();
            hash = 37 * hash + ((_defaultDocumentType.Value == null) ? 0 : _defaultDocumentType.Value.GetHashCode());
            hash = 37 * hash + _guidRepresentation.Value.GetHashCode();
            hash = 37 * hash + ((_readEncoding.Value == null) ? 0 : _readEncoding.Value.GetHashCode());
            hash = 37 * hash + ((_readPreference.Value == null) ? 0 : _readPreference.Value.GetHashCode());
            hash = 37 * hash + ((_writeConcern.Value == null) ? 0 :_writeConcern.Value.GetHashCode());
            hash = 37 * hash + ((_writeEncoding.Value == null) ? 0 : _writeEncoding.Value.GetHashCode());
            return hash;
        }

        /// <summary>
        /// Returns a string representation of the settings.
        /// </summary>
        /// <returns>A string representation of the settings.</returns>
        public override string ToString()
        {
            if (_isFrozen)
            {
                return _frozenStringRepresentation;
            }

            var parts = new List<string>();
            if (_collectionName.HasBeenSet)
            {
                parts.Add(string.Format("CollectionName={0}", _collectionName.Value));
            }
            if (_defaultDocumentType.HasBeenSet)
            {
                parts.Add(string.Format("DefaultDocumentType={0}", _defaultDocumentType.Value));
            }
            parts.Add(string.Format("AssignIdOnInsert={0}", _assignIdOnInsert));
            parts.Add(string.Format("GuidRepresentation={0}", _guidRepresentation));
            if (_readEncoding.HasBeenSet)
            {
                parts.Add(string.Format("ReadEncoding={0}", (_readEncoding.Value == null) ? "null" : "UTF8Encoding"));
            }
            parts.Add(string.Format("ReadPreference={0}", _readPreference));
            parts.Add(string.Format("WriteConcern={0}", _writeConcern));
            if (_writeEncoding.HasBeenSet)
            {
                parts.Add(string.Format("WriteEncoding={0}", (_writeEncoding.Value == null) ? "null" : "UTF8Encoding"));
            }
            return string.Join(";", parts.ToArray());
        }

        // internal methods
        internal void ApplyDefaultValues(MongoDatabaseSettings databaseSettings)
        {
            if (!_assignIdOnInsert.HasBeenSet)
            {
                AssignIdOnInsert = MongoDefaults.AssignIdOnInsert;
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
            if (!_writeConcern.HasBeenSet)
            {
                WriteConcern = databaseSettings.WriteConcern;
            }
            if (!_writeEncoding.HasBeenSet)
            {
                WriteEncoding = databaseSettings.WriteEncoding;
            }
        }
    }

    /// <summary>
    /// Settings used to access a collection (this class is obsolete, use the non-generic MongoCollectionSettings class instead).
    /// </summary>
    /// <typeparam name="TDefaultDocument">The default document type of the collection.</typeparam>
    [Obsolete("Use the non-generic MongoCollectionSettings class instead and provide the default document type on the call to GetCollection.")]
    public class MongoCollectionSettings<TDefaultDocument> : MongoCollectionSettings
    {
        // constructors
        /// <summary>
        /// Creates a new instance of MongoCollectionSettings.
        /// </summary>
        /// <param name="database">The database to inherit settings from.</param>
        /// <param name="collectionName">The name of the collection.</param>
        public MongoCollectionSettings(MongoDatabase database, string collectionName)
            : base(database, collectionName, typeof(TDefaultDocument))
        {
        }

        /// <summary>
        /// Creates a new instance of MongoCollectionSettings.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="assignIdOnInsert">Whether the driver should assign the Id values if necessary.</param>
        /// <param name="guidRepresentation">The representation for Guids.</param>
        /// <param name="readPreference">The read preference.</param>
        /// <param name="writeConcern">The write concern to use.</param>
        private MongoCollectionSettings(
            string collectionName,
            bool assignIdOnInsert,
            GuidRepresentation guidRepresentation,
            ReadPreference readPreference,
            WriteConcern writeConcern)
            : base(collectionName, assignIdOnInsert, typeof(TDefaultDocument), guidRepresentation, readPreference, writeConcern)
        {
        }

        // public methods
        /// <summary>
        /// Creates a clone of the settings.
        /// </summary>
        /// <returns>A clone of the settings.</returns>
        public override MongoCollectionSettings Clone()
        {
            return new MongoCollectionSettings<TDefaultDocument>(CollectionName, AssignIdOnInsert, GuidRepresentation, ReadPreference, WriteConcern);
        }
    }
}
