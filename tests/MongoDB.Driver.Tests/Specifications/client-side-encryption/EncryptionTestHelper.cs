/* Copyright 2010-present MongoDB Inc.
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
using System.Linq;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Tests.Specifications.client_side_encryption
{
    public static class EncryptionTestHelper
    {
        public static void ConfigureDefaultExtraOptions(Dictionary<string, object> extraOptions)
        {
            Ensure.IsNotNull(extraOptions, nameof(extraOptions));

            if (!extraOptions.ContainsKey("mongocryptdSpawnPath"))
            {
                var mongocryptdSpawnPath = Environment.GetEnvironmentVariable("MONGODB_BINARIES") ?? "";
                extraOptions.Add("mongocryptdSpawnPath", mongocryptdSpawnPath);
            }

            var mongocryptdPort = Environment.GetEnvironmentVariable("FLE_MONGOCRYPTD_PORT");
            if (mongocryptdPort != null)
            {
                if (!extraOptions.ContainsKey("mongocryptdURI"))
                {
                    extraOptions.Add("mongocryptdURI", $"mongodb://localhost:{mongocryptdPort}");
                }

                var portKey = "--port=";
                var portValue = $" {portKey}{mongocryptdPort}";
                if (extraOptions.TryGetValue("mongocryptdSpawnArgs", out var args))
                {
                    object effectiveValue;
                    switch (args)
                    {
                        case string str:
                            {
                                effectiveValue = str.Contains(portKey) ? str : str + portValue;
                            }
                            break;
                        case IEnumerable enumerable:
                            {
                                var list = new List<string>(enumerable.Cast<string>());
                                if (list.Any(v => v.Contains(portKey)))
                                {
                                    effectiveValue = list;
                                }
                                else
                                {
                                    list.Add(portValue);
                                    effectiveValue = list;
                                }
                            }
                            break;
                        default: throw new Exception("Unsupported mongocryptdSpawnArgs type.");
                    }
                    extraOptions["mongocryptdSpawnArgs"] = effectiveValue;
                }
                else
                {
                    extraOptions.Add("mongocryptdSpawnArgs", portValue);
                }
            }
        }
    }
}
