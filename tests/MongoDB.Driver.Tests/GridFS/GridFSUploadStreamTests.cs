/* Copyright 2015-present MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Tests;
using Xunit;
using MongoDB.Driver.GridFS;

namespace MongoDB.Driver.Tests.GridFS
{
    public class GridFSUploadStreamTests
    {
        // public methods
        [Theory]
        [ParameterAttributeData]
        public void CopyTo_should_throw(
            [Values(false, true)] bool async)
        {
            var bucket = CreateBucket();
            var subject = bucket.OpenUploadStream("Filename");

            using (var destination = new MemoryStream())
            {
                Action action;
                if (async)
                {
                    action = () => subject.CopyToAsync(destination).GetAwaiter().GetResult();
                }
                else
                {
                    action = () => subject.CopyTo(destination);
                }

                action.ShouldThrow<NotSupportedException>();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Flush_should_not_throw(
            [Values(false, true)] bool async)
        {
            var bucket = CreateBucket();
            var subject = bucket.OpenUploadStream("Filename");

            Action action;
            if (async)
            {
                action = () => subject.FlushAsync(CancellationToken.None).GetAwaiter().GetResult(); ;
            }
            else
            {
                action = () => subject.Flush();
            }

            action.ShouldNotThrow();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Upload_of_duplicate_file_should_not_invalidate_existing_data(
            [Values(false, true)] bool async)
        {
            var content1 = Enumerable.Repeat((byte)1, 20).ToArray();
            var content2 = Enumerable.Repeat((byte)2, 100).ToArray();

            var fileId = ObjectId.GenerateNewId();
            var fileName = "filename";
            var uploadOptions = new GridFSUploadOptions
            {
                ChunkSizeBytes = 10,
                BatchSize = 5,
            };

            var bucket = CreateBucket();
            if (async)
            {
                await bucket.UploadFromBytesAsync(fileId, fileName, content1, uploadOptions);
            }
            else
            {
                bucket.UploadFromBytes(fileId, fileName, content1, uploadOptions);
            }

            var exception = async
                ? await Record.ExceptionAsync(() =>  bucket.UploadFromBytesAsync(fileId, fileName, content2, uploadOptions))
                : Record.Exception(() => bucket.UploadFromBytes(fileId, fileName, content2, uploadOptions));
            exception.Should().BeOfType<MongoBulkWriteException<BsonDocument>>();

            var uploadedContent = async
                ? await bucket.DownloadAsBytesAsync(fileId)
                : bucket.DownloadAsBytes(fileId);
            uploadedContent.Should().Equal(content1);
        }

        // private methods
        private IGridFSBucket CreateBucket()
        {
            var client = DriverTestConfiguration.Client;
            var databaseNamespace = DriverTestConfiguration.DatabaseNamespace;
            var database = client.GetDatabase(databaseNamespace.DatabaseName);
            return new GridFSBucket(database);
        }
    }
}
