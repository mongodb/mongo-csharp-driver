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

namespace MongoDB.Driver.GridFS {
    [Serializable]
    public class MongoGridFSSettings : IEquatable<MongoGridFSSettings> {
        #region private fields
        private bool isFrozen;
        private string chunksCollectionName = "fs.chunks";
        private int defaultChunkSize = 256 * 1024; // 256KiB
        private string filesCollectionName = "fs.files";
        private string root = "fs";
        private SafeMode safeMode = SafeMode.False;
        #endregion

        #region constructors
        public MongoGridFSSettings() {
        }
        #endregion

        #region public properties
        public string ChunksCollectionName {
            get { return chunksCollectionName; }
        }

        public int DefaultChunkSize {
            get { return defaultChunkSize; }
            set {
                if (isFrozen) { ThrowFrozen(); }
                defaultChunkSize = value;
            }
        }

        public string FilesCollectionName {
            get { return filesCollectionName; }
        }

        public bool IsFrozen {
            get { return isFrozen; }
        }

        public string Root {
            get { return root; }
            set {
                if (isFrozen) { ThrowFrozen(); }
                root = value;
                filesCollectionName = value + ".files";
                chunksCollectionName = value + ".chunks";
            }
        }

        public SafeMode SafeMode {
            get { return safeMode; }
            set {
                if (isFrozen) { ThrowFrozen(); }
                safeMode = value;
            }
        }
        #endregion

        #region public operators
        public static bool operator !=(
            MongoGridFSSettings lhs,
            MongoGridFSSettings rhs
        ) {
            return !(lhs == rhs);
        }

        public static bool operator ==(
            MongoGridFSSettings lhs,
            MongoGridFSSettings rhs
        ) {
            return object.Equals(lhs, rhs);
        }
        #endregion

        #region public methods
        public MongoGridFSSettings Clone() {
            return new MongoGridFSSettings {
                DefaultChunkSize = defaultChunkSize,
                Root = root,
                SafeMode = safeMode
            };
        }

        public bool Equals(
            MongoGridFSSettings rhs
        ) {
            if (rhs == null) { return false; }
            return 
                this.defaultChunkSize == rhs.defaultChunkSize &&
                this.root == rhs.root && 
                this.safeMode == rhs.safeMode;
        }

        public override bool Equals(object obj) {
            return Equals(obj as MongoGridFSSettings); // works even if obj is null
        }

        public MongoGridFSSettings Freeze() {
            isFrozen = true;
            return this;
        }

        public override int GetHashCode() {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + defaultChunkSize.GetHashCode();
            hash = 37 * hash + root.GetHashCode();
            hash = 37 * hash + safeMode.GetHashCode();
            return hash;
        }
        #endregion

        #region private methods
        private void ThrowFrozen() {
            throw new InvalidOperationException("A MongoGridFSSettings object cannot be modified once it has been frozen");
        }
        #endregion
    }
}
