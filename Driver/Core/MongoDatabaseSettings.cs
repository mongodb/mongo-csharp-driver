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

namespace MongoDB.Driver {
    /// <summary>
    /// The settings used to access a database.
    /// </summary>
    public class MongoDatabaseSettings {
        #region private fields
        private string databaseName;
        private MongoCredentials credentials;
        private GuidRepresentation guidRepresentation;
        private SafeMode safeMode;
        private bool slaveOk;
        // the following fields are set when Freeze is called
        private bool isFrozen;
        private int frozenHashCode;
        private string frozenStringRepresentation;
        #endregion

        #region constructors
        /// <summary>
        /// Creates a new instance of MongoDatabaseSettings.
        /// </summary>
        /// <param name="server">The server to inherit settings from.</param>
        /// <param name="databaseName">The name of the database.</param>
        public MongoDatabaseSettings(
            MongoServer server,
            string databaseName
        ) {
            var serverSettings = server.Settings;
            this.databaseName = databaseName;
            this.credentials = serverSettings.DefaultCredentials;
            this.guidRepresentation = serverSettings.GuidRepresentation;
            this.safeMode = serverSettings.SafeMode;
            this.slaveOk = serverSettings.SlaveOk;
        }

        /// <summary>
        /// Creates a new instance of MongoDatabaseSettings.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="credentials">The credentials to access the database.</param>
        /// <param name="guidRepresentation">The representation for Guids.</param>
        /// <param name="safeMode">The safe mode to use.</param>
        /// <param name="slaveOk">Whether queries should be sent to secondary servers.</param>
        public MongoDatabaseSettings(
            string databaseName,
            MongoCredentials credentials,
            GuidRepresentation guidRepresentation,
            SafeMode safeMode,
            bool slaveOk
        ) {
            this.databaseName = databaseName;
            this.credentials = credentials;
            this.guidRepresentation = guidRepresentation;
            this.safeMode = safeMode;
            this.slaveOk = slaveOk;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets or sets the credentials to access the database.
        /// </summary>
        public MongoCredentials Credentials {
            get { return credentials; }
            set {
                if (isFrozen) { throw new InvalidOperationException("MongoDatabaseSettings is frozen."); }
                credentials = value;
            }
        }

        /// <summary>
        /// Gets the name of the database.
        /// </summary>
        public string DatabaseName {
            get { return databaseName; }
        }

        /// <summary>
        /// Gets or sets the representation to use for Guids.
        /// </summary>
        public GuidRepresentation GuidRepresentation {
            get { return guidRepresentation; }
            set {
                if (isFrozen) { throw new InvalidOperationException("MongoDatabaseSettings is frozen."); }
                guidRepresentation = value;
            }
        }

        /// <summary>
        /// Gets whether the settings have been frozen to prevent further changes.
        /// </summary>
        public bool IsFrozen {
            get { return isFrozen; }
        }

        /// <summary>
        /// Gets or sets the SafeMode to use.
        /// </summary>
        public SafeMode SafeMode {
            get { return safeMode; }
            set {
                if (isFrozen) { throw new InvalidOperationException("MongoDatabaseSettings is frozen."); }
                safeMode = value;
            }
        }

        /// <summary>
        /// Gets or sets whether queries should be sent to secondary servers.
        /// </summary>
        public bool SlaveOk {
            get { return slaveOk; }
            set {
                if (isFrozen) { throw new InvalidOperationException("MongoDatabaseSettings is frozen."); }
                slaveOk = value;
            }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Creates a clone of the settings.
        /// </summary>
        /// <returns>A clone of the settings.</returns>
        public MongoDatabaseSettings Clone() {
            return new MongoDatabaseSettings(
                databaseName,
                credentials,
                guidRepresentation,
                safeMode,
                slaveOk
            );
        }

        /// <summary>
        /// Compares two MongoDatabaseSettings instances.
        /// </summary>
        /// <param name="obj">The other instance.</param>
        /// <returns>True if the two instances are equal.</returns>
        public override bool Equals(object obj) {
            var rhs = obj as MongoDatabaseSettings;
            if (rhs == null) {
                return false;
            } else {
                if (this.isFrozen && rhs.isFrozen) {
                    return this.frozenStringRepresentation == rhs.frozenStringRepresentation;
                } else {
                    return
                        this.databaseName == rhs.databaseName &&
                        this.credentials == rhs.credentials &&
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
        public MongoDatabaseSettings Freeze() {
            if (!isFrozen) {
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
        public MongoDatabaseSettings FrozenCopy() {
            if (isFrozen) {
                return this;
            } else {
                return Clone().Freeze();
            }
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode() {
            if (isFrozen) {
                return frozenHashCode;
            } else {
                return GetHashCodeHelper();
            }
        }

        /// <summary>
        /// Returns a string representation of the settings.
        /// </summary>
        /// <returns>A string representation of the settings.</returns>
        public override string ToString() {
            if (isFrozen) {
                return frozenStringRepresentation;
            } else {
                return ToStringHelper();
            }
        }
        #endregion

        #region private methods
        private int GetHashCodeHelper() {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + ((databaseName == null) ? 0 : databaseName.GetHashCode());
            hash = 37 * hash + ((credentials == null) ? 0 : credentials.GetHashCode());
            hash = 37 * hash + guidRepresentation.GetHashCode();
            hash = 37 * hash + ((safeMode == null) ? 0 : safeMode.GetHashCode());
            hash = 37 * hash + slaveOk.GetHashCode();
            return hash;
        }

        private string ToStringHelper() {
            return string.Format(
                "DatabaseName={0};Credentials={1};GuidRepresentation={2};SafeMode={3};SlaveOk={4}",
                databaseName,
                credentials,
                guidRepresentation,
                safeMode,
                slaveOk
            );
        }
        #endregion
    }
}
