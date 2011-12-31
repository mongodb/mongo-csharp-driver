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
using System.IO;
using System.Linq;
using System.Text;

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// Represents a BSON writer to a BsonDocument.
    /// </summary>
    public class BsonDocumentWriter : BsonWriter
    {
        // private fields
        private BsonDocument _topLevelDocument;
        private BsonDocumentWriterSettings _documentWriterSettings; // same value as in base class just declared as derived class
        private BsonDocumentWriterContext _context;

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonDocumentWriter class.
        /// </summary>
        /// <param name="topLevelDocument">The document to write to (normally starts out as an empty document).</param>
        /// <param name="settings">The settings.</param>
        public BsonDocumentWriter(BsonDocument topLevelDocument, BsonDocumentWriterSettings settings)
            : base(settings)
        {
            _topLevelDocument = topLevelDocument;
            _documentWriterSettings = settings; // already frozen by base class
            _context = null;
            _state = BsonWriterState.Initial;
        }

        // public properties
        /// <summary>
        /// Gets the top level BsonDocument.
        /// </summary>
        public BsonDocument TopLevelDocument
        {
            get { return _topLevelDocument; }
        }

        // public methods
        /// <summary>
        /// Closes the writer.
        /// </summary>
        public override void Close()
        {
            // Close can be called on Disposed objects
            if (_state != BsonWriterState.Closed)
            {
                _context = null;
                _state = BsonWriterState.Closed;
            }
        }

        /// <summary>
        /// Flushes any pending data to the output destination.
        /// </summary>
        public override void Flush()
        {
            if (_disposed) { throw new ObjectDisposedException("BsonDocumentWriter"); }
        }

        /// <summary>
        /// Writes BSON binary data to the writer.
        /// </summary>
        /// <param name="bytes">The binary data.</param>
        /// <param name="subType">The binary data subtype.</param>
        /// <param name="guidRepresentation">The representation for Guids.</param>
        public override void WriteBinaryData(byte[] bytes, BsonBinarySubType subType, GuidRepresentation guidRepresentation)
        {
            if (_disposed) { throw new ObjectDisposedException("BsonDocumentWriter"); }
            if (_state != BsonWriterState.Value)
            {
                ThrowInvalidState("WriteBinaryData", BsonWriterState.Value);
            }

            WriteValue(new BsonBinaryData(bytes, subType, guidRepresentation));
            _state = GetNextState();
        }

        /// <summary>
        /// Writes a BSON Boolean to the writer.
        /// </summary>
        /// <param name="value">The Boolean value.</param>
        public override void WriteBoolean(bool value)
        {
            if (_disposed) { throw new ObjectDisposedException("BsonDocumentWriter"); }
            if (_state != BsonWriterState.Value)
            {
                ThrowInvalidState("WriteBoolean", BsonWriterState.Value);
            }

            WriteValue(value);
            _state = GetNextState();
        }

        /// <summary>
        /// Writes a BSON DateTime to the writer.
        /// </summary>
        /// <param name="value">The number of milliseconds since the Unix epoch.</param>
        public override void WriteDateTime(long value)
        {
            if (_disposed) { throw new ObjectDisposedException("BsonDocumentWriter"); }
            if (_state != BsonWriterState.Value)
            {
                ThrowInvalidState("WriteDateTime", BsonWriterState.Value);
            }

            WriteValue(new BsonDateTime(value));
            _state = GetNextState();
        }

        /// <summary>
        /// Writes a BSON Double to the writer.
        /// </summary>
        /// <param name="value">The Double value.</param>
        public override void WriteDouble(double value)
        {
            if (_disposed) { throw new ObjectDisposedException("BsonDocumentWriter"); }
            if (_state != BsonWriterState.Value)
            {
                ThrowInvalidState("WriteDouble", BsonWriterState.Value);
            }

            WriteValue(value);
            _state = GetNextState();
        }

        /// <summary>
        /// Writes the end of a BSON array to the writer.
        /// </summary>
        public override void WriteEndArray()
        {
            if (_disposed) { throw new ObjectDisposedException("BsonDocumentWriter"); }
            if (_state != BsonWriterState.Value)
            {
                ThrowInvalidState("WriteEndArray", BsonWriterState.Value);
            }
            if (_context.ContextType != ContextType.Array)
            {
                ThrowInvalidContextType("WriteEndArray", _context.ContextType, ContextType.Array);
            }

            var array = _context.Array;
            _context = _context.ParentContext;
            WriteValue(array);
            _state = GetNextState();
        }

        /// <summary>
        /// Writes the end of a BSON document to the writer.
        /// </summary>
        public override void WriteEndDocument()
        {
            if (_disposed) { throw new ObjectDisposedException("BsonDocumentWriter"); }
            if (_state != BsonWriterState.Name)
            {
                ThrowInvalidState("WriteEndDocument", BsonWriterState.Name);
            }
            if (_context.ContextType != ContextType.Document && _context.ContextType != ContextType.ScopeDocument)
            {
                ThrowInvalidContextType("WriteEndDocument", _context.ContextType, ContextType.Document, ContextType.ScopeDocument);
            }

            if (_context.ContextType == ContextType.ScopeDocument)
            {
                var scope = _context.Document;
                _context = _context.ParentContext;
                var code = _context.Code;
                _context = _context.ParentContext;
                WriteValue(new BsonJavaScriptWithScope(code, scope));
            }
            else
            {
                var document = _context.Document;
                _context = _context.ParentContext;
                if (_context != null)
                {
                    WriteValue(document);
                }
            }

            if (_context == null)
            {
                _state = BsonWriterState.Done;
            }
            else
            {
                _state = GetNextState();
            }
        }

        /// <summary>
        /// Writes a BSON Int32 to the writer.
        /// </summary>
        /// <param name="value">The Int32 value.</param>
        public override void WriteInt32(int value)
        {
            if (_disposed) { throw new ObjectDisposedException("BsonDocumentWriter"); }
            if (_state != BsonWriterState.Value)
            {
                ThrowInvalidState("WriteInt32", BsonWriterState.Value);
            }

            WriteValue(value);
            _state = GetNextState();
        }

        /// <summary>
        /// Writes a BSON Int64 to the writer.
        /// </summary>
        /// <param name="value">The Int64 value.</param>
        public override void WriteInt64(long value)
        {
            if (_disposed) { throw new ObjectDisposedException("BsonDocumentWriter"); }
            if (_state != BsonWriterState.Value)
            {
                ThrowInvalidState("WriteInt64", BsonWriterState.Value);
            }

            WriteValue(value);
            _state = GetNextState();
        }

        /// <summary>
        /// Writes a BSON JavaScript to the writer.
        /// </summary>
        /// <param name="code">The JavaScript code.</param>
        public override void WriteJavaScript(string code)
        {
            if (_disposed) { throw new ObjectDisposedException("BsonDocumentWriter"); }
            if (_state != BsonWriterState.Value)
            {
                ThrowInvalidState("WriteJavaScript", BsonWriterState.Value);
            }

            WriteValue(new BsonJavaScript(code));
            _state = GetNextState();
        }

        /// <summary>
        /// Writes a BSON JavaScript to the writer (call WriteStartDocument to start writing the scope).
        /// </summary>
        /// <param name="code">The JavaScript code.</param>
        public override void WriteJavaScriptWithScope(string code)
        {
            if (_disposed) { throw new ObjectDisposedException("BsonDocumentWriter"); }
            if (_state != BsonWriterState.Value)
            {
                ThrowInvalidState("WriteJavaScriptWithScope", BsonWriterState.Value);
            }

            _context = new BsonDocumentWriterContext(_context, ContextType.JavaScriptWithScope, code);
            _state = BsonWriterState.ScopeDocument;
        }

        /// <summary>
        /// Writes a BSON MaxKey to the writer.
        /// </summary>
        public override void WriteMaxKey()
        {
            if (_disposed) { throw new ObjectDisposedException("BsonDocumentWriter"); }
            if (_state != BsonWriterState.Value)
            {
                ThrowInvalidState("WriteMaxKey", BsonWriterState.Value);
            }

            WriteValue(BsonMaxKey.Value);
            _state = GetNextState();
        }

        /// <summary>
        /// Writes a BSON MinKey to the writer.
        /// </summary>
        public override void WriteMinKey()
        {
            if (_disposed) { throw new ObjectDisposedException("BsonDocumentWriter"); }
            if (_state != BsonWriterState.Value)
            {
                ThrowInvalidState("WriteMinKey", BsonWriterState.Value);
            }

            WriteValue(BsonMinKey.Value);
            _state = GetNextState();
        }

        /// <summary>
        /// Writes the name of an element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public override void WriteName(string name)
        {
            base.WriteName(name);
            _context.Name = name;
        }

        /// <summary>
        /// Writes a BSON null to the writer.
        /// </summary>
        public override void WriteNull()
        {
            if (_disposed) { throw new ObjectDisposedException("BsonDocumentWriter"); }
            if (_state != BsonWriterState.Value)
            {
                ThrowInvalidState("WriteNull", BsonWriterState.Value);
            }

            WriteValue(BsonNull.Value);
            _state = GetNextState();
        }

        /// <summary>
        /// Writes a BSON ObjectId to the writer.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        public override void WriteObjectId(int timestamp, int machine, short pid, int increment)
        {
            if (_disposed) { throw new ObjectDisposedException("BsonDocumentWriter"); }
            if (_state != BsonWriterState.Value)
            {
                ThrowInvalidState("WriteObjectId", BsonWriterState.Value);
            }

            WriteValue(new ObjectId(timestamp, machine, pid, increment));
            _state = GetNextState();
        }

        /// <summary>
        /// Writes a BSON regular expression to the writer.
        /// </summary>
        /// <param name="pattern">A regular expression pattern.</param>
        /// <param name="options">A regular expression options.</param>
        public override void WriteRegularExpression(string pattern, string options)
        {
            if (_disposed) { throw new ObjectDisposedException("BsonDocumentWriter"); }
            if (_state != BsonWriterState.Value)
            {
                ThrowInvalidState("WriteRegularExpression", BsonWriterState.Value);
            }

            WriteValue(new BsonRegularExpression(pattern, options));
            _state = GetNextState();
        }

        /// <summary>
        /// Writes the start of a BSON array to the writer.
        /// </summary>
        public override void WriteStartArray()
        {
            if (_disposed) { throw new ObjectDisposedException("BsonDocumentWriter"); }
            if (_state != BsonWriterState.Value)
            {
                ThrowInvalidState("WriteStartArray", BsonWriterState.Value);
            }

            _context = new BsonDocumentWriterContext(_context, ContextType.Array, new BsonArray());
            _state = BsonWriterState.Value;
        }

        /// <summary>
        /// Writes the start of a BSON document to the writer.
        /// </summary>
        public override void WriteStartDocument()
        {
            if (_disposed) { throw new ObjectDisposedException("BsonDocumentWriter"); }
            if (_state != BsonWriterState.Initial && _state != BsonWriterState.Value && _state != BsonWriterState.ScopeDocument && _state != BsonWriterState.Done)
            {
                ThrowInvalidState("WriteStartDocument", BsonWriterState.Initial, BsonWriterState.Value, BsonWriterState.ScopeDocument, BsonWriterState.Done);
            }

            switch (_state)
            {
                case BsonWriterState.Initial:
                case BsonWriterState.Done:
                    _context = new BsonDocumentWriterContext(null, ContextType.Document, _topLevelDocument);
                    break;
                case BsonWriterState.Value:
                    _context = new BsonDocumentWriterContext(_context, ContextType.Document, new BsonDocument());
                    break;
                case BsonWriterState.ScopeDocument:
                    _context = new BsonDocumentWriterContext(_context, ContextType.ScopeDocument, new BsonDocument());
                    break;
                default:
                    throw new BsonInternalException("Unexpected state.");
            }

            _state = BsonWriterState.Name;
        }

        /// <summary>
        /// Writes a BSON String to the writer.
        /// </summary>
        /// <param name="value">The String value.</param>
        public override void WriteString(string value)
        {
            if (_disposed) { throw new ObjectDisposedException("BsonDocumentWriter"); }
            if (_state != BsonWriterState.Value)
            {
                ThrowInvalidState("WriteString", BsonWriterState.Value);
            }

            WriteValue(value);
            _state = GetNextState();
        }

        /// <summary>
        /// Writes a BSON Symbol to the writer.
        /// </summary>
        /// <param name="value">The symbol.</param>
        public override void WriteSymbol(string value)
        {
            if (_disposed) { throw new ObjectDisposedException("BsonDocumentWriter"); }
            if (_state != BsonWriterState.Value)
            {
                ThrowInvalidState("WriteSymbol", BsonWriterState.Value);
            }

            WriteValue(BsonSymbol.Create(value));
            _state = GetNextState();
        }

        /// <summary>
        /// Writes a BSON timestamp to the writer.
        /// </summary>
        /// <param name="value">The combined timestamp/increment value.</param>
        public override void WriteTimestamp(long value)
        {
            if (_disposed) { throw new ObjectDisposedException("BsonDocumentWriter"); }
            if (_state != BsonWriterState.Value)
            {
                ThrowInvalidState("WriteTimestamp", BsonWriterState.Value);
            }

            WriteValue(new BsonTimestamp(value));
            _state = GetNextState();
        }

        /// <summary>
        /// Writes a BSON undefined to the writer.
        /// </summary>
        public override void WriteUndefined()
        {
            if (_disposed) { throw new ObjectDisposedException("BsonDocumentWriter"); }
            if (_state != BsonWriterState.Value)
            {
                ThrowInvalidState("WriteUndefined", BsonWriterState.Value);
            }

            WriteValue(BsonUndefined.Value);
            _state = GetNextState();
        }

        // protected methods
        /// <summary>
        /// Disposes of any resources used by the writer.
        /// </summary>
        /// <param name="disposing">True if called from Dispose.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
            }
        }

        // private methods
        private BsonWriterState GetNextState()
        {
            if (_context.ContextType == ContextType.Array)
            {
                return BsonWriterState.Value;
            }
            else
            {
                return BsonWriterState.Name;
            }
        }

        private void WriteValue(BsonValue value)
        {
            if (_context.ContextType == ContextType.Array)
            {
                _context.Array.Add(value);
            }
            else
            {
                _context.Document.Add(_context.Name, value);
            }
        }
    }
}
