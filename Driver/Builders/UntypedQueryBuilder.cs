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
using System.Text.RegularExpressions;

using MongoDB.Bson;

namespace MongoDB.Driver.Builders
{
    internal class UntypedQueryBuilder
    {
        // public methods
        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery All(string name, IEnumerable<BsonValue> values)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            var condition = new BsonDocument("$all", new BsonArray(values));
            return new QueryDocument(name, condition);
        }

        /// <summary>
        /// Tests that all the queries are true (see $and in newer versions of the server).
        /// </summary>
        /// <param name="queries">A list of subqueries.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery And(IEnumerable<IMongoQuery> queries)
        {
            if (queries == null)
            {
                throw new ArgumentNullException("queries");
            }
            if (!queries.Any())
            {
                throw new ArgumentOutOfRangeException("queries", "And cannot be called with zero queries.");
            }

            var queryDocument = new QueryDocument();
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

            return queryDocument;
        }

        /// <summary>
        /// Tests that all the queries are true (see $and in newer versions of the server).
        /// </summary>
        /// <param name="queries">A list of subqueries.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery And(params IMongoQuery[] queries)
        {
            return And((IEnumerable<IMongoQuery>)queries);
        }

        /// <summary>
        /// Tests that at least one item of the named array element matches a query (see $elemMatch).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="query">The query to match elements with.</param>
        /// <returns>
        /// An IMongoQuery.
        /// </returns>
        public IMongoQuery ElemMatch(string name, IMongoQuery query)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var condition = new BsonDocument("$elemMatch", query.ToBsonDocument());
            return new QueryDocument(name, condition);
        }

        /// <summary>
        /// Tests that the value of the named element is equal to some value.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>
        /// An IMongoQuery.
        /// </returns>
        public IMongoQuery EQ(string name, BsonValue value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            return new QueryDocument(name, value);
        }

        /// <summary>
        /// Tests that an element of that name exists (see $exists).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <returns>
        /// An IMongoQuery.
        /// </returns>
        public IMongoQuery Exists(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            return new QueryDocument(name, new BsonDocument("$exists", true));
        }

        /// <summary>
        /// Tests that the value of the named element is greater than some value (see $gt).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>
        /// An IMongoQuery.
        /// </returns>
        public IMongoQuery GT(string name, BsonValue value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
                
            return new QueryDocument(name, new BsonDocument("$gt", value));
        }

        /// <summary>
        /// Tests that the value of the named element is greater than or equal to some value (see $gte).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>
        /// An IMongoQuery.
        /// </returns>
        public IMongoQuery GTE(string name, BsonValue value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            return new QueryDocument(name, new BsonDocument("$gte", value));
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>
        /// An IMongoQuery.
        /// </returns>
        public IMongoQuery In(string name, IEnumerable<BsonValue> values)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            
            return new QueryDocument(name, new BsonDocument("$in", new BsonArray(values)));
        }

        /// <summary>
        /// Tests that the value of the named element is less than some value (see $lt).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>
        /// An IMongoQuery.
        /// </returns>
        public IMongoQuery LT(string name, BsonValue value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            return new QueryDocument(name, new BsonDocument("$lt", value));
        }

        /// <summary>
        /// Tests that the value of the named element is less than or equal to some value (see $lte).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>
        /// An IMongoQuery.
        /// </returns>
        public IMongoQuery LTE(string name, BsonValue value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            return new QueryDocument(name, new BsonDocument("$lte", value));
        }

        /// <summary>
        /// Tests that the value of the named element matches a regular expression (see $regex).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="regex">The regex.</param>
        /// <returns>
        /// A query.
        /// </returns>
        public IMongoQuery Matches(string name, BsonRegularExpression regex)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (regex == null)
            {
                throw new ArgumentNullException("regex");
            }

            return new QueryDocument(name, regex);
        }

        /// <summary>
        /// Tests that the value of the named element matches a regular expression (see $regex).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="pattern">The pattern.</param>
        /// <returns>
        /// A query.
        /// </returns>
        public IMongoQuery Matches(string name, string pattern)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (pattern == null)
            {
                throw new ArgumentNullException("pattern");
            }

            return new QueryDocument(name, new BsonRegularExpression(pattern));
        }

        /// <summary>
        /// Tests that the value of the named element matches a regular expression (see $regex).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="pattern">The pattern.</param>
        /// <param name="options">The options.</param>
        /// <returns>
        /// A query.
        /// </returns>
        public IMongoQuery Matches(string name, string pattern, string options)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (pattern == null)
            {
                throw new ArgumentNullException("pattern");
            }
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            return new QueryDocument(name, new BsonRegularExpression(pattern, options));
        }

        /// <summary>
        /// Tests that the value of the named element matches a regular expression (see $regex).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="regex">The regex.</param>
        /// <returns>
        /// A query.
        /// </returns>
        public IMongoQuery Matches(string name, Regex regex)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (regex == null)
            {
                throw new ArgumentNullException("regex");
            }

            return new QueryDocument(name, new BsonRegularExpression(regex));
        }

        /// <summary>
        /// Tests that the modulus of the value of the named element matches some value (see $mod).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="modulus">The modulus.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// An IMongoQuery.
        /// </returns>
        public IMongoQuery Mod(string name, int modulus, int value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            var condition = new BsonDocument("$mod", new BsonArray { modulus, value });
            return new QueryDocument(name, condition);
        }

        /// <summary>
        /// Tests that the value of the named element is near some location (see $near).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="x">The x value of the origin.</param>
        /// <param name="y">The y value of the origin.</param>
        /// <returns>
        /// An IMongoQuery.
        /// </returns>
        public IMongoQuery Near(string name, double x, double y)
        {
            return Near(name, x, y, double.MaxValue);
        }

        /// <summary>
        /// Tests that the value of the named element is near some location (see $near).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="x">The x value of the origin.</param>
        /// <param name="y">The y value of the origin.</param>
        /// <param name="maxDistance">The max distance.</param>
        /// <returns>
        /// An IMongoQuery.
        /// </returns>
        public IMongoQuery Near(string name, double x, double y, double maxDistance)
        {
            return Near(name, x, y, maxDistance, false);
        }

        /// <summary>
        /// Tests that the value of the named element is near some location (see $near).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="x">The x value of the origin.</param>
        /// <param name="y">The y value of the origin.</param>
        /// <param name="maxDistance">The max distance.</param>
        /// <param name="spherical">if set to <c>true</c> then the query will be translated to $nearSphere.</param>
        /// <returns>
        /// An IMongoQuery.
        /// </returns>
        public IMongoQuery Near(string name, double x, double y, double maxDistance, bool spherical)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            var op = spherical ? "$nearSphere" : "$near";
            var condition = new BsonDocument(op, new BsonArray { x, y });
            if (maxDistance != double.MaxValue)
            {
                condition.Add("$maxDistance", maxDistance);
            }

            return new QueryDocument(name, condition);
        }

        /// <summary>
        /// Tests that the inverse of the query is true (see $not).
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public IMongoQuery Not(IMongoQuery query)
        {
            var queryDocument = query.ToBsonDocument();
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

        /// <summary>
        /// Tests that an element does not equal the value (see $ne).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// An IMongoQuery.
        /// </returns>
        public IMongoQuery NE(string name, BsonValue value)
        {
            return new QueryDocument(name, new BsonDocument("$ne", value));
        }

        /// <summary>
        /// Tests that an element of that name does not exist (see $exists).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <returns>
        /// An IMongoQuery.
        /// </returns>
        public IMongoQuery NotExists(string name)
        {
            return new QueryDocument(name, new BsonDocument("$exists", false));
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any item in a list of values (see $nin).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">The values to compare.</param>
        /// <returns>
        /// An IMongoQuery.
        /// </returns>
        public IMongoQuery NotIn(string name, IEnumerable<BsonValue> values)
        {
            return new QueryDocument(name, new BsonDocument("$nin", new BsonArray(values)));
        }

        /// <summary>
        /// Tests that at least one of the subqueries is true (see $or).
        /// </summary>
        /// <param name="queries">The subqueries.</param>
        /// <returns>
        /// An IMongoQuery.
        /// </returns>
        public IMongoQuery Or(IEnumerable<IMongoQuery> queries)
        {
            if (queries == null)
            {
                throw new ArgumentNullException("queries");
            }
            if (!queries.Any())
            {
                throw new ArgumentOutOfRangeException("queries", "Or cannot be called with zero queries.");
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
                    // skip query like { } which matches everything
                    if (queryDocument.ElementCount != 0)
                    {
                        queryArray.Add(queryDocument);
                    }
                }
            }

            switch (queryArray.Count)
            {
                case 0:
                    return new QueryComplete(new QueryDocument()); // all queries were empty queries so just return an empty query
                case 1:
                    return new QueryDocument(queryArray[0].AsBsonDocument);
                default:
                    return new QueryDocument("$or", queryArray);
            }
        }

        /// <summary>
        /// Tests that at least one of the subqueries is true (see $or).
        /// </summary>
        /// <param name="queries">The subqueries.</param>
        /// <returns>
        /// An IMongoQuery.
        /// </returns>
        public IMongoQuery Or(params IMongoQuery[] queries)
        {
            return Or((IEnumerable<IMongoQuery>)queries);
        }

        /// <summary>
        /// Tests that the size of the named array is equal to some value (see $size).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="size">The size to compare to.</param>
        /// <returns>
        /// An IMongoQuery.
        /// </returns>
        public IMongoQuery Size(string name, int size)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            var condition = new BsonDocument("$size", size);
            return new QueryDocument(name, condition);
        }

        /// <summary>
        /// Tests that the type of the named element is equal to some type (see $type).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="type">The type to compare to.</param>
        /// <returns>
        /// An IMongoQuery.
        /// </returns>
        public IMongoQuery Type(string name, BsonType type)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            var condition = new BsonDocument("$type", (int)type);
            return new QueryDocument(name, condition);
        }

        /// <summary>
        /// Tests that a JavaScript expression is true (see $where).
        /// </summary>
        /// <param name="javascript">The javascript.</param>
        /// <returns>
        /// A query.
        /// </returns>
        public IMongoQuery Where(BsonJavaScript javascript)
        {
            if (javascript == null)
            {
                throw new ArgumentNullException("javascript");
            }

            return new QueryDocument("$where", javascript);
        }

        /// <summary>
        /// Tests that the value of the named element is within a circle (see $within and $center).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="centerX">The x coordinate of the origin.</param>
        /// <param name="centerY">The y coordinate of the origin.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <returns>
        /// An IMongoQuery.
        /// </returns>
        public IMongoQuery WithinCircle(string name, double centerX, double centerY, double radius)
        {
            return WithinCircle(name, centerX, centerY, radius, false);
        }

        /// <summary>
        /// Tests that the value of the named element is within a circle (see $within and $center).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="centerX">The x coordinate of the origin.</param>
        /// <param name="centerY">The y coordinate of the origin.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="spherical">if set to <c>true</c> [spherical].</param>
        /// <returns>
        /// An IMongoQuery.
        /// </returns>
        public IMongoQuery WithinCircle(string name, double centerX, double centerY, double radius, bool spherical)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            var shape = spherical ? "$centerSphere" : "$center";
            var condition = new BsonDocument("$within", new BsonDocument(shape, new BsonArray { new BsonArray { centerX, centerY }, radius }));
            return new QueryDocument(name, condition);
        }

        /// <summary>
        /// Tests that the value of the named element is within a polygon (see $within and $polygon).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="points">An array of points that defines the polygon (the second dimension must be of length 2).</param>
        /// <returns>
        /// An IMongoQuery.
        /// </returns>
        public IMongoQuery WithinPolygon(string name, double[,] points)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
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

            var condition = new BsonDocument("$within", new BsonDocument("$polygon", arrayOfPoints));
            return new QueryDocument(name, condition);
        }

        /// <summary>
        /// Tests that the value of the named element is within a rectangle (see $within and $box).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="lowerLeftX">The x coordinate of the lower left corner.</param>
        /// <param name="lowerLeftY">The y coordinate of the lower left corner.</param>
        /// <param name="upperRightX">The x coordinate of the upper right corner.</param>
        /// <param name="upperRightY">The y coordinate of the upper right corner.</param>
        /// <returns>
        /// An IMongoQuery.
        /// </returns>
        public IMongoQuery WithinRectangle(string name, double lowerLeftX, double lowerLeftY, double upperRightX, double upperRightY)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            var condition = new BsonDocument("$within", new BsonDocument("$box", new BsonArray { new BsonArray { lowerLeftX, lowerLeftY }, new BsonArray { upperRightX, upperRightY } }));
            return new QueryDocument(name, condition);
        }

        // private methods
        private void AddAndClause(BsonDocument query, BsonElement clause)
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

        private void PromoteQueryToDollarAndForm(BsonDocument query, BsonElement clause)
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
}