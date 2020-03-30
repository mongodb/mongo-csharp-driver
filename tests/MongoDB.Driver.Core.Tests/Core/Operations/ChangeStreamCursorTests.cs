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
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class ChangeStreamCursorTests : OperationTestBase
    {
        #region static
        private static readonly SemanticVersion __dummyServerVersion = new SemanticVersion(0, 0, 0);
        #endregion

        [Theory]
        [InlineData("{ 'd' : '4' }", "{ 'a' : '1' }", "{ 'b' : '2' }", 3L, "{ 'c' : '3' }", null, "{ 'd' : '4' }", null)]
        [InlineData(null, "{ 'a' : '1' }", "{ 'b' : '2' }", 3L, "{ 'c' : '3' }", null, "{ 'c' : '3' }", null)]
        [InlineData(null, "{ 'a' : '1' }", "{ 'b' : '2' }", 3L, null, null, "{ 'b' : '2' }", null)]
        [InlineData(null, "{ 'a' : '1' }", null, 3L, null, null, "{ 'a' : '1' }", null)]
        [InlineData(null, null, null, 3L, null, null, null, 3L)]
        [InlineData(null, null, null, null, null, 4L, null, 4L)]
        public void ChangeStreamOperation_should_have_expected_change_stream_operation_options_for_resume_process_after_resumable_error(
            string postBatchResumeTokenJson,
            string resumeAfterJson,
            string startAfterJson,
            object startAtOperationTimeValue,
            string documentResumeTokenJson,
            object initialOperationTimeObj,
            string expectedResumeAfter,
            object expectedStartAtOperationTimeValue)
        {
            var postBatchResumeToken = postBatchResumeTokenJson != null ? BsonDocument.Parse(postBatchResumeTokenJson) : null;
            var resumeAfter = resumeAfterJson != null ? BsonDocument.Parse(resumeAfterJson) : null;
            var startAfter = startAfterJson != null ? BsonDocument.Parse(startAfterJson) : null;
            var startAtOperationTime = startAtOperationTimeValue != null ? BsonTimestamp.Create(startAtOperationTimeValue) : null;
            var documentResumeToken = documentResumeTokenJson != null ? BsonDocument.Parse(documentResumeTokenJson) : null;
            var initialOperationTime = initialOperationTimeObj != null ? BsonTimestamp.Create(initialOperationTimeObj) : null;

            var mockCursor = CreateMockCursor();
            var subject = CreateSubject(
                cursor: mockCursor.Object,
                startAfter: startAfter,
                resumeAfter: resumeAfter,
                startAtOperationTime: startAtOperationTime,
                postBatchResumeToken: postBatchResumeToken,
                initialOperationTime: initialOperationTime);

            subject._documentResumeToken(documentResumeToken);

            var result = subject.GetResumeValues();

            result.ResumeAfter.Should().Be(expectedResumeAfter != null ? BsonDocument.Parse(expectedResumeAfter) : null);
            result.StartAfter.Should().BeNull();
            result.StartAtOperationTime.Should().Be(expectedStartAtOperationTimeValue != null ? BsonTimestamp.Create(expectedStartAtOperationTimeValue) : null);
        }

        [Fact]
        public void constructor_should_initialize_instance()
        {
            var cursor = new Mock<IAsyncCursor<RawBsonDocument>>().Object;
            var documentSerializer = new Mock<IBsonSerializer<BsonDocument>>().Object;
            var binding = new Mock<IReadBinding>().Object;
            var initialOperationTime = new BsonTimestamp(3L);
            var postBatchResumeToken = new BsonDocument("c", 3);
            var changeStreamOperation = CreateChangeStreamOperation();
            var startAfter = new BsonDocument("a", 1);
            var resumeAfter = new BsonDocument("b", 2);
            var startAtOperationTime = BsonTimestamp.Create(3L);

            var subject = new ChangeStreamCursor<BsonDocument>(
                cursor,
                documentSerializer,
                binding,
                changeStreamOperation,
                postBatchResumeToken,
                initialOperationTime,
                startAfter,
                resumeAfter,
                startAtOperationTime,
                __dummyServerVersion);

            subject._binding().Should().BeSameAs(binding);
            subject._changeStreamOperation().Should().BeSameAs(changeStreamOperation);
            subject._current().Should().BeNull();
            subject._cursor().Should().BeSameAs(cursor);
            subject._disposed().Should().BeFalse();
            subject._documentSerializer().Should().BeSameAs(documentSerializer);
            subject._postBatchResumeToken().Should().BeSameAs(postBatchResumeToken);
            subject._initialOperationTime().Should().BeSameAs(initialOperationTime);
            subject._initialStartAfter().Should().Be(startAfter);
            subject._initialResumeAfter().Should().Be(resumeAfter);
            subject._initialStartAtOperationTime().Should().Be(startAtOperationTime);
            subject._serverVersion().Should().Be(__dummyServerVersion);
        }

        [Fact]
        public void constructor_should_throw_when_cursor_is_null()
        {
            var documentSerializer = new Mock<IBsonSerializer<BsonDocument>>().Object;
            var binding = new Mock<IReadBinding>().Object;
            var initialOperationTime = new BsonTimestamp(3L);
            var postBatchResumeToken = Mock.Of<BsonDocument>();
            var changeStreamOperation = CreateChangeStreamOperation();

            var exception = Record.Exception(() => new ChangeStreamCursor<BsonDocument>(null, documentSerializer, binding, changeStreamOperation, postBatchResumeToken, initialOperationTime, null, null, null, __dummyServerVersion));

            var argumnetNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumnetNullException.ParamName.Should().Be("cursor");
        }

        [Fact]
        public void constructor_should_throw_when_documentSerializer_is_null()
        {
            var cursor = new Mock<IAsyncCursor<RawBsonDocument>>().Object;
            var binding = new Mock<IReadBinding>().Object;
            var initialOperationTime = new BsonTimestamp(3L);
            var postBatchResumeToken = Mock.Of<BsonDocument>();
            var changeStreamOperation = CreateChangeStreamOperation();

            var exception = Record.Exception(() => new ChangeStreamCursor<BsonDocument>(cursor, null, binding, changeStreamOperation, postBatchResumeToken, initialOperationTime, null, null, null, __dummyServerVersion));

            var argumnetNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumnetNullException.ParamName.Should().Be("documentSerializer");
        }

        [Fact]
        public void constructor_should_throw_when_binding_is_null()
        {
            var cursor = new Mock<IAsyncCursor<RawBsonDocument>>().Object;
            var documentSerializer = new Mock<IBsonSerializer<BsonDocument>>().Object;
            var initialOperationTime = new BsonTimestamp(3L);
            var postBatchResumeToken = Mock.Of<BsonDocument>();
            var changeStreamOperation = CreateChangeStreamOperation();

            var exception = Record.Exception(() => new ChangeStreamCursor<BsonDocument>(cursor, documentSerializer, null, changeStreamOperation, postBatchResumeToken, initialOperationTime, null, null, null, __dummyServerVersion));

            var argumnetNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumnetNullException.ParamName.Should().Be("binding");
        }

        [Fact]
        public void constructor_should_throw_when_changeStreamOperation_is_null()
        {
            var cursor = new Mock<IAsyncCursor<RawBsonDocument>>().Object;
            var documentSerializer = new Mock<IBsonSerializer<BsonDocument>>().Object;
            var initialOperationTime = new BsonTimestamp(3L);
            var postBatchResumeToken = Mock.Of<BsonDocument>();
            var binding = new Mock<IReadBinding>().Object;

            var exception = Record.Exception(() => new ChangeStreamCursor<BsonDocument>(cursor, documentSerializer, binding, null, postBatchResumeToken, initialOperationTime, null, null, null, __dummyServerVersion));

            var argumnetNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumnetNullException.ParamName.Should().Be("changeStreamOperation");
        }

        [Fact]
        public void constructor_should_throw_when_serverVersion_is_null()
        {
            var cursor = new Mock<IAsyncCursor<RawBsonDocument>>().Object;
            var documentSerializer = new Mock<IBsonSerializer<BsonDocument>>().Object;
            var initialOperationTime = new BsonTimestamp(3L);
            var postBatchResumeToken = Mock.Of<BsonDocument>();
            var binding = new Mock<IReadBinding>().Object;
            var changeStreamOperation = CreateChangeStreamOperation();

            var exception = Record.Exception(() => new ChangeStreamCursor<BsonDocument>(cursor, documentSerializer, binding, changeStreamOperation, postBatchResumeToken, initialOperationTime, null, null, null, null));

            var argumnetNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumnetNullException.ParamName.Should().Be("serverVersion");
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
        [InlineData("{ a : 1 }", "{ b : 2 }", "{ c : 3 }", "{ d : 4 }", "{ a : 1 }")]
        [InlineData(null, "{ b : 2 }", "{ c : 3 }", "{ d : 4 }", "{ b : 2 }")]
        [InlineData(null, null, "{ c : 3 }", "{ d : 4 }", "{ c : 3 }")]
        [InlineData(null, null, null, "{ d : 4 }", "{ d : 4 }")]
        [InlineData(null, null, null, null, null)]
        public void GetResumeToken_should_return_expected_result(
            string postBatchResumeTokenJson,
            string documentResumeTokenJson,
            string startAfterJson,
            string resumeAfterJson,
            string expectedResult)
        {
            var mockCursor = CreateMockCursor();
            var postBatchResumeToken = postBatchResumeTokenJson != null ? BsonDocument.Parse(postBatchResumeTokenJson) : null;
            var documentResumeToken = documentResumeTokenJson != null ? BsonDocument.Parse(documentResumeTokenJson) : null;
            var startAfter = startAfterJson != null ? BsonDocument.Parse(startAfterJson) : null;
            var resumeAfter = resumeAfterJson != null ? BsonDocument.Parse(resumeAfterJson) : null;

            var subject = CreateSubject(
                cursor: mockCursor.Object,
                postBatchResumeToken: postBatchResumeToken,
                startAfter: startAfter,
                resumeAfter: resumeAfter);
            subject._documentResumeToken(documentResumeToken);
            subject.GetResumeToken().Should().Be(expectedResult);
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void GetResumeToken_should_return_expected_results_when_batch_is_empty_or_fully_iterated(
            [Values(false, true)] bool async,
            [Values(false, true)] bool withResumeAfter)
        {
            RequireServer.Check().Supports(Feature.ChangeStreamStage).ClusterTypes(ClusterType.ReplicaSet);
            var pipeline = new BsonDocument[0];
            var resultSerializer = new ChangeStreamDocumentSerializer<BsonDocument>(BsonDocumentSerializer.Instance);
            var messageEncoderSettings = new MessageEncoderSettings();
            var subject = new ChangeStreamOperation<ChangeStreamDocument<BsonDocument>>(_collectionNamespace, pipeline, resultSerializer, messageEncoderSettings)
            {
                BatchSize = 2
            };
            EnsureDatabaseExists();
            DropCollection();

            if (withResumeAfter)
            {
                subject.ResumeAfter = GenerateResumeAfterToken(async, true);
            }

            using (var cursor = ExecuteOperation(subject, async))
            using (var enumerator = new AsyncCursorEnumerator<ChangeStreamDocument<BsonDocument>>(cursor, CancellationToken.None))
            {
                var resumeResult = cursor.GetResumeToken();
                // the batch is empty
                if (Feature.ChangeStreamPostBatchResumeToken.IsSupported(CoreTestConfiguration.ServerVersion))
                {
                    var postBatchResumeToken = cursor._postBatchResumeToken();
                    postBatchResumeToken.Should().NotBeNull();
                    resumeResult.Should().Be(postBatchResumeToken);
                }
                else
                {
                    if (withResumeAfter)
                    {
                        resumeResult.Should().Be(subject.ResumeAfter);
                    }
                    else
                    {
                        resumeResult.Should().BeNull();
                    }
                }

                // the batch has been iterated to the last document
                Insert("{ a : 1 }");
                enumerator.MoveNext();
                resumeResult = cursor.GetResumeToken();
                if (Feature.ChangeStreamPostBatchResumeToken.IsSupported(CoreTestConfiguration.ServerVersion))
                {
                    var postBatchResumeToken = cursor._postBatchResumeToken();
                    postBatchResumeToken.Should().NotBeNull();
                    resumeResult.Should().Be(postBatchResumeToken);
                }
                else
                {
                    var documentResumeToken = cursor._documentResumeToken();
                    documentResumeToken.Should().NotBeNull();
                    resumeResult.Should().Be(documentResumeToken);
                }
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void GetResumeToken_should_return_expected_results_when_batch_is_not_empty_and_has_not_been_iterated(
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.ChangeStreamStage).ClusterTypes(ClusterType.ReplicaSet);
            var pipeline = new BsonDocument[0];
            var resultSerializer = new ChangeStreamDocumentSerializer<BsonDocument>(BsonDocumentSerializer.Instance);
            var messageEncoderSettings = new MessageEncoderSettings();
            var subject = new ChangeStreamOperation<ChangeStreamDocument<BsonDocument>>(_collectionNamespace, pipeline, resultSerializer, messageEncoderSettings)
            {
                BatchSize = 2
            };
            EnsureDatabaseExists();
            DropCollection();

            subject.ResumeAfter = GenerateResumeAfterToken(async);

            using (var cursor = ExecuteOperation(subject, async))
            using (var enumerator = new AsyncCursorEnumerator<ChangeStreamDocument<BsonDocument>>(cursor, CancellationToken.None))
            {
                // The batch is not empty.
                // The batch hasn’t been iterated at all.
                // Only the initial aggregate command has been executed.
                var resumeToken = cursor.GetResumeToken();

                resumeToken.Should().Be(subject.ResumeAfter);

                enumerator.MoveNext();
                enumerator.MoveNext(); // `aggregate` passed

                enumerator.MoveNext(); // `getMore`

                resumeToken = cursor.GetResumeToken();

                // The batch is not empty.
                // The batch hasn’t been iterated at all.
                // The stream has iterated beyond a previous batch and a getMore command has just been executed.
                if (Feature.ChangeStreamPostBatchResumeToken.IsSupported(CoreTestConfiguration.ServerVersion))
                {
                    var postBatchResumeToken = cursor._postBatchResumeToken();
                    postBatchResumeToken.Should().NotBeNull();
                    resumeToken.Should().Be(postBatchResumeToken);
                }
                else
                {
                    var documentResumeToken = cursor._documentResumeToken();
                    documentResumeToken.Should().NotBeNull();
                    resumeToken.Should().Be(documentResumeToken);
                }
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void MoveNext_should_call_MoveNext_on_cursor(
            [Values(false, true)] bool expectedResult,
            [Values(false, true)] bool async)
        {
            var mockCursor = CreateMockCursor();
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
            [Values(
                typeof(MongoConnectionException), // network error
                typeof(MongoNotPrimaryException),
                typeof(MongoNodeIsRecoveringException))] Type resumableExceptionType,
            [Values(false, true)] bool expectedResult,
            [Values(false, true)] bool async)
        {
            var mockCursor = CreateMockCursor();
            var mockBinding = new Mock<IReadBinding>();
            var mockOperation = new Mock<IChangeStreamOperation<BsonDocument>>();
            var subject = CreateSubject(cursor: mockCursor.Object, binding: mockBinding.Object, changeStreamOperation: mockOperation.Object);
            var cancellationToken = new CancellationTokenSource().Token;
            var resumableException = CoreExceptionHelper.CreateException(resumableExceptionType);
            var mockResumedCursor = CreateMockCursor();

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
            var mockCursor = CreateMockCursor();
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
            var mockCursor = CreateMockCursor();
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
            var postBatchResumeToken = new BsonDocument("a", 1);
            var mockCursor = CreateMockCursor(postBatchResumeToken);
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

            subject._documentResumeToken().Should().Be("{ resumeAfter : 1 }");
            subject._postBatchResumeToken().Should().Be(postBatchResumeToken);
        }

        [Theory]
        [ParameterAttributeData]
        void ProcessBatch_should_throw_when_resume_token_is_missing(
            [Values(false, true)] bool async)
        {
            var mockCursor = CreateMockCursor();
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

        private Mock<IAsyncCursor<RawBsonDocument>> CreateMockCursor(BsonDocument postBatchResumeToken = null)
        {
            var mockBatchInfo = new Mock<ICursorBatchInfo>();
            mockBatchInfo.Setup(c => c.PostBatchResumeToken).Returns(postBatchResumeToken);
            return mockBatchInfo.As<IAsyncCursor<RawBsonDocument>>();
        }

        private ChangeStreamCursor<BsonDocument> CreateSubject(
            IAsyncCursor<RawBsonDocument> cursor = null,
            IBsonSerializer<BsonDocument> documentSerializer = null,
            IReadBinding binding = null,
            IChangeStreamOperation<BsonDocument> changeStreamOperation = null,
            BsonDocument postBatchResumeToken = null,
            BsonDocument startAfter = null,
            BsonDocument resumeAfter = null,
            BsonTimestamp startAtOperationTime = null,
            BsonTimestamp initialOperationTime = null)
        {
            cursor = cursor ?? new Mock<IAsyncCursor<RawBsonDocument>>().Object;
            documentSerializer = documentSerializer ?? new Mock<IBsonSerializer<BsonDocument>>().Object;
            binding = binding ?? new Mock<IReadBinding>().Object;
            changeStreamOperation = changeStreamOperation ?? Mock.Of<IChangeStreamOperation<BsonDocument>>();
            return new ChangeStreamCursor<BsonDocument>(cursor, documentSerializer, binding, changeStreamOperation, postBatchResumeToken, initialOperationTime, startAfter, resumeAfter, startAtOperationTime, __dummyServerVersion);
        }

        private BsonDocument GenerateResumeAfterToken(bool async, bool shouldBeEmpty = false)
        {
            var pipeline = new BsonDocument[0];
            var resultSerializer = new ChangeStreamDocumentSerializer<BsonDocument>(BsonDocumentSerializer.Instance);
            var messageEncoderSettings = new MessageEncoderSettings();
            var subject = new ChangeStreamOperation<ChangeStreamDocument<BsonDocument>>(_collectionNamespace, pipeline, resultSerializer, messageEncoderSettings);

            subject.BatchSize = 2;
            using (var cursor = ExecuteOperation(subject, async))
            using (var enumerator = new AsyncCursorEnumerator<ChangeStreamDocument<BsonDocument>>(cursor, CancellationToken.None))
            {
                Insert("{ a : 1 }");
                Insert("{ b : 2 }");
                Insert("{ c : 2 }");
                Insert("{ d : 4 }");
                enumerator.MoveNext();
                if (shouldBeEmpty)
                {
                    enumerator.MoveNext();
                    enumerator.MoveNext();
                    enumerator.MoveNext();
                }

                return enumerator.Current.ResumeToken;
            }
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
            return (IReadBinding)Reflector.GetFieldValue(cursor, nameof(_binding));
        }

        public static IChangeStreamOperation<BsonDocument> _changeStreamOperation(this ChangeStreamCursor<BsonDocument> cursor)
        {
            return (IChangeStreamOperation<BsonDocument>)Reflector.GetFieldValue(cursor, nameof(_changeStreamOperation));
        }

        public static IEnumerable<BsonDocument> _current(this ChangeStreamCursor<BsonDocument> cursor)
        {
            return (IEnumerable<BsonDocument>)Reflector.GetFieldValue(cursor, nameof(_current));
        }

        public static IAsyncCursor<RawBsonDocument> _cursor(this ChangeStreamCursor<BsonDocument> cursor)
        {
            return (IAsyncCursor<RawBsonDocument>)Reflector.GetFieldValue(cursor, nameof(_cursor));
        }

        public static bool _disposed(this ChangeStreamCursor<BsonDocument> cursor)
        {
            return (bool)Reflector.GetFieldValue(cursor, nameof(_disposed));
        }

        public static IBsonSerializer<BsonDocument> _documentSerializer(this ChangeStreamCursor<BsonDocument> cursor)
        {
            return (IBsonSerializer<BsonDocument>)Reflector.GetFieldValue(cursor, nameof(_documentSerializer));
        }

        public static BsonDocument _documentResumeToken<TDocument>(this IChangeStreamCursor<TDocument> cursor)
        {
            return (BsonDocument)Reflector.GetFieldValue(cursor, nameof(_documentResumeToken));
        }

        public static void _documentResumeToken<TDocument>(this IChangeStreamCursor<TDocument> cursor, BsonDocument value)
        {
            Reflector.SetFieldValue(cursor, nameof(_documentResumeToken), value);
        }

        public static ChangeStreamCursor<TDocument>.ResumeValues GetResumeValues<TDocument>(this IChangeStreamCursor<TDocument> cursor)
        {
            return (ChangeStreamCursor<TDocument>.ResumeValues)Reflector.Invoke(cursor, nameof(GetResumeValues));
        }

        public static BsonTimestamp _initialOperationTime<TDocument>(this IChangeStreamCursor<TDocument> cursor)
        {
            return (BsonTimestamp)Reflector.GetFieldValue(cursor, nameof(_initialOperationTime));
        }

        public static BsonDocument _postBatchResumeToken<TDocument>(this IChangeStreamCursor<TDocument> cursor)
        {
            return (BsonDocument)Reflector.GetFieldValue(cursor, nameof(_postBatchResumeToken));
        }

        public static BsonDocument _initialResumeAfter<TDocument>(this IChangeStreamCursor<TDocument> cursor)
        {
            return (BsonDocument)Reflector.GetFieldValue(cursor, nameof(_initialResumeAfter));
        }

        public static BsonDocument _initialStartAfter<TDocument>(this IChangeStreamCursor<TDocument> cursor)
        {
            return (BsonDocument)Reflector.GetFieldValue(cursor, nameof(_initialStartAfter));
        }

        public static BsonTimestamp _initialStartAtOperationTime<TDocument>(this IChangeStreamCursor<TDocument> cursor)
        {
            return (BsonTimestamp)Reflector.GetFieldValue(cursor, nameof(_initialStartAtOperationTime));
        }

        public static SemanticVersion _serverVersion<TDocument>(this IChangeStreamCursor<TDocument> cursor)
        {
            return (SemanticVersion)Reflector.GetFieldValue(cursor, nameof(_serverVersion));
        }
    }
}
