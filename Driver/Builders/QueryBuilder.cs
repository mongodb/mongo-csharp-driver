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
using System.Text;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Builders {
    /// <summary>
    /// A builder for creating queries.
    /// </summary>
    [Serializable]
    public abstract class QueryBuilder : BuilderBase {
        #region private fields
        /// <summary>
        /// A BSON document containing the query being built.
        /// </summary>
        protected BsonDocument document;
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
        /// <param name="name">The name of the element to test.</param>
        public QueryConditionList(
            string name
        )
            : base(new BsonDocument(name, new BsonDocument())) {
            conditions = document[0].AsBsonDocument;
        }
        #endregion

        #region public methods
        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList All(
            BsonArray values
        ) {
            conditions.Add("$all", values);
            return this;
        }

        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList All(
            IEnumerable<BsonValue> values
        ) {
            conditions.Add("$all", new BsonArray(values));
            return this;
        }

        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="arg1">The first value to compare to.</param>
        /// <param name="arg2">The second value to compare to.</param>
        /// <param name="args">The additional values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList All(
            BsonValue arg1,
            BsonValue arg2,
            params BsonValue[] args
        ) {

            return this.All(ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList All(
            IEnumerable values
        ) {

            return this.All(new BsonArray(values));
        }

        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="arg1">The first value to compare to.</param>
        /// <param name="arg2">The second value to compare to.</param>
        /// <param name="args">The additional values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList All(
            object arg1,
            object arg2,
            params object[] args
        ) {

            return this.All(ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Tests that at least one item of the named array element matches a query (see $elemMatch).
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
        /// Tests that an element of that name does or does not exist (see $exists).
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
        /// Tests that the value of the named element is greater than some value (see $gt).
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
        /// Tests that the value of the named element is greater than or equal to some value (see $gte).
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
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList In(
            BsonArray values
        ) {
            conditions.Add("$in", values);
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList In(
            IEnumerable<BsonValue> values
        ) {
            return In(new BsonArray(values));
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="arg1">The first value to compare to.</param>
        /// <param name="arg2">The second value to compare to.</param>
        /// <param name="args">The additional values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList In(
            BsonValue arg1,
            BsonValue arg2,
            params BsonValue[] args
        ) {
            return this.In(ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList In(
            IEnumerable values
        ) {
            return this.In(new BsonArray(values));
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="arg1">The first value to compare to.</param>
        /// <param name="arg2">The second value to compare to.</param>
        /// <param name="args">The additional values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList In(
            object arg1,
            object arg2,
            params object[] args
        ) {
            return this.In(ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Tests that the value of the named element is less than some value (see $lt).
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
        /// Tests that the value of the named element is less than or equal to some value (see $lte).
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
        /// Tests that the modulus of the value of the named element matches some value (see $mod).
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
        /// Tests that the value of the named element is not equal to some value (see $ne).
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList NE(
            object value
        ) {
            conditions.Add("$ne", BsonValue.Create(value));
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is near some location (see $near).
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
        /// Tests that the value of the named element is near some location (see $near).
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
        /// Tests that the value of the named element is near some location (see $near and $nearSphere).
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
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList NotIn(
            BsonArray values
        ) {
            conditions.Add("$nin", values);
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList NotIn(
            IEnumerable<BsonValue> values
        ) {
            return this.NotIn(new BsonArray(values));
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="arg1">The first value to compare to.</param>
        /// <param name="arg2">The second value to compare to.</param>
        /// <param name="args">The additional values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList NotIn(
            BsonValue arg1,
            BsonValue arg2,
            params BsonValue[] args
        ) {
            return this.NotIn(ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList NotIn(
            IEnumerable values
        ) {
            return this.NotIn(new BsonArray(values));
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="arg1">The first value to compare to.</param>
        /// <param name="arg2">The second value to compare to.</param>
        /// <param name="args">The additional values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList NotIn(
            object arg1,
            object arg2,
            params object[] args
        ) {
            return this.NotIn(ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Tests that the size of the named array is equal to some value (see $size).
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
        /// Tests that the type of the named element is equal to some type (see $type).
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
        /// Tests that the value of the named element is within a circle (see $within and $center).
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
        /// Tests that the value of the named element is within a circle (see $within and $center/$centerSphere).
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
        /// Tests that the value of the named element is within a polygon (see $within and $polygon).
        /// </summary>
        /// <param name="points">An array of points that defines the polygon (the second dimension must be of length 2).</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryConditionList WithinPolygon(
            double[,] points
        ) {
            if (points.GetLength(1) != 2) {
                var message = string.Format("The second dimension of the points array must be of length 2, not {0}.", points.GetLength(1));
                throw new ArgumentOutOfRangeException("points", message);
            }

            var arrayOfPoints = new BsonArray(points.GetLength(0));
            for (var i = 0; i < points.GetLength(0); i++) {
                arrayOfPoints.Add(new BsonArray(2) { points[i, 0], points[i, 1] });
            }
            conditions.Add("$within", new BsonDocument("$polygon", arrayOfPoints));
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
        /// <param name="name">The name of the element to test.</param>
        public QueryNot(
            string name
        ) {
            this.name = name;
        }
        #endregion

        #region public methods
        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList All(
            BsonArray values
        ) {
            return new QueryNotConditionList(name).All(values);
        }

        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList All(
            IEnumerable<BsonValue> values
        ) {
            return new QueryNotConditionList(name).All(values);
        }

        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList All(
            params BsonValue[] values
        ) {
            return new QueryNotConditionList(name).All(values);
        }

        /// <summary>
        /// Tests that at least one item of the named array element matches a query (see $elemMatch).
        /// </summary>
        /// <param name="query">The query to match elements with.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList ElemMatch(
            IMongoQuery query
        ) {
            return new QueryNotConditionList(name).ElemMatch(query);
        }

        /// <summary>
        /// Tests that an element of that name does or does not exist (see $exists).
        /// </summary>
        /// <param name="exists">Whether to test for the existence or absence of an element.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList Exists(
            bool exists
        ) {
            return new QueryNotConditionList(name).Exists(exists);
        }

        /// <summary>
        /// Tests that the value of the named element is greater than some value (see $gt).
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList GT(
            BsonValue value
        ) {
            return new QueryNotConditionList(name).GT(value);
        }

        /// <summary>
        /// Tests that the value of the named element is greater than or equal to some value (see $gte).
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList GTE(
            BsonValue value
        ) {
            return new QueryNotConditionList(name).GTE(value);
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList In(
            BsonArray values
        ) {
            return new QueryNotConditionList(name).In(values);
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList In(
            IEnumerable<BsonValue> values
        ) {
            return new QueryNotConditionList(name).In(values);
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList In(
            params BsonValue[] values
        ) {
            return new QueryNotConditionList(name).In(values);
        }

        /// <summary>
        /// Tests that the value of the named element is less than some value (see $lt).
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList LT(
            BsonValue value
        ) {
            return new QueryNotConditionList(name).LT(value);
        }

        /// <summary>
        /// Tests that the value of the named element is less than or equal to some value (see $lte).
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList LTE(
            BsonValue value
        ) {
            return new QueryNotConditionList(name).LTE(value);
        }

        /// <summary>
        /// Tests that the modulus of the value of the named element matches some value (see $mod).
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
        /// Tests that the value of the named element is not equal to some value (see $ne).
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList NE(
            BsonValue value
        ) {
            return new QueryNotConditionList(name).NE(value);
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList NotIn(
            BsonArray values
        ) {
            return new QueryNotConditionList(name).NotIn(values);
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList NotIn(
            IEnumerable<BsonValue> values
        ) {
            return new QueryNotConditionList(name).NotIn(values);
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList NotIn(
            params BsonValue[] values
        ) {
            return new QueryNotConditionList(name).NotIn(values);
        }

        /// <summary>
        /// Tests that the value of the named element matches a regular expression (see $regex).
        /// </summary>
        /// <param name="regex">The regular expression to match against.</param>
        /// <returns>A query.</returns>
        public QueryComplete Matches(
            BsonRegularExpression regex
        ) {
            return new QueryComplete(new BsonDocument(name, new BsonDocument("$not", regex)));
        }

        /// <summary>
        /// Tests that the size of the named array is equal to some value (see $size).
        /// </summary>
        /// <param name="size">The size of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList Size(
            int size
        ) {
            return new QueryNotConditionList(name).Size(size);
        }

        /// <summary>
        /// Tests that the type of the named element is equal to some type (see $type).
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
        /// <param name="name">The name of the first element to test.</param>
        public QueryNotConditionList(
            string name
        )
            : base(new BsonDocument(name, new BsonDocument("$not", new BsonDocument()))) {
            conditions = document[0].AsBsonDocument[0].AsBsonDocument;
        }
        #endregion

        #region public methods
        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList All(
            BsonArray values
        ) {
            conditions.Add("$all", values);
            return this;
        }

        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList All(
            IEnumerable<BsonValue> values
        ) {
            conditions.Add("$all", new BsonArray(values));
            return this;
        }

        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList All(
            params BsonValue[] values
        ) {
            conditions.Add("$all", new BsonArray(values));
            return this;
        }

        /// <summary>
        /// Tests that at least one item of the named array element matches a query (see $elemMatch).
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
        /// Tests that an element of that name does or does not exist (see $exists).
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
        /// Tests that the value of the named element is greater than some value (see $gt).
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
        /// Tests that the value of the named element is greater than or equal to some value (see $gte).
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
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList In(
            BsonArray values
        ) {
            conditions.Add("$in", values);
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList In(
            IEnumerable<BsonValue> values
        ) {
            return this.In(new BsonArray(values));
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="arg1">The first value to compare to.</param>
        /// <param name="arg2">The second value to compare to.</param>
        /// <param name="args">The additional values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList In(
            BsonValue arg1,
            BsonValue arg2,
            params BsonValue[] args
        ) {
            return this.In(ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList In(
            IEnumerable values
        ) {
            return this.In(new BsonArray(values));
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="arg1">The first value to compare to.</param>
        /// <param name="arg2">The second value to compare to.</param>
        /// <param name="args">The additional values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList In(
            object arg1,
            object arg2,
            params object[] args
        ) {
            return this.In(ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Tests that the value of the named element is less than some value (see $lt).
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
        /// Tests that the value of the named element is less than or equal to some value (see $lte).
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
        /// Tests that the modulus of the value of the named element matches some value (see $mod).
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
        /// Tests that the value of the named element is not equal to some value ($ne).
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
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList NotIn(
            BsonArray values
        ) {
            conditions.Add("$nin", values);
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList NotIn(
            IEnumerable<BsonValue> values
        ) {
            conditions.Add("$nin", new BsonArray(values));
            return this;
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public QueryNotConditionList NotIn(
            params BsonValue[] values
        ) {
            conditions.Add("$nin", new BsonArray(values));
            return this;
        }

        /// <summary>
        /// Tests that the size of the named array is equal to some value (see $size).
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
        /// Tests that the type of the named element is equal to some type (see $type).
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
