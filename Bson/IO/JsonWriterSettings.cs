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
    [Serializable]
    public class JsonWriterSettings {
        #region private static fields
        private static JsonWriterSettings defaults = new JsonWriterSettings();
        #endregion

        #region private fields
        private bool closeOutput = false;
        private Encoding encoding = Encoding.UTF8;
        private bool indent = false;
        private string indentChars = "  ";
        private string newLineChars = "\r\n";
        private JsonOutputMode outputMode = JsonOutputMode.Strict;
        private bool isFrozen;
        #endregion

        #region constructors
        public JsonWriterSettings() {
        }
   
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
        public static JsonWriterSettings Defaults {
            get { return defaults; }
            set { defaults = value; }
        }
        #endregion

        #region public properties
        public bool CloseOutput {
            get { return closeOutput; }
            set {
                if (isFrozen) { throw new InvalidOperationException("JsonWriterSettings is frozen"); }
                closeOutput = value;
            }
        }

        public Encoding Encoding {
            get { return encoding; }
            set {
                if (isFrozen) { throw new InvalidOperationException("JsonWriterSettings is frozen"); }
                encoding = value;
            }
        }

        public bool Indent {
            get { return indent; }
            set {
                if (isFrozen) { throw new InvalidOperationException("JsonWriterSettings is frozen"); }
                indent = value;
            }
        }

        public string IndentChars {
            get { return indentChars; }
            set {
                if (isFrozen) { throw new InvalidOperationException("JsonWriterSettings is frozen"); }
                indentChars = value;
            }
        }

        public bool IsFrozen {
            get { return isFrozen; }
        }

        public string NewLineChars {
            get { return newLineChars; }
            set {
                if (isFrozen) { throw new InvalidOperationException("JsonWriterSettings is frozen"); }
                newLineChars = value;
            }
        }

        public JsonOutputMode OutputMode {
            get { return outputMode; }
            set {
                if (isFrozen) { throw new InvalidOperationException("JsonWriterSettings is frozen"); }
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

        public JsonWriterSettings Freeze() {
            isFrozen = true;
            return this;
        }
        #endregion
    }
}
