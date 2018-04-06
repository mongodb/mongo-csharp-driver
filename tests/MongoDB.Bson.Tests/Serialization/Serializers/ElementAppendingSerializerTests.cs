/* Copyright 2017-present MongoDB Inc.
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
using System.Reflection;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Tests.IO;
using Moq;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class ElementAppendingSerializerTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var documentSerializer = BsonDocumentSerializer.Instance;
            var elements = new BsonElement[0];

            var result = new ElementAppendingSerializer<BsonDocument>(documentSerializer, elements);

            result._documentSerializer().Should().BeSameAs(documentSerializer);
            result._elements().Should().Equal(elements);
        }

        [Fact]
        public void constructor_should_enumerate_elements()
        {
            var documentSerializer = BsonDocumentSerializer.Instance;
            var mockElements = new Mock<IEnumerable<BsonElement>>();
            var mockEnumerator = new Mock<IEnumerator<BsonElement>>();
            mockElements.Setup(m => m.GetEnumerator()).Returns(mockEnumerator.Object);
            mockEnumerator.Setup(m => m.MoveNext()).Returns(false);

            var subject = new ElementAppendingSerializer<BsonDocument>(documentSerializer, mockElements.Object);

            mockElements.Verify(m => m.GetEnumerator(), Times.Once);
        }

        [Fact]
        public void constructor_should_throw_when_documentSerializer_is_null()
        {
            var elements = new BsonElement[0];

            var exception = Record.Exception(() => new ElementAppendingSerializer<BsonDocument>(null, elements));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("documentSerializer");
        }

        [Fact]
        public void constructor_should_throw_when_elements_is_null()
        {
            var documentSerializer = BsonDocumentSerializer.Instance;

            var exception = Record.Exception(() => new ElementAppendingSerializer<BsonDocument>(documentSerializer, null));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("elements");
        }

        [Fact]
        public void ValueType_should_return_expected_result()
        {
            var subject = CreateSubject();

            var result = subject.ValueType;

            result.Should().Be(typeof(BsonDocument));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Deserialize_should_throw(bool useGenericInterface)
        {
            var subject = CreateSubject();
            var reader = new Mock<IBsonReader>().Object;
            var context = BsonDeserializationContext.CreateRoot(reader);
            var args = new BsonDeserializationArgs { NominalType = typeof(BsonDocument) };

            Exception exception;
            if (useGenericInterface)
            {
                exception = Record.Exception(() => subject.Deserialize(context, args));
            }
            else
            {
                exception = Record.Exception(() => ((IBsonSerializer)subject).Deserialize(context, args));
            }

            exception.Should().BeOfType<NotSupportedException>();
        }

        [Theory]
        [InlineData("{ }", "{ }", "{ }")]
        [InlineData("{ }", "{ a : 1 }", "{ \"a\" : 1 }")]
        [InlineData("{ a : 1 }", "{ }", "{ \"a\" : 1 }")]
        [InlineData("{ a : 1 }", "{ b : 2 }", "{ \"a\" : 1, \"b\" : 2 }")]
        [InlineData("{ a : 1 }", "{ b : 2, c : 3 }", "{ \"a\" : 1, \"b\" : 2, \"c\" : 3 }")]
        [InlineData("{ a : 1 , b : 2 }", "{ }", "{ \"a\" : 1, \"b\" : 2 }")]
        [InlineData("{ a : 1 , b : 2 }", "{ c : 3 }", "{ \"a\" : 1, \"b\" : 2, \"c\" : 3 }")]
        [InlineData("{ a : 1 , b : 2 }", "{ c : 3, d : 4 }", "{ \"a\" : 1, \"b\" : 2, \"c\" : 3, \"d\" : 4 }")]
        public void Serialize_should_have_expected_result(string valueString, string elementsString, string expectedResult)
        {
            var value = BsonDocument.Parse(valueString);
            var elements = BsonDocument.Parse(elementsString).Elements;
            var subject = CreateSubject(elements);

            foreach (var useGenericInterface in new[] { false, true })
            {
                string result;
                using (var textWriter = new StringWriter())
                using (var writer = new JsonWriter(textWriter))
                {
                    var context = BsonSerializationContext.CreateRoot(writer);
                    var args = new BsonSerializationArgs { NominalType = typeof(BsonDocument) };

                    if (useGenericInterface)
                    {
                        subject.Serialize(context, args, value);
                    }
                    else
                    {
                        ((IBsonSerializer)subject).Serialize(context, args, value);
                    }

                    result = textWriter.ToString();
                }

                result.Should().Be(expectedResult);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Serialize_should_not_convert_Uuids_in_elements(bool useGenericInterface)
        {
            var guid = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10");
            var value = new BsonDocument { { "_id", new BsonBinaryData(guid, GuidRepresentation.Standard) }, { "x", 1 } };
            var elements = new BsonDocument("b", new BsonBinaryData(guid, GuidRepresentation.Standard));
            var subject = CreateSubject(elements);

            string result;
            using (var textWriter = new StringWriter())
            using (var writer = new JsonWriter(textWriter))
            {
                var context = BsonSerializationContext.CreateRoot(writer);
                var args = new BsonSerializationArgs { NominalType = typeof(BsonDocument) };

                if (useGenericInterface)
                {
                    subject.Serialize(context, args, value);
                }
                else
                {
                    ((IBsonSerializer)subject).Serialize(context, args, value);
                }

                result = textWriter.ToString();
            }

            // note that "_id" was converted to a CSUUID but "b" was not
            result.Should().Be("{ \"_id\" : CSUUID(\"01020304-0506-0708-090a-0b0c0d0e0f10\"), \"x\" : 1, \"b\" : UUID(\"01020304-0506-0708-090a-0b0c0d0e0f10\") }");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Serialize_should_configure_GuidRepresentation(bool useGenericInterface)
        {
            var mockDocumentSerializer = new Mock<IBsonSerializer<BsonDocument>>();
            var writerSettingsConfigurator = (Action<BsonWriterSettings>)(s => s.GuidRepresentation = GuidRepresentation.Unspecified);
            var subject = new ElementAppendingSerializer<BsonDocument>(mockDocumentSerializer.Object, new BsonElement[0], writerSettingsConfigurator);
            var stream = new MemoryStream();
            var settings = new BsonBinaryWriterSettings { GuidRepresentation = GuidRepresentation.CSharpLegacy };
            var writer = new BsonBinaryWriter(stream, settings);
            var context = BsonSerializationContext.CreateRoot(writer);
            var args = new BsonSerializationArgs { NominalType = typeof(BsonDocument) };
            var value = new BsonDocument();
            GuidRepresentation? capturedGuidRepresentation = null;
            mockDocumentSerializer
                .Setup(m => m.Serialize(It.IsAny<BsonSerializationContext>(), args, value))
                .Callback((BsonSerializationContext c, BsonSerializationArgs a, BsonDocument v) =>
                {
                    var elementAppendingWriter = (ElementAppendingBsonWriter)c.Writer;
                    var configurator = elementAppendingWriter._settingsConfigurator();
                    var configuredSettings = new BsonBinaryWriterSettings { GuidRepresentation = GuidRepresentation.CSharpLegacy };
                    configurator(configuredSettings);
                    capturedGuidRepresentation = configuredSettings.GuidRepresentation;
                });

            if (useGenericInterface)
            {
                subject.Serialize(context, args, value);
            }
            else
            {
                ((IBsonSerializer)subject).Serialize(context, args, value);
            }

            capturedGuidRepresentation.Should().Be(GuidRepresentation.Unspecified);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void Serialize_should_preserve_IsDynamicType_when_creating_new_context(bool isDynamicType, bool useGenericInterface)
        {
            var mockDocumentSerializer = new Mock<IBsonSerializer<BsonDocument>>();
            var subject = new ElementAppendingSerializer<BsonDocument>(mockDocumentSerializer.Object, new BsonElement[0]);
            var writer = new Mock<IBsonWriter>().Object;
            var context = BsonSerializationContext.CreateRoot(writer, b => { b.IsDynamicType = t => isDynamicType; });
            var args = new BsonSerializationArgs { NominalType = typeof(BsonDocument) };
            var value = new BsonDocument();
            bool? capturedIsDynamicType = null;
            mockDocumentSerializer
                .Setup(m => m.Serialize(It.IsAny<BsonSerializationContext>(), args, value))
                .Callback((BsonSerializationContext c, BsonSerializationArgs a, BsonDocument v) => capturedIsDynamicType = c.IsDynamicType(typeof(BsonDocument)));

            if (useGenericInterface)
            {
                subject.Serialize(context, args, value);
            }
            else
            {
                ((IBsonSerializer)subject).Serialize(context, args, value);
            }

            mockDocumentSerializer.Verify(
                m => m.Serialize(It.Is<BsonSerializationContext>(c => c.IsDynamicType(typeof(BsonDocument)) == isDynamicType), args, value),
                Times.Once);

            capturedIsDynamicType.Should().Be(isDynamicType);
        }

        // private methods
        private ElementAppendingSerializer<BsonDocument> CreateSubject(
            IEnumerable<BsonElement> elements = null,
            Action<BsonWriterSettings> writerSettingsConfigurator = null)
        {
            var documentSerializer = BsonDocumentSerializer.Instance;
            elements = elements ?? new BsonElement[0];
            writerSettingsConfigurator = writerSettingsConfigurator ?? (s => s.GuidRepresentation = GuidRepresentation.Unspecified);
            return new ElementAppendingSerializer<BsonDocument>(documentSerializer, elements, writerSettingsConfigurator);
        }
    }

    public static class ElementAppendingSerializerReflector
    {
        public static IBsonSerializer<TDocument> _documentSerializer<TDocument>(this ElementAppendingSerializer<TDocument> instance)
        {
            var fieldInfo = typeof(ElementAppendingSerializer<TDocument>).GetField("_documentSerializer", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IBsonSerializer<TDocument>)fieldInfo.GetValue(instance);
        }

        public static List<BsonElement> _elements<TDocument>(this ElementAppendingSerializer<TDocument> instance)
        {
            var fieldInfo = typeof(ElementAppendingSerializer<TDocument>).GetField("_elements", BindingFlags.NonPublic | BindingFlags.Instance);
            return (List<BsonElement>)fieldInfo.GetValue(instance);
        }
    }
}
