/* Copyright 2013-present MongoDB Inc.
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

using System.Diagnostics.CodeAnalysis;
using System.IO;
using SharpCompress.Compressors;
using SharpCompress.Compressors.Deflate;

namespace MongoDB.Driver.Core.Compression
{
	/// <summary>
	/// Compressors using zlib algorithm
	/// </summary> 
	public sealed class ZlibCompressor : ICompressor
	{
		private readonly CompressionLevel _compressionLevel;

		/// <inheritdoc />
		public string Name => "zlib";

		/// <inheritdoc />
		public CompressorId Id => CompressorId.zlib;

		/// <summary>
		/// Initializes a new instance of the <see cref="ZlibCompressor" /> class.
		/// </summary>
		/// <param name="compressionLevel">The compression level.</param>
		public ZlibCompressor(int compressionLevel)
		{
			_compressionLevel = GetCompressionLevel(compressionLevel);
		}

		/// <inheritdoc />
		[SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
		public byte[] Compress(byte[] bytesToCompress, int offset)
		{
			using (var memoryStream = new MemoryStream())
			{
				using (var zlibStream = new ZlibStream(memoryStream, CompressionMode.Compress, _compressionLevel))
				{
					zlibStream.Write(bytesToCompress, offset, bytesToCompress.Length - offset);
				}
				
				return memoryStream.ToArray();
			}
		}

		/// <inheritdoc />
		[SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
		public byte[] Decompress(byte[] bytesToDecompress)
		{
			using (var memoryStream = new MemoryStream())
			{
				using (var zlibStream = new ZlibStream(memoryStream, CompressionMode.Decompress))
				{
					zlibStream.Write(bytesToDecompress, 0, bytesToDecompress.Length);
				}
				
				return memoryStream.ToArray();
			}
		}

		private static CompressionLevel GetCompressionLevel(int compressionLevel)
		{
			if (compressionLevel < 0)
				return CompressionLevel.Default;

			if (compressionLevel > 9)
				return CompressionLevel.BestCompression;

			return (CompressionLevel) compressionLevel;
		}
	}
}