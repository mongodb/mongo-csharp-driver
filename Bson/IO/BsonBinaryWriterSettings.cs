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
    /// Represents settings for a BsonBinaryWriter.
    /// </summary>
    [Serializable]
    public class BsonBinaryWriterSettings : BsonWriterSettings {
        #region private static fields
        private static BsonBinaryWriterSettings defaults = null; // delay creation to pick up the latest default values
        #endregion

        #region private fields
        private bool closeOutput = false;
        private bool fixOldBinarySubTypeOnOutput = true;
        private int maxDocumentSize = BsonDefaults.MaxDocumentSize;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonBinaryWriterSettings class.
        /// </summary>
        public BsonBinaryWriterSettings() {
        }

        /// <summary>
        /// Initializes a new instance of the BsonBinaryWriterSettings class.
        /// </summary>
        /// <param name="closeOutput">Whether to close the output stream when the writer is closed.</param>
        /// <param name="fixOldBinarySubTypeOnOutput">Whether to fix old binary data subtype on output.</param>
        /// <param name="guidRepresentation">The representation for Guids.</param>
        /// <param name="maxDocumentSize">The max document size.</param>
        public BsonBinaryWriterSettings(
            bool closeOutput,
            bool fixOldBinarySubTypeOnOutput,
            GuidRepresentation guidRepresentation,
            int maxDocumentSize
        )
            : base(guidRepresentation) {
            this.closeOutput = closeOutput;
            this.fixOldBinarySubTypeOnOutput = fixOldBinarySubTypeOnOutput;
            this.maxDocumentSize = maxDocumentSize;
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets or sets the default BsonBinaryWriter settings.
        /// </summary>
        public static BsonBinaryWriterSettings Defaults {
            get {
                if (defaults == null) {
                    defaults = new BsonBinaryWriterSettings();
                }
                return defaults;
            }
            set { defaults = value; }
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets or sets whether to close the output when the writer is closed.
        /// </summary>
        public bool CloseOutput {
            get { return closeOutput; }
            set {
                if (isFrozen) { throw new InvalidOperationException("BsonBinaryWriterSettings is frozen."); }
                closeOutput = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to fix the old binary data subtype on output.
        /// </summary>
        public bool FixOldBinarySubTypeOnOutput {
            get { return fixOldBinarySubTypeOnOutput; }
            set {
                if (isFrozen) { throw new InvalidOperationException("BsonBinaryWriterSettings is frozen."); }
                fixOldBinarySubTypeOnOutput = value;
            }
        }

        /// <summary>
        /// Gets or sets the max document size.
        /// </summary>
        public int MaxDocumentSize {
            get { return maxDocumentSize; }
            set {
                if (isFrozen) { throw new InvalidOperationException("BsonBinaryWriterSettings is frozen."); }
                maxDocumentSize = value;
            }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Creates a clone of the settings.
        /// </summary>
        /// <returns>A clone of the settings.</returns>
        public new BsonBinaryWriterSettings Clone() {
            return (BsonBinaryWriterSettings) CloneImplementation();
        }
        #endregion

        #region protected methods
        /// <summary>
        /// Creates a clone of the settings.
        /// </summary>
        /// <returns>A clone of the settings.</returns>
        protected override BsonWriterSettings CloneImplementation() {
            return new BsonBinaryWriterSettings(
                closeOutput,
                fixOldBinarySubTypeOnOutput,
                guidRepresentation,
                maxDocumentSize
            );
        }
        #endregion
    }
}
