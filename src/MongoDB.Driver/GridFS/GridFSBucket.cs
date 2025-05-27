/* Copyright 2016-present MongoDB Inc.
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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
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
    /// <typeparam name="TFileId">The type of the file identifier.</typeparam>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")] // we can get away with not calling Dispose on our SemaphoreSlim
    public class GridFSBucket<TFileId> : IGridFSBucket<TFileId>
    {
        // fields
        private readonly IClusterInternal _cluster;
        private readonly IMongoDatabase _database;
        private bool _ensureIndexesDone;
        private SemaphoreSlim _ensureIndexesSemaphore = new SemaphoreSlim(1);
        private readonly IBsonSerializer<GridFSFileInfo<TFileId>> _fileInfoSerializer;
        private readonly BsonSerializationInfo _idSerializationInfo;
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

            _cluster = database.Client.GetClusterInternal();

            var idSerializer = _options.SerializerRegistry.GetSerializer<TFileId>();
            _idSerializationInfo = new BsonSerializationInfo("_id", idSerializer, typeof(TFileId));
            _fileInfoSerializer = new GridFSFileInfoSerializer<TFileId>(idSerializer);
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

        // public methods
        /// <inheritdoc />
        public void Delete(TFileId id, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull((object)id, nameof(id));
            // TODO: CSOT implement proper way to obtain the operationCancellationContext
            var operationCancellationContext = new OperationCancellationContext(Timeout.InfiniteTimeSpan, cancellationToken);
            using (var binding = GetSingleServerReadWriteBinding(operationCancellationContext))
            {
                var filesCollectionDeleteOperation = CreateDeleteFileOperation(id);
                var filesCollectionDeleteResult = filesCollectionDeleteOperation.Execute(binding, operationCancellationContext);

                var chunksDeleteOperation = CreateDeleteChunksOperation(id);
                chunksDeleteOperation.Execute(binding, operationCancellationContext);

                if (filesCollectionDeleteResult.DeletedCount == 0)
                {
                    throw new GridFSFileNotFoundException(_idSerializationInfo.SerializeValue(id));
                }
            }
        }

        /// <inheritdoc />
        public async Task DeleteAsync(TFileId id, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull((object)id, nameof(id));
            // TODO: CSOT implement proper way to obtain the operationCancellationContext
            var operationCancellationContext = new OperationCancellationContext(Timeout.InfiniteTimeSpan, cancellationToken);
            using (var binding = await GetSingleServerReadWriteBindingAsync(operationCancellationContext).ConfigureAwait(false))
            {
                var filesCollectionDeleteOperation = CreateDeleteFileOperation(id);
                var filesCollectionDeleteResult = await filesCollectionDeleteOperation.ExecuteAsync(binding, operationCancellationContext).ConfigureAwait(false);

                var chunksDeleteOperation = CreateDeleteChunksOperation(id);
                await chunksDeleteOperation.ExecuteAsync(binding, operationCancellationContext).ConfigureAwait(false);

                if (filesCollectionDeleteResult.DeletedCount == 0)
                {
                    throw new GridFSFileNotFoundException(_idSerializationInfo.SerializeValue(id));
                }
            }
        }

        /// <inheritdoc />
        public byte[] DownloadAsBytes(TFileId id, GridFSDownloadOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull((object)id, nameof(id));
            // TODO: CSOT implement proper way to obtain the operationCancellationContext
            var operationCancellationContext = new OperationCancellationContext(Timeout.InfiniteTimeSpan, cancellationToken);
            options = options ?? new GridFSDownloadOptions();
            using (var binding = GetSingleServerReadBinding(operationCancellationContext))
            {
                var fileInfo = GetFileInfo(binding, id, operationCancellationContext);
                return DownloadAsBytesHelper(binding, fileInfo, options, cancellationToken);
            }
        }

        /// <inheritdoc />
        public async Task<byte[]> DownloadAsBytesAsync(TFileId id, GridFSDownloadOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull((object)id, nameof(id));
            // TODO: CSOT implement proper way to obtain the operationCancellationContext
            var operationCancellationContext = new OperationCancellationContext(Timeout.InfiniteTimeSpan, cancellationToken);
            options = options ?? new GridFSDownloadOptions();
            using (var binding = await GetSingleServerReadBindingAsync(operationCancellationContext).ConfigureAwait(false))
            {
                var fileInfo = await GetFileInfoAsync(binding, id, operationCancellationContext).ConfigureAwait(false);
                return await DownloadAsBytesHelperAsync(binding, fileInfo, options, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public byte[] DownloadAsBytesByName(string filename, GridFSDownloadByNameOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(filename, nameof(filename));
            // TODO: CSOT implement proper way to obtain the operationCancellationContext
            var operationCancellationContext = new OperationCancellationContext(Timeout.InfiniteTimeSpan, cancellationToken);
            options = options ?? new GridFSDownloadByNameOptions();

            using (var binding = GetSingleServerReadBinding(operationCancellationContext))
            {
                var fileInfo = GetFileInfoByName(binding, filename, options.Revision, operationCancellationContext);
                return DownloadAsBytesHelper(binding, fileInfo, options, cancellationToken);
            }
        }

        /// <inheritdoc />
        public async Task<byte[]> DownloadAsBytesByNameAsync(string filename, GridFSDownloadByNameOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(filename, nameof(filename));
            // TODO: CSOT implement proper way to obtain the operationCancellationContext
            var operationCancellationContext = new OperationCancellationContext(Timeout.InfiniteTimeSpan, cancellationToken);
            options = options ?? new GridFSDownloadByNameOptions();

            using (var binding = await GetSingleServerReadBindingAsync(operationCancellationContext).ConfigureAwait(false))
            {
                var fileInfo = await GetFileInfoByNameAsync(binding, filename, options.Revision, operationCancellationContext).ConfigureAwait(false);
                return await DownloadAsBytesHelperAsync(binding, fileInfo, options, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public void DownloadToStream(TFileId id, Stream destination, GridFSDownloadOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull((object)id, nameof(id));
            Ensure.IsNotNull(destination, nameof(destination));
            // TODO: CSOT implement proper way to obtain the operationCancellationContext
            var operationCancellationContext = new OperationCancellationContext(Timeout.InfiniteTimeSpan, cancellationToken);
            options = options ?? new GridFSDownloadOptions();
            using (var binding = GetSingleServerReadBinding(operationCancellationContext))
            {
                var fileInfo = GetFileInfo(binding, id, operationCancellationContext);
                DownloadToStreamHelper(binding, fileInfo, destination, options, cancellationToken);
            }
        }

        /// <inheritdoc />
        public async Task DownloadToStreamAsync(TFileId id, Stream destination, GridFSDownloadOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull((object)id, nameof(id));
            Ensure.IsNotNull(destination, nameof(destination));
            // TODO: CSOT implement proper way to obtain the operationCancellationContext
            var operationCancellationContext = new OperationCancellationContext(Timeout.InfiniteTimeSpan, cancellationToken);
            options = options ?? new GridFSDownloadOptions();
            using (var binding = await GetSingleServerReadBindingAsync(operationCancellationContext).ConfigureAwait(false))
            {
                var fileInfo = await GetFileInfoAsync(binding, id, operationCancellationContext).ConfigureAwait(false);
                await DownloadToStreamHelperAsync(binding, fileInfo, destination, options, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public void DownloadToStreamByName(string filename, Stream destination, GridFSDownloadByNameOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(filename, nameof(filename));
            Ensure.IsNotNull(destination, nameof(destination));
            // TODO: CSOT implement proper way to obtain the operationCancellationContext
            var operationCancellationContext = new OperationCancellationContext(Timeout.InfiniteTimeSpan, cancellationToken);
            options = options ?? new GridFSDownloadByNameOptions();

            using (var binding = GetSingleServerReadBinding(operationCancellationContext))
            {
                var fileInfo = GetFileInfoByName(binding, filename, options.Revision, operationCancellationContext);
                DownloadToStreamHelper(binding, fileInfo, destination, options, cancellationToken);
            }
        }

        /// <inheritdoc />
        public async Task DownloadToStreamByNameAsync(string filename, Stream destination, GridFSDownloadByNameOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(filename, nameof(filename));
            Ensure.IsNotNull(destination, nameof(destination));
            // TODO: CSOT implement proper way to obtain the operationCancellationContext
            var operationCancellationContext = new OperationCancellationContext(Timeout.InfiniteTimeSpan, cancellationToken);
            options = options ?? new GridFSDownloadByNameOptions();

            using (var binding = await GetSingleServerReadBindingAsync(operationCancellationContext).ConfigureAwait(false))
            {
                var fileInfo = await GetFileInfoByNameAsync(binding, filename, options.Revision, operationCancellationContext).ConfigureAwait(false);
                await DownloadToStreamHelperAsync(binding, fileInfo, destination, options, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public void Drop(CancellationToken cancellationToken = default(CancellationToken))
        {
            // TODO: CSOT implement proper way to obtain the operationCancellationContext
            var operationCancellationContext = new OperationCancellationContext(Timeout.InfiniteTimeSpan, cancellationToken);
            var filesCollectionNamespace = this.GetFilesCollectionNamespace();
            var chunksCollectionNamespace = this.GetChunksCollectionNamespace();
            var messageEncoderSettings = this.GetMessageEncoderSettings();

            using (var binding = GetSingleServerReadWriteBinding(operationCancellationContext))
            {
                var filesCollectionDropOperation = CreateDropCollectionOperation(filesCollectionNamespace, messageEncoderSettings);
                filesCollectionDropOperation.Execute(binding, operationCancellationContext);

                var chunksCollectionDropOperation = CreateDropCollectionOperation(chunksCollectionNamespace, messageEncoderSettings);
                chunksCollectionDropOperation.Execute(binding, operationCancellationContext);
            }
        }

        /// <inheritdoc />
        public async Task DropAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // TODO: CSOT implement proper way to obtain the operationCancellationContext
            var operationCancellationContext = new OperationCancellationContext(Timeout.InfiniteTimeSpan, cancellationToken);
            var filesCollectionNamespace = this.GetFilesCollectionNamespace();
            var chunksCollectionNamespace = this.GetChunksCollectionNamespace();
            var messageEncoderSettings = this.GetMessageEncoderSettings();

            using (var binding = await GetSingleServerReadWriteBindingAsync(operationCancellationContext).ConfigureAwait(false))
            {
                var filesCollectionDropOperation = CreateDropCollectionOperation(filesCollectionNamespace, messageEncoderSettings);
                await filesCollectionDropOperation.ExecuteAsync(binding, operationCancellationContext).ConfigureAwait(false);

                var chunksCollectionDropOperation = CreateDropCollectionOperation(chunksCollectionNamespace, messageEncoderSettings);
                await chunksCollectionDropOperation.ExecuteAsync(binding, operationCancellationContext).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public IAsyncCursor<GridFSFileInfo<TFileId>> Find(FilterDefinition<GridFSFileInfo<TFileId>> filter, GridFSFindOptions<TFileId> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(filter, nameof(filter));
            // TODO: CSOT implement proper way to obtain the operationCancellationContext
            var operationCancellationContext = new OperationCancellationContext(Timeout.InfiniteTimeSpan, cancellationToken);
            options = options ?? new GridFSFindOptions<TFileId>();

            var translationOptions = _database.Client.Settings.TranslationOptions;
            var operation = CreateFindOperation(filter, options, translationOptions);
            using (var binding = GetSingleServerReadBinding(operationCancellationContext))
            {
                return operation.Execute(binding, operationCancellationContext);
            }
        }

        /// <inheritdoc />
        public async Task<IAsyncCursor<GridFSFileInfo<TFileId>>> FindAsync(FilterDefinition<GridFSFileInfo<TFileId>> filter, GridFSFindOptions<TFileId> options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(filter, nameof(filter));
            // TODO: CSOT implement proper way to obtain the operationCancellationContext
            var operationCancellationContext = new OperationCancellationContext(Timeout.InfiniteTimeSpan, cancellationToken);
            options = options ?? new GridFSFindOptions<TFileId>();

            var translationOptions = _database.Client.Settings.TranslationOptions;
            var operation = CreateFindOperation(filter, options, translationOptions);
            using (var binding = await GetSingleServerReadBindingAsync(operationCancellationContext).ConfigureAwait(false))
            {
                return await operation.ExecuteAsync(binding, operationCancellationContext).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public GridFSDownloadStream<TFileId> OpenDownloadStream(TFileId id, GridFSDownloadOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull((object)id, nameof(id));
            // TODO: CSOT implement proper way to obtain the operationCancellationContext
            var operationCancellationContext = new OperationCancellationContext(Timeout.InfiniteTimeSpan, cancellationToken);
            options = options ?? new GridFSDownloadOptions();
            using (var binding = GetSingleServerReadBinding(operationCancellationContext))
            {
                var fileInfo = GetFileInfo(binding, id, operationCancellationContext);
                return CreateDownloadStream(binding.Fork(), fileInfo, options, cancellationToken);
            }
        }

        /// <inheritdoc />
        public async Task<GridFSDownloadStream<TFileId>> OpenDownloadStreamAsync(TFileId id, GridFSDownloadOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull((object)id, nameof(id));
            // TODO: CSOT implement proper way to obtain the operationCancellationContext
            var operationCancellationContext = new OperationCancellationContext(Timeout.InfiniteTimeSpan, cancellationToken);
            options = options ?? new GridFSDownloadOptions();
            using (var binding = await GetSingleServerReadBindingAsync(operationCancellationContext).ConfigureAwait(false))
            {
                var fileInfo = await GetFileInfoAsync(binding, id, operationCancellationContext).ConfigureAwait(false);
                return CreateDownloadStream(binding.Fork(), fileInfo, options, cancellationToken);
            }
        }

        /// <inheritdoc />
        public GridFSDownloadStream<TFileId> OpenDownloadStreamByName(string filename, GridFSDownloadByNameOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(filename, nameof(filename));
            // TODO: CSOT implement proper way to obtain the operationCancellationContext
            var operationCancellationContext = new OperationCancellationContext(Timeout.InfiniteTimeSpan, cancellationToken);
            options = options ?? new GridFSDownloadByNameOptions();

            using (var binding = GetSingleServerReadBinding(operationCancellationContext))
            {
                var fileInfo = GetFileInfoByName(binding, filename, options.Revision, operationCancellationContext);
                return CreateDownloadStream(binding.Fork(), fileInfo, options);
            }
        }

        /// <inheritdoc />
        public async Task<GridFSDownloadStream<TFileId>> OpenDownloadStreamByNameAsync(string filename, GridFSDownloadByNameOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(filename, nameof(filename));
            // TODO: CSOT implement proper way to obtain the operationCancellationContext
            var operationCancellationContext = new OperationCancellationContext(Timeout.InfiniteTimeSpan, cancellationToken);
            options = options ?? new GridFSDownloadByNameOptions();

            using (var binding = await GetSingleServerReadBindingAsync(operationCancellationContext).ConfigureAwait(false))
            {
                var fileInfo = await GetFileInfoByNameAsync(binding, filename, options.Revision, operationCancellationContext).ConfigureAwait(false);
                return CreateDownloadStream(binding.Fork(), fileInfo, options);
            }
        }

        /// <inheritdoc />
        public GridFSUploadStream<TFileId> OpenUploadStream(TFileId id, string filename, GridFSUploadOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull((object)id, nameof(id));
            Ensure.IsNotNull(filename, nameof(filename));
            // TODO: CSOT implement proper way to obtain the operationCancellationContext
            var operationCancellationContext = new OperationCancellationContext(Timeout.InfiniteTimeSpan, cancellationToken);
            options = options ?? new GridFSUploadOptions();

            using (var binding = GetSingleServerReadWriteBinding(operationCancellationContext))
            {
                EnsureIndexes(binding, operationCancellationContext);
                return CreateUploadStream(binding, id, filename, options);
            }
        }

        /// <inheritdoc />
        public async Task<GridFSUploadStream<TFileId>> OpenUploadStreamAsync(TFileId id, string filename, GridFSUploadOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull((object)id, nameof(id));
            Ensure.IsNotNull(filename, nameof(filename));
            // TODO: CSOT implement proper way to obtain the operationCancellationContext
            var operationCancellationContext = new OperationCancellationContext(Timeout.InfiniteTimeSpan, cancellationToken);
            options = options ?? new GridFSUploadOptions();

            using (var binding = await GetSingleServerReadWriteBindingAsync(operationCancellationContext).ConfigureAwait(false))
            {
                await EnsureIndexesAsync(binding, operationCancellationContext).ConfigureAwait(false);
                return CreateUploadStream(binding, id, filename, options);
            }
        }

        /// <inheritdoc />
        public void Rename(TFileId id, string newFilename, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull((object)id, nameof(id));
            Ensure.IsNotNull(newFilename, nameof(newFilename));
            // TODO: CSOT implement proper way to obtain the operationCancellationContext
            var operationCancellationContext = new OperationCancellationContext(Timeout.InfiniteTimeSpan, cancellationToken);
            var renameOperation = CreateRenameOperation(id, newFilename);
            using (var binding = GetSingleServerReadWriteBinding(operationCancellationContext))
            {
                var result = renameOperation.Execute(binding, operationCancellationContext);

                if (result.IsModifiedCountAvailable && result.ModifiedCount == 0)
                {
                    throw new GridFSFileNotFoundException(_idSerializationInfo.SerializeValue(id));
                }
            }
        }

        /// <inheritdoc />
        public async Task RenameAsync(TFileId id, string newFilename, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull((object)id, nameof(id));
            Ensure.IsNotNull(newFilename, nameof(newFilename));
            // TODO: CSOT implement proper way to obtain the operationCancellationContext
            var operationCancellationContext = new OperationCancellationContext(Timeout.InfiniteTimeSpan, cancellationToken);
            var renameOperation = CreateRenameOperation(id, newFilename);
            using (var binding = await GetSingleServerReadWriteBindingAsync(operationCancellationContext).ConfigureAwait(false))
            {
                var result = await renameOperation.ExecuteAsync(binding, operationCancellationContext).ConfigureAwait(false);

                if (result.IsModifiedCountAvailable && result.ModifiedCount == 0)
                {
                    throw new GridFSFileNotFoundException(_idSerializationInfo.SerializeValue(id));
                }
            }
        }

        /// <inheritdoc />
        public void UploadFromBytes(TFileId id, string filename, byte[] source, GridFSUploadOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull((object)id, nameof(id));
            Ensure.IsNotNull(filename, nameof(filename));
            Ensure.IsNotNull(source, nameof(source));
            // TODO: CSOT implement proper way to obtain the operationCancellationContext
            var operationCancellationContext = new OperationCancellationContext(Timeout.InfiniteTimeSpan, cancellationToken);
            options = options ?? new GridFSUploadOptions();

            using (var sourceStream = new MemoryStream(source))
            {
                UploadFromStream(id, filename, sourceStream, options, cancellationToken);
            }
        }

        /// <inheritdoc />
        public async Task UploadFromBytesAsync(TFileId id, string filename, byte[] source, GridFSUploadOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull((object)id, nameof(id));
            Ensure.IsNotNull(filename, nameof(filename));
            Ensure.IsNotNull(source, nameof(source));
            // TODO: CSOT implement proper way to obtain the operationCancellationContext
            var operationCancellationContext = new OperationCancellationContext(Timeout.InfiniteTimeSpan, cancellationToken);
            options = options ?? new GridFSUploadOptions();

            using (var sourceStream = new MemoryStream(source))
            {
                await UploadFromStreamAsync(id, filename, sourceStream, options, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public void UploadFromStream(TFileId id, string filename, Stream source, GridFSUploadOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull((object)id, nameof(id));
            Ensure.IsNotNull(filename, nameof(filename));
            Ensure.IsNotNull(source, nameof(source));
            options = options ?? new GridFSUploadOptions();

            using (var destination = OpenUploadStream(id, filename, options, cancellationToken))
            {
                var chunkSizeBytes = options.ChunkSizeBytes ?? _options.ChunkSizeBytes;
                var buffer = new byte[chunkSizeBytes];

                while (true)
                {
                    int bytesRead = 0;
                    try
                    {
                        bytesRead = source.Read(buffer, 0, buffer.Length);
                    }
                    catch
                    {
                        try
                        {
                            destination.Abort();
                        }
                        catch
                        {
                            // ignore any exceptions because we're going to rethrow the original exception
                        }
                        throw;
                    }
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    destination.Write(buffer, 0, bytesRead);
                }

                destination.Close(cancellationToken);
            }
        }

        /// <inheritdoc />
        public async Task UploadFromStreamAsync(TFileId id, string filename, Stream source, GridFSUploadOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull((object)id, nameof(id));
            Ensure.IsNotNull(filename, nameof(filename));
            Ensure.IsNotNull(source, nameof(source));
            options = options ?? new GridFSUploadOptions();

            using (var destination = await OpenUploadStreamAsync(id, filename, options, cancellationToken).ConfigureAwait(false))
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
            }
        }

        // private methods
        private bool ChunksCollectionIndexesExist(List<BsonDocument> indexes)
        {
            var key = new BsonDocument { { "files_id", 1 }, { "n", 1 } };
            return IndexExists(indexes, key);
        }

        private bool ChunksCollectionIndexesExist(IReadBindingHandle binding, OperationCancellationContext cancellationContext)
        {
            var indexes = ListIndexes(binding, this.GetChunksCollectionNamespace(), cancellationContext);
            return ChunksCollectionIndexesExist(indexes);
        }

        private async Task<bool> ChunksCollectionIndexesExistAsync(IReadBindingHandle binding, OperationCancellationContext cancellationContext)
        {
            var indexes = await ListIndexesAsync(binding, this.GetChunksCollectionNamespace(), cancellationContext).ConfigureAwait(false);
            return ChunksCollectionIndexesExist(indexes);
        }

        private void CreateChunksCollectionIndexes(IReadWriteBindingHandle binding, OperationCancellationContext cancellationContext)
        {
            var operation = CreateCreateChunksCollectionIndexesOperation();
            operation.Execute(binding, cancellationContext);
        }

        private async Task CreateChunksCollectionIndexesAsync(IReadWriteBindingHandle binding, OperationCancellationContext cancellationContext)
        {
            var operation = CreateCreateChunksCollectionIndexesOperation();
            await operation.ExecuteAsync(binding, cancellationContext).ConfigureAwait(false);
        }

        internal CreateIndexesOperation CreateCreateChunksCollectionIndexesOperation()
        {
            var collectionNamespace = this.GetChunksCollectionNamespace();
            var requests = new[] { new CreateIndexRequest(new BsonDocument { { "files_id", 1 }, { "n", 1 } }) { Unique = true } };
            var messageEncoderSettings = this.GetMessageEncoderSettings();
            return new CreateIndexesOperation(collectionNamespace, requests, messageEncoderSettings)
            {
                WriteConcern = _options.WriteConcern ?? _database.Settings.WriteConcern
            };
        }

        internal CreateIndexesOperation CreateCreateFilesCollectionIndexesOperation()
        {
            var collectionNamespace = this.GetFilesCollectionNamespace();
            var requests = new[] { new CreateIndexRequest(new BsonDocument { { "filename", 1 }, { "uploadDate", 1 } }) };
            var messageEncoderSettings = this.GetMessageEncoderSettings();
            return new CreateIndexesOperation(collectionNamespace, requests, messageEncoderSettings)
            {
                WriteConcern = _options.WriteConcern ?? _database.Settings.WriteConcern
            };
        }

        private BulkMixedWriteOperation CreateDeleteChunksOperation(TFileId id)
        {
            var filter = new BsonDocument("files_id", _idSerializationInfo.SerializeValue(id));
            return new BulkMixedWriteOperation(
                this.GetChunksCollectionNamespace(),
                new[] { new DeleteRequest(filter) { Limit = 0 } },
                this.GetMessageEncoderSettings());
        }

        private GridFSDownloadStream<TFileId> CreateDownloadStream(IReadBindingHandle binding, GridFSFileInfo<TFileId> fileInfo, GridFSDownloadOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var seekable = options.Seekable ?? false;

            if (seekable)
            {
                return new GridFSSeekableDownloadStream<TFileId>(this, binding, fileInfo);
            }
            else
            {
                return new GridFSForwardOnlyDownloadStream<TFileId>(this, binding, fileInfo);
            }
        }

        internal DropCollectionOperation CreateDropCollectionOperation(CollectionNamespace collectionNamespace, MessageEncoderSettings messageEncoderSettings)
        {
            return new DropCollectionOperation(collectionNamespace, messageEncoderSettings)
            {
                WriteConcern = _options.WriteConcern ?? _database.Settings.WriteConcern
            };
        }

        private BulkMixedWriteOperation CreateDeleteFileOperation(TFileId id)
        {
            var filter = new BsonDocument("_id", _idSerializationInfo.SerializeValue(id));
            return new BulkMixedWriteOperation(
                this.GetFilesCollectionNamespace(),
                new[] { new DeleteRequest(filter) },
                this.GetMessageEncoderSettings());
        }

        private void CreateFilesCollectionIndexes(IReadWriteBindingHandle binding, OperationCancellationContext cancellationContext)
        {
            var operation = CreateCreateFilesCollectionIndexesOperation();
            operation.Execute(binding, cancellationContext);
        }

        private async Task CreateFilesCollectionIndexesAsync(IReadWriteBindingHandle binding, OperationCancellationContext cancellationContext)
        {
            var operation = CreateCreateFilesCollectionIndexesOperation();
            await operation.ExecuteAsync(binding, cancellationContext).ConfigureAwait(false);
        }

        private FindOperation<GridFSFileInfo<TFileId>> CreateFindOperation(
            FilterDefinition<GridFSFileInfo<TFileId>> filter,
            GridFSFindOptions<TFileId> options,
            ExpressionTranslationOptions translationOptions)
        {
            var filesCollectionNamespace = this.GetFilesCollectionNamespace();
            var messageEncoderSettings = this.GetMessageEncoderSettings();
            var args = new RenderArgs<GridFSFileInfo<TFileId>>(_fileInfoSerializer, _options.SerializerRegistry, translationOptions: translationOptions);
            var renderedFilter = filter.Render(args);
            var renderedSort = options.Sort == null ? null : options.Sort.Render(args);

            return new FindOperation<GridFSFileInfo<TFileId>>(
                filesCollectionNamespace,
                _fileInfoSerializer,
                messageEncoderSettings)
            {
                AllowDiskUse = options.AllowDiskUse,
                BatchSize = options.BatchSize,
                Filter = renderedFilter,
                Limit = options.Limit,
                MaxTime = options.MaxTime,
                NoCursorTimeout = options.NoCursorTimeout,
                ReadConcern = GetReadConcern(),
                RetryRequested = _database.Client.Settings.RetryReads,
                Skip = options.Skip,
                Sort = renderedSort
            };
        }

        private FindOperation<GridFSFileInfo<TFileId>> CreateGetFileInfoByNameOperation(string filename, int revision)
        {
            var collectionNamespace = this.GetFilesCollectionNamespace();
            var messageEncoderSettings = this.GetMessageEncoderSettings();
            var filter = new BsonDocument("filename", filename);
            var skip = revision >= 0 ? revision : -revision - 1;
            var limit = 1;
            var sort = new BsonDocument("uploadDate", revision >= 0 ? 1 : -1);

            return new FindOperation<GridFSFileInfo<TFileId>>(
                collectionNamespace,
                _fileInfoSerializer,
                messageEncoderSettings)
            {
                Filter = filter,
                Limit = limit,
                ReadConcern = GetReadConcern(),
                RetryRequested = _database.Client.Settings.RetryReads,
                Skip = skip,
                Sort = sort
            };
        }

        private FindOperation<GridFSFileInfo<TFileId>> CreateGetFileInfoOperation(TFileId id)
        {
            var filesCollectionNamespace = this.GetFilesCollectionNamespace();
            var messageEncoderSettings = this.GetMessageEncoderSettings();
            var filter = new BsonDocument("_id", _idSerializationInfo.SerializeValue(id));

            return new FindOperation<GridFSFileInfo<TFileId>>(
                filesCollectionNamespace,
                _fileInfoSerializer,
                messageEncoderSettings)
            {
                Filter = filter,
                Limit = 1,
                ReadConcern = GetReadConcern(),
                RetryRequested = _database.Client.Settings.RetryReads,
                SingleBatch = true
            };
        }

        private FindOperation<BsonDocument> CreateIsFilesCollectionEmptyOperation()
        {
            var filesCollectionNamespace = this.GetFilesCollectionNamespace();
            var messageEncoderSettings = this.GetMessageEncoderSettings();
            return new FindOperation<BsonDocument>(filesCollectionNamespace, BsonDocumentSerializer.Instance, messageEncoderSettings)
            {
                Limit = 1,
                ReadConcern = GetReadConcern(),
                SingleBatch = true,
                Projection = new BsonDocument("_id", 1),
                RetryRequested = _database.Client.Settings.RetryReads
            };
        }

        private ListIndexesOperation CreateListIndexesOperation(CollectionNamespace collectionNamespace)
        {
            var messageEncoderSettings = this.GetMessageEncoderSettings();
            return new ListIndexesOperation(collectionNamespace, messageEncoderSettings)
            {
                RetryRequested = _database.Client.Settings.RetryReads
            };
        }

        private BulkMixedWriteOperation CreateRenameOperation(TFileId id, string newFilename)
        {
            var filesCollectionNamespace = this.GetFilesCollectionNamespace();
            var filter = new BsonDocument("_id", _idSerializationInfo.SerializeValue(id));
            var update = new BsonDocument("$set", new BsonDocument("filename", newFilename));
            var requests = new[] { new UpdateRequest(UpdateType.Update, filter, update) };
            var messageEncoderSettings = this.GetMessageEncoderSettings();
            return new BulkMixedWriteOperation(filesCollectionNamespace, requests, messageEncoderSettings);
        }

        private GridFSUploadStream<TFileId> CreateUploadStream(IReadWriteBindingHandle binding, TFileId id, string filename, GridFSUploadOptions options)
        {
#pragma warning disable 618
            var chunkSizeBytes = options.ChunkSizeBytes ?? _options.ChunkSizeBytes;
            var batchSize = options.BatchSize ?? (16 * 1024 * 1024 / chunkSizeBytes);

            return new GridFSForwardOnlyUploadStream<TFileId>(
                this,
                binding.Fork(),
                id,
                filename,
                options.Metadata,
                chunkSizeBytes,
                batchSize);
#pragma warning restore
        }

        private byte[] DownloadAsBytesHelper(IReadBindingHandle binding, GridFSFileInfo<TFileId> fileInfo, GridFSDownloadOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (fileInfo.Length > int.MaxValue)
            {
                throw new NotSupportedException("GridFS stored file is too large to be returned as a byte array.");
            }

            var bytes = new byte[(int)fileInfo.Length];
            using (var destination = new MemoryStream(bytes))
            {
                DownloadToStreamHelper(binding, fileInfo, destination, options, cancellationToken);
                return bytes;
            }
        }

        private async Task<byte[]> DownloadAsBytesHelperAsync(IReadBindingHandle binding, GridFSFileInfo<TFileId> fileInfo, GridFSDownloadOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (fileInfo.Length > int.MaxValue)
            {
                throw new NotSupportedException("GridFS stored file is too large to be returned as a byte array.");
            }

            var bytes = new byte[(int)fileInfo.Length];
            using (var destination = new MemoryStream(bytes))
            {
                await DownloadToStreamHelperAsync(binding, fileInfo, destination, options, cancellationToken).ConfigureAwait(false);
                return bytes;
            }
        }

        private void DownloadToStreamHelper(IReadBindingHandle binding, GridFSFileInfo<TFileId> fileInfo, Stream destination, GridFSDownloadOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var retryReads = _database.Client.Settings.RetryReads;
            using (var source = new GridFSForwardOnlyDownloadStream<TFileId>(this, binding.Fork(), fileInfo) { RetryReads = retryReads })
            {
                var count = source.Length;
                var buffer = new byte[fileInfo.ChunkSizeBytes];

                while (count > 0)
                {
                    var partialCount = (int)Math.Min(buffer.Length, count);
                    source.ReadBytes(buffer, 0, partialCount, cancellationToken);
                    //((Stream)source).ReadBytes(buffer, 0, partialCount, cancellationToken);
                    destination.Write(buffer, 0, partialCount);
                    count -= partialCount;
                }
            }
        }

        private async Task DownloadToStreamHelperAsync(IReadBindingHandle binding, GridFSFileInfo<TFileId> fileInfo, Stream destination, GridFSDownloadOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var retryReads = _database.Client.Settings.RetryReads;
            using (var source = new GridFSForwardOnlyDownloadStream<TFileId>(this, binding.Fork(), fileInfo) { RetryReads = retryReads })
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

        private void EnsureIndexes(IReadWriteBindingHandle binding, OperationCancellationContext cancellationContext)
        {
            _ensureIndexesSemaphore.Wait(cancellationContext.RemainingTimeout, cancellationContext.CancellationToken);
            try
            {
                if (!_ensureIndexesDone)
                {
                    var isFilesCollectionEmpty = IsFilesCollectionEmpty(binding, cancellationContext);
                    if (isFilesCollectionEmpty)
                    {
                        if (!FilesCollectionIndexesExist(binding, cancellationContext))
                        {
                            CreateFilesCollectionIndexes(binding, cancellationContext);
                        }
                        if (!ChunksCollectionIndexesExist(binding, cancellationContext))
                        {
                            CreateChunksCollectionIndexes(binding, cancellationContext);
                        }
                    }

                    _ensureIndexesDone = true;
                }
            }
            finally
            {
                _ensureIndexesSemaphore.Release();
            }
        }

        private async Task EnsureIndexesAsync(IReadWriteBindingHandle binding, OperationCancellationContext cancellationContext)
        {
            await _ensureIndexesSemaphore.WaitAsync(cancellationContext.RemainingTimeout, cancellationContext.CancellationToken).ConfigureAwait(false);
            try
            {
                if (!_ensureIndexesDone)
                {
                    var isFilesCollectionEmpty = await IsFilesCollectionEmptyAsync(binding, cancellationContext).ConfigureAwait(false);
                    if (isFilesCollectionEmpty)
                    {
                        if (!(await FilesCollectionIndexesExistAsync(binding, cancellationContext).ConfigureAwait(false)))
                        {
                            await CreateFilesCollectionIndexesAsync(binding, cancellationContext).ConfigureAwait(false);
                        }
                        if (!(await ChunksCollectionIndexesExistAsync(binding, cancellationContext).ConfigureAwait(false)))
                        {
                            await CreateChunksCollectionIndexesAsync(binding, cancellationContext).ConfigureAwait(false);
                        }
                    }

                    _ensureIndexesDone = true;
                }
            }
            finally
            {
                _ensureIndexesSemaphore.Release();
            }
        }

        private bool FilesCollectionIndexesExist(List<BsonDocument> indexes)
        {
            var key = new BsonDocument { { "filename", 1 }, { "uploadDate", 1 } };
            return IndexExists(indexes, key);
        }

        private bool FilesCollectionIndexesExist(IReadBindingHandle binding, OperationCancellationContext cancellationContext)
        {
            var indexes = ListIndexes(binding, this.GetFilesCollectionNamespace(), cancellationContext);
            return FilesCollectionIndexesExist(indexes);
        }

        private async Task<bool> FilesCollectionIndexesExistAsync(IReadBindingHandle binding, OperationCancellationContext cancellationContext)
        {
            var indexes = await ListIndexesAsync(binding, this.GetFilesCollectionNamespace(), cancellationContext).ConfigureAwait(false);
            return FilesCollectionIndexesExist(indexes);
        }

        private GridFSFileInfo<TFileId> GetFileInfo(IReadBindingHandle binding, TFileId id, OperationCancellationContext cancellationContext)
        {
            var operation = CreateGetFileInfoOperation(id);
            using (var cursor = operation.Execute(binding, cancellationContext))
            {
                // TODO: CSOT add a way to propagate cancellationContext into cursor methods.
                var fileInfo = cursor.FirstOrDefault(cancellationContext.CancellationToken);
                if (fileInfo == null)
                {
                    throw new GridFSFileNotFoundException(_idSerializationInfo.SerializeValue(id));
                }
                return fileInfo;
            }
        }

        private async Task<GridFSFileInfo<TFileId>> GetFileInfoAsync(IReadBindingHandle binding, TFileId id, OperationCancellationContext cancellationContext)
        {
            var operation = CreateGetFileInfoOperation(id);
            using (var cursor = await operation.ExecuteAsync(binding, cancellationContext).ConfigureAwait(false))
            {
                // TODO: CSOT add a way to propagate cancellationContext into cursor methods.
                var fileInfo = await cursor.FirstOrDefaultAsync(cancellationContext.CancellationToken).ConfigureAwait(false);
                if (fileInfo == null)
                {
                    throw new GridFSFileNotFoundException(_idSerializationInfo.SerializeValue(id));
                }
                return fileInfo;
            }
        }

        private GridFSFileInfo<TFileId> GetFileInfoByName(IReadBindingHandle binding, string filename, int revision, OperationCancellationContext cancellationContext)
        {
            var operation = CreateGetFileInfoByNameOperation(filename, revision);
            using (var cursor = operation.Execute(binding, cancellationContext))
            {
                // TODO: CSOT add a way to propagate cancellationContext into cursor methods.
                var fileInfo = cursor.FirstOrDefault(cancellationContext.CancellationToken);
                if (fileInfo == null)
                {
                    throw new GridFSFileNotFoundException(filename, revision);
                }
                return fileInfo;
            }
        }

        private async Task<GridFSFileInfo<TFileId>> GetFileInfoByNameAsync(IReadBindingHandle binding, string filename, int revision, OperationCancellationContext cancellationContext)
        {
            var operation = CreateGetFileInfoByNameOperation(filename, revision);
            using (var cursor = await operation.ExecuteAsync(binding, cancellationContext).ConfigureAwait(false))
            {
                // TODO: CSOT add a way to propagate cancellationContext into cursor methods.
                var fileInfo = await cursor.FirstOrDefaultAsync(cancellationContext.CancellationToken).ConfigureAwait(false);
                if (fileInfo == null)
                {
                    throw new GridFSFileNotFoundException(filename, revision);
                }
                return fileInfo;
            }
        }

        private ReadConcern GetReadConcern()
        {
            return _options.ReadConcern ?? _database.Settings.ReadConcern;
        }

        private IReadBindingHandle GetSingleServerReadBinding(OperationCancellationContext cancellationContext)
        {
            var readPreference = _options.ReadPreference ?? _database.Settings.ReadPreference;
            var selector = new ReadPreferenceServerSelector(readPreference);
            var server = _cluster.SelectServer(selector, cancellationContext);
            var binding = new SingleServerReadBinding(server, readPreference, NoCoreSession.NewHandle());
            return new ReadBindingHandle(binding);
        }

        private async Task<IReadBindingHandle> GetSingleServerReadBindingAsync(OperationCancellationContext cancellationContext)
        {
            var readPreference = _options.ReadPreference ?? _database.Settings.ReadPreference;
            var selector = new ReadPreferenceServerSelector(readPreference);
            var server = await _cluster.SelectServerAsync(selector, cancellationContext).ConfigureAwait(false);
            var binding = new SingleServerReadBinding(server, readPreference, NoCoreSession.NewHandle());
            return new ReadBindingHandle(binding);
        }

        private IReadWriteBindingHandle GetSingleServerReadWriteBinding(OperationCancellationContext cancellationContext)
        {
            var selector = WritableServerSelector.Instance;
            var server = _cluster.SelectServer(selector, cancellationContext);
            var binding = new SingleServerReadWriteBinding(server, NoCoreSession.NewHandle());
            return new ReadWriteBindingHandle(binding);
        }

        private async Task<IReadWriteBindingHandle> GetSingleServerReadWriteBindingAsync(OperationCancellationContext cancellationContext)
        {
            var selector = WritableServerSelector.Instance;
            var server = await _cluster.SelectServerAsync(selector, cancellationContext).ConfigureAwait(false);
            var binding = new SingleServerReadWriteBinding(server, NoCoreSession.NewHandle());
            return new ReadWriteBindingHandle(binding);
        }

        private bool IndexExists(List<BsonDocument> indexes, BsonDocument key)
        {
            foreach (var index in indexes)
            {
                if (index["key"].Equals(key))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsFilesCollectionEmpty(IReadWriteBindingHandle binding, OperationCancellationContext cancellationContext)
        {
            var operation = CreateIsFilesCollectionEmptyOperation();
            using (var cursor = operation.Execute(binding, cancellationContext))
            {
                // TODO: CSOT add a way to propagate cancellationContext into cursor methods.
                var firstOrDefault = cursor.FirstOrDefault(cancellationContext.CancellationToken);
                return firstOrDefault == null;
            }
        }

        private async Task<bool> IsFilesCollectionEmptyAsync(IReadWriteBindingHandle binding, OperationCancellationContext cancellationContext)
        {
            var operation = CreateIsFilesCollectionEmptyOperation();
            using (var cursor = await operation.ExecuteAsync(binding, cancellationContext).ConfigureAwait(false))
            {
                // TODO: CSOT add a way to propagate cancellationContext into cursor methods.
                var firstOrDefault = await cursor.FirstOrDefaultAsync(cancellationContext.CancellationToken).ConfigureAwait(false);
                return firstOrDefault == null;
            }
        }

        private List<BsonDocument> ListIndexes(IReadBinding binding, CollectionNamespace collectionNamespace, OperationCancellationContext cancellationContext)
        {
            var operation = CreateListIndexesOperation(collectionNamespace);
            return operation.Execute(binding, cancellationContext).ToList();
        }

        private async Task<List<BsonDocument>> ListIndexesAsync(IReadBinding binding, CollectionNamespace collectionNamespace, OperationCancellationContext cancellationContext)
        {
            var operation = CreateListIndexesOperation(collectionNamespace);
            var cursor = await operation.ExecuteAsync(binding, cancellationContext).ConfigureAwait(false);
            return await cursor.ToListAsync(cancellationContext.CancellationToken).ConfigureAwait(false);
        }
    }
}
