/* Copyright 2010-present MongoDB Inc.
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
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Bson.IO;

internal sealed class ReadOnlyMemoryBsonReader : BsonReader
{
    private static readonly BsonReaderState[] __stateMap;

    private readonly IByteBufferSlicer _byteBufferSlicer;
    private readonly ReadOnlyMemory<byte> _memory;
    private int _position;

    private BsonBinaryReaderContext _context;
    private readonly Stack<BsonBinaryReaderContext> _contextStack = new(4);

    static ReadOnlyMemoryBsonReader()
    {
        var count = Enum.GetValues(typeof(ContextType)).Length;
        __stateMap = Enumerable.Repeat(BsonReaderState.Closed, count).ToArray();
        __stateMap[(int)ContextType.Array] = BsonReaderState.Type;
        __stateMap[(int)ContextType.Document] = BsonReaderState.Type;
        __stateMap[(int)ContextType.ScopeDocument] = BsonReaderState.Type;
        __stateMap[(int)ContextType.TopLevel] = BsonReaderState.Initial;
    }

    public ReadOnlyMemoryBsonReader(ReadOnlyMemory<byte> memory)
        : this(memory, ReadOnlyMemoryReaderSettings.Defaults)
    {
    }

    public ReadOnlyMemoryBsonReader(ReadOnlyMemory<byte> memory, ReadOnlyMemoryReaderSettings settings)
        : this(memory, new ReadOnlyMemorySlicer(memory), settings)
    {
    }

    public ReadOnlyMemoryBsonReader(ReadOnlyMemory<byte> memory, IByteBufferSlicer byteBufferSlicer, ReadOnlyMemoryReaderSettings settings)
        : base(settings)
    {
        if (byteBufferSlicer == null)
        {
            throw new ArgumentNullException(nameof(byteBufferSlicer));
        }

        _memory = memory;
        _byteBufferSlicer = byteBufferSlicer;
        _position = 0;

        _context = new BsonBinaryReaderContext(ContextType.TopLevel, 0, 0);
    }

    /// <summary>
    /// Gets or sets the current position within the BSON data.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the assigned value is less than 0 or greater than the length of the BSON data.
    /// </exception>
    public int Position
    {
        get => _position;
        set
        {
            if (value < 0 || value > _memory.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"Valid range is [{0}..{_memory.Length}]");
            }

            _position = value;
        }
    }

    /// <summary>
    /// Gets the settings of the reader.
    /// </summary>
    public new ReadOnlyMemoryReaderSettings Settings => (ReadOnlyMemoryReaderSettings)base.Settings;

    /// <inheritdoc />
    public override void Close()
    {
        // Close can be called on Disposed objects
        State = BsonReaderState.Closed;
    }

    /// <inheritdoc />
    public override BsonReaderBookmark GetBookmark() =>
        new BsonBinaryReaderBookmark(State, CurrentBsonType, CurrentName, _context, _contextStack, _position);

    /// <inheritdoc />
    public override bool IsAtEndOfFile() => _position >= _memory.Length;

    /// <inheritdoc />
#pragma warning disable 618 // about obsolete BsonBinarySubType.OldBinary
    public override BsonBinaryData ReadBinaryData()
    {
        VerifyBsonTypeAndSetNextState(BsonType.Binary);

        var dataSize = ReadSize();
        var totalSize = dataSize + 1; // data + subtype
        var span = _memory.Span.Slice(_position, totalSize);
        _position += totalSize;

        var subType = (BsonBinarySubType)span[0];
        if (subType == BsonBinarySubType.OldBinary)
        {
            // sub type OldBinary has two sizes (for historical reasons)
            int dataSize2 = ReadSize();
            if (dataSize2 != dataSize - 4)
            {
                throw new FormatException("Binary sub type OldBinary has inconsistent sizes");
            }
            dataSize = dataSize2;

            if (Settings.FixOldBinarySubTypeOnInput)
            {
                subType = BsonBinarySubType.Binary; // replace obsolete OldBinary with new Binary sub type
            }
        }

        var bytes = span.Slice(1, dataSize).ToArray();

        if ((subType == BsonBinarySubType.UuidStandard || subType == BsonBinarySubType.UuidLegacy) &&
            bytes.Length != 16)
        {
            throw new FormatException($"Length must be 16, not {bytes.Length}, when subType is {subType}.");
        }

        return new BsonBinaryData(bytes, subType);
    }
#pragma warning restore 618

    /// <inheritdoc />
    public override bool ReadBoolean()
    {
        VerifyBsonTypeAndSetNextState(BsonType.Boolean);

        var b = _memory.Span[_position++];

        return b switch
        {
            0 => false,
            1 => true,
            _ => throw new FormatException($"Invalid BsonBoolean value: {b}.")
        };
    }

    /// <inheritdoc />
    public override BsonType ReadBsonType()
    {
        var state = State;

        if (state is BsonReaderState.Initial or BsonReaderState.ScopeDocument)
        {
            // there is an implied type of Document for the top level and for scope documents
            CurrentBsonType = BsonType.Document;
            State = BsonReaderState.Value;
            return CurrentBsonType;
        }
        if (state != BsonReaderState.Type)
        {
            ThrowInvalidState(nameof(ReadBsonType), BsonReaderState.Type);
        }

        if (_context.ContextType == ContextType.Array)
        {
            _context.ArrayIndex++;
        }

        CurrentBsonType = (BsonType)_memory.Span[_position++];

        if (!BsonStreamExtensions.IsValidBsonType(CurrentBsonType))
        {
            var dottedElementName = BsonBinaryReaderUtils.GenerateDottedElementName(_context, _contextStack.ToArray(), () => ReadCStringFromMemory(Utf8Encodings.Lenient));
            throw new FormatException($"Detected unknown BSON type \"\\x{(int)CurrentBsonType:x2}\" for fieldname \"{dottedElementName}\". Are you using the latest driver version?");
        }

        if (CurrentBsonType == BsonType.EndOfDocument)
        {
            switch (_context.ContextType)
            {
                case ContextType.Array:
                    State = BsonReaderState.EndOfArray;
                    return BsonType.EndOfDocument;
                case ContextType.Document:
                case ContextType.ScopeDocument:
                    State = BsonReaderState.EndOfDocument;
                    return BsonType.EndOfDocument;
                default:
                    throw new FormatException($"BsonType EndOfDocument is not valid when ContextType is {_context.ContextType}.");
            }
        }

        switch (_context.ContextType)
        {
            case ContextType.Array:
                SkipCString();
                State = BsonReaderState.Value;
                break;
            case ContextType.Document:
            case ContextType.ScopeDocument:
                State = BsonReaderState.Name;
                break;
            default:
                throw new BsonInternalException("Unexpected ContextType.");
        }

        return CurrentBsonType;
    }

    private void SkipCString()
    {
        var offset = _memory.Span.Slice(_position).IndexOf((byte)0);
        _position += offset + 1;
    }

    /// <inheritdoc />
    public override byte[] ReadBytes()
    {
        VerifyBsonTypeAndSetNextState(BsonType.Binary);

        var size = ReadSize();
        var subType = (BsonBinarySubType)_memory.Span[_position++];

#pragma warning disable 618
        if (subType != BsonBinarySubType.Binary && subType != BsonBinarySubType.OldBinary)
        {
            throw new FormatException($"{nameof(ReadBytes)} requires the binary sub type to be Binary, not {subType}.");
        }
#pragma warning restore 618

        var result = _memory.Span.Slice(_position, size).ToArray();
        _position += size;

        return result;
    }

    /// <inheritdoc />
    public override long ReadDateTime()
    {
        VerifyBsonTypeAndSetNextState(BsonType.DateTime);

        var value = ReadInt64FromMemory();

        if (value == BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch + 1)
        {
            if (Settings.FixOldDateTimeMaxValueOnInput)
            {
                value = BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch;
            }
        }
        return value;
    }

    /// <inheritdoc />
    public override Decimal128 ReadDecimal128()
    {
        VerifyBsonTypeAndSetNextState(BsonType.Decimal128);

        var lowBits = (ulong)ReadInt64FromMemory();
        var highBits = (ulong)ReadInt64FromMemory();
        return Decimal128.FromIEEEBits(highBits, lowBits);
    }

    /// <inheritdoc />
    public override double ReadDouble()
    {
        VerifyBsonTypeAndSetNextState(BsonType.Double);

        var result = BinaryPrimitivesCompat.ReadDoubleLittleEndian(_memory.Span.Slice(_position, 8));
        _position += 8;

        return result;
    }

    /// <inheritdoc />
    public override void ReadEndArray()
    {
        if (_context.ContextType != ContextType.Array)
        {
            ThrowInvalidContextType(nameof(ReadEndArray), _context.ContextType, ContextType.Array);
        }
        if (State == BsonReaderState.Type)
        {
            ReadBsonType(); // will set state to EndOfArray if at end of array
        }
        if (State != BsonReaderState.EndOfArray)
        {
            ThrowInvalidState(nameof(ReadEndArray), BsonReaderState.EndOfArray);
        }

        PopContext();

        switch (_context.ContextType)
        {
            case ContextType.Array: State = BsonReaderState.Type; break;
            case ContextType.Document: State = BsonReaderState.Type; break;
            case ContextType.TopLevel: State = BsonReaderState.Initial; break;
            default: throw new BsonInternalException("Unexpected ContextType.");
        }
    }

    /// <inheritdoc />
    public override void ReadEndDocument()
    {
        if (_context.ContextType != ContextType.Document && _context.ContextType != ContextType.ScopeDocument)
        {
            ThrowInvalidContextType(nameof(ReadEndDocument), _context.ContextType, ContextType.Document, ContextType.ScopeDocument);
        }
        if (State == BsonReaderState.Type)
        {
            ReadBsonType(); // will set state to EndOfDocument if at end of document
        }
        if (State != BsonReaderState.EndOfDocument)
        {
            ThrowInvalidState(nameof(ReadEndDocument), BsonReaderState.EndOfDocument);
        }

        PopContext();
        if (_context.ContextType == ContextType.JavaScriptWithScope)
        {
            PopContext(); // JavaScriptWithScope
        }
        switch (_context.ContextType)
        {
            case ContextType.Array: State = BsonReaderState.Type; break;
            case ContextType.Document: State = BsonReaderState.Type; break;
            case ContextType.TopLevel: State = BsonReaderState.Initial; break;
            default: throw new BsonInternalException("Unexpected ContextType.");
        }
    }

    /// <inheritdoc />
    public override int ReadInt32()
    {
        VerifyBsonTypeAndSetNextState(BsonType.Int32);

        return ReadInt32FromMemory();
    }

    /// <inheritdoc />
    public override long ReadInt64()
    {
        VerifyBsonTypeAndSetNextState(BsonType.Int64);

        return ReadInt64FromMemory();
    }

    /// <inheritdoc />
    public override string ReadJavaScript()
    {
        VerifyBsonTypeAndSetNextState(BsonType.JavaScript);

        return ReadStringFromMemory();
    }

    /// <inheritdoc />
    public override string ReadJavaScriptWithScope()
    {
        VerifyBsonType(BsonType.JavaScriptWithScope);

        var startPosition = _position;
        var size = ReadSize();

        PushContext(new(ContextType.JavaScriptWithScope, startPosition, size));

        var code = ReadStringFromMemory();

        State = BsonReaderState.ScopeDocument;
        return code;
    }

    /// <inheritdoc />
    public override void ReadMaxKey() =>
        VerifyBsonTypeAndSetNextState(BsonType.MaxKey);

    /// <inheritdoc />
    public override void ReadMinKey() =>
        VerifyBsonTypeAndSetNextState(BsonType.MinKey);

    /// <inheritdoc />
    public override string ReadName(INameDecoder nameDecoder)
    {
        if (State == BsonReaderState.Type)
        {
            ReadBsonType();
        }
        if (State != BsonReaderState.Name)
        {
            ThrowInvalidState(nameof(ReadName), BsonReaderState.Name);
        }

        var span = _memory.Span.Slice(_position);
        var nameEndIndex = span.IndexOf((byte)0);
        var nameSpan = span.Slice(0, nameEndIndex);

        if (nameDecoder is INameDecoderInternal nameDecoderInternal)
        {
            CurrentName = nameDecoderInternal.Decode(nameSpan, Settings.Encoding);
        }
        else
        {
            throw new Exception($"Name decoder {nameDecoder.GetType()} is not supported");
        }

        _position += nameEndIndex + 1;

        State = BsonReaderState.Value;

        if (_context.ContextType == ContextType.Document)
        {
            _context.ElementName = CurrentName;
        }

        return CurrentName;
    }

    /// <inheritdoc />
    public override void ReadNull() =>
        VerifyBsonTypeAndSetNextState(BsonType.Null);

    /// <inheritdoc />
    public override ObjectId ReadObjectId()
    {
        VerifyBsonTypeAndSetNextState(BsonType.ObjectId);

        var result = new ObjectId(_memory.Span.Slice(_position, 12));
        _position += 12;

        return result;
    }

    /// <summary>
    /// Reads a raw BSON array.
    /// </summary>
    /// <returns>
    /// The raw BSON array.
    /// </returns>
    public override IByteBuffer ReadRawBsonArray()
    {
        VerifyBsonType(BsonType.Array);

        var slice = ReadSliceFromMemory();

        switch (_context.ContextType)
        {
            case ContextType.Array: State = BsonReaderState.Type; break;
            case ContextType.Document: State = BsonReaderState.Type; break;
            case ContextType.TopLevel: State = BsonReaderState.Initial; break;
            default: throw new BsonInternalException("Unexpected ContextType.");
        }

        return slice;
    }

    /// <summary>
    /// Reads a raw BSON document.
    /// </summary>
    /// <returns>
    /// The raw BSON document.
    /// </returns>
    public override IByteBuffer ReadRawBsonDocument()
    {
        VerifyBsonType(BsonType.Document);

        var slice = ReadSliceFromMemory();

        if (_context.ContextType == ContextType.JavaScriptWithScope)
        {
            PopContext(); // JavaScriptWithScope
        }
        switch (_context.ContextType)
        {
            case ContextType.Array: State = BsonReaderState.Type; break;
            case ContextType.Document: State = BsonReaderState.Type; break;
            case ContextType.TopLevel: State = BsonReaderState.Initial; break;
            default: throw new BsonInternalException("Unexpected ContextType.");
        }

        return slice;
    }

    /// <inheritdoc />
    public override BsonRegularExpression ReadRegularExpression()
    {
        VerifyBsonTypeAndSetNextState(BsonType.RegularExpression);

        var pattern = ReadCStringFromMemory();
        var options = ReadCStringFromMemory();
        return new(pattern, options);
    }

    /// <inheritdoc />
    public override void ReadStartArray()
    {
        VerifyBsonType(BsonType.Array);

        var startPosition = _position;
        var size = ReadSize();

        PushContext(new(ContextType.Array, startPosition, size));

        State = BsonReaderState.Type;
    }

    /// <inheritdoc />
    public override void ReadStartDocument()
    {
        VerifyBsonType(BsonType.Document);

        var contextType = (State == BsonReaderState.ScopeDocument) ? ContextType.ScopeDocument : ContextType.Document;
        var startPosition = _position;
        var size = ReadSize();

        PushContext(new(contextType, startPosition, size));

        State = BsonReaderState.Type;
    }

    /// <inheritdoc />
    public override string ReadString()
    {
        VerifyBsonTypeAndSetNextState(BsonType.String);

        return ReadStringFromMemory();
    }

    /// <inheritdoc />
    public override string ReadSymbol()
    {
        VerifyBsonTypeAndSetNextState(BsonType.Symbol);

        return ReadStringFromMemory();
    }

    /// <inheritdoc />
    public override long ReadTimestamp()
    {
        VerifyBsonTypeAndSetNextState(BsonType.Timestamp);

        return ReadInt64FromMemory();
    }

    /// <inheritdoc />
    public override void ReadUndefined()
    {
        VerifyBsonTypeAndSetNextState(BsonType.Undefined);
    }

    /// <inheritdoc />
    public override void ReturnToBookmark(BsonReaderBookmark bookmark)
    {
        var binaryReaderBookmark = (BsonBinaryReaderBookmark)bookmark;
        State = binaryReaderBookmark.State;
        CurrentBsonType = binaryReaderBookmark.CurrentBsonType;
        CurrentName = binaryReaderBookmark.CurrentName;
        _context = binaryReaderBookmark.RestoreContext(_contextStack);
        _position = checked((int)binaryReaderBookmark.Position);
    }

    /// <inheritdoc />
    public override void SkipName()
    {
        if (State != BsonReaderState.Name)
        {
            ThrowInvalidState(nameof(SkipName), BsonReaderState.Name);
        }

        SkipCString();

        CurrentName = null;
        State = BsonReaderState.Value;

        if (_context.ContextType == ContextType.Document)
        {
            _context.ElementName = CurrentName;
        }
    }

    /// <inheritdoc />
    public override void SkipValue()
    {
        if (State != BsonReaderState.Value)
        {
            ThrowInvalidState(nameof(SkipValue), BsonReaderState.Value);
        }

        int skip;
        switch (CurrentBsonType)
        {
            case BsonType.Array: skip = ReadSize() - 4; break;
            case BsonType.Binary: skip = ReadSize() + 1; break;
            case BsonType.Boolean: skip = 1; break;
            case BsonType.DateTime: skip = 8; break;
            case BsonType.Document: skip = ReadSize() - 4; break;
            case BsonType.Decimal128: skip = 16; break;
            case BsonType.Double: skip = 8; break;
            case BsonType.Int32: skip = 4; break;
            case BsonType.Int64: skip = 8; break;
            case BsonType.JavaScript: skip = ReadSize(); break;
            case BsonType.JavaScriptWithScope: skip = ReadSize() - 4; break;
            case BsonType.MaxKey: skip = 0; break;
            case BsonType.MinKey: skip = 0; break;
            case BsonType.Null: skip = 0; break;
            case BsonType.ObjectId: skip = 12; break;
            case BsonType.RegularExpression: SkipCString(); SkipCString(); skip = 0; break;
            case BsonType.String: skip = ReadSize(); break;
            case BsonType.Symbol: skip = ReadSize(); break;
            case BsonType.Timestamp: skip = 8; break;
            case BsonType.Undefined: skip = 0; break;
            default: throw new BsonInternalException("Unexpected BsonType.");
        }

        _position += skip;
        State = BsonReaderState.Type;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                Close();
            }
            catch
            {
                // ignore exceptions
            }
        }
        base.Dispose(disposing);
    }

    private void PopContext()
    {
        var actualSize = _position - _context.StartPosition;
        if (actualSize != _context.Size)
        {
            throw new FormatException($"Expected size to be {_context.Size}, not {actualSize}.");
        }

        _context = _contextStack.Pop();
    }

    private void PushContext(BsonBinaryReaderContext newContext)
    {
        _contextStack.Push(_context);
        _context = newContext;
    }

    private int ReadSize()
    {
        var size = ReadInt32FromMemory();

        if (size < 0)
        {
            throw new FormatException($"Size {size} is not valid because it is negative.");
        }
        if (size > Settings.MaxDocumentSize)
        {
            throw new FormatException($"Size {size} is not valid because it is larger than MaxDocumentSize {Settings.MaxDocumentSize}.");
        }

        return size;
    }

    private int ReadInt32FromMemory()
    {
        var result = BinaryPrimitives.ReadInt32LittleEndian(_memory.Span.Slice(_position));
        _position += 4;
        return result;
    }

    private long ReadInt64FromMemory()
    {
        var result = BinaryPrimitives.ReadInt64LittleEndian(_memory.Span.Slice(_position));
        _position += 8;
        return result;
    }

    private IByteBuffer ReadSliceFromMemory()
    {
        var memoryAtPosition = _memory.Slice(_position);
        var length = BinaryPrimitives.ReadInt32LittleEndian(memoryAtPosition.Span);

        var result = _byteBufferSlicer.GetSlice(_position, length);
        _position += length;

        return result;
    }

    private string ReadStringFromMemory()
    {
        var span = _memory.Span.Slice(_position);
        var length = BinaryPrimitives.ReadInt32LittleEndian(span);

        if (span[4 + length - 1] != 0)
        {
            throw new FormatException("String is missing terminating null byte.");
        }

        var result = Utf8Helper.DecodeUtf8String(span.Slice(4, length - 1), Settings.Encoding);
        _position += length + 4;

        return result;
    }

    private string ReadCStringFromMemory(UTF8Encoding encoding = null)
    {
        var span = _memory.Span.Slice(_position);
        var index = span.IndexOf((byte)0);

        var result = Utf8Helper.DecodeUtf8String(span.Slice(0, index), encoding ?? Settings.Encoding);
        _position += index + 1;

        return result;
    }

    private void VerifyBsonTypeAndSetNextState(BsonType requiredBsonType, [System.Runtime.CompilerServices.CallerMemberName]string methodName = null)
    {
        VerifyBsonType(requiredBsonType, methodName);
        var nextState = __stateMap[(int)_context.ContextType];

        if (nextState == BsonReaderState.Closed)
        {
            throw new BsonInternalException($"Unexpected ContextType {_context.ContextType}.");
        }

        State = nextState;
    }
}
