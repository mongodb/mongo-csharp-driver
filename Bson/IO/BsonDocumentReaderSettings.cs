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

namespace MongoDB.Bson.IO {
    /// <summary>
    /// Represents settings for a BsonDocumentReader.
    /// </summary>
    public class BsonDocumentReaderSettings {
        #region private static fields
        private static BsonDocumentReaderSettings defaults = new BsonDocumentReaderSettings();
        #endregion

        #region private fields
        private GuidByteOrder guidByteOrder = BsonDefaults.GuidByteOrder;
        private bool isFrozen;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonDocumentReaderSettings class.
        /// </summary>
        public BsonDocumentReaderSettings() {
        }

        /// <summary>
        /// Initializes a new instance of the BsonDocumentReaderSettings class.
        /// </summary>
        /// <param name="guidByteOrder">The byte order for Guids.</param>
        public BsonDocumentReaderSettings(
            GuidByteOrder guidByteOrder
        ) {
            this.guidByteOrder = guidByteOrder;
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets or sets the default settings for a BsonDocumentReader.
        /// </summary>
        public static BsonDocumentReaderSettings Defaults {
            get { return defaults; }
            set { defaults = value; }
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets or sets the byte order for Guids.
        /// </summary>
        public GuidByteOrder GuidByteOrder {
            get { return guidByteOrder; }
            set {
                if (isFrozen) { throw new InvalidOperationException("BsonDocumentReaderSettings is frozen."); }
                guidByteOrder = value;
            }
        }

        /// <summary>
        /// Gets whether the settings are frozen.
        /// </summary>
        public bool IsFrozen {
            get { return isFrozen; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Creates a clone of the settings.
        /// </summary>
        /// <returns>A clone of the settings.</returns>
        public BsonDocumentReaderSettings Clone() {
            return new BsonDocumentReaderSettings(
                guidByteOrder
            );
        }

        /// <summary>
        /// Freezes the settings.
        /// </summary>
        /// <returns>The settings.</returns>
        public BsonDocumentReaderSettings Freeze() {
            isFrozen = true;
            return this;
        }
        #endregion
    }
}
