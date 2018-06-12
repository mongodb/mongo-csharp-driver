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
		noop = 0,
		///// <summary>
		///// Compression using snappy algorithm. NOT SUPPORTED YET.
		///// </summary>
		//snappy = 1, 
		/// <summary>
		/// Compression using zlib algorithm. 
		/// </summary>
		zlib = 2
	}
}