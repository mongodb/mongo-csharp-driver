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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class ChangeStreamCursorTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var cursor = new Mock<IAsyncCursor<RawBsonDocument>>().Object;
            var documentSerializer = new Mock<IBsonSerializer<BsonDocument>>().Object;
            var binding = new Mock<IReadBinding>().Object;
            var changeStreamOperation = CreateChangeStreamOperation();

            var subject = new ChangeStreamCursor<BsonDocument>(cursor, documentSerializer, binding, changeStreamOperation);

            subject._binding().Should().BeSameAs(binding);
            subject._changeStreamOperation().Should().BeSameAs(changeStreamOperation);
            subject._current().Should().BeNull();
            subject._cursor().Should().BeSameAs(cursor);
            subject._disposed().Should().BeFalse();
            subject._documentSerializer().Should().BeSameAs(documentSerializer);
        }

        [Fact]
        public void constructor_should_throw_when_cursor_is_null()
        {
            var documentSerializer = new Mock<IBsonSerializer<BsonDocument>>().Object;
            var binding = new Mock<IReadBinding>().Object;
            var changeStreamOperation = CreateChangeStreamOperation();

            var exception = Record.Exception(() => new ChangeStreamCursor<BsonDocument>(null, documentSerializer, binding, changeStreamOperation));

            var argumnetNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumnetNullException.ParamName.Should().Be("cursor");
        }

        [Fact]
        public void constructor_should_throw_when_documentSerializer_is_null()
        {
            var cursor = new Mock<IAsyncCursor<RawBsonDocument>>().Object;
            var binding = new Mock<IReadBinding>().Object;
            var changeStreamOperation = CreateChangeStreamOperation();

            var exception = Record.Exception(() => new ChangeStreamCursor<BsonDocument>(cursor, null, binding, changeStreamOperation));

            var argumnetNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumnetNullException.ParamName.Should().Be("documentSerializer");
        }

        [Fact]
        public void constructor_should_throw_when_binding_is_null()
        {
            var cursor = new Mock<IAsyncCursor<RawBsonDocument>>().Object;
            var documentSerializer = new Mock<IBsonSerializer<BsonDocument>>().Object;
            var changeStreamOperation = CreateChangeStreamOperation();

            var exception = Record.Exception(() => new ChangeStreamCursor<BsonDocument>(cursor, documentSerializer, null, changeStreamOperation));

            var argumnetNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumnetNullException.ParamName.Should().Be("binding");
        }

        [Fact]
        public void constructor_should_throw_when_changeStreamOperation_is_null()
        {
            var cursor = new Mock<IAsyncCursor<RawBsonDocument>>().Object;
            var documentSerializer = new Mock<IBsonSerializer<BsonDocument>>().Object;
            var binding = new Mock<IReadBinding>().Object;

            var exception = Record.Exception(() => new ChangeStreamCursor<BsonDocument>(cursor, documentSerializer, binding, null));

            var argumnetNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumnetNullException.ParamName.Should().Be("changeStreamOperation");
        }

        [Fact]
        public void Dispose_should_set_disposed_to_true()
        {
            var subject = CreateSubject();

            subject.Dispose();

            subject._disposed().Should().BeTrue();
        }

        [Fact]
        public void Dispose_should_call_Dispose_on_cursor()
        {
            var mockCursor = new Mock<IAsyncCursor<RawBsonDocument>>();
            var subject = CreateSubject(cursor: mockCursor.Object);

            subject.Dispose();

            mockCursor.Verify(s => s.Dispose(), Times.Once);
        }

        [Fact]
        public void Dispose_should_call_Dispose_on_binding()
        {
            var mockBinding = new Mock<IReadBinding>();
            var subject = CreateSubject(binding: mockBinding.Object);

            subject.Dispose();

            mockBinding.Verify(s => s.Dispose(), Times.Once);
        }

        [Fact]
        public void Dispose_can_be_called_more_than_once()
        {
            var mockCursor = new Mock<IAsyncCursor<RawBsonDocument>>();
            var mockBinding = new Mock<IReadBinding>();
            var subject = CreateSubject(cursor: mockCursor.Object, binding: mockBinding.Object);

            subject.Dispose();
            subject.Dispose();

            mockCursor.Verify(s => s.Dispose(), Times.Once);
            mockBinding.Verify(s => s.Dispose(), Times.Once);
        }

        [Theory]
        [ParameterAttributeData]
        public void MoveNext_should_call_MoveNext_on_cursor(
            [Values(false, true)] bool expectedResult,
            [Values(false, true)] bool async)
        {
            var mockCursor = new Mock<IAsyncCursor<RawBsonDocument>>();
            var subject = CreateSubject(cursor: mockCursor.Object);
            var cancellationToken = new CancellationTokenSource().Token;

            bool result;
            if (async)
            {
                mockCursor.Setup(c => c.MoveNextAsync(cancellationToken)).Returns(Task.FromResult(expectedResult));

                result = subject.MoveNextAsync(cancellationToken).GetAwaiter().GetResult();

                mockCursor.Verify(c => c.MoveNextAsync(cancellationToken), Times.Once);
            }
            else
            {
                mockCursor.Setup(c => c.MoveNext(cancellationToken)).Returns(expectedResult);

                result = subject.MoveNext(cancellationToken);

                mockCursor.Verify(c => c.MoveNext(cancellationToken), Times.Once);
            }

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void MoveNext_should_call_Resume_after_resumable_exception(
            [Values(typeof(MongoConnectionException), typeof(MongoNotPrimaryException), typeof(MongoCursorNotFoundException))] Type resumableExceptionType,
            [Values(false, true)] bool expectedResult,
            [Values(false, true)] bool async)
        {
            var mockCursor = new Mock<IAsyncCursor<RawBsonDocument>>();
            var mockBinding = new Mock<IReadBinding>();
            var mockOperation = new Mock<IChangeStreamOperation<BsonDocument>>();
            var subject = CreateSubject(cursor: mockCursor.Object, binding: mockBinding.Object, changeStreamOperation: mockOperation.Object);
            var cancellationToken = new CancellationTokenSource().Token;
            var resumableException = CoreExceptionHelper.CreateException(resumableExceptionType);
            var mockResumedCursor = new Mock<IAsyncCursor<RawBsonDocument>>();

            // process the first batch so that we have a resume token
            var resumeToken = BsonDocument.Parse("{ resumeToken : 1 }");
            var firstDocument = BsonDocument.Parse("{ _id : { resumeToken : 1 }, operationType : \"insert\", ns : { db : \"db\", coll : \"coll\" }, documentKey : { _id : 1 }, fullDocument : { _id : 1 } }");
            var firstBatch = new[] { ToRawDocument(firstDocument) };
            mockCursor.Setup(c => c.MoveNext(cancellationToken)).Returns(true);
            mockCursor.SetupGet(c => c.Current).Returns(firstBatch);
            subject.MoveNext(cancellationToken);

            bool result;
            if (async)
            {
                mockCursor.Setup(c => c.MoveNextAsync(cancellationToken)).Returns(CreateFaultedTask<bool>(resumableException));
                mockOperation.Setup(o => o.ResumeAsync(mockBinding.Object, cancellationToken)).Returns(Task.FromResult(mockResumedCursor.Object));
                mockResumedCursor.Setup(c => c.MoveNextAsync(cancellationToken)).Returns(Task.FromResult(expectedResult));

                result = subject.MoveNextAsync(cancellationToken).GetAwaiter().GetResult();

                mockCursor.Verify(c => c.MoveNextAsync(cancellationToken), Times.Once);
                mockOperation.Verify(o => o.ResumeAsync(mockBinding.Object, cancellationToken), Times.Once);
                mockResumedCursor.Verify(c => c.MoveNextAsync(cancellationToken), Times.Once);
            }
            else
            {
                mockCursor.Setup(c => c.MoveNext(cancellationToken)).Throws(resumableException);
                mockOperation.Setup(o => o.Resume(mockBinding.Object, cancellationToken)).Returns(mockResumedCursor.Object);
                mockResumedCursor.Setup(c => c.MoveNext(cancellationToken)).Returns(expectedResult);

                result = subject.MoveNext(cancellationToken);

                mockCursor.Verify(c => c.MoveNext(cancellationToken), Times.Exactly(2));
                mockOperation.Verify(o => o.Resume(mockBinding.Object, cancellationToken), Times.Once);
                mockResumedCursor.Verify(c => c.MoveNext(cancellationToken), Times.Once);
            }

            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        void ProcessBatch_should_deserialize_documents(
            [Values(false, true)] bool async)
        {
            var mockCursor = new Mock<IAsyncCursor<RawBsonDocument>>();
            var mockSerializer = new Mock<IBsonSerializer<BsonDocument>>();
            var subject = CreateSubject(cursor: mockCursor.Object, documentSerializer: mockSerializer.Object);
            var document = BsonDocument.Parse("{ _id : { resumeAfter : 1 }, operationType : \"insert\", ns : { db : \"db\", coll : \"coll\" }, documentKey : { _id : 1 }, fullDocument : { _id : 1 } }");
            var rawDocuments = new[] { ToRawDocument(document) };
            var cancellationToken = new CancellationTokenSource().Token;

            mockCursor.SetupGet(c => c.Current).Returns(rawDocuments);
            mockSerializer.Setup(s => s.Deserialize(It.IsAny<BsonDeserializationContext>(), It.IsAny<BsonDeserializationArgs>())).Returns(document);

            bool result;
            if (async)
            {
                mockCursor.Setup(c => c.MoveNextAsync(cancellationToken)).Returns(Task.FromResult(true));

                result = subject.MoveNextAsync(cancellationToken).GetAwaiter().GetResult();
            }
            else
            {
                mockCursor.Setup(c => c.MoveNext(cancellationToken)).Returns(true);

                result = subject.MoveNext(cancellationToken);
            }

            result.Should().BeTrue();
            subject.Current.Should().Equal(new[] { document });
            mockSerializer.Verify(s => s.Deserialize(It.IsAny<BsonDeserializationContext>(), It.IsAny<BsonDeserializationArgs>()), Times.Once);
        }

        [Theory]
        [ParameterAttributeData]
        void ProcessBatch_should_Dispose_rawDocuments(
            [Values(false, true)] bool async)
        {
            var mockCursor = new Mock<IAsyncCursor<RawBsonDocument>>();
            var mockSerializer = new Mock<IBsonSerializer<BsonDocument>>();
            var subject = CreateSubject(cursor: mockCursor.Object, documentSerializer: mockSerializer.Object);
            var document = BsonDocument.Parse("{ _id : { resumeAfter : 1 }, operationType : \"insert\", ns : { db : \"db\", coll : \"coll\" }, documentKey : { _id : 1 }, fullDocument : { _id : 1 } }");
            var rawDocument = ToRawDocument(document);
            var rawDocuments = new[] { rawDocument };
            var cancellationToken = new CancellationTokenSource().Token;

            mockCursor.SetupGet(c => c.Current).Returns(rawDocuments);
            mockSerializer.Setup(s => s.Deserialize(It.IsAny<BsonDeserializationContext>(), It.IsAny<BsonDeserializationArgs>())).Returns(document);

            bool result;
            if (async)
            {
                mockCursor.Setup(c => c.MoveNextAsync(cancellationToken)).Returns(Task.FromResult(true));

                result = subject.MoveNextAsync(cancellationToken).GetAwaiter().GetResult();
            }
            else
            {
                mockCursor.Setup(c => c.MoveNext(cancellationToken)).Returns(true);

                result = subject.MoveNext(cancellationToken);
            }

            var exception = Record.Exception(() => rawDocument.Contains("x"));
            exception.Should().BeOfType<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        void ProcessBatch_should_save_documentResumeToken(
            [Values(false, true)] bool async)
        {
            var mockCursor = new Mock<IAsyncCursor<RawBsonDocument>>();
            var mockSerializer = new Mock<IBsonSerializer<BsonDocument>>();
            var subject = CreateSubject(cursor: mockCursor.Object, documentSerializer: mockSerializer.Object);
            var document = BsonDocument.Parse("{ _id : { resumeAfter : 1 }, operationType : \"insert\", ns : { db : \"db\", coll : \"coll\" }, documentKey : { _id : 1 }, fullDocument : { _id : 1 } }");
            var rawDocuments = new[] { ToRawDocument(document) };
            var cancellationToken = new CancellationTokenSource().Token;

            mockCursor.SetupGet(c => c.Current).Returns(rawDocuments);
            mockSerializer.Setup(s => s.Deserialize(It.IsAny<BsonDeserializationContext>(), It.IsAny<BsonDeserializationArgs>())).Returns(document);

            bool result;
            if (async)
            {
                mockCursor.Setup(c => c.MoveNextAsync(cancellationToken)).Returns(Task.FromResult(true));

                result = subject.MoveNextAsync(cancellationToken).GetAwaiter().GetResult();
            }
            else
            {
                mockCursor.Setup(c => c.MoveNext(cancellationToken)).Returns(true);

                result = subject.MoveNext(cancellationToken);
            }

            subject._changeStreamOperation().DocumentResumeToken.Should().Be("{ resumeAfter : 1 }");
        }

        [Theory]
        [ParameterAttributeData]
        void ProcessBatch_should_throw_when_resume_token_is_missing(
            [Values(false, true)] bool async)
        {
            var mockCursor = new Mock<IAsyncCursor<RawBsonDocument>>();
            var subject = CreateSubject(cursor: mockCursor.Object);
            var document = BsonDocument.Parse("{ operationType : \"insert\", ns : { db : \"db\", coll : \"coll\" }, documentKey : { _id : 1 }, fullDocument : { _id : 1 } }");
            var rawDocuments = new[] { ToRawDocument(document) };
            var cancellationToken = new CancellationTokenSource().Token;

            mockCursor.SetupGet(c => c.Current).Returns(rawDocuments);

            Exception exception;
            if (async)
            {
                mockCursor.Setup(c => c.MoveNextAsync(cancellationToken)).Returns(Task.FromResult(true));

                exception = Record.Exception(() => subject.MoveNextAsync(cancellationToken).GetAwaiter().GetResult());
            }
            else
            {
                mockCursor.Setup(c => c.MoveNext(cancellationToken)).Returns(true);

                exception = Record.Exception(() => subject.MoveNext(cancellationToken));
            }

            exception.Should().BeOfType<MongoClientException>();
            exception.Message.Should().Be("Cannot provide resume functionality when the resume token is missing.");
        }

        // private methods
        private ChangeStreamOperation<BsonDocument> CreateChangeStreamOperation()
        {
            var collectionNamespace = new CollectionNamespace(new DatabaseNamespace("database"), "collection");
            var pipeline = new BsonDocument[0];
            var resultSerializer = BsonDocumentSerializer.Instance;
            var messageEncoderSettings = new MessageEncoderSettings();
            return new ChangeStreamOperation<BsonDocument>(collectionNamespace, pipeline, resultSerializer, messageEncoderSettings);
        }

        private Task<TResult> CreateFaultedTask<TResult>(Exception exception)
        {
            var completionSource = new TaskCompletionSource<TResult>();
            completionSource.SetException(exception);
            return completionSource.Task;
        }

        private ChangeStreamCursor<BsonDocument> CreateSubject(
            IAsyncCursor<RawBsonDocument> cursor = null,
            IBsonSerializer<BsonDocument> documentSerializer = null,
            IReadBinding binding = null,
            IChangeStreamOperation<BsonDocument> changeStreamOperation = null)
        {
            cursor = cursor ?? new Mock<IAsyncCursor<RawBsonDocument>>().Object;
            documentSerializer = documentSerializer ?? new Mock<IBsonSerializer<BsonDocument>>().Object;
            binding = binding ?? new Mock<IReadBinding>().Object;
            changeStreamOperation = changeStreamOperation ?? Mock.Of<IChangeStreamOperation<BsonDocument>>();
            return new ChangeStreamCursor<BsonDocument>(cursor, documentSerializer, binding, changeStreamOperation);
        }

        private RawBsonDocument ToRawDocument(BsonDocument document)
        {
            using (var reader = new BsonDocumentReader(document))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);
                return RawBsonDocumentSerializer.Instance.Deserialize(context);
            }
        }
    }

    internal static class ChangeStreamCursorReflector
    {
        public static IReadBinding _binding(this ChangeStreamCursor<BsonDocument> cursor)
        {
            var fieldInfo = typeof(ChangeStreamCursor<BsonDocument>).GetField("_binding", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IReadBinding)fieldInfo.GetValue(cursor);
        }

        public static IChangeStreamOperation<BsonDocument> _changeStreamOperation(this ChangeStreamCursor<BsonDocument> cursor)
        {
            var fieldInfo = typeof(ChangeStreamCursor<BsonDocument>).GetField("_changeStreamOperation", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IChangeStreamOperation<BsonDocument>)fieldInfo.GetValue(cursor);
        }

        public static IEnumerable<BsonDocument> _current(this ChangeStreamCursor<BsonDocument> cursor)
        {
            var fieldInfo = typeof(ChangeStreamCursor<BsonDocument>).GetField("_current", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IEnumerable<BsonDocument>)fieldInfo.GetValue(cursor);
        }

        public static IAsyncCursor<RawBsonDocument> _cursor(this ChangeStreamCursor<BsonDocument> cursor)
        {
            var fieldInfo = typeof(ChangeStreamCursor<BsonDocument>).GetField("_cursor", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IAsyncCursor<RawBsonDocument>)fieldInfo.GetValue(cursor);
        }

        public static bool _disposed(this ChangeStreamCursor<BsonDocument> cursor)
        {
            var fieldInfo = typeof(ChangeStreamCursor<BsonDocument>).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)fieldInfo.GetValue(cursor);
        }

        public static IBsonSerializer<BsonDocument> _documentSerializer(this ChangeStreamCursor<BsonDocument> cursor)
        {
            var fieldInfo = typeof(ChangeStreamCursor<BsonDocument>).GetField("_documentSerializer", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IBsonSerializer<BsonDocument>)fieldInfo.GetValue(cursor);
        }
    }
}
