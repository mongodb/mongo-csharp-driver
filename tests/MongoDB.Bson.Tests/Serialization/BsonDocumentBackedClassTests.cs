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
using FluentAssertions;
using MongoDB.Bson.Serialization;
using Moq;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class BsonDocumentBackedClassTests
    {
        [Fact]
        public void Constructor_should_initialize_with_provided_document_and_serializer()
        {
            // Arrange
            var mockSerializer = new Mock<IBsonDocumentSerializer>();
            var document = new BsonDocument();

            // Act
            var testClass = new TestBsonDocumentBackedClass(document, mockSerializer.Object);

            // Assert
            testClass.GetBackingDocument().Should().BeSameAs(document);
        }

        [Fact]
        public void Constructor_should_initialize_with_new_document_and_serializer()
        {
            // Arrange
            var mockSerializer = new Mock<IBsonDocumentSerializer>();

            // Act
            var testClass = new TestBsonDocumentBackedClass(mockSerializer.Object);

            // Assert
            testClass.GetBackingDocument().Should().NotBeNull();
            testClass.GetBackingDocument().ElementCount.Should().Be(0);
        }

        [Fact]
        public void Constructor_should_throw_when_backingDocument_is_null()
        {
            // Arrange
            var mockSerializer = new Mock<IBsonDocumentSerializer>();

            // Act
            Action action = () => new TestBsonDocumentBackedClass(null, mockSerializer.Object);

            // Assert
            action.ShouldThrow<ArgumentNullException>()
                .And.ParamName.Should().Be("backingDocument");
        }

        [Fact]
        public void Constructor_should_throw_when_serializer_is_null()
        {
            // Arrange
            var document = new BsonDocument();

            // Act
            Action action = () => new TestBsonDocumentBackedClass(document, null);

            // Assert
            action.ShouldThrow<ArgumentNullException>()
                .And.ParamName.Should().Be("serializer");
        }

        [Fact]
        public void BackingDocument_should_return_backing_document()
        {
            // Arrange
            var mockSerializer = new Mock<IBsonDocumentSerializer>();
            var document = new BsonDocument();
            var testClass = new TestBsonDocumentBackedClass(document, mockSerializer.Object);

            // Act
            var result = testClass.GetBackingDocument();

            // Assert
            result.Should().BeSameAs(document);
        }

        [Fact]
        public void GetValue_should_throw_when_member_does_not_exist()
        {
            // Arrange
            var memberName = "nonExistingMember";
            var mockSerializer = new Mock<IBsonDocumentSerializer>();

            mockSerializer
                .Setup(s => s.TryGetMemberSerializationInfo(memberName, out It.Ref<BsonSerializationInfo>.IsAny))
                .Returns(false);

            var testClass = new TestBsonDocumentBackedClass(mockSerializer.Object);

            // Act
            Action action = () => testClass.GetValue<int>(memberName);

            // Assert
            action.ShouldThrow<ArgumentException>()
                .And.ParamName.Should().Be("memberName");
        }

        [Fact]
        public void GetValue_should_throw_when_element_not_found()
        {
            // Arrange
            var memberName = "testMember";
            var elementName = "testElement";

            var document = new BsonDocument(); // Empty document
            var mockSerializer = new Mock<IBsonDocumentSerializer>();
            var serializationInfo = new BsonSerializationInfo(elementName, mockSerializer.Object, typeof(BsonDocument));

            mockSerializer
                .Setup(s => s.TryGetMemberSerializationInfo(memberName, out It.Ref<BsonSerializationInfo>.IsAny))
                .Callback((string name, out BsonSerializationInfo info) => { info = serializationInfo; })
                .Returns(true);

            var testClass = new TestBsonDocumentBackedClass(document, mockSerializer.Object);

            // Act
            Action action = () => testClass.GetValue<int>(memberName);

            // Assert
            action.ShouldThrow<KeyNotFoundException>()
                .WithMessage($"The backing document does not contain an element named '{elementName}'.");
        }

        [Fact]
        public void GetValue_with_default_should_return_default_when_element_not_found()
        {
            // Arrange
            var memberName = "testMember";
            var elementName = "testElement";
            var defaultValue = 99;

            var document = new BsonDocument(); // Empty document
            var mockSerializer = new Mock<IBsonDocumentSerializer>();
            var serializationInfo = new BsonSerializationInfo(elementName, mockSerializer.Object, typeof(BsonDocument));

            mockSerializer
                .Setup(s => s.TryGetMemberSerializationInfo(memberName, out It.Ref<BsonSerializationInfo>.IsAny))
                .Callback((string name, out BsonSerializationInfo info) => { info = serializationInfo; })
                .Returns(true);

            var testClass = new TestBsonDocumentBackedClass(document, mockSerializer.Object);

            // Act
            var result = testClass.GetValue<int>(memberName, defaultValue);

            // Assert
            result.Should().Be(defaultValue);
        }

        [Fact]
        public void GetValue_with_default_should_throw_when_member_not_found()
        {
            // Arrange
            var memberName = "nonExistingMember";
            var defaultValue = 99;
            var mockSerializer = new Mock<IBsonDocumentSerializer>();

            mockSerializer
                .Setup(s => s.TryGetMemberSerializationInfo(memberName, out It.Ref<BsonSerializationInfo>.IsAny))
                .Returns(false);

            var testClass = new TestBsonDocumentBackedClass(mockSerializer.Object);

            // Act
            Action action = () => testClass.GetValue<int>(memberName, defaultValue);

            // Assert
            action.ShouldThrow<ArgumentException>()
                .And.ParamName.Should().Be("memberName");
        }

        [Fact]
        public void SetValue_should_throw_when_member_not_found()
        {
            // Arrange
            var memberName = "nonExistingMember";
            var value = 42;
            var mockSerializer = new Mock<IBsonDocumentSerializer>();

            mockSerializer
                .Setup(s => s.TryGetMemberSerializationInfo(memberName, out It.Ref<BsonSerializationInfo>.IsAny))
                .Returns(false);

            var testClass = new TestBsonDocumentBackedClass(mockSerializer.Object);

            // Act
            Action action = () => testClass.SetValue(memberName, value);

            // Assert
            action.ShouldThrow<ArgumentException>()
                .And.ParamName.Should().Be("memberName");
        }

        private class TestBsonDocumentBackedClass : BsonDocumentBackedClass
        {
            public TestBsonDocumentBackedClass(IBsonDocumentSerializer serializer)
                : base(serializer)
            {
            }

            public TestBsonDocumentBackedClass(BsonDocument backingDocument, IBsonDocumentSerializer serializer)
                : base(backingDocument, serializer)
            {
            }

            public BsonDocument GetBackingDocument()
            {
                return BackingDocument;
            }

            public new T GetValue<T>(string memberName)
            {
                return base.GetValue<T>(memberName);
            }

            public new T GetValue<T>(string memberName, T defaultValue)
            {
                return base.GetValue<T>(memberName, defaultValue);
            }

            public new void SetValue(string memberName, object value)
            {
                base.SetValue(memberName, value);
            }
        }
    }
}
