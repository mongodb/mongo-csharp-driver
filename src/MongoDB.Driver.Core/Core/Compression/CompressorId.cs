namespace MongoDB.Driver.Core.Compression
{
	/// <summary>
	/// Represents the compressor id.
	/// </summary>
	public enum CompressorId
	{
		/// <summary>
		/// No compression.
		/// </summary>
		noop,
		/// <summary>
		/// Compression using snappy algorithm.
		/// </summary>
		snappy,
		/// <summary>
		/// Compression using zlib algorithm. 
		/// </summary>
		zlib
	}
}