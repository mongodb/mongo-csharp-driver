/* Copyright 2010-2011 10gen Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Builders;

namespace MongoDB.Driver
{

    /// <summary>
    /// Executes a query for a MongoCollection and converts the method from linq to sql.
    /// </summary>
    public class MongoQueryProvider : IQueryProvider
    {

        /// <summary>
        /// Contains the private instance of the mongo collection
        /// </summary>
        private MongoCollection collection;


        /// <summary>
        /// Initializes a new instance of the <see cref="MongoQueryProvider"/> class.
        /// </summary>
        /// <param name="collection">The collection.</param>
        public MongoQueryProvider(MongoCollection collection)
        {
            this.collection = collection;
        }

        /// <summary>
        /// Creates the query.
        /// </summary>
        /// <typeparam name="TElement">The type of the element.</typeparam>
        /// <param name="expression">The expression to create the query for.</param>
        /// <returns></returns>
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new MongoCollection<TElement>(collection.Database, new MongoCollectionSettings<TElement>(collection.Database, collection.Settings.CollectionName), this, expression);
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
            Type elementType = TypeSystem.GetElementType(expression.Type);
            try
            {
                return (IQueryable)Activator.CreateInstance(typeof(MongoCollection<>).MakeGenericType(elementType), new object[] { this, expression });
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException;
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
            return (TResult)Execute(expression);

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
            var parser = new QueryParser(collection);
            parser.Parse(expression);




            //Get the cursor 
            var cursor = parser.Where != null ? collection.FindAs(collection.ElementType, parser.Where) : collection.FindAllAs(collection.ElementType);

            //If sort is defined, set the sort criteria
            if (parser.Sort != null)
                cursor.SetSortOrder(parser.Sort);

            //Set the limit
            if (parser.Limit.HasValue)
                cursor.SetLimit(parser.Limit.Value);

            //Set the skip
            if (parser.Skip.HasValue)
                cursor.SetSkip(parser.Skip.Value);





            //If first or FirstOrDefault
            if (parser.First || parser.Last)
            {
                return GetFirstOrLast(parser, cursor);
            }





            //If looking for just a count of items, return count;
            if (parser.Count)
                return (int)cursor.Size();
            //If looking for a long count, return that.
            else if (parser.LongCount)
                return cursor.Size();

            //do the select
            else if (parser.Select != null)
                return GetSelect(parser, cursor);
            //All else just return the cursor itself
            else
                return cursor;


        }

        /// <summary>
        /// Parses the select statement and 
        /// </summary>
        /// <param name="parser">The parser.</param>
        /// <param name="cursor">The cursor.</param>
        /// <returns></returns>
        private object GetSelect(QueryParser parser, MongoCursor cursor)
        {
            var itemType = parser.Select.Method.ReturnType;
            var method = this.GetType().GetMethod("IterateSelect", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(itemType);
            var startReturn = method.Invoke(null, new object[] { cursor, parser.Select });
            while (parser.SelectRest.Count > 0)
            {
                var currMethod = parser.SelectRest.Dequeue();

                var param = new List<object> { startReturn };
                if (currMethod.Arguments.Count > 1)
                {
                    for (int i = 1; i < currMethod.Arguments.Count; i++)
                    {
                        var arg = currMethod.Arguments[i];
                        param.Add(GetParameter(arg));
                    }
                }


                startReturn = currMethod.Method.Invoke(startReturn, param.ToArray());
            }

            return startReturn;
        }

        /// <summary>
        /// Gets the first or last item from the cursor.
        /// </summary>
        /// <param name="parser">The parser.</param>
        /// <param name="cursor">The cursor.</param>
        /// <returns></returns>
        private static object GetFirstOrLast(QueryParser parser, MongoCursor cursor)
        {
            cursor.Limit = 1;

            //if getting last, then add sort order to order by id inverse
            if (parser.Last)
            {
                var sort = parser.Sort ?? new SortByBuilder();
                if (sort.ElementExists("$natural"))
                {
                    throw new InvalidOperationException("Either sorting with $natural or calling reverse while using Last(OrDefault) does not work. Do you want to use First(OrDefault)");
                }
                cursor.SetSortOrder(sort.Descending("$natural"));
            }


            var iterator = ((IEnumerable)cursor).GetEnumerator();
            if (iterator.MoveNext())
            {
                return iterator.Current;
            }
            else if (parser.ThrowIfNull)
            {
                throw new ArgumentNullException();
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// Gets the parameter from the expression.
        /// </summary>
        /// <param name="expression">The expression to get the parameter from.</param>
        /// <returns></returns>
        private static object GetParameter(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Constant:
                    return (expression as ConstantExpression).Value;
                    break;
                case ExpressionType.Quote:
                    return (expression as UnaryExpression).Operand;
                    break;
            }
            return null;
        }

        /// <summary>
        /// Iterates the select.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cursor">The cursor.</param>
        /// <param name="select">The select.</param>
        /// <returns></returns>
        private static IQueryable<T> IterateSelect<T>(MongoCursor cursor, Delegate select)
        {
            return GetEnumerator<T>(cursor, select).AsQueryable();
        }

        /// <summary>
        /// Gets the enumerator to iterate through the cursor
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cursor">The cursor pointing to the list of Mongo data.</param>
        /// <param name="select">The select delegate which converts from one type to another.</param>
        /// <returns></returns>
        private static IEnumerable<T> GetEnumerator<T>(MongoCursor cursor, Delegate select)
        {
            foreach (var item in cursor)
            {
                yield return (T)select.DynamicInvoke(item);
            }
        }
    }

    /// <summary>
    /// Parses the query and returns a query and flags to be converted to a mongo query.
    /// </summary>
    internal class QueryParser
    {
        /// <summary>
        /// Contains a reference to the mongo collection.
        /// </summary>
        private MongoCollection collection;



        /// <summary>
        /// Initializes a new instance of the <see cref="QueryParser"/> class.
        /// </summary>
        /// <param name="collection">The mongo collection.</param>
        public QueryParser(MongoCollection collection)
        {
            this.collection = collection;
        }


        /// <summary>
        /// Gets or sets the where query.
        /// </summary>
        /// <value>
        /// The where query.
        /// </value>
        public QueryComplete Where
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the sort query.
        /// </summary>
        /// <value>
        /// The sort query.
        /// </value>
        public SortByBuilder Sort
        {
            get;
            set;
        }


        /// <summary>
        /// Gets or sets the number of items to skip. If skip is null (or nothing), then no skip is defined.
        /// </summary>
        /// <value>
        /// The number of items skip.
        /// </value>
        public int? Skip
        {
            get;
            set;
        }


        /// <summary>
        /// Gets or sets the the number of items to limit. If the limit is null (or nothing), then no limit is defined.
        /// </summary>
        /// <value>
        /// The limit.
        /// </value>
        public int? Limit
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="QueryParser"/> ends with a count.
        /// </summary>
        /// <value>
        ///   <c>true</c> if count; otherwise, <c>false</c>.
        /// </value>
        public bool Count
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets a value indicating whether this  <see cref="QueryParser"/> ends with a long count query.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [long count]; otherwise, <c>false</c>.
        /// </value>
        public bool LongCount
        {
            get;
            set;
        }



        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="QueryParser"/> is ending with simply getting a first element.
        /// </summary>
        /// <value>
        ///   <c>true</c> if first; otherwise, <c>false</c>.
        /// </value>
        public bool First
        {
            get;
            set;
        }



        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="QueryParser"/> is ending with simply getting the last element.
        /// </summary>
        /// <value>
        ///   <c>true</c> if last; otherwise, <c>false</c>.
        /// </value>
        public bool Last
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether if to throw an exception when the first or last elements are null. If the "First" method is called, and no item
        /// is returned, then an InvalidOperationException should be thrown.
        /// </summary>
        /// <value>
        ///   <c>true</c> if throw when null otherwise, <c>false</c>.
        /// </value>
        public bool ThrowIfNull
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the select method which converts from the collection type to another type.
        /// </summary>
        /// <value>
        /// The select method.
        /// </value>
        public Delegate Select
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the select rest of the method chain after the select to be reconstructed later.
        /// </summary>
        /// <value>
        /// The rest of the select method.
        /// </value>
        public Queue<MethodCallExpression> SelectRest
        {
            get;
            set;
        }


        /// <summary>
        /// Parses the specified expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        public void Parse(Expression expression)
        {
            var list = new List<IMongoQuery>();

            //find select
            Queue<MethodCallExpression> tree = new Queue<MethodCallExpression>();
            var select = FindSelect(expression, tree);
            if (select != null)
            {
                Parse(select, list);
                SelectRest = tree;
            }
            else
            {
                Parse(expression, list);
            }


            Where = Query.And(list.ToArray());
        }

        /// <summary>
        /// Finds the select method in the chain or null.
        /// </summary>
        /// <param name="expression">The expression chain to look.</param>
        /// <param name="tree">The tree of calls which got to the Select method.</param>
        /// <returns>The select method.</returns>
        private MethodCallExpression FindSelect(Expression expression, Queue<MethodCallExpression> tree)
        {

            switch (expression.NodeType)
            {
                case ExpressionType.Call:
                    var method = expression as MethodCallExpression;
                    if (method.Method.Name == "Select")
                    {

                        return method;

                    }
                    else
                    {
                        foreach (var arg in method.Arguments)
                        {
                            var sel = FindSelect(arg, tree);
                            if (sel != null)
                            {
                                tree.Enqueue(method);
                                return sel;
                            }

                        }
                    }
                    break;
            }

            return null;
        }

        /// <summary>
        /// Parses the specified expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="list">The list or Where clause which receives the query parameters.</param>
        private void Parse(Expression expression, List<IMongoQuery> list)
        {

            switch (expression.NodeType)
            {
                case ExpressionType.Call:
                    Parse(expression as System.Linq.Expressions.MethodCallExpression, list);
                    break;
                case ExpressionType.Lambda:
                    Parse((expression as System.Linq.Expressions.LambdaExpression).Body, list);
                    break;
                case ExpressionType.Quote:
                    Parse((expression as System.Linq.Expressions.UnaryExpression).Operand, list);
                    break;



                case ExpressionType.Equal:
                    ParseBinary(expression as System.Linq.Expressions.BinaryExpression, list, Query.EQ);
                    break;
                case ExpressionType.NotEqual:
                    ParseBinary(expression as System.Linq.Expressions.BinaryExpression, list, Query.NE);
                    break;
                case ExpressionType.GreaterThan:
                    ParseBinary(expression as System.Linq.Expressions.BinaryExpression, list, Query.GT);
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    ParseBinary(expression as System.Linq.Expressions.BinaryExpression, list, Query.GTE);
                    break;
                case ExpressionType.LessThan:
                    ParseBinary(expression as System.Linq.Expressions.BinaryExpression, list, Query.LT);
                    break;
                case ExpressionType.LessThanOrEqual:
                    ParseBinary(expression as System.Linq.Expressions.BinaryExpression, list, Query.LTE);
                    break;



                case ExpressionType.And:
                    ParseAnd(expression as System.Linq.Expressions.BinaryExpression, list);
                    break;
                case ExpressionType.AndAlso:
                    ParseAnd(expression as System.Linq.Expressions.BinaryExpression, list);
                    break;
                case ExpressionType.Or:
                    ParseOr(expression as System.Linq.Expressions.BinaryExpression, list);
                    break;
                case ExpressionType.OrElse:
                    ParseOr(expression as System.Linq.Expressions.BinaryExpression, list);
                    break;
                case ExpressionType.MemberAccess:
                    Parse(expression as System.Linq.Expressions.MemberExpression, list);
                    break;
            }
        }




        /// <summary>
        /// Parses the method expression which specifies how to handle each expression.
        /// </summary>
        /// <param name="exp">The expression to parse.</param>
        /// <param name="query">The list of queries for the where clause.</param>
        private void Parse(MethodCallExpression exp, List<IMongoQuery> query)
        {
            switch (exp.Method.Name)
            {
                case "Take":
                    foreach (var arg in exp.Arguments)
                    {
                        if (arg.NodeType == ExpressionType.Constant && arg.Type == typeof(int))
                        {
                            this.Limit = (int)(arg as ConstantExpression).Value;
                        }
                        else
                            Parse(arg, query);
                    }
                    return;
                case "Skip":

                    foreach (var arg in exp.Arguments)
                    {
                        if (arg.NodeType == ExpressionType.Constant && arg.Type == typeof(int))
                        {
                            this.Skip = (int)(arg as ConstantExpression).Value;
                        }
                        else
                            Parse(arg, query);
                    }
                    return;
                case "Select":
                    foreach (var arg in exp.Arguments)
                    {
                        if (arg.NodeType == ExpressionType.Quote)
                        {
                            var node = (arg as System.Linq.Expressions.UnaryExpression);

                            var lam = node.Operand as LambdaExpression;
                            var del = lam.Compile();
                            this.Select = del;
                        }
                        else
                            Parse(arg, query);
                    }
                    return;

                case "OrderBy":
                case "OrderByDescending":
                    ParseSort(exp, null);
                    return;
                case "Reverse":
                    if (this.Sort == null)
                        this.Sort = new SortByBuilder();
                    this.Sort.Descending("$natural");
                    return;
                case "First":
                    this.ThrowIfNull = true;
                    this.First = true;
                    break;
                case "FirstOrDefault":
                    this.First = true;
                    break;
                case "Last":
                    this.ThrowIfNull = true;
                    this.Last = true;
                    break;
                case "LastOrDefault":
                    this.Last = true;
                    break;
                case "Count":
                    this.Count = true;
                    break;
                case "LongCount":
                    this.LongCount = true;
                    break;

                case "Where":

                    //fall through and parse at the end
                    break;
                default:
                    throw new NotSupportedException("Method " + exp.Method.Name + " is not supported.");

            }
            foreach (var arg in exp.Arguments)
            {
                Parse(arg, query);
            }






        }


        /// <summary>
        /// Parses the an "And" between two items in a query.
        /// </summary>
        /// <param name="exp">The binary expression.</param>
        /// <param name="query">The list of query parameters.</param>
        private void ParseAnd(BinaryExpression exp, List<IMongoQuery> query)
        {
            var andQuery = new List<IMongoQuery>();
            Parse(exp.Left, andQuery);
            Parse(exp.Right, andQuery);

            query.Add(Query.And(andQuery.ToArray()));
        }
        /// <summary>
        /// Parses the an "Or" between two items in a query.
        /// </summary>
        /// <param name="exp">The binary expression.</param>
        /// <param name="query">The list of query parameters.</param>
        private void ParseOr(BinaryExpression exp, List<IMongoQuery> query)
        {
            var orQuery = new List<IMongoQuery>();
            Parse(exp.Left, orQuery);
            Parse(exp.Right, orQuery);

            query.Add(Query.Or(orQuery.ToArray()));
        }

        /// <summary>
        /// Parses the name of an element or the id if the element parameter is the id.
        /// </summary>
        /// <param name="exp">The expresssion to parse.</param>
        /// <returns>The name of the item</returns>
        private string ParseName(MemberExpression exp)
        {
            return string.Join(".", GetProperties(exp).Select(k =>
            {
                var attr = k.GetCustomAttributes(typeof(BsonIdAttribute), true);
                if (attr.Length > 0 || k.PropertyType == typeof(ObjectId))
                {
                    return "_id";
                }
                else
                {
                    return k.Name;
                }

            }).ToArray());
        }


        /// <summary>
        /// Parses the binary expression for queries such as "Equal", "NotEqual" etc.
        /// </summary>
        /// <param name="exp">The expresion to parse.</param>
        /// <param name="query">The list querys.</param>
        /// <param name="addQuery">The method to link the parameters together in this chain of queries.</param>
        private void ParseBinary(BinaryExpression exp, List<IMongoQuery> query, Func<string, BsonValue, IMongoQuery> addQuery)
        {

            object value = GetValue(exp.Right);

            query.Add(addQuery(ParseName(exp.Left as MemberExpression), BsonValue.Create(value)));


        }

        /// <summary>
        /// Gets the value of the expression.
        /// </summary>
        /// <param name="expression">The expression to parse.</param>
        /// <returns>The constant value it represents or the value its lambda method executes to.</returns>
        private object GetValue(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Constant)
            {
                return (expression as ConstantExpression).Value;
            }
            LambdaExpression lambda = Expression.Lambda(expression);
            Delegate fn = lambda.Compile();
            return fn.DynamicInvoke(null);
        }






        /// <summary>
        /// Parses the sort expression using a method which decides what to do when sorting.
        /// </summary>
        /// <param name="expression">The expression to sort.</param>
        /// <param name="foundMember">The method which decides what type of sort to do on this expression.</param>
        private void ParseSort(Expression expression, Action<SortByBuilder, MemberExpression> foundMember)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Call:
                    var exp = expression as MethodCallExpression;
                    Action<SortByBuilder, MemberExpression> found = null;
                    if (exp.Method.Name == "OrderBy")
                    {
                        found = (query, mem) => query.Ascending(ParseName(mem));
                    }
                    else if (exp.Method.Name == "OrderByDescending")
                    {
                        found = (query, mem) => query.Descending(ParseName(mem));
                    }



                    foreach (var arg in exp.Arguments)
                    {
                        ParseSort(arg, found);
                    }


                    break;
                case ExpressionType.Lambda:
                    var lambda = expression as System.Linq.Expressions.LambdaExpression;
                    ParseSort(lambda.Body, foundMember);
                    break;
                case ExpressionType.Quote:
                    ParseSort((expression as System.Linq.Expressions.UnaryExpression).Operand, foundMember);
                    break;
                case ExpressionType.MemberAccess:
                    var member = expression as System.Linq.Expressions.MemberExpression;
                    if (this.Sort == null)
                        this.Sort = new SortByBuilder();
                    foundMember(this.Sort, member);
                    break;
            }
        }





        /// <summary>
        /// Gets the properties of a function expression.
        /// </summary>
        /// <typeparam name="T">The type of properties iterate through.</typeparam>
        /// <param name="propertyExpression">The expression which to get the properties from.</param>
        /// <returns>A IEnumerable collection of property objects.</returns>
        private static IEnumerable<PropertyInfo> GetProperties<T>(Expression<Func<T, object>> propertyExpression)
        {
            return GetProperties(propertyExpression.Body);
        }

        /// <summary>
        /// Gets the properties of the expression.
        /// </summary>
        /// <param name="expression">The expression to get the properties of.</param>
        /// <returns>A IEnumerable collection of property objects.</returns>
        private static IEnumerable<PropertyInfo> GetProperties(Expression expression)
        {
            var memberExpression = expression as MemberExpression;
            if (memberExpression == null) yield break;

            var property = memberExpression.Member as PropertyInfo;
            if (property == null)
            {
                throw new Exception("Donno");
            }
            foreach (var propertyInfo in GetProperties(memberExpression.Expression))
            {
                yield return propertyInfo;
            }
            yield return property;
        }


    }


    internal static class TypeSystem
    {
        internal static Type GetElementType(Type seqType)
        {
            Type ienum = FindIEnumerable(seqType);
            if (ienum == null) return seqType;
            return ienum.GetGenericArguments()[0];
        }
        private static Type FindIEnumerable(Type seqType)
        {
            if (seqType == null || seqType == typeof(string))
                return null;
            if (seqType.IsArray)
                return typeof(IEnumerable<>).MakeGenericType(seqType.GetElementType());
            if (seqType.IsGenericType)
            {
                foreach (Type arg in seqType.GetGenericArguments())
                {
                    Type ienum = typeof(IEnumerable<>).MakeGenericType(arg);
                    if (ienum.IsAssignableFrom(seqType))
                    {
                        return ienum;
                    }
                }
            }
            Type[] ifaces = seqType.GetInterfaces();
            if (ifaces != null && ifaces.Length > 0)
            {
                foreach (Type iface in ifaces)
                {
                    Type ienum = FindIEnumerable(iface);
                    if (ienum != null) return ienum;
                }
            }
            if (seqType.BaseType != null && seqType.BaseType != typeof(object))
            {
                return FindIEnumerable(seqType.BaseType);
            }
            return null;
        }
    }
}
