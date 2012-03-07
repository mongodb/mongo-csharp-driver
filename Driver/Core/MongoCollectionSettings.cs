/* Copyright 2010-2012 10gen Inc.
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

using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// The settings used to access a collection (an abstract class, see MongoCollectionSettings{TDefaultDocument}).
    /// </summary>
    public abstract class MongoCollectionSettings
    {
        // private fields
        private string _collectionName;
        private bool _assignIdOnInsert;
        private Type _defaultDocumentType;
        private GuidRepresentation _guidRepresentation;
        private SafeMode _safeMode;
        private bool _slaveOk;

        // private fields
        // the following fields are set when Freeze is called
        private bool _isFrozen;
        private int _frozenHashCode;
        private string _frozenStringRepresentation;

        // constructors
        /// <summary>
        /// Initializes a new instance of the MongoCollectionSettings class.
        /// </summary>
        /// <param name="database">The database that contains the collection (some collection settings will be inherited from the database settings).</param>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="defaultDocumentType">The default document type for the collection.</param>
        protected MongoCollectionSettings(MongoDatabase database, string collectionName, Type defaultDocumentType)
        {
            var databaseSettings = database.Settings;
            _collectionName = collectionName;
            _assignIdOnInsert = MongoDefaults.AssignIdOnInsert;
            _defaultDocumentType = defaultDocumentType;
            _guidRepresentation = databaseSettings.GuidRepresentation;
            _safeMode = databaseSettings.SafeMode;
            _slaveOk = databaseSettings.SlaveOk;
        }

        /// <summary>
        /// Initializes a new instance of the MongoCollectionSettings class.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="assignIdOnInsert">Whether to automatically assign a value to an empty document Id on insert.</param>
        /// <param name="defaultDocumentType">The default document type for the collection.</param>
        /// <param name="guidRepresentation">The GUID representation to use with this collection.</param>
        /// <param name="safeMode">The SafeMode to use with this collection.</param>
        /// <param name="slaveOk">Whether to route reads to secondaries for this collection.</param>
        protected MongoCollectionSettings(
            string collectionName,
            bool assignIdOnInsert,
            Type defaultDocumentType,
            GuidRepresentation guidRepresentation,
            SafeMode safeMode,
            bool slaveOk)
        {
            _collectionName = collectionName;
            _assignIdOnInsert = assignIdOnInsert;
            _defaultDocumentType = defaultDocumentType;
            _guidRepresentation = guidRepresentation;
            _safeMode = safeMode;
            _slaveOk = slaveOk;
        }

        // public properties
        /// <summary>
        /// Gets or sets whether the driver should assign Id values when missing.
        /// </summary>
        public bool AssignIdOnInsert
        {
            get { return _assignIdOnInsert; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoCollectionSettings is frozen."); }
                _assignIdOnInsert = value;
            }
        }

        /// <summary>
        /// Gets the name of the collection.
        /// </summary>
        public string CollectionName
        {
            get { return _collectionName; }
        }

        /// <summary>
        /// Gets the default document type of the collection.
        /// </summary>
        public Type DefaultDocumentType
        {
            get { return _defaultDocumentType; }
        }

        /// <summary>
        /// Gets or sets the representation used for Guids.
        /// </summary>
        public GuidRepresentation GuidRepresentation
        {
            get { return _guidRepresentation; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoCollectionSettings is frozen."); }
                _guidRepresentation = value;
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
        /// Gets or sets the SafeMode to use.
        /// </summary>
        public SafeMode SafeMode
        {
            get { return _safeMode; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoCollectionSettings is frozen."); }
                _safeMode = value;
            }
        }

        /// <summary>
        /// Gets or sets whether queries should be sent to secondary servers.
        /// </summary>
        public bool SlaveOk
        {
            get { return _slaveOk; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoCollectionSettings is frozen."); }
                _slaveOk = value;
            }
        }

        // public methods
        /// <summary>
        /// Creates a clone of the settings.
        /// </summary>
        /// <returns>A clone of the settings.</returns>
        public abstract MongoCollectionSettings Clone();

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
                        _collectionName == rhs._collectionName &&
                        _assignIdOnInsert == rhs._assignIdOnInsert &&
                        _defaultDocumentType == rhs._defaultDocumentType &&
                        _guidRepresentation == rhs._guidRepresentation &&
                        _safeMode == rhs._safeMode &&
                        _slaveOk == rhs._slaveOk;
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
                _safeMode = _safeMode.FrozenCopy();
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
            hash = 37 * hash + ((_collectionName == null) ? 0 : _collectionName.GetHashCode());
            hash = 37 * hash + _assignIdOnInsert.GetHashCode();
            hash = 37 * hash + ((_defaultDocumentType == null) ? 0 : _defaultDocumentType.GetHashCode());
            hash = 37 * hash + _guidRepresentation.GetHashCode();
            hash = 37 * hash + ((_safeMode == null) ? 0 : _safeMode.GetHashCode());
            hash = 37 * hash + _slaveOk.GetHashCode();
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

            return string.Format(
                "CollectionName={0};AssignIdOnInsert={1};DefaultDocumentType={2};GuidRepresentation={3};SafeMode={4};SlaveOk={5}",
                _collectionName, _assignIdOnInsert, _defaultDocumentType, _guidRepresentation, _safeMode, _slaveOk);
        }
    }

    /// <summary>
    /// Settings used to access a collection.
    /// </summary>
    /// <typeparam name="TDefaultDocument">The default document type of the collection.</typeparam>
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
        /// <param name="safeMode">The safe mode to use.</param>
        /// <param name="slaveOk">Whether queries should be sent to secondary servers.</param>
        private MongoCollectionSettings(
            string collectionName,
            bool assignIdOnInsert,
            GuidRepresentation guidRepresentation,
            SafeMode safeMode,
            bool slaveOk)
            : base(collectionName, assignIdOnInsert, typeof(TDefaultDocument), guidRepresentation, safeMode, slaveOk)
        {
        }

        // public methods
        /// <summary>
        /// Creates a clone of the settings.
        /// </summary>
        /// <returns>A clone of the settings.</returns>
        public override MongoCollectionSettings Clone()
        {
            return new MongoCollectionSettings<TDefaultDocument>(CollectionName, AssignIdOnInsert, GuidRepresentation, SafeMode, SlaveOk);
        }
    }
}
