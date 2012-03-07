﻿/* Copyright 2010-2012 10gen Inc.
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
using System.Threading;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Wrappers;

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
        private Func<IEnumerable, object> _elementSelector; // used for First, Last, etc...

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
            var cursor = Collection.FindAs(DocumentType, query);

            if (_orderBy != null)
            {
                var sortBy = new SortByDocument();
                foreach (var clause in _orderBy)
                {
                    var memberExpression = (MemberExpression)clause.Key.Body;
                    var serializationInfo = GetSerializationInfo(memberExpression);
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

            IEnumerable enumerable;
            if (_projection == null)
            {
                enumerable = cursor;
            }
            else
            {
                var lambdaType = _projection.GetType();
                var delegateType = lambdaType.GetGenericArguments()[0];
                var sourceType = delegateType.GetGenericArguments()[0];
                var resultType = delegateType.GetGenericArguments()[1];
                var projectorType = typeof(Projector<,>).MakeGenericType(sourceType, resultType);
                var projection = _projection.Compile();
                var projector = Activator.CreateInstance(projectorType, cursor, projection);
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
            var methodCallExpression = expression as MethodCallExpression;
            if (methodCallExpression == null)
            {
                throw new ArgumentOutOfRangeException("expression");
            }

            if (methodCallExpression.Arguments.Count == 0)
            {
                throw new ArgumentOutOfRangeException("expression");
            }

            var source = methodCallExpression.Arguments[0];
            if (source is MethodCallExpression)
            {
                Translate(source);
            }
            
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
                    throw new InvalidOperationException(message);
            }
        }

        // private methods
        private IMongoQuery BuildAndAlsoQuery(BinaryExpression binaryExpression)
        {
            return Query.And(BuildQuery(binaryExpression.Left), BuildQuery(binaryExpression.Right));
        }

        private IMongoQuery BuildArrayLengthQuery(BinaryExpression binaryExpression)
        {
            var leftUnaryExpression = binaryExpression.Left as UnaryExpression;
            if (leftUnaryExpression != null)
            {
                if (leftUnaryExpression.NodeType == ExpressionType.ArrayLength)
                {
                    var memberExpression = leftUnaryExpression.Operand as MemberExpression;
                    var valueExpression = binaryExpression.Right as ConstantExpression;
                    if (memberExpression != null && valueExpression != null)
                    {
                        var serializationInfo = GetSerializationInfo(memberExpression);
                        var value = (int)valueExpression.Value;
                        if (binaryExpression.NodeType == ExpressionType.Equal)
                        {
                            return Query.Size(serializationInfo.ElementName, value);
                        }
                        else
                        {
                            return Query.Not(serializationInfo.ElementName).Size(value);
                        }
                    }
                }
            }

            var leftMemberExpression = binaryExpression.Left as MemberExpression;
            if (leftMemberExpression != null)
            {
                if (leftMemberExpression.Member.Name == "Count")
                {
                    var memberExpression = leftMemberExpression.Expression as MemberExpression;
                    var valueExpression = binaryExpression.Right as ConstantExpression;
                    if (memberExpression != null && valueExpression != null)
                    {
                        var serializationInfo = GetSerializationInfo(memberExpression);
                        var value = (int)valueExpression.Value;
                        if (binaryExpression.NodeType == ExpressionType.Equal)
                        {
                            return Query.Size(serializationInfo.ElementName, value);
                        }
                        else
                        {
                            return Query.Not(serializationInfo.ElementName).Size(value);
                        }
                    }
                }
            }

            var leftMethodCallExpression = binaryExpression.Left as MethodCallExpression;
            if (leftMethodCallExpression != null)
            {
                if (leftMethodCallExpression.Method.Name == "Count")
                {
                    var arguments = leftMethodCallExpression.Arguments.ToArray();
                    if (arguments.Length == 1)
                    {
                        var memberExpression = leftMethodCallExpression.Arguments[0] as MemberExpression;
                        var valueExpression = binaryExpression.Right as ConstantExpression;
                        if (memberExpression != null && valueExpression != null)
                        {
                            var serializationInfo = GetSerializationInfo(memberExpression);
                            var value = (int)valueExpression.Value;
                            if (binaryExpression.NodeType == ExpressionType.Equal)
                            {
                                return Query.Size(serializationInfo.ElementName, value);
                            }
                            else
                            {
                                return Query.Not(serializationInfo.ElementName).Size(value);
                            }
                        }
                    }
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
            if (binaryExpression.NodeType == ExpressionType.Equal || binaryExpression.NodeType == ExpressionType.NotEqual)
            {
                var query = BuildArrayLengthQuery(binaryExpression);
                if (query != null)
                {
                    return query;
                }

                query = BuildModQuery(binaryExpression);
                if (query != null)
                {
                    return query;
                }
            }

            var valueExpression = binaryExpression.Right as ConstantExpression;
            if (valueExpression != null)
            {
                var unaryExpression = binaryExpression.Left as UnaryExpression;
                if (unaryExpression != null && unaryExpression.NodeType == ExpressionType.Convert && unaryExpression.Operand.Type.IsEnum)
                {
                    var enumType = unaryExpression.Operand.Type;
                    if (unaryExpression.Type == Enum.GetUnderlyingType(enumType))
                    {
                        var enumSerializationInfo = GetSerializationInfo(unaryExpression.Operand);
                        if (enumSerializationInfo != null)
                        {
                            var numericValue = valueExpression.Value;
                            var enumValue = Enum.ToObject(enumType, numericValue);
                            var serializedValue = SerializeValue(enumSerializationInfo, enumValue);
                            switch (binaryExpression.NodeType)
                            {
                                case ExpressionType.Equal: return Query.EQ(enumSerializationInfo.ElementName, serializedValue);
                                case ExpressionType.GreaterThan: return Query.GT(enumSerializationInfo.ElementName, serializedValue);
                                case ExpressionType.GreaterThanOrEqual: return Query.GTE(enumSerializationInfo.ElementName, serializedValue);
                                case ExpressionType.LessThan: return Query.LT(enumSerializationInfo.ElementName, serializedValue);
                                case ExpressionType.LessThanOrEqual: return Query.LTE(enumSerializationInfo.ElementName, serializedValue);
                                case ExpressionType.NotEqual: return Query.NE(enumSerializationInfo.ElementName, serializedValue);
                            }
                        }
                    }

                    return null;
                } 
                
                var serializationInfo = GetSerializationInfo(binaryExpression.Left);
                if (serializationInfo != null)
                {
                    var serializedValue = SerializeValue(serializationInfo, valueExpression.Value);
                    switch (binaryExpression.NodeType)
                    {
                        case ExpressionType.Equal: return Query.EQ(serializationInfo.ElementName, serializedValue);
                        case ExpressionType.GreaterThan: return Query.GT(serializationInfo.ElementName, serializedValue);
                        case ExpressionType.GreaterThanOrEqual: return Query.GTE(serializationInfo.ElementName, serializedValue);
                        case ExpressionType.LessThan: return Query.LT(serializationInfo.ElementName, serializedValue);
                        case ExpressionType.LessThanOrEqual: return Query.LTE(serializationInfo.ElementName, serializedValue);
                        case ExpressionType.NotEqual: return Query.NE(serializationInfo.ElementName, serializedValue);
                    }
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

        private IMongoQuery BuildMethodCallQuery(MethodCallExpression methodCallExpression)
        {
            switch (methodCallExpression.Method.Name)
            {
                case "Contains": return BuildContainsQuery(methodCallExpression);
                case "ContainsAll": return BuildContainsAllQuery(methodCallExpression);
                case "ContainsAny": return BuildContainsAnyQuery(methodCallExpression);
                case "EndsWith": return BuildStringQuery(methodCallExpression);
                case "In": return BuildInQuery(methodCallExpression);
                case "Inject": return BuildInjectQuery(methodCallExpression);
                case "IsMatch": return BuildIsMatchQuery(methodCallExpression);
                case "StartsWith": return BuildStringQuery(methodCallExpression);
            }
            return null;
        }

        private IMongoQuery BuildModQuery(BinaryExpression binaryExpression)
        {
            var leftBinaryExpression = binaryExpression.Left as BinaryExpression;
            if (leftBinaryExpression != null && leftBinaryExpression.NodeType == ExpressionType.Modulo)
            {
                var serializationInfo = GetSerializationInfo(leftBinaryExpression.Left);
                var modulusExpression = leftBinaryExpression.Right as ConstantExpression;
                var equalsExpression = binaryExpression.Right as ConstantExpression;
                if (serializationInfo != null && modulusExpression != null && equalsExpression != null)
                {
                    var modulus = Convert.ToInt32(modulusExpression.Value);
                    var equals = Convert.ToInt32(equalsExpression.Value);
                    if (binaryExpression.NodeType == ExpressionType.Equal)
                    {
                        return Query.Mod(serializationInfo.ElementName, modulus, equals);
                    }
                    else
                    {
                        return Query.Not(serializationInfo.ElementName).Mod(modulus, equals);
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
                if (elementName == "$or")
                {
                    var clauses = queryDocument[0].AsBsonArray;
                    return new QueryDocument("$nor", clauses);
                }

                var operatorDocument = queryDocument[0] as BsonDocument;
                if (operatorDocument != null && operatorDocument.ElementCount == 1)
                {
                    var operatorName = operatorDocument.GetElement(0).Name;
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
                        case "$lt":
                        case "$lte":
                        case "$ne":
                        case "$gt":
                        case "$gte":
                            string oppositeOperator;
                            switch (operatorName)
                            {
                                case "$lt": oppositeOperator = "$gte"; break;
                                case "$lte": oppositeOperator = "$gt"; break;
                                case "$ne": oppositeOperator = "$eq"; break;
                                case "$gt": oppositeOperator = "$lte"; break;
                                case "$gte": oppositeOperator = "$lt"; break;
                                default: throw new InvalidOperationException("Unreachable code.");
                            }
                            var comparisonValue = operatorDocument[0];
                            if (oppositeOperator == "$eq")
                            {
                                return new QueryDocument(elementName, comparisonValue);
                            }
                            else
                            {
                                return new QueryDocument(elementName, new BsonDocument(oppositeOperator, comparisonValue));
                            }
                    }

                    // use $not as a meta operator
                    if (operatorName[0] == '$')
                    {
                        return new QueryDocument(elementName, new BsonDocument("$not", operatorDocument));
                    }
                }

                var operatorValue = queryDocument[0];
                if (operatorValue.IsBsonRegularExpression)
                {
                    return new QueryDocument(elementName, new BsonDocument("$not", operatorValue));
                }

                if (operatorValue.IsBoolean)
                {
                    // turn implied boolean test into test against opposite boolean value
                    var oppositeValue = !operatorValue.AsBoolean;
                    return new QueryDocument(elementName, oppositeValue);
                }
                else
                {
                    // turn implied equality comparison into $ne
                    return new QueryDocument(elementName, new BsonDocument("$ne", operatorValue));
                }
            }

            // $not only works as a meta operator so simulate $not using $nor
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
            }

            if (query == null)
            {
                var message = string.Format("Unsupported where clause: {0}.", ExpressionFormatter.ToString(expression));
                throw new ArgumentException(message);
            }

            return query;
        }

        private IMongoQuery BuildStringQuery(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.DeclaringType == typeof(string))
            {
                switch (methodCallExpression.Method.Name)
                {
                    case "Contains":
                    case "EndsWith":
                    case "StartsWith":
                        var arguments = methodCallExpression.Arguments.ToArray();
                        if (arguments.Length == 1)
                        {
                            var serializationInfo = GetSerializationInfo(methodCallExpression.Object);
                            var valueExpression = arguments[0] as ConstantExpression;
                            if (serializationInfo != null && valueExpression != null)
                            {
                                var s = (string)valueExpression.Value;
                                BsonRegularExpression regex;
                                switch (methodCallExpression.Method.Name)
                                {
                                    case "Contains": regex = new BsonRegularExpression(s); break;
                                    case "EndsWith": regex = new BsonRegularExpression(s + "$"); break;
                                    case "StartsWith": regex = new BsonRegularExpression("^" + s); break;
                                    default: throw new InvalidOperationException("Unreachable code");
                                }
                                return Query.Matches(serializationInfo.ElementName, regex);
                            }
                        }
                        break;
                }
            }
            return null;
        }

        private void CombinePredicateWithWhereClause(MethodCallExpression methodCallExpression, LambdaExpression predicate)
        {
            if (predicate != null)
            {
                if (_projection != null)
                {
                    var message = string.Format("{0} with predicate after a projection is not supported.", methodCallExpression.Method.Name);
                    throw new InvalidOperationException(message);
                }

                if (_where == null)
                {
                    _where = predicate;
                    return;
                }

                var whereBody = _where.Body;
                var predicateBody = ExpressionParameterReplacer.ReplaceParameter(predicate.Body, predicate.Parameters[0], _where.Parameters[0]);
                var combinedBody = Expression.AndAlso(whereBody, predicateBody);
                _where = Expression.Lambda(combinedBody, _where.Parameters.ToArray());
            }
        }

        private BsonSerializationInfo GetSerializationInfo(Expression expression)
        {
            var documentSerializer = BsonSerializer.LookupSerializer(DocumentType);
            return GetSerializationInfo(documentSerializer, expression);
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

        private BsonSerializationInfo GetSerializationInfoMember(IBsonSerializer serializer, MemberExpression memberExpression)
        {
            var declaringType = memberExpression.Expression.Type;
            var memberName = memberExpression.Member.Name;

            var containingExpression = memberExpression.Expression;
            if (containingExpression.Type == DocumentType)
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
                throw new InvalidOperationException(message);
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

            return (int) constantExpression.Value;
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

        private void TranslateOrderBy(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Arguments.Count != 2)
            {
                throw new ArgumentOutOfRangeException("methodCallExpression");
            }

            if (_orderBy != null)
            {
                throw new InvalidOperationException("Only one OrderBy or OrderByDescending clause is allowed (use ThenBy or ThenByDescending for multiple order by clauses).");
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
                throw new InvalidOperationException(message);
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
                throw new InvalidOperationException("ThenBy or ThenByDescending can only be used after OrderBy or OrderByDescending.");
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
                throw new InvalidOperationException(message);
            }

            CombinePredicateWithWhereClause(methodCallExpression, predicate);
        }
    }
}
