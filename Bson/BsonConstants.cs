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

namespace MongoDB.Bson {
    public static class BsonConstants {
        #region private static fields
        private static DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        #endregion

        #region public static properties
        public static DateTime UnixEpoch { get { return unixEpoch; } }
        #endregion

        #region obsolete members
        [Obsolete("Use BsonBoolean.False instead (BsonConstants.False will be removed in version 0.9)")]
        public static BsonBoolean False { get { return BsonBoolean.False; } }
        [Obsolete("Use BsonMaxKey.Value instead (BsonConstants.MaxKey will be removed in version 0.9)")]
        public static BsonMaxKey MaxKey { get { return BsonMaxKey.Value; } }
        [Obsolete("Use BsonMinKey.Value instead (BsonConstants.MinKey will be removed in version 0.9)")]
        public static BsonMinKey MinKey { get { return BsonMinKey.Value; } }
        [Obsolete("Use BsonNull.Value instead (BsonConstants.Null will be removed in version 0.9)")]
        public static BsonNull Null { get { return BsonNull.Value; } }
        [Obsolete("Use BsonBoolean.True instead (BsonConstants.FalTruese will be removed in version 0.9)")]
        public static BsonBoolean True { get { return BsonBoolean.True; } }
        #endregion
    }
}
