using System;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace MongoDB.Linq
{
    internal class MongoQueryObject
    {
        /// <summary>
        /// Gets or sets the aggregator.
        /// </summary>
        /// <value>The aggregator.</value>
        public LambdaExpression Aggregator { get; set; }

        /// <summary>
        /// Gets or sets the name of the collection.
        /// </summary>
        /// <value>The name of the collection.</value>
        public string CollectionName { get; set; }

        /// <summary>
        /// Gets or sets the database.
        /// </summary>
        /// <value>The database.</value>
        public MongoDatabase Database { get; set; }

        /// <summary>
        /// Gets or sets the type of the document.
        /// </summary>
        /// <value>The type of the document.</value>
        public Type DocumentType { get; set; }

        /// <summary>
        /// Gets or sets the fields.
        /// </summary>
        /// <value>The fields.</value>
        public BsonDocument Fields { get; set; }

        /// <summary>
        /// Gets or sets the finalizer function.
        /// </summary>
        /// <value>The finalizer function.</value>
        public string FinalizerFunction { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is a count query.
        /// </summary>
        /// <value><c>true</c> if this is a count query; otherwise, <c>false</c>.</value>
        public bool IsCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is map reduce.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is map reduce; otherwise, <c>false</c>.
        /// </value>
        public bool IsMapReduce { get; set; }

        /// <summary>
        /// Gets or sets the map function.
        /// </summary>
        /// <value>The map function.</value>
        public string MapFunction { get; set; }

        /// <summary>
        /// Gets or sets the reduce function.
        /// </summary>
        /// <value>The reduce function.</value>
        public string ReduceFunction { get; set; }

        /// <summary>
        /// Gets or sets the number to skip.
        /// </summary>
        /// <value>The number to skip.</value>
        public int NumberToSkip { get; set; }

        /// <summary>
        /// Gets or sets the number to limit.
        /// </summary>
        /// <value>The number to limit.</value>
        public int NumberToLimit { get; set; }

        /// <summary>
        /// Gets or sets the projector.
        /// </summary>
        /// <value>The projector.</value>
        public LambdaExpression Projector { get; set; }

        /// <summary>
        /// Gets or sets the query.
        /// </summary>
        /// <value>The query.</value>
        public BsonDocument Query { get; private set; }

        /// <summary>
        /// Gets the sort.
        /// </summary>
        /// <value>The sort.</value>
        public BsonDocument Sort { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoQueryObject"/> class.
        /// </summary>
        public MongoQueryObject()
        {
            Fields = new BsonDocument();
            Query = new BsonDocument();
        }

        /// <summary>
        /// Adds the sort.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public void AddSort(string name, int value)
        {
            if(Sort == null)
                Sort = new BsonDocument();
            Sort.Add(name, value);
        }

        /// <summary>
        /// Sets the query document.
        /// </summary>
        /// <param name="document">The document.</param>
        public void SetQueryDocument(BsonDocument document)
        {
            Query = document;
        }

        /// <summary>
        /// Sets the where clause.
        /// </summary>
        /// <param name="whereClause">The where clause.</param>
        public void SetWhereClause(string whereClause)
        {
            Query = MongoDB.Driver.Builders.Query.Where(new BsonJavaScript(whereClause)).ToBsonDocument();
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return "queryobject";
        }
    }
}