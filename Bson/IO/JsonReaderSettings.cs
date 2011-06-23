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
    /// Represents settings for a JsonReader.
    /// </summary>
    [Serializable]
    public class JsonReaderSettings : BsonReaderSettings {
        #region private static fields
        private static JsonReaderSettings defaults = null; // delay creation to pick up the latest default values
        #endregion

        #region private fields
        private bool closeInput = false;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the JsonReaderSettings class.
        /// </summary>
        public JsonReaderSettings() {
        }

        /// <summary>
        /// Initializes a new instance of the JsonReaderSettings class.
        /// </summary>
        /// <param name="closeInput">Whether to close the input stream when the reader is closed.</param>
        /// <param name="guidRepresentation">The representation for Guids.</param>
        public JsonReaderSettings(
            bool closeInput,
            GuidRepresentation guidRepresentation
        )
            : base(guidRepresentation) {
            this.closeInput = closeInput;
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets or sets the default settings for a JsonReader.
        /// </summary>
        public static JsonReaderSettings Defaults {
            get {
                if (defaults == null) {
                    defaults = new JsonReaderSettings();
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
                if (isFrozen) { throw new InvalidOperationException("JsonReaderSettings is frozen."); }
                closeInput = value;
            }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Creates a clone of the settings.
        /// </summary>
        /// <returns>A clone of the settings.</returns>
        public new JsonReaderSettings Clone() {
            return (JsonReaderSettings) CloneImplementation();
        }
        #endregion

        #region protected methods
        /// <summary>
        /// Creates a clone of the settings.
        /// </summary>
        /// <returns>A clone of the settings.</returns>
        protected override BsonReaderSettings CloneImplementation() {
            return new JsonReaderSettings(
                closeInput,
                guidRepresentation
            );
        }
        #endregion
    }
}
