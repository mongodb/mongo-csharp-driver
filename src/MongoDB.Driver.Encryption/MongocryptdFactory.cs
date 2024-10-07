/* Copyright 2019-present MongoDB Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MongoDB.Driver.Core.Events;

namespace MongoDB.Driver.Encryption
{
    internal class MongocryptdFactory
    {
        private readonly bool? _bypassQueryAnalysis;
        private readonly IEventSubscriber _eventSubscriber;
        private readonly IReadOnlyDictionary<string, object> _extraOptions;

        public MongocryptdFactory(IReadOnlyDictionary<string, object> extraOptions, bool? bypassQueryAnalysis, IEventSubscriber eventSubscriber = null)
        {
            _bypassQueryAnalysis = bypassQueryAnalysis;
            _eventSubscriber = eventSubscriber;
            _extraOptions = extraOptions ?? new Dictionary<string, object>();
        }

        // public methods
        public IMongoClient CreateMongocryptdClient()
        {
            var clientSettings = CreateMongocryptdClientSettings();

            if (_eventSubscriber != null)
            {
                clientSettings.ClusterConfigurator = c => c.Subscribe(_eventSubscriber);
            }

            return new MongoClient(clientSettings);
        }

        public void SpawnMongocryptdProcessIfRequired()
        {
            if (ShouldMongocryptdBeSpawned(out var path, out var args))
            {
                StartProcess(path, args);
            }
        }

        // private methods
        private MongoClientSettings CreateMongocryptdClientSettings()
        {
            var connectionString = CreateMongocryptdConnectionString();
            var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
            clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);
            return clientSettings;
        }

        private string CreateMongocryptdConnectionString()
        {
            if (_extraOptions.TryGetValue("mongocryptdURI", out var connectionString))
            {
                return (string)connectionString;
            }
            else
            {
                return "mongodb://localhost:27020";
            }
        }

        private bool ShouldMongocryptdBeSpawned(out string path, out string args)
        {
            path = null;
            args = null;

            // bypassAutoEncryption=true doesn't enable autoencryption
            if (_bypassQueryAnalysis.GetValueOrDefault(defaultValue: false))
            {
                return false;
            }

            // csfle shared library option is not validated here as
            // Mongocryptd invocation is libmongocrypt responsibility
            if (!_extraOptions.TryGetValue("mongocryptdBypassSpawn", out var mongoCryptBypassSpawn)
                || !(bool)mongoCryptBypassSpawn)
            {
                if (_extraOptions.TryGetValue("mongocryptdSpawnPath", out var objPath))
                {
                    path = (string)objPath;
                }
                else
                {
                    path = string.Empty; // look at the PATH env variable
                }

                if (string.IsNullOrEmpty(path) || Directory.Exists(path))
                {
                    string fileName = $"mongocryptd{GetMongocryptdExtension()}";
                    path = Path.Combine(path ?? "", fileName);
                }

                args = string.Empty;
                if (_extraOptions.TryGetValue("mongocryptdSpawnArgs", out var mongocryptdSpawnArgs))
                {
                    switch (mongocryptdSpawnArgs)
                    {
                        case string str:
                            args += str;
                            break;
                        case IEnumerable enumerable:
                            foreach (var item in enumerable)
                            {
                                args += $"--{item.ToString().TrimStart(' ').TrimStart('-')} ";
                            }
                            break;
                        default:
                            throw new InvalidCastException($"Invalid type: {mongocryptdSpawnArgs.GetType().Name} of mongocryptdSpawnArgs option.");
                    }
                }

                args = args.Trim();
                if (!args.Contains("idleShutdownTimeoutSecs"))
                {
                    args += " --idleShutdownTimeoutSecs 60";
                }

                if (!args.Contains("logpath")) // disable logging by the mongocryptd process
                {
                    args += $" --logpath {GetLogPath()}";

                    if (!args.Contains("logappend"))
                    {
                        args += " --logappend";
                    }
                }
                args = args.Trim();

                return true;
            }

            return false;

            string GetMongocryptdExtension()
            {
                var currentOperatingSystem = OperatingSystemHelper.CurrentOperatingSystem;
                switch (currentOperatingSystem)
                {
                    case OperatingSystemPlatform.Windows:
                        return ".exe";
                    case OperatingSystemPlatform.Linux:
                    case OperatingSystemPlatform.MacOS:
                    default:
                        return "";
                }
            }

            string GetLogPath()
            {
                var currentOperatingSystem = OperatingSystemHelper.CurrentOperatingSystem;
                switch (currentOperatingSystem)
                {
                    case OperatingSystemPlatform.Windows:
                        // "nul" is the windows specific value
                        return "nul";
                    // Unix - based platforms should use "/dev/null"
                    case OperatingSystemPlatform.Linux:
                    case OperatingSystemPlatform.MacOS:
                    default:
                        return "/dev/null";
                }
            }
        }

        private static void StartProcess(string path, string args)
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.Arguments = args;
                    process.StartInfo.FileName = path;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.UseShellExecute = false;

                    if (!process.Start())
                    {
                        // skip it. This case can happen if no new process resource is started
                        // (for example, if an existing process is reused)
                    }
                }
            }
            catch (Exception ex)
            {
                throw new MongoClientException("Exception starting mongocryptd process. Is mongocryptd on the system path?", ex);
            }
        }
    }
}
