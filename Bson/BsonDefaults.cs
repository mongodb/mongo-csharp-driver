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

namespace MongoDB.Bson {
    /// <summary>
    /// A static class containing BSON defaults.
    /// </summary>
    public static class BsonDefaults {
        #region private static fields
        private static GuidRepresentation guidRepresentation = GuidRepresentation.CSharpLegacy;
        private static int initialBsonBufferSize = 4 * 1024; // 4KiB
        private static int maxDocumentSize = 4 * 1024 * 1024; // 4MiB
        #endregion

        #region public static properties
        /// <summary>
        /// Gets or sets the default Guid representation.
        /// </summary>
        public static GuidRepresentation GuidRepresentation {
            get { return guidRepresentation; }
            set { guidRepresentation = value; }
        }

        /// <summary>
        /// Gets or sets the default initial BSON buffer size.
        /// </summary>
        public static int InitialBsonBufferSize {
            get { return initialBsonBufferSize; }
            set { initialBsonBufferSize = value; }
        }

        /// <summary>
        /// Gets or sets the default max document size.
        /// </summary>
        public static int MaxDocumentSize {
            get { return maxDocumentSize; }
            set { maxDocumentSize = value; }
        }
        #endregion
    }
}
