namespace MongoDB.Driver.Core.Compression
{
	/// <summary>
	/// Represents a compressor.
	/// </summary>
	public interface ICompressor
	{
		/// <summary>
		/// Gets the name of the compressor.
		/// </summary>
		string Name { get; }
		
		/// <summary>
		/// Gets the id of the compressor
		/// </summary>
		CompressorId Id { get; }

		/// <summary>
		/// Compresses the specified byte array with a given offset.
		/// </summary>
		/// <param name="bytesToCompress">Bytes to compress.</param>
		/// <param name="offset">Offset of the bytes.</param>
		byte[] Compress(byte[] bytesToCompress, int offset);

		/// <summary>
		/// Decompresses the specified byte array.
		/// </summary>
		/// <param name="bytesToDecompress">Bytes to decompress.</param>
		byte[] Decompress(byte[] bytesToDecompress);
	}
}