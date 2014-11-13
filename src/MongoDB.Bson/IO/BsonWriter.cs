﻿/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// Represents a BSON writer for some external format (see subclasses).
    /// </summary>
    public abstract class BsonWriter : IDisposable
    {
        // private fields
        private Func<IElementNameValidator> _childElementNameValidatorFactory = () => NoOpElementNameValidator.Instance;
        private bool _disposed = false;
        private IElementNameValidator _elementNameValidator = NoOpElementNameValidator.Instance;
        private Stack<IElementNameValidator> _elementNameValidatorStack = new Stack<IElementNameValidator>();
        private BsonWriterSettings _settings;
        private BsonWriterState _state;
        private string _name;
        private int _serializationDepth;

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonWriter class.
        /// </summary>
        /// <param name="settings">The writer settings.</param>
        protected BsonWriter(BsonWriterSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            _settings = settings.FrozenCopy();
            _state = BsonWriterState.Initial;
        }

        // public properties
        /// <summary>
        /// Gets the current serialization depth.
        /// </summary>
        public int SerializationDepth
        {
            get { return _serializationDepth; }
        }

        /// <summary>
        /// Gets the settings of the writer.
        /// </summary>
        public BsonWriterSettings Settings
        {
            get { return _settings; }
        }

        /// <summary>
        /// Gets the current state of the writer.
        /// </summary>
        public BsonWriterState State
        {
            get { return _state; }
            protected set { _state = value; }
        }

        // protected properties
        /// <summary>
        /// Gets whether the BsonWriter has been disposed.
        /// </summary>
        public bool Disposed
        {
            get { return _disposed; }
        }

        // protected properties
        /// <summary>
        /// Gets the name of the element being written.
        /// </summary>
        protected string Name
        {
            get { return _name; }
        }

        // public static methods
        // public methods
        /// <summary>
        /// Closes the writer.
        /// </summary>
        public abstract void Close();

        /// <summary>
        /// Disposes of any resources used by the writer.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                Dispose(true);
                _disposed = true;
            }
        }

        /// <summary>
        /// Flushes any pending data to the output destination.
        /// </summary>
        public abstract void Flush();

        /// <summary>
        /// Pops the element name validator.
        /// </summary>
        /// <returns>The popped element validator.</returns>
        public void PopElementNameValidator()
        {
            _elementNameValidator = _elementNameValidatorStack.Pop();
            _childElementNameValidatorFactory = () => _elementNameValidator;
        }

        /// <summary>
        /// Pushes the element name validator.
        /// </summary>
        /// <param name="validator">The validator.</param>
        public void PushElementNameValidator(IElementNameValidator validator)
        {
            if (validator == null)
            {
                throw new ArgumentNullException("validator");
            }

            _elementNameValidatorStack.Push(_elementNameValidator);
            _elementNameValidator = validator;
            _childElementNameValidatorFactory = () => _elementNameValidator;
        }

        /// <summary>
        /// Writes BSON binary data to the writer.
        /// </summary>
        /// <param name="binaryData">The binary data.</param>
        public abstract void WriteBinaryData(BsonBinaryData binaryData);

        /// <summary>
        /// Writes a BSON binary data element to the writer.
        /// </summary>
        /// <param name="bytes">The binary data.</param>
        /// <param name="subType">The binary data subtype.</param>
        [Obsolete("Use WriteBinaryData(BsonBinaryData binaryData) instead.")]
        public void WriteBinaryData(byte[] bytes, BsonBinarySubType subType)
        {
            var guidRepresentation = (subType == BsonBinarySubType.UuidStandard) ? GuidRepresentation.Standard : GuidRepresentation.Unspecified;
            WriteBinaryData(bytes, subType, guidRepresentation);
        }

        /// <summary>
        /// Writes BSON binary data to the writer.
        /// </summary>
        /// <param name="bytes">The binary data.</param>
        /// <param name="subType">The binary data subtype.</param>
        /// <param name="guidRepresentation">The respresentation for Guids.</param>
        [Obsolete("Use WriteBinaryData(BsonBinaryData binaryData) instead.")]
        public void WriteBinaryData(
            byte[] bytes,
            BsonBinarySubType subType,
            GuidRepresentation guidRepresentation)
        {
            var binaryData = new BsonBinaryData(bytes, subType, guidRepresentation);
            WriteBinaryData(binaryData);
        }

        /// <summary>
        /// Writes a BSON binary data element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="binaryData">The binary data.</param>
        public void WriteBinaryData(string name, BsonBinaryData binaryData)
        {
            WriteName(name);
            WriteBinaryData(binaryData);
        }

        /// <summary>
        /// Writes a BSON binary data element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="bytes">The binary data.</param>
        /// <param name="subType">The binary data subtype.</param>
        [Obsolete("Use WriteBinaryData(string name, BsonBinaryData binaryData) instead.")]
        public void WriteBinaryData(string name, byte[] bytes, BsonBinarySubType subType)
        {
            WriteName(name);
            WriteBinaryData(bytes, subType);
        }

        /// <summary>
        /// Writes a BSON binary data element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="bytes">The binary data.</param>
        /// <param name="subType">The binary data subtype.</param>
        /// <param name="guidRepresentation">The representation for Guids.</param>
        [Obsolete("Use WriteBinaryData(string name, BsonBinaryData binaryData) instead.")]
        public void WriteBinaryData(
            string name,
            byte[] bytes,
            BsonBinarySubType subType,
            GuidRepresentation guidRepresentation)
        {
            WriteName(name);
            WriteBinaryData(bytes, subType, guidRepresentation);
        }

        /// <summary>
        /// Writes a BSON Boolean to the writer.
        /// </summary>
        /// <param name="value">The Boolean value.</param>
        public abstract void WriteBoolean(bool value);

        /// <summary>
        /// Writes a BSON Boolean element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The Boolean value.</param>
        public void WriteBoolean(string name, bool value)
        {
            WriteName(name);
            WriteBoolean(value);
        }

        /// <summary>
        /// Writes BSON binary data to the writer.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        public abstract void WriteBytes(byte[] bytes);

        /// <summary>
        /// Writes a BSON binary data element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="bytes">The bytes.</param>
        public void WriteBytes(string name, byte[] bytes)
        {
            WriteName(name);
            WriteBytes(bytes);
        }

        /// <summary>
        /// Writes a BSON DateTime to the writer.
        /// </summary>
        /// <param name="value">The number of milliseconds since the Unix epoch.</param>
        public abstract void WriteDateTime(long value);

        /// <summary>
        /// Writes a BSON DateTime element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The number of milliseconds since the Unix epoch.</param>
        public void WriteDateTime(string name, long value)
        {
            WriteName(name);
            WriteDateTime(value);
        }

        /// <summary>
        /// Writes a BSON Double to the writer.
        /// </summary>
        /// <param name="value">The Double value.</param>
        public abstract void WriteDouble(double value);

        /// <summary>
        /// Writes a BSON Double element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The Double value.</param>
        public void WriteDouble(string name, double value)
        {
            WriteName(name);
            WriteDouble(value);
        }

        /// <summary>
        /// Writes the end of a BSON array to the writer.
        /// </summary>
        public virtual void WriteEndArray()
        {
            _serializationDepth--;
        }

        /// <summary>
        /// Writes the end of a BSON document to the writer.
        /// </summary>
        public virtual void WriteEndDocument()
        {
            _serializationDepth--;

            PopElementNameValidator();
        }

        /// <summary>
        /// Writes a BSON Int32 to the writer.
        /// </summary>
        /// <param name="value">The Int32 value.</param>
        public abstract void WriteInt32(int value);

        /// <summary>
        /// Writes a BSON Int32 element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The Int32 value.</param>
        public void WriteInt32(string name, int value)
        {
            WriteName(name);
            WriteInt32(value);
        }

        /// <summary>
        /// Writes a BSON Int64 to the writer.
        /// </summary>
        /// <param name="value">The Int64 value.</param>
        public abstract void WriteInt64(long value);

        /// <summary>
        /// Writes a BSON Int64 element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The Int64 value.</param>
        public void WriteInt64(string name, long value)
        {
            WriteName(name);
            WriteInt64(value);
        }

        /// <summary>
        /// Writes a BSON JavaScript to the writer.
        /// </summary>
        /// <param name="code">The JavaScript code.</param>
        public abstract void WriteJavaScript(string code);

        /// <summary>
        /// Writes a BSON JavaScript element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="code">The JavaScript code.</param>
        public void WriteJavaScript(string name, string code)
        {
            WriteName(name);
            WriteJavaScript(code);
        }

        /// <summary>
        /// Writes a BSON JavaScript to the writer (call WriteStartDocument to start writing the scope).
        /// </summary>
        /// <param name="code">The JavaScript code.</param>
        public abstract void WriteJavaScriptWithScope(string code);

        /// <summary>
        /// Writes a BSON JavaScript element to the writer (call WriteStartDocument to start writing the scope).
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="code">The JavaScript code.</param>
        public void WriteJavaScriptWithScope(string name, string code)
        {
            WriteName(name);
            WriteJavaScriptWithScope(code);
        }

        /// <summary>
        /// Writes a BSON MaxKey to the writer.
        /// </summary>
        public abstract void WriteMaxKey();

        /// <summary>
        /// Writes a BSON MaxKey element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public void WriteMaxKey(string name)
        {
            WriteName(name);
            WriteMaxKey();
        }

        /// <summary>
        /// Writes a BSON MinKey to the writer.
        /// </summary>
        public abstract void WriteMinKey();

        /// <summary>
        /// Writes a BSON MinKey element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public void WriteMinKey(string name)
        {
            WriteName(name);
            WriteMinKey();
        }

        /// <summary>
        /// Writes the name of an element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public virtual void WriteName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (name.IndexOf('\0') != -1)
            {
                throw new BsonSerializationException("Element names cannot contain nulls.");
            }
            if (_disposed) { throw new ObjectDisposedException(this.GetType().Name); }
            if (_state != BsonWriterState.Name)
            {
                ThrowInvalidState("WriteName", BsonWriterState.Name);
            }

            if (!_elementNameValidator.IsValidElementName(name))
            {
                var message = string.Format("Element name '{0}' is not valid'.", name);
                throw new BsonSerializationException(message);
            }
            _childElementNameValidatorFactory = () => _elementNameValidator.GetValidatorForChildContent(name);

            _name = name;
            _state = BsonWriterState.Value;
        }

        /// <summary>
        /// Writes a BSON null to the writer.
        /// </summary>
        public abstract void WriteNull();

        /// <summary>
        /// Writes a BSON null element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public void WriteNull(string name)
        {
            WriteName(name);
            WriteNull();
        }

        /// <summary>
        /// Writes a BSON ObjectId to the writer.
        /// </summary>
        /// <param name="objectId">The ObjectId.</param>
        public abstract void WriteObjectId(ObjectId objectId);

        /// <summary>
        /// Writes a BSON ObjectId to the writer.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        [Obsolete("Use WriteObjectId(ObjectId objectId) instead.")]
        public void WriteObjectId(int timestamp, int machine, short pid, int increment)
        {
            var objectId = new ObjectId(timestamp, machine, pid, increment);
            WriteObjectId(objectId);
        }

        /// <summary>
        /// Writes a BSON ObjectId element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="objectId">The ObjectId.</param>
        public void WriteObjectId(string name, ObjectId objectId)
        {
            WriteName(name);
            WriteObjectId(objectId);
        }

        /// <summary>
        /// Writes a BSON ObjectId element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        [Obsolete("Use WriteObjectId(string name, ObjectId objectId) instead.")]
        public void WriteObjectId(string name, int timestamp, int machine, short pid, int increment)
        {
            WriteName(name);
            WriteObjectId(timestamp, machine, pid, increment);
        }

        /// <summary>
        /// Writes a raw BSON array.
        /// </summary>
        /// <param name="slice">The byte buffer containing the raw BSON array.</param>
        public virtual void WriteRawBsonArray(IByteBuffer slice)
        {
            // overridden in BsonBinaryWriter to write the raw bytes to the stream
            // for all other streams, deserialize the raw bytes and serialize the resulting array instead

            var documentLength = slice.Length + 8;
            using (var memoryStream = new MemoryStream(documentLength))
            {
                // wrap the array in a fake document so we can deserialize it
                var streamWriter = new BsonStreamWriter(memoryStream, Utf8Encodings.Strict);
                streamWriter.WriteInt32(documentLength);
                streamWriter.WriteBsonType(BsonType.Array);
                streamWriter.WriteByte((byte)'x');
                streamWriter.WriteByte(0);
                slice.WriteTo(streamWriter.BaseStream);
                streamWriter.WriteByte(0);

                memoryStream.Position = 0;
                using (var bsonReader = new BsonBinaryReader(memoryStream, BsonBinaryReaderSettings.Defaults))
                {
                    var deserializationContext = BsonDeserializationContext.CreateRoot<BsonDocument>(bsonReader);
                    bsonReader.ReadStartDocument();
                    bsonReader.ReadName("x");
                    var array = deserializationContext.DeserializeWithChildContext(BsonArraySerializer.Instance);
                    bsonReader.ReadEndDocument();

                    var serializationContext = BsonSerializationContext.CreateRoot<BsonArray>(this);
                    BsonArraySerializer.Instance.Serialize(serializationContext, array);
                }
            }
        }

        /// <summary>
        /// Writes a raw BSON array.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="slice">The byte buffer containing the raw BSON array.</param>
        public void WriteRawBsonArray(string name, IByteBuffer slice)
        {
            WriteName(name);
            WriteRawBsonArray(slice);
        }

        /// <summary>
        /// Writes a raw BSON document.
        /// </summary>
        /// <param name="slice">The byte buffer containing the raw BSON document.</param>
        public virtual void WriteRawBsonDocument(IByteBuffer slice)
        {
            // overridden in BsonBinaryWriter to write the raw bytes to the stream
            // for all other streams, deserialize the raw bytes and serialize the resulting document instead

            using (var stream = new ByteBufferStream(slice, ownsByteBuffer: false))
            using (var bsonReader = new BsonBinaryReader(stream, BsonBinaryReaderSettings.Defaults))
            {
                var deserializationContext = BsonDeserializationContext.CreateRoot<BsonDocument>(bsonReader);
                var document = BsonDocumentSerializer.Instance.Deserialize(deserializationContext);

                var serializationContext = BsonSerializationContext.CreateRoot<BsonDocument>(this);
                BsonDocumentSerializer.Instance.Serialize(serializationContext, document);
            }
        }

        /// <summary>
        /// Writes a raw BSON document.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="slice">The byte buffer containing the raw BSON document.</param>
        public void WriteRawBsonDocument(string name, IByteBuffer slice)
        {
            WriteName(name);
            WriteRawBsonDocument(slice);
        }

        /// <summary>
        /// Writes a BSON regular expression to the writer.
        /// </summary>
        /// <param name="regex">A BsonRegularExpression.</param>
        public abstract void WriteRegularExpression(BsonRegularExpression regex);

        /// <summary>
        /// Writes a BSON regular expression to the writer.
        /// </summary>
        /// <param name="pattern">A regular expression pattern.</param>
        /// <param name="options">A regular expression options.</param>
        [Obsolete("Use WriteRegularExpression(BsonRegularExpression regex) instead.")]
        public void WriteRegularExpression(string pattern, string options)
        {
            var regex = new BsonRegularExpression(pattern, options);
            WriteRegularExpression(regex);
        }

        /// <summary>
        /// Writes a BSON regular expression element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="regex">A BsonRegularExpression.</param>
        public void WriteRegularExpression(string name, BsonRegularExpression regex)
        {
            WriteName(name);
            WriteRegularExpression(regex);
        }

        /// <summary>
        /// Writes a BSON regular expression element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="pattern">A regular expression pattern.</param>
        /// <param name="options">A regular expression options.</param>
        [Obsolete("Use WriteRegularExpression(string name, BsonRegularExpression regex) instead.")]
        public void WriteRegularExpression(string name, string pattern, string options)
        {
            WriteName(name);
            WriteRegularExpression(pattern, options);
        }

        /// <summary>
        /// Writes the start of a BSON array to the writer.
        /// </summary>
        public virtual void WriteStartArray()
        {
            _serializationDepth++;
            if (_serializationDepth > _settings.MaxSerializationDepth)
            {
                throw new BsonSerializationException("Maximum serialization depth exceeded (does the object being serialized have a circular reference?).");
            }
        }

        /// <summary>
        /// Writes the start of a BSON array element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public void WriteStartArray(string name)
        {
            WriteName(name);
            WriteStartArray();
        }

        /// <summary>
        /// Writes the start of a BSON document to the writer.
        /// </summary>
        public virtual void WriteStartDocument()
        {
            _serializationDepth++;
            if (_serializationDepth > _settings.MaxSerializationDepth)
            {
                throw new BsonSerializationException("Maximum serialization depth exceeded (does the object being serialized have a circular reference?).");
            }

            PushElementNameValidator(_childElementNameValidatorFactory());
        }

        /// <summary>
        /// Writes the start of a BSON document element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public void WriteStartDocument(string name)
        {
            WriteName(name);
            WriteStartDocument();
        }

        /// <summary>
        /// Writes a BSON String to the writer.
        /// </summary>
        /// <param name="value">The String value.</param>
        public abstract void WriteString(string value);

        /// <summary>
        /// Writes a BSON String element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The String value.</param>
        public void WriteString(string name, string value)
        {
            WriteName(name);
            WriteString(value);
        }

        /// <summary>
        /// Writes a BSON Symbol to the writer.
        /// </summary>
        /// <param name="value">The symbol.</param>
        public abstract void WriteSymbol(string value);

        /// <summary>
        /// Writes a BSON Symbol element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The symbol.</param>
        public void WriteSymbol(string name, string value)
        {
            WriteName(name);
            WriteSymbol(value);
        }

        /// <summary>
        /// Writes a BSON timestamp to the writer.
        /// </summary>
        /// <param name="value">The combined timestamp/increment value.</param>
        public abstract void WriteTimestamp(long value);

        /// <summary>
        /// Writes a BSON timestamp element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="value">The combined timestamp/increment value.</param>
        public void WriteTimestamp(string name, long value)
        {
            WriteName(name);
            WriteTimestamp(value);
        }

        /// <summary>
        /// Writes a BSON undefined to the writer.
        /// </summary>
        public abstract void WriteUndefined();

        /// <summary>
        /// Writes a BSON undefined element to the writer.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public void WriteUndefined(string name)
        {
            WriteName(name);
            WriteUndefined();
        }

        // protected methods
        /// <summary>
        /// Disposes of any resources used by the writer.
        /// </summary>
        /// <param name="disposing">True if called from Dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Throws an InvalidOperationException when the method called is not valid for the current ContextType.
        /// </summary>
        /// <param name="methodName">The name of the method.</param>
        /// <param name="actualContextType">The actual ContextType.</param>
        /// <param name="validContextTypes">The valid ContextTypes.</param>
        protected void ThrowInvalidContextType(
            string methodName,
            ContextType actualContextType,
            params ContextType[] validContextTypes)
        {
            var validContextTypesString = string.Join(" or ", validContextTypes.Select(c => c.ToString()).ToArray());
            var message = string.Format(
                "{0} can only be called when ContextType is {1}, not when ContextType is {2}.",
                methodName, validContextTypesString, actualContextType);
            throw new InvalidOperationException(message);
        }

        /// <summary>
        /// Throws an InvalidOperationException when the method called is not valid for the current state.
        /// </summary>
        /// <param name="methodName">The name of the method.</param>
        /// <param name="validStates">The valid states.</param>
        protected void ThrowInvalidState(string methodName, params BsonWriterState[] validStates)
        {
            string message;
            if (_state == BsonWriterState.Initial || _state == BsonWriterState.ScopeDocument || _state == BsonWriterState.Done)
            {
                if (!methodName.StartsWith("End", StringComparison.Ordinal) && methodName != "WriteName")
                {
                    var typeName = methodName.Substring(5);
                    if (typeName.StartsWith("Start", StringComparison.Ordinal))
                    {
                        typeName = typeName.Substring(5);
                    }
                    var article = "A";
                    if (new char[] { 'A', 'E', 'I', 'O', 'U' }.Contains(typeName[0]))
                    {
                        article = "An";
                    }
                    message = string.Format(
                        "{0} {1} value cannot be written to the root level of a BSON document.",
                        article, typeName);
                    throw new InvalidOperationException(message);
                }
            }

            var validStatesString = string.Join(" or ", validStates.Select(s => s.ToString()).ToArray());
            message = string.Format(
                "{0} can only be called when State is {1}, not when State is {2}",
                methodName, validStatesString, _state);
            throw new InvalidOperationException(message);
        }
    }
}
