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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Bson.Tests.IO;

public class ReadOnlyMemoryReaderTests
{
    [Fact]
    public void Bookmarks_should_work()
    {
        var document = new BsonDocument { { "x", 1 }, { "y", 2 }, { "z", new BsonDocument { { "a", "a"}, { "b", "b" } } } };
        var bytes = document.ToBson();

        using var bsonReader = new ReadOnlyMemoryBsonReader(bytes);

        AssertRead(() => bsonReader.ReadBsonType(), BsonType.Document);
        DoReaderAction(() => bsonReader.ReadStartDocument());

        var bookmarkX = bsonReader.GetBookmark();
        AssertRead(() => bsonReader.ReadBsonType(), BsonType.Int32);
        AssertRead(() => bsonReader.ReadName(), "x");
        AssertRead(() => bsonReader.ReadInt32(), 1);

        AssertRead(() => bsonReader.ReadBsonType(), BsonType.Int32);
        AssertRead(() => bsonReader.ReadName(), "y");
        AssertRead(() => bsonReader.ReadInt32(), 2);

        AssertRead(() => bsonReader.ReadBsonType(), BsonType.Document);
        AssertRead(() => bsonReader.ReadName(), "z");
        DoReaderAction(() => bsonReader.ReadStartDocument());

        AssertRead(() => bsonReader.ReadBsonType(), BsonType.String);
        AssertRead(() => bsonReader.ReadName(), "a");
        AssertRead(() => bsonReader.ReadString(), "a");

        var bookmarkB = bsonReader.GetBookmark();
        AssertRead(() => bsonReader.ReadBsonType(), BsonType.String);
        AssertRead(() => bsonReader.ReadName(), "b");
        AssertRead(() => bsonReader.ReadString(), "b");
        DoReaderAction(() => bsonReader.ReadEndDocument());

        DoReaderAction(() => bsonReader.ReadEndDocument());
        bsonReader.State.Should().Be(BsonReaderState.Initial);
        bsonReader.IsAtEndOfFile().Should().BeTrue();

        bsonReader.ReturnToBookmark(bookmarkX);
        AssertRead(() => bsonReader.ReadBsonType(), BsonType.Int32);
        AssertRead(() => bsonReader.ReadName(), "x");
        AssertRead(() => bsonReader.ReadInt32(), 1);

        bsonReader.ReturnToBookmark(bookmarkB);
        AssertRead(() => bsonReader.ReadBsonType(), BsonType.String);
        AssertRead(() => bsonReader.ReadName(), "b");
        AssertRead(() => bsonReader.ReadString(), "b");
        DoReaderAction(() => bsonReader.ReadEndDocument());

        // do everything twice returning to bookmark in between
        void DoReaderAction(Action readerAction)
        {
            var bookmark = bsonReader.GetBookmark();
            readerAction();
            bsonReader.ReturnToBookmark(bookmark);
            readerAction();
        }

        void AssertRead<T>(Func<T> reader, T expected)
        {
            var bookmark = bsonReader.GetBookmark();
            reader().Should().Be(expected);
            bsonReader.ReturnToBookmark(bookmark);
            reader().Should().Be(expected);
        }
    }

    [Theory]
    [MemberData(nameof(BsonValues))]
    public void BsonValue_should_be_deserialized(BsonValue value)
    {
        var document = new BsonDocument("x", value);

        RehydrateAndValidate(document);
    }

    public static readonly IEnumerable<object[]> BsonValues =
    [
        [new BsonDecimal128(1.0M)],
        [new BsonDouble(1.0)],
        [new BsonBoolean(true)],
        [new BsonInt32(1)],
        [new BsonInt64(1L)],
        [new BsonString("")],
        [new BsonTimestamp(2)],
        [new BsonBinaryData([1, 2, 3, 4, 5, 6])],
        [new BsonRegularExpression("^p")],
        [new BsonObjectId(new ObjectId("4d0ce088e447ad08b4721a37"))],
        [BsonSymbolTable.Lookup("name")],
        [BsonNull.Value],
        [BsonMaxKey.Value],
        [BsonMinKey.Value],
        [BsonUndefined.Value],
        [new BsonJavaScript("function f() { return 1; }")],
        [new BsonJavaScriptWithScope("function f() { return n; }", new BsonDocument("n", 1))],
        [new BsonArray([1, "s", 5m])],
        [new BsonBinaryData([1, 2, 3])],
        [new BsonDocument { { "a", "1" }, { "b", 1 }, { "c", new BsonDocument { { "d", new BsonArray([1, "s", new BsonDocument("tt", 11)]) } } } }],
        [new BsonDocument()]
    ];

    [Fact]
    public void ComplexDocument_should_be_deserialized()
    {
        var dictionary = BsonValues.Select((v, i) => ($"key_{i}", v[0])).ToDictionary(p => p.Item1, p => p.Item2);
        var document = new BsonDocument(dictionary);

        RehydrateAndValidate(document);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 10)]
    [InlineData(5, 10)]
    [InlineData(10, 10)]
    public void Position_set_should_set_expected_position(int position, int length)
    {
        using var subject = new ReadOnlyMemoryBsonReader(Enumerable.Range(0, length).Select(i => (byte)i).ToArray());

        subject.Position = position;
        subject.Position.Should().Be(position);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void Position_set_should_thrown_on_invalid_value(int position)
    {
        using var subject = new ReadOnlyMemoryBsonReader(new byte[] { 1, 2 });

        var exception = Record.Exception(() => subject.Position = position);
        var formatException = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;

        formatException.ParamName.Should().Be("value");
    }

    [Fact]
    public void ReadBoolean_should_throw_on_invalid_value()
    {
        byte invalidBoolean = 9;
        var bytes = new byte[]
        {
            9, 0, 0, 0, // Length
            (byte)BsonType.Boolean, // Type
            (byte)'x', 0, // Name
            invalidBoolean, 0 // boolean value
        };

        using var subject = new ReadOnlyMemoryBsonReader(bytes);
        var exception = Record.Exception(() => BsonSerializer.Deserialize<BsonDocument>(subject));
        var formatException = exception.Should().BeOfType<FormatException>().Subject;

        formatException.Message.Should().Contain($"Invalid BsonBoolean value: {invalidBoolean}");
    }

    [Theory]
    [InlineData(BsonBinarySubType.UuidStandard)]
    [InlineData(BsonBinarySubType.UuidLegacy)]
    public void ReadBinaryData_should_read_correct_subtype(BsonBinarySubType subType)
    {
        var bytes = new byte[] { 29, 0, 0, 0, 5, 120, 0, 16, 0, 0, 0, (byte)subType, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 0 };

        using var reader = new ReadOnlyMemoryBsonReader(bytes);
        reader.ReadStartDocument();
        var type = reader.ReadBsonType();
        var name = reader.ReadName();
        var binaryData = reader.ReadBinaryData();
        var endOfDocument = reader.ReadBsonType();
        reader.ReadEndDocument();

        name.Should().Be("x");
        type.Should().Be(BsonType.Binary);
        binaryData.SubType.Should().Be(subType);
        binaryData.Bytes.Should().Equal(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
        endOfDocument.Should().Be(BsonType.EndOfDocument);
    }

    [Theory]
    [InlineData("00000000 f0 6100", "a")]
    [InlineData("00000000 08 6100 00 f0 6200", "b")]
    [InlineData("00000000 03 6100 00000000 f0 6200", "a.b")]
    [InlineData("00000000 03 6100 00000000 08 6200 00 f0 6300", "a.c")]
    [InlineData("00000000 04 6100 00000000 f0", "a.0")]
    [InlineData("00000000 04 6100 00000000 08 3000 00 f0", "a.1")]
    [InlineData("00000000 04 6100 00000000 03 3000 00000000 f0 6200", "a.0.b")]
    [InlineData("00000000 04 6100 00000000 03 3000 00000000 08 6200 00 f0 6300", "a.0.c")]
    [InlineData("00000000 04 6100 00000000 08 3000 00 03 3100 00000000 f0 6200", "a.1.b")]
    [InlineData("00000000 04 6100 00000000 08 3000 00 03 3200 00000000 08 6200 00 f0 6300", "a.1.c")]
    public void ReadBsonType_should_throw_when_bson_type_is_invalid(string hexBytes, string expectedElementName)
    {
        var bytes = BsonUtils.ParseHexString(hexBytes.Replace(" ", ""));
        var expectedMessage = $"Detected unknown BSON type \"\\xf0\" for fieldname \"{expectedElementName}\". Are you using the latest driver version?";

        using var subject = new ReadOnlyMemoryBsonReader(bytes);

        var exception = Record.Exception(() => BsonSerializer.Deserialize<BsonDocument>(subject));
        var formatException = exception.Should().BeOfType<FormatException>().Subject;

        formatException.Message.Should().Contain(expectedMessage);
    }

    [Fact]
    public void ReadBytes_should_return_expected_result()
    {
        byte[] bytesExpected = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];
        byte[] bytes = [29, 0, 0, 0, 5, 120, 0, 16, 0, 0, 0, 0, ..bytesExpected, 0 ];

        using var reader = new ReadOnlyMemoryBsonReader(bytes);
        reader.ReadStartDocument();
        var type = reader.ReadBsonType();
        var name = reader.ReadName();
        var bytesActual = reader.ReadBytes();
        var endOfDocument = reader.ReadBsonType();
        reader.ReadEndDocument();

        name.Should().Be("x");
        type.Should().Be(BsonType.Binary);
        bytesActual.ShouldAllBeEquivalentTo(bytesExpected);
        endOfDocument.Should().Be(BsonType.EndOfDocument);
    }

    [Theory]
    [ParameterAttributeData]
    public void ReadOnlyMemoryBsonReader_should_support_reading_multiple_documents([Range(0, 3)]int numberOfDocuments)
    {
        var document = new BsonDocument("x", 1);
        var bson = document.ToBson();
        var input = Enumerable.Repeat(bson, numberOfDocuments).Aggregate(Enumerable.Empty<byte>(), (a, b) => a.Concat(b)).ToArray();
        var expectedResult = Enumerable.Repeat(document, numberOfDocuments);

        using var reader = new ReadOnlyMemoryBsonReader(input);
        var result = new List<BsonDocument>();

        while (!reader.IsAtEndOfFile())
        {
            reader.ReadStartDocument();
            var name = reader.ReadName();
            var value = reader.ReadInt32();
            reader.ReadEndDocument();

            var resultDocument = new BsonDocument(name, value);
            result.Add(resultDocument);
        }

        result.Should().Equal(expectedResult);
    }

    [Fact]
    public void ReadRawBsonArray_should_return_expected_result()
    {
        var bsonDocument = new BsonDocument { { "_id", 1 }, { "A", new BsonArray { 1, 2 } } };
        var bson = bsonDocument.ToBson();

        using var reader = new ReadOnlyMemoryBsonReader(bson);
        using var document = BsonSerializer.Deserialize<CWithRawBsonArray>(reader);

        document.Id.Should().Be(1);
        document.A.Count.Should().Be(2);
        document.A[0].AsInt32.Should().Be(1);
        document.A[1].AsInt32.Should().Be(2);
        bson.ShouldBeEquivalentTo(document.ToBson());

        var slice = document.A.Slice.Should().BeOfType<ReadOnlyMemoryBuffer>().Subject;
        MemoryMarshal.TryGetArray(slice.Memory, out var arraySegment).Should().BeTrue();
        arraySegment.Array.Should().BeSameAs(bson);
    }

    [Fact]
    public void ReadRawBsonArray_should_read_slice_from_byteBuffer_slicer()
    {
        var bsonDocument = new BsonDocument { { "_id", 1 }, { "A", new BsonArray { 1, 2 } } };
        var bson = bsonDocument.ToBson();
        var slicer = new ReadOnlyMemorySlicerMock(bson);

        using var reader = new ReadOnlyMemoryBsonReader(bson, slicer, new());
        using var document = BsonSerializer.Deserialize<CWithRawBsonArray>(reader);

        document.Id.Should().Be(1);
        document.A.Count.Should().Be(2);
        document.A[0].AsInt32.Should().Be(1);
        document.A[1].AsInt32.Should().Be(2);
        bson.ShouldBeEquivalentTo(document.ToBson());

        slicer.GetSliceCalledCount.Should().Be(1);

        var slice = document.A.Slice.Should().BeOfType<ReadOnlyMemoryBuffer>().Subject;
        MemoryMarshal.TryGetArray(slice.Memory, out var arraySegment).Should().BeTrue();
        arraySegment.Array.Should().NotBeSameAs(bson);
    }

    [Fact]
    public void ReadRawBsonDocument_should_return_expected_result()
    {
        var bsonDocument = new BsonDocument { { "_id", 1 }, { "A", new BsonDocument { { "x", 1 }, { "y", "2" } } } };
        var bson = bsonDocument.ToBson();

        using var reader = new ReadOnlyMemoryBsonReader(bson);
        using var document = BsonSerializer.Deserialize<CWithRawBsonDocument>(reader);

        document.Id.Should().Be(1);
        document.A.Values.Count().Should().Be(2);
        document.A["x"].AsInt32.Should().Be(1);
        document.A["y"].AsString.Should().Be("2");
        bson.ShouldBeEquivalentTo(document.ToBson());

        var slice = document.A.Slice.Should().BeOfType<ReadOnlyMemoryBuffer>().Subject;
        MemoryMarshal.TryGetArray(slice.Memory, out var arraySegment).Should().BeTrue();
        arraySegment.Array.Should().BeSameAs(bson);
    }

    [Fact]
    public void ReadRawBsonDocument_should_read_slice_from_byteBuffer_slicer()
    {
        var bsonDocument = new BsonDocument { { "_id", 1 }, { "A", new BsonDocument { { "x", 1 }, { "y", "2" } } } };
        var bson = bsonDocument.ToBson();
        var slicer = new ReadOnlyMemorySlicerMock(bson);

        using var reader = new ReadOnlyMemoryBsonReader(bson, slicer, new());
        using var document = BsonSerializer.Deserialize<CWithRawBsonDocument>(reader);

        document.Id.Should().Be(1);
        document.A.Values.Count().Should().Be(2);
        document.A["x"].AsInt32.Should().Be(1);
        document.A["y"].AsString.Should().Be("2");
        bson.ShouldBeEquivalentTo(document.ToBson());

        slicer.GetSliceCalledCount.Should().Be(1);

        var slice = document.A.Slice.Should().BeOfType<ReadOnlyMemoryBuffer>().Subject;
        MemoryMarshal.TryGetArray(slice.Memory, out var arraySegment).Should().BeTrue();
        arraySegment.Array.Should().NotBeSameAs(bson);
    }

    private void RehydrateAndValidate(BsonDocument expectedDocument)
    {
        using var reader = CreateSubject(expectedDocument);
        var context = BsonDeserializationContext.CreateRoot(reader);

        var actualDocument = BsonDocumentSerializer.Instance.Deserialize(context);

        Assert.True(expectedDocument.Equals(actualDocument));
    }

    private ReadOnlyMemoryBsonReader CreateSubject(BsonDocument bsonDocument)
    {
        var bson = bsonDocument.ToBson();
        return new ReadOnlyMemoryBsonReader(bson);
    }

    private class CWithRawBsonArray : IDisposable
    {
        public int Id { get; set; }
        public RawBsonArray A { get; set; }

        public void Dispose()
        {
            if (A != null)
            {
                A.Dispose();
                A = null;
            }
        }
    }

    private class CWithRawBsonDocument : IDisposable
    {
        public int Id { get; set; }
        public RawBsonDocument A { get; set; }

        public void Dispose()
        {
            if (A != null)
            {
                A.Dispose();
                A = null;
            }
        }
    }

    internal sealed class ReadOnlyMemorySlicerMock(ReadOnlyMemory<byte> ReadOnlyMemory) : IByteBufferSlicer
    {
        public int GetSliceCalledCount { get; private set; }

        public IByteBuffer GetSlice(int position, int length)
        {
            GetSliceCalledCount++;

            var slice = ReadOnlyMemory.Slice(position, length).ToArray();
            return new ReadOnlyMemoryBuffer(slice, new ReadOnlyMemorySlicerMock(slice));
        }
    }
}

