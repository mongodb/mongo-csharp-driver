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
using System.Linq;
using System.Text;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Builders
{
    /// <summary>
    /// A builder for creating queries.
    /// </summary>
    public static class Query
    {
        // public static properties
        /// <summary>
        /// Gets a null value with a type of IMongoQuery.
        /// </summary>
        public static IMongoQuery Null
        {
            get { return null; }
        }

        // public static methods
        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList All(string name, BsonArray values)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            return new QueryConditionList(name).All(values);
        }

        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList All(string name, IEnumerable<BsonValue> values)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Contains(null))
            {
                throw new ArgumentOutOfRangeException("values", "One of the values is null.");
            }
            return new QueryConditionList(name).All(values);
        }

        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList All(string name, params BsonValue[] values)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Contains(null))
            {
                throw new ArgumentOutOfRangeException("values", "One of the values is null.");
            }
            return new QueryConditionList(name).All(values);
        }

        /// <summary>
        /// Tests that all the subqueries are true (see $and in newer versions of the server).
        /// </summary>
        /// <param name="queries">A list of subqueries.</param>
        /// <returns>A query.</returns>
        public static QueryComplete And(params IMongoQuery[] queries)
        {
            if (queries == null)
            {
                throw new ArgumentNullException("queries");
            }
            if (queries.Length == 0)
            {
                throw new ArgumentOutOfRangeException("queries", "Query.And cannot be called with zero queries.");
            }

            var queryDocument = new BsonDocument();
            foreach (var query in queries)
            {
                if (query == null)
                {
                    throw new ArgumentOutOfRangeException("queries", "One of the queries is null.");
                }
                foreach (var clause in query.ToBsonDocument())
                {
                    AddAndClause(queryDocument, clause);
                }
            }

            return new QueryComplete(queryDocument);
        }

        /// <summary>
        /// Tests that at least one item of the named array element matches a query (see $elemMatch).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="query">The query to match elements with.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList ElemMatch(string name, IMongoQuery query)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }
            return new QueryConditionList(name).ElemMatch(query);
        }

        /// <summary>
        /// Tests that the value of the named element is equal to some value.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>A query.</returns>
        public static QueryComplete EQ(string name, BsonValue value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            return new QueryComplete(new BsonDocument(name, value));
        }

        /// <summary>
        /// Tests that an element of that name does or does not exist (see $exists).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="exists">Whether to test for the existence or absence of an element.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList Exists(string name, bool exists)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            return new QueryConditionList(name).Exists(exists);
        }

        /// <summary>
        /// Tests that the value of the named element is greater than some value (see $gt).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList GT(string name, BsonValue value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            return new QueryConditionList(name).GT(value);
        }

        /// <summary>
        /// Tests that the value of the named element is greater than or equal to some value (see $gte).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList GTE(string name, BsonValue value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            return new QueryConditionList(name).GTE(value);
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList In(string name, BsonArray values)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            return new QueryConditionList(name).In(values);
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList In(string name, IEnumerable<BsonValue> values)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Contains(null))
            {
                throw new ArgumentOutOfRangeException("values", "One of the values is null.");
            }
            return new QueryConditionList(name).In(values);
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList In(string name, params BsonValue[] values)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Contains(null))
            {
                throw new ArgumentOutOfRangeException("values", "One of the values is null.");
            }
            return new QueryConditionList(name).In(values);
        }

        /// <summary>
        /// Tests that the value of the named element is less than some value (see $lt).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList LT(string name, BsonValue value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            return new QueryConditionList(name).LT(value);
        }

        /// <summary>
        /// Tests that the value of the named element is less than or equal to some value (see $lte).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList LTE(string name, BsonValue value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            return new QueryConditionList(name).LTE(value);
        }

        /// <summary>
        /// Tests that the value of the named element matches a regular expression (see $regex).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="regex">The regular expression to match against.</param>
        /// <returns>A query.</returns>
        public static QueryComplete Matches(string name, BsonRegularExpression regex)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (regex == null)
            {
                throw new ArgumentNullException("regex");
            }
            return new QueryComplete(new BsonDocument(name, regex));
        }

        /// <summary>
        /// Tests that the modulus of the value of the named element matches some value (see $mod).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="modulus">The modulus.</param>
        /// <param name="equals">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList Mod(string name, int modulus, int equals)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            return new QueryConditionList(name).Mod(modulus, equals);
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to some value (see $ne).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList NE(string name, BsonValue value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            return new QueryConditionList(name).NE(value);
        }

        /// <summary>
        /// Tests that the value of the named element is near some location (see $near).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="x">The x value of the origin.</param>
        /// <param name="y">The y value of the origin.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList Near(string name, double x, double y)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            return new QueryConditionList(name).Near(x, y);
        }

        /// <summary>
        /// Tests that the value of the named element is near some location (see $near).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="x">The x value of the origin.</param>
        /// <param name="y">The y value of the origin.</param>
        /// <param name="maxDistance">The max distance for a document to be included in the results.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList Near(string name, double x, double y, double maxDistance)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            return new QueryConditionList(name).Near(x, y, maxDistance);
        }

        /// <summary>
        /// Tests that the value of the named element is near some location (see $near and $nearSphere).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="x">The x value of the origin.</param>
        /// <param name="y">The y value of the origin.</param>
        /// <param name="maxDistance">The max distance for a document to be included in the results.</param>
        /// <param name="spherical">Whether to do a spherical search.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList Near(string name, double x, double y, double maxDistance, bool spherical)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            return new QueryConditionList(name).Near(x, y, maxDistance, spherical);
        }

        /// <summary>
        /// Tests that none of the subqueries is true (see $nor).
        /// </summary>
        /// <param name="queries">The subqueries.</param>
        /// <returns>A query.</returns>
        public static QueryComplete Nor(params IMongoQuery[] queries)
        {
            if (queries == null)
            {
                throw new ArgumentNullException("queries");
            }
            var clauses = new BsonArray();
            foreach (var query in queries)
            {
                if (query == null)
                {
                    throw new ArgumentOutOfRangeException("queries", "One of the queries is null.");
                }
                clauses.Add(query.ToBsonDocument());
            }
            var document = new BsonDocument("$nor", clauses);
            return new QueryComplete(document);
        }

        /// <summary>
        /// Tests that the value of the named element does not match any of the tests that follow (see $not).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryNot Not(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            return new QueryNot(name);
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList NotIn(string name, BsonArray values)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            return new QueryConditionList(name).NotIn(values);
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList NotIn(string name, IEnumerable<BsonValue> values)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Contains(null))
            {
                throw new ArgumentOutOfRangeException("values", "One of the values is null.");
            }
            return new QueryConditionList(name).NotIn(values);
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList NotIn(string name, params BsonValue[] values)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Contains(null))
            {
                throw new ArgumentOutOfRangeException("values", "One of the values is null.");
            }
            return new QueryConditionList(name).NotIn(values);
        }

        /// <summary>
        /// Tests that at least one of the subqueries is true (see $or).
        /// </summary>
        /// <param name="queries">The subqueries.</param>
        /// <returns>A query.</returns>
        public static QueryComplete Or(params IMongoQuery[] queries)
        {
            if (queries == null)
            {
                throw new ArgumentNullException("queries");
            }
            if (queries.Length == 0)
            {
                throw new ArgumentOutOfRangeException("queries", "Query.Or cannot be called with zero queries.");
            }

            var queryArray = new BsonArray();
            foreach (var query in queries)
            {
                if (query == null)
                {
                    throw new ArgumentOutOfRangeException("queries", "One of the queries is null.");
                }
                // flatten out nested $or
                var queryDocument = query.ToBsonDocument();
                if (queryDocument.ElementCount == 1 && queryDocument.GetElement(0).Name == "$or")
                {
                    foreach (var nestedQuery in queryDocument[0].AsBsonArray)
                    {
                        queryArray.Add(nestedQuery);
                    }
                }
                else
                {
                    queryArray.Add(queryDocument);
                }
            }

            switch (queryArray.Count)
            {
                case 1:
                    return new QueryComplete(queryArray[0].AsBsonDocument);
                default:
                    return new QueryComplete(new BsonDocument("$or", queryArray));
            }
        }

        /// <summary>
        /// Tests that the size of the named array is equal to some value (see $size).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="size">The size to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList Size(string name, int size)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            return new QueryConditionList(name).Size(size);
        }

        /// <summary>
        /// Tests that the type of the named element is equal to some type (see $type).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="type">The type to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList Type(string name, BsonType type)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            return new QueryConditionList(name).Type(type);
        }

        /// <summary>
        /// Tests that a JavaScript expression is true (see $where).
        /// </summary>
        /// <param name="javaScript">The where clause.</param>
        /// <returns>A query.</returns>
        public static QueryComplete Where(BsonJavaScript javaScript)
        {
            if (javaScript == null)
            {
                throw new ArgumentNullException("javaScript");
            }
            return new QueryComplete(new BsonDocument("$where", javaScript));
        }

        /// <summary>
        /// Tests that the value of the named element is within a circle (see $within and $center).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="centerX">The x coordinate of the origin.</param>
        /// <param name="centerY">The y coordinate of the origin.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList WithinCircle(string name, double centerX, double centerY, double radius)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            return new QueryConditionList(name).WithinCircle(centerX, centerY, radius);
        }

        /// <summary>
        /// Tests that the value of the named element is within a circle (see $within and $center/$centerSphere).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="centerX">The x coordinate of the origin.</param>
        /// <param name="centerY">The y coordinate of the origin.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="spherical">Whether to do a spherical search.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList WithinCircle(
            string name,
            double centerX,
            double centerY,
            double radius,
            bool spherical)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            return new QueryConditionList(name).WithinCircle(centerX, centerY, radius, spherical);
        }

        /// <summary>
        /// Tests that the value of the named element is within a polygon (see $within and $polygon).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="points">An array of points that defines the polygon (the second dimension must be of length 2).</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList WithinPolygon(string name, double[,] points)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (points == null)
            {
                throw new ArgumentNullException("points");
            }
            return new QueryConditionList(name).WithinPolygon(points);
        }

        /// <summary>
        /// Tests that the value of the named element is within a rectangle (see $within and $box).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="lowerLeftX">The x coordinate of the lower left corner.</param>
        /// <param name="lowerLeftY">The y coordinate of the lower left corner.</param>
        /// <param name="upperRightX">The x coordinate of the upper right corner.</param>
        /// <param name="upperRightY">The y coordinate of the upper right corner.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList WithinRectangle(
            string name,
            double lowerLeftX,
            double lowerLeftY,
            double upperRightX,
            double upperRightY)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            return new QueryConditionList(name).WithinRectangle(lowerLeftX, lowerLeftY, upperRightX, upperRightY);
        }

        // private static methods
        // try to keey the query in simple form and only promote to $and form if necessary
        private static void AddAndClause(BsonDocument query, BsonElement clause)
        {
            // flatten out nested $and
            if (clause.Name == "$and")
            {
                foreach (var item in clause.Value.AsBsonArray)
                {
                    foreach (var element in item.AsBsonDocument.Elements)
                    {
                        AddAndClause(query, element);
                    }
                }
                return;
            }

            if (query.ElementCount == 1 && query.GetElement(0).Name == "$and")
            {
                query[0].AsBsonArray.Add(new BsonDocument(clause));
            }
            else
            {
                if (clause.Name == "$and")
                {
                    PromoteQueryToDollarAndForm(query, clause);
                }
                else
                {
                    if (query.Contains(clause.Name))
                    {
                        var existingClause = query.GetElement(clause.Name);
                        if (existingClause.Value.IsBsonDocument && clause.Value.IsBsonDocument)
                        {
                            var clauseValue = clause.Value.AsBsonDocument;
                            var existingClauseValue = existingClause.Value.AsBsonDocument;
                            if (clauseValue.Names.Any(op => existingClauseValue.Contains(op)))
                            {
                                PromoteQueryToDollarAndForm(query, clause);
                            }
                            else
                            {
                                foreach (var element in clauseValue)
                                {
                                    existingClauseValue.Add(element);
                                }
                            }
                        }
                        else
                        {
                            PromoteQueryToDollarAndForm(query, clause);
                        }
                    }
                    else
                    {
                        query.Add(clause);
                    }
                }
            }
        }

        private static void PromoteQueryToDollarAndForm(BsonDocument query, BsonElement clause)
        {
            var clauses = new BsonArray();
            foreach (var queryElement in query)
            {
                clauses.Add(new BsonDocument(queryElement));
            }
            clauses.Add(new BsonDocument(clause));
            query.Clear();
            query.Add("$and", clauses);
        }
    }

    /// <summary>
    /// A builder for creating queries.
    /// </summary>
    [Serializable]
    public abstract class QueryBuilder : BuilderBase
    {
        // private fields
        /// <summary>
        /// A BSON document containing the query being built.
        /// </summary>
        private BsonDocument _document;

        // constructors
        /// <summary>
        /// Initializes a new instance of the QueryBuilder class.
        /// </summary>
        /// <param name="document">A document representing the query.</param>
        protected QueryBuilder(BsonDocument document)
        {
            _document = document;
        }

        // protected properties
        /// <summary>
        /// Gets the document containing the query being built.
        /// </summary>
        protected BsonDocument Document
        {
            get { return _document; }
        }

        // public methods
        /// <summary>
        /// Returns the result of the builder as a BsonDocument.
        /// </summary>
        /// <returns>A BsonDocument.</returns>
        public override BsonDocument ToBsonDocument()
        {
            return _document;
        }

        // protected methods
        /// <summary>
        /// Serializes the result of the builder to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The writer.</param>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="options">The serialization options.</param>
        protected override void Serialize(BsonWriter bsonWriter, Type nominalType, IBsonSerializationOptions options)
        {
            _document.Serialize(bsonWriter, nominalType, options);
        }
    }

    /// <summary>
    /// A builder for creating queries.
    /// </summary>
    [Serializable]
    public class QueryComplete : QueryBuilder, IMongoQuery
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the QueryComplete class.
        /// </summary>
        /// <param name="document">A document representing the query.</param>
        public QueryComplete(BsonDocument document)
            : base(document)
        {
        }
    }

    /// <summary>
    /// A builder for creating queries.
    /// </summary>
    [Serializable]
    public class QueryConditionList : QueryComplete
    {
        // private fields
        private BsonDocument _conditions;

        // constructors
        /// <summary>
        /// Initializes a new instance of the QueryConditionList class.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        public QueryConditionList(string name)
            : base(new BsonDocument(name, new BsonDocument()))
        {
            _conditions = Document[0].AsBsonDocument;
        }

        // public methods
        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList All(BsonArray values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            _conditions.Add("$all", values);
            return this;
        }

        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList All(IEnumerable<BsonValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Contains(null))
            {
                throw new ArgumentOutOfRangeException("values", "One of the values is null.");
            }
            _conditions.Add("$all", new BsonArray(values));
            return this;
        }

        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList All(params BsonValue[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Contains(null))
            {
                throw new ArgumentOutOfRangeException("values", "One of the values is null.");
            }
            _conditions.Add("$all", new BsonArray(values));
            return this;
        }

        /// <summary>
        /// Tests that at least one item of the named array element matches a query (see $elemMatch).
        /// </summary>
        /// <param name="query">The query to match elements with.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList ElemMatch(IMongoQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }
            _conditions.Add("$elemMatch", query.ToBsonDocument());
            return this;
        }

        /// <summary>
        /// Tests that an element of that name does or does not exist (see $exists).
        /// </summary>
        /// <param name="exists">Whether to test for the existence or absence of an element.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList Exists(bool exists)
        {
            _conditions.Add("$exists", BsonBoolean.Create(exists));
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is greater than some value (see $gt).
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList GT(BsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            _conditions.Add("$gt", value);
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is greater than or equal to some value (see $gte).
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList GTE(BsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            _conditions.Add("$gte", value);
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList In(BsonArray values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            _conditions.Add("$in", values);
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList In(IEnumerable<BsonValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Contains(null))
            {
                throw new ArgumentOutOfRangeException("values", "One of the values is null.");
            }
            _conditions.Add("$in", new BsonArray(values));
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList In(params BsonValue[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Contains(null))
            {
                throw new ArgumentOutOfRangeException("values", "One of the values is null.");
            }
            _conditions.Add("$in", new BsonArray(values));
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is less than some value (see $lt).
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList LT(BsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            _conditions.Add("$lt", value);
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is less than or equal to some value (see $lte).
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList LTE(BsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            _conditions.Add("$lte", value);
            return this;
        }

        /// <summary>
        /// Tests that the modulus of the value of the named element matches some value (see $mod).
        /// </summary>
        /// <param name="modulus">The modulus.</param>
        /// <param name="equals">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList Mod(int modulus, int equals)
        {
            _conditions.Add("$mod", new BsonArray { modulus, equals });
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to some value (see $ne).
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList NE(BsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            _conditions.Add("$ne", value);
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is near some location (see $near).
        /// </summary>
        /// <param name="x">The x value of the origin.</param>
        /// <param name="y">The y value of the origin.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList Near(double x, double y)
        {
            return Near(x, y, double.MaxValue);
        }

        /// <summary>
        /// Tests that the value of the named element is near some location (see $near).
        /// </summary>
        /// <param name="x">The x value of the origin.</param>
        /// <param name="y">The y value of the origin.</param>
        /// <param name="maxDistance">The max distance for a document to be included in the results.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList Near(double x, double y, double maxDistance)
        {
            return Near(x, y, maxDistance, false); // not spherical
        }

        /// <summary>
        /// Tests that the value of the named element is near some location (see $near and $nearSphere).
        /// </summary>
        /// <param name="x">The x value of the origin.</param>
        /// <param name="y">The y value of the origin.</param>
        /// <param name="maxDistance">The max distance for a document to be included in the results.</param>
        /// <param name="spherical">Whether to do a spherical search.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList Near(double x, double y, double maxDistance, bool spherical)
        {
            var op = spherical ? "$nearSphere" : "$near";
            _conditions.Add(op, new BsonArray { x, y });
            if (maxDistance != double.MaxValue)
            {
                _conditions.Add("$maxDistance", maxDistance);
            }
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList NotIn(BsonArray values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            _conditions.Add("$nin", values);
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList NotIn(IEnumerable<BsonValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Contains(null))
            {
                throw new ArgumentOutOfRangeException("values", "One of the values is null.");
            }
            _conditions.Add("$nin", new BsonArray(values));
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList NotIn(params BsonValue[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Contains(null))
            {
                throw new ArgumentOutOfRangeException("values", "One of the values is null.");
            }
            _conditions.Add("$nin", new BsonArray(values));
            return this;
        }

        /// <summary>
        /// Tests that the size of the named array is equal to some value (see $size).
        /// </summary>
        /// <param name="size">The size of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList Size(int size)
        {
            _conditions.Add("$size", size);
            return this;
        }

        /// <summary>
        /// Tests that the type of the named element is equal to some type (see $type).
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList Type(BsonType type)
        {
            _conditions.Add("$type", (int)type);
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is within a circle (see $within and $center).
        /// </summary>
        /// <param name="x">The x coordinate of the origin.</param>
        /// <param name="y">The y coordinate of the origin.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList WithinCircle(double x, double y, double radius)
        {
            return WithinCircle(x, y, radius, false); // not spherical
        }

        /// <summary>
        /// Tests that the value of the named element is within a circle (see $within and $center/$centerSphere).
        /// </summary>
        /// <param name="x">The x coordinate of the origin.</param>
        /// <param name="y">The y coordinate of the origin.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="spherical">Whether to do a spherical search.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList WithinCircle(double x, double y, double radius, bool spherical)
        {
            var shape = spherical ? "$centerSphere" : "$center";
            _conditions.Add("$within", new BsonDocument(shape, new BsonArray { new BsonArray { x, y }, radius }));
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is within a polygon (see $within and $polygon).
        /// </summary>
        /// <param name="points">An array of points that defines the polygon (the second dimension must be of length 2).</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList WithinPolygon(double[,] points)
        {
            if (points == null)
            {
                throw new ArgumentNullException("points");
            }
            if (points.GetLength(1) != 2)
            {
                var message = string.Format("The second dimension of the points array must be of length 2, not {0}.", points.GetLength(1));
                throw new ArgumentOutOfRangeException("points", message);
            }

            var arrayOfPoints = new BsonArray(points.GetLength(0));
            for (var i = 0; i < points.GetLength(0); i++)
            {
                arrayOfPoints.Add(new BsonArray(2) { points[i, 0], points[i, 1] });
            }
            _conditions.Add("$within", new BsonDocument("$polygon", arrayOfPoints));
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is within a rectangle (see $within and $box).
        /// </summary>
        /// <param name="lowerLeftX">The x coordinate of the lower left corner.</param>
        /// <param name="lowerLeftY">The y coordinate of the lower left corner.</param>
        /// <param name="upperRightX">The x coordinate of the upper right corner.</param>
        /// <param name="upperRightY">The y coordinate of the upper right corner.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList WithinRectangle(
            double lowerLeftX,
            double lowerLeftY,
            double upperRightX,
            double upperRightY)
        {
            _conditions.Add("$within", new BsonDocument("$box", new BsonArray { new BsonArray { lowerLeftX, lowerLeftY }, new BsonArray { upperRightX, upperRightY } }));
            return this;
        }
    }

    /// <summary>
    /// A builder for creating queries.
    /// </summary>
    public class QueryNot
    {
        // private fields
        private string _name;

        // constructors
        /// <summary>
        /// Initializes a new instance of the QueryNot class.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        public QueryNot(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            _name = name;
        }

        // public methods
        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList All(BsonArray values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            return new QueryNotConditionList(_name).All(values);
        }

        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList All(IEnumerable<BsonValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Contains(null))
            {
                throw new ArgumentOutOfRangeException("values", "One of the values is null.");
            }
            return new QueryNotConditionList(_name).All(values);
        }

        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList All(params BsonValue[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Contains(null))
            {
                throw new ArgumentOutOfRangeException("values", "One of the values is null.");
            }
            return new QueryNotConditionList(_name).All(values);
        }

        /// <summary>
        /// Tests that at least one item of the named array element matches a query (see $elemMatch).
        /// </summary>
        /// <param name="query">The query to match elements with.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList ElemMatch(IMongoQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }
            return new QueryNotConditionList(_name).ElemMatch(query);
        }

        /// <summary>
        /// Tests that an element of that name does or does not exist (see $exists).
        /// </summary>
        /// <param name="exists">Whether to test for the existence or absence of an element.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList Exists(bool exists)
        {
            return new QueryNotConditionList(_name).Exists(exists);
        }

        /// <summary>
        /// Tests that the value of the named element is greater than some value (see $gt).
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList GT(BsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            return new QueryNotConditionList(_name).GT(value);
        }

        /// <summary>
        /// Tests that the value of the named element is greater than or equal to some value (see $gte).
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList GTE(BsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            return new QueryNotConditionList(_name).GTE(value);
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList In(BsonArray values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            return new QueryNotConditionList(_name).In(values);
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList In(IEnumerable<BsonValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Contains(null))
            {
                throw new ArgumentOutOfRangeException("values", "One of the values is null.");
            }
            return new QueryNotConditionList(_name).In(values);
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList In(params BsonValue[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Contains(null))
            {
                throw new ArgumentOutOfRangeException("values", "One of the values is null.");
            }
            return new QueryNotConditionList(_name).In(values);
        }

        /// <summary>
        /// Tests that the value of the named element is less than some value (see $lt).
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList LT(BsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            return new QueryNotConditionList(_name).LT(value);
        }

        /// <summary>
        /// Tests that the value of the named element is less than or equal to some value (see $lte).
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList LTE(BsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            return new QueryNotConditionList(_name).LTE(value);
        }

        /// <summary>
        /// Tests that the modulus of the value of the named element matches some value (see $mod).
        /// </summary>
        /// <param name="modulus">The modulus.</param>
        /// <param name="equals">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList Mod(int modulus, int equals)
        {
            return new QueryNotConditionList(_name).Mod(modulus, equals);
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to some value (see $ne).
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList NE(BsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            return new QueryNotConditionList(_name).NE(value);
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList NotIn(BsonArray values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            return new QueryNotConditionList(_name).NotIn(values);
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList NotIn(IEnumerable<BsonValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Contains(null))
            {
                throw new ArgumentOutOfRangeException("values", "One of the values is null.");
            }
            return new QueryNotConditionList(_name).NotIn(values);
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList NotIn(params BsonValue[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Contains(null))
            {
                throw new ArgumentOutOfRangeException("values", "One of the values is null.");
            }
            return new QueryNotConditionList(_name).NotIn(values);
        }

        /// <summary>
        /// Tests that the value of the named element matches a regular expression (see $regex).
        /// </summary>
        /// <param name="regex">The regular expression to match against.</param>
        /// <returns>A query.</returns>
        public QueryComplete Matches(BsonRegularExpression regex)
        {
            if (regex == null)
            {
                throw new ArgumentNullException("regex");
            }
            return new QueryComplete(new BsonDocument(_name, new BsonDocument("$not", regex)));
        }

        /// <summary>
        /// Tests that the size of the named array is equal to some value (see $size).
        /// </summary>
        /// <param name="size">The size of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList Size(int size)
        {
            return new QueryNotConditionList(_name).Size(size);
        }

        /// <summary>
        /// Tests that the type of the named element is equal to some type (see $type).
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList Type(BsonType type)
        {
            return new QueryNotConditionList(_name).Type(type);
        }
    }

    /// <summary>
    /// A builder for creating queries.
    /// </summary>
    [Serializable]
    public class QueryNotConditionList : QueryComplete
    {
        // private fields
        private BsonDocument _conditions;

        // constructors
        /// <summary>
        /// Initializes a new instance of the QueryNotConditionList.
        /// </summary>
        /// <param name="name">The name of the first element to test.</param>
        public QueryNotConditionList(string name)
            : base(new BsonDocument(name, new BsonDocument("$not", new BsonDocument())))
        {
            _conditions = Document[0].AsBsonDocument[0].AsBsonDocument;
        }

        // public methods
        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList All(BsonArray values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            _conditions.Add("$all", values);
            return this;
        }

        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList All(IEnumerable<BsonValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Contains(null))
            {
                throw new ArgumentOutOfRangeException("values", "One of the values is null.");
            }
            _conditions.Add("$all", new BsonArray(values));
            return this;
        }

        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList All(params BsonValue[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Contains(null))
            {
                throw new ArgumentOutOfRangeException("values", "One of the values is null.");
            }
            _conditions.Add("$all", new BsonArray(values));
            return this;
        }

        /// <summary>
        /// Tests that at least one item of the named array element matches a query (see $elemMatch).
        /// </summary>
        /// <param name="query">The query to match elements with.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList ElemMatch(IMongoQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }
            _conditions.Add("$elemMatch", query.ToBsonDocument());
            return this;
        }

        /// <summary>
        /// Tests that an element of that name does or does not exist (see $exists).
        /// </summary>
        /// <param name="exists">Whether to test for the existence or absence of an element.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList Exists(bool exists)
        {
            _conditions.Add("$exists", BsonBoolean.Create(exists));
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is greater than some value (see $gt).
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList GT(BsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            _conditions.Add("$gt", value);
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is greater than or equal to some value (see $gte).
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList GTE(BsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            _conditions.Add("$gte", value);
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList In(BsonArray values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            _conditions.Add("$in", values);
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList In(IEnumerable<BsonValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Contains(null))
            {
                throw new ArgumentOutOfRangeException("values", "One of the values is null.");
            }
            _conditions.Add("$in", new BsonArray(values));
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList In(params BsonValue[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Contains(null))
            {
                throw new ArgumentOutOfRangeException("values", "One of the values is null.");
            }
            _conditions.Add("$in", new BsonArray(values));
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is less than some value (see $lt).
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList LT(BsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            _conditions.Add("$lt", value);
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is less than or equal to some value (see $lte).
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList LTE(BsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            _conditions.Add("$lte", value);
            return this;
        }

        /// <summary>
        /// Tests that the modulus of the value of the named element matches some value (see $mod).
        /// </summary>
        /// <param name="modulus">The modulus.</param>
        /// <param name="equals">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList Mod(int modulus, int equals)
        {
            _conditions.Add("$mod", new BsonArray { modulus, equals });
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to some value ($ne).
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList NE(BsonValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            _conditions.Add("$ne", value);
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList NotIn(BsonArray values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            _conditions.Add("$nin", values);
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList NotIn(IEnumerable<BsonValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Contains(null))
            {
                throw new ArgumentOutOfRangeException("values", "One of the values is null.");
            }
            _conditions.Add("$nin", new BsonArray(values));
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList NotIn(params BsonValue[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Contains(null))
            {
                throw new ArgumentOutOfRangeException("values", "One of the values is null.");
            }
            _conditions.Add("$nin", new BsonArray(values));
            return this;
        }

        /// <summary>
        /// Tests that the size of the named array is equal to some value (see $size).
        /// </summary>
        /// <param name="size">The size of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList Size(int size)
        {
            _conditions.Add("$size", size);
            return this;
        }

        /// <summary>
        /// Tests that the type of the named element is equal to some type (see $type).
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList Type(BsonType type)
        {
            _conditions.Add("$type", (int)type);
            return this;
        }
    }
}
