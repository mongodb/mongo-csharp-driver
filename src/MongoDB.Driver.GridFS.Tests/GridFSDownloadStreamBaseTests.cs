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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Tests;
using NUnit.Framework;

namespace MongoDB.Driver.GridFS.Tests
{
    [TestFixture]
    public class GridFSDownloadStreamBaseTests
    {
        // public methods
        [Test]
        public void CopyTo_should_copy_stream(
            [Values(0.0, 0.5, 1.0, 1.5, 2.0, 2.5)] double contentSizeMultiple,
            [Values(null, 128)] int? bufferSize,
            [Values(false, true)] bool async)
        {
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

        [Test]
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
    }
}
