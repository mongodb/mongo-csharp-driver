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
using System.Linq;
using System.Reflection;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Encryption
{
    internal static class EncryptionExtraOptionsValidator
    {
        #region static
        private static readonly Dictionary<string, Type[]> __supportedExtraOptions = new Dictionary<string, Type[]>
        {
            { "mongocryptdURI", new [] { typeof(string) } },
            { "mongocryptdBypassSpawn", new [] { typeof(bool) } },
            { "mongocryptdSpawnPath", new [] { typeof(string) } },
            { "mongocryptdSpawnArgs", new [] { typeof(string), typeof(IEnumerable<string>) } }
        };
        #endregion

        public static void EnsureThatExtraOptionsAreValid(IReadOnlyDictionary<string, object> extraOptions)
        {
            if (extraOptions == null)
            {
                return;
            }

            foreach (var extraOption in extraOptions)
            {
                if (__supportedExtraOptions.TryGetValue(extraOption.Key, out var validTypes))
                {
                    var extraOptionValue = Ensure.IsNotNull(extraOption.Value, nameof(extraOptions));
                    var extraOptionValueType = extraOptionValue.GetType();
                    var isExtraOptionValueTypeValid = validTypes.Any(t => t.GetTypeInfo().IsAssignableFrom(extraOptionValueType));
                    if (!isExtraOptionValueTypeValid)
                    {
                        throw new ArgumentException($"Extra option {extraOption.Key} has invalid type: {extraOptionValueType}.", nameof(extraOptions));
                    }
                }
                else
                {
                    throw new ArgumentException($"Invalid extra option key: {extraOption.Key}.", nameof(extraOptions));
                }
            }
        }
    }

    internal class MongocryptdFactory
    {
        private readonly IReadOnlyDictionary<string, object> _extraOptions;

        public MongocryptdFactory(IReadOnlyDictionary<string, object> extraOptions)
        {
            _extraOptions = extraOptions ?? new Dictionary<string, object>();
        }

        // public methods
        public IMongoClient CreateMongocryptdClient()
        {
            var connectionString = CreateMongocryptdConnectionString();
            var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
            clientSettings.ServerSelectionTimeout = TimeSpan.FromMilliseconds(1000);
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

                if (!Path.HasExtension(path))
                {
                    string fileName = "mongocryptd.exe";
                    path = Path.Combine(path, fileName);
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
                                args += $"--{item.ToString().TrimStart('-')} ";
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
                    // "nul" is the windows specific value. Unix-based platforms should use "/dev/null"
                    args += " --logpath nul";

                    if (!args.Contains("logappend"))
                    {
                        args += " --logappend";
                    }
                }
                args = args.Trim();

                return true;
            }

            return false;
        }

        private void StartProcess(string path, string args)
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
