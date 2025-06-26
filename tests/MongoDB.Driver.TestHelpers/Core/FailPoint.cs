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
    internal static class FailPointName
    {
        // public constants
        public const string FailCommand = "failCommand";
        public const string MaxTimeAlwaysTimeout = "maxTimeAlwaysTimeOut";
        public const string OnPrimaryTransactionalWrite = "onPrimaryTransactionalWrite";
    }

    internal sealed class FailPoint : IDisposable
    {
        private const string ApplicationNameTestableSuffix = "_async_";

        #region static

        public static FailPoint Configure(IClusterInternal cluster, ICoreSessionHandle session, BsonDocument command, bool? withAsync = null)
        {
            var server = GetWriteableServer(cluster);
            return FailPoint.Configure(server.Server, server.RoundTripTime, session, command, withAsync);
        }

        public static FailPoint Configure(IServer server, TimeSpan serverRoundTripTime, ICoreSessionHandle session, BsonDocument command, bool? withAsync = null)
        {
            var binding = new SingleServerReadWriteBinding(server, serverRoundTripTime, session.Fork());
            if (withAsync.HasValue)
            {
                MakeFailPointApplicationNameTestableIfConfigured(command, withAsync.Value);
            }

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

        public static FailPoint Configure(IClusterInternal cluster, ICoreSessionHandle session, string name, BsonDocument args, bool? withAsync = null)
        {
            Ensure.IsNotNull(name, nameof(name));
            Ensure.IsNotNull(args, nameof(args));
            var command = new BsonDocument("configureFailPoint", name).Merge(args, overwriteExistingElements: false);
            return Configure(cluster, session, command, withAsync);
        }

        public static FailPoint ConfigureAlwaysOn(IClusterInternal cluster, ICoreSessionHandle session, string name, bool? withAsync = null)
        {
            var args = new BsonDocument("mode", "alwaysOn");
            return Configure(cluster, session, name, args, withAsync);
        }

        public static string DecorateApplicationName(string applicationName, bool async) => $"{applicationName}{ApplicationNameTestableSuffix}{async}";

        // private static methods
        private static (IServer Server, TimeSpan RoundTripTime) GetWriteableServer(IClusterInternal cluster)
        {
            var selector = WritableServerSelector.Instance;
            return cluster.SelectServer(OperationContext.NoTimeout, selector);
        }

        private static void MakeFailPointApplicationNameTestableIfConfigured(BsonDocument command, bool async)
        {
            if (command.TryGetValue("data", out var dataBsonValue))
            {
                var dataDocument = dataBsonValue.AsBsonDocument;
                if (dataDocument.TryGetValue("appName", out var appName) && !appName.AsString.Contains(ApplicationNameTestableSuffix))
                {
                    dataDocument["appName"] = DecorateApplicationName(appName.AsString, async);
                }
            }
        }
        #endregion

        // private fields
        private readonly BsonDocument _command;
        private readonly IReadWriteBinding _binding;
        private bool _disposed;
        private readonly IServer _server;

        // constructors
        /// <summary>
        /// Construct a new FailPoint with a specified name and custom arguments.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="binding">Must be a single server binding.</param>
        /// <param name="command">The command.</param>
        internal FailPoint(IServer server, IReadWriteBinding binding, BsonDocument command)
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
        internal IReadWriteBinding Binding => _binding;

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
        private void Configure(BsonDocument command, bool waitForConnected = false)
        {
            ExecuteCommand(command, waitForConnected);
        }

        private void ConfigureOff()
        {
            var name = _command[0].AsString;
            var command = new BsonDocument
            {
                { "configureFailPoint", name },
                { "mode", "off" }
            };
            Configure(command, true);
        }

        private void ExecuteCommand(BsonDocument command, bool waitForConnected)
        {
            if (waitForConnected)
            {
                // server can transition to unknown state during the test, wait until server is connected
                if (!SpinWait.SpinUntil(() => _server.Description.State == ServerState.Connected, 1000))
                {
                    throw new InvalidOperationException("Server is not connected.");
                }
            }

            var adminDatabase = new DatabaseNamespace("admin");
            var operation = new WriteCommandOperation<BsonDocument>(
                adminDatabase,
                command,
                BsonDocumentSerializer.Instance,
                new MessageEncoderSettings());

            operation.Execute(OperationContext.NoTimeout, _binding);
        }
    }
}
