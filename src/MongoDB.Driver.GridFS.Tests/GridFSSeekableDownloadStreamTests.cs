/* Copyright 2015 MongoDB Inc.
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
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Tests;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.GridFS.Tests
{
    [TestFixture]
    public class GridFSSeekableDownloadStreamTests
    {
        [Test]
        public void CanSeek_should_return_true()
        {
            var subject = CreateSubject();

            var result = subject.CanSeek;

            result.Should().BeTrue();
        }

        [Test]
        public void constructor_should_initialize_instance()
        {
            var database = Substitute.For<IMongoDatabase>();
            var bucket = new GridFSBucket(database);
            var binding = Substitute.For<IReadBinding>();
            var fileInfo = new GridFSFileInfo(new BsonDocument());

            var result = new GridFSSeekableDownloadStream(bucket, binding, fileInfo);

            result.Position.Should().Be(0);
            result._chunk().Should().BeNull();
            result._n().Should().Be(-1);
        }

        [Test]
        public void Position_get_and_set_should_work(
            [Values(0, 1, 2)] long value)
        {
            var subject = CreateSubject();

            subject.Position = value;
            var result = subject.Position;

            result.Should().Be(value);
        }

        [Test]
        public void Position_set_should_throw_when_value_is_negative()
        {
            var subject = CreateSubject();

            Action action = () => subject.Position = -1;

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("value");
        }

        [Test]
        [RequiresServer]
        public void Read_should_return_expected_result(
            [Values(0.0, 0.5, 1.0, 1.5, 2.0, 2.5)] double fileLengthMultiple,
            [Values(0.0, 0.5)] double positionMultiple,
            [Values(0, 2)] int offset,
            [Values(0.0, 0.5, 1.0)] double countMultiple,
            [Values(false, true)] bool async)
        {
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

        [Test]
        public void Read_should_throw_when_buffer_is_null(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();

            Action action = () => subject.Read(null, 0, 0);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("buffer");
        }

        [TestCase(0, 0, -1, false)]
        [TestCase(0, 0, -1, true)]
        [TestCase(0, 0, 1, false)]
        [TestCase(0, 0, 1, true)]
        [TestCase(1, 0, 2, false)]
        [TestCase(1, 0, 2, true)]
        [TestCase(1, 1, 1, false)]
        [TestCase(1, 1, 1, true)]
        [TestCase(2, 0, 3, false)]
        [TestCase(2, 0, 3, true)]
        [TestCase(2, 1, 2, false)]
        [TestCase(2, 1, 2, true)]
        [TestCase(2, 2, 1, false)]
        [TestCase(2, 2, 1, true)]
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

        [TestCase(0, -1, false)]
        [TestCase(0, -1, true)]
        [TestCase(0, 1, false)]
        [TestCase(0, 1, true)]
        [TestCase(1, 2, false)]
        [TestCase(1, 2, true)]
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

        [TestCase(0, 1, SeekOrigin.Begin, 1)]
        [TestCase(1, 1, SeekOrigin.Current, 2)]
        [TestCase(2, -1, SeekOrigin.End, 1)]
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

        [Test]
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

        private GridFSSeekableDownloadStream CreateSubject(long? length = null)
        {
            var database = Substitute.For<IMongoDatabase>();
            var bucket = new GridFSBucket(database);
            var binding = Substitute.For<IReadBinding>();
            var fileInfoDocument = new BsonDocument
            {
                { "length", () => length.Value, length.HasValue }
            };
            var fileInfo = new GridFSFileInfo(fileInfoDocument);

            return new GridFSSeekableDownloadStream(bucket, binding, fileInfo);
        }
    }

    internal static class GridFSSeekableDownloadStreamExtensions
    {
        public static byte[] _chunk(this GridFSSeekableDownloadStream stream)
        {
            var fieldInfo = typeof(GridFSSeekableDownloadStream).GetField("_chunk", BindingFlags.NonPublic | BindingFlags.Instance);
            return (byte[])fieldInfo.GetValue(stream);
        }

        public static long _n(this GridFSSeekableDownloadStream stream)
        {
            var fieldInfo = typeof(GridFSSeekableDownloadStream).GetField("_n", BindingFlags.NonPublic | BindingFlags.Instance);
            return (long)fieldInfo.GetValue(stream);
        }
    }
}
