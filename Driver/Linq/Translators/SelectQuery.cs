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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
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
        private Type _ofType;
        private List<OrderByClause> _orderBy;
        private LambdaExpression _projection;
        private Expression _skip;
        private Expression _take;
        private Func<IEnumerable, object> _elementSelector; // used for First, Last, etc...
        private bool _distinct;

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
        /// Gets the final result type if an OfType query operator was used (otherwise null).
        /// </summary>
        public Type OfType
        {
            get { return _ofType; }
        }

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
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery BuildQuery()
        {
            if (_where == null)
            {
                return null;
            }

            // TODO: check lambda for proper type

            var body = _where.Body;
            return BuildQuery(body);
        }

        /// <summary>
        /// Executes the translated Find query.
        /// </summary>
        /// <returns>The result of executing the translated Find query.</returns>
        public override object Execute()
        {
            var query = BuildQuery();

            if (_distinct)
            {
                return ExecuteDistinct(query);
            }

            var cursor = Collection.FindAs(DocumentType, query);

            if (_orderBy != null)
            {
                var sortBy = new SortByDocument();
                foreach (var clause in _orderBy)
                {
                    var keyExpression = clause.Key.Body;
                    var serializationInfo = GetSerializationInfo(keyExpression);
                    if (serializationInfo == null)
                    {
                        var message = string.Format("Invalid OrderBy expression: {0}.", ExpressionFormatter.ToString(keyExpression));
                        throw new NotSupportedException(message);
                    }
                    var direction = (clause.Direction == OrderByDirection.Descending) ? -1 : 1;
                    sortBy.Add(serializationInfo.ElementName, direction);
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

            var projection = _projection;
            if (_ofType != null)
            {
                if (projection == null)
                {
                    var paramExpression = Expression.Parameter(DocumentType, "x");
                    var convertExpression = Expression.Convert(paramExpression, _ofType);
                    projection = Expression.Lambda(convertExpression, paramExpression);
                }
                else
                {
                    // TODO: handle projection after OfType
                    throw new NotSupportedException();
                }
            }

            IEnumerable enumerable;
            if (projection == null)
            {
                enumerable = cursor;
            }
            else
            {
                var lambdaType = projection.GetType();
                var delegateType = lambdaType.GetGenericArguments()[0];
                var sourceType = delegateType.GetGenericArguments()[0];
                var resultType = delegateType.GetGenericArguments()[1];
                var projectorType = typeof(Projector<,>).MakeGenericType(sourceType, resultType);
                var compiledProjection = projection.Compile();
                var projector = Activator.CreateInstance(projectorType, cursor, compiledProjection);
                enumerable = (IEnumerable)projector;
            }

            if (_elementSelector != null)
            {
                return _elementSelector(enumerable);
            }
            else
            {
                return enumerable;
            }
        }

        /// <summary>
        /// Translates a LINQ query expression tree.
        /// </summary>
        /// <param name="expression">The LINQ query expression tree.</param>
        public void Translate(Expression expression)
        {
            // when we reach the original MongoQueryable<TDocument> we're done
            var constantExpression = expression as ConstantExpression;
            if (constantExpression != null)
            {
                if (constantExpression.Type == typeof(MongoQueryable<>).MakeGenericType(DocumentType))
                {
                    return;
                }
            }

            var methodCallExpression = expression as MethodCallExpression;
            if (methodCallExpression != null)
            {
                TranslateMethodCall(methodCallExpression);
                return;
            }

            var message = string.Format("Don't know how to translate expression: {0}.", ExpressionFormatter.ToString(expression));
            throw new NotSupportedException(message);
        }

        // private methods
        private IMongoQuery BuildAndAlsoQuery(BinaryExpression binaryExpression)
        {
            return Query.And(BuildQuery(binaryExpression.Left), BuildQuery(binaryExpression.Right));
        }

        private IMongoQuery BuildAnyQuery(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.DeclaringType == typeof(Enumerable))
            {
                var arguments = methodCallExpression.Arguments.ToArray();
                if (arguments.Length == 1)
                {
                    var serializationInfo = GetSerializationInfo(arguments[0]);
                    if (serializationInfo != null)
                    {
                        return Query.And(
                            Query.NE(serializationInfo.ElementName, BsonNull.Value),
                            Query.Not(serializationInfo.ElementName).Size(0));
                    }
                }
                else if (arguments.Length == 2)
                {
                    throw new NotSupportedException("Enumerable.Any with a predicate is not supported.");
                }
            }
            return null;
        }

        private IMongoQuery BuildArrayLengthQuery(Expression variableExpression, ExpressionType operatorType, ConstantExpression constantExpression)
        {
            if (operatorType != ExpressionType.Equal && operatorType != ExpressionType.NotEqual)
            {
                return null;
            }

            if (constantExpression.Type != typeof(int))
            {
                return null;
            }
            var value = ToInt32(constantExpression);

            BsonSerializationInfo serializationInfo = null;

            var unaryExpression = variableExpression as UnaryExpression;
            if (unaryExpression != null && unaryExpression.NodeType == ExpressionType.ArrayLength)
            {
                var arrayMemberExpression = unaryExpression.Operand as MemberExpression;
                if (arrayMemberExpression != null)
                {
                    serializationInfo = GetSerializationInfo(arrayMemberExpression);
                }
            }

            var memberExpression = variableExpression as MemberExpression;
            if (memberExpression != null && memberExpression.Member.Name == "Count")
            {
                var arrayMemberExpression = memberExpression.Expression as MemberExpression;
                if (arrayMemberExpression != null)
                {
                    serializationInfo = GetSerializationInfo(arrayMemberExpression);
                }
            }

            var methodCallExpression = variableExpression as MethodCallExpression;
            if (methodCallExpression != null && methodCallExpression.Method.Name == "Count" && methodCallExpression.Method.DeclaringType == typeof(Enumerable))
            {
                var arguments = methodCallExpression.Arguments.ToArray();
                if (arguments.Length == 1)
                {
                    var arrayMemberExpression = methodCallExpression.Arguments[0] as MemberExpression;
                    if (arrayMemberExpression != null && arrayMemberExpression.Type != typeof(string))
                    {
                        serializationInfo = GetSerializationInfo(arrayMemberExpression);
                    }
                }
            }

            if (serializationInfo != null)
            {
                if (operatorType == ExpressionType.Equal)
                {
                    return Query.Size(serializationInfo.ElementName, value);
                }
                else
                {
                    return Query.Not(serializationInfo.ElementName).Size(value);
                }
            }

            return null;
        }

        private IMongoQuery BuildBooleanQuery(Expression expression)
        {
            if (expression.Type == typeof(bool))
            {
                var serializationInfo = GetSerializationInfo(expression);
                if (serializationInfo != null)
                {
                    return new QueryDocument(serializationInfo.ElementName, true);
                }
            }
            return null;
        }

        private IMongoQuery BuildComparisonQuery(BinaryExpression binaryExpression)
        {
            // the constant could be on either side
            var variableExpression = binaryExpression.Left;
            var constantExpression = binaryExpression.Right as ConstantExpression;
            var operatorType = binaryExpression.NodeType;
            if (constantExpression == null)
            {
                constantExpression = binaryExpression.Left as ConstantExpression;
                variableExpression = binaryExpression.Right;
                // if the constant was on the left some operators need to be flipped
                switch (operatorType)
                {
                    case ExpressionType.LessThan: operatorType = ExpressionType.GreaterThan; break;
                    case ExpressionType.LessThanOrEqual: operatorType = ExpressionType.GreaterThanOrEqual; break;
                    case ExpressionType.GreaterThan: operatorType = ExpressionType.LessThan; break;
                    case ExpressionType.GreaterThanOrEqual: operatorType = ExpressionType.LessThanOrEqual; break;
                }
            }

            if (constantExpression == null)
            {
                return null;
            }

            var query = BuildArrayLengthQuery(variableExpression, operatorType, constantExpression);
            if (query != null)
            {
                return query;
            }

            query = BuildModQuery(variableExpression, operatorType, constantExpression);
            if (query != null)
            {
                return query;
            }

            query = BuildStringIndexOfQuery(variableExpression, operatorType, constantExpression);
            if (query != null)
            {
                return query;
            }

            query = BuildStringIndexQuery(variableExpression, operatorType, constantExpression);
            if (query != null)
            {
                return query;
            }

            query = BuildStringLengthQuery(variableExpression, operatorType, constantExpression);
            if (query != null)
            {
                return query;
            }

            query = BuildTypeComparisonQuery(variableExpression, operatorType, constantExpression);
            if (query != null)
            {
                return query;
            }

            return BuildComparisonQuery(variableExpression, operatorType, constantExpression);
        }

        private IMongoQuery BuildComparisonQuery(Expression variableExpression, ExpressionType operatorType, ConstantExpression constantExpression)
        {
            BsonSerializationInfo serializationInfo = null;
            var value = constantExpression.Value;

            var unaryExpression = variableExpression as UnaryExpression;
            if (unaryExpression != null && unaryExpression.NodeType == ExpressionType.Convert && unaryExpression.Operand.Type.IsEnum)
            {
                var enumType = unaryExpression.Operand.Type;
                if (unaryExpression.Type == Enum.GetUnderlyingType(enumType))
                {
                    serializationInfo = GetSerializationInfo(unaryExpression.Operand);
                    value = Enum.ToObject(enumType, value); // serialize enum instead of underlying integer
                }
            }
            else
            {
                serializationInfo = GetSerializationInfo(variableExpression);
            }

            if (serializationInfo != null)
            {
                var serializedValue = SerializeValue(serializationInfo, value);
                switch (operatorType)
                {
                    case ExpressionType.Equal: return Query.EQ(serializationInfo.ElementName, serializedValue);
                    case ExpressionType.GreaterThan: return Query.GT(serializationInfo.ElementName, serializedValue);
                    case ExpressionType.GreaterThanOrEqual: return Query.GTE(serializationInfo.ElementName, serializedValue);
                    case ExpressionType.LessThan: return Query.LT(serializationInfo.ElementName, serializedValue);
                    case ExpressionType.LessThanOrEqual: return Query.LTE(serializationInfo.ElementName, serializedValue);
                    case ExpressionType.NotEqual: return Query.NE(serializationInfo.ElementName, serializedValue);
                }
            }

            return null;
        }

        private IMongoQuery BuildConstantQuery(ConstantExpression constantExpression)
        {
            var value = constantExpression.Value;
            if (value != null && value.GetType() == typeof(bool))
            {
                // simulate true or false with a tautology or a reverse tautology
                // the particular reverse tautology chosen has the nice property that it uses the index to return no results quickly
                return new QueryDocument("_id", new BsonDocument("$exists", (bool)value));
            }

            return null;
        }

        private IMongoQuery BuildContainsAllQuery(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.DeclaringType == typeof(LinqToMongo))
            {
                var arguments = methodCallExpression.Arguments.ToArray();
                if (arguments.Length == 2)
                {
                    var serializationInfo = GetSerializationInfo(arguments[0]);
                    var valuesExpression = arguments[1] as ConstantExpression;
                    if (serializationInfo != null && valuesExpression != null)
                    {
                        var itemSerializationInfo = serializationInfo.Serializer.GetItemSerializationInfo();
                        var serializedValues = SerializeValues(itemSerializationInfo, (IEnumerable)valuesExpression.Value);
                        return Query.All(serializationInfo.ElementName, serializedValues);
                    }
                }
            }
            return null;
        }

        private IMongoQuery BuildContainsAnyQuery(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.DeclaringType == typeof(LinqToMongo))
            {
                var arguments = methodCallExpression.Arguments.ToArray();
                if (arguments.Length == 2)
                {
                    var serializationInfo = GetSerializationInfo(arguments[0]);
                    var valuesExpression = arguments[1] as ConstantExpression;
                    if (serializationInfo != null && valuesExpression != null)
                    {
                        var itemSerializationInfo = serializationInfo.Serializer.GetItemSerializationInfo();
                        var serializedValues = SerializeValues(itemSerializationInfo, (IEnumerable)valuesExpression.Value);
                        return Query.In(serializationInfo.ElementName, serializedValues);
                    }
                }
            }
            return null;
        }

        private IMongoQuery BuildContainsQuery(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.DeclaringType == typeof(string))
            {
                return BuildStringQuery(methodCallExpression);
            }

            BsonSerializationInfo serializationInfo = null;
            ConstantExpression valueExpression = null;
            var arguments = methodCallExpression.Arguments.ToArray();
            if (arguments.Length == 1)
            {
                if (typeof(IEnumerable).IsAssignableFrom(methodCallExpression.Method.DeclaringType))
                {
                    serializationInfo = GetSerializationInfo(methodCallExpression.Object);
                    valueExpression = arguments[0] as ConstantExpression;
                }
            }
            else if (arguments.Length == 2)
            {
                if (methodCallExpression.Method.DeclaringType == typeof(Enumerable))
                {
                    serializationInfo = GetSerializationInfo(arguments[0]);
                    valueExpression = arguments[1] as ConstantExpression;
                }
            }

            if (serializationInfo != null && valueExpression != null)
            {
                var itemSerializationInfo = serializationInfo.Serializer.GetItemSerializationInfo();
                var serializedValue = SerializeValue(itemSerializationInfo, valueExpression.Value);
                return Query.EQ(serializationInfo.ElementName, serializedValue);
            }

            return null;
        }

        private IMongoQuery BuildEqualsQuery(MethodCallExpression methodCallExpression)
        {
            var arguments = methodCallExpression.Arguments.ToArray();

            // assume that static and instance Equals mean the same thing for all classes (i.e. an equality test)
            Expression firstExpression = null;
            Expression secondExpression = null;
            if (methodCallExpression.Object == null)
            {
                // static Equals method
                if (arguments.Length == 2)
                {
                    firstExpression = arguments[0];
                    secondExpression = arguments[1];
                }
            }
            else
            {
                // instance Equals method
                if (arguments.Length == 1)
                {
                    firstExpression = methodCallExpression.Object;
                    secondExpression = arguments[0];
                }
            }

            if (firstExpression != null && secondExpression != null)
            {
                // the constant could be either expression
                var variableExpression = firstExpression;
                var constantExpression = secondExpression as ConstantExpression;
                if (constantExpression == null)
                {
                    constantExpression = firstExpression as ConstantExpression;
                    variableExpression = secondExpression;
                }

                if (constantExpression == null)
                {
                    return null;
                }

                return BuildComparisonQuery(variableExpression, ExpressionType.Equal, constantExpression);
            }

            return null;
        }

        private IMongoQuery BuildInQuery(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.DeclaringType == typeof(LinqToMongo))
            {
                var arguments = methodCallExpression.Arguments.ToArray();
                if (arguments.Length == 2)
                {
                    var serializationInfo = GetSerializationInfo(arguments[0]);
                    var valuesExpression = arguments[1] as ConstantExpression;
                    if (serializationInfo != null && valuesExpression != null)
                    {
                        var serializedValues = SerializeValues(serializationInfo, (IEnumerable)valuesExpression.Value);
                        return Query.In(serializationInfo.ElementName, serializedValues);
                    }
                }
            }
            return null;
        }

        private IMongoQuery BuildInjectQuery(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.DeclaringType == typeof(LinqToMongo))
            {
                var arguments = methodCallExpression.Arguments.ToArray();
                if (arguments.Length == 1)
                {
                    var queryExpression = arguments[0] as ConstantExpression;
                    if (queryExpression != null)
                    {
                        return (IMongoQuery)queryExpression.Value;
                    }
                }
            }
            return null;
        }

        private IMongoQuery BuildIsMatchQuery(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.DeclaringType == typeof(Regex))
            {
                var arguments = methodCallExpression.Arguments.ToArray();
                var obj = methodCallExpression.Object;
                if (obj == null)
                {
                    if (arguments.Length == 2 || arguments.Length == 3)
                    {
                        var serializationInfo = GetSerializationInfo(arguments[0]);
                        var patternExpression = arguments[1] as ConstantExpression;
                        if (serializationInfo != null && patternExpression != null)
                        {
                            var pattern = patternExpression.Value as string;
                            if (pattern != null)
                            {
                                var options = RegexOptions.None;
                                if (arguments.Length == 3)
                                {
                                    var optionsExpression = arguments[2] as ConstantExpression;
                                    if (optionsExpression == null || optionsExpression.Type != typeof(RegexOptions))
                                    {
                                        return null;
                                    }
                                    options = (RegexOptions)optionsExpression.Value;
                                }
                                var regex = new Regex(pattern, options);
                                return Query.Matches(serializationInfo.ElementName, regex);
                            }
                        }
                    }
                }
                else
                {
                    var regexExpression = obj as ConstantExpression;
                    if (regexExpression != null && arguments.Length == 1)
                    {
                        var serializationInfo = GetSerializationInfo(arguments[0]);
                        var regex = regexExpression.Value as Regex;
                        if (serializationInfo != null && regex != null)
                        {
                            return Query.Matches(serializationInfo.ElementName, regex);
                        }
                    }
                }
            }
            return null;
        }

        private IMongoQuery BuildIsNullOrEmptyQuery(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.DeclaringType == typeof(string) && methodCallExpression.Object == null)
            {
                var arguments = methodCallExpression.Arguments.ToArray();
                if (arguments.Length == 1)
                {
                    var serializationInfo = GetSerializationInfo(arguments[0]);
                    if (serializationInfo != null)
                    {
                        return Query.Or(
                            Query.Type(serializationInfo.ElementName, BsonType.Null), // this is the safe way to test for null
                            Query.EQ(serializationInfo.ElementName, "")
                        );
                    }
                }
            }

            return null;
        }

        private IMongoQuery BuildMethodCallQuery(MethodCallExpression methodCallExpression)
        {
            switch (methodCallExpression.Method.Name)
            {
                case "Any": return BuildAnyQuery(methodCallExpression);
                case "Contains": return BuildContainsQuery(methodCallExpression);
                case "ContainsAll": return BuildContainsAllQuery(methodCallExpression);
                case "ContainsAny": return BuildContainsAnyQuery(methodCallExpression);
                case "EndsWith": return BuildStringQuery(methodCallExpression);
                case "Equals": return BuildEqualsQuery(methodCallExpression);
                case "In": return BuildInQuery(methodCallExpression);
                case "Inject": return BuildInjectQuery(methodCallExpression);
                case "IsMatch": return BuildIsMatchQuery(methodCallExpression);
                case "IsNullOrEmpty": return BuildIsNullOrEmptyQuery(methodCallExpression);
                case "StartsWith": return BuildStringQuery(methodCallExpression);
            }
            return null;
        }

        private IMongoQuery BuildModQuery(Expression variableExpression, ExpressionType operatorType, ConstantExpression constantExpression)
        {
            if (operatorType != ExpressionType.Equal && operatorType != ExpressionType.NotEqual)
            {
                return null;
            }

            if (constantExpression.Type != typeof(int))
            {
                return null;
            }
            var value = ToInt32(constantExpression);

            var modBinaryExpression = variableExpression as BinaryExpression;
            if (modBinaryExpression != null && modBinaryExpression.NodeType == ExpressionType.Modulo)
            {
                var serializationInfo = GetSerializationInfo(modBinaryExpression.Left);
                var modulusExpression = modBinaryExpression.Right as ConstantExpression;
                if (serializationInfo != null && modulusExpression != null)
                {
                    var modulus = ToInt32(modulusExpression);
                    if (operatorType == ExpressionType.Equal)
                    {
                        return Query.Mod(serializationInfo.ElementName, modulus, value);
                    }
                    else
                    {
                        return Query.Not(serializationInfo.ElementName).Mod(modulus, value);
                    }
                }
            }

            return null;
        }

        private IMongoQuery BuildNotQuery(UnaryExpression unaryExpression)
        {
            var queryDocument = BuildQuery(unaryExpression.Operand).ToBsonDocument();
            if (queryDocument.ElementCount == 1)
            {
                var elementName = queryDocument.GetElement(0).Name;
                switch (elementName)
                {
                    case "$and":
                        // there is no $nand and $not only works as a meta operator on a single operator so simulate $not using $nor
                        return new QueryDocument("$nor", new BsonArray { queryDocument });
                    case "$or":
                        return new QueryDocument("$nor", queryDocument[0].AsBsonArray);
                    case "$nor":
                        return new QueryDocument("$or", queryDocument[0].AsBsonArray);
                }

                var operatorDocument = queryDocument[0] as BsonDocument;
                if (operatorDocument != null && operatorDocument.ElementCount > 0)
                {
                    var operatorName = operatorDocument.GetElement(0).Name;
                    if (operatorDocument.ElementCount == 1)
                    {
                        switch (operatorName)
                        {
                            case "$exists":
                                var boolValue = operatorDocument[0].AsBoolean;
                                return new QueryDocument(elementName, new BsonDocument("$exists", !boolValue));
                            case "$in":
                                var values = operatorDocument[0].AsBsonArray;
                                return new QueryDocument(elementName, new BsonDocument("$nin", values));
                            case "$not":
                                var predicate = operatorDocument[0];
                                return new QueryDocument(elementName, predicate);
                            case "$ne":
                                var comparisonValue = operatorDocument[0];
                                return new QueryDocument(elementName, comparisonValue);
                        }
                        if (operatorName[0] == '$')
                        {
                            // use $not as a meta operator on a single operator
                            return new QueryDocument(elementName, new BsonDocument("$not", operatorDocument));
                        }
                    }
                    else
                    {
                        // $ref isn't an operator (it's the first field of a DBRef)
                        if (operatorName[0] == '$' && operatorName != "$ref")
                        {
                            // $not only works as a meta operator on a single operator so simulate $not using $nor
                            return new QueryDocument("$nor", new BsonArray { queryDocument });
                        }
                    }
                }

                var operatorValue = queryDocument[0];
                if (operatorValue.IsBsonRegularExpression)
                {
                    return new QueryDocument(elementName, new BsonDocument("$not", operatorValue));
                }

                // turn implied equality comparison into $ne
                return new QueryDocument(elementName, new BsonDocument("$ne", operatorValue));
            }

            // $not only works as a meta operator on a single operator so simulate $not using $nor
            return new QueryDocument("$nor", new BsonArray { queryDocument });
        }

        private IMongoQuery BuildOrElseQuery(BinaryExpression binaryExpression)
        {
            return Query.Or(BuildQuery(binaryExpression.Left), BuildQuery(binaryExpression.Right));
        }

        private IMongoQuery BuildQuery(Expression expression)
        {
            IMongoQuery query = null;

            switch (expression.NodeType)
            {
                case ExpressionType.AndAlso:
                    query = BuildAndAlsoQuery((BinaryExpression)expression);
                    break;
                case ExpressionType.ArrayIndex:
                    query = BuildBooleanQuery(expression);
                    break;
                case ExpressionType.Call:
                    query = BuildMethodCallQuery((MethodCallExpression)expression);
                    break;
                case ExpressionType.Constant:
                    query = BuildConstantQuery((ConstantExpression)expression);
                    break;
                case ExpressionType.Equal:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.NotEqual:
                    query = BuildComparisonQuery((BinaryExpression)expression);
                    break;
                case ExpressionType.MemberAccess:
                    query = BuildBooleanQuery(expression);
                    break;
                case ExpressionType.Not:
                    query = BuildNotQuery((UnaryExpression)expression);
                    break;
                case ExpressionType.OrElse:
                    query = BuildOrElseQuery((BinaryExpression)expression);
                    break;
                case ExpressionType.TypeIs:
                    query = BuildTypeIsQuery((TypeBinaryExpression)expression);
                    break;
            }

            if (query == null)
            {
                var message = string.Format("Unsupported where clause: {0}.", ExpressionFormatter.ToString(expression));
                throw new ArgumentException(message);
            }

            return query;
        }

        private IMongoQuery BuildStringIndexOfQuery(Expression variableExpression, ExpressionType operatorType, ConstantExpression constantExpression)
        {
            // TODO: support other comparison operators
            if (operatorType != ExpressionType.Equal)
            {
                return null;
            }

            if (constantExpression.Type != typeof(int))
            {
                return null;
            }
            var index = ToInt32(constantExpression);

            var methodCallExpression = variableExpression as MethodCallExpression;
            if (methodCallExpression != null && 
                (methodCallExpression.Method.Name == "IndexOf" || methodCallExpression.Method.Name == "IndexOfAny") &&
                methodCallExpression.Method.DeclaringType == typeof(string))
            {
                var serializationInfo = GetSerializationInfo(methodCallExpression.Object);
                if (serializationInfo == null)
                {
                    return null;
                }

                object value;
                var startIndex = -1;
                var count = -1;

                var args = methodCallExpression.Arguments.ToArray();
                switch (args.Length)
                {
                    case 3:
                        var countExpression = args[2] as ConstantExpression;
                        if (countExpression == null)
                        {
                            return null;
                        }
                        count = ToInt32(countExpression);
                        goto case 2;
                    case 2:
                        var startIndexExpression = args[1] as ConstantExpression;
                        if (startIndexExpression == null)
                        {
                            return null;
                        }
                        startIndex = ToInt32(startIndexExpression);
                        goto case 1;
                    case 1:
                        var valueExpression = args[0] as ConstantExpression;
                        if (valueExpression == null)
                        {
                            return null;
                        }
                        value = valueExpression.Value;
                        break;
                    default:
                        return null;
                }

                string pattern = null;
                if (value.GetType() == typeof(char) || value.GetType() == typeof(char[]))
                {
                    char[] chars;
                    if (value.GetType() == typeof(char))
                    {
                        chars = new char[] { (char)value };
                    }
                    else
                    {
                        chars = (char[])value;
                    }
                    var positiveClass = string.Join("", chars.Select(c => (c == '-') ? "\\-" : (c == ']') ? "\\]" : Regex.Escape(c.ToString())).ToArray());
                    var negativeClass = "[^" + positiveClass + "]";
                    if (chars.Length > 1)
                    {
                        positiveClass = "[" + positiveClass + "]";
                    }

                    if (startIndex == -1)
                    {
                        // the regex for: IndexOf(c) == index 
                        // is: /^[^c]{index}c/
                        pattern = string.Format("^{0}{{{1}}}{2}", negativeClass, index, positiveClass);
                    }
                    else
                    {
                        if (count == -1)
                        {
                            // the regex for: IndexOf(c, startIndex) == index
                            // is: /^.{startIndex}[^c]{index - startIndex}c/
                            pattern = string.Format("^.{{{0}}}{1}{{{2}}}{3}", startIndex, negativeClass, index - startIndex, positiveClass);
                        }
                        else
                        {
                            if (index >= startIndex + count)
                            {
                                // index is outside of the substring so no match is possible
                                return Query.Exists("_id", false); // matches no documents
                            }
                            else
                            {
                                // the regex for: IndexOf(c, startIndex, count) == index
                                // is: /^.{startIndex}(?=.{count})[^c]{index - startIndex}c/
                                pattern = string.Format("^.{{{0}}}(?=.{{{1}}}){2}{{{3}}}{4}", startIndex, count, negativeClass, index - startIndex, positiveClass);
                            }
                        }
                    }
                }
                else if (value.GetType() == typeof(string))
                {
                    var escapedString = Regex.Escape((string)value);
                    if (startIndex == -1)
                    {
                        // the regex for: IndexOf(s) == index 
                        // is: /^(?!.{0,index - 1}s).{index}s/
                        pattern = string.Format("^(?!.{{0,{2}}}{0}).{{{1}}}{0}", escapedString, index, index - 1);
                    }
                    else
                    {
                        if (count == -1)
                        {
                            // the regex for: IndexOf(s, startIndex) == index
                            // is: /^.{startIndex}(?!.{0, index - startIndex - 1}s).{index - startIndex}s/
                            pattern = string.Format("^.{{{1}}}(?!.{{0,{2}}}{0}).{{{3}}}{0}", escapedString, startIndex, index - startIndex - 1, index - startIndex);
                        }
                        else
                        {
                            var unescapedLength = ((string)value).Length;
                            if (unescapedLength > startIndex + count - index)
                            {
                                // substring isn't long enough to match
                                return Query.Exists("_id", false); // matches no documents
                            }
                            else
                            {
                                // the regex for: IndexOf(s, startIndex, count) == index
                                // is: /^.{startIndex}(?=.{count})(?!.{0,index - startIndex - 1}s).{index - startIndex)s/
                                pattern = string.Format("^.{{{1}}}(?=.{{{2}}})(?!.{{0,{3}}}{0}).{{{4}}}{0}", escapedString, startIndex, count, index - startIndex - 1, index - startIndex);
                            }
                        }
                    }
                }

                if (pattern != null)
                {
                    return Query.Matches(serializationInfo.ElementName, new BsonRegularExpression(pattern, "s"));
                }
            }

            return null;
        }

        private IMongoQuery BuildStringIndexQuery(Expression variableExpression, ExpressionType operatorType, ConstantExpression constantExpression)
        {
            var unaryExpression = variableExpression as UnaryExpression;
            if (unaryExpression == null)
            {
                return null;
            }

            if (unaryExpression.NodeType != ExpressionType.Convert || unaryExpression.Type != typeof(int))
            {
                return null;
            }

            var methodCallExpression = unaryExpression.Operand as MethodCallExpression;
            if (methodCallExpression == null)
            {
                return null;
            }

            var method = methodCallExpression.Method;
            if (method.DeclaringType != typeof(string) || method.Name != "get_Chars")
            {
                return null;
            }

            var stringExpression = methodCallExpression.Object;
            if (stringExpression == null)
            {
                return null;
            }

            var serializationInfo = GetSerializationInfo(stringExpression);
            if (serializationInfo == null)
            {
                return null;
            }

            var args = methodCallExpression.Arguments.ToArray();
            if (args.Length != 1)
            {
                return null;
            }

            var indexExpression = args[0] as ConstantExpression;
            if (indexExpression == null)
            {
                return null;
            }
            var index = ToInt32(indexExpression);

            if (constantExpression.Type != typeof(int))
            {
                return null;
            }
            var value = ToInt32(constantExpression);

            var c = new string((char)value, 1);
            var positiveClass = (c == "-") ? "\\-" : (c == "]") ? "\\]" : Regex.Escape(c);
            var negativeClass = "[^" + positiveClass + "]";

            string characterClass;
            switch (operatorType)
            {
                case ExpressionType.Equal:
                    characterClass = positiveClass;
                    break;
                case ExpressionType.NotEqual:
                    characterClass = negativeClass;
                    break;
                default:
                    return null; // TODO: suport other comparison operators?
            }
            var pattern = string.Format("^.{{{0}}}{1}", index, characterClass);

            return Query.Matches(serializationInfo.ElementName, new BsonRegularExpression(pattern, "s"));
        }

        private IMongoQuery BuildStringLengthQuery(Expression variableExpression, ExpressionType operatorType, ConstantExpression constantExpression)
        {
            if (constantExpression.Type != typeof(int))
            {
                return null;
            }
            var value = ToInt32(constantExpression);

            BsonSerializationInfo serializationInfo = null;

            var memberExpression = variableExpression as MemberExpression;
            if (memberExpression != null && memberExpression.Member.Name == "Length")
            {
                var stringMemberExpression = memberExpression.Expression as MemberExpression;
                if (stringMemberExpression != null && stringMemberExpression.Type == typeof(string))
                {
                    serializationInfo = GetSerializationInfo(stringMemberExpression);
                }
            }

            var methodCallExpression = variableExpression as MethodCallExpression;
            if (methodCallExpression != null && methodCallExpression.Method.Name == "Count" && methodCallExpression.Method.DeclaringType == typeof(Enumerable))
            {
                var args = methodCallExpression.Arguments.ToArray();
                if (args.Length == 1)
                {
                    var stringMemberExpression = args[0] as MemberExpression;
                    if (stringMemberExpression != null && stringMemberExpression.Type == typeof(string))
                    {
                        serializationInfo = GetSerializationInfo(stringMemberExpression);
                    }
                }
            }

            if (serializationInfo != null)
            {
                string regex = null;
                switch (operatorType)
                {
                    case ExpressionType.NotEqual: case ExpressionType.Equal: regex = @"/^.{" + value.ToString() + "}$/s"; break;
                    case ExpressionType.GreaterThan: regex = @"/^.{" + (value + 1).ToString() + ",}$/s"; break;
                    case ExpressionType.GreaterThanOrEqual: regex = @"/^.{" + value.ToString() + ",}$/s"; break;
                    case ExpressionType.LessThan: regex = @"/^.{0," + (value - 1).ToString() + "}$/s"; break;
                    case ExpressionType.LessThanOrEqual: regex = @"/^.{0," + value.ToString() + "}$/s"; break;
                }
                if (regex != null)
                {
                    if (operatorType == ExpressionType.NotEqual)
                    {
                        return Query.Not(serializationInfo.ElementName).Matches(regex);
                    }
                    else
                    {
                        return Query.Matches(serializationInfo.ElementName, regex);
                    }
                }
            }

            return null;
        }

        private IMongoQuery BuildStringQuery(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.DeclaringType != typeof(string))
            {
                return null;
            }

            var arguments = methodCallExpression.Arguments.ToArray();
            if (arguments.Length != 1)
            {
                return null;
            }

            var stringExpression = methodCallExpression.Object;
            var constantExpression = arguments[0] as ConstantExpression;
            if (constantExpression == null)
            {
                return null;
            }

            var pattern = Regex.Escape((string)constantExpression.Value);
            switch (methodCallExpression.Method.Name)
            {
                case "Contains": pattern = ".*" + pattern + ".*"; break;
                case "EndsWith": pattern = ".*" + pattern; break;
                case "StartsWith": pattern = pattern + ".*"; break; // query optimizer will use index for rooted regular expressions
                default: return null;
            }

            var caseInsensitive = false;
            MethodCallExpression stringMethodCallExpression;
            while ((stringMethodCallExpression = stringExpression as MethodCallExpression) != null)
            {
                var trimStart = false;
                var trimEnd = false;
                Expression trimCharsExpression = null;
                switch (stringMethodCallExpression.Method.Name)
                {
                    case "ToLower":
                        caseInsensitive = true;
                        break;
                    case "ToUpper":
                        caseInsensitive = true;
                        break;
                    case "Trim":
                        trimStart = true;
                        trimEnd = true;
                        trimCharsExpression = stringMethodCallExpression.Arguments.FirstOrDefault();
                        break;
                    case "TrimEnd":
                        trimEnd = true;
                        trimCharsExpression = stringMethodCallExpression.Arguments.First();
                        break;
                    case "TrimStart":
                        trimStart = true;
                        trimCharsExpression = stringMethodCallExpression.Arguments.First();
                        break;
                    default:
                        return null;
                }

                if (trimStart || trimEnd)
                {
                    var trimCharsPattern = GetTrimCharsPattern(trimCharsExpression);
                    if (trimCharsPattern == null)
                    {
                        return null;
                    }

                    if (trimStart)
                    {
                        pattern = trimCharsPattern + pattern;
                    }
                    if (trimEnd)
                    {
                        pattern = pattern + trimCharsPattern;
                    }
                }

                stringExpression = stringMethodCallExpression.Object;
            }

            pattern = "^" + pattern + "$";
            if (pattern.StartsWith("^.*"))
            {
                pattern = pattern.Substring(3);
            }
            if (pattern.EndsWith(".*$"))
            {
                pattern = pattern.Substring(0, pattern.Length - 3);
            }

            var serializationInfo = GetSerializationInfo(stringExpression);
            if (serializationInfo != null)
            {
                var options = caseInsensitive ? "is" : "s";
                return Query.Matches(serializationInfo.ElementName, new BsonRegularExpression(pattern, options));
            }

            return null;
        }

        private IMongoQuery BuildTypeComparisonQuery(Expression variableExpression, ExpressionType operatorType, ConstantExpression constantExpression)
        {
            if (operatorType != ExpressionType.Equal)
            {
                // TODO: support NotEqual?
                return null;
            }

            if (constantExpression.Type != typeof(Type))
            {
                return null;
            }
            var actualType = (Type)constantExpression.Value;

            var methodCallExpression = variableExpression as MethodCallExpression;
            if (methodCallExpression == null)
            {
                return null;
            }
            if (methodCallExpression.Method.Name != "GetType" || methodCallExpression.Object == null)
            {
                return null;
            }
            if (methodCallExpression.Arguments.Count != 0)
            {
                return null;
            }

            // TODO: would the object ever not be a ParameterExpression?
            var parameterExpression = methodCallExpression.Object as ParameterExpression;
            if (parameterExpression == null)
            {
                return null;
            }

            var serializationInfo = GetSerializationInfo(parameterExpression);
            if (serializationInfo == null)
            {
                return null;
            }
            var nominalType = serializationInfo.NominalType;

            var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(nominalType);
            var discriminator = discriminatorConvention.GetDiscriminator(nominalType, actualType);
            if (discriminator == null)
            {
                return new QueryDocument(); // matches everything
            }

            if (discriminator.IsBsonArray)
            {
                var discriminatorArray = discriminator.AsBsonArray;
                var queries = new IMongoQuery[discriminatorArray.Count + 1];
                queries[0] = Query.Size(discriminatorConvention.ElementName, discriminatorArray.Count);
                for (var i = 0; i < discriminatorArray.Count; i++)
                {
                    queries[i + 1] = Query.EQ(string.Format("{0}.{1}", discriminatorConvention.ElementName, i), discriminatorArray[i]);
                }
                return Query.And(queries);
            }
            else
            {
                return Query.And(
                    Query.Exists(discriminatorConvention.ElementName + ".0", false), // trick to check that element is not an array
                    Query.EQ(discriminatorConvention.ElementName, discriminator));
            }
        }

        private IMongoQuery BuildTypeIsQuery(TypeBinaryExpression typeBinaryExpression)
        {
            var nominalType = typeBinaryExpression.Expression.Type;
            var actualType = typeBinaryExpression.TypeOperand;

            var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(nominalType);
            var discriminator = discriminatorConvention.GetDiscriminator(nominalType, actualType);
            if (discriminator == null)
            {
                return new QueryDocument(); // matches everything
            }

            if (discriminator.IsBsonArray)
            {
                discriminator = discriminator.AsBsonArray[discriminator.AsBsonArray.Count - 1];
            }

            return Query.EQ(discriminatorConvention.ElementName, discriminator);
        }

        private void CombinePredicateWithWhereClause(MethodCallExpression methodCallExpression, LambdaExpression predicate)
        {
            if (predicate != null)
            {
                if (_projection != null)
                {
                    var message = string.Format("{0} with predicate after a projection is not supported.", methodCallExpression.Method.Name);
                    throw new NotSupportedException(message);
                }

                if (_where == null)
                {
                    _where = predicate;
                    return;
                }

                if (_where.Parameters.Count != 1)
                {
                    throw new MongoInternalException("Where lambda expression should have one parameter.");
                }
                var whereBody = _where.Body;
                var whereParameter = _where.Parameters[0];

                if (predicate.Parameters.Count != 1)
                {
                    throw new MongoInternalException("Predicate lambda expression should have one parameter.");
                }
                var predicateBody = predicate.Body;
                var predicateParameter = predicate.Parameters[0];

                // when using OfType the parameter types might not match (but they do have to be compatible)
                ParameterExpression parameter;
                if (predicateParameter.Type.IsAssignableFrom(whereParameter.Type))
                {
                    predicateBody = ExpressionParameterReplacer.ReplaceParameter(predicateBody, predicateParameter, whereParameter);
                    parameter = whereParameter;
                }
                else if (whereParameter.Type.IsAssignableFrom(predicateParameter.Type))
                {
                    whereBody = ExpressionParameterReplacer.ReplaceParameter(whereBody, whereParameter, predicateParameter);
                    parameter = predicateParameter;
                }
                else
                {
                    throw new NotSupportedException("Can't combine existing where clause with new predicate because parameter types are incompatible.");
                }

                var combinedBody = Expression.AndAlso(whereBody, predicateBody);
                _where = Expression.Lambda(combinedBody, parameter);
            }
        }

        private object ExecuteDistinct(IMongoQuery query)
        {
            if (_orderBy != null)
            {
                throw new NotSupportedException("Distinct cannot be used with OrderBy.");
            }
            if (_skip != null || _take != null)
            {
                throw new NotSupportedException("Distinct cannot be used with Skip or Take.");
            }
            if (_projection == null)
            {
                throw new NotSupportedException("Distinct must be used with Select to identify the field whose distinct values are to be found.");
            }

            var keyExpression = _projection.Body;
            var serializationInfo = GetSerializationInfo(keyExpression);
            if (serializationInfo == null)
            {
                var message = string.Format("Select used with Distinct is not valid: {0}.", ExpressionFormatter.ToString(keyExpression));
                throw new NotSupportedException(message);
            }
            var dottedElementName = serializationInfo.ElementName;
            var source = Collection.Distinct(dottedElementName, query);

            var deserializationProjectorGenericDefinition = typeof(DeserializationProjector<>);
            var deserializationProjectorType = deserializationProjectorGenericDefinition.MakeGenericType(keyExpression.Type);
            return Activator.CreateInstance(deserializationProjectorType, source, serializationInfo);
        }

        private BsonSerializationInfo GetSerializationInfo(Expression expression)
        {
            var parameterExpression = expression as ParameterExpression;
            if (parameterExpression != null)
            {
                var serializer = BsonSerializer.LookupSerializer(parameterExpression.Type);
                return new BsonSerializationInfo(
                    null, // elementName
                    serializer,
                    parameterExpression.Type, // nominalType
                    null); // serialization options
            }

            // when using OfType the documentType used by the parameter might be a subclass of the DocumentType from the collection
            parameterExpression = ExpressionParameterFinder.FindParameter(expression);
            if (parameterExpression != null)
            {
                var serializer = BsonSerializer.LookupSerializer(parameterExpression.Type);
                return GetSerializationInfo(serializer, expression);
            }

            return null;
        }

        private BsonSerializationInfo GetSerializationInfo(IBsonSerializer serializer, Expression expression)
        {
            var memberExpression = expression as MemberExpression;
            if (memberExpression != null)
            {
                return GetSerializationInfoMember(serializer, memberExpression);
            }

            var binaryExpression = expression as BinaryExpression;
            if (binaryExpression != null && binaryExpression.NodeType == ExpressionType.ArrayIndex)
            {
                return GetSerializationInfoArrayIndex(serializer, binaryExpression);
            }

            var methodCallExpression = expression as MethodCallExpression;
            if (methodCallExpression != null && methodCallExpression.Method.Name == "get_Item")
            {
                return GetSerializationInfoGetItem(serializer, methodCallExpression);
            }

            return null;
        }

        private BsonSerializationInfo GetSerializationInfoArrayIndex(IBsonSerializer serializer, BinaryExpression binaryExpression)
        {
            var arraySerializationInfo = GetSerializationInfo(serializer, binaryExpression.Left);
            if (arraySerializationInfo != null)
            {
                var itemSerializationInfo = arraySerializationInfo.Serializer.GetItemSerializationInfo();
                var indexEpression = binaryExpression.Right as ConstantExpression;
                if (indexEpression != null)
                {
                    var index = Convert.ToInt32(indexEpression.Value);
                    return new BsonSerializationInfo(
                        arraySerializationInfo.ElementName + "." + index.ToString(),
                        itemSerializationInfo.Serializer,
                        itemSerializationInfo.NominalType,
                        itemSerializationInfo.SerializationOptions);
                }
            }

            return null;
        }

        private BsonSerializationInfo GetSerializationInfoGetItem(IBsonSerializer serializer, MethodCallExpression methodCallExpression)
        {
            var arguments = methodCallExpression.Arguments.ToArray();
            if (arguments.Length == 1)
            {
                var arraySerializationInfo = GetSerializationInfo(serializer, methodCallExpression.Object);
                if (arraySerializationInfo != null)
                {
                    var itemSerializationInfo = arraySerializationInfo.Serializer.GetItemSerializationInfo();
                    var indexEpression = arguments[0] as ConstantExpression;
                    if (indexEpression != null)
                    {
                        var index = Convert.ToInt32(indexEpression.Value);
                        return new BsonSerializationInfo(
                            arraySerializationInfo.ElementName + "." + index.ToString(),
                            itemSerializationInfo.Serializer,
                            itemSerializationInfo.NominalType,
                            itemSerializationInfo.SerializationOptions);
                    }
                }
            }

            return null;
        }

        private BsonSerializationInfo GetSerializationInfoMember(IBsonSerializer serializer, MemberExpression memberExpression)
        {
            var declaringType = memberExpression.Expression.Type;
            var memberName = memberExpression.Member.Name;

            var containingExpression = memberExpression.Expression;
            if (containingExpression.NodeType == ExpressionType.Parameter)
            {
                try
                {
                    return serializer.GetMemberSerializationInfo(memberName);
                }
                catch (NotSupportedException)
                {
                    var message = string.Format("LINQ queries on fields or properties of class {0} are not supported because the serializer for {0} does not implement the GetMemberSerializationInfo method.", declaringType.Name);
                    throw new NotSupportedException(message);
                }
            }
            else
            {
                var containingSerializationInfo = GetSerializationInfo(serializer, containingExpression);
                try
                {
                    var memberSerializationInfo = containingSerializationInfo.Serializer.GetMemberSerializationInfo(memberName);
                    return new BsonSerializationInfo(
                        containingSerializationInfo.ElementName + "." + memberSerializationInfo.ElementName,
                        memberSerializationInfo.Serializer,
                        memberSerializationInfo.NominalType,
                        memberSerializationInfo.SerializationOptions);
                }
                catch (NotSupportedException)
                {
                    var message = string.Format("LINQ queries on fields or properties of class {0} are not supported because the serializer for {0} does not implement the GetMemberSerializationInfo method.", declaringType.Name);
                    throw new NotSupportedException(message);
                }
            }
        }

        private string GetTrimCharsPattern(Expression trimCharsExpression)
        {
            if (trimCharsExpression == null)
            {
                return "\\s*";
            }

            var constantExpresion = trimCharsExpression as ConstantExpression;
            if (constantExpresion == null || constantExpresion.Type != typeof(char[]))
            {
                return null;
            }

            var trimChars = (char[])constantExpresion.Value;
            if (trimChars.Length == 0)
            {
                return "\\s*";
            }

            // build a pattern that matches the characters to be trimmed
            var characterClass = string.Join("", trimChars.Select(c => (c == '-') ? "\\-" : (c == ']') ? "\\]" : Regex.Escape(c.ToString())).ToArray());
            if (trimChars.Length > 1)
            {
                characterClass = "[" + characterClass + "]";
            }
            return characterClass + "*";
        }

        private BsonValue SerializeValue(BsonSerializationInfo serializationInfo, object value)
        {
            var bsonDocument = new BsonDocument();
            var bsonWriter = BsonWriter.Create(bsonDocument);
            bsonWriter.WriteStartDocument();
            bsonWriter.WriteName("value");
            serializationInfo.Serializer.Serialize(bsonWriter, serializationInfo.NominalType, value, serializationInfo.SerializationOptions);
            bsonWriter.WriteEndDocument();
            return bsonDocument[0];
        }

        private BsonArray SerializeValues(BsonSerializationInfo serializationInfo, IEnumerable values)
        {
            var bsonDocument = new BsonDocument();
            var bsonWriter = BsonWriter.Create(bsonDocument);
            bsonWriter.WriteStartDocument();
            bsonWriter.WriteName("values");
            bsonWriter.WriteStartArray();
            foreach (var value in values)
            {
                serializationInfo.Serializer.Serialize(bsonWriter, serializationInfo.NominalType, value, serializationInfo.SerializationOptions);
            }
            bsonWriter.WriteEndArray();
            bsonWriter.WriteEndDocument();
            return bsonDocument[0].AsBsonArray;
        }

        private void SetElementSelector(MethodCallExpression methodCallExpression, Func<IEnumerable, object> elementSelector)
        {
            if (_elementSelector != null)
            {
                var message = string.Format("{0} cannot be combined with any other element selector.", methodCallExpression.Method.Name);
                throw new NotSupportedException(message);
            }
            _elementSelector = elementSelector;
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

            return (int)constantExpression.Value;
        }

        private void TranslateAny(MethodCallExpression methodCallExpression)
        {
            LambdaExpression predicate = null;
            switch (methodCallExpression.Arguments.Count)
            {
                case 1:
                    break;
                case 2:
                    predicate = (LambdaExpression)StripQuote(methodCallExpression.Arguments[1]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("methodCallExpression");
            }
            CombinePredicateWithWhereClause(methodCallExpression, predicate);

            // ignore any projection since we only are interested in the count
            _projection = null;

            // note: recall that cursor method Size respects Skip and Limit while Count does not
            SetElementSelector(methodCallExpression, source => ((int)((MongoCursor)source).Size()) > 0);
        }

        private void TranslateCount(MethodCallExpression methodCallExpression)
        {
            LambdaExpression predicate = null;
            switch (methodCallExpression.Arguments.Count)
            {
                case 1:
                    break;
                case 2:
                    predicate = (LambdaExpression)StripQuote(methodCallExpression.Arguments[1]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("methodCallExpression");
            }
            CombinePredicateWithWhereClause(methodCallExpression, predicate);

            // ignore any projection since we only are interested in the count
            _projection = null;

            // note: recall that cursor method Size respects Skip and Limit while Count does not
            switch (methodCallExpression.Method.Name)
            {
                case "Count":
                    SetElementSelector(methodCallExpression, source => (int)((MongoCursor)source).Size());
                    break;
                case "LongCount":
                    SetElementSelector(methodCallExpression, source => ((MongoCursor)source).Size());
                    break;
            }
        }

        private void TranslateDistinct(MethodCallExpression methodCallExpression)
        {
            var arguments = methodCallExpression.Arguments.ToArray();
            if (arguments.Length != 1)
            {
                var message = "The version of the Distinct query operator with an equality comparer is not supported.";
                throw new NotSupportedException(message);
            }

            _distinct = true;
        }

        private void TranslateElementAt(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Arguments.Count != 2)
            {
                throw new ArgumentOutOfRangeException("methodCallExpression");
            }

            // ElementAt can be implemented more efficiently in terms of Skip, Limit and First
            var index = ToInt32(methodCallExpression.Arguments[1]);
            _skip = Expression.Constant(index);
            _take = Expression.Constant(1);

            switch (methodCallExpression.Method.Name)
            {
                case "ElementAt":
                    SetElementSelector(methodCallExpression, source => source.Cast<object>().First());
                    break;
                case "ElementAtOrDefault":
                    SetElementSelector(methodCallExpression, source => source.Cast<object>().FirstOrDefault());
                    break;
            }
        }

        private void TranslateFirstOrSingle(MethodCallExpression methodCallExpression)
        {
            LambdaExpression predicate = null;
            switch (methodCallExpression.Arguments.Count)
            {
                case 1:
                    break;
                case 2:
                    predicate = (LambdaExpression)StripQuote(methodCallExpression.Arguments[1]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("methodCallExpression");
            }
            CombinePredicateWithWhereClause(methodCallExpression, predicate);

            switch (methodCallExpression.Method.Name)
            {
                case "First":
                    _take = Expression.Constant(1);
                    SetElementSelector(methodCallExpression, source => source.Cast<object>().First());
                    break;
                case "FirstOrDefault":
                    _take = Expression.Constant(1);
                    SetElementSelector(methodCallExpression, source => source.Cast<object>().FirstOrDefault());
                    break;
                case "Single":
                    _take = Expression.Constant(2);
                    SetElementSelector(methodCallExpression, source => source.Cast<object>().Single());
                    break;
                case "SingleOrDefault":
                    _take = Expression.Constant(2);
                    SetElementSelector(methodCallExpression, source => source.Cast<object>().SingleOrDefault());
                    break;
            }
        }

        private void TranslateLast(MethodCallExpression methodCallExpression)
        {
            LambdaExpression predicate = null;
            switch (methodCallExpression.Arguments.Count)
            {
                case 1:
                    break;
                case 2:
                    predicate = (LambdaExpression)StripQuote(methodCallExpression.Arguments[1]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("methodCallExpression");
            }
            CombinePredicateWithWhereClause(methodCallExpression, predicate);

            // when using OrderBy without Take Last can be much faster by reversing the sort order and using First instead of Last
            if (_orderBy != null && _take == null)
            {
                for (int i = 0; i < _orderBy.Count; i++)
                {
                    var clause = _orderBy[i];
                    var oppositeDirection = (clause.Direction == OrderByDirection.Descending) ? OrderByDirection.Ascending : OrderByDirection.Descending;
                    _orderBy[i] = new OrderByClause(clause.Key, oppositeDirection);
                }
                _take = Expression.Constant(1);

                switch (methodCallExpression.Method.Name)
                {
                    case "Last":
                        SetElementSelector(methodCallExpression, source => source.Cast<object>().First());
                        break;
                    case "LastOrDefault":
                        SetElementSelector(methodCallExpression, source => source.Cast<object>().FirstOrDefault());
                        break;
                }
            }
            else
            {
                switch (methodCallExpression.Method.Name)
                {
                    case "Last":
                        SetElementSelector(methodCallExpression, source => source.Cast<object>().Last());
                        break;
                    case "LastOrDefault":
                        SetElementSelector(methodCallExpression, source => source.Cast<object>().LastOrDefault());
                        break;
                }
            }
        }

        private void TranslateMaxMin(MethodCallExpression methodCallExpression)
        {
            var methodName = methodCallExpression.Method.Name;

            if (_orderBy != null)
            {
                var message = string.Format("{0} cannot be used with OrderBy.", methodName);
                throw new NotSupportedException(message);
            }
            if (_skip != null || _take != null)
            {
                var message = string.Format("{0} cannot be used with Skip or Take.", methodName);
                throw new NotSupportedException(message);
            }

            switch (methodCallExpression.Arguments.Count)
            {
                case 1:
                    break;
                case 2:
                    if (_projection != null)
                    {
                        var message = string.Format("{0} must be used with either Select or a selector argument, but not both.", methodName);
                        throw new NotSupportedException(message);
                    }
                    _projection = (LambdaExpression)StripQuote(methodCallExpression.Arguments[1]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("methodCallExpression");
            }
            if (_projection == null)
            {
                var message = string.Format("{0} must be used with either Select or a selector argument.", methodName);
                throw new NotSupportedException(message);
            }

            // implement Max/Min by sorting on the relevant field(s) and taking the first result
            _orderBy = new List<OrderByClause>();
            if (_projection.Body.NodeType == ExpressionType.New)
            {
                // take the individual constructor arguments and make new lambdas out of them for the OrderByClauses
                var newExpression = (NewExpression)_projection.Body;
                foreach (var keyExpression in newExpression.Arguments)
                {
                    var delegateTypeGenericDefinition = typeof(Func<,>);
                    var delegateType = delegateTypeGenericDefinition.MakeGenericType(_projection.Parameters[0].Type, keyExpression.Type);
                    var keyLambda = Expression.Lambda(delegateType, keyExpression, _projection.Parameters);
                    var clause = new OrderByClause(keyLambda, (methodName == "Min") ? OrderByDirection.Ascending : OrderByDirection.Descending);
                    _orderBy.Add(clause);
                }
            }
            else
            {
                var clause = new OrderByClause(_projection, (methodName == "Min") ? OrderByDirection.Ascending : OrderByDirection.Descending);
                _orderBy.Add(clause);
            }

            _take = Expression.Constant(1);
            SetElementSelector(methodCallExpression, source => source.Cast<object>().First());
        }

        private void TranslateMethodCall(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Arguments.Count == 0)
            {
                var message = string.Format("Method call expression has no arguments: {0}.", ExpressionFormatter.ToString(methodCallExpression));
                throw new ArgumentOutOfRangeException(message);
            }

            var source = methodCallExpression.Arguments[0];
            Translate(source);

            var methodName = methodCallExpression.Method.Name;
            switch (methodName)
            {
                case "Any":
                    TranslateAny(methodCallExpression);
                    break;
                case "Count":
                case "LongCount":
                    TranslateCount(methodCallExpression);
                    break;
                case "Distinct":
                    TranslateDistinct(methodCallExpression);
                    break;
                case "ElementAt":
                case "ElementAtOrDefault":
                    TranslateElementAt(methodCallExpression);
                    break;
                case "First":
                case "FirstOrDefault":
                case "Single":
                case "SingleOrDefault":
                    TranslateFirstOrSingle(methodCallExpression);
                    break;
                case "Last":
                case "LastOrDefault":
                    TranslateLast(methodCallExpression);
                    break;
                case "Max":
                case "Min":
                    TranslateMaxMin(methodCallExpression);
                    break;
                case "OfType":
                    TranslateOfType(methodCallExpression);
                    break;
                case "OrderBy":
                case "OrderByDescending":
                    TranslateOrderBy(methodCallExpression);
                    break;
                case "Select":
                    TranslateSelect(methodCallExpression);
                    break;
                case "Skip":
                    TranslateSkip(methodCallExpression);
                    break;
                case "Take":
                    TranslateTake(methodCallExpression);
                    break;
                case "ThenBy":
                case "ThenByDescending":
                    TranslateThenBy(methodCallExpression);
                    break;
                case "Where":
                    TranslateWhere(methodCallExpression);
                    break;
                default:
                    var message = string.Format("The {0} query operator is not supported.", methodName);
                    throw new NotSupportedException(message);
            }
        }

        private void TranslateOfType(MethodCallExpression methodCallExpression)
        {
            var method = methodCallExpression.Method;
            if (method.DeclaringType != typeof(Queryable))
            {
                var message = string.Format("OfType method of class {0} is not supported.", BsonUtils.GetFriendlyTypeName(method.DeclaringType));
                throw new NotSupportedException(message);
            }
            if (!method.IsStatic)
            {
                throw new NotSupportedException("Expected OfType to be a static method.");
            }
            if (!method.IsGenericMethod)
            {
                throw new NotSupportedException("Expected OfType to be a generic method.");
            }
            var actualType = method.GetGenericArguments()[0];

            var args = methodCallExpression.Arguments.ToArray();
            if (args.Length != 1)
            {
                throw new NotSupportedException("Expected OfType method to have a single argument.");
            }
            var sourceExpression = args[0];
            if (!sourceExpression.Type.IsGenericType)
            {
                throw new NotSupportedException("Expected source argument to OfType to be a generic type.");
            }
            var nominalType = sourceExpression.Type.GetGenericArguments()[0];

            if (_projection != null)
            {
                throw new NotSupportedException("OfType after a projection is not supported.");
            }

            var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(nominalType);
            var discriminator = discriminatorConvention.GetDiscriminator(nominalType, actualType);
            if (discriminator == null)
            {
                return; // nothing to do
            }

            if (discriminator.IsBsonArray)
            {
                discriminator = discriminator.AsBsonArray[discriminator.AsBsonArray.Count - 1];
            }
            var query = Query.EQ(discriminatorConvention.ElementName, discriminator);

            var injectMethodInfo = typeof(LinqToMongo).GetMethod("Inject");
            var body = Expression.Call(injectMethodInfo, Expression.Constant(query));
            var parameter = Expression.Parameter(nominalType, "x");
            var predicate = Expression.Lambda(body, parameter);
            CombinePredicateWithWhereClause(methodCallExpression, predicate);

            _ofType = actualType;
        }

        private void TranslateOrderBy(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Arguments.Count != 2)
            {
                throw new ArgumentOutOfRangeException("methodCallExpression");
            }

            if (_orderBy != null)
            {
                throw new NotSupportedException("Only one OrderBy or OrderByDescending clause is allowed (use ThenBy or ThenByDescending for multiple order by clauses).");
            }

            var key = (LambdaExpression)StripQuote(methodCallExpression.Arguments[1]);
            var direction = (methodCallExpression.Method.Name == "OrderByDescending") ? OrderByDirection.Descending : OrderByDirection.Ascending;
            var clause = new OrderByClause(key, direction);

            _orderBy = new List<OrderByClause>();
            _orderBy.Add(clause);
        }

        private void TranslateSelect(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Arguments.Count != 2)
            {
                throw new ArgumentOutOfRangeException("methodCallExpression");
            }

            var lambdaExpression = (LambdaExpression)StripQuote(methodCallExpression.Arguments[1]);
            if (lambdaExpression.Parameters.Count == 2)
            {
                var message = "The indexed version of the Select query operator is not supported.";
                throw new NotSupportedException(message);
            }
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

        private void TranslateSkip(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Arguments.Count != 2)
            {
                throw new ArgumentOutOfRangeException("methodCallExpression");
            }

            _skip = StripQuote(methodCallExpression.Arguments[1]);
        }

        private void TranslateTake(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Arguments.Count != 2)
            {
                throw new ArgumentOutOfRangeException("methodCallExpression");
            }

            _take = StripQuote(methodCallExpression.Arguments[1]);
        }

        private void TranslateThenBy(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Arguments.Count != 2)
            {
                throw new ArgumentOutOfRangeException("methodCallExpression");
            }

            if (_orderBy == null)
            {
                throw new NotSupportedException("ThenBy or ThenByDescending can only be used after OrderBy or OrderByDescending.");
            }

            var key = (LambdaExpression)StripQuote(methodCallExpression.Arguments[1]);
            var direction = (methodCallExpression.Method.Name == "ThenByDescending") ? OrderByDirection.Descending : OrderByDirection.Ascending;
            var clause = new OrderByClause(key, direction);

            _orderBy.Add(clause);
        }

        private void TranslateWhere(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Arguments.Count != 2)
            {
                throw new ArgumentOutOfRangeException("methodCallExpression");
            }

            var predicate = (LambdaExpression)StripQuote(methodCallExpression.Arguments[1]);
            if (predicate.Parameters.Count == 2)
            {
                var message = "The indexed version of the Where query operator is not supported.";
                throw new NotSupportedException(message);
            }

            CombinePredicateWithWhereClause(methodCallExpression, predicate);
        }
    }
}
