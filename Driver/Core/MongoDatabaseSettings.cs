/* Copyright 2010 10gen Inc.
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
    public class MongoDatabaseSettings {
        #region private fields
        private string databaseName;
        private MongoCredentials credentials;
        private SafeMode safeMode;
        // the following fields are set when Freeze is called
        private bool isFrozen;
        private int frozenHashCode;
        private string frozenStringRepresentation;
        #endregion

        #region constructors
        public MongoDatabaseSettings() {
            this.databaseName = null;
            this.credentials = null;
            this.safeMode = SafeMode.False;
        }

        public MongoDatabaseSettings(
            string databaseName,
            MongoCredentials credentials,
            SafeMode safeMode
        ) {
            this.databaseName = databaseName;
            this.credentials = credentials;
            this.safeMode = safeMode;
        }
        #endregion

        #region public properties
        public MongoCredentials Credentials {
            get { return credentials; }
            set {
                if (isFrozen) { throw new InvalidOperationException("MongoDatabaseSettings is frozen"); }
                credentials = value;
            }
        }

        public string DatabaseName {
            get { return databaseName; }
            set {
                if (isFrozen) { throw new InvalidOperationException("MongoDatabaseSettings is frozen"); }
                databaseName = value;
            }
        }

        public bool IsFrozen {
            get { return isFrozen; }
        }

        public SafeMode SafeMode {
            get { return safeMode; }
            set {
                if (isFrozen) { throw new InvalidOperationException("MongoDatabaseSettings is frozen"); }
                safeMode = value;
            }
        }
        #endregion

        #region public methods
        public MongoDatabaseSettings Clone() {
            return new MongoDatabaseSettings(
                databaseName,
                credentials,
                safeMode
            );
        }

        public void Freeze() {
            if (!isFrozen) {
                frozenHashCode = GetHashCodeHelper();
                frozenStringRepresentation = ToStringHelper();
                isFrozen = true;
            }
        }

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
                        this.safeMode == rhs.safeMode;
                }
            }
        }

        public override int GetHashCode() {
            if (isFrozen) {
                return frozenHashCode;
            } else {
                return GetHashCodeHelper();
            }
        }

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
            hash = 37 * hash + ((safeMode == null) ? 0 : safeMode.GetHashCode());
            return hash;
        }

        private string ToStringHelper() {
            return string.Format(
                "DatabaseName={0};Credentials={1};SafeMode={2}",
                databaseName,
                credentials,
                safeMode
            );
        }
        #endregion
    }
}
