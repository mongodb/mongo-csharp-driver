/* Copyright 2010-2011 10gen Inc.
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
        // protected fields
        /// <summary>
        /// The name of the collection.
        /// </summary>
        protected string collectionName;
        /// <summary>
        /// Whether to automatically assign a value to an empty document Id on insert.
        /// </summary>
        protected bool assignIdOnInsert;
        /// <summary>
        /// The default document type of the collection.
        /// </summary>
        protected Type defaultDocumentType;
        /// <summary>
        /// The GUID representation.
        /// </summary>
        protected GuidRepresentation guidRepresentation;
        /// <summary>
        /// The SafeMode.
        /// </summary>
        protected SafeMode safeMode;
        /// <summary>
        /// Whether to route reads to secondaries.
        /// </summary>
        protected bool slaveOk;

        // private fields
        // the following fields are set when Freeze is called
        private bool isFrozen;
        private int frozenHashCode;
        private string frozenStringRepresentation;

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
            this.collectionName = collectionName;
            this.assignIdOnInsert = MongoDefaults.AssignIdOnInsert;
            this.defaultDocumentType = defaultDocumentType;
            this.guidRepresentation = databaseSettings.GuidRepresentation;
            this.safeMode = databaseSettings.SafeMode;
            this.slaveOk = databaseSettings.SlaveOk;
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
        protected MongoCollectionSettings(string collectionName, bool assignIdOnInsert, Type defaultDocumentType, GuidRepresentation guidRepresentation, SafeMode safeMode, bool slaveOk)
        {
            this.collectionName = collectionName;
            this.assignIdOnInsert = assignIdOnInsert;
            this.defaultDocumentType = defaultDocumentType;
            this.guidRepresentation = guidRepresentation;
            this.safeMode = safeMode;
            this.slaveOk = slaveOk;
        }

        // public properties
        /// <summary>
        /// Gets or sets whether the driver should assign Id values when missing.
        /// </summary>
        public bool AssignIdOnInsert
        {
            get { return assignIdOnInsert; }
            set
            {
                if (isFrozen) { throw new InvalidOperationException("MongoCollectionSettings is frozen."); }
                assignIdOnInsert = value;
            }
        }

        /// <summary>
        /// Gets the name of the collection.
        /// </summary>
        public string CollectionName
        {
            get { return collectionName; }
        }

        /// <summary>
        /// Gets the default document type of the collection.
        /// </summary>
        public Type DefaultDocumentType
        {
            get { return defaultDocumentType; }
        }

        /// <summary>
        /// Gets or sets the representation used for Guids.
        /// </summary>
        public GuidRepresentation GuidRepresentation
        {
            get { return guidRepresentation; }
            set
            {
                if (isFrozen) { throw new InvalidOperationException("MongoCollectionSettings is frozen."); }
                guidRepresentation = value;
            }
        }

        /// <summary>
        /// Gets whether the settings have been frozen to prevent further changes.
        /// </summary>
        public bool IsFrozen
        {
            get { return isFrozen; }
        }

        /// <summary>
        /// Gets or sets the SafeMode to use.
        /// </summary>
        public SafeMode SafeMode
        {
            get { return safeMode; }
            set
            {
                if (isFrozen) { throw new InvalidOperationException("MongoCollectionSettings is frozen."); }
                safeMode = value;
            }
        }

        /// <summary>
        /// Gets or sets whether queries should be sent to secondary servers.
        /// </summary>
        public bool SlaveOk
        {
            get { return slaveOk; }
            set
            {
                if (isFrozen) { throw new InvalidOperationException("MongoCollectionSettings is frozen."); }
                slaveOk = value;
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
                if (this.isFrozen && rhs.isFrozen)
                {
                    return this.frozenStringRepresentation == rhs.frozenStringRepresentation;
                }
                else
                {
                    return
                        this.collectionName == rhs.collectionName &&
                        this.assignIdOnInsert == rhs.assignIdOnInsert &&
                        this.defaultDocumentType == rhs.defaultDocumentType &&
                        this.guidRepresentation == rhs.guidRepresentation &&
                        this.safeMode == rhs.safeMode &&
                        this.slaveOk == rhs.slaveOk;
                }
            }
        }

        /// <summary>
        /// Freezes the settings.
        /// </summary>
        /// <returns>The frozen settings.</returns>
        public MongoCollectionSettings Freeze()
        {
            if (!isFrozen)
            {
                safeMode = safeMode.FrozenCopy();
                frozenHashCode = GetHashCodeHelper();
                frozenStringRepresentation = ToStringHelper();
                isFrozen = true;
            }
            return this;
        }

        /// <summary>
        /// Returns a frozen copy of the settings.
        /// </summary>
        /// <returns>A frozen copy of the settings.</returns>
        public MongoCollectionSettings FrozenCopy()
        {
            if (isFrozen)
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
            if (isFrozen)
            {
                return frozenHashCode;
            }
            else
            {
                return GetHashCodeHelper();
            }
        }

        /// <summary>
        /// Returns a string representation of the settings.
        /// </summary>
        /// <returns>A string representation of the settings.</returns>
        public override string ToString()
        {
            if (isFrozen)
            {
                return frozenStringRepresentation;
            }
            else
            {
                return ToStringHelper();
            }
        }

        // private methods
        private int GetHashCodeHelper()
        {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + ((collectionName == null) ? 0 : collectionName.GetHashCode());
            hash = 37 * hash + assignIdOnInsert.GetHashCode();
            hash = 37 * hash + ((defaultDocumentType == null) ? 0 : defaultDocumentType.GetHashCode());
            hash = 37 * hash + guidRepresentation.GetHashCode();
            hash = 37 * hash + ((safeMode == null) ? 0 : safeMode.GetHashCode());
            hash = 37 * hash + slaveOk.GetHashCode();
            return hash;
        }

        private string ToStringHelper()
        {
            return string.Format(
                "CollectionName={0};AssignIdOnInsert={1};DefaultDocumentType={2};GuidRepresentation={3};SafeMode={4};SlaveOk={5}",
                collectionName, assignIdOnInsert, defaultDocumentType, guidRepresentation, safeMode, slaveOk);
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
        private MongoCollectionSettings(string collectionName, bool assignIdOnInsert, GuidRepresentation guidRepresentation, SafeMode safeMode, bool slaveOk)
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
            return new MongoCollectionSettings<TDefaultDocument>(collectionName, assignIdOnInsert, guidRepresentation, safeMode, slaveOk);
        }
    }
}
