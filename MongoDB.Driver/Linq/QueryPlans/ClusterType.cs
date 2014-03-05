using System;

namespace MongoDB.Driver.Linq.QueryPlans
{
	/// <summary>
	/// clusteredType is a string that reports the access pattern for shards.
	/// </summary>
	public enum ClusterType
	{
		/// <summary>
		/// ParallelSort, if the mongos queries shards in parallel.
		/// </summary>
		ParallelSort,
		/// <summary>
		/// SerialServer, if the mongos queries shards sequentially.
		/// </summary>
		SerialServer
	}
}