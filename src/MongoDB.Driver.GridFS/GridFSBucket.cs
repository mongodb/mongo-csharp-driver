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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.GridFS
{
    /// <summary>
    /// Represents a GridFS bucket.
    /// </summary>
    public class GridFSBucket : IGridFSBucket
    {
        // fields
        private readonly ICluster _cluster;
        private readonly IMongoDatabase _database;
        private int _ensuredIndexes;
        private readonly ImmutableGridFSBucketOptions _options;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GridFSBucket" /> class.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="options">The options.</param>
        public GridFSBucket(IMongoDatabase database, GridFSBucketOptions options = null)
        {
            _database = Ensure.IsNotNull(database, nameof(database));
            _options = options == null ? ImmutableGridFSBucketOptions.Defaults : new ImmutableGridFSBucketOptions(options);

            _cluster = database.Client.Cluster;
            _ensuredIndexes = 0;
        }

        // properties
        /// <inheritdoc />
        public IMongoDatabase Database
        {
            get { return _database; }
        }

        /// <inheritdoc />
        public ImmutableGridFSBucketOptions Options
        {
            get { return _options; }
        }

        // methods
        /// <summary>
        /// Deletes a file from GridFS.
        /// </summary>
        /// <param name="id">The file id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task.</returns>
        [Obsolete("All new GridFS files should use an ObjectId as the Id.")]
        public Task DeleteAsync(BsonValue id, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(id, nameof(id));
            return DeleteHelperAsync(id, cancellationToken);
        }

        /// <inheritdoc />
        public Task DeleteAsync(ObjectId id, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DeleteHelperAsync(new BsonObjectId(id), cancellationToken);
        }

        /// <summary>
        /// Downloads a file stored in GridFS and returns it as a byte array.
        /// </summary>
        /// <param name="id">The file id.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is a byte array containing the contents of the file stored in GridFS.</returns>
        [Obsolete("All new GridFS files should use an ObjectId as the Id.")]
        public Task<byte[]> DownloadAsBytesAsync(BsonValue id, GridFSDownloadOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(id, nameof(id));
            options = options ?? new GridFSDownloadOptions();
            return DownloadAsBytesHelperAsync(id, options, cancellationToken);
        }

        /// <inheritdoc />
        public Task<byte[]> DownloadAsBytesAsync(ObjectId id, GridFSDownloadOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            options = options ?? new GridFSDownloadOptions();
            return DownloadAsBytesHelperAsync(new BsonObjectId(id), options, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<byte[]> DownloadAsBytesByNameAsync(string filename, GridFSDownloadByNameOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(filename, nameof(filename));
            options = options ?? new GridFSDownloadByNameOptions();

            using (var binding = await GetSingleServerReadBindingAsync(cancellationToken).ConfigureAwait(false))
            {
                var fileInfo = await GetFileInfoByNameAsync(binding, filename, options.Revision, cancellationToken).ConfigureAwait(false);
                return await DownloadAsBytesHelperAsync(binding, fileInfo, options, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Downloads a file stored in GridFS and writes the contents to a stream.
        /// </summary>
        /// <param name="id">The file id.</param>
        /// <param name="destination">The destination.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task.</returns>
        [Obsolete("All new GridFS files should use an ObjectId as the Id.")]
        public Task DownloadToStreamAsync(BsonValue id, Stream destination, GridFSDownloadOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(id, nameof(id));
            Ensure.IsNotNull(destination, nameof(destination));
            options = options ?? new GridFSDownloadOptions();
            return DownloadToStreamHelperAsync(id, destination, options, cancellationToken);
        }

        /// <inheritdoc />
        public Task DownloadToStreamAsync(ObjectId id, Stream destination, GridFSDownloadOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(destination, nameof(destination));
            options = options ?? new GridFSDownloadOptions();
            return DownloadToStreamHelperAsync(new BsonObjectId(id), destination, options, cancellationToken);
        }

        /// <inheritdoc />
        public async Task DownloadToStreamByNameAsync(string filename, Stream destination, GridFSDownloadByNameOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(filename, nameof(filename));
            Ensure.IsNotNull(destination, nameof(destination));
            options = options ?? new GridFSDownloadByNameOptions();

            using (var binding = await GetSingleServerReadBindingAsync(cancellationToken).ConfigureAwait(false))
            {
                var fileInfo = await GetFileInfoByNameAsync(binding, filename, options.Revision, cancellationToken).ConfigureAwait(false);
                await DownloadToStreamHelperAsync(binding, fileInfo, destination, options, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task DropAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var filesCollectionNamespace = GetFilesCollectionNamespace();
            var chunksCollectionNamespace = GetChunksCollectionNamespace();
            var messageEncoderSettings = GetMessageEncoderSettings();

            using (var binding = await GetSingleServerReadWriteBindingAsync(cancellationToken).ConfigureAwait(false))
            {
                var filesCollectionDropOperation = new DropCollectionOperation(filesCollectionNamespace, messageEncoderSettings);
                await filesCollectionDropOperation.ExecuteAsync(binding, cancellationToken).ConfigureAwait(false);

                var chunksCollectionDropOperation = new DropCollectionOperation(chunksCollectionNamespace, messageEncoderSettings);
                await chunksCollectionDropOperation.ExecuteAsync(binding, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<IAsyncCursor<GridFSFileInfo>> FindAsync(FilterDefinition<GridFSFileInfo> filter, GridFSFindOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(filter, nameof(filter));
            options = options ?? new GridFSFindOptions();

            var filesCollectionNamespace = GetFilesCollectionNamespace();
            var serializerRegistry = _database.Settings.SerializerRegistry;
            var fileInfoSerializer = serializerRegistry.GetSerializer<GridFSFileInfo>();
            var messageEncoderSettings = GetMessageEncoderSettings();
            var renderedFilter = filter.Render(fileInfoSerializer, serializerRegistry);
            var renderedSort = options.Sort == null ? null : options.Sort.Render(fileInfoSerializer, serializerRegistry);

            var operation = new FindOperation<GridFSFileInfo>(
                filesCollectionNamespace,
                fileInfoSerializer,
                messageEncoderSettings)
            {
                BatchSize = options.BatchSize,
                Filter = renderedFilter,
                Limit = options.Limit,
                MaxTime = options.MaxTime,
                NoCursorTimeout = options.NoCursorTimeout ?? false,
                Skip = options.Skip,
                Sort = renderedSort
            };

            using (var binding = await GetSingleServerReadBindingAsync(cancellationToken).ConfigureAwait(false))
            {
                return await operation.ExecuteAsync(binding, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Opens a Stream that can be used by the application to read data from a GridFS file.
        /// </summary>
        /// <param name="id">The file id.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task whose result is a Stream.</returns>
        [Obsolete("All new GridFS files should use an ObjectId as the Id.")]
        public Task<GridFSDownloadStream> OpenDownloadStreamAsync(BsonValue id, GridFSDownloadOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(id, nameof(id));
            options = options ?? new GridFSDownloadOptions();
            return OpenDownloadStreamHelperAsync(id, options, cancellationToken);
        }

        /// <inheritdoc />
        public Task<GridFSDownloadStream> OpenDownloadStreamAsync(ObjectId id, GridFSDownloadOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            options = options ?? new GridFSDownloadOptions();
            return OpenDownloadStreamHelperAsync(new BsonObjectId(id), options, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<GridFSDownloadStream> OpenDownloadStreamByNameAsync(string filename, GridFSDownloadByNameOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(filename, nameof(filename));
            options = options ?? new GridFSDownloadByNameOptions();

            using (var binding = await GetSingleServerReadBindingAsync(cancellationToken).ConfigureAwait(false))
            {
                var fileInfo = await GetFileInfoByNameAsync(binding, filename, options.Revision, cancellationToken).ConfigureAwait(false);
                return CreateDownloadStream(binding.Fork(), fileInfo, options);
            }
        }

        /// <inheritdoc />
        public async Task<GridFSUploadStream> OpenUploadStreamAsync(string filename, GridFSUploadOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(filename, nameof(filename));
            options = options ?? new GridFSUploadOptions();

            using (var binding = await GetSingleServerReadWriteBindingAsync(cancellationToken).ConfigureAwait(false))
            {
                await EnsureIndexesAsync(binding, cancellationToken).ConfigureAwait(false);

#pragma warning disable 618
                var id = ObjectId.GenerateNewId();
                var chunkSizeBytes = options.ChunkSizeBytes ?? _options.ChunkSizeBytes;
                var batchSize = options.BatchSize ?? (16 * 1024 * 1024 / chunkSizeBytes);

                return new GridFSForwardOnlyUploadStream(
                    this,
                    binding.Fork(),
                    id,
                    filename,
                    options.Metadata,
                    options.Aliases,
                    options.ContentType,
                    chunkSizeBytes,
                    batchSize);
#pragma warning restore
            }
        }

        /// <summary>
        /// Renames a GridFS file.
        /// </summary>
        /// <param name="id">The file id.</param>
        /// <param name="newFilename">The new filename.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task.</returns>
        [Obsolete("All new GridFS files should use an ObjectId as the Id.")]
        public Task RenameAsync(BsonValue id, string newFilename, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(id, nameof(id));
            Ensure.IsNotNull(newFilename, nameof(newFilename));
            return RenameHelperAsync(id, newFilename, cancellationToken);
        }

        /// <inheritdoc />
        public Task RenameAsync(ObjectId id, string newFilename, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(newFilename, nameof(newFilename));
            return RenameHelperAsync(new BsonObjectId(id), newFilename, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<ObjectId> UploadFromBytesAsync(string filename, byte[] source, GridFSUploadOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(filename, nameof(filename));
            Ensure.IsNotNull(source, nameof(source));
            options = options ?? new GridFSUploadOptions();

            using (var sourceStream = new MemoryStream(source))
            {
                return await UploadFromStreamAsync(filename, sourceStream, options, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<ObjectId> UploadFromStreamAsync(string filename, Stream source, GridFSUploadOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(filename, nameof(filename));
            Ensure.IsNotNull(source, nameof(source));
            options = options ?? new GridFSUploadOptions();

            using (var destination = await OpenUploadStreamAsync(filename, options, cancellationToken).ConfigureAwait(false))
            {
                var chunkSizeBytes = options.ChunkSizeBytes ?? _options.ChunkSizeBytes;
                var buffer = new byte[chunkSizeBytes];

                while (true)
                {
                    int bytesRead = 0;
                    Exception sourceException = null;
                    try
                    {
                        bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        // cannot await in the body of a catch clause
                        sourceException = ex;
                    }
                    if (sourceException != null)
                    {
                        try
                        {
                            await destination.AbortAsync().ConfigureAwait(false);
                        }
                        catch
                        {
                            // ignore any exceptions because we're going to rethrow the original exception
                        }
                        throw sourceException;
                    }
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                }

                await destination.CloseAsync(cancellationToken).ConfigureAwait(false);

                return destination.Id;
            }
        }

        // private methods
        private async Task CreateChunksCollectionIndexesAsync(IReadWriteBindingHandle binding, CancellationToken cancellationToken)
        {
            var collectionNamespace = GetChunksCollectionNamespace();
            var requests = new[] { new CreateIndexRequest(new BsonDocument { { "files_id", 1 }, { "n", 1 } }) { Unique = true } };
            var messageEncoderSettings = GetMessageEncoderSettings();
            var operation = new CreateIndexesOperation(collectionNamespace, requests, messageEncoderSettings);
            await operation.ExecuteAsync(binding, cancellationToken).ConfigureAwait(false);
        }

        private GridFSDownloadStream CreateDownloadStream(IReadBindingHandle binding, GridFSFileInfo fileInfo, GridFSDownloadOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var checkMD5 = options.CheckMD5 ?? false;
            var seekable = options.Seekable ?? false;
            if (checkMD5 && seekable)
            {
                throw new ArgumentException("CheckMD5 can only be used when Seekable is false.");
            }

            if (seekable)
            {
                return new GridFSSeekableDownloadStream(this, binding, fileInfo);
            }
            else
            {
                return new GridFSForwardOnlyDownloadStream(this, binding, fileInfo, checkMD5);
            }
        }

        private async Task CreateFilesCollectionIndexesAsync(IReadWriteBindingHandle binding, CancellationToken cancellationToken)
        {
            var collectionNamespace = GetFilesCollectionNamespace();
            var requests = new[] { new CreateIndexRequest(new BsonDocument { { "filename", 1 }, { "uploadDate", 1 } }) };
            var messageEncoderSettings = GetMessageEncoderSettings();
            var operation = new CreateIndexesOperation(collectionNamespace, requests, messageEncoderSettings);
            await operation.ExecuteAsync(binding, cancellationToken).ConfigureAwait(false);
        }

        private async Task DeleteHelperAsync(BsonValue id, CancellationToken cancellationToken = default(CancellationToken))
        {
            var filesCollectionNamespace = GetFilesCollectionNamespace();
            var chunksCollectionNamespace = GetChunksCollectionNamespace();
            var messageEncoderSettings = GetMessageEncoderSettings();

            using (var binding = await GetSingleServerReadWriteBindingAsync(cancellationToken).ConfigureAwait(false))
            {
                var filesCollectionDeleteRequests = new[] { new DeleteRequest(new BsonDocument("_id", id)) };
                var filesCollectionDeleteOperation = new BulkMixedWriteOperation(
                    filesCollectionNamespace,
                    filesCollectionDeleteRequests,
                    messageEncoderSettings);
                var filesCollectionDeleteResult = await filesCollectionDeleteOperation.ExecuteAsync(binding, cancellationToken).ConfigureAwait(false);

                var chunksDeleteRequests = new[] { new DeleteRequest(new BsonDocument("files_id", id)) { Limit = 0 } };
                var chunksDeleteOperation = new BulkMixedWriteOperation(
                    chunksCollectionNamespace,
                    chunksDeleteRequests,
                    messageEncoderSettings);
                await chunksDeleteOperation.ExecuteAsync(binding, cancellationToken).ConfigureAwait(false);

                if (filesCollectionDeleteResult.DeletedCount == 0)
                {
                    throw new GridFSFileNotFoundException(id);
                }
            }
        }

        private async Task<byte[]> DownloadAsBytesHelperAsync(BsonValue id, GridFSDownloadOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var binding = await GetSingleServerReadBindingAsync(cancellationToken).ConfigureAwait(false))
            {
                var fileInfo = await GetFileInfoAsync(binding, id, cancellationToken).ConfigureAwait(false);
                return await DownloadAsBytesHelperAsync(binding, fileInfo, options, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<byte[]> DownloadAsBytesHelperAsync(IReadBindingHandle binding, GridFSFileInfo fileInfo, GridFSDownloadOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (fileInfo.Length > int.MaxValue)
            {
                throw new NotSupportedException("GridFS stored file is too large to be returned as a byte array.");
            }

            using (var destination = new MemoryStream((int)fileInfo.Length))
            {
                await DownloadToStreamHelperAsync(binding, fileInfo, destination, options, cancellationToken).ConfigureAwait(false);
                return destination.GetBuffer();
            }
        }

        private async Task DownloadToStreamHelperAsync(IReadBindingHandle binding, GridFSFileInfo fileInfo, Stream destination, GridFSDownloadOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var checkMD5 = options.CheckMD5 ?? false;

            using (var source = new GridFSForwardOnlyDownloadStream(this, binding.Fork(), fileInfo, checkMD5))
            {
                var count = source.Length;
                var buffer = new byte[fileInfo.ChunkSizeBytes];

                while (count > 0)
                {
                    var partialCount = (int)Math.Min(buffer.Length, count);
                    await source.ReadBytesAsync(buffer, 0, partialCount, cancellationToken).ConfigureAwait(false);
                    await destination.WriteAsync(buffer, 0, partialCount, cancellationToken).ConfigureAwait(false);
                    count -= partialCount;
                }

                await source.CloseAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task DownloadToStreamHelperAsync(BsonValue id, Stream destination, GridFSDownloadOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var binding = await GetSingleServerReadBindingAsync(cancellationToken).ConfigureAwait(false))
            {
                var fileInfo = await GetFileInfoAsync(binding, id, cancellationToken).ConfigureAwait(false);
                await DownloadToStreamHelperAsync(binding, fileInfo, destination, options, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task EnsureIndexesAsync(IReadWriteBindingHandle binding, CancellationToken cancellationToken)
        {
            var ensuredIndexes = Interlocked.CompareExchange(ref _ensuredIndexes, 0, 0);
            if (ensuredIndexes == 0)
            {
                var isFilesCollectionEmpty = await IsFilesCollectionEmptyAsync(binding, cancellationToken).ConfigureAwait(false);
                if (isFilesCollectionEmpty)
                {
                    await CreateFilesCollectionIndexesAsync(binding, cancellationToken).ConfigureAwait(false);
                    await CreateChunksCollectionIndexesAsync(binding, cancellationToken).ConfigureAwait(false);
                }

                Interlocked.Exchange(ref _ensuredIndexes, 1);
            }
        }

        internal CollectionNamespace GetChunksCollectionNamespace()
        {
            return new CollectionNamespace(_database.DatabaseNamespace, _options.BucketName + ".chunks");
        }

        private async Task<GridFSFileInfo> GetFileInfoAsync(IReadBindingHandle binding, BsonValue id, CancellationToken cancellationToken)
        {
            var filesCollectionNamespace = GetFilesCollectionNamespace();
            var serializerRegistry = _database.Settings.SerializerRegistry;
            var fileInfoSerializer = serializerRegistry.GetSerializer<GridFSFileInfo>();
            var messageEncoderSettings = GetMessageEncoderSettings();
            var filter = new BsonDocument("_id", id);

            var operation = new FindOperation<GridFSFileInfo>(
                filesCollectionNamespace,
                fileInfoSerializer,
                messageEncoderSettings)
            {
                Filter = filter,
                Limit = 1,
                SingleBatch = true
            };

            using (var cursor = await operation.ExecuteAsync(binding, cancellationToken).ConfigureAwait(false))
            {
                var fileInfoList = await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);
                var fileInfo = fileInfoList.FirstOrDefault();
                if (fileInfo == null)
                {
                    throw new GridFSFileNotFoundException(id);
                }
                return fileInfo;
            }
        }

        private async Task<GridFSFileInfo> GetFileInfoByNameAsync(IReadBindingHandle binding, string filename, int revision, CancellationToken cancellationToken)
        {
            var collectionNamespace = GetFilesCollectionNamespace();
            var serializerRegistry = _database.Settings.SerializerRegistry;
            var fileInfoSerializer = serializerRegistry.GetSerializer<GridFSFileInfo>();
            var messageEncoderSettings = GetMessageEncoderSettings();
            var filter = new BsonDocument("filename", filename);
            var skip = revision >= 0 ? revision : -revision - 1;
            var limit = 1;
            var sort = new BsonDocument("uploadDate", revision >= 0 ? 1 : -1);

            var operation = new FindOperation<GridFSFileInfo>(
                collectionNamespace,
                fileInfoSerializer,
                messageEncoderSettings)
            {
                Filter = filter,
                Limit = limit,
                Skip = skip,
                Sort = sort
            };

            using (var cursor = await operation.ExecuteAsync(binding, cancellationToken).ConfigureAwait(false))
            {
                var fileInfoList = await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);
                var fileInfo = fileInfoList.FirstOrDefault();
                if (fileInfo == null)
                {
                    throw new GridFSFileNotFoundException(filename, revision);
                }
                return fileInfo;
            }
        }

        internal CollectionNamespace GetFilesCollectionNamespace()
        {
            return new CollectionNamespace(_database.DatabaseNamespace, _options.BucketName + ".files");
        }

        internal MessageEncoderSettings GetMessageEncoderSettings()
        {
            return new MessageEncoderSettings
            {
                { MessageEncoderSettingsName.GuidRepresentation, _database.Settings.GuidRepresentation },
                { MessageEncoderSettingsName.ReadEncoding,  _database.Settings.ReadEncoding ?? Utf8Encodings.Strict },
                { MessageEncoderSettingsName.WriteEncoding,  _database.Settings.WriteEncoding ?? Utf8Encodings.Strict }
            };
        }

        private async Task<IReadBindingHandle> GetSingleServerReadBindingAsync(CancellationToken cancellationToken)
        {
            var readPreference = _options.ReadPreference ?? _database.Settings.ReadPreference;
            var selector = new ReadPreferenceServerSelector(readPreference);
            var server = await _cluster.SelectServerAsync(selector, cancellationToken).ConfigureAwait(false);
            var binding = new SingleServerReadBinding(server, readPreference);
            return new ReadBindingHandle(binding);
        }

        private async Task<IReadWriteBindingHandle> GetSingleServerReadWriteBindingAsync(CancellationToken cancellationToken)
        {
            var selector = WritableServerSelector.Instance;
            var server = await _cluster.SelectServerAsync(selector, cancellationToken).ConfigureAwait(false);
            var binding = new SingleServerReadWriteBinding(server);
            return new ReadWriteBindingHandle(binding);
        }

        private async Task<bool> IsFilesCollectionEmptyAsync(IReadWriteBindingHandle binding, CancellationToken cancellationToken)
        {
            var filesCollectionNamespace = GetFilesCollectionNamespace();
            var messageEncoderSettings = GetMessageEncoderSettings();
            var operation = new FindOperation<BsonDocument>(filesCollectionNamespace, BsonDocumentSerializer.Instance, messageEncoderSettings)
            {
                Limit = 1,
                SingleBatch = true,
                Projection = new BsonDocument("_id", 1)
            };

            using (var cursor = await operation.ExecuteAsync(binding, cancellationToken).ConfigureAwait(false))
            {
                if (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                {
                    var batch = cursor.Current;
                    if (batch.Count() > 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private async Task<GridFSDownloadStream> OpenDownloadStreamHelperAsync(BsonValue id, GridFSDownloadOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var binding = await GetSingleServerReadBindingAsync(cancellationToken).ConfigureAwait(false))
            {
                var fileInfo = await GetFileInfoAsync(binding, id, cancellationToken).ConfigureAwait(false);
                return CreateDownloadStream(binding.Fork(), fileInfo, options, cancellationToken);
            }
        }

        private async Task RenameHelperAsync(BsonValue id, string newFilename, CancellationToken cancellationToken = default(CancellationToken))
        {
            var filesCollectionNamespace = GetFilesCollectionNamespace();
            var filter = new BsonDocument("_id", id);
            var update = new BsonDocument("$set", new BsonDocument("filename", newFilename));
            var requests = new[] { new UpdateRequest(UpdateType.Update, filter, update) };
            var messageEncoderSettings = GetMessageEncoderSettings();
            var updateOperation = new BulkMixedWriteOperation(filesCollectionNamespace, requests, messageEncoderSettings);

            using (var binding = await GetSingleServerReadWriteBindingAsync(cancellationToken).ConfigureAwait(false))
            {
                var result = await updateOperation.ExecuteAsync(binding, cancellationToken).ConfigureAwait(false);

                if (result.IsModifiedCountAvailable && result.ModifiedCount == 0)
                {
                    throw new GridFSFileNotFoundException(id);
                }
            }
        }
    }
}
