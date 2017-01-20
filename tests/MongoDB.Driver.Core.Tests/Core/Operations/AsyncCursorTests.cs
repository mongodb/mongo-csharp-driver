/* Copyright 2015-2016 MongoDB Inc.
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
using System.Reflection;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    // public methods
    public class AsyncCursorTests
    {
        // public methods
        [Fact]
        public void constructor_should_dispose_channel_source_when_cursor_id_is_zero()
        {
            var mockChannelSource = new Mock<IChannelSource>();
            var subject = CreateSubject(cursorId: 0, channelSource: Optional.Create(mockChannelSource.Object));

            mockChannelSource.Verify(s => s.Dispose(), Times.Once);
        }

        [Fact]
        public void constructor_should_initialize_instance()
        {
            var channelSource = new Mock<IChannelSource>().Object;
            var databaseNamespace = new DatabaseNamespace("test");
            var collectionNamespace = new CollectionNamespace(databaseNamespace, "test");
            var query = new BsonDocument("x", 1);
            var firstBatch = new BsonDocument[] { new BsonDocument("y", 2) };
            var cursorId = 1L;
            var batchSize = 2;
            var limit = 3;
            var serializer = BsonDocumentSerializer.Instance;
            var messageEncoderSettings = new MessageEncoderSettings();
            var maxTime = TimeSpan.FromSeconds(1);

            var result = new AsyncCursor<BsonDocument>(
                channelSource,
                collectionNamespace,
                query,
                firstBatch,
                cursorId,
                batchSize,
                limit,
                serializer,
                messageEncoderSettings,
                maxTime);

            var reflector = new Reflector(result);
            reflector.BatchSize.Should().Be(batchSize);
            reflector.ChannelSource.Should().Be(channelSource);
            reflector.CollectionNamespace.Should().Be(collectionNamespace);
            reflector.Count.Should().Be(firstBatch.Length);
            reflector.CurrentBatch.Should().BeNull();
            reflector.CursorId.Should().Be(cursorId);
            reflector.Disposed.Should().BeFalse();
            reflector.FirstBatch.Should().Equal(firstBatch);
            reflector.Limit.Should().Be(limit);
            reflector.MaxTime.Should().Be(maxTime);
            reflector.MessageEncoderSettings.Should().BeEquivalentTo(messageEncoderSettings);
            reflector.Query.Should().Be(query);
            reflector.Serializer.Should().Be(serializer);
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_should_throw_when_batch_size_is_invalid(
            [Values(-1)]
            int value)
        {
            Action action = () => CreateSubject(batchSize: value);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("batchSize");
        }

        [Fact]
        public void constructor_should_throw_when_collection_namespace_is_null()
        {
            Action action = () => CreateSubject(collectionNamespace: null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("collectionNamespace");
        }

        [Fact]
        public void constructor_should_throw_when_first_batch_is_null()
        {
            Action action = () => CreateSubject(firstBatch: null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("firstBatch");
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_should_throw_when_limit_is_invalid(
            [Values(-1)]
            int value)
        {
            Action action = () => CreateSubject(limit: value);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("limit");
        }

        [Fact]
        public void constructor_should_throw_when_query_is_null()
        {
            Action action = () => CreateSubject(query: null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("query");
        }

        [Fact]
        public void constructor_should_throw_when_serializer_is_null()
        {
            Action action = () => CreateSubject(serializer: null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("serializer");
        }

        [Fact]
        public void CreateGetMoreCommand_should_return_expected_result()
        {
            var subject = CreateSubject();
            var reflector = new Reflector(subject);

            var result = reflector.CreateGetMoreCommand();

            result.Should().Be("{ getMore : 0, collection : \"test\" }");
        }

        [Fact]
        public void CreateGetMoreCommand_should_return_expected_result_when_batchSize_is_provided()
        {
            var subject = CreateSubject(batchSize: 2);
            var reflector = new Reflector(subject);

            var result = reflector.CreateGetMoreCommand();

            result.Should().Be("{ getMore : 0, collection : \"test\", batchSize : 2 }");
        }

        [Fact]
        public void CreateGetMoreCommand_should_return_expected_result_when_maxTime_is_provided()
        {
            var subject = CreateSubject(maxTime: TimeSpan.FromSeconds(2));
            var reflector = new Reflector(subject);

            var result = reflector.CreateGetMoreCommand();

            result.Should().Be("{ getMore : 0, collection : \"test\", maxTimeMS : 2000 }");
        }

        [Fact]
        public void Dispose_should_dispose_channel_source_when_cursor_id_is_zero()
        {
            var mockChannelSource = new Mock<IChannelSource>();
            var subject = CreateSubject(cursorId: 1, channelSource: Optional.Create(mockChannelSource.Object));

            subject.Dispose();

            mockChannelSource.Verify(s => s.Dispose(), Times.Once);
        }

        // private methods
        private AsyncCursor<BsonDocument> CreateSubject(
            Optional<IChannelSource> channelSource = default(Optional<IChannelSource>),
            Optional<CollectionNamespace> collectionNamespace = default(Optional<CollectionNamespace>),
            Optional<IBsonSerializer<BsonDocument>> serializer = default(Optional<IBsonSerializer<BsonDocument>>),
            Optional<BsonDocument> query = default(Optional<BsonDocument>),
            Optional<IReadOnlyList<BsonDocument>> firstBatch = default(Optional<IReadOnlyList<BsonDocument>>),
            Optional<long> cursorId = default(Optional<long>),
            Optional<int?> batchSize = default(Optional<int?>),
            Optional<int?> limit = default(Optional<int?>),
            Optional<TimeSpan?> maxTime = default(Optional<TimeSpan?>))
        {
            return new AsyncCursor<BsonDocument>(
                channelSource.WithDefault(new Mock<IChannelSource>().Object),
                collectionNamespace.WithDefault(new CollectionNamespace("test", "test")),
                query.WithDefault(new BsonDocument()),
                firstBatch.WithDefault(new List<BsonDocument>()),
                cursorId.WithDefault(0),
                batchSize.WithDefault(null),
                limit.WithDefault(null),
                serializer.WithDefault(BsonDocumentSerializer.Instance),
                new MessageEncoderSettings(),
                maxTime.WithDefault(null));
        }

        // nested types
        private class Reflector
        {
            // private fields
            private AsyncCursor<BsonDocument> _instance;

            // constructors
            public Reflector(AsyncCursor<BsonDocument> instance)
            {
                _instance = instance;
            }

            // public properties
            public int? BatchSize
            {
                get
                {
                    var fieldInfo = _instance.GetType().GetField("_batchSize", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (int?)fieldInfo.GetValue(_instance);
                }
            }

            public IChannelSource ChannelSource
            {
                get
                {
                    var fieldInfo = _instance.GetType().GetField("_channelSource", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (IChannelSource)fieldInfo.GetValue(_instance);
                }
            }

            public CollectionNamespace CollectionNamespace
            {
                get
                {
                    var fieldInfo = _instance.GetType().GetField("_collectionNamespace", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (CollectionNamespace)fieldInfo.GetValue(_instance);
                }
            }

            public int Count
            {
                get
                {
                    var fieldInfo = _instance.GetType().GetField("_count", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (int)fieldInfo.GetValue(_instance);
                }
            }

            public IReadOnlyList<BsonDocument> CurrentBatch
            {
                get
                {
                    var fieldInfo = _instance.GetType().GetField("_currentBatch", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (IReadOnlyList<BsonDocument>)fieldInfo.GetValue(_instance);
                }
            }

            public long CursorId
            {
                get
                {
                    var fieldInfo = _instance.GetType().GetField("_cursorId", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (long)fieldInfo.GetValue(_instance);
                }
            }

            public bool Disposed
            {
                get
                {
                    var fieldInfo = _instance.GetType().GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (bool)fieldInfo.GetValue(_instance);
                }
            }

            public IReadOnlyList<BsonDocument> FirstBatch
            {
                get
                {
                    var fieldInfo = _instance.GetType().GetField("_firstBatch", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (IReadOnlyList<BsonDocument>)fieldInfo.GetValue(_instance);
                }
            }

            public int Limit
            {
                get
                {
                    var fieldInfo = _instance.GetType().GetField("_limit", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (int)fieldInfo.GetValue(_instance);
                }
            }

            public TimeSpan? MaxTime
            {
                get
                {
                    var fieldInfo = _instance.GetType().GetField("_maxTime", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (TimeSpan?)fieldInfo.GetValue(_instance);
                }
            }

            public MessageEncoderSettings MessageEncoderSettings
            {
                get
                {
                    var fieldInfo = _instance.GetType().GetField("_messageEncoderSettings", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (MessageEncoderSettings)fieldInfo.GetValue(_instance);
                }
            }

            public BsonDocument Query
            {
                get
                {
                    var fieldInfo = _instance.GetType().GetField("_query", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (BsonDocument)fieldInfo.GetValue(_instance);
                }
            }

            public IBsonSerializer<BsonDocument> Serializer
            {
                get
                {
                    var fieldInfo = _instance.GetType().GetField("_serializer", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (IBsonSerializer<BsonDocument>)fieldInfo.GetValue(_instance);
                }
            }

            // public methods
            public BsonDocument CreateGetMoreCommand()
            {
                var methodInfo = _instance.GetType().GetMethod("CreateGetMoreCommand", BindingFlags.NonPublic | BindingFlags.Instance);
                return (BsonDocument)methodInfo.Invoke(_instance, new object[0]);
            }
        }
    }
}
