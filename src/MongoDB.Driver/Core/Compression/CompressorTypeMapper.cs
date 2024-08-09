/* Copyright 2020–present MongoDB Inc.
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

namespace MongoDB.Driver.Core.Compression
{
    internal static class CompressorTypeMapper
    {
        public static string ToServerName(CompressorType compressorType)
        {
            switch (compressorType)
            {
                case CompressorType.Noop:
                    return "noop";
                case CompressorType.Zlib:
                    return "zlib";
                case CompressorType.Snappy:
                    return "snappy";
                case CompressorType.ZStandard:
                    return "zstd";
                default:
                    throw new ArgumentOutOfRangeException(nameof(compressorType));
            }
        }

        public static bool TryFromServerName(string serverName, out CompressorType compressorType)
        {
            compressorType = default;
            switch (serverName.ToLowerInvariant())
            {
                case "noop": compressorType = CompressorType.Noop; break;
                case "zlib": compressorType = CompressorType.Zlib; break;
                case "snappy": compressorType = CompressorType.Snappy; break;
                case "zstd": compressorType = CompressorType.ZStandard; break;
                default: return false;
            }
            return true;
        }
    }
}
