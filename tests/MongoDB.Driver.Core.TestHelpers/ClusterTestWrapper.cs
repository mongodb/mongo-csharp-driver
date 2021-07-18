/* Copyright 2021-present MongoDB Inc.
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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Authentication;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.TestHelpers
{
    public static class ClusterBuilderHelper
    {
        public static ICluster CreateCluster(ClusterBuilder builder, TraceSource traceSource)
        {
            var hasWritableServer = 0;
            var cluster = builder.BuildCluster();
            cluster.DescriptionChanged += (o, e) =>
            {
                var anyWritableServer = e.NewClusterDescription.Servers.Any(
                    description => description.Type.IsWritable());
                if (traceSource != null)
                {
                    traceSource.TraceEvent(TraceEventType.Information, 0, $"CreateCluster: DescriptionChanged event handler called.");
                    traceSource.TraceEvent(TraceEventType.Information, 0, $"CreateCluster: anyWritableServer = {anyWritableServer}.");
                    traceSource.TraceEvent(TraceEventType.Information, 0, $"CreateCluster: new description: {e.NewClusterDescription.ToString()}.");
                }
                Interlocked.Exchange(ref hasWritableServer, anyWritableServer ? 1 : 0);
            };
            if (traceSource != null)
            {
                traceSource.TraceEvent(TraceEventType.Information, 0, "CreateCluster: initializing cluster.");
            }
            cluster.Initialize();

            // wait until the cluster has connected to a writable server
            SpinWait.SpinUntil(() => Interlocked.CompareExchange(ref hasWritableServer, 0, 0) != 0, TimeSpan.FromSeconds(30));
            if (Interlocked.CompareExchange(ref hasWritableServer, 0, 0) == 0)
            {
                var message = string.Format(
                    "Test cluster has no writable server. Client view of the cluster is {0}.",
                    cluster.Description.ToString());
                throw new Exception(message);
            }

            if (traceSource != null)
            {
                traceSource.TraceEvent(TraceEventType.Information, 0, "CreateCluster: writable server found.");
            }

            return cluster;
        }

        public static ClusterBuilder BaseConfigureCluster(CoreEnvironmentConfiguration coreEnvironmentConfiguration, ClusterBuilder builder)
        {
            var serverSelectionTimeoutString = coreEnvironmentConfiguration.DefaultServerSelection;
            var connectionString = coreEnvironmentConfiguration.DefaultConnectionString;

            builder = builder
                .ConfigureWithConnectionString(connectionString, coreEnvironmentConfiguration.ServerApi)
                .ConfigureCluster(c => c.With(serverSelectionTimeout: TimeSpan.FromMilliseconds(serverSelectionTimeoutString)));

            if (connectionString.Tls.HasValue &&
                connectionString.Tls.Value &&
                connectionString.AuthMechanism != null &&
                connectionString.AuthMechanism == MongoDBX509Authenticator.MechanismName)
            {
                var certificate = coreEnvironmentConfiguration.X509Certificate2;
                builder = builder.ConfigureSsl(ssl => ssl.With(clientCertificates: new[] { certificate }));
            }

            return builder;
        }

        public static bool TryCreateTraceSourceIfConfigured(CoreEnvironmentConfiguration coreEnvironmentConfiguration, out TraceSource traceSource)
        {
            var sourceLevels = coreEnvironmentConfiguration.Logging;
            if (!sourceLevels.HasValue)
            {
                traceSource = null;
                return false;
            }
            traceSource = new TraceSource("mongodb-tests", sourceLevels.Value);
            return true;
        }

        public static ClusterBuilder ConfigureLogging(ClusterBuilder builder, TraceSource traceSource)
        {
            traceSource.Listeners.Clear(); // remove the default listener
            var listener = new TextWriterTraceListener(Console.Out);
            listener.TraceOutputOptions = TraceOptions.DateTime;
            traceSource.Listeners.Add(listener);
            return builder.TraceWith(traceSource);
        }

        public static ICluster CreateAndConfigureCluster(CoreEnvironmentConfiguration coreEnvironmentConfiguration)
        {
            var clusterBuilder = new ClusterBuilder();
            clusterBuilder = BaseConfigureCluster(coreEnvironmentConfiguration, clusterBuilder);
            if (TryCreateTraceSourceIfConfigured(coreEnvironmentConfiguration, out var traceSource))
            {
                clusterBuilder = ConfigureLogging(clusterBuilder, traceSource);
            }
            return CreateCluster(clusterBuilder, traceSource);
        }
    }

    public sealed class ClusterTestWrapper
    {
        private readonly Lazy<BuildInfoResult> _buildInfoResult;
        private readonly ICluster _cluster;
        private readonly MessageEncoderSettings _messageEncoderSettings;

        public ClusterTestWrapper(ICluster cluster)
        {
            _cluster = Ensure.IsNotNull(cluster, nameof(cluster));
            _buildInfoResult = new Lazy<BuildInfoResult>(() => RunBuildInfo(), isThreadSafe: true);
            _messageEncoderSettings = new MessageEncoderSettings();
        }

        // properties
        public ICluster Cluster => _cluster;

        public SemanticVersion ServerVersion
        {
            get
            {
                var server = _cluster.SelectServer(WritableServerSelector.Instance, CancellationToken.None);
                var description = server.Description;
                var version = description.Version ?? _buildInfoResult.Value.ServerVersion;
                if (version == null)
                {
                    throw new InvalidOperationException("ServerDescription.Version is unexpectedly null.");
                }
                return version;
            }
        }

        // methods
        public bool AreSessionsSupported()
        {
            SpinWait.SpinUntil(() => _cluster.Description.Servers.Any(s => s.State == ServerState.Connected), TimeSpan.FromSeconds(30));
            return AreSessionsSupported(_cluster.Description);
        }

        public bool AreSessionsSupported(ClusterDescription clusterDescription)
        {
            return
                clusterDescription.Servers.Any(s => s.State == ServerState.Connected) &&
                (clusterDescription.LogicalSessionTimeout.HasValue || clusterDescription.Type == ClusterType.LoadBalanced);
        }

        public ICoreSessionHandle StartSession(CoreSessionOptions options = null)
        {
            if (AreSessionsSupported())
            {
                return _cluster.StartSession(options);
            }
            else
            {
                return NoCoreSession.NewHandle();
            }
        }

        public BsonDocument GetServerParameters()
        {
            using (var session = StartSession())
            using (var binding = CreateReadBinding(session))
            {
                var command = new BsonDocument("getParameter", new BsonString("*"));
                var operation = new ReadCommandOperation<BsonDocument>(DatabaseNamespace.Admin, command, BsonDocumentSerializer.Instance, _messageEncoderSettings);
                var serverParameters = operation.Execute(binding, CancellationToken.None);

                return serverParameters;
            }
        }

        public string GetStorageEngine()
        {
            using (var session = StartSession())
            using (var binding = CreateReadWriteBinding(session))
            {
                var command = new BsonDocument("serverStatus", 1);
                var operation = new ReadCommandOperation<BsonDocument>(DatabaseNamespace.Admin, command, BsonDocumentSerializer.Instance, _messageEncoderSettings);
                var response = operation.Execute(binding, CancellationToken.None);
                BsonValue storageEngine;
                if (response.TryGetValue("storageEngine", out storageEngine) && storageEngine.AsBsonDocument.Contains("name"))
                {
                    return storageEngine["name"].AsString;
                }
                else
                {
                    return "mmapv1";
                }
            }
        }

        public BuildInfoResult RunBuildInfo()
        {
            using (var session = StartSession())
            using (var binding = CreateReadBinding(session))
            {
                var command = new BsonDocument("buildinfo", 1);
                var operation = new ReadCommandOperation<BsonDocument>(DatabaseNamespace.Admin, command, BsonDocumentSerializer.Instance, _messageEncoderSettings);
                var response = operation.Execute(binding, CancellationToken.None);
                return new BuildInfoResult(response);
            }
        }

        // private methods
        private IReadBindingHandle CreateReadBinding(ICoreSessionHandle session)
        {
            return CreateReadBinding(ReadPreference.Primary, session);
        }

        private IReadBindingHandle CreateReadBinding(ReadPreference readPreference, ICoreSessionHandle session)
        {
            var binding = new ReadPreferenceBinding(_cluster, readPreference, session.Fork());
            return new ReadBindingHandle(binding);
        }

        private IReadWriteBindingHandle CreateReadWriteBinding(ICoreSessionHandle session)
        {
            var binding = new WritableServerBinding(_cluster, session.Fork());
            return new ReadWriteBindingHandle(binding);
        }
    }
}
