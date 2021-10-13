/* Copyright 2013-present MongoDB Inc.
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents an aggregate operation that writes the results to an output collection.
    /// </summary>
    public class AggregateToCollectionOperation : IWriteOperation<BsonDocument>
    {
        // fields
        private bool? _allowDiskUse;
        private bool? _bypassDocumentValidation;
        private Collation _collation;
        private readonly CollectionNamespace _collectionNamespace;
        private string _comment;
        private readonly DatabaseNamespace _databaseNamespace;
        private ReadPreference _effectiveReadPreference;
        private BsonValue _hint;
        private ReadPreference _initialReadPreference;
        private BsonDocument _let;
        private TimeSpan? _maxTime;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly IReadOnlyList<BsonDocument> _pipeline;
        private ReadConcern _readConcern;
        private WriteConcern _writeConcern;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateToCollectionOperation"/> class.
        /// </summary>
        /// <param name="databaseNamespace">The database namespace.</param>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public AggregateToCollectionOperation(DatabaseNamespace databaseNamespace, IEnumerable<BsonDocument> pipeline, MessageEncoderSettings messageEncoderSettings)
        {
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, nameof(databaseNamespace));
            _pipeline = Ensure.IsNotNull(pipeline, nameof(pipeline)).ToList();
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, nameof(messageEncoderSettings));

            EnsureIsOutputToCollectionPipeline();
            _pipeline = SimplifyOutStageIfOutputDatabaseIsSameAsInputDatabase(_pipeline);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateToCollectionOperation"/> class.
        /// </summary>
        /// <param name="collectionNamespace">The collection namespace.</param>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public AggregateToCollectionOperation(CollectionNamespace collectionNamespace, IEnumerable<BsonDocument> pipeline, MessageEncoderSettings messageEncoderSettings)
            : this(Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace)).DatabaseNamespace, pipeline, messageEncoderSettings)
        {
            _collectionNamespace = collectionNamespace;
        }

        // properties
        /// <summary>
        /// Gets or sets a value indicating whether the server is allowed to use the disk.
        /// </summary>
        /// <value>
        /// A value indicating whether the server is allowed to use the disk.
        /// </value>
        public bool? AllowDiskUse
        {
            get { return _allowDiskUse; }
            set { _allowDiskUse = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to bypass document validation.
        /// </summary>
        /// <value>
        /// A value indicating whether to bypass document validation.
        /// </value>
        public bool? BypassDocumentValidation
        {
            get { return _bypassDocumentValidation; }
            set { _bypassDocumentValidation = value; }
        }

        /// <summary>
        /// Gets or sets the collation.
        /// </summary>
        /// <value>
        /// The collation.
        /// </value>
        public Collation Collation
        {
            get { return _collation; }
            set { _collation = value; }
        }

        /// <summary>
        /// Gets the collection namespace.
        /// </summary>
        /// <value>
        /// The collection namespace.
        /// </value>
        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        /// <summary>
        /// Gets or sets the comment.
        /// </summary>
        /// <value>
        /// The comment.
        /// </value>
        public string Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }

        /// <summary>
        /// Gets the database namespace.
        /// </summary>
        /// <value>
        /// The database namespace.
        /// </value>
        public DatabaseNamespace DatabaseNamespace
        {
            get { return _databaseNamespace; }
        }

        /// <summary>
        /// The effective read preference.
        /// </summary>
        public ReadPreference EffectiveReadPreference
        {
            get { return _effectiveReadPreference ?? _initialReadPreference; }
        }

        /// <summary>
        /// Gets or sets the hint. This must either be a BsonString representing the index name or a BsonDocument representing the key pattern of the index.
        /// </summary>
        /// <value>
        /// The hint.
        /// </value>
        public BsonValue Hint
        {
            get { return _hint; }
            set { _hint = value; }
        }

        /// <summary>
        /// The initial read preference.
        /// </summary>
        public ReadPreference InitialReadPreference
        {
            get { return _initialReadPreference; }
            set { _initialReadPreference = value; }
        }

        /// <summary>
        /// Gets or sets the "let" definition.
        /// </summary>
        /// <value>
        /// The "let" definition.
        /// </value>
        public BsonDocument Let
        {
            get { return _let; }
            set { _let = value; }
        }

        /// <summary>
        /// Gets or sets the maximum time the server should spend on this operation.
        /// </summary>
        /// <value>
        /// The maximum time the server should spend on this operation.
        /// </value>
        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = Ensure.IsNullOrInfiniteOrGreaterThanOrEqualToZero(value, nameof(value)); }
        }

        /// <summary>
        /// Gets the message encoder settings.
        /// </summary>
        /// <value>
        /// The message encoder settings.
        /// </value>
        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        /// <summary>
        /// Gets the pipeline.
        /// </summary>
        /// <value>
        /// The pipeline.
        /// </value>
        public IReadOnlyList<BsonDocument> Pipeline
        {
            get { return _pipeline; }
        }

        /// <summary>
        /// Gets or sets the read concern.
        /// </summary>
        /// <value>
        /// The read concern.
        /// </value>
        public ReadConcern ReadConcern
        {
            get { return _readConcern; }
            set
            {
                _readConcern = value;
            }
        }

        /// <summary>
        /// Gets or sets the write concern.
        /// </summary>
        /// <value>
        /// The write concern.
        /// </value>
        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = value; }
        }

        // methods
        /// <inheritdoc/>
        public BsonDocument Execute(IWriteBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (var channelSource = binding.GetWriteChannelSource(cancellationToken))
            {
                _effectiveReadPreference = GetEffectiveReadPreference(channelSource.ServerDescription);
                using (var channel = channelSource.GetChannel(cancellationToken))
                using (var channelBinding = ChannelReadWriteBinding.CreateChannelReadWriteBindingWithReadPreference(channelSource.Server, channel, binding.Session.Fork(), _effectiveReadPreference))
                {
                    var operation = CreateWriteOperation(channelBinding.Session, channel.ConnectionDescription, _effectiveReadPreference);
                    return operation.Execute(channelBinding, cancellationToken);
                }
            }
        }

        /// <inheritdoc/>
        public async Task<BsonDocument> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (var channelSource = await binding.GetWriteChannelSourceAsync(cancellationToken).ConfigureAwait(false))
            {
                _effectiveReadPreference = GetEffectiveReadPreference(channelSource.ServerDescription);
                using (var channel = await channelSource.GetChannelAsync(cancellationToken).ConfigureAwait(false))
                using (var channelBinding = ChannelReadWriteBinding.CreateChannelReadWriteBindingWithReadPreference(channelSource.Server, channel, binding.Session.Fork(), _effectiveReadPreference))
                {
                    var operation = CreateWriteOperation(channelBinding.Session, channel.ConnectionDescription, _effectiveReadPreference);
                    return await operation.ExecuteAsync(channelBinding, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Create $out/$merge related read write binding.
        /// </summary>
        /// <param name="cluster">The cluster.</param>
        /// <param name="session">The session.</param>
        /// <returns>A read write binding.</returns>
        public IReadWriteBinding CreateReadWriteBinding(ICluster cluster, ICoreSessionHandle session)
        {
            if (!ChannelPinningHelper.TryCreatePinnedReadWriteBinding(cluster, session, out var readWriteBinding))
            {
                readWriteBinding = new AggregateToCollectionWriteBinding(cluster, session, _initialReadPreference);
            }

            return new ReadWriteBindingHandle(readWriteBinding);
        }

        private ReadPreference GetEffectiveReadPreference(ServerDescription serverDescription)
        {
            if (_initialReadPreference != null && (ChannelPinningHelper.IsInLoadBalancedMode(serverDescription) || Feature.AggregateOutOnSecondary.IsSupported(serverDescription.Version)))
            {
                return _initialReadPreference;
            }
            else
            {
                return ReadPreference.Primary;
            }
        }

        internal BsonDocument CreateCommand(ICoreSessionHandle session, ConnectionDescription connectionDescription)
        {
            var serverVersion = connectionDescription.ServerVersion;
            Feature.Collation.ThrowIfNotSupported(serverVersion, _collation);

            var readConcern = _readConcern != null
                ? ReadConcernHelper.GetReadConcernForCommand(session, connectionDescription, _readConcern)
                : null;
            var writeConcern = WriteConcernHelper.GetWriteConcernForCommandThatWrites(session, _writeConcern, serverVersion);
            return new BsonDocument
            {
                { "aggregate", _collectionNamespace == null ? (BsonValue)1 : _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(_pipeline) },
                { "allowDiskUse", () => _allowDiskUse.Value, _allowDiskUse.HasValue },
                { "bypassDocumentValidation", () => _bypassDocumentValidation.Value, _bypassDocumentValidation.HasValue && Feature.BypassDocumentValidation.IsSupported(serverVersion) },
                { "maxTimeMS", () => MaxTimeHelper.ToMaxTimeMS(_maxTime.Value), _maxTime.HasValue },
                { "collation", () => _collation.ToBsonDocument(), _collation != null },
                { "readConcern", readConcern, readConcern != null },
                { "writeConcern", writeConcern, writeConcern != null },
                { "cursor", new BsonDocument(), serverVersion >= new SemanticVersion(3, 6, 0) },
                { "hint", () => _hint, _hint != null },
                { "let", () => _let, _let != null },
                { "comment", () => _comment, _comment != null }
            };
        }

        private AggregateToCollectionWriteCommandOperation<BsonDocument> CreateWriteOperation(ICoreSessionHandle session, ConnectionDescription connectionDescription, ReadPreference readPreference)
        {
            var command = CreateCommand(session, connectionDescription);
            return new AggregateToCollectionWriteCommandOperation<BsonDocument>(
                CollectionNamespace?.DatabaseNamespace ?? _databaseNamespace,
                command,
                readPreference: readPreference,
                BsonDocumentSerializer.Instance,
                MessageEncoderSettings);
        }

        private void EnsureIsOutputToCollectionPipeline()
        {
            var lastStage = _pipeline.LastOrDefault();
            var lastStageName = lastStage?.GetElement(0).Name;
            if (lastStage == null || (lastStageName != "$out" && lastStageName != "$merge"))
            {
                throw new ArgumentException("The last stage of the pipeline for an AggregateOutputToCollectionOperation must have a $out or $merge operator.", "pipeline");
            }
        }

        private IReadOnlyList<BsonDocument> SimplifyOutStageIfOutputDatabaseIsSameAsInputDatabase(IReadOnlyList<BsonDocument> pipeline)
        {
            var lastStage = pipeline.Last();
            var lastStageName = lastStage.GetElement(0).Name;
            if (lastStageName == "$out" && lastStage["$out"] is BsonDocument outDocument)
            {
                if (outDocument.TryGetValue("db", out var db) && db.IsString &&
                    outDocument.TryGetValue("coll", out var coll) && coll.IsString)
                {
                    var outputDatabaseName = db.AsString;
                    if (outputDatabaseName == _databaseNamespace.DatabaseName)
                    {
                        var outputCollectionName = coll.AsString;
                        var simplifiedOutStage = lastStage.Clone().AsBsonDocument;
                        simplifiedOutStage["$out"] = outputCollectionName;

                        var modifiedPipeline = new List<BsonDocument>(pipeline);
                        modifiedPipeline[modifiedPipeline.Count - 1] = simplifiedOutStage;

                        return modifiedPipeline;
                    }
                }
            }

            return pipeline; // unchanged
        }

        private class AggregateToCollectionWriteBinding : IReadWriteBinding
        {
            // fields
            private readonly ICluster _cluster;
            private bool _disposed;
            private ReadPreference _readPreference;
            private readonly ICoreSessionHandle _session;
            private IServerSelector _serverSelector;

            // constructors
            public AggregateToCollectionWriteBinding(ICluster cluster, ICoreSessionHandle session, ReadPreference readPreference)
            {
                _cluster = Ensure.IsNotNull(cluster, nameof(cluster));
                _session = Ensure.IsNotNull(session, nameof(session));
                _readPreference = Ensure.IsNotNull(readPreference, nameof(readPreference));
                _serverSelector = CreateServerSelector();
            }

            // properties
            /// <inheritdoc/>
            public ReadPreference ReadPreference
            {
                get { return _readPreference; }
            }

            /// <inheritdoc/>
            public ICoreSessionHandle Session
            {
                get { return _session; }
            }

            // methods
            /// <inheritdoc/>
            public IChannelSourceHandle GetReadChannelSource(CancellationToken cancellationToken)
            {
                ThrowIfDisposed();
                var server = _cluster.SelectServerAndPinIfNeeded(_session, _serverSelector, cancellationToken);

                return GetChannelSourceHelper(server);
            }

            /// <inheritdoc/>
            public async Task<IChannelSourceHandle> GetReadChannelSourceAsync(CancellationToken cancellationToken)
            {
                ThrowIfDisposed();
                var server = await _cluster.SelectServerAndPinIfNeededAsync(_session, _serverSelector, cancellationToken).ConfigureAwait(false);
                return GetChannelSourceHelper(server);
            }

            /// <inheritdoc/>
            public IChannelSourceHandle GetWriteChannelSource(CancellationToken cancellationToken)
            {
                ThrowIfDisposed();
                var server = _cluster.SelectServerAndPinIfNeeded(_session, _serverSelector, cancellationToken);
                return GetChannelSourceHelper(server);
            }

            /// <inheritdoc/>
            public async Task<IChannelSourceHandle> GetWriteChannelSourceAsync(CancellationToken cancellationToken)
            {
                ThrowIfDisposed();
                var server = await _cluster.SelectServerAndPinIfNeededAsync(_session, _serverSelector, cancellationToken).ConfigureAwait(false);
                return GetChannelSourceHelper(server);
            }

            private IChannelSourceHandle GetChannelSourceHelper(IServer server)
            {
                return new ChannelSourceHandle(new ServerChannelSource(server, _session.Fork()));
            }

            /// <inheritdoc/>
            private IServerSelector CreateServerSelector()
            {
                return new DelegateServerSelector(
                   (c, s) =>
                   {
                       IServerSelector serverSelector;
                       if (ChannelPinningHelper.IsInLoadBalancedMode(c) || Feature.AggregateOutOnSecondary.IsSupported(s.Max(i => i.Version)))
                       {
                           serverSelector = new ReadPreferenceServerSelector(_readPreference);
                       }
                       else
                       {
                           serverSelector = WritableServerSelector.Instance;
                       }
                       return serverSelector.SelectServers(c, s);
                   });
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _session.Dispose();
                    _disposed = true;
                }
            }

            private void ThrowIfDisposed()
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }
            }
        }

        private class AggregateToCollectionWriteCommandOperation<TCommandResult> : CommandOperationBase<TCommandResult>, IWriteOperation<TCommandResult>
        {
            private readonly ReadPreference _readPreference;

            // constructors
            public AggregateToCollectionWriteCommandOperation(
                DatabaseNamespace databaseNamespace,
                BsonDocument command,
                ReadPreference readPreference,
                IBsonSerializer<TCommandResult> resultSerializer,
                MessageEncoderSettings messageEncoderSettings)
                : base(databaseNamespace, command, resultSerializer, messageEncoderSettings)
            {
                _readPreference = readPreference;
            }

            // methods
            /// <inheritdoc/>
            public TCommandResult Execute(IWriteBinding binding, CancellationToken cancellationToken)
            {
                Ensure.IsNotNull(binding, nameof(binding));

                using (EventContext.BeginOperation())
                using (var channelSource = binding.GetWriteChannelSource(cancellationToken))
                {
                    return ExecuteProtocol(channelSource, binding.Session, _readPreference, cancellationToken);
                }
            }

            /// <inheritdoc/>
            public async Task<TCommandResult> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken = default(CancellationToken))
            {
                Ensure.IsNotNull(binding, nameof(binding));

                using (EventContext.BeginOperation())
                using (var channelSource = await binding.GetWriteChannelSourceAsync(cancellationToken).ConfigureAwait(false))
                {
                    return await ExecuteProtocolAsync(channelSource, binding.Session, _readPreference, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
