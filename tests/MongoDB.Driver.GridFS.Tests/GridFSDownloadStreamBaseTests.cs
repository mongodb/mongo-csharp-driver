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
    public class GridFSDownloadStreamBaseTests
    {
        // public methods
        [Fact]
        public void CanRead_should_return_true()
        {
            var subject = CreateSubject();

            var result = subject.CanRead;

            result.Should().BeTrue();
        }

        [Fact]
        public void CanWrite_should_return_false()
        {
            var subject = CreateSubject();

            var result = subject.CanWrite;

            result.Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void Close_can_be_called_more_than_once(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();

            if (async)
            {
                subject.CloseAsync().GetAwaiter().GetResult();
                subject.CloseAsync().GetAwaiter().GetResult();
            }
            else
            {
                subject.Close();
                subject.Close();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Close_should_dispose_subject(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();

            if (async)
            {
                subject.CloseAsync().GetAwaiter().GetResult();
            }
            else
            {
                subject.Close();
            }

            subject._disposed().Should().Be(true);
        }

        [Theory]
        [ParameterAttributeData]
        public void Close_with_cancellationToken_can_be_called_more_than_once(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();

            if (async)
            {
                subject.CloseAsync(CancellationToken.None).GetAwaiter().GetResult();
                subject.CloseAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.Close(CancellationToken.None);
                subject.Close(CancellationToken.None);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Close_with_cancellationToken_should_dispose_subject(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();

            if (async)
            {
                subject.CloseAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.Close(CancellationToken.None);
            }

            subject._disposed().Should().Be(true);
        }

        [Fact]
        public void constructor_should_initialize_instance()
        {
            var database = (new Mock<IMongoDatabase> { DefaultValue = DefaultValue.Mock }).Object;
            var bucket = new GridFSBucket<ObjectId>(database);
            var binding = new Mock<IReadBinding>().Object;
            var fileInfo = new GridFSFileInfo<ObjectId>(new BsonDocument(), new GridFSFileInfoSerializer<ObjectId>());

            var result = new FakeGridFSDownloadStream(bucket, binding, fileInfo);

            result.FileInfo.Should().Be(fileInfo);
            result._binding().Should().Be(binding);
            result._bucket().Should().Be(bucket);
            result._disposed().Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void CopyTo_should_copy_stream(
            [Values(0.0, 0.5, 1.0, 1.5, 2.0, 2.5)] double contentSizeMultiple,
            [Values(null, 128)] int? bufferSize,
            [Values(false, true)] bool async)
        {
            RequireServer.Check();
            var bucket = CreateBucket(128);
            var contentSize = (int)(bucket.Options.ChunkSizeBytes * contentSizeMultiple);
            var content = CreateContent(contentSize);
            var id = CreateGridFSFile(bucket, content);
            var subject = bucket.OpenDownloadStream(id);

            using (var destination = new MemoryStream())
            {
                if (async)
                {
                    if (bufferSize.HasValue)
                    {
                        subject.CopyToAsync(destination, bufferSize.Value).GetAwaiter().GetResult();
                    }
                    else
                    {
                        subject.CopyToAsync(destination).GetAwaiter().GetResult();
                    }
                }
                else
                {
                    if (bufferSize.HasValue)
                    {
                        subject.CopyTo(destination, bufferSize.Value);
                    }
                    else
                    {
                        subject.CopyTo(destination);
                    }
                }

                destination.ToArray().Should().Equal(content);
            }
        }

        [Fact]
        public void Dispose_can_be_called_more_than_once()
        {
            var subject = CreateSubject();

            subject.Dispose();
            subject.Dispose();
        }

        [Fact]
        public void Dispose_should_have_expected_result()
        {
            var subject = CreateSubject();

            subject.Dispose();

            subject._disposed().Should().Be(true);
        }

        [Fact]
        public void FileInfo_should_return_expected_result()
        {
            var fileInfo = new GridFSFileInfo<ObjectId>(new BsonDocument(), new GridFSFileInfoSerializer<ObjectId>());
            var subject = CreateSubject(fileInfo: fileInfo);

            var result = subject.FileInfo;

            result.Should().Be(fileInfo);
        }

        [Theory]
        [ParameterAttributeData]
        public void Flush_should_throw(
            [Values(false, true)] bool async)
        {
            var bucket = CreateBucket(128);
            var content = CreateContent();
            var id = CreateGridFSFile(bucket, content);
            var subject = bucket.OpenDownloadStream(id);

            Action action;
            if (async)
            {
                action = () => subject.FlushAsync(CancellationToken.None).GetAwaiter().GetResult(); ;
            }
            else
            {
                action = () => subject.Flush();
            }

            action.ShouldThrow<NotSupportedException>();
        }

        [Fact]
        public void Length_should_return_expected_result()
        {
            var length = 123;
            var fileInfo = new GridFSFileInfo<ObjectId>(new BsonDocument("length", length), new GridFSFileInfoSerializer<ObjectId>());
            var subject = CreateSubject(fileInfo: fileInfo);

            var result = subject.Length;

            result.Should().Be(length);
        }

        [Fact]
        public void SetLength_should_throw()
        {
            var subject = CreateSubject();

            Action action = () => subject.SetLength(0);

            action.ShouldThrow<NotSupportedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Write_should_throw(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var buffer = new byte[0];
            var offset = 0;
            var count = 0;

            Action action;
            if (async)
            {
                action = () => subject.WriteAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.Write(buffer, offset, count);
            }

            action.ShouldThrow<NotSupportedException>();
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

        private GridFSDownloadStreamBase<ObjectId> CreateSubject(GridFSFileInfo<ObjectId> fileInfo = null)
        {
            var database = (new Mock<IMongoDatabase> { DefaultValue = DefaultValue.Mock }).Object;
            var bucket = new GridFSBucket<ObjectId>(database);
            var binding = new Mock<IReadBinding>().Object;
            fileInfo = fileInfo ?? new GridFSFileInfo<ObjectId>(new BsonDocument(), new GridFSFileInfoSerializer<ObjectId>());

            return new FakeGridFSDownloadStream(bucket, binding, fileInfo);
        }

        // nested types
        private class FakeGridFSDownloadStream : GridFSDownloadStreamBase<ObjectId>
        {
            public FakeGridFSDownloadStream(GridFSBucket<ObjectId> bucket, IReadBinding binding, GridFSFileInfo<ObjectId> fileInfo)
                : base(bucket, binding, fileInfo)
            {
            }

            public override bool CanSeek
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override long Position
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }
        }
    }

    internal static class GridFSDownloadStreamBaseExtensions
    {
        public static IReadBinding _binding(this GridFSDownloadStreamBase<ObjectId> stream)
        {
            var fieldInfo = typeof(GridFSDownloadStreamBase<ObjectId>).GetField("_binding", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IReadBinding)fieldInfo.GetValue(stream);
        }

        public static GridFSBucket<ObjectId> _bucket(this GridFSDownloadStreamBase<ObjectId> stream)
        {
            var fieldInfo = typeof(GridFSDownloadStreamBase<ObjectId>).GetField("_bucket", BindingFlags.NonPublic | BindingFlags.Instance);
            return (GridFSBucket<ObjectId>)fieldInfo.GetValue(stream);
        }


        public static bool _disposed(this GridFSDownloadStreamBase<ObjectId> stream)
        {
            var fieldInfo = typeof(GridFSDownloadStreamBase<ObjectId>).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)fieldInfo.GetValue(stream);
        }
    }
}
