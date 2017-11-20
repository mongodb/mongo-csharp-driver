/* Copyright 2017 MongoDB Inc.
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
using System.Linq;
using System.Reflection;
using FluentAssertions;
using MongoDB.Bson.IO;
using Moq;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class WrappingBsonWriterTests
    {
        [Fact]
        public void constructor_should_return_expected_result()
        {
            var wrapped = new Mock<IBsonWriter>().Object;

            var result = new MockWrappingBsonWriter(wrapped);

            result.Wrapped.Should().BeSameAs(wrapped);
            result._disposed().Should().BeFalse();
        }

        [Fact]
        public void constructor_should_throw_when_wrapped_is_null()
        {
            var exception = Record.Exception(() => new MockWrappingBsonWriter(null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("wrapped");
        }

        [Fact]
        public void Position_should_call_wrapped_Position()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            var result = subject.Position;

            mockWrapped.VerifyGet(w => w.Position, Times.Once);
        }

        [Fact]
        public void Position_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.Position);

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void SerializationDepth_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            var result = subject.SerializationDepth;

            mockWrapped.VerifyGet(w => w.SerializationDepth, Times.Once);
        }

        [Fact]
        public void SerializationDepth_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.SerializationDepth);

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void Settings_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            var result = subject.Settings;

            mockWrapped.VerifyGet(w => w.Settings, Times.Once);
        }

        [Fact]
        public void Settings_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.Settings);

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void State_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            var result = subject.State;

            mockWrapped.VerifyGet(w => w.State, Times.Once);
        }

        [Fact]
        public void State_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.State);

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void Wrapped_should_return_expected_result()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            var result = subject.Wrapped;

            result.Should().BeSameAs(mockWrapped.Object);
        }

        [Fact]
        public void Wrapped_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.Wrapped);

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Close_should_call_wrapped(bool disposed)
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);
            if (disposed)
            {
                subject.Dispose();
            }

            subject.Close();

            mockWrapped.Verify(m => m.Close(), Times.Once); // even if it has been Disposed
        }

        [Fact]
        public void Dispose_should_call_Dispose_true()
        {
            var subject = CreateSubject();

            subject.Dispose();

            ((MockWrappingBsonWriter)subject).DisposeTrueWasCalled.Should().BeTrue();
        }

        [Fact]
        public void Dispose_can_be_called_multiple_times()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            subject.Dispose();
            subject.Dispose();

            mockWrapped.Verify(m => m.Dispose(), Times.Once);
        }

        [Fact]
        public void Flush_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            subject.Flush();

            mockWrapped.Verify(m => m.Flush(), Times.Once);
        }

        [Fact]
        public void Flush_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.Flush());

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void PopElementNameValidator_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            subject.PopElementNameValidator();

            mockWrapped.Verify(m => m.PopElementNameValidator(), Times.Once);
        }

        [Fact]
        public void PopElementNameValidator_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.PopElementNameValidator());

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void PopSettings_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            subject.PopSettings();

            mockWrapped.Verify(m => m.PopSettings(), Times.Once);
        }

        [Fact]
        public void PopSettings_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.PopSettings());

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void PushElementNameValidator_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);
            var validator = new Mock<IElementNameValidator>().Object;

            subject.PushElementNameValidator(validator);

            mockWrapped.Verify(m => m.PushElementNameValidator(validator), Times.Once);
        }

        [Fact]
        public void PushElementNameValidator_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();
            var validator = new Mock<IElementNameValidator>().Object;

            var exception = Record.Exception(() => subject.PushElementNameValidator(validator));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void PushSettings_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);
            Action<BsonWriterSettings> configurator = s => { };

            subject.PushSettings(configurator);

            mockWrapped.Verify(m => m.PushSettings(configurator), Times.Once);
        }

        [Fact]
        public void PushSettings_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();
            Action<BsonWriterSettings> configurator = s => { };

            var exception = Record.Exception(() => subject.PushSettings(configurator));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void WriteBinaryData_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);
            var value = new BsonBinaryData(new byte[0]);

            subject.WriteBinaryData(value);

            mockWrapped.Verify(m => m.WriteBinaryData(value), Times.Once);
        }

        [Fact]
        public void WriteBinaryData_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();
            var value = new BsonBinaryData(new byte[0]);

            var exception = Record.Exception(() => subject.WriteBinaryData(value));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void WriteBooelan_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);
            var value = true;

            subject.WriteBoolean(value);

            mockWrapped.Verify(m => m.WriteBoolean(value), Times.Once);
        }

        [Fact]
        public void WriteBoolean_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();
            var value = true;

            var exception = Record.Exception(() => subject.WriteBoolean(value));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void WriteBytes_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);
            var value = new byte[0];

            subject.WriteBytes(value);

            mockWrapped.Verify(m => m.WriteBytes(value), Times.Once);
        }

        [Fact]
        public void WriteBytes_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();
            var value = new byte[0];

            var exception = Record.Exception(() => subject.WriteBytes(value));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void WriteDateTime_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);
            var value = 123L;

            subject.WriteDateTime(value);

            mockWrapped.Verify(m => m.WriteDateTime(value), Times.Once);
        }

        [Fact]
        public void WriteDateTime_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();
            var value = 123L;

            var exception = Record.Exception(() => subject.WriteDateTime(value));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void WriteDecimal128_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);
            var value = Decimal128.Parse("123");

            subject.WriteDecimal128(value);

            mockWrapped.Verify(m => m.WriteDecimal128(value), Times.Once);
        }

        [Fact]
        public void WriteDecimal128_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();
            var value = Decimal128.Parse("123");

            var exception = Record.Exception(() => subject.WriteDecimal128(value));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void WriteDouble_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);
            var value = 123.0;

            subject.WriteDouble(value);

            mockWrapped.Verify(m => m.WriteDouble(value), Times.Once);
        }

        [Fact]
        public void WriteDouble_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();
            var value = 123.0;

            var exception = Record.Exception(() => subject.WriteDouble(value));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void WriteEndArray_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            subject.WriteEndArray();

            mockWrapped.Verify(m => m.WriteEndArray(), Times.Once);
        }

        [Fact]
        public void WriteEndArray_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.WriteEndArray());

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void WriteEndDocument_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            subject.WriteEndDocument();

            mockWrapped.Verify(m => m.WriteEndDocument(), Times.Once);
        }

        [Fact]
        public void WriteEndDocument_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.WriteEndDocument());

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void WriteInt32_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);
            var value = 123;

            subject.WriteInt32(value);

            mockWrapped.Verify(m => m.WriteInt32(value), Times.Once);
        }

        [Fact]
        public void WriteInt32_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();
            var value = 123;

            var exception = Record.Exception(() => subject.WriteInt32(value));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void WriteInt64_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);
            var value = 123L;

            subject.WriteInt64(value);

            mockWrapped.Verify(m => m.WriteInt64(value), Times.Once);
        }

        [Fact]
        public void WriteInt64_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();
            var value = 123L;

            var exception = Record.Exception(() => subject.WriteInt64(value));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void WriteJavaScript_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);
            var value = "code";

            subject.WriteJavaScript(value);

            mockWrapped.Verify(m => m.WriteJavaScript(value), Times.Once);
        }

        [Fact]
        public void WriteJavaScript_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();
            var value = "code";

            var exception = Record.Exception(() => subject.WriteJavaScript(value));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void WriteJavaScriptWithScope_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);
            var value = "code";

            subject.WriteJavaScriptWithScope(value);

            mockWrapped.Verify(m => m.WriteJavaScriptWithScope(value), Times.Once);
        }

        [Fact]
        public void WriteJavaScriptWithScope_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();
            var value = "code";

            var exception = Record.Exception(() => subject.WriteJavaScriptWithScope(value));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void WriteMaxKey_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            subject.WriteMaxKey();

            mockWrapped.Verify(m => m.WriteMaxKey(), Times.Once);
        }

        [Fact]
        public void WriteMaxKey_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.WriteMaxKey());

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void WriteMinKey_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            subject.WriteMinKey();

            mockWrapped.Verify(m => m.WriteMinKey(), Times.Once);
        }

        [Fact]
        public void WriteMinKey_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.WriteMinKey());

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void WriteName_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);
            var name = "name";

            subject.WriteName(name);

            mockWrapped.Verify(m => m.WriteName(name), Times.Once);
        }

        [Fact]
        public void WriteName_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();
            var name = "name";

            var exception = Record.Exception(() => subject.WriteName(name));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void WriteNull_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            subject.WriteNull();

            mockWrapped.Verify(m => m.WriteNull(), Times.Once);
        }

        [Fact]
        public void WriteNull_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.WriteNull());

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void WriteObjectId_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);
            var value = ObjectId.GenerateNewId(); ;

            subject.WriteObjectId(value);

            mockWrapped.Verify(m => m.WriteObjectId(value), Times.Once);
        }

        [Fact]
        public void WriteObjectId_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();
            var value = ObjectId.GenerateNewId(); ;

            var exception = Record.Exception(() => subject.WriteObjectId(value));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void WriteRawBsonArray_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);
            var value = new Mock<IByteBuffer>().Object;

            subject.WriteRawBsonArray(value);

            mockWrapped.Verify(m => m.WriteRawBsonArray(value), Times.Once);
        }

        [Fact]
        public void WriteRawBsonArray_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();
            var value = new Mock<IByteBuffer>().Object;

            var exception = Record.Exception(() => subject.WriteRawBsonArray(value));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void WriteRawBsonDocument_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);
            var value = new Mock<IByteBuffer>().Object;

            subject.WriteRawBsonDocument(value);

            mockWrapped.Verify(m => m.WriteRawBsonDocument(value), Times.Once);
        }

        [Fact]
        public void WriteRawBsonDocument_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();
            var value = new Mock<IByteBuffer>().Object;

            var exception = Record.Exception(() => subject.WriteRawBsonDocument(value));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void WriteRegularExpression_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);
            var value = new BsonRegularExpression("pattern", "options");

            subject.WriteRegularExpression(value);

            mockWrapped.Verify(m => m.WriteRegularExpression(value), Times.Once);
        }

        [Fact]
        public void WriteRegularExpression_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();
            var value = new BsonRegularExpression("pattern", "options");

            var exception = Record.Exception(() => subject.WriteRegularExpression(value));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void WriteStartArray_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            subject.WriteStartArray();

            mockWrapped.Verify(m => m.WriteStartArray(), Times.Once);
        }

        [Fact]
        public void WriteStartArray_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.WriteStartArray());

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void WriteStartDocument_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            subject.WriteStartDocument();

            mockWrapped.Verify(m => m.WriteStartDocument(), Times.Once);
        }

        [Fact]
        public void WriteStartDocument_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.WriteStartDocument());

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void WriteString_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);
            var value = "abc";

            subject.WriteString(value);

            mockWrapped.Verify(m => m.WriteString(value), Times.Once);
        }

        [Fact]
        public void WriteString_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();
            var value = "abc";

            var exception = Record.Exception(() => subject.WriteString(value));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void WriteSymbol_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);
            var value = "abc";

            subject.WriteSymbol(value);

            mockWrapped.Verify(m => m.WriteSymbol(value), Times.Once);
        }

        [Fact]
        public void WriteSymbol_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();
            var value = "abc";

            var exception = Record.Exception(() => subject.WriteSymbol(value));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void WriteTimestamp_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);
            var value = 123L;

            subject.WriteTimestamp(value);

            mockWrapped.Verify(m => m.WriteTimestamp(value), Times.Once);
        }

        [Fact]
        public void WriteTimestamp_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();
            var value = 123L;

            var exception = Record.Exception(() => subject.WriteTimestamp(value));

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Fact]
        public void WriteUndefined_should_call_wrapped()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            subject.WriteUndefined();

            mockWrapped.Verify(m => m.WriteUndefined(), Times.Once);
        }

        [Fact]
        public void WriteUndefined_should_throw_when_disposed()
        {
            var subject = CreateDisposedSubject();

            var exception = Record.Exception(() => subject.WriteUndefined());

            var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
            e.ObjectName.Should().Be(subject.GetType().FullName);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Dispose_bool_should_have_expected_result(bool disposing)
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);

            subject.Dispose(disposing);

            subject._disposed().Should().BeTrue();
            mockWrapped.Verify(m => m.Dispose(), Times.Exactly(disposing ? 1 : 0));
        }

        // private methods
        private WrappingBsonWriter CreateDisposedSubject()
        {
            Mock<IBsonWriter> mockWrapped;
            var subject = CreateSubject(out mockWrapped);
            subject.Dispose();
            return subject;
        }

        private WrappingBsonWriter CreateSubject()
        {
            Mock<IBsonWriter> mockWriter;
            return CreateSubject(out mockWriter);
        }

        private WrappingBsonWriter CreateSubject(out Mock<IBsonWriter> mockWrapped)
        {
            mockWrapped = new Mock<IBsonWriter>();
            return new MockWrappingBsonWriter(mockWrapped.Object);
        }

        // nested types
        private class MockWrappingBsonWriter : WrappingBsonWriter
        {
            // constructors
            public MockWrappingBsonWriter(IBsonWriter wrapped)
                : base(wrapped)
            {
            }

            // public properties
            public bool DisposeTrueWasCalled;

            // public methods
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    DisposeTrueWasCalled = true;
                }
                base.Dispose(disposing);
            }
        }
    }

    internal static class WrappingBsonWriterReflector
    {
        public static bool _disposed(this WrappingBsonWriter instance)
        {
            var propertyInfo = typeof(WrappingBsonWriter).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)propertyInfo.GetValue(instance);
        }

        public static void Dispose(this WrappingBsonWriter instance, bool disposing)
        {
            var methodInfo = typeof(WrappingBsonWriter).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.Name == "Dispose" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(bool))
                .Single();
            methodInfo.Invoke(instance, new object[] { disposing });
        }
    }
}
