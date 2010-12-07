﻿/* Copyright 2010 10gen Inc.
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

namespace MongoDB.Bson {
    [Serializable]
    public class BsonMinKey : BsonValue, IComparable<BsonMinKey>, IEquatable<BsonMinKey> {
        #region private static fields
        private static BsonMinKey singleton = new BsonMinKey();
        #endregion

        #region constructors
        // private so only the singleton instance can be created
        private BsonMinKey()
            : base(BsonType.MinKey) {
        }
        #endregion

        #region public static properties
        public static BsonMinKey Value { get { return singleton; } }
        #endregion

        #region public methods
        public int CompareTo(
            BsonMinKey other
        ) {
            if (other == null) { return 1; }
            return 0; // it's a singleton
        }

        public override int CompareTo(
            BsonValue other
        ) {
            if (other == null) { return 1; }
            if (other is BsonMinKey) { return 0; }
            return -1;
        }

        public bool Equals(
            BsonMinKey rhs
        ) {
            return rhs != null; // it's a singleton
        }

        public override bool Equals(
            object obj
        ) {
            return Equals(obj as BsonMinKey); // works even if obj is null
        }

        public override int GetHashCode() {
            return bsonType.GetHashCode();
        }

        public override string ToString() {
            return "BsonMinKey";
        }
        #endregion
    }
}
