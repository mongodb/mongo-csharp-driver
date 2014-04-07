using System;

namespace MongoDB.Driver.Linq.QueryPlans
{
	/// <summary>
	/// Cursor is a string that reports the type of cursor used by the query operation.
	/// </summary>
	public enum CursorType
	{
		/// <summary>
		/// Cusor type could not be determined.
		/// </summary>
		Unknown,
		/// <summary>
		/// BasicCursor indicates a full collection scan.
		/// </summary>
		BasicCursor,
		/// <summary>
		/// BtreeCursor indicates that the query used an index. The cursor includes name of the index. When a query uses an index, the output of explain() includes indexBounds details.
		/// </summary>
		BtreeCursor,
		/// <summary>
		/// GeoSearchCursor indicates that the query used a geospatial index.
		/// </summary>
		GeoSearchCursor
	}
}