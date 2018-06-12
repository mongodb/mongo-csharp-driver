using System.Collections.Generic;

namespace MongoDB.Driver.Core.Configuration
{
	/// <summary>
	/// Describes a compressor.
	/// </summary>
	public sealed class MongoCompressor
	{
		/// <summary>
		/// Initializes an instance of <see cref="MongoCompressor"/>.
		/// </summary>
		/// <param name="name">Name of the compressor.</param>
		public MongoCompressor(string name)
		{
			Name = name;
			Properties = new Dictionary<string, object>();
		}

		/// <summary>
		/// Key for the compression level
		/// </summary>
		public const string Level = "Level";
		
		/// <summary>
		/// Name of the compressor
		/// </summary>
		public string Name { get; }
		
		/// <summary>
		/// Properties of the compressor
		/// </summary>
		public IDictionary<string, object> Properties { get; }
	}
}