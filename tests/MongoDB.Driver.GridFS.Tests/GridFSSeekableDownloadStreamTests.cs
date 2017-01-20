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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Tests;
using Moq;
using Xunit;

namespace MongoDB.Driver.GridFS.Tests
{
    public class GridFSSeekableDownloadStreamTests
    {
        [Fact]
        public void CanSeek_should_return_true()
        {
            var subject = CreateSubject();

            var result = subject.CanSeek;

            result.Should().BeTrue();
        }

        [Fact]
        public void constructor_should_initialize_instance()
        {
            var database = (new Mock<IMongoDatabase> { DefaultValue = DefaultValue.Mock }).Object;
            var bucket = new GridFSBucket<ObjectId>(database);
            var binding = new Mock<IReadBinding>().Object;
            var fileInfo = new GridFSFileInfo<ObjectId>(new BsonDocument { { "_id", ObjectId.GenerateNewId() } }, new GridFSFileInfoSerializer<ObjectId>());

            var result = new GridFSSeekableDownloadStream<ObjectId>(bucket, binding, fileInfo);

            result.Position.Should().Be(0);
            result._chunk().Should().BeNull();
            result._n().Should().Be(-1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Position_get_and_set_should_work(
            [Values(0, 1, 2)] long value)
        {
            var subject = CreateSubject();

            subject.Position = value;
            var result = subject.Position;

            result.Should().Be(value);
        }

        [Fact]
        public void Position_set_should_throw_when_value_is_negative()
        {
            var subject = CreateSubject();

            Action action = () => subject.Position = -1;

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("value");
        }

        [Theory]
        [ParameterAttributeData]
        public void Read_should_return_expected_result(
            [Values(0.0, 0.5, 1.0, 1.5, 2.0, 2.5)] double fileLengthMultiple,
            [Values(0.0, 0.5)] double positionMultiple,
            [Values(0, 2)] int offset,
            [Values(0.0, 0.5, 1.0)] double countMultiple,
            [Values(false, true)] bool async)
        {
            RequireServer.Check();
            var chunkSize = 4;
            var bucket = CreateBucket(chunkSize);
            var fileLength = (int)(chunkSize * fileLengthMultiple);
            var content = CreateContent(fileLength);
            var id = CreateGridFSFile(bucket, content);
            var options = new GridFSDownloadOptions { Seekable = true };
            var subject = bucket.OpenDownloadStream(id, options);
            var position = (int)(fileLength * positionMultiple);
            subject.Position = position;
            var count = (int)(fileLength * countMultiple);
            var buffer = new byte[offset + count + 1];
            var expectedResult = Math.Min(count, fileLength - position);

            int result;
            if (async)
            {
                result = subject.ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                result = subject.Read(buffer, offset, count);
            }

            result.Should().Be(expectedResult);
            buffer.Take(offset).Any(b => b != 0).Should().BeFalse();
            buffer.Skip(offset).Take(result).Should().Equal(content.Skip(position).Take(result));
            buffer.Last().Should().Be(0);
        }

        [Theory]
        [ParameterAttributeData]
        public void Read_should_throw_when_buffer_is_null(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();

            Action action = () => subject.Read(null, 0, 0);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("buffer");
        }

        [Theory]
        [InlineData(0, 0, -1, false)]
        [InlineData(0, 0, -1, true)]
        [InlineData(0, 0, 1, false)]
        [InlineData(0, 0, 1, true)]
        [InlineData(1, 0, 2, false)]
        [InlineData(1, 0, 2, true)]
        [InlineData(1, 1, 1, false)]
        [InlineData(1, 1, 1, true)]
        [InlineData(2, 0, 3, false)]
        [InlineData(2, 0, 3, true)]
        [InlineData(2, 1, 2, false)]
        [InlineData(2, 1, 2, true)]
        [InlineData(2, 2, 1, false)]
        [InlineData(2, 2, 1, true)]
        public void Read_should_throw_when_count_is_invalid(int bufferLength, int offset, int count, bool async)
        {
            var subject = CreateSubject();
            var buffer = new byte[bufferLength];

            Action action;
            if (async)
            {
                action = () => subject.ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.Read(buffer, offset, count);
            }

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("count");
        }

        [Theory]
        [InlineData(0, -1, false)]
        [InlineData(0, -1, true)]
        [InlineData(0, 1, false)]
        [InlineData(0, 1, true)]
        [InlineData(1, 2, false)]
        [InlineData(1, 2, true)]
        public void Read_should_throw_when_offset_is_invalid(int bufferLength, int offset, bool async)
        {
            var subject = CreateSubject();
            var buffer = new byte[bufferLength];

            Action action;
            if (async)
            {
                action = () => subject.ReadAsync(buffer, offset, 0, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.Read(buffer, offset, 0);
            }

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("offset");
        }

        [Theory]
        [InlineData(0, 1, SeekOrigin.Begin, 1)]
        [InlineData(1, 1, SeekOrigin.Current, 2)]
        [InlineData(2, -1, SeekOrigin.End, 1)]
        public void Seek_should_return_expected_result(
            long position,
            long offset,
            SeekOrigin origin,
            long expectedResult)
        {
            var subject = CreateSubject();
            subject.Position = position;

            var result = subject.Seek(offset, origin);

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void Seek_should_throw_when_new_position_is_negative()
        {
            var subject = CreateSubject();

            Action action = () => subject.Seek(-1, SeekOrigin.Begin);

            action.ShouldThrow<IOException>();
        }

        // private methods
        private IGridFSBucket CreateBucket(int chunkSize)
        {
            var client = DriverTestConfiguration.Client;
            var databaseNamespace = DriverTestConfiguration.DatabaseNamespace;
            var database = client.GetDatabase(databaseNamespace.DatabaseName);
            var bucketOptions = new GridFSBucketOptions { ChunkSizeBytes = chunkSize };
            return new GridFSBucket(database, bucketOptions);
        }

        private byte[] CreateContent(int contentSize = 0)
        {
            return Enumerable.Range(0, contentSize).Select(i => (byte)i).ToArray();
        }

        private ObjectId CreateGridFSFile(IGridFSBucket bucket, byte[] content)
        {
            return bucket.UploadFromBytes("filename", content);
        }

        private GridFSSeekableDownloadStream<ObjectId> CreateSubject(long? length = null)
        {
            var database = (new Mock<IMongoDatabase> { DefaultValue = DefaultValue.Mock }).Object;
            var bucket = new GridFSBucket<ObjectId>(database);
            var binding = new Mock<IReadBinding>().Object;
            var fileInfoDocument = new BsonDocument
            {
                { "_id", ObjectId.Parse("0102030405060708090a0b0c") },
                { "length", () => length.Value, length.HasValue }
            };
            var fileInfo = new GridFSFileInfo<ObjectId>(fileInfoDocument, new GridFSFileInfoSerializer<ObjectId>());

            return new GridFSSeekableDownloadStream<ObjectId>(bucket, binding, fileInfo);
        }
    }

    internal static class GridFSSeekableDownloadStreamExtensions
    {
        public static byte[] _chunk<ObjectId>(this GridFSSeekableDownloadStream<ObjectId> stream)
        {
            var fieldInfo = typeof(GridFSSeekableDownloadStream<ObjectId>).GetField("_chunk", BindingFlags.NonPublic | BindingFlags.Instance);
            return (byte[])fieldInfo.GetValue(stream);
        }

        public static long _n<ObjectId>(this GridFSSeekableDownloadStream<ObjectId> stream)
        {
            var fieldInfo = typeof(GridFSSeekableDownloadStream<ObjectId>).GetField("_n", BindingFlags.NonPublic | BindingFlags.Instance);
            return (long)fieldInfo.GetValue(stream);
        }
    }
}
