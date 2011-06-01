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
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Builders {
    /// <summary>
    /// A builder for creating queries.
    /// </summary>
    public static class Query {
        #region public static properties
        /// <summary>
        /// Gets a null value with a type of IMongoQuery.
        /// </summary>
        public static IMongoQuery Null {
            get { return null; }
        }
        #endregion

        #region public static methods
        /// <summary>
        /// Adds a $all test to the query.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">A BsonArray of values.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList All(
            string name,
            BsonArray values
        ) {
            return new QueryConditionList(name).All(values);
        }

        /// <summary>
        /// Adds a $all test to the query.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">One or more BsonValues.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList All(
            string name,
            params BsonValue[] values
        ) {
            return new QueryConditionList(name).All(values);
        }

        /// <summary>
        /// Combines subqueries with an and operator.
        /// </summary>
        /// <param name="queries">The subqueries.</param>
        /// <returns>A query.</returns>
        public static QueryComplete And(
            params IMongoQuery[] queries
        ) {
            var document = new BsonDocument();
            foreach (var query in queries) {
                if (query != null) {
                    foreach (var queryElement in query.ToBsonDocument()) {
                        // if result document has existing operations for same field append the new ones
                        if (document.Contains(queryElement.Name)) {
                            var existingOperations = document[queryElement.Name] as BsonDocument;
                            var newOperations = queryElement.Value as BsonDocument;

                            // make sure that no conditions are Query.EQ, because duplicates aren't allowed
                            if (existingOperations == null || newOperations == null) {
                                var message = string.Format("Query.And does not support combining equality comparisons with other operators (field '{0}').", queryElement.Name);
                                throw new InvalidOperationException(message);
                            }

                            // add each new operation to the existing operations
                            foreach (var operation in newOperations) {
                                // make sure that there are no duplicate $operators
                                if (existingOperations.Contains(operation.Name)) {
                                    var message = string.Format("Query.And does not support using the same operator more than once (field '{0}', operator '{1}').", queryElement.Name, operation.Name);
                                    throw new InvalidOperationException(message);
                                } else {
                                    existingOperations.Add(operation);
                                }
                            }
                        } else {
                            document.Add(queryElement);
                        }
                    }
                }
            }

            return document.ElementCount > 0 ? new QueryComplete(document) : null;
        }

        /// <summary>
        /// Adds an $elemMatch test to the query.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="query">The query to match elements with.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList ElemMatch(
            string name,
            IMongoQuery query
        ) {
            return new QueryConditionList(name).ElemMatch(query);
        }

        /// <summary>
        /// Adds an equality test to the query.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>A query.</returns>
        public static QueryComplete EQ(
            string name,
            BsonValue value
        ) {
            return new QueryComplete(new BsonDocument(name, value));
        }

        /// <summary>
        /// Adds a $exist test to the query.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="exists">Whether to test for the existence or absence of an element.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList Exists(
            string name,
            bool exists
        ) {
            return new QueryConditionList(name).Exists(exists);
        }

        /// <summary>
        /// Adds a $gt test to the query.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList GT(
            string name,
            BsonValue value
        ) {
            return new QueryConditionList(name).GT(value);
        }

        /// <summary>
        /// Adds a $gte test to the query.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList GTE(
            string name,
            BsonValue value
        ) {
            return new QueryConditionList(name).GTE(value);
        }

        /// <summary>
        /// Adds a $in test to the query.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">A BsonArray of values.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList In(
            string name,
            BsonArray values
        ) {
            return new QueryConditionList(name).In(values);
        }

        /// <summary>
        /// Adds a $in test to the query.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">One or more BsonValues.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList In(
            string name,
            params BsonValue[] values
        ) {
            return new QueryConditionList(name).In(values);
        }

        /// <summary>
        /// Adds a $lt test to the query.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList LT(
            string name,
            BsonValue value
        ) {
            return new QueryConditionList(name).LT(value);
        }

        /// <summary>
        /// Adds a $lte test to the query.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList LTE(
            string name,
            BsonValue value
        ) {
            return new QueryConditionList(name).LTE(value);
        }

        /// <summary>
        /// Adds a regular expression test to the query.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="regex">The regular expression to match against.</param>
        /// <returns>A query.</returns>
        public static QueryComplete Matches(
            string name,
            BsonRegularExpression regex
        ) {
            return new QueryComplete(new BsonDocument(name, regex));
        }

        /// <summary>
        /// Adds a $mod test to the query.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="modulus">The modulus.</param>
        /// <param name="equals">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList Mod(
            string name,
            int modulus,
            int equals
        ) {
            return new QueryConditionList(name).Mod(modulus, equals);
        }

        /// <summary>
        /// Adds a $ne test to the query.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList NE(
            string name,
            BsonValue value
        ) {
            return new QueryConditionList(name).NE(value);
        }

        /// <summary>
        /// Adds a $near test to the query.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="x">The x value of the origin.</param>
        /// <param name="y">The y value of the origin.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList Near(
            string name,
            double x,
            double y
        ) {
            return new QueryConditionList(name).Near(x, y);
        }

        /// <summary>
        /// Adds a $near test to the query.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="x">The x value of the origin.</param>
        /// <param name="y">The y value of the origin.</param>
        /// <param name="maxDistance">The max distance for a document to be included in the results.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList Near(
            string name,
            double x,
            double y,
            double maxDistance
        ) {
            return new QueryConditionList(name).Near(x, y, maxDistance);
        }

        /// <summary>
        /// Adds a $near or $nearSphere test to the query.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="x">The x value of the origin.</param>
        /// <param name="y">The y value of the origin.</param>
        /// <param name="maxDistance">The max distance for a document to be included in the results.</param>
        /// <param name="spherical">Whether to do a spherical search.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList Near(
            string name,
            double x,
            double y,
            double maxDistance,
            bool spherical
        ) {
            return new QueryConditionList(name).Near(x, y, maxDistance, spherical);
        }

        /// <summary>
        /// Combines subqueries with a nor operator.
        /// </summary>
        /// <param name="queries">The subqueries.</param>
        /// <returns>A query.</returns>
        public static QueryComplete Nor(
            params IMongoQuery[] queries
        ) {
            var clauses = new BsonArray();
            foreach (var query in queries) {
                clauses.Add(query.ToBsonDocument());
            }
            var document = new BsonDocument("$nor", clauses);
            return new QueryComplete(document);
        }

        /// <summary>
        /// Adds a $nin test to the query.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">A BsonArray of values.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList NotIn(
            string name,
            BsonArray values
        ) {
            return new QueryConditionList(name).NotIn(values);
        }

        /// <summary>
        /// Adds a $nin test to the query.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">One or more BsonValues.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList NotIn(
            string name,
            params BsonValue[] values
        ) {
            return new QueryConditionList(name).NotIn(values);
        }

        /// <summary>
        /// Adds a $not test to the query.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryNot Not(
            string name
        ) {
            return new QueryNot(name);
        }

        /// <summary>
        /// Combines subqueries with an or operator.
        /// </summary>
        /// <param name="queries">The subqueries.</param>
        /// <returns>A query.</returns>
        public static QueryComplete Or(
            params IMongoQuery[] queries
        ) {
            var clauses = new BsonArray();
            foreach (var query in queries) {
                if (query != null) {
                    clauses.Add(query.ToBsonDocument());
                }
            }

            switch (clauses.Count) {
                case 0:
                    return null;
                case 1:
                    return new QueryComplete(clauses[0].AsBsonDocument);
                default:
                    return new QueryComplete(new BsonDocument("$or", clauses));
            }
        }

        /// <summary>
        /// Adds a $size test to the query.
        /// </summary>
        /// <param name="name">The name of the array element to test.</param>
        /// <param name="size">The size of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList Size(
            string name,
            int size
        ) {
            return new QueryConditionList(name).Size(size);
        }

        /// <summary>
        /// Adds a $type test to the query.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="type">The type.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList Type(
            string name,
            BsonType type
        ) {
            return new QueryConditionList(name).Type(type);
        }

        /// <summary>
        /// Adds a $where test to the query.
        /// </summary>
        /// <param name="javaScript">The where clause.</param>
        /// <returns>A query.</returns>
        public static QueryComplete Where(
            BsonJavaScript javaScript
        ) {
            return new QueryComplete(new BsonDocument("$where", javaScript));
        }

        /// <summary>
        /// Adds a $within/$center test to the query.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="centerX">The x coordinate of the origin.</param>
        /// <param name="centerY">The y coordinate of the origin.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList WithinCircle(
            string name,
            double centerX,
            double centerY,
            double radius
        ) {
            return new QueryConditionList(name).WithinCircle(centerX, centerY, radius);
        }

        /// <summary>
        /// Adds a $within/$center or $within/$centerSphere test to the query.
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
            bool spherical
        ) {
            return new QueryConditionList(name).WithinCircle(centerX, centerY, radius, spherical);
        }

        /// <summary>
        /// Adds a $within/$box test to the query.
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
            double upperRightY
        ) {
            return new QueryConditionList(name).WithinRectangle(lowerLeftX, lowerLeftY, upperRightX, upperRightY);
        }
        #endregion
    }

    /// <summary>
    /// A builder for creating queries.
    /// </summary>
    [Serializable]
    public abstract class QueryBuilder : BuilderBase {
        #region private fields
#pragma warning disable 1591 // missing XML comment (it's warning about protected members also)
        protected BsonDocument document;
#pragma warning restore
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the QueryBuilder class.
        /// </summary>
        /// <param name="document">A document representing the query.</param>
        protected QueryBuilder(
            BsonDocument document
        ) {
            this.document = document;
        }
        #endregion

        #region public methods
        /// <summary>
        /// Returns the result of the builder as a BsonDocument.
        /// </summary>
        /// <returns>A BsonDocument.</returns>
        public override BsonDocument ToBsonDocument() {
            return document;
        }
        #endregion

        #region protected methods
        /// <summary>
        /// Serializes the result of the builder to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The writer.</param>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="options">The serialization options.</param>
        protected override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            document.Serialize(bsonWriter, nominalType, options);
        }
        #endregion
    }

    /// <summary>
    /// A builder for creating queries.
    /// </summary>
    [Serializable]
    public class QueryComplete : QueryBuilder, IMongoQuery {
        #region constructors
        /// <summary>
        /// Initializes a new instance of the QueryComplete class.
        /// </summary>
        /// <param name="document">A document representing the query.</param>
        public QueryComplete(
            BsonDocument document
        )
            : base(document) {
        }
        #endregion
    }

    /// <summary>
    /// A builder for creating queries.
    /// </summary>
    [Serializable]
    public class QueryConditionList : QueryComplete {
        #region private fields
        private BsonDocument conditions;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the QueryConditionList class.
        /// </summary>
        /// <param name="name">The name of the element to be tested.</param>
        public QueryConditionList(
            string name
        )
            : base(new BsonDocument(name, new BsonDocument())) {
            conditions = document[0].AsBsonDocument;
        }
        #endregion

        #region public methods
        /// <summary>
        /// Adds a $all test to the query.
        /// </summary>
        /// <param name="values">A BsonArray of values.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList All(
            BsonArray values
        ) {
            conditions.Add("$all", values);
            return this;
        }

        /// <summary>
        /// Adds a $all test to the query.
        /// </summary>
        /// <param name="values">One or more BsonValues.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList All(
            params BsonValue[] values
        ) {
            conditions.Add("$all", new BsonArray((IEnumerable<BsonValue>) values));
            return this;
        }

        /// <summary>
        /// Adds an $elemMatch test to the query.
        /// </summary>
        /// <param name="query">The query to match elements with.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList ElemMatch(
            IMongoQuery query
        ) {
            conditions.Add("$elemMatch", query.ToBsonDocument());
            return this;
        }

        /// <summary>
        /// Adds a $exist test to the query.
        /// </summary>
        /// <param name="exists">Whether to test for the existence or absence of an element.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList Exists(
            bool exists
        ) {
            conditions.Add("$exists", BsonBoolean.Create(exists));
            return this;
        }

        /// <summary>
        /// Adds a $gt test to the query.
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList GT(
            BsonValue value
        ) {
            conditions.Add("$gt", value);
            return this;
        }

        /// <summary>
        /// Adds a $gte test to the query.
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList GTE(
            BsonValue value
        ) {
            conditions.Add("$gte", value);
            return this;
        }

        /// <summary>
        /// Adds a $in test to the query.
        /// </summary>
        /// <param name="values">A BsonArray of values.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList In(
            BsonArray values
        ) {
            conditions.Add("$in", values);
            return this;
        }

        /// <summary>
        /// Adds a $in test to the query.
        /// </summary>
        /// <param name="values">One or more BsonValues.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList In(
            params BsonValue[] values
        ) {
            conditions.Add("$in", new BsonArray((IEnumerable<BsonValue>) values));
            return this;
        }

        /// <summary>
        /// Adds a $lt test to the query.
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList LT(
            BsonValue value
        ) {
            conditions.Add("$lt", value);
            return this;
        }

        /// <summary>
        /// Adds a $lte test to the query.
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList LTE(
            BsonValue value
        ) {
            conditions.Add("$lte", value);
            return this;
        }

        /// <summary>
        /// Adds a $mod test to the query.
        /// </summary>
        /// <param name="modulus">The modulus.</param>
        /// <param name="equals">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList Mod(
            int modulus,
            int equals
        ) {
            conditions.Add("$mod", new BsonArray { modulus, equals });
            return this;
        }

        /// <summary>
        /// Adds a $ne test to the query.
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList NE(
            BsonValue value
        ) {
            conditions.Add("$ne", value);
            return this;
        }

        /// <summary>
        /// Adds a $near test to the query.
        /// </summary>
        /// <param name="x">The x value of the origin.</param>
        /// <param name="y">The y value of the origin.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList Near(
            double x,
            double y
        ) {
            return Near(x, y, double.MaxValue);
        }

        /// <summary>
        /// Adds a $near test to the query.
        /// </summary>
        /// <param name="x">The x value of the origin.</param>
        /// <param name="y">The y value of the origin.</param>
        /// <param name="maxDistance">The max distance for a document to be included in the results.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList Near(
            double x,
            double y,
            double maxDistance
        ) {
            return Near(x, y, maxDistance, false); // not spherical
        }

        /// <summary>
        /// Adds a $near or $nearSphere test to the query.
        /// </summary>
        /// <param name="x">The x value of the origin.</param>
        /// <param name="y">The y value of the origin.</param>
        /// <param name="maxDistance">The max distance for a document to be included in the results.</param>
        /// <param name="spherical">Whether to do a spherical search.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList Near(
            double x,
            double y,
            double maxDistance,
            bool spherical
        ) {
            var op = spherical ? "$nearSphere" : "$near";
            conditions.Add(op, new BsonArray { x, y });
            if (maxDistance != double.MaxValue) {
                conditions.Add("$maxDistance", maxDistance);
            }
            return this;
        }

        /// <summary>
        /// Adds a $nin test to the query.
        /// </summary>
        /// <param name="values">A BsonArray of values.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList NotIn(
            BsonArray values
        ) {
            conditions.Add("$nin", values);
            return this;
        }

        /// <summary>
        /// Adds a $nin test to the query.
        /// </summary>
        /// <param name="values">One or more BsonValues.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList NotIn(
            params BsonValue[] values
        ) {
            conditions.Add("$nin", new BsonArray((IEnumerable<BsonValue>) values));
            return this;
        }

        /// <summary>
        /// Adds a $size test to the query.
        /// </summary>
        /// <param name="size">The size of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList Size(
            int size
        ) {
            conditions.Add("$size", size);
            return this;
        }

        /// <summary>
        /// Adds a $type test to the query.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList Type(
            BsonType type
        ) {
            conditions.Add("$type", (int) type);
            return this;
        }

        /// <summary>
        /// Adds a $within/$center test to the query.
        /// </summary>
        /// <param name="x">The x coordinate of the origin.</param>
        /// <param name="y">The y coordinate of the origin.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList WithinCircle(
            double x,
            double y,
            double radius
        ) {
            return WithinCircle(x, y, radius, false); // not spherical
        }

        /// <summary>
        /// Adds a $within/$center or $within/$centerSphere test to the query.
        /// </summary>
        /// <param name="x">The x coordinate of the origin.</param>
        /// <param name="y">The y coordinate of the origin.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="spherical">Whether to do a spherical search.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList WithinCircle(
            double x,
            double y,
            double radius,
            bool spherical
        ) {
            var shape = spherical ? "$centerSphere" : "$center";
            conditions.Add("$within", new BsonDocument(shape, new BsonArray { new BsonArray { x, y }, radius }));
            return this;
        }

        /// <summary>
        /// Adds a $within/$box test to the query.
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
            double upperRightY
        ) {
            conditions.Add("$within", new BsonDocument("$box", new BsonArray { new BsonArray { lowerLeftX, lowerLeftY }, new BsonArray { upperRightX, upperRightY } }));
            return this;
        }
        #endregion
    }

    /// <summary>
    /// A builder for creating queries.
    /// </summary>
    public class QueryNot {
        #region private fields
        private string name;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the QueryNot class.
        /// </summary>
        /// <param name="name">The name of the element to be tested.</param>
        public QueryNot(
            string name
        ) {
            this.name = name;
        }
        #endregion

        #region public methods
        /// <summary>
        /// Adds a $all test to the query.
        /// </summary>
        /// <param name="values">A BsonArray of values.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList All(
            BsonArray values
        ) {
            return new QueryNotConditionList(name).All(values);
        }

        /// <summary>
        /// Adds a $all test to the query.
        /// </summary>
        /// <param name="values">One or more BsonValues.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList All(
            params BsonValue[] values
        ) {
            return new QueryNotConditionList(name).All(values);
        }

        /// <summary>
        /// Adds an $elemMatch test to the query.
        /// </summary>
        /// <param name="query">The query to match elements with.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList ElemMatch(
            IMongoQuery query
        ) {
            return new QueryNotConditionList(name).ElemMatch(query);
        }

        /// <summary>
        /// Adds a $exist test to the query.
        /// </summary>
        /// <param name="exists">Whether to test for the existence or absence of an element.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList Exists(
            bool exists
        ) {
            return new QueryNotConditionList(name).Exists(exists);
        }

        /// <summary>
        /// Adds a $gt test to the query.
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList GT(
            BsonValue value
        ) {
            return new QueryNotConditionList(name).GT(value);
        }

        /// <summary>
        /// Adds a $gte test to the query.
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList GTE(
            BsonValue value
        ) {
            return new QueryNotConditionList(name).GTE(value);
        }

        /// <summary>
        /// Adds a $in test to the query.
        /// </summary>
        /// <param name="values">A BsonArray of values.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList In(
            BsonArray values
        ) {
            return new QueryNotConditionList(name).In(values);
        }

        /// <summary>
        /// Adds a $in test to the query.
        /// </summary>
        /// <param name="values">One or more BsonValues.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList In(
            params BsonValue[] values
        ) {
            return new QueryNotConditionList(name).In(values);
        }

        /// <summary>
        /// Adds a $lt test to the query.
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList LT(
            BsonValue value
        ) {
            return new QueryNotConditionList(name).LT(value);
        }

        /// <summary>
        /// Adds a $lte test to the query.
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList LTE(
            BsonValue value
        ) {
            return new QueryNotConditionList(name).LTE(value);
        }

        /// <summary>
        /// Adds a $mod test to the query.
        /// </summary>
        /// <param name="modulus">The modulus.</param>
        /// <param name="equals">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList Mod(
            int modulus,
            int equals
        ) {
            return new QueryNotConditionList(name).Mod(modulus, equals);
        }

        /// <summary>
        /// Adds a $ne test to the query.
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList NE(
            BsonValue value
        ) {
            return new QueryNotConditionList(name).NE(value);
        }

        /// <summary>
        /// Adds a $nin test to the query.
        /// </summary>
        /// <param name="values">A BsonArray of values.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList NotIn(
            BsonArray values
        ) {
            return new QueryNotConditionList(name).NotIn(values);
        }

        /// <summary>
        /// Adds a $nin test to the query.
        /// </summary>
        /// <param name="values">One or more BsonValues.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList NotIn(
            params BsonValue[] values
        ) {
            return new QueryNotConditionList(name).NotIn(values);
        }

        /// <summary>
        /// Adds a regular expression test to the query.
        /// </summary>
        /// <param name="regex">The regular expression to match against.</param>
        /// <returns>A query.</returns>
        public QueryComplete Matches(
            BsonRegularExpression regex
        ) {
            return new QueryComplete(new BsonDocument(name, new BsonDocument("$not", regex)));
        }

        /// <summary>
        /// Adds a $size test to the query.
        /// </summary>
        /// <param name="size">The size of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList Size(
            int size
        ) {
            return new QueryNotConditionList(name).Size(size);
        }

        /// <summary>
        /// Adds a $type test to the query.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList Type(
            BsonType type
        ) {
            return new QueryNotConditionList(name).Type(type);
        }
        #endregion
    }

    /// <summary>
    /// A builder for creating queries.
    /// </summary>
    [Serializable]
    public class QueryNotConditionList : QueryComplete {
        #region private fields
        private BsonDocument conditions;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the QueryNotConditionList.
        /// </summary>
        /// <param name="name">The name of the first element to be tested.</param>
        public QueryNotConditionList(
            string name
        )
            : base(new BsonDocument(name, new BsonDocument("$not", new BsonDocument()))) {
            conditions = document[0].AsBsonDocument[0].AsBsonDocument;
        }
        #endregion

        #region public methods
        /// <summary>
        /// Adds a $all test to the query.
        /// </summary>
        /// <param name="values">A BsonArray of values.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList All(
            BsonArray values
        ) {
            conditions.Add("$all", values);
            return this;
        }

        /// <summary>
        /// Adds a $all test to the query.
        /// </summary>
        /// <param name="values">One or more BsonValues.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList All(
            params BsonValue[] values
        ) {
            conditions.Add("$all", new BsonArray((IEnumerable<BsonValue>) values));
            return this;
        }

        /// <summary>
        /// Adds an $elemMatch test to the query.
        /// </summary>
        /// <param name="query">The query to match elements with.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList ElemMatch(
            IMongoQuery query
        ) {
            conditions.Add("$elemMatch", query.ToBsonDocument());
            return this;
        }

        /// <summary>
        /// Adds a $exist test to the query.
        /// </summary>
        /// <param name="exists">Whether to test for the existence or absence of an element.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList Exists(
            bool exists
        ) {
            conditions.Add("$exists", BsonBoolean.Create(exists));
            return this;
        }

        /// <summary>
        /// Adds a $gt test to the query.
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList GT(
            BsonValue value
        ) {
            conditions.Add("$gt", value);
            return this;
        }

        /// <summary>
        /// Adds a $gte test to the query.
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList GTE(
            BsonValue value
        ) {
            conditions.Add("$gte", value);
            return this;
        }

        /// <summary>
        /// Adds a $in test to the query.
        /// </summary>
        /// <param name="values">A BsonArray of values.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList In(
            BsonArray values
        ) {
            conditions.Add("$in", values);
            return this;
        }

        /// <summary>
        /// Adds a $in test to the query.
        /// </summary>
        /// <param name="values">One or more BsonValues.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList In(
            params BsonValue[] values
        ) {
            conditions.Add("$in", new BsonArray((IEnumerable<BsonValue>) values));
            return this;
        }

        /// <summary>
        /// Adds a $lt test to the query.
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList LT(
            BsonValue value
        ) {
            conditions.Add("$lt", value);
            return this;
        }

        /// <summary>
        /// Adds a $lte test to the query.
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList LTE(
            BsonValue value
        ) {
            conditions.Add("$lte", value);
            return this;
        }

        /// <summary>
        /// Adds a $mod test to the query.
        /// </summary>
        /// <param name="modulus">The modulus.</param>
        /// <param name="equals">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList Mod(
            int modulus,
            int equals
        ) {
            conditions.Add("$mod", new BsonArray { modulus, equals });
            return this;
        }

        /// <summary>
        /// Adds a $ne test to the query.
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList NE(
            BsonValue value
        ) {
            conditions.Add("$ne", value);
            return this;
        }

        /// <summary>
        /// Adds a $nin test to the query.
        /// </summary>
        /// <param name="values">A BsonArray of values.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList NotIn(
            BsonArray values
        ) {
            conditions.Add("$nin", values);
            return this;
        }

        /// <summary>
        /// Adds a $nin test to the query.
        /// </summary>
        /// <param name="values">One or more BsonValues.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList NotIn(
            params BsonValue[] values
        ) {
            conditions.Add("$nin", new BsonArray((IEnumerable<BsonValue>) values));
            return this;
        }

        /// <summary>
        /// Adds a $size test to the query.
        /// </summary>
        /// <param name="size">The size of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList Size(
            int size
        ) {
            conditions.Add("$size", size);
            return this;
        }

        /// <summary>
        /// Adds a $type test to the query.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList Type(
            BsonType type
        ) {
            conditions.Add("$type", (int) type);
            return this;
        }
        #endregion
    }
}
