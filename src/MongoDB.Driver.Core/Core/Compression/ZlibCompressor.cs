using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Net.Configuration;
using CompressionLevel = Ionic.Zlib.CompressionLevel;
using CompressionMode = Ionic.Zlib.CompressionMode;

namespace MongoDB.Driver.Core.Compression
{
	/// <summary>
	/// Compressors using zlib algorithm
	/// </summary>
	public sealed class ZlibCompressor : ICompressor
	{
		private readonly int _compressionLevel;

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
			_compressionLevel = compressionLevel;
		}

		/// <inheritdoc />
		[SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
		public byte[] Compress(byte[] bytesToCompress, int offset)
		{
			using (var memoryStream = new MemoryStream())
			{
				using (var zlibStream = new Ionic.Zlib.ZlibStream(memoryStream, CompressionMode.Compress, GetCompressionLevel()))
				{
					zlibStream.Write(bytesToCompress, offset, bytesToCompress.Length - offset);
				}
				
				return memoryStream.ToArray();
			}
		}

		/// <inheritdoc />
		public byte[] Decompress(byte[] bytesToDecompress)
		{
			using (var memoryStream = new MemoryStream())
			{
				using (var zlibStream = new Ionic.Zlib.ZlibStream(memoryStream, CompressionMode.Decompress))
				{
					zlibStream.Write(bytesToDecompress, 0, bytesToDecompress.Length);
				}
				
				return memoryStream.ToArray();
			}
		}

		private CompressionLevel GetCompressionLevel()
		{
			if (_compressionLevel < 0)
				return CompressionLevel.Default;

			if (_compressionLevel > 9)
				return CompressionLevel.BestCompression;

			return (CompressionLevel) _compressionLevel;
		}
	}
}