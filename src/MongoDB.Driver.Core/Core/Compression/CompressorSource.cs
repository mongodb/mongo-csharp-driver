/* Copyright 2019–present MongoDB Inc.
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Compression
{
    /// <summary>
    /// Represents a compressor source.
    /// </summary>
    public interface ICompressorSource
    {
        /// <summary>
        /// Gets or creates a compressor based on the compressor type.
        /// </summary>
        /// <param name="compressorType">The compressor type.</param>
        /// <returns>The compressor.</returns>
        ICompressor Get(CompressorType compressorType);
    }

    internal class CompressorSource : ICompressorSource
    {
        #region static
        public static bool IsCompressorSupported(CompressorType compressorType)
        {
            switch (compressorType)
            {
                case CompressorType.Snappy:
                case CompressorType.Zlib:
                case CompressorType.ZStandard:
                case CompressorType.Noop: // This is realistically only used for testing
                    return true;
                default:
                    return false;
            }
        }
        #endregion

        private readonly IReadOnlyList<CompressorConfiguration> _allowedCompressors;
        private readonly ConcurrentDictionary<CompressorType, ICompressor> _cache;

        public CompressorSource(IReadOnlyList<CompressorConfiguration> allowedCompressors)
        {
            _allowedCompressors = Ensure.IsNotNull(allowedCompressors, nameof(allowedCompressors));
            _cache = new ConcurrentDictionary<CompressorType, ICompressor>();
        }

        public ICompressor Get(CompressorType compressorType)
        {
            return _cache.GetOrAdd(compressorType, CreateCompressor);
        }

        private ICompressor CreateCompressor(CompressorConfiguration compressorConfiguration)
        {
            switch (compressorConfiguration.Type)
            {
                case CompressorType.Noop:
                    return new NoopCompressor();
                case CompressorType.Snappy:
                    return new SnappyCompressor();
                case CompressorType.Zlib:
                    {
                        int? zlibCompressionLevel = null;
                        if (compressorConfiguration.Properties.ContainsKey("Level"))
                        {
                            zlibCompressionLevel = (int)compressorConfiguration.Properties["Level"];
                        }

                        return new ZlibCompressor(zlibCompressionLevel);
                    }
                case CompressorType.ZStandard:
                    return new ZstandardCompressor();
            }

            throw new NotSupportedException($"The compressor {compressorConfiguration.Type} is not supported.");
        }

        private ICompressor CreateCompressor(CompressorType compressorType)
        {
            var compressorConfiguration = _allowedCompressors.FirstOrDefault(c => c.Type == compressorType);
            if (compressorConfiguration == null)
            {
                throw new NotSupportedException($"The compressor {compressorType} is not one of the allowed compressors.");
            }

            return CreateCompressor(compressorConfiguration);
        }
    }
}
