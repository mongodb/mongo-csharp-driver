using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.DefaultSerializer;
using MongoDB.Driver;
using MongoDB.Linq.Expressions;
using MongoDB.Linq.Translators;
using MongoDB.Linq.Util;

namespace MongoDB.Linq
{
    public class MapReduceCommand
    {
        [BsonElement("map")]
        public BsonJavaScript Map { get; set; }
        [BsonElement("limit")]
        public int? Limit { get; set; }
        [BsonElement("sort")]
        public BsonDocument Sort { get; set; }
        [BsonElement("reduce")]
        public BsonJavaScript Reduce { get; set; }
        [BsonElement("finalize")]
        public BsonJavaScript Finalize { get; set; }
        [BsonElement("query")]
        public BsonDocument Query { get; set; }
    }
    /// <summary>
    /// 
    /// </summary>
    public static class LinqExtensions
    {
        /// <summary>
        /// Counts the specified collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="selector">The selector.</param>
        /// <returns></returns>
        public static int Count<T>(this MongoCollection<T> collection, Expression<Func<T, bool>> selector) where T : class
        {
            return collection.Linq().Count(selector);
        }

        /// <summary>
        /// Deletes the documents according to the selector.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="selector">The selector.</param>
        [Obsolete("Use Remove instead")]
        public static void Delete<T>(this MongoCollection<T> collection, Expression<Func<T, bool>> selector) where T : class
        {
            collection.Remove(GetQuery(collection, selector));
        }

        /// <summary>
        /// Removes the specified collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="selector">The selector.</param>
        public static void Remove<T>(this MongoCollection<T> collection, Expression<Func<T, bool>> selector) where T : class
        {
            collection.Remove(GetQuery(collection, selector));
        }

        /// <summary>
        /// Finds the selectorified collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="selector">The selector.</param>
        /// <returns></returns>
        public static MongoCursor<BsonDocument,T> Find<T>(this MongoCollection<T> collection, Expression<Func<T, bool>> selector) where T : class
        {
            return collection.Find(GetQuery(collection, selector));
        }

        /// <summary>
        /// Finds the one.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="selector">The selector.</param>
        /// <returns></returns>
        public static T FindOne<T>(this MongoCollection<T> collection, Expression<Func<T, bool>> selector) where T : class
        {
            return collection.FindOne(GetQuery(collection, selector));
        }

        /// <summary>
        /// Linqs the selectorified collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        /// <returns></returns>
        public static IQueryable<T> Linq<T>(this MongoCollection<T> collection) where T : class
        {
            return new MongoQuery<T>(new MongoQueryProvider(collection.Database, collection.Name));
        }

        /// <summary>
        /// Linqs the selectorified collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <returns></returns>
        public static IQueryable<BsonDocument> Linq(this MongoCollection collection)
        {
            return new MongoQuery<BsonDocument>(new MongoQueryProvider(collection.Database, collection.Name));
        }

        /// <summary>
        /// Updates the selectorified collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="document">The document.</param>
        /// <param name="selector">The selector.</param>
        public static void Update<T>(this MongoCollection<T> collection, object document, Expression<Func<T, bool>> selector) where T : class
        {
            collection.Update(document, GetQuery(collection, selector));
        }

        /// <summary>
        /// Updates the selectorified collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="document">The document.</param>
        /// <param name="selector">The selector.</param>
        /// <param name="safeMode">if set to <c>true</c> [safe mode].</param>
        public static void Update<T>(this MongoCollection<T> collection, object document, Expression<Func<T, bool>> selector, SafeMode safeMode) where T : class
        {
            collection.Update(document, GetQuery(collection, selector), safeMode);
        }

        /// <summary>
        /// Updates the selectorified collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="document">The document.</param>
        /// <param name="selector">The selector.</param>
        /// <param name="flags">The flags.</param>
        public static void Update<T>(this MongoCollection<T> collection, object document, Expression<Func<T, bool>> selector, UpdateFlags flags) where T : class
        {
            collection.Update(document, GetQuery(collection, selector), flags);
        }

        /// <summary>
        /// Updates the selectorified collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="document">The document.</param>
        /// <param name="selector">The selector.</param>
        /// <param name="flags">The flags.</param>
        /// <param name="safeMode">if set to <c>true</c> [safe mode].</param>
        public static void Update<T>(this MongoCollection<T> collection, object document, Expression<Func<T, bool>> selector, UpdateFlags flags, SafeMode safeMode) where T : class
        {
            collection.Update(document, GetQuery(collection, selector), flags, safeMode);
        }

        /// <summary>
        /// Updates all.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="document">The document.</param>
        /// <param name="selector">The selector.</param>
        public static void UpdateAll<T>(this MongoCollection<T> collection, object document, Expression<Func<T, bool>> selector) where T : class
        {
            collection.Update(document, GetQuery(collection, selector), UpdateFlags.Multi);
        }

        /// <summary>
        /// Updates all.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="document">The document.</param>
        /// <param name="selector">The selector.</param>
        /// <param name="safeMode">if set to <c>true</c> [safe mode].</param>
        public static void UpdateAll<T>(this MongoCollection<T> collection, object document, Expression<Func<T, bool>> selector, SafeMode safeMode) where T : class
        {
            collection.Update(document, GetQuery(collection, selector), UpdateFlags.Multi, safeMode);
        }

        /// <summary>
        /// Gets the query.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="selector">The selector.</param>
        /// <returns></returns>
        private static BsonDocument GetQuery<T>(MongoCollection<T> collection, Expression<Func<T, bool>> selector) where T : class
        {
            var query = new MongoQuery<T>(new MongoQueryProvider(collection.Database, collection.Name))
                .Where(selector);
            var queryObject = ((IMongoQueryable)query).GetQueryObject();
            return queryObject.Query;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    internal class MongoQueryProvider : IQueryProvider
    {
        private readonly string _collectionName;
        private readonly MongoDatabase _database;

        /// <summary>
        /// Gets the name of the collection.
        /// </summary>
        /// <value>The name of the collection.</value>
        public string CollectionName
        {
            get { return _collectionName; }
        }

        /// <summary>
        /// Gets the database.
        /// </summary>
        /// <value>The database.</value>
        public MongoDatabase Database
        {
            get { return _database; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoQueryProvider"/> class.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="collectionName">Name of the collection.</param>
        public MongoQueryProvider(MongoDatabase database, string collectionName)
        {
            if (database == null)
                throw new ArgumentNullException("database");
            if (collectionName == null)
                throw new ArgumentNullException("collectionName");

            _collectionName = collectionName;
            _database = database;
        }

        /// <summary>
        /// Creates the query.
        /// </summary>
        /// <typeparam name="TElement">The type of the element.</typeparam>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new MongoQuery<TElement>(this, expression);
        }

        /// <summary>
        /// Constructs an <see cref="T:System.Linq.IQueryable"/> object that can evaluate the query represented by a specified expression tree.
        /// </summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <returns>
        /// An <see cref="T:System.Linq.IQueryable"/> that can evaluate the query represented by the specified expression tree.
        /// </returns>
        public IQueryable CreateQuery(Expression expression)
        {
            Type elementType = TypeHelper.GetElementType(expression.Type);
            try
            {
                return (IQueryable)Activator.CreateInstance(typeof(MongoQuery<>).MakeGenericType(elementType), new object[] { this, expression });
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        /// <summary>
        /// Executes the specified expression.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        public TResult Execute<TResult>(Expression expression)
        {
            object result = Execute(expression);
            return (TResult)result;
        }

        /// <summary>
        /// Executes the query represented by a specified expression tree.
        /// </summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <returns>
        /// The value that results from executing the specified query.
        /// </returns>
        public object Execute(Expression expression)
        {
            var plan = BuildExecutionPlan(expression);

            var lambda = expression as LambdaExpression;
            if (lambda != null)
            {
                var fn = Expression.Lambda(lambda.Type, plan, lambda.Parameters);
                return fn.Compile();
            }
            else
            {
                var efn = Expression.Lambda<Func<object>>(Expression.Convert(plan, typeof(object)));
                var fn = efn.Compile();
                return fn();
            }
        }

        /// <summary>
        /// Gets the query object.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        internal MongoQueryObject GetQueryObject(Expression expression)
        {
            var projection = Translate(expression);
            return new MongoQueryObjectBuilder().Build(projection);
        }

        /// <summary>
        /// Executes the query object.
        /// </summary>
        /// <param name="queryObject">The query object.</param>
        /// <returns></returns>
        internal object ExecuteQueryObject(MongoQueryObject queryObject){
            if (queryObject.IsCount)
                return ExecuteCount(queryObject);
            if (queryObject.IsMapReduce)
                return ExecuteMapReduce(queryObject);
            return ExecuteFind(queryObject);
        }

        private Expression BuildExecutionPlan(Expression expression)
        {
            var lambda = expression as LambdaExpression;
            if (lambda != null)
                expression = lambda.Body;

            var projection = Translate(expression);

            var rootQueryable = new RootQueryableFinder().Find(expression);
            var provider = Expression.Convert(
                Expression.Property(rootQueryable, typeof(IQueryable).GetProperty("Provider")),
                typeof(MongoQueryProvider));

            return new ExecutionBuilder().Build(projection, provider);
        }

        private Expression Translate(Expression expression)
        {
            var rootQueryable = new RootQueryableFinder().Find(expression);
            var elementType = ((IQueryable)((ConstantExpression)rootQueryable).Value).ElementType;

            expression = PartialEvaluator.Evaluate(expression, CanBeEvaluatedLocally);

            expression = new FieldBinder().Bind(expression, elementType);
            expression = new QueryBinder(this, expression).Bind(expression);
            expression = new AggregateRewriter().Rewrite(expression);
            expression = new RedundantFieldRemover().Remove(expression);
            expression = new RedundantSubqueryRemover().Remove(expression);

            expression = new OrderByRewriter().Rewrite(expression);
            expression = new RedundantFieldRemover().Remove(expression);
            expression = new RedundantSubqueryRemover().Remove(expression);

            return expression;
        }

        /// <summary>
        /// Determines whether this instance [can be evaluated locally] the specified expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>
        /// 	<c>true</c> if this instance [can be evaluated locally] the specified expression; otherwise, <c>false</c>.
        /// </returns>
        private bool CanBeEvaluatedLocally(Expression expression)
        {
            // any operation on a query can't be done locally
            ConstantExpression cex = expression as ConstantExpression;
            if (cex != null)
            {
                IQueryable query = cex.Value as IQueryable;
                if (query != null && query.Provider == this)
                    return false;
            }
            MethodCallExpression mc = expression as MethodCallExpression;
            if (mc != null && (mc.Method.DeclaringType == typeof(Enumerable) || mc.Method.DeclaringType == typeof(Queryable) || mc.Method.DeclaringType == typeof(MongoQueryable)))
            {
                return false;
            }
            if (expression.NodeType == ExpressionType.Convert &&
                expression.Type == typeof(object))
                return true;
            return expression.NodeType != ExpressionType.Parameter &&
                   expression.NodeType != ExpressionType.Lambda;
        }

        /// <summary>
        /// Executes the count.
        /// </summary>
        /// <param name="queryObject">The query object.</param>
        /// <returns></returns>
        private object ExecuteCount(MongoQueryObject queryObject)
        {
            var miGetCollection = typeof(MongoDatabase).GetMethods().Where(m => m.Name == "GetCollection" && m.GetGenericArguments().Length == 1 && m.GetParameters().Length == 1).Single().MakeGenericMethod(queryObject.DocumentType);
            var collection = (MongoCollection)miGetCollection.Invoke(queryObject.Database, new[] { queryObject.CollectionName });

            if (queryObject.Query == null)
                return collection.Count();

            return collection.Count(queryObject.Query);
        }

        internal object ExecuteFindInternal<TDocument>(MongoQueryObject queryObject)
        {
            var collection = Database.GetCollection<TDocument>(queryObject.CollectionName);
            var cursor = queryObject.Query == null ? collection.FindAll() : collection.Find(queryObject.Query);
            if (queryObject.Sort != null)
                cursor = cursor.SetSortOrder(queryObject.Sort);
            if (queryObject.Fields != null)
                cursor = cursor.SetFields(queryObject.Fields);
            if (queryObject.NumberToLimit != 0)
                cursor = cursor.SetLimit(queryObject.NumberToLimit);
            if (queryObject.NumberToSkip != 0)
                cursor = cursor.SetSkip(queryObject.NumberToSkip);
            var executor = GetExecutor(queryObject.DocumentType, queryObject.Projector, queryObject.Aggregator, true);
            return executor.Compile().DynamicInvoke(cursor);
        }

        private object ExecuteFind(MongoQueryObject queryObject)
        {
            var method = this.GetType().GetMethod("ExecuteFindInternal",BindingFlags.NonPublic|BindingFlags.Instance);
            return method.MakeGenericMethod(queryObject.DocumentType).Invoke(this, new [] { queryObject });
        }

        private object ExecuteMapReduce(MongoQueryObject queryObject)
        {
            var miGetCollection = typeof(MongoDatabase).GetMethods().Where(m => m.Name == "GetCollection" && m.GetGenericArguments().Length == 1 && m.GetParameters().Length == 1).Single().MakeGenericMethod(queryObject.DocumentType);
            var collection = miGetCollection.Invoke(queryObject.Database, new[] { queryObject.CollectionName }) as MongoCollection;

            var mapReduceCommand = new MapReduceCommand();
            mapReduceCommand.Map = new BsonJavaScript(queryObject.MapFunction);
            mapReduceCommand.Reduce = new BsonJavaScript(queryObject.ReduceFunction);
            mapReduceCommand.Finalize = new BsonJavaScript(queryObject.FinalizerFunction);
            mapReduceCommand.Query = queryObject.Query;

            if(queryObject.Sort != null)
                mapReduceCommand.Sort = queryObject.Sort;

            mapReduceCommand.Limit = queryObject.NumberToLimit;

            if (queryObject.NumberToSkip != 0)
                throw new MongoQueryException("MapReduce queries do no support Skips.");

            var result = collection.MapReduce(new BsonJavaScript(queryObject.MapFunction),
                                 new BsonJavaScript(queryObject.ReduceFunction), mapReduceCommand);

            var executor = GetExecutor(typeof(BsonDocument), queryObject.Projector, queryObject.Aggregator, true);
            return executor.Compile().DynamicInvoke(result.GetResults<BsonDocument>());
        }

        private static LambdaExpression GetExecutor(Type documentType, LambdaExpression projector,  
            Expression aggregator, bool boxReturn)
        {
            var documents = Expression.Parameter(typeof(IEnumerable<>).MakeGenericType(documentType), "documents");
            Expression body = Expression.Call(
                typeof(MongoQueryProvider),
                "Project",
                new[] { documentType, projector.Body.Type },
                documents,
                projector);
            if (aggregator != null)
                body = Expression.Invoke(aggregator, body);

            if (boxReturn && body.Type != typeof(object))
                body = Expression.Convert(body, typeof(object));

            return Expression.Lambda(body, documents);
        }

        private static IEnumerable<TResult> Project<TDocument, TResult>(IEnumerable<TDocument> documents, Func<TDocument, TResult> projector)
        {
            return documents.Select(projector);
        }

        private class RootQueryableFinder : MongoExpressionVisitor
        {
            private Expression _root;

            public Expression Find(Expression expression)
            {
                Visit(expression);
                return _root;
            }

            protected override Expression Visit(Expression exp)
            {
                Expression result = base.Visit(exp);

                if (this._root == null && result != null && typeof(IQueryable).IsAssignableFrom(result.Type))
                {
                    this._root = result;
                }

                return result;
            }
        }
    }
}