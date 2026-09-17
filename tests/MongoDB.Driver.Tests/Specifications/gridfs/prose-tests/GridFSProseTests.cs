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

using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.GridFS;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.gridfs.prose_tests;

[Trait("Category", "Integration")]
public class GridFSProseTests
{
    [Theory]
    [ParameterAttributeData]
    public async Task Aborting_an_upload_with_an_injected_file_id_does_not_delete_other_files_chunks(
        [Values(false, true)] bool async)
    {
        RequireServer.Check().VersionGreaterThanOrEqualTo("5.0");

        var database = DriverTestConfiguration.Client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
        var bucketName = "gridfs_injected_id";
        var bucket = new GridFSBucket<BsonValue>(database, new GridFSBucketOptions { BucketName = bucketName });
        var chunksCollection = database.GetCollection<BsonDocument>($"{bucketName}.chunks");

        var file1Bytes = new byte[] { 0x11, 0x22, 0x33 };
        var file2Bytes = new byte[] { 0x44, 0x55, 0x66, 0x77 };
        var injectedId = BsonDocument.Parse("{ $gt : { $minKey : 1 } }");
        var injectedIdChunks = new BsonDocument("files_id", new BsonDocument("$eq", injectedId));
        var uploadOptions = new GridFSUploadOptions { ChunkSizeBytes = 2, BatchSize = 1 };

        if (async)
        {
            await bucket.DropAsync();
            await bucket.UploadFromBytesAsync(new BsonObjectId(ObjectId.GenerateNewId()), "file1", file1Bytes);

            using (var uploadStream = await bucket.OpenUploadStreamAsync(injectedId, "file2", uploadOptions))
            {
                await uploadStream.WriteAsync(file2Bytes, 0, file2Bytes.Length);
                (await chunksCollection.CountDocumentsAsync(injectedIdChunks)).Should().Be(1);

                await uploadStream.AbortAsync();
            }
        }
        else
        {
            bucket.Drop();
            bucket.UploadFromBytes(new BsonObjectId(ObjectId.GenerateNewId()), "file1", file1Bytes);

            using (var uploadStream = bucket.OpenUploadStream(injectedId, "file2", uploadOptions))
            {
                uploadStream.Write(file2Bytes, 0, file2Bytes.Length);
                chunksCollection.CountDocuments(injectedIdChunks).Should().Be(1);

                uploadStream.Abort();
            }
        }

        chunksCollection.CountDocuments(injectedIdChunks).Should().Be(0);
        chunksCollection.CountDocuments("{}").Should().Be(1);

        var downloadedFile1Bytes = async
            ? await bucket.DownloadAsBytesByNameAsync("file1")
            : bucket.DownloadAsBytesByName("file1");
        downloadedFile1Bytes.Should().Equal(file1Bytes);

        var exception = async
            ? await Record.ExceptionAsync(() => bucket.DownloadAsBytesByNameAsync("file2"))
            : Record.Exception(() => bucket.DownloadAsBytesByName("file2"));
        exception.Should().BeOfType<GridFSFileNotFoundException>();
    }
}
