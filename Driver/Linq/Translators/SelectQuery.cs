/* Copyright 2010-2012 10gen Inc.
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;

using MongoDB.Bson;
using MongoDB.Driver.Builders;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// Represents a LINQ query that has been translated to an equivalent MongoDB Find query.
    /// </summary>
    public class SelectQuery : TranslatedQuery
    {
        // private fields
        private LambdaExpression _where;
        private List<OrderByClause> _orderBy;
        private LambdaExpression _projection;
        private Expression _skip;
        private Expression _take;

        // constructors
        /// <summary>
        /// Initializes a new instance of the MongoLinqFindQuery class.
        /// </summary>
        /// <param name="collection">The collection being queried.</param>
        /// <param name="documentType">The document type.</param>
        public SelectQuery(MongoCollection collection, Type documentType)
            : base(collection, documentType)
        {
        }

        // public properties
        /// <summary>
        /// Gets a list of Expressions that defines the sort order (or null if not specified).
        /// </summary>
        public ReadOnlyCollection<OrderByClause> OrderBy
        {
            get { return (_orderBy == null) ? null :_orderBy.AsReadOnly(); }
        }

        /// <summary>
        /// Gets the Expression that defines the projection (or null if not specified).
        /// </summary>
        public LambdaExpression Projection
        {
            get { return _projection; }
        }

        /// <summary>
        /// Gets the Expression that defines how many documents to skip (or null if not specified).
        /// </summary>
        public Expression Skip
        {
            get { return _skip; }
        }

        /// <summary>
        /// Gets the Expression that defines how many documents to take (or null if not specified);
        /// </summary>
        public Expression Take
        {
            get { return _take; }
        }

        /// <summary>
        /// Gets the LambdaExpression that defines the where clause (or null if not specified).
        /// </summary>
        public LambdaExpression Where
        {
            get { return _where; }
        }

        // public methods
        /// <summary>
        /// Creates an IMongoQuery from the where clause (returns null if no where clause was specified).
        /// </summary>
        /// <returns></returns>
        public IMongoQuery CreateMongoQuery()
        {
            if (_where == null)
            {
                return null;
            }

            // TODO: check lambda for proper type

            var body = _where.Body;
            return CreateMongoQuery(body);
        }

        /// <summary>
        /// Executes the translated Find query.
        /// </summary>
        /// <returns>The result of executing the translated Find query.</returns>
        public override object Execute()
        {
            var query = CreateMongoQuery();
            var cursor = _collection.FindAs(_documentType, query);

            if (_orderBy != null)
            {
                var sortBy = new SortByDocument();
                foreach (var clause in _orderBy)
                {
                    var memberExpression = (MemberExpression)clause.Key.Body;
                    var keyName = memberExpression.Member.Name;
                    var direction = (clause.Direction == OrderByDirection.Descending) ? -1 : 1;
                    sortBy.Add(keyName, direction);
                }
                cursor.SetSortOrder(sortBy);
            }

            if (_skip != null)
            {
                cursor.SetSkip(ToInt32(_skip));
            }

            if (_take != null)
            {
                cursor.SetLimit(ToInt32(_take));
            }
            return cursor;
        }

        /// <summary>
        /// Translates a LINQ query expression tree.
        /// </summary>
        /// <param name="expression">The LINQ query expression tree.</param>
        public void Translate(Expression expression)
        {
            var methodCallExpression = expression as MethodCallExpression;
            if (methodCallExpression == null)
            {
                throw new ArgumentOutOfRangeException("expression");
            }

            var arguments = methodCallExpression.Arguments;
            if (methodCallExpression.Arguments.Count != 2)
            {
                throw new ArgumentOutOfRangeException("expression");
            }
            var source = arguments[0];
            var argument = arguments[1];

            if (source is MethodCallExpression)
            {
                Translate(source);
            }
            
            var methodName = methodCallExpression.Method.Name;
            switch (methodName)
            {
                case "OrderBy":
                case "OrderByDescending":
                    TranslateOrderBy(methodCallExpression);
                    return;
                case "Select":
                    TranslateSelect(argument);
                    return;
                case "Skip":
                    TranslateSkip(argument);
                    return;
                case "Take":
                    TranslateTake(argument);
                    return;
                case "Where":
                    TranslateWhere(argument);
                    return;
            }

            var message = string.Format("LINQ to Mongo does not support method: {0}", methodName);
            throw new InvalidOperationException(message);
        }

        // private methods
        private IMongoQuery CreateMongoQuery(Expression expression)
        {
            BinaryExpression binaryExpression;
            switch (expression.NodeType)
            {
                case ExpressionType.Equal:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.NotEqual:
                    binaryExpression = (BinaryExpression)expression;
                    var elementName = ((MemberExpression)binaryExpression.Left).Member.Name;
                    var value = BsonValue.Create(((ConstantExpression)binaryExpression.Right).Value);
                    switch (expression.NodeType)
                    {
                        case ExpressionType.Equal: return Query.EQ(elementName, value);
                        case ExpressionType.GreaterThan: return Query.GT(elementName, value);
                        case ExpressionType.GreaterThanOrEqual: return Query.GTE(elementName, value);
                        case ExpressionType.LessThan: return Query.EQ(elementName, value);
                        case ExpressionType.LessThanOrEqual: return Query.EQ(elementName, value);
                        case ExpressionType.NotEqual: return Query.EQ(elementName, value);
                    }
                    throw new MongoInternalException("Should not havereached here.");
                case ExpressionType.AndAlso:
                    binaryExpression = (BinaryExpression)expression;
                    return Query.And(CreateMongoQuery(binaryExpression.Left), CreateMongoQuery(binaryExpression.Right));
                case ExpressionType.OrElse:
                    binaryExpression = (BinaryExpression)expression;
                    return Query.Or(CreateMongoQuery(binaryExpression.Left), CreateMongoQuery(binaryExpression.Right));
                default:
                    throw new ArgumentException("Unsupported where clause");
            }
        }

        private Expression StripQuote(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Quote)
            {
                return ((UnaryExpression)expression).Operand;
            }
            return expression;
        }

        private int ToInt32(Expression expression)
        {
            if (expression.Type != typeof(int))
            {
                throw new ArgumentOutOfRangeException("expression", "Expected an Expression of Type Int32.");
            }

            var constantExpression = expression as ConstantExpression;
            if (constantExpression == null)
            {
                throw new ArgumentOutOfRangeException("expression", "Expected a ConstantExpression.");
            }

            return (int) constantExpression.Value;
        }

        private void TranslateOrderBy(MethodCallExpression expression)
        {
            if (_orderBy != null)
            {
                throw new InvalidOperationException("Only one OrderBy or OrderByDescending clause is allowed (use ThenBy or ThenByDescending for multiple order by clauses).");
            }

            var key = (LambdaExpression)StripQuote(expression.Arguments[1]);
            var direction = (expression.Method.Name == "OrderByDescending") ? OrderByDirection.Descending : OrderByDirection.Ascending;
            var clause = new OrderByClause(key, direction);

            _orderBy = new List<OrderByClause>();
            _orderBy.Add(clause);
        }

        private void TranslateSelect(Expression expression)
        {
            var lambdaExpression = (LambdaExpression)StripQuote(expression);
            if (lambdaExpression.Parameters.Count != 1)
            {
                throw new ArgumentOutOfRangeException("expression");
            }
            // ignore trivial projections of the form: d => d
            if (lambdaExpression.Body == lambdaExpression.Parameters[0])
            {
                return;
            }
            _projection = lambdaExpression;
        }

        private void TranslateSkip(Expression expression)
        {
            _skip = StripQuote(expression);
        }

        private void TranslateTake(Expression expression)
        {
            _take = StripQuote(expression);
        }

        private void TranslateWhere(Expression expression)
        {
            _where = (LambdaExpression)StripQuote(expression);
        }
    }
}
