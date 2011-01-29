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
    public abstract class MongoCollectionSettings {
        #region private fields
        private string collectionName;
        private bool assignIdOnInsert;
        private Type defaultDocumentType;
        private SafeMode safeMode;
        private bool slaveOk;
        // the following fields are set when Freeze is called
        private bool isFrozen;
        private int frozenHashCode;
        private string frozenStringRepresentation;
        #endregion

        #region constructors
        protected MongoCollectionSettings(
            string collectionName,
            bool assignIdOnInsert,
            Type defaultDocumentType,
            SafeMode safeMode,
            bool slaveOk
        ) {
            this.collectionName = collectionName;
            this.assignIdOnInsert = assignIdOnInsert;
            this.defaultDocumentType = defaultDocumentType;
            this.safeMode = safeMode;
            this.slaveOk = slaveOk;
        }
        #endregion

        #region public properties
        public bool AssignIdOnInsert {
            get { return assignIdOnInsert; }
            set {
                if (isFrozen) { throw new InvalidOperationException("MongoDatabaseSettings is frozen"); }
                assignIdOnInsert = value;
            }
        }

        public string CollectionName {
            get { return collectionName; }
        }

        public Type DefaultDocumentType {
            get { return defaultDocumentType; }
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

        public bool SlaveOk {
            get { return slaveOk; }
            set {
                if (isFrozen) { throw new InvalidOperationException("MongoDatabaseSettings is frozen"); }
                slaveOk = value;
            }
        }
        #endregion

        #region public methods
        public void Freeze() {
            if (!isFrozen) {
                frozenHashCode = GetHashCodeHelper();
                frozenStringRepresentation = ToStringHelper();
                isFrozen = true;
            }
        }

        public override bool Equals(object obj) {
            var rhs = obj as MongoCollectionSettings;
            if (rhs == null) {
                return false;
            } else {
                if (this.isFrozen && rhs.isFrozen) {
                    return this.frozenStringRepresentation == rhs.frozenStringRepresentation;
                } else {
                    return
                        this.collectionName == rhs.collectionName &&
                        this.assignIdOnInsert == rhs.assignIdOnInsert &&
                        this.defaultDocumentType == rhs.defaultDocumentType &&
                        this.safeMode == rhs.safeMode &&
                        this.slaveOk == rhs.slaveOk;
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
            hash = 37 * hash + ((collectionName == null) ? 0 : collectionName.GetHashCode());
            hash = 37 * hash + assignIdOnInsert.GetHashCode();
            hash = 37 * hash + ((defaultDocumentType == null) ? 0 : defaultDocumentType.GetHashCode());
            hash = 37 * hash + ((safeMode == null) ? 0 : safeMode.GetHashCode());
            hash = 37 * hash + slaveOk.GetHashCode();
            return hash;
        }

        private string ToStringHelper() {
            return string.Format(
                "CollectionName={0};AssignIdOnInsert={1};DefaultDocumentType={2};SafeMode={3};SlaveOk={4}",
                collectionName,
                assignIdOnInsert,
                defaultDocumentType,
                safeMode,
                slaveOk
            );
        }
        #endregion
    }

    public class MongoCollectionSettings<TDefaultDocument> : MongoCollectionSettings {
        #region constructors
        public MongoCollectionSettings(
            string collectionName,
            bool assignIdOnInsert,
            SafeMode safeMode,
            bool slaveOk
        )
            : base(collectionName, assignIdOnInsert, typeof(TDefaultDocument), safeMode, slaveOk) {
        }
        #endregion
    }
}
