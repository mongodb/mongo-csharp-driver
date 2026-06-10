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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Connections
{
    internal class ClientDocumentHelper
    {
        #region static
        // private static fields
        private static Lazy<BsonDocument> __driverDocument;
        private static Lazy<BsonDocument> __envDocument;
        private static Lazy<BsonDocument> __osDocument;
        private static Lazy<string> __platformString;
        private static IEnvironmentVariableProvider __environmentVariableProvider;
        private static IFileSystemProvider __fileSystemProvider;

        internal static void Initialize()
        {
            __driverDocument = new Lazy<BsonDocument>(CreateDriverDocument);
            __envDocument = new Lazy<BsonDocument>(CreateEnvDocument);
            __osDocument = new Lazy<BsonDocument>(CreateOSDocument);
            __platformString = new Lazy<string>(GetPlatformString);
            __environmentVariableProvider = EnvironmentVariableProvider.Instance;
            __fileSystemProvider = FileSystemProvider.Instance;
        }

        static ClientDocumentHelper() => Initialize();

        internal static void SetEnvironmentVariableProvider(IEnvironmentVariableProvider environmentVariableProvider)
        {
            __environmentVariableProvider = environmentVariableProvider;
        }

        internal static void SetFileSystemProvider(IFileSystemProvider fileSystemProvider)
        {
            __fileSystemProvider = fileSystemProvider;
        }

        // private static methods
        internal static BsonDocument CreateClientDocument(string applicationName, LibraryInfo libraryInfo)
        {
            return CreateClientDocument(applicationName, __driverDocument.Value, __osDocument.Value, __platformString.Value, __envDocument.Value, libraryInfo);
        }

        internal static BsonDocument CreateClientDocument(string applicationName, BsonDocument driverDocument, BsonDocument osDocument, string platformString, BsonDocument envDocument, LibraryInfo libraryInfo)
        {
            driverDocument = AppendLibraryInfoToDriverDocument(driverDocument, libraryInfo);

            var clientDocument = new BsonDocument
            {
                { "application", () => new BsonDocument("name", applicationName), applicationName != null },
                { "driver", driverDocument },
                { "os", osDocument.Clone() }, // clone because we might be removing optional fields from this particular clientDocument
                { "platform", platformString },
                { "env", () => envDocument.Clone(), envDocument != null }
            };

            return RemoveOptionalFieldsUntilDocumentIsLessThan512Bytes(clientDocument);
        }

        internal static BsonDocument CreateDriverDocument()
        {
            var assembly = typeof(ConnectionInitializer).GetTypeInfo().Assembly;
            var driverVersion = GetAssemblyVersion(assembly);

            return CreateDriverDocument(driverVersion);
        }

        internal static BsonDocument CreateDriverDocument(string driverVersion)
        {
            var driverName = "mongo-csharp-driver";
            if (TryGetType("MongoDB.AspNetCore.OData.MongoEnableQueryAttribute, MongoDB.AspNetCore.OData", out var queryAttributeType))
            {
                var odataVersion = GetAssemblyVersion(queryAttributeType.Assembly);
                driverVersion = $"{driverVersion}|{odataVersion}";
                driverName = $"{driverName}|odata";
            }

            if (TryGetType("MongoDB.EntityFrameworkCore.Query.MongoQueryContext, MongoDB.EntityFrameworkCore", out var queryContextType))
            {
                var efVersion = GetAssemblyVersion(queryContextType.Assembly);
                driverVersion = $"{driverVersion}|{efVersion}";
                driverName = $"{driverName}|efcore";
            }

            return new BsonDocument
            {
                { "name", driverName },
                { "version", driverVersion }
            };
        }

        internal static BsonDocument CreateEnvDocument()
        {
            const string awsLambdaName = "aws.lambda";
            const string azureFuncName = "azure.func";
            const string gcpFuncName = "gcp.func";
            const string vercelName = "vercel";

            var name = GetName();

            if (name != null)
            {
                var timeout = GetTimeoutSec(name);
                var memoryDb = GetMemoryMb(name);
                var region = GetRegion(name);
                var container = GetContainerDocument();

                return new BsonDocument
                {
                    { "name", name },
                    { "timeout_sec", timeout, timeout.HasValue },
                    { "memory_mb", memoryDb, memoryDb.HasValue },
                    { "region", region, region != null },
                    { "container", container, container != null }
                };
            }
            else
            {
                return null;
            }

            string GetName()
            {
                string result = null;

                if ((__environmentVariableProvider.GetEnvironmentVariable("AWS_EXECUTION_ENV")?.StartsWith("AWS_Lambda_") ?? false) ||
                    __environmentVariableProvider.GetEnvironmentVariable("AWS_LAMBDA_RUNTIME_API") != null)
                {
                    result = awsLambdaName;
                }
                if (__environmentVariableProvider.GetEnvironmentVariable("FUNCTIONS_WORKER_RUNTIME") != null)
                {
                    if (result != null) return null;

                    result = azureFuncName;
                }
                if (__environmentVariableProvider.GetEnvironmentVariable("K_SERVICE") != null || __environmentVariableProvider.GetEnvironmentVariable("FUNCTION_NAME") != null)
                {
                    if (result != null) return null;

                    result = gcpFuncName;
                }
                if (__environmentVariableProvider.GetEnvironmentVariable("VERCEL") != null)
                {
                    if (result != null && result != awsLambdaName) return null;

                    result = vercelName;
                }

                return result;
            }

            string GetRegion(string name) =>
                name switch
                {
                    awsLambdaName => __environmentVariableProvider.GetEnvironmentVariable("AWS_REGION"),
                    gcpFuncName => __environmentVariableProvider.GetEnvironmentVariable("FUNCTION_REGION"),
                    vercelName => __environmentVariableProvider.GetEnvironmentVariable("VERCEL_REGION"),
                    _ => null
                };

            int? GetMemoryMb(string name) =>
                name switch
            {
                awsLambdaName => GetIntValue("AWS_LAMBDA_FUNCTION_MEMORY_SIZE"),
                gcpFuncName => GetIntValue("FUNCTION_MEMORY_MB"),
                _ => null,
            };

            int? GetTimeoutSec(string name) =>
                name switch
                {
                    gcpFuncName => GetIntValue("FUNCTION_TIMEOUT_SEC"),
                    _ => null,
                };

            BsonDocument GetContainerDocument()
            {
                var isExecutionContainerDocker = __fileSystemProvider.File.Exists("/.dockerenv");
                var isOrchestratorKubernetes = __environmentVariableProvider.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST") != null;

                if (isExecutionContainerDocker || isOrchestratorKubernetes)
                {
                    return new()
                    {
                        { "runtime", "docker", isExecutionContainerDocker },
                        { "orchestrator", "kubernetes", isOrchestratorKubernetes }
                    };
                }

                return null;
            }

            int? GetIntValue(string environmentVariable) =>
                int.TryParse(__environmentVariableProvider.GetEnvironmentVariable(environmentVariable), out var value) ? value : null;
        }

        internal static BsonDocument CreateOSDocument()
        {
            string osType;
            string osName;
            string architecture;
            string osVersion;

            if (TryGetType("Mono.Runtime", out _))
            {
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.Win32S:
                    case PlatformID.Win32Windows:
                    case PlatformID.Win32NT:
                    case PlatformID.WinCE:
                        osType = "Windows";
                        break;

                    case PlatformID.Unix:
                        osType = "Linux";
                        break;

                    case PlatformID.Xbox:
                        osType = "XBox";
                        break;

                    case PlatformID.MacOSX:
                        osType = "macOS";
                        break;

                    default:
                        osType = "unknown";
                        break;
                }

                osName = Environment.OSVersion.VersionString;

                PortableExecutableKinds peKind;
                ImageFileMachine machine;
                typeof(object).Module.GetPEKind(out peKind, out machine);
                switch (machine)
                {
                    case ImageFileMachine.I386:
                        architecture = "x86_32";
                        break;
                    case ImageFileMachine.IA64:
                    case ImageFileMachine.AMD64:
                        architecture = "x86_64";
                        break;
                    case ImageFileMachine.ARM:
                        architecture = "arm" + (Environment.Is64BitProcess ? "64" : "");
                        break;
                    default:
                        architecture = null;
                        break;
                }

                osVersion = Environment.OSVersion.Version.ToString();

                return CreateOSDocument(osType, osName, architecture, osVersion);
            }

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

            osName = RuntimeInformation.OSDescription.Trim();

            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.Arm: architecture = "arm"; break;
                case Architecture.Arm64: architecture = "arm64"; break;
                case Architecture.X64: architecture = "x86_64"; break;
                case Architecture.X86: architecture = "x86_32"; break;
                default: architecture = null; break;
            }

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

        internal static BsonDocument RemoveOneOptionalField(BsonDocument clientDocument)
        {
            if (clientDocument.TryGetValue("env", out var env))
            {
                if (TryRemoveElement(env.AsBsonDocument, "name", onlyLeaveElement: true))
                {
                    return clientDocument;
                }
            }

            if (clientDocument.TryGetValue("os", out var os))
            {
                if (TryRemoveElement(os.AsBsonDocument, "type", onlyLeaveElement: true))
                {
                    return clientDocument;
                }
            }

            if (TryRemoveElement(clientDocument, "env"))
            {
                return clientDocument;
            }

            if (TryRemoveElement(clientDocument, "platform"))
            {
                return clientDocument;
            }

            return null;

            static bool TryRemoveElement(BsonDocument document, string elementName, bool onlyLeaveElement = false)
            {
                if (document.TryGetElement(elementName, out var element))
                {
                    if (onlyLeaveElement)
                    {
                        if (document.ElementCount == 1)
                        {
                            return false;
                        }

                        RemoveAll(document, elementName);
                        return true;
                    }
                    else
                    {
                        document.RemoveElement(element);
                        return true;
                    }
                }

                return false;
            }

            static void RemoveAll(BsonDocument document, string protectedField = null)
            {
                for (int i = document.ElementCount - 1; i >= 0; i--)
                {
                    var element = document.GetElement(i);
                    if (protectedField == null || element.Name != protectedField)
                    {
                        document.RemoveElement(element);
                    }
                }
            }
        }

        internal static BsonDocument RemoveOptionalFieldsUntilDocumentIsLessThan512Bytes(BsonDocument clientDocument)
        {
            while (clientDocument != null && clientDocument.ToBson().Length > 512)
            {
                clientDocument = RemoveOneOptionalField(clientDocument);
            }

            return clientDocument;
        }

        private static BsonDocument AppendLibraryInfoToDriverDocument(BsonDocument driverDocument, LibraryInfo libraryInfo)
        {
            if (libraryInfo == null)
            {
                return driverDocument;
            }

            var driverName = $"{driverDocument["name"]}|{libraryInfo.Name}";
            var driverVersion = driverDocument["version"];

            if (!string.IsNullOrWhiteSpace(libraryInfo.Version))
            {
                driverVersion = $"{driverVersion}|{libraryInfo.Version}";
            }

            return new()
            {
                { "name", driverName },
                { "version", driverVersion }
            };
        }

        private static bool TryGetType(string typeName, out Type type)
        {
            try
            {
                type = Type.GetType(typeName);
                return type != null;
            }
            catch
            {
                // ignore any exceptions here.
                type = null;
                return false;
            }
        }

        internal static string GetAssemblyVersion(Assembly assembly)
        {
            var versionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var hashIndex = versionAttribute.InformationalVersion.IndexOf('+');
            if (hashIndex == -1)
            {
                return versionAttribute.InformationalVersion;
            }

            return versionAttribute.InformationalVersion.Substring(0, hashIndex);
        }

        #endregion
    }
}
