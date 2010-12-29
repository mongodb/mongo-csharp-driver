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
using System.IO;
using System.Linq;
using System.Text;

namespace MongoDB.Bson.IO {
    public abstract class BsonBaseWriter : BsonWriter {
        #region protected fields
        protected bool disposed = false;
        protected BsonWriterState state;
        protected string name;
        #endregion

        #region constructors
        protected BsonBaseWriter() {
        }
        #endregion

        #region public properties
        public override BsonWriterState State {
            get { return state; }
        }
        #endregion

        #region public methods
        public override void Dispose() {
            if (!disposed) {
                Dispose(true);
                disposed = true;
            }
        }

        public override void WriteBinaryData(
            string name,
            byte[] bytes,
            BsonBinarySubType subType
        ) {
            WriteName(name);
            WriteBinaryData(bytes, subType);
        }

        public override void WriteBoolean(
            string name,
            bool value
        ) {
            WriteName(name);
            WriteBoolean(value);
        }

        public override void WriteDateTime(
            string name,
            DateTime value
        ) {
            WriteName(name);
            WriteDateTime(value);
        }

        public override void WriteDouble(
            string name,
            double value
        ) {
            WriteName(name);
            WriteDouble(value);
        }

        public override void WriteInt32(
            string name,
            int value
        ) {
            WriteName(name);
            WriteInt32(value);
        }

        public override void WriteInt64(
            string name,
            long value
        ) {
            WriteName(name);
            WriteInt64(value);
        }

        public override void WriteJavaScript(
            string name,
            string code
        ) {
            WriteName(name);
            WriteJavaScript(code);
        }

        public override void WriteJavaScriptWithScope(
            string name,
            string code
        ) {
            WriteName(name);
            WriteJavaScriptWithScope(code);
        }

        public override void WriteMaxKey(
            string name
        ) {
            WriteName(name);
            WriteMaxKey();
        }

        public override void WriteMinKey(
            string name
        ) {
            WriteName(name);
            WriteMinKey();
        }

        public override void WriteName(
            string name
        ) {
            if (disposed) { throw new ObjectDisposedException(this.GetType().Name); }
            if (state != BsonWriterState.Name) {
                var message = string.Format("WriteName cannot be called when State is: {0}", state);
                throw new InvalidOperationException(message);
            }

            this.name = name;
            state = BsonWriterState.Value;
        }

        public override void WriteNull(
            string name
        ) {
            WriteName(name);
            WriteNull();
        }

        public override void WriteObjectId(
            string name,
            int timestamp,
            int machine,
            short pid,
            int increment
        ) {
            WriteName(name);
            WriteObjectId(timestamp, machine, pid, increment);
        }

        public override void WriteRegularExpression(
            string name,
            string pattern,
            string options
        ) {
            WriteName(name);
            WriteRegularExpression(pattern, options);
        }

        public override void WriteStartArray(
            string name
        ) {
            WriteName(name);
            WriteStartArray();
        }

        public override void WriteStartDocument(
            string name
        ) {
            WriteName(name);
            WriteStartDocument();
        }

        public override void WriteString(
            string name,
            string value
        ) {
            WriteName(name);
            WriteString(value);
        }

        public override void WriteSymbol(
            string name,
            string value
        ) {
            WriteName(name);
            WriteSymbol(value);
        }

        public override void WriteTimestamp(
            string name,
            long value
        ) {
            WriteName(name);
            WriteTimestamp(value);
        }
        #endregion

        #region protected methods
        protected abstract void Dispose(
            bool disposing
        );
        #endregion
    }
}
