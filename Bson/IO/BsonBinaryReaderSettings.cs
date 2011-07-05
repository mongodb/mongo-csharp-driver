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
    /// Represents settings for a BsonBinaryReader.
    /// </summary>
    [Serializable]
    public class BsonBinaryReaderSettings : BsonReaderSettings {
        #region private static fields
        private static BsonBinaryReaderSettings defaults = null; // delay creation to pick up the latest default values
        #endregion

        #region private fields
        private bool closeInput = false;
        private bool fixOldBinarySubTypeOnInput = true;
        private bool fixOldDateTimeMaxValueOnInput = true;
        private int maxDocumentSize = BsonDefaults.MaxDocumentSize;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonBinaryReaderSettings class.
        /// </summary>
        public BsonBinaryReaderSettings() {
        }

        /// <summary>
        /// Initializes a new instance of the BsonBinaryReaderSettings class.
        /// </summary>
        /// <param name="closeInput">Whether to close the input stream when the reader is closed.</param>
        /// <param name="fixOldBinarySubTypeOnInput">Whether to fix occurrences of the old binary subtype on input.</param>
        /// <param name="fixOldDateTimeMaxValueOnInput">Whether to fix occurrences of the old representation of DateTime.MaxValue on input.</param>
        /// <param name="guidRepresentation">The representation for Guids.</param>
        /// <param name="maxDocumentSize">The max document size.</param>
        public BsonBinaryReaderSettings(
            bool closeInput,
            bool fixOldBinarySubTypeOnInput,
            bool fixOldDateTimeMaxValueOnInput,
            GuidRepresentation guidRepresentation,
            int maxDocumentSize
        ) 
            : base(guidRepresentation) {
            this.closeInput = closeInput;
            this.fixOldBinarySubTypeOnInput = fixOldBinarySubTypeOnInput;
            this.fixOldDateTimeMaxValueOnInput = fixOldDateTimeMaxValueOnInput;
            this.maxDocumentSize = maxDocumentSize;
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets or sets the default settings for a BsonBinaryReader.
        /// </summary>
        public static BsonBinaryReaderSettings Defaults {
            get {
                if (defaults == null) {
                    defaults = new BsonBinaryReaderSettings();
                }
                return defaults;
            }
            set { defaults = value; }
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets or sets whether to close the input stream when the reader is closed.
        /// </summary>
        public bool CloseInput {
            get { return closeInput; }
            set {
                if (isFrozen) { throw new InvalidOperationException("BsonBinaryReaderSettings is frozen."); }
                closeInput = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to fix occurrences of the old binary subtype on input. 
        /// </summary>
        public bool FixOldBinarySubTypeOnInput {
            get { return fixOldBinarySubTypeOnInput; }
            set {
                if (isFrozen) { throw new InvalidOperationException("BsonBinaryReaderSettings is frozen."); }
                fixOldBinarySubTypeOnInput = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to fix occurrences of the old representation of DateTime.MaxValue on input. 
        /// </summary>
        public bool FixOldDateTimeMaxValueOnInput {
            get { return fixOldDateTimeMaxValueOnInput; }
            set {
                if (isFrozen) { throw new InvalidOperationException("BsonBinaryReaderSettings is frozen."); }
                fixOldDateTimeMaxValueOnInput = value;
            }
        }

        /// <summary>
        /// Gets or sets the max document size.
        /// </summary>
        public int MaxDocumentSize {
            get { return maxDocumentSize; }
            set {
                if (isFrozen) { throw new InvalidOperationException("BsonBinaryReaderSettings is frozen."); }
                maxDocumentSize = value;
            }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Creates a clone of the settings.
        /// </summary>
        /// <returns>A clone of the settings.</returns>
        public new BsonBinaryReaderSettings Clone() {
            return (BsonBinaryReaderSettings) CloneImplementation();
        }
        #endregion

        #region protected methods
        /// <summary>
        /// Creates a clone of the settings.
        /// </summary>
        /// <returns>A clone of the settings.</returns>
        protected override BsonReaderSettings CloneImplementation() {
            return new BsonBinaryReaderSettings(
                closeInput,
                fixOldBinarySubTypeOnInput,
                fixOldDateTimeMaxValueOnInput,
                guidRepresentation,
                maxDocumentSize
            );
        }
        #endregion
    }
}
