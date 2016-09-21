/* Copyright 2013-2016 MongoDB Inc.
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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Authentication;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Represents a connection initializer (opens and authenticates connections).
    /// </summary>
    internal class ConnectionInitializer : IConnectionInitializer
    {
        #region static
        // private static fields
        private static readonly Lazy<BsonDocument> __driverDocument = new Lazy<BsonDocument>(CreateDriverDocument);
        private static readonly Lazy<BsonDocument> __osDocument = new Lazy<BsonDocument>(CreateOSDocument);
        private static readonly Lazy<string> __platformString = new Lazy<string>(GetPlatformString);

        // private static methods
        internal static BsonDocument CreateDriverDocument()
        {
            var assembly = typeof(ConnectionInitializer).GetTypeInfo().Assembly;
            var fileVersionAttribute = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
            var driverVersion = fileVersionAttribute.Version;

            return CreateDriverDocument(driverVersion);
        }

        internal static BsonDocument CreateDriverDocument(string driverVersion)
        {
            return new BsonDocument
            {
                { "name", "dotnet" },
                { "version", driverVersion }
            };
        }

        internal static BsonDocument CreateOSDocument()
        {
            string osType;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                osType = "Windows";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                osType = "Linux";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                osType = "macOS";
            }
            else
            {
                osType = "unknown";
            }

            var osName = RuntimeInformation.OSDescription.Trim();

            string architecture;
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.Arm: architecture = "arm"; break;
                case Architecture.Arm64: architecture = "arm64"; break;
                case Architecture.X64: architecture = "x86_64"; break;
                case Architecture.X86: architecture = "x86_32"; break;
                default: architecture = null; break;
            }

            string osVersion;
            var match = Regex.Match(osName, @" (?<version>\d+\.\d[^ ]*)");
            if (match.Success)
            {
                osVersion = match.Groups["version"].Value;
            }
            else
            {
                osVersion = null;
            }

            return CreateOSDocument(osType, osName, architecture, osVersion);
        }

        internal static BsonDocument CreateOSDocument(string osType, string osName, string architecture, string osVersion)
        {
            return new BsonDocument
            {
                { "type", osType },
                { "name", osName },
                { "architecture", architecture, architecture != null },
                { "version", osVersion, osVersion != null }
            };
        }

        internal static string GetPlatformString()
        {
            return RuntimeInformation.FrameworkDescription;
        }
        #endregion

        private readonly BsonDocument _clientDocument;

        public ConnectionInitializer(string applicationName)
        {
            _clientDocument = CreateClientDocument(applicationName);
        }

        public ConnectionDescription InitializeConnection(IConnection connection, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, nameof(connection));

            var isMasterProtocol = CreateIsMasterProtocol();
            var isMasterResult = new IsMasterResult(isMasterProtocol.Execute(connection, cancellationToken));

            var buildInfoProtocol = CreateBuildInfoProtocol();
            var buildInfoResult = new BuildInfoResult(buildInfoProtocol.Execute(connection, cancellationToken));

            var description = new ConnectionDescription(connection.ConnectionId, isMasterResult, buildInfoResult);

            AuthenticationHelper.Authenticate(connection, description, cancellationToken);

            try
            {
                var getLastErrorProtocol = CreateGetLastErrorProtocol();
                var getLastErrorResult = getLastErrorProtocol.Execute(connection, cancellationToken);

                description = UpdateConnectionIdWithServerValue(description, getLastErrorResult);
            }
            catch
            {
                // if we couldn't get the server's connection id, so be it.
            }

            return description;
        }

        public async Task<ConnectionDescription> InitializeConnectionAsync(IConnection connection, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, nameof(connection));

            var isMasterProtocol = CreateIsMasterProtocol();
            var isMasterResult = new IsMasterResult(await isMasterProtocol.ExecuteAsync(connection, cancellationToken).ConfigureAwait(false));

            var buildInfoProtocol = CreateBuildInfoProtocol();
            var buildInfoResult = new BuildInfoResult(await buildInfoProtocol.ExecuteAsync(connection, cancellationToken).ConfigureAwait(false));

            var description = new ConnectionDescription(connection.ConnectionId, isMasterResult, buildInfoResult);

            await AuthenticationHelper.AuthenticateAsync(connection, description, cancellationToken).ConfigureAwait(false);

            try
            {
                var getLastErrorProtocol = CreateGetLastErrorProtocol();
                var getLastErrorResult = await getLastErrorProtocol.ExecuteAsync(connection, cancellationToken).ConfigureAwait(false);

                description = UpdateConnectionIdWithServerValue(description, getLastErrorResult);
            }
            catch
            {
                // if we couldn't get the server's connection id, so be it.
            }

            return description;
        }

        // private methods
        private CommandWireProtocol<BsonDocument> CreateBuildInfoProtocol()
        {
            var buildInfoCommand = new BsonDocument("buildInfo", 1);
            var buildInfoProtocol = new CommandWireProtocol<BsonDocument>(
                DatabaseNamespace.Admin,
                buildInfoCommand,
                true,
                BsonDocumentSerializer.Instance,
               null);
            return buildInfoProtocol;
        }

        internal BsonDocument CreateClientDocument(string applicationName)
        {
            return CreateClientDocument(applicationName, __driverDocument.Value, __osDocument.Value, __platformString.Value);
        }

        internal BsonDocument CreateClientDocument(string applicationName, BsonDocument driverDocument, BsonDocument osDocument, string platformString)
        {
            return new BsonDocument
            {
                { "application", () => new BsonDocument("name", applicationName), applicationName != null },
                { "driver", driverDocument },
                { "os", osDocument },
                { "platform", platformString }
            };
        }

        private CommandWireProtocol<BsonDocument> CreateGetLastErrorProtocol()
        {
            var getLastErrorCommand = new BsonDocument("getLastError", 1);
            var getLastErrorProtocol = new CommandWireProtocol<BsonDocument>(
                DatabaseNamespace.Admin,
                getLastErrorCommand,
                true,
                BsonDocumentSerializer.Instance,
                null);
            return getLastErrorProtocol;
        }

        internal BsonDocument CreateIsMasterCommand()
        {
            return CreateIsMasterCommand(_clientDocument);
        }

        internal BsonDocument CreateIsMasterCommand(BsonDocument clientDocument)
        {
            return new BsonDocument
            {
                { "isMaster", 1 },
                { "client", clientDocument }
            };
        }

        private CommandWireProtocol<BsonDocument> CreateIsMasterProtocol()
        {
            var isMasterCommand = CreateIsMasterCommand();
            var isMasterProtocol = new CommandWireProtocol<BsonDocument>(
                DatabaseNamespace.Admin,
                isMasterCommand,
                true,
                BsonDocumentSerializer.Instance,
                null);
            return isMasterProtocol;
        }

        private ConnectionDescription UpdateConnectionIdWithServerValue(ConnectionDescription description, BsonDocument getLastErrorResult)
        {
            BsonValue connectionIdBsonValue;
            if (getLastErrorResult.TryGetValue("connectionId", out connectionIdBsonValue))
            {
                var connectionId = description.ConnectionId.WithServerValue(connectionIdBsonValue.ToInt32());
                description = description.WithConnectionId(connectionId);
            }

            return description;
        }
    }
}
