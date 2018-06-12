using System.Collections.Generic;

namespace MongoDB.Driver.Core.Configuration
{
	/// <summary>
	/// Describes a compressor.
	/// </summary>
	public sealed class MongoCompressor
	{
		/// <summary>
		/// Key for the compression level
		/// </summary>
		public const string Level = "Level";
		
		/// <summary>
		/// Name of the compressor
		/// </summary>
		public string Name { get; set; }
		
		/// <summary>
		/// Properties of the compressor
		/// </summary>
		public IDictionary<string, object> Properties { get; set; }
	}
}