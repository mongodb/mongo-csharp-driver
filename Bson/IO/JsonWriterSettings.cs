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
    /// Represents settings for a JsonWriter.
    /// </summary>
    [Serializable]
    public class JsonWriterSettings {
        #region private static fields
        private static JsonWriterSettings defaults = null; // delay creation to pick up the latest default values
        #endregion

        #region private fields
        private bool closeOutput = false;
        private Encoding encoding = Encoding.UTF8;
        private GuidRepresentation guidRepresentation = BsonDefaults.GuidRepresentation;
        private bool indent = false;
        private string indentChars = "  ";
        private string newLineChars = "\r\n";
        private JsonOutputMode outputMode = JsonOutputMode.Shell;
        private bool isFrozen;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the JsonWriterSettings class.
        /// </summary>
        public JsonWriterSettings() {
        }
   
        /// <summary>
        /// Initializes a new instance of the JsonWriterSettings class.
        /// </summary>
        /// <param name="closeOutput">Whether to close the output when the writer is closed.</param>
        /// <param name="encoding">The output Encoding.</param>
        /// <param name="indent">Whether to indent the output.</param>
        /// <param name="indentChars">The indentation characters.</param>
        /// <param name="newLineChars">The new line characters.</param>
        /// <param name="outputMode">The output mode.</param>
        public JsonWriterSettings(
            bool closeOutput,
            Encoding encoding,
            bool indent,
            string indentChars,
            string newLineChars,
            JsonOutputMode outputMode
        ) {
            this.closeOutput = closeOutput;
            this.encoding = encoding;
            this.indent = indent;
            this.indentChars = indentChars;
            this.newLineChars = newLineChars;
            this.outputMode = outputMode;
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets or sets the default JsonWriterSettings.
        /// </summary>
        public static JsonWriterSettings Defaults {
            get {
                if (defaults == null) {
                    defaults = new JsonWriterSettings();
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
                if (isFrozen) { throw new InvalidOperationException("JsonWriterSettings is frozen."); }
                closeOutput = value;
            }
        }

        /// <summary>
        /// Gets or sets the output Encoding.
        /// </summary>
        public Encoding Encoding {
            get { return encoding; }
            set {
                if (isFrozen) { throw new InvalidOperationException("JsonWriterSettings is frozen."); }
                encoding = value;
            }
        }

        /// <summary>
        /// Gets or sets the representation for Guids.
        /// </summary>
        public GuidRepresentation GuidRepresentation {
            get { return guidRepresentation; }
            set {
                if (isFrozen) { throw new InvalidOperationException("JsonWriterSettings is frozen."); }
                guidRepresentation = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to indent the output.
        /// </summary>
        public bool Indent {
            get { return indent; }
            set {
                if (isFrozen) { throw new InvalidOperationException("JsonWriterSettings is frozen."); }
                indent = value;
            }
        }

        /// <summary>
        /// Gets or sets the indent characters.
        /// </summary>
        public string IndentChars {
            get { return indentChars; }
            set {
                if (isFrozen) { throw new InvalidOperationException("JsonWriterSettings is frozen."); }
                indentChars = value;
            }
        }

        /// <summary>
        /// Gets whether the settings are frozen.
        /// </summary>
        public bool IsFrozen {
            get { return isFrozen; }
        }

        /// <summary>
        /// Gets or sets the new line characters.
        /// </summary>
        public string NewLineChars {
            get { return newLineChars; }
            set {
                if (isFrozen) { throw new InvalidOperationException("JsonWriterSettings is frozen."); }
                newLineChars = value;
            }
        }

        /// <summary>
        /// Gets or sets the output mode.
        /// </summary>
        public JsonOutputMode OutputMode {
            get { return outputMode; }
            set {
                if (isFrozen) { throw new InvalidOperationException("JsonWriterSettings is frozen."); }
                outputMode = value;
            }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Creates a clone of the settings.
        /// </summary>
        /// <returns>A clone of the settings.</returns>
        public JsonWriterSettings Clone() {
            return new JsonWriterSettings(
                closeOutput,
                encoding,
                indent,
                indentChars,
                newLineChars,
                outputMode
            );
        }

        /// <summary>
        /// Freezes the settings.
        /// </summary>
        /// <returns>The settings.</returns>
        public JsonWriterSettings Freeze() {
            isFrozen = true;
            return this;
        }
        #endregion
    }
}
