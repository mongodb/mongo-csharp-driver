using System.Diagnostics.CodeAnalysis;
using System.IO;
using CompressionLevel = Ionic.Zlib.CompressionLevel;
using CompressionMode = Ionic.Zlib.CompressionMode;

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
				using (var zlibStream = new Ionic.Zlib.ZlibStream(memoryStream, CompressionMode.Compress, _compressionLevel))
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
				using (var zlibStream = new Ionic.Zlib.ZlibStream(memoryStream, CompressionMode.Decompress))
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