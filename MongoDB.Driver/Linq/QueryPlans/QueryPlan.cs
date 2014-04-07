using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Driver.Linq.QueryPlans
{
	/// <summary>
	/// 
	/// </summary>
	[BsonIgnoreExtraElements]
	public class QueryPlan
	{
		/// <summary>
		/// Cursor is a string that reports the type of cursor used by the query operation.
		/// </summary>
		[BsonElement("cursor")]
		public string Cursor { get; set; }

		/// <summary>
		/// Cursor is a string that reports the type of cursor used by the query operation.
		/// </summary>
		public CursorType CursorType
		{
			get
			{
				try
				{
					var s = Cursor.Split(' ')[0];
					CursorType result = (CursorType) Enum.Parse(typeof (CursorType), s, true);
					return result;
				}
				catch (Exception)
				{
					return CursorType.Unknown;
				}
				
			}
		}

		/// <summary>
		/// isMultiKey is a boolean. When true, the query uses a multikey index, where one of the fields in the index holds an array.
		/// </summary>
		[BsonElement("isMultiKey")]
		public bool IsMultiKey { get; set; }

		/// <summary>
		/// n is a number that reflects the number of documents that match the query selection criteria.
		/// </summary>
		[BsonElement("n")]
		public int DocumentCount { get; set; }

		/// <summary>
		/// pecifies the total number of documents or index entries scanned during the database operation. You want n and nscanned to be close in value as possible. The nscanned value may be higher than the nscannedObjects value, such as if the index covers a query. See indexOnly.
		/// </summary>
		[BsonElement("nscanned")]
		public int ScannedDocumentCount { get; set; }

		/// <summary>
		/// nscannedObjectsAllPlans is a number that reflects the total number of documents scanned for all query plans during the database operation.
		/// </summary>
		[BsonElement("nscannedObjectsAllPlans")]
		public int ScannedCount { get; set; }

		/// <summary>
		/// nscannedAllPlans is a number that reflects the total number of documents or index entries scanned for all query plans during the database operation.
		/// </summary>
		[BsonElement("nscannedAllPlans")]
		public int ScannedDocumentCountAllPlans { get; set; }

		/// <summary>
		/// Specifies the total number of documents scanned during the query. The nscannedObjects may be lower than nscanned, such as if the index covers a query. See indexOnly. Additionally, the nscannedObjects may be lower than nscanned in the case of multikey index on an array field with duplicate documents.
		/// </summary>
		[BsonElement("nscannedObjects")]
		public int ScannedCountAllPlans { get; set; }

		/// <summary>
		/// scanAndOrder is a boolean that is true when the query cannot use the order of documents in the index for returning sorted results: MongoDB must sort the documents after it receives the documents from a cursor.
		///
		/// If scanAndOrder is false, MongoDB can use the order of the documents in an index to return sorted results.
		/// </summary>
		[BsonElement("scanAndOrder")]
		public bool ScanAndOrder { get; set; }

		/// <summary>
		/// indexOnly is a boolean value that returns true when the query is covered by the index indicated in the cursor field. When an index covers a query, MongoDB can both match the query conditions and return the results using only the index because:
		/// * all the fields in the query are part of that index, and
		/// * all the fields returned in the results set are in the same index.
		/// </summary>
		[BsonElement("indexOnly")]
		public bool IndexOnly { get; set; }

		/// <summary>
		/// nYields is a number that reflects the number of times this query yielded the read lock to allow waiting writes execute.
		/// </summary>
		[BsonElement("nYields")]
		public int YieldCount { get; set; }

		/// <summary>
		/// nChunkSkips is a number that reflects the number of documents skipped because of active chunk migrations in a sharded system. Typically this will be zero. A number greater than zero is ok, but indicates a little bit of inefficiency.
		/// </summary>
		[BsonElement("nChunkSkips")]
		public int ChunkSkipCount { get; set; }

		/// <summary>
		/// millis is a number that reflects the time in milliseconds to complete the query.
		/// </summary>
		[BsonElement("millis")]
		public int Milliseconds { get; set; }

		/// <summary>
		/// indexBounds is a document that contains the lower and upper index key bounds.
		/// </summary>
		[BsonElement("indexBounds")]
		public Dictionary<string, BsonValue> IndexBounds { get; set; }

		/// <summary>
		/// server is a string that reports the MongoDB server.
		/// </summary>
		[BsonElement("server")]
		public string Server { get; set; }

		/// <summary>
		/// allPlans is an array that holds the list of plans the query optimizer runs in order to select the index for the query. Displays only when the verbose parameter to explain() is true or 1.
		/// </summary>
		[BsonElement("allPlans")]
		public QueryPlan[] AllPlans { get; set; }

		/// <summary>
		/// oldPlan is a document value that contains the previous plan selected by the query optimizer for the query. Displays only when the verbose parameter to explain() is true or 1.
		/// </summary>
		[BsonElement("oldPlan")]
		public QueryPlan OldPlan { get; set; }

		/// <summary>
		/// clusteredType is a string that reports the access pattern for shards.
		/// </summary>
		[BsonElement("clusteredType"), BsonRepresentation(BsonType.String)]
		public ClusterType? ClusteredType { get; set; }

		/// <summary>
		/// millisShardTotal is a number that reports the total time in milliseconds for the query to run on the shards.
		/// </summary>
		[BsonElement("millisShardTotal")]
		public int MillisShardTotal { get; set; }

		/// <summary>
		/// millisShardAvg is a number that reports the average time in millisecond for the query to run on each shard.
		/// </summary>
		[BsonElement("millisShardAvg")]
		public int MillisShardAvg { get; set; }

		/// <summary>
		/// numQueries is a number that reports the total number of queries executed.
		/// </summary>
		[BsonElement("numQueries")]
		public int NumQueries { get; set; }

		/// <summary>
		/// numShards is a number that reports the total number of shards queried.
		/// </summary>
		[BsonElement("numShards")]
		public int NumShards { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[BsonElement("filterSet")]
		public bool FilterSet { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[BsonElement("stats")]
		public QueryPlanStats Stats { get; set; }
	}
}