/* Copyright 2010-2014 MongoDB Inc.
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
using System.IO;
using System.Linq;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// Represents a BSON reader for some external format (see subclasses).
    /// </summary>
    public abstract class BsonReader : IDisposable
    {
        // private fields
        private bool _disposed = false;
        private BsonReaderSettings _settings;
        private BsonReaderState _state;
        private BsonType _currentBsonType;
        private string _currentName;

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonReader class.
        /// </summary>
        /// <param name="settings">The reader settings.</param>
        protected BsonReader(BsonReaderSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            _settings = settings.FrozenCopy();
            _state = BsonReaderState.Initial;
        }

        // public properties
        /// <summary>
        /// Gets the current BsonType.
        /// </summary>
        public BsonType CurrentBsonType
        {
            get { return _currentBsonType; }
            protected set { _currentBsonType = value; }
        }

        /// <summary>
        /// Gets the settings of the reader.
        /// </summary>
        public BsonReaderSettings Settings
        {
            get { return _settings; }
        }

        /// <summary>
        /// Gets the current state of the reader.
        /// </summary>
        public BsonReaderState State
        {
            get { return _state; }
            protected set { _state = value; }
        }

        // protected properties
        /// <summary>
        /// Gets the current name.
        /// </summary>
        protected string CurrentName
        {
            get { return _currentName; }
            set { _currentName = value; }
        }

        /// <summary>
        /// Gets whether the BsonReader has been disposed.
        /// </summary>
        protected bool Disposed
        {
            get { return _disposed; }
        }

        // public methods
        /// <summary>
        /// Closes the reader.
        /// </summary>
        public abstract void Close();

        /// <summary>
        /// Disposes of any resources used by the reader.
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
        /// Positions the reader to an element by name.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>True if the element was found.</returns>
        public bool FindElement(string name)
        {
            if (_disposed) { ThrowObjectDisposedException(); }
            if (_state != BsonReaderState.Type)
            {
                ThrowInvalidState("FindElement", BsonReaderState.Type);
            }

            while (ReadBsonType() != BsonType.EndOfDocument)
            {
                var elementName = ReadName();
                if (elementName == name)
                {
                    return true;
                }
                SkipValue();
            }

            return false;
        }

        /// <summary>
        /// Positions the reader to a string element by name.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>True if the element was found.</returns>
        public string FindStringElement(string name)
        {
            if (_disposed) { ThrowObjectDisposedException(); }
            if (_state != BsonReaderState.Type)
            {
                ThrowInvalidState("FindStringElement", BsonReaderState.Type);
            }

            BsonType bsonType;
            while ((bsonType = ReadBsonType()) != BsonType.EndOfDocument)
            {
                var elementName = ReadName();
                if (bsonType == BsonType.String && elementName == name)
                {
                    return ReadString();
                }
                else
                {
                    SkipValue();
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a bookmark to the reader's current position and state.
        /// </summary>
        /// <returns>A bookmark.</returns>
        public abstract BsonReaderBookmark GetBookmark();

        /// <summary>
        /// Gets the current BsonType (calls ReadBsonType if necessary).
        /// </summary>
        /// <returns>The current BsonType.</returns>
        public BsonType GetCurrentBsonType()
        {
            if (_state == BsonReaderState.Initial || _state == BsonReaderState.Done || _state == BsonReaderState.ScopeDocument || _state == BsonReaderState.Type)
            {
                ReadBsonType();
            }
            if (_state != BsonReaderState.Value)
            {
                ThrowInvalidState("GetCurrentBsonType", BsonReaderState.Value);
            }
            return _currentBsonType;
        }

        /// <summary>
        /// Determines whether this reader is at end of file.
        /// </summary>
        /// <returns>
        /// Whether this reader is at end of file.
        /// </returns>
        public abstract bool IsAtEndOfFile();

        /// <summary>
        /// Reads BSON binary data from the reader.
        /// </summary>
        /// <returns>A BsonBinaryData.</returns>
        public abstract BsonBinaryData ReadBinaryData();

        /// <summary>
        /// Reads BSON binary data from the reader.
        /// </summary>
        /// <param name="bytes">The binary data.</param>
        /// <param name="subType">The binary data subtype.</param>
        [Obsolete("Use ReadBinaryData() instead.")]
        public void ReadBinaryData(out byte[] bytes, out BsonBinarySubType subType)
        {
            GuidRepresentation guidRepresentation;
            ReadBinaryData(out bytes, out subType, out guidRepresentation);
        }

        /// <summary>
        /// Reads BSON binary data from the reader.
        /// </summary>
        /// <param name="bytes">The binary data.</param>
        /// <param name="subType">The binary data subtype.</param>
        /// <param name="guidRepresentation">The representation for Guids.</param>
        [Obsolete("Use ReadBinaryData() instead.")]
        public void ReadBinaryData(
            out byte[] bytes,
            out BsonBinarySubType subType,
            out GuidRepresentation guidRepresentation)
        {
            var binaryData = ReadBinaryData();
            bytes = binaryData.Bytes;
            subType = binaryData.SubType;
            guidRepresentation = binaryData.GuidRepresentation;
        }

        /// <summary>
        /// Reads a BSON binary data element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>A BsonBinaryData.</returns>
        public BsonBinaryData ReadBinaryData(string name)
        {
            VerifyName(name);
            return ReadBinaryData();
        }

        /// <summary>
        /// Reads a BSON binary data element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="bytes">The binary data.</param>
        /// <param name="subType">The binary data subtype.</param>
        [Obsolete("Use ReadBinaryData(string name) instead.")]
        public void ReadBinaryData(string name, out byte[] bytes, out BsonBinarySubType subType)
        {
            GuidRepresentation guidRepresentation;
            ReadBinaryData(name, out bytes, out subType, out guidRepresentation);
        }

        /// <summary>
        /// Reads a BSON binary data element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="bytes">The binary data.</param>
        /// <param name="subType">The binary data subtype.</param>
        /// <param name="guidRepresentation">The representation for Guids.</param>
        [Obsolete("Use ReadBinaryData(string name) instead.")]
        public void ReadBinaryData(
            string name,
            out byte[] bytes,
            out BsonBinarySubType subType,
            out GuidRepresentation guidRepresentation)
        {
            VerifyName(name);
            ReadBinaryData(out bytes, out subType, out guidRepresentation);
        }

        /// <summary>
        /// Reads a BSON boolean from the reader.
        /// </summary>
        /// <returns>A Boolean.</returns>
        public abstract bool ReadBoolean();

        /// <summary>
        /// Reads a BSON boolean element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>A Boolean.</returns>
        public bool ReadBoolean(string name)
        {
            VerifyName(name);
            return ReadBoolean();
        }

        /// <summary>
        /// Reads a BsonType from the reader.
        /// </summary>
        /// <returns>A BsonType.</returns>
        public abstract BsonType ReadBsonType();

        /// <summary>
        /// Reads BSON binary data from the reader.
        /// </summary>
        /// <returns>A byte array.</returns>
        public abstract byte[] ReadBytes();

        /// <summary>
        /// Reads a BSON binary data element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>A byte array.</returns>
        public byte[] ReadBytes(string name)
        {
            VerifyName(name);
            return ReadBytes();
        }

        /// <summary>
        /// Reads a BSON DateTime from the reader.
        /// </summary>
        /// <returns>The number of milliseconds since the Unix epoch.</returns>
        public abstract long ReadDateTime();

        /// <summary>
        /// Reads a BSON DateTime element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>The number of milliseconds since the Unix epoch.</returns>
        public long ReadDateTime(string name)
        {
            VerifyName(name);
            return ReadDateTime();
        }

        /// <summary>
        /// Reads a BSON Double from the reader.
        /// </summary>
        /// <returns>A Double.</returns>
        public abstract double ReadDouble();

        /// <summary>
        /// Reads a BSON Double element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>A Double.</returns>
        public double ReadDouble(string name)
        {
            VerifyName(name);
            return ReadDouble();
        }

        /// <summary>
        /// Reads the end of a BSON array from the reader.
        /// </summary>
        public abstract void ReadEndArray();

        /// <summary>
        /// Reads the end of a BSON document from the reader.
        /// </summary>
        public abstract void ReadEndDocument();

        /// <summary>
        /// Reads a BSON Int32 from the reader.
        /// </summary>
        /// <returns>An Int32.</returns>
        public abstract int ReadInt32();

        /// <summary>
        /// Reads a BSON Int32 element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>An Int32.</returns>
        public int ReadInt32(string name)
        {
            VerifyName(name);
            return ReadInt32();
        }

        /// <summary>
        /// Reads a BSON Int64 from the reader.
        /// </summary>
        /// <returns>An Int64.</returns>
        public abstract long ReadInt64();

        /// <summary>
        /// Reads a BSON Int64 element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>An Int64.</returns>
        public long ReadInt64(string name)
        {
            VerifyName(name);
            return ReadInt64();
        }

        /// <summary>
        /// Reads a BSON JavaScript from the reader.
        /// </summary>
        /// <returns>A string.</returns>
        public abstract string ReadJavaScript();

        /// <summary>
        /// Reads a BSON JavaScript element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>A string.</returns>
        public string ReadJavaScript(string name)
        {
            VerifyName(name);
            return ReadJavaScript();
        }

        /// <summary>
        /// Reads a BSON JavaScript with scope from the reader (call ReadStartDocument next to read the scope).
        /// </summary>
        /// <returns>A string.</returns>
        public abstract string ReadJavaScriptWithScope();

        /// <summary>
        /// Reads a BSON JavaScript with scope element from the reader (call ReadStartDocument next to read the scope).
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>A string.</returns>
        public string ReadJavaScriptWithScope(string name)
        {
            VerifyName(name);
            return ReadJavaScriptWithScope();
        }

        /// <summary>
        /// Reads a BSON MaxKey from the reader.
        /// </summary>
        public abstract void ReadMaxKey();

        /// <summary>
        /// Reads a BSON MaxKey element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public void ReadMaxKey(string name)
        {
            VerifyName(name);
            ReadMaxKey();
        }

        /// <summary>
        /// Reads a BSON MinKey from the reader.
        /// </summary>
        public abstract void ReadMinKey();

        /// <summary>
        /// Reads a BSON MinKey element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public void ReadMinKey(string name)
        {
            VerifyName(name);
            ReadMinKey();
        }

        /// <summary>
        /// Reads the name of an element from the reader.
        /// </summary>
        /// <returns>The name of the element.</returns>
        public virtual string ReadName()
        {
            return ReadName(Utf8NameDecoder.Instance);
        }

        /// <summary>
        /// Reads the name of an element from the reader (using the provided name decoder).
        /// </summary>
        /// <param name="nameDecoder">The name decoder.</param>
        /// <returns>
        /// The name of the element.
        /// </returns>
        public abstract string ReadName(INameDecoder nameDecoder);

        /// <summary>
        /// Reads the name of an element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public void ReadName(string name)
        {
            VerifyName(name);
        }

        /// <summary>
        /// Reads a BSON null from the reader.
        /// </summary>
        public abstract void ReadNull();

        /// <summary>
        /// Reads a BSON null element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public void ReadNull(string name)
        {
            VerifyName(name);
            ReadNull();
        }

        /// <summary>
        /// Reads a BSON ObjectId from the reader.
        /// </summary>
        /// <returns>An ObjectId.</returns>
        public abstract ObjectId ReadObjectId();

        /// <summary>
        /// Reads a BSON ObjectId from the reader.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        [Obsolete("Use ReadObjectId() instead.")]
        public void ReadObjectId(out int timestamp, out int machine, out short pid, out int increment)
        {
            var objectId = ReadObjectId();
            timestamp = objectId.Timestamp;
            machine = objectId.Machine;
            pid = objectId.Pid;
            increment = objectId.Increment;
        }

        /// <summary>
        /// Reads a BSON ObjectId element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>An ObjectId.</returns>
        public ObjectId ReadObjectId(string name)
        {
            VerifyName(name);
            return ReadObjectId();
        }

        /// <summary>
        /// Reads a BSON ObjectId element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="machine">The machine hash.</param>
        /// <param name="pid">The PID.</param>
        /// <param name="increment">The increment.</param>
        [Obsolete("Use ReadObjectId(string name) instead.")]
        public void ReadObjectId(string name, out int timestamp, out int machine, out short pid, out int increment)
        {
            VerifyName(name);
            ReadObjectId(out timestamp, out machine, out pid, out increment);
        }

        /// <summary>
        /// Reads a raw BSON array.
        /// </summary>
        /// <returns>The raw BSON array.</returns>
        public virtual IByteBuffer ReadRawBsonArray()
        {
            // overridden in BsonBinaryReader to read the raw bytes from the stream
            // for all other streams, deserialize the array and reserialize it using a BsonBinaryWriter to get the raw bytes

            var deserializationContext = BsonDeserializationContext.CreateRoot<BsonArray>(this);
            var array = BsonArraySerializer.Instance.Deserialize(deserializationContext);

            using (var memoryStream = new MemoryStream())
            using (var bsonWriter = new BsonBinaryWriter(memoryStream, BsonBinaryWriterSettings.Defaults))
            {
                var serializationContext = BsonSerializationContext.CreateRoot<BsonDocument>(bsonWriter);
                bsonWriter.WriteStartDocument();
                var startPosition = memoryStream.Position + 3; // just past BsonType, "x" and null byte
                bsonWriter.WriteName("x");
                serializationContext.SerializeWithChildContext(BsonArraySerializer.Instance, array);
                var endPosition = memoryStream.Position;
                bsonWriter.WriteEndDocument();

                var bytes = memoryStream.ToArray();
                return new ByteArrayBuffer(bytes, (int)startPosition, (int)(endPosition - startPosition), true);
            }
        }

        /// <summary>
        /// Reads a raw BSON array.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>
        /// The raw BSON array.
        /// </returns>
        public IByteBuffer ReadRawBsonArray(string name)
        {
            VerifyName(name);
            return ReadRawBsonArray();
        }

        /// <summary>
        /// Reads a raw BSON document.
        /// </summary>
        /// <returns>The raw BSON document.</returns>
        public virtual IByteBuffer ReadRawBsonDocument()
        {
            // overridden in BsonBinaryReader to read the raw bytes from the stream
            // for all other streams, deserialize the document and use ToBson to get the raw bytes

            var deserializationContext = BsonDeserializationContext.CreateRoot<BsonDocument>(this);
            var document = BsonDocumentSerializer.Instance.Deserialize(deserializationContext);
            var bytes = document.ToBson();
            return new ByteArrayBuffer(bytes, 0, bytes.Length, true);
        }

        /// <summary>
        /// Reads a raw BSON document.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The raw BSON document.</returns>
        public IByteBuffer ReadRawBsonDocument(string name)
        {
            VerifyName(name);
            return ReadRawBsonDocument();
        }

        /// <summary>
        /// Reads a BSON regular expression from the reader.
        /// </summary>
        /// <returns>A BsonRegularExpression.</returns>
        public abstract BsonRegularExpression ReadRegularExpression();

        /// <summary>
        /// Reads a BSON regular expression from the reader.
        /// </summary>
        /// <param name="pattern">A regular expression pattern.</param>
        /// <param name="options">A regular expression options.</param>
        [Obsolete("Use ReadRegularExpression() instead.")]
        public void ReadRegularExpression(out string pattern, out string options)
        {
            var regex = ReadRegularExpression();
            pattern = regex.Pattern;
            options = regex.Options;
        }

        /// <summary>
        /// Reads a BSON regular expression element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>A BsonRegularExpression.</returns>
        public BsonRegularExpression ReadRegularExpression(string name)
        {
            VerifyName(name);
            return ReadRegularExpression();
        }

        /// <summary>
        /// Reads a BSON regular expression element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <param name="pattern">A regular expression pattern.</param>
        /// <param name="options">A regular expression options.</param>
        [Obsolete("Use ReadRegularExpression(string name) instead.")]
        public void ReadRegularExpression(string name, out string pattern, out string options)
        {
            VerifyName(name);
            ReadRegularExpression(out pattern, out options);
        }

        /// <summary>
        /// Reads the start of a BSON array.
        /// </summary>
        public abstract void ReadStartArray();

        /// <summary>
        /// Reads the start of a BSON document.
        /// </summary>
        public abstract void ReadStartDocument();

        /// <summary>
        /// Reads a BSON string from the reader.
        /// </summary>
        /// <returns>A String.</returns>
        public abstract string ReadString();

        /// <summary>
        /// Reads a BSON string element from the reader.
        /// </summary>
        /// <returns>A String.</returns>
        /// <param name="name">The name of the element.</param>
        public string ReadString(string name)
        {
            VerifyName(name);
            return ReadString();
        }

        /// <summary>
        /// Reads a BSON symbol from the reader.
        /// </summary>
        /// <returns>A string.</returns>
        public abstract string ReadSymbol();

        /// <summary>
        /// Reads a BSON symbol element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        /// <returns>A string.</returns>
        public string ReadSymbol(string name)
        {
            VerifyName(name);
            return ReadSymbol();
        }

        /// <summary>
        /// Reads a BSON timestamp from the reader.
        /// </summary>
        /// <returns>The combined timestamp/increment.</returns>
        public abstract long ReadTimestamp();

        /// <summary>
        /// Reads a BSON timestamp element from the reader.
        /// </summary>
        /// <returns>The combined timestamp/increment.</returns>
        /// <param name="name">The name of the element.</param>
        public long ReadTimestamp(string name)
        {
            VerifyName(name);
            return ReadTimestamp();
        }

        /// <summary>
        /// Reads a BSON undefined from the reader.
        /// </summary>
        public abstract void ReadUndefined();

        /// <summary>
        /// Reads a BSON undefined element from the reader.
        /// </summary>
        /// <param name="name">The name of the element.</param>
        public void ReadUndefined(string name)
        {
            VerifyName(name);
            ReadUndefined();
        }

        /// <summary>
        /// Returns the reader to previously bookmarked position and state.
        /// </summary>
        /// <param name="bookmark">The bookmark.</param>
        public abstract void ReturnToBookmark(BsonReaderBookmark bookmark);

        /// <summary>
        /// Skips the name (reader must be positioned on a name).
        /// </summary>
        public abstract void SkipName();

        /// <summary>
        /// Skips the value (reader must be positioned on a value).
        /// </summary>
        public abstract void SkipValue();

        // protected methods
        /// <summary>
        /// Disposes of any resources used by the reader.
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
        protected void ThrowInvalidState(string methodName, params BsonReaderState[] validStates)
        {
            var validStatesString = string.Join(" or ", validStates.Select(s => s.ToString()).ToArray());
            var message = string.Format(
                "{0} can only be called when State is {1}, not when State is {2}.",
                methodName, validStatesString, _state);
            throw new InvalidOperationException(message);
        }

        /// <summary>
        /// Throws an ObjectDisposedException.
        /// </summary>
        protected void ThrowObjectDisposedException()
        {
            throw new ObjectDisposedException(this.GetType().Name);
        }

        /// <summary>
        /// Verifies the current state and BsonType of the reader.
        /// </summary>
        /// <param name="methodName">The name of the method calling this one.</param>
        /// <param name="requiredBsonType">The required BSON type.</param>
        protected void VerifyBsonType(string methodName, BsonType requiredBsonType)
        {
            if (_state == BsonReaderState.Initial || _state == BsonReaderState.ScopeDocument || _state == BsonReaderState.Type)
            {
                ReadBsonType();
            }
            if (_state == BsonReaderState.Name)
            {
                // ignore name
                SkipName();
            }
            if (_state != BsonReaderState.Value)
            {
                ThrowInvalidState(methodName, BsonReaderState.Value);
            }
            if (_currentBsonType != requiredBsonType)
            {
                var message = string.Format(
                    "{0} can only be called when CurrentBsonType is {1}, not when CurrentBsonType is {2}.",
                    methodName, requiredBsonType, _currentBsonType);
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Verifies the name of the current element.
        /// </summary>
        /// <param name="expectedName">The expected name.</param>
        protected void VerifyName(string expectedName)
        {
            var actualName = ReadName();
            if (actualName != expectedName)
            {
                var message = string.Format(
                    "Expected element name to be '{0}', not '{1}'.",
                    expectedName, actualName);
                throw new FileFormatException(message);
            }
        }
    }
}
