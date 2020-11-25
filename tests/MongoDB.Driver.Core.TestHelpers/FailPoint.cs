/* Copyright 2018-present MongoDB Inc.
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
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.TestHelpers
{
    public static class FailPointName
    {
        // public constants
        public const string MaxTimeAlwaysTimeout = "maxTimeAlwaysTimeOut";
        public const string OnPrimaryTransactionalWrite = "onPrimaryTransactionalWrite";
    }


    public sealed class FailPoint : IDisposable
    {
        #region static
        // public static methods
        /// <summary>
        /// Create a FailPoint and executes a configureFailPoint command on the selected server.
        /// </summary>
        /// <param name="cluster">The cluster.</param>
        /// <param name="session">The session.</param>
        /// <param name="command">The command.</param>
        /// <returns>A FailPoint containing the proper binding.</returns>
        public static FailPoint Configure(ICluster cluster, ICoreSessionHandle session, BsonDocument command)
        {
            var server = GetWriteableServer(cluster);
            return FailPoint.Configure(server, session, command);
        }

        /// <summary>
        /// Create a FailPoint and executes a configureFailPoint command on the selected server.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="session">The session.</param>
        /// <param name="command">The command.</param>
        /// <returns>A FailPoint containing the proper binding.</returns>
        public static FailPoint Configure(IServer server, ICoreSessionHandle session, BsonDocument command)
        {
            var binding = new SingleServerReadWriteBinding(server, session.Fork());
            var failpoint = new FailPoint(server, binding, command);
            try
            {
                failpoint.Configure();
                return failpoint;
            }
            catch
            {
                try { failpoint.Dispose(); } catch { }
                throw;
            }
        }

        /// <summary>
        /// Create a FailPoint and executes a configureFailPoint command on the selected server.
        /// </summary>
        /// <param name="cluster">The cluster.</param>
        /// <param name="session">The session.</param>
        /// <param name="name">The name.</param>
        /// <param name="args">The arguments for the FailPoint.</param>
        /// <returns>A FailPoint containing the proper binding.</returns>
        public static FailPoint Configure(ICluster cluster, ICoreSessionHandle session, string name, BsonDocument args)
        {
            Ensure.IsNotNull(name, nameof(name));
            Ensure.IsNotNull(args, nameof(args));
            var command = new BsonDocument("configureFailPoint", name).Merge(args, overwriteExistingElements: false);
            return Configure(cluster, session, command);
        }

        /// <summary>
        /// Creates a FailPoint that is always on.
        /// </summary>
        /// <param name="cluster">The cluster.</param>
        /// <param name="session">The session.</param>
        /// <param name="name">The name.</param>
        /// <returns>A FailPoint containing the proper binding.</returns>
        public static FailPoint ConfigureAlwaysOn(ICluster cluster, ICoreSessionHandle session, string name)
        {
            var args = new BsonDocument("mode", "alwaysOn");
            return Configure(cluster, session, name, args);
        }

        /// <summary>
        /// Creates a FailPoint that fails <paramref name="n"/> times.
        /// </summary>
        /// <param name="cluster">The cluster.</param>
        /// <param name="session">The session.</param>
        /// <param name="name">The name.</param>
        /// <param name="n">The number of times to fail.</param>
        /// <returns>A FailPoint containing the proper binding.</returns>
        public static FailPoint ConfigureTimes(ICluster cluster, ICoreSessionHandle session, string name, int n)
        {
            var args = new BsonDocument("mode", new BsonDocument("times", n));
            return Configure(cluster, session, name, args);
        }

        // private static methods
        private static IServer GetWriteableServer(ICluster cluster)
        {
            var selector = WritableServerSelector.Instance;
            return cluster.SelectServer(selector, CancellationToken.None);
        }
        #endregion

        // private fields
        private readonly BsonDocument _command;
        private readonly IReadWriteBinding _binding;
        private bool _disposed;
        private readonly IServer _server;

        // constructors
        /// <summary>
        /// Construct a new FailPoing with a specified name and custom arguments.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="binding">Must be a single server binding.</param>
        /// <param name="command">The command.</param>
        public FailPoint(IServer server, IReadWriteBinding binding, BsonDocument command)
        {
            _server = Ensure.IsNotNull(server, nameof(server));
            _binding = Ensure.IsNotNull(binding, nameof(binding));
            _command = Ensure.IsNotNull(command, nameof(command));
            Ensure.That(command.GetElement(0).Name == "configureFailPoint", "Command name must be \"configureFailPoint\".", nameof(command));
        }

        // public properties
        /// <summary>
        /// The binding used by the FailPoint and associated commands.
        /// </summary>
        /// <value>The binding is a single server binding.</value>
        public IReadWriteBinding Binding => _binding;

        // public methods
        /// <summary>
        /// Configures the failpoint on the server.
        /// </summary>
        public void Configure()
        {
            Configure(_command);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                ConfigureOff();
                _binding.Dispose();
                _disposed = true;
            }
        }

        // private methods
        private void Configure(BsonDocument command)
        {
            ExecuteCommand(command);
        }

        private void ConfigureOff()
        {
            var name = _command[0].AsString;
            var command = new BsonDocument
            {
                { "configureFailPoint", name },
                { "mode", "off" }
            };
            Configure(command);
        }

        private void ExecuteCommand(BsonDocument command)
        {
            var adminDatabase = new DatabaseNamespace("admin");
            var operation = new WriteCommandOperation<BsonDocument>(
                adminDatabase,
                command,
                BsonDocumentSerializer.Instance,
                new MessageEncoderSettings());
            operation.Execute(_binding, CancellationToken.None);
        }
    }
}
