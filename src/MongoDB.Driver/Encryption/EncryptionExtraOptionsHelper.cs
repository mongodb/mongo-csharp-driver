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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Encryption
{
    internal static class EncryptionExtraOptionsHelper
    {
        private static readonly Dictionary<string, Type[]> __supportedExtraOptions = new()
        {
            { "cryptSharedLibPath", new [] { typeof(string) } },
            { "cryptSharedLibRequired", new [] { typeof(bool) } },
            { "mongocryptdURI", new [] { typeof(string) } },
            { "mongocryptdBypassSpawn", new [] { typeof(bool) } },
            { "mongocryptdSpawnPath", new [] { typeof(string) } },
            { "mongocryptdSpawnArgs", new [] { typeof(string), typeof(IEnumerable<string>) } }
        };

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

        public static string ExtractCryptSharedLibPath(IReadOnlyDictionary<string, object> dict) =>
            dict.GetValueOrDefault<string, string, object>("cryptSharedLibPath");

        public static bool? ExtractCryptSharedLibRequired(IReadOnlyDictionary<string, object> dict) =>
            dict.GetValueOrDefault<bool?, string, object>("cryptSharedLibRequired");

    }
}
