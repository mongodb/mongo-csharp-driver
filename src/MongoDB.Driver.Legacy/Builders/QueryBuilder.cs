/* Copyright 2010-2015 MongoDB Inc.
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
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver.GeoJsonObjectModel;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Utils;

namespace MongoDB.Driver.Builders
{
    /// <summary>
    /// A builder for creating queries.
    /// </summary>
    public static class Query
    {
        // public static properties
        /// <summary>
        /// Gets an empty query.
        /// </summary>
        /// <value>
        /// An empty query.
        /// </value>
        public static IMongoQuery Empty
        {
            get { return Query.Create(new BsonDocument()); }
        }

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
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery All(string name, IEnumerable<BsonValue> values)
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
            return Query.Create(name, condition);
        }

        /// <summary>
        /// Tests that all the queries are true (see $and in newer versions of the server).
        /// </summary>
        /// <param name="queries">A list of subqueries.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery And(IEnumerable<IMongoQuery> queries)
        {
            if (queries == null)
            {
                throw new ArgumentNullException("queries");
            }
            if (!queries.Any())
            {
                throw new ArgumentOutOfRangeException("queries", "And cannot be called with zero queries.");
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

            return Query.Create(queryDocument);
        }

        /// <summary>
        /// Tests that all the queries are true (see $and in newer versions of the server).
        /// </summary>
        /// <param name="queries">A list of subqueries.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery And(params IMongoQuery[] queries)
        {
            return And((IEnumerable<IMongoQuery>)queries);
        }

        /// <summary>
        /// Tests that the value of the named element has all of the specified bits clear.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="bitmask">The bitmask.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery BitsAllClear(string name, long bitmask)
        {
            return Query.Create(name, new BsonDocument("$bitsAllClear", bitmask));
        }

        /// <summary>
        /// Tests that the value of the named element has all of the specified bits set.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="bitmask">The bitmask.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery BitsAllSet(string name, long bitmask)
        {
            return Query.Create(name, new BsonDocument("$bitsAllSet", bitmask));
        }

        /// <summary>
        /// Tests that the value of the named element has any of the specified bits clear.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="bitmask">The bitmask.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery BitsAnyClear(string name, long bitmask)
        {
            return Query.Create(name, new BsonDocument("$bitsAnyClear", bitmask));
        }

        /// <summary>
        /// Tests that the value of the named element has any of the specified bits set.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="bitmask">The bitmask.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery BitsAnySet(string name, long bitmask)
        {
            return Query.Create(name, new BsonDocument("$bitsAnySet", bitmask));
        }

        /// <summary>
        /// Creates a query manually.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Create(BsonDocument query)
        {
            return new MongoQueryWrapper(query);
        }

        /// <summary>
        /// Creates a query manually.
        /// </summary>
        /// <param name="name">The element name.</param>
        /// <param name="condition">The condition.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Create(string name, BsonValue condition)
        {
            return new MongoQueryWrapper(new BsonDocument(name, condition));
        }

        /// <summary>
        /// Tests that at least one item of the named array element matches a query (see $elemMatch).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="query">The query to match elements with.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery ElemMatch(string name, IMongoQuery query)
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
            return Query.Create(name, condition);
        }

        /// <summary>
        /// Tests that the value of the named element is equal to some value.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery EQ(string name, BsonValue value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            return Query.Create(name, value);
        }

        /// <summary>
        /// Tests that an element of that name exists (see $exists).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Exists(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            return Query.Create(name, new BsonDocument("$exists", true));
        }

        /// <summary>
        /// Tests that a location element specified by name intersects with the geometry (see $geoIntersects).
        /// </summary>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <param name="name">The name.</param>
        /// <param name="geometry">The geometry.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery GeoIntersects<TCoordinates>(string name, GeoJsonGeometry<TCoordinates> geometry)
            where TCoordinates : GeoJsonCoordinates
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (geometry == null)
            {
                throw new ArgumentNullException("geometry");
            }

            var geoDoc = new BsonDocument("$geometry", BsonDocumentWrapper.Create(geometry));
            var condition = new BsonDocument("$geoIntersects", geoDoc);

            return Query.Create(name, condition);
        }

        /// <summary>
        /// Tests that the value of the named element is greater than some value (see $gt).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery GT(string name, BsonValue value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            return Query.Create(name, new BsonDocument("$gt", value));
        }

        /// <summary>
        /// Tests that the value of the named element is greater than or equal to some value (see $gte).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery GTE(string name, BsonValue value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            return Query.Create(name, new BsonDocument("$gte", value));
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery In(string name, IEnumerable<BsonValue> values)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            return Query.Create(name, new BsonDocument("$in", new BsonArray(values)));
        }

        /// <summary>
        /// Tests that the value of the named element is less than some value (see $lt).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery LT(string name, BsonValue value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            return Query.Create(name, new BsonDocument("$lt", value));
        }

        /// <summary>
        /// Tests that the value of the named element is less than or equal to some value (see $lte).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery LTE(string name, BsonValue value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            return Query.Create(name, new BsonDocument("$lte", value));
        }

        /// <summary>
        /// Tests that the value of the named element matches a regular expression (see $regex).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="regex">The regex.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Matches(string name, BsonRegularExpression regex)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (regex == null)
            {
                throw new ArgumentNullException("regex");
            }

            return Query.Create(name, regex);
        }

        /// <summary>
        /// Tests that the modulus of the value of the named element matches some value (see $mod).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="modulus">The modulus.</param>
        /// <param name="value">The value.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Mod(string name, long modulus, long value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            BsonDocument condition;
            if (modulus >= int.MinValue && modulus <= int.MaxValue &&
                value >= int.MinValue && value <= int.MaxValue)
            {
                condition = new BsonDocument("$mod", new BsonArray { (int)modulus, (int)value });
            }
            else
            {
                condition = new BsonDocument("$mod", new BsonArray { modulus, value });
            }
            return Query.Create(name, condition);
        }

        /// <summary>
        /// Tests that the value of the named element is near a point (see $near).
        /// </summary>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="point">The point.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Near<TCoordinates>(string name, GeoJsonPoint<TCoordinates> point)
            where TCoordinates : GeoJsonCoordinates
        {
            return Near(name, point, double.MaxValue);
        }

        /// <summary>
        /// Tests that the value of the named element is near some location (see $near).
        /// </summary>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="point">The point.</param>
        /// <param name="maxDistance">The max distance.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Near<TCoordinates>(string name, GeoJsonPoint<TCoordinates> point, double maxDistance)
            where TCoordinates : GeoJsonCoordinates
        {
            return Near(name, point, maxDistance, false);
        }

        /// <summary>
        /// Tests that the value of the named element is near some location (see $near).
        /// </summary>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="point">The point.</param>
        /// <param name="maxDistance">The max distance.</param>
        /// <param name="spherical">if set to <c>true</c> then the query will be translated to $nearSphere.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Near<TCoordinates>(string name, GeoJsonPoint<TCoordinates> point, double maxDistance, bool spherical)
            where TCoordinates : GeoJsonCoordinates
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (point == null)
            {
                throw new ArgumentNullException("point");
            }

            var op = spherical ? "$nearSphere" : "$near";
            var geometry = new BsonDocument("$geometry", BsonDocumentWrapper.Create(point));
            if (maxDistance != double.MaxValue)
            {
                geometry.Add("$maxDistance", maxDistance);
            }
            var condition = new BsonDocument(op, geometry);

            return Query.Create(name, condition);
        }

        /// <summary>
        /// Tests that the value of the named element is near some location (see $near).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="x">The x value of the origin.</param>
        /// <param name="y">The y value of the origin.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Near(string name, double x, double y)
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
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Near(string name, double x, double y, double maxDistance)
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
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Near(string name, double x, double y, double maxDistance, bool spherical)
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

            return Query.Create(name, condition);
        }

        /// <summary>
        /// Tests that the inverse of the query is true (see $not).
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Not(IMongoQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            return NegateQuery(query.ToBsonDocument());
        }

        /// <summary>
        /// Tests that an element does not equal the value (see $ne).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="value">The value.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery NE(string name, BsonValue value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            return Query.Create(name, new BsonDocument("$ne", value));
        }

        /// <summary>
        /// Tests that an element of that name does not exist (see $exists).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery NotExists(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            return Query.Create(name, new BsonDocument("$exists", false));
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any item in a list of values (see $nin).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">The values to compare.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery NotIn(string name, IEnumerable<BsonValue> values)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            return Query.Create(name, new BsonDocument("$nin", new BsonArray(values)));
        }

        /// <summary>
        /// Tests that at least one of the subqueries is true (see $or).
        /// </summary>
        /// <param name="queries">The subqueries.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Or(IEnumerable<IMongoQuery> queries)
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
                    if (queryDocument.ElementCount != 0)
                    {
                        queryArray.Add(queryDocument);
                    }
                    else
                    {
                        // if any query is { } (which matches everything) then the overall Or matches everything also
                        return Query.Empty;
                    }
                }
            }

            switch (queryArray.Count)
            {
                case 0:
                    return Query.Empty; // all queries were empty so just return an empty query
                case 1:
                    return Query.Create(queryArray[0].AsBsonDocument);
                default:
                    return Query.Create("$or", queryArray);
            }
        }

        /// <summary>
        /// Tests that at least one of the subqueries is true (see $or).
        /// </summary>
        /// <param name="queries">The subqueries.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Or(params IMongoQuery[] queries)
        {
            return Or((IEnumerable<IMongoQuery>)queries);
        }

        /// <summary>
        /// Tests that the size of the named array is equal to some value (see $size).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="size">The size to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Size(string name, int size)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            var condition = new BsonDocument("$size", size);
            return Query.Create(name, condition);
        }

        /// <summary>
        /// Tests that the size of the named array is greater than some value.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="size">The size to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery SizeGreaterThan(string name, int size)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            var elementName = string.Format("{0}.{1}", name, size);
            var condition = new BsonDocument("$exists", true);
            return Query.Create(elementName, condition);
        }

        /// <summary>
        /// Tests that the size of the named array is greater than or equal to some value.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="size">The size to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery SizeGreaterThanOrEqual(string name, int size)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            var elementName = string.Format("{0}.{1}", name, size - 1);
            var condition = new BsonDocument("$exists", true);
            return Query.Create(elementName, condition);
        }

        /// <summary>
        /// Tests that the size of the named array is less than some value.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="size">The size to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery SizeLessThan(string name, int size)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            var elementName = string.Format("{0}.{1}", name, size - 1);
            var condition = new BsonDocument("$exists", false);
            return Query.Create(elementName, condition);
        }

        /// <summary>
        /// Tests that the size of the named array is less than or equal to some value.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="size">The size to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery SizeLessThanOrEqual(string name, int size)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            var elementName = string.Format("{0}.{1}", name, size);
            var condition = new BsonDocument("$exists", false);
            return Query.Create(elementName, condition);
        }

        /// <summary>
        /// Tests that the type of the named element is equal to some type (see $type).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="type">The type to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Type(string name, BsonType type)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            var condition = new BsonDocument("$type", (int)type);
            return Query.Create(name, condition);
        }

        /// <summary>
        /// Tests that the type of the named element is equal to some type (see $type).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="type">The type to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Type(string name, string type)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            var condition = new BsonDocument("$type", type);
            return Query.Create(name, condition);
        }

        /// <summary>
        /// Tests that a JavaScript expression is true (see $where).
        /// </summary>
        /// <param name="javascript">The javascript.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Where(BsonJavaScript javascript)
        {
            if (javascript == null)
            {
                throw new ArgumentNullException("javascript");
            }

            return Query.Create("$where", javascript);
        }

        /// <summary>
        /// Tests that the value of the named element is within the specified geometry (see $within).
        /// </summary>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="polygon">The polygon.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Within<TCoordinates>(string name, GeoJsonPolygon<TCoordinates> polygon)
            where TCoordinates : GeoJsonCoordinates
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (polygon == null)
            {
                throw new ArgumentNullException("polygon");
            }

            var geoDoc = new BsonDocument("$geometry", BsonDocumentWrapper.Create(polygon));
            var condition = new BsonDocument("$within", geoDoc);

            return Query.Create(name, condition);
        }

        /// <summary>
        /// Tests that the value of the named element is within a circle (see $within and $center).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="centerX">The x coordinate of the origin.</param>
        /// <param name="centerY">The y coordinate of the origin.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery WithinCircle(string name, double centerX, double centerY, double radius)
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
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery WithinCircle(string name, double centerX, double centerY, double radius, bool spherical)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            var shape = spherical ? "$centerSphere" : "$center";
            var condition = new BsonDocument("$within", new BsonDocument(shape, new BsonArray { new BsonArray { centerX, centerY }, radius }));
            return Query.Create(name, condition);
        }

        /// <summary>
        /// Tests that the value of the named element is within a polygon (see $within and $polygon).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="points">An array of points that defines the polygon (the second dimension must be of length 2).</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery WithinPolygon(string name, double[,] points)
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
            return Query.Create(name, condition);
        }

        /// <summary>
        /// Tests that the value of the named element is within a rectangle (see $within and $box).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="lowerLeftX">The x coordinate of the lower left corner.</param>
        /// <param name="lowerLeftY">The y coordinate of the lower left corner.</param>
        /// <param name="upperRightX">The x coordinate of the upper right corner.</param>
        /// <param name="upperRightY">The y coordinate of the upper right corner.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery WithinRectangle(string name, double lowerLeftX, double lowerLeftY, double upperRightX, double upperRightY)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            var condition = new BsonDocument("$within", new BsonDocument("$box", new BsonArray { new BsonArray { lowerLeftX, lowerLeftY }, new BsonArray { upperRightX, upperRightY } }));
            return Query.Create(name, condition);
        }

        /// <summary>
        /// Generate a text search query that tests whether the given search string is present.
        /// </summary>
        /// <param name="searchString">The search string.</param>
        /// <returns>An IMongoQuery that represents the text search.</returns>
        public static IMongoQuery Text(string searchString)
        {
            return Text(searchString, new TextSearchOptions());
        }

        /// <summary>
        /// Generate a text search query that tests whether the given search string is present using the specified language's rules. 
        /// Specifies use of language appropriate stop words, stemming rules etc.
        /// </summary>
        /// <param name="searchString">The search string.</param>
        /// <param name="language">The language to restrict the search by.</param>
        /// <returns>An IMongoQuery that represents the text search for the particular language.</returns>
        public static IMongoQuery Text(string searchString, string language)
        {
            if (searchString == null)
            {
                throw new ArgumentNullException("searchString");
            }
            var options = new TextSearchOptions { Language = language };
            return Text(searchString, options);
        }

        /// <summary>
        /// Generate a text search query that tests whether the given search string is present using the specified language's rules. 
        /// Specifies use of language appropriate stop words, stemming rules etc.
        /// </summary>
        /// <param name="searchString">The search string.</param>
        /// <param name="options">The text search options.</param>
        /// <returns>An IMongoQuery that represents the text search for the particular language.</returns>
        public static IMongoQuery Text(string searchString, TextSearchOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }
            var condition = new BsonDocument
            {
                { "$search", searchString },
                { "$language", options.Language, options.Language != null },
                { "$caseSensitive", () => options.CaseSensitive.Value, options.CaseSensitive.HasValue },
                { "$diacriticSensitive", () => options.DiacriticSensitive.Value, options.DiacriticSensitive.HasValue }
            };
            return Query.Create("$text", condition);
        }

        // private methods
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

        private static IMongoQuery NegateArbitraryQuery(BsonDocument query)
        {
            // $not only works as a meta operator on a single operator so simulate Not using $nor
            return Query.Create("$nor", new BsonArray { query });
        }

        private static IMongoQuery NegateQuery(BsonDocument query)
        {
            if (query.ElementCount == 1)
            {
                return NegateSingleElementQuery(query, query.GetElement(0));
            }
            else
            {
                return NegateArbitraryQuery(query);
            }
        }

        private static IMongoQuery NegateSingleElementQuery(BsonDocument query, BsonElement element)
        {
            if (element.Name[0] == '$')
            {
                return NegateSingleTopLevelOperatorQuery(query, element.Name, element.Value);
            }
            else
            {
                return NegateSingleFieldQuery(query, element.Name, element.Value);
            }
        }

        private static IMongoQuery NegateSingleFieldOperatorQuery(BsonDocument query, string fieldName, string operatorName, BsonValue args)
        {
            switch (operatorName)
            {
                case "$exists":
                    return Query.Create(fieldName, new BsonDocument("$exists", !args.AsBoolean));
                case "$in":
                    return Query.Create(fieldName, new BsonDocument("$nin", args.AsBsonArray));
                case "$ne":
                case "$not":
                    return Query.Create(fieldName, args);
                case "$nin":
                    return Query.Create(fieldName, new BsonDocument("$in", args.AsBsonArray));
                default:
                    return Query.Create(fieldName, new BsonDocument("$not", new BsonDocument(operatorName, args)));
            }
        }

        private static IMongoQuery NegateSingleFieldQuery(BsonDocument query, string fieldName, BsonValue selector)
        {
            var selectorDocument = selector as BsonDocument;
            if (selectorDocument != null)
            {
                if (selectorDocument.ElementCount >= 1)
                {
                    var operatorName = selectorDocument.GetElement(0).Name;
                    if (operatorName[0] == '$' && operatorName != "$ref")
                    {
                        if (selectorDocument.ElementCount == 1)
                        {
                            return NegateSingleFieldOperatorQuery(query, fieldName, operatorName, selectorDocument[0]);
                        }
                        else
                        {
                            return NegateArbitraryQuery(query);
                        }
                    }
                }
            }

            return NegateSingleFieldValueQuery(query, fieldName, selector);
        }

        private static IMongoQuery NegateSingleFieldValueQuery(BsonDocument query, string fieldName, BsonValue value)
        {
            if (value.IsBsonRegularExpression)
            {
                return Query.Create(fieldName, new BsonDocument("$not", value));
            }
            else
            {
                // turn implied equality comparison into $ne
                return Query.Create(fieldName, new BsonDocument("$ne", value));
            }
        }

        private static IMongoQuery NegateSingleTopLevelOperatorQuery(BsonDocument query, string operatorName, BsonValue args)
        {
            switch (operatorName)
            {
                case "$or":
                    return Query.Create("$nor", args);
                case "$nor":
                    return Query.Create("$or", args);
                default:
                    return NegateArbitraryQuery(query);
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
    /// Aids in building mongo queries based on type information.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public static class Query<TDocument>
    {
        // public static methods
        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <typeparam name="TValue">The type of the enumerable member values.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery All<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, IEnumerable<TValue> values)
        {
            return new QueryBuilder<TDocument>().All(memberExpression, values);
        }

        /// <summary>
        /// Tests that the value of the named element has all of the specified bits clear.
        /// </summary>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="bitmask">The bitmask.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery BitsAllClear(Expression<Func<TDocument, object>> memberExpression, long bitmask)
        {
            return new QueryBuilder<TDocument>().BitsAllClear(memberExpression, bitmask);
        }

        /// <summary>
        /// Tests that the value of the named element has all of the specified bits set.
        /// </summary>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="bitmask">The bitmask.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery BitsAllSet(Expression<Func<TDocument, object>> memberExpression, long bitmask)
        {
            return new QueryBuilder<TDocument>().BitsAllSet(memberExpression, bitmask);
        }

        /// <summary>
        /// Tests that the value of the named element has any of the specified bits clear.
        /// </summary>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="bitmask">The bitmask.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery BitsAnyClear(Expression<Func<TDocument, object>> memberExpression, long bitmask)
        {
            return new QueryBuilder<TDocument>().BitsAnyClear(memberExpression, bitmask);
        }

        /// <summary>
        /// Tests that the value of the named element has any of the specified bits set.
        /// </summary>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="bitmask">The bitmask.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery BitsAnySet(Expression<Func<TDocument, object>> memberExpression, long bitmask)
        {
            return new QueryBuilder<TDocument>().BitsAnySet(memberExpression, bitmask);
        }

        /// <summary>
        /// Tests that at least one item of the named array element matches a query (see $elemMatch).
        /// </summary>
        /// <typeparam name="TValue">The type of the enumerable member values.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="elementQueryBuilderFunction">A function that builds a query using the supplied query builder.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery ElemMatch<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, Func<QueryBuilder<TValue>, IMongoQuery> elementQueryBuilderFunction)
        {
            return new QueryBuilder<TDocument>().ElemMatch(memberExpression, elementQueryBuilderFunction);
        }

        /// <summary>
        /// Tests that the value of the named element is equal to some value.
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery EQ<TMember>(Expression<Func<TDocument, TMember>> memberExpression, TMember value)
        {
            return new QueryBuilder<TDocument>().EQ(memberExpression, value);
        }

        /// <summary>
        /// Tests that any of the values in the named array element is equal to some value.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery EQ<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, TValue value)
        {
            return new QueryBuilder<TDocument>().EQ(memberExpression, value);
        }

        /// <summary>
        /// Tests that an element of that name does or does not exist (see $exists).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Exists<TMember>(Expression<Func<TDocument, TMember>> memberExpression)
        {
            return new QueryBuilder<TDocument>().Exists(memberExpression);
        }

        /// <summary>
        /// Tests that a location element specified by name intersects with the geometry (see $geoIntersects).
        /// </summary>
        /// <typeparam name="TMember">The type of the member.</typeparam>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="geometry">The geometry.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery GeoIntersects<TMember, TCoordinates>(Expression<Func<TDocument, TMember>> memberExpression, GeoJsonGeometry<TCoordinates> geometry)
            where TCoordinates : GeoJsonCoordinates
        {
            return new QueryBuilder<TDocument>().GeoIntersects(memberExpression, geometry);
        }

        /// <summary>
        /// Tests that the value of the named element is greater than some value (see $gt).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery GT<TMember>(Expression<Func<TDocument, TMember>> memberExpression, TMember value)
        {
            return new QueryBuilder<TDocument>().GT(memberExpression, value);
        }

        /// <summary>
        /// Tests that any of the values in the named array element is greater than some value (see $lt).
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery GT<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, TValue value)
        {
            return new QueryBuilder<TDocument>().GT(memberExpression, value);
        }

        /// <summary>
        /// Tests that the value of the named element is greater than or equal to some value (see $gte).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery GTE<TMember>(Expression<Func<TDocument, TMember>> memberExpression, TMember value)
        {
            return new QueryBuilder<TDocument>().GTE(memberExpression, value);
        }

        /// <summary>
        /// Tests that any of the values in the named array element is greater than or equal to some value (see $gte).
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery GTE<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, TValue value)
        {
            return new QueryBuilder<TDocument>().GTE(memberExpression, value);
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery In<TMember>(Expression<Func<TDocument, TMember>> memberExpression, IEnumerable<TMember> values)
        {
            return new QueryBuilder<TDocument>().In(memberExpression, values);
        }

        /// <summary>
        /// Tests that any of the values in the named array element are equal to one of a list of values (see $in).
        /// </summary>
        /// <typeparam name="TValue">The type of the enumerable member values.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery In<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, IEnumerable<TValue> values)
        {
            return new QueryBuilder<TDocument>().In(memberExpression, values);
        }

        /// <summary>
        /// Tests that the value of the named element is less than some value (see $lt).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery LT<TMember>(Expression<Func<TDocument, TMember>> memberExpression, TMember value)
        {
            return new QueryBuilder<TDocument>().LT(memberExpression, value);
        }

        /// <summary>
        /// Tests that any of the values in the named array element is less than some value (see $lt).
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery LT<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, TValue value)
        {
            return new QueryBuilder<TDocument>().LT(memberExpression, value);
        }

        /// <summary>
        /// Tests that the value of the named element is less than or equal to some value (see $lte).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery LTE<TMember>(Expression<Func<TDocument, TMember>> memberExpression, TMember value)
        {
            return new QueryBuilder<TDocument>().LTE(memberExpression, value);
        }

        /// <summary>
        /// Tests that any of the values in the named array element is less than or equal to some value (see $lte).
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery LTE<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, TValue value)
        {
            return new QueryBuilder<TDocument>().LTE(memberExpression, value);
        }

        /// <summary>
        /// Tests that the value of the named element matches a regular expression (see $regex).
        /// </summary>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="regex">The regex.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Matches(Expression<Func<TDocument, string>> memberExpression, BsonRegularExpression regex)
        {
            return new QueryBuilder<TDocument>().Matches(memberExpression, regex);
        }

        /// <summary>
        /// Tests that any of the values in the named array element matches a regular expression (see $regex).
        /// </summary>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="regex">The regex.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Matches(Expression<Func<TDocument, IEnumerable<string>>> memberExpression, BsonRegularExpression regex)
        {
            return new QueryBuilder<TDocument>().Matches(memberExpression, regex);
        }

        /// <summary>
        /// Tests that the modulus of the value of the named element matches some value (see $mod).
        /// </summary>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="modulus">The modulus.</param>
        /// <param name="value">The value.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Mod(Expression<Func<TDocument, int>> memberExpression, long modulus, long value)
        {
            return new QueryBuilder<TDocument>().Mod(memberExpression, modulus, value);
        }

        /// <summary>
        /// Tests that the any of the values in the named array element match some value (see $mod).
        /// </summary>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="modulus">The modulus.</param>
        /// <param name="value">The value.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Mod(Expression<Func<TDocument, IEnumerable<int>>> memberExpression, long modulus, long value)
        {
            return new QueryBuilder<TDocument>().Mod(memberExpression, modulus, value);
        }

        /// <summary>
        /// Tests that the value of the named element is near a point (see $near).
        /// </summary>
        /// <typeparam name="TMember">The type of the member.</typeparam>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="point">The point.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Near<TMember, TCoordinates>(Expression<Func<TDocument, TMember>> memberExpression, GeoJsonPoint<TCoordinates> point)
            where TCoordinates : GeoJsonCoordinates
        {
            return new QueryBuilder<TDocument>().Near(memberExpression, point);
        }

        /// <summary>
        /// Tests that the value of the named element is near some location (see $near).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="point">The point.</param>
        /// <param name="maxDistance">The max distance.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Near<TMember, TCoordinates>(Expression<Func<TDocument, TMember>> memberExpression, GeoJsonPoint<TCoordinates> point, double maxDistance)
            where TCoordinates : GeoJsonCoordinates
        {
            return new QueryBuilder<TDocument>().Near(memberExpression, point, maxDistance);
        }

        /// <summary>
        /// Tests that the value of the named element is near some location (see $near).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="point">The point.</param>
        /// <param name="maxDistance">The max distance.</param>
        /// <param name="spherical">if set to <c>true</c> [spherical].</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Near<TMember, TCoordinates>(Expression<Func<TDocument, TMember>> memberExpression, GeoJsonPoint<TCoordinates> point, double maxDistance, bool spherical)
            where TCoordinates : GeoJsonCoordinates
        {
            return new QueryBuilder<TDocument>().Near(memberExpression, point, maxDistance, spherical);
        }

        /// <summary>
        /// Tests that the value of the named element is near some location (see $near).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="x">The x value of the origin.</param>
        /// <param name="y">The y value of the origin.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Near<TMember>(Expression<Func<TDocument, TMember>> memberExpression, double x, double y)
        {
            return new QueryBuilder<TDocument>().Near(memberExpression, x, y);
        }

        /// <summary>
        /// Tests that the value of the named element is near some location (see $near).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="x">The x value of the origin.</param>
        /// <param name="y">The y value of the origin.</param>
        /// <param name="maxDistance">The max distance.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Near<TMember>(Expression<Func<TDocument, TMember>> memberExpression, double x, double y, double maxDistance)
        {
            return new QueryBuilder<TDocument>().Near(memberExpression, x, y, maxDistance);
        }

        /// <summary>
        /// Tests that the value of the named element is near some location (see $near).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="x">The x value of the origin.</param>
        /// <param name="y">The y value of the origin.</param>
        /// <param name="maxDistance">The max distance.</param>
        /// <param name="spherical">if set to <c>true</c> [spherical].</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Near<TMember>(Expression<Func<TDocument, TMember>> memberExpression, double x, double y, double maxDistance, bool spherical)
        {
            return new QueryBuilder<TDocument>().Near(memberExpression, x, y, maxDistance, spherical);
        }

        /// <summary>
        /// Tests that an element does not equal the value (see $ne).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="value">The value.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery NE<TMember>(Expression<Func<TDocument, TMember>> memberExpression, TMember value)
        {
            return new QueryBuilder<TDocument>().NE(memberExpression, value);
        }

        /// <summary>
        /// Tests that none of the values in the named array element is equal to some value (see $ne).
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery NE<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, TValue value)
        {
            return new QueryBuilder<TDocument>().NE(memberExpression, value);
        }

        /// <summary>
        /// Tests that an element of that name does not exist (see $exists).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery NotExists<TMember>(Expression<Func<TDocument, TMember>> memberExpression)
        {
            return new QueryBuilder<TDocument>().NotExists(memberExpression);
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any item in a list of values (see $nin).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery NotIn<TMember>(Expression<Func<TDocument, TMember>> memberExpression, IEnumerable<TMember> values)
        {
            return new QueryBuilder<TDocument>().NotIn(memberExpression, values);
        }

        /// <summary>
        /// Tests that the none of the values of the named array element is equal to any item in a list of values (see $nin).
        /// </summary>
        /// <typeparam name="TValue">The type of the enumerable member values.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="values">The values to compare.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery NotIn<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, IEnumerable<TValue> values)
        {
            return new QueryBuilder<TDocument>().NotIn(memberExpression, values);
        }

        /// <summary>
        /// Tests that the size of the named array is equal to some value (see $size).
        /// </summary>
        /// <typeparam name="TValue">The type of the enumerable member values.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="size">The size to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Size<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, int size)
        {
            return new QueryBuilder<TDocument>().Size(memberExpression, size);
        }

        /// <summary>
        /// Tests that the type of the named element is equal to some type (see $type).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="type">The type to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Type<TMember>(Expression<Func<TDocument, TMember>> memberExpression, BsonType type)
        {
            return new QueryBuilder<TDocument>().Type(memberExpression, type);
        }

        /// <summary>
        /// Tests that the type of the named element is equal to some type (see $type).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="type">The type to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Type<TMember>(Expression<Func<TDocument, TMember>> memberExpression, string type)
        {
            return new QueryBuilder<TDocument>().Type(memberExpression, type);
        }

        /// <summary>
        /// Tests that any of the values in the named array element is equal to some type (see $type).
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="type">The type to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Type<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, BsonType type)
        {
            return new QueryBuilder<TDocument>().Type(memberExpression, type);
        }

        /// <summary>
        /// Tests that any of the values in the named array element is equal to some type (see $type).
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="type">The type to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Type<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, string type)
        {
            return new QueryBuilder<TDocument>().Type(memberExpression, type);
        }

        /// <summary>
        /// Builds a query from an expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Where(Expression<Func<TDocument, bool>> expression)
        {
            return new QueryBuilder<TDocument>().Where(expression);
        }

        /// <summary>
        /// Tests that the value of the named element is within the specified geometry (see $within).
        /// </summary>
        /// <typeparam name="TMember">The type of the member.</typeparam>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="polygon">The polygon.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Within<TMember, TCoordinates>(Expression<Func<TDocument, TMember>> memberExpression, GeoJsonPolygon<TCoordinates> polygon)
            where TCoordinates : GeoJsonCoordinates
        {
            return new QueryBuilder<TDocument>().Within(memberExpression, polygon);
        }

        /// <summary>
        /// Tests that the value of the named element is within a circle (see $within and $center).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="centerX">The x coordinate of the origin.</param>
        /// <param name="centerY">The y coordinate of the origin.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery WithinCircle<TMember>(Expression<Func<TDocument, TMember>> memberExpression, double centerX, double centerY, double radius)
        {
            return new QueryBuilder<TDocument>().WithinCircle(memberExpression, centerX, centerY, radius);
        }

        /// <summary>
        /// Tests that the value of the named element is within a circle (see $within and $center).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="centerX">The x coordinate of the origin.</param>
        /// <param name="centerY">The y coordinate of the origin.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="spherical">if set to <c>true</c> [spherical].</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery WithinCircle<TMember>(Expression<Func<TDocument, TMember>> memberExpression, double centerX, double centerY, double radius, bool spherical)
        {
            return new QueryBuilder<TDocument>().WithinCircle(memberExpression, centerX, centerY, radius, spherical);
        }

        /// <summary>
        /// Tests that the value of the named element is within a polygon (see $within and $polygon).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="points">An array of points that defines the polygon (the second dimension must be of length 2).</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery WithinPolygon<TMember>(Expression<Func<TDocument, TMember>> memberExpression, double[,] points)
        {
            return new QueryBuilder<TDocument>().WithinPolygon(memberExpression, points);
        }

        /// <summary>
        /// Tests that the value of the named element is within a rectangle (see $within and $box).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="lowerLeftX">The x coordinate of the lower left corner.</param>
        /// <param name="lowerLeftY">The y coordinate of the lower left corner.</param>
        /// <param name="upperRightX">The x coordinate of the upper right corner.</param>
        /// <param name="upperRightY">The y coordinate of the upper right corner.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery WithinRectangle<TMember>(Expression<Func<TDocument, TMember>> memberExpression, double lowerLeftX, double lowerLeftY, double upperRightX, double upperRightY)
        {
            return new QueryBuilder<TDocument>()
                .WithinRectangle(memberExpression, lowerLeftX, lowerLeftY, upperRightX, upperRightY);
        }
    }

    /// <summary>
    /// Aids in building mongo queries based on type information.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public class QueryBuilder<TDocument>
    {
        // private fields
        private readonly BsonSerializationInfoHelper _serializationInfoHelper;
        private readonly PredicateTranslator _predicateTranslator;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryBuilder{TDocument}"/> class.
        /// </summary>
        public QueryBuilder()
            : this(new BsonSerializationInfoHelper())
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryBuilder{TDocument}"/> class.
        /// </summary>
        /// <param name="serializationInfoHelper">The serialization info helper.</param>
        internal QueryBuilder(BsonSerializationInfoHelper serializationInfoHelper)
        {
            _serializationInfoHelper = serializationInfoHelper;
            _predicateTranslator = new PredicateTranslator(_serializationInfoHelper);
        }

        // public methods
        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <typeparam name="TValue">The type of the enumerable member values.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery All<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, IEnumerable<TValue> values)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            var itemSerializationInfo = _serializationInfoHelper.GetItemSerializationInfo("All", serializationInfo);
            var serializedValues = _serializationInfoHelper.SerializeValues(itemSerializationInfo, values);
            return Query.All(serializationInfo.ElementName, serializedValues);
        }

        /// <summary>
        /// Tests that all the queries are true (see $and in newer versions of the server).
        /// </summary>
        /// <param name="queries">A list of subqueries.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery And(IEnumerable<IMongoQuery> queries)
        {
            return Query.And(queries);
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
        /// Tests that the value of the named element has all of the specified bits clear.
        /// </summary>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="bitmask">The bitmask.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery BitsAllClear(Expression<Func<TDocument, object>> memberExpression, long bitmask)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            return Query.BitsAllClear(serializationInfo.ElementName, bitmask);
        }

        /// <summary>
        /// Tests that the value of the named element has all of the specified bits set.
        /// </summary>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="bitmask">The bitmask.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery BitsAllSet(Expression<Func<TDocument, object>> memberExpression, long bitmask)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            return Query.BitsAllSet(serializationInfo.ElementName, bitmask);
        }

        /// <summary>
        /// Tests that the value of the named element has any of the specified bits clear.
        /// </summary>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="bitmask">The bitmask.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery BitsAnyClear(Expression<Func<TDocument, object>> memberExpression, long bitmask)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            return Query.BitsAnyClear(serializationInfo.ElementName, bitmask);
        }

        /// <summary>
        /// Tests that the value of the named element has any of the specified bits set.
        /// </summary>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="bitmask">The bitmask.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery BitsAnySet(Expression<Func<TDocument, object>> memberExpression, long bitmask)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            return Query.BitsAnySet(serializationInfo.ElementName, bitmask);
        }

        /// <summary>
        /// Tests that at least one item of the named array element matches a query (see $elemMatch).
        /// </summary>
        /// <typeparam name="TValue">The type of the enumerable member values.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="elementQueryBuilderFunction">A function that builds a query using the supplied query builder.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery ElemMatch<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, Func<QueryBuilder<TValue>, IMongoQuery> elementQueryBuilderFunction)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }
            if (elementQueryBuilderFunction == null)
            {
                throw new ArgumentNullException("elementQueryBuilderFunction");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            _serializationInfoHelper.GetItemSerializationInfo("ElemMatch", serializationInfo); // TODO: there must be a better way to do whatever this line is doing
            var elementQueryBuilder = new QueryBuilder<TValue>(_serializationInfoHelper);
            var elementQuery = elementQueryBuilderFunction(elementQueryBuilder);
            return Query.ElemMatch(serializationInfo.ElementName, elementQuery);
        }

        /// <summary>
        /// Tests that the value of the named element is equal to some value.
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery EQ<TMember>(Expression<Func<TDocument, TMember>> memberExpression, TMember value)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            var serializedValue = _serializationInfoHelper.SerializeValue(serializationInfo, value);
            return Query.EQ(serializationInfo.ElementName, serializedValue);
        }

        /// <summary>
        /// Tests that any of the values in the named array element is equal to some value.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery EQ<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, TValue value)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            var itemSerializationInfo = _serializationInfoHelper.GetItemSerializationInfo("EQ", serializationInfo);
            var serializedValue = _serializationInfoHelper.SerializeValue(itemSerializationInfo, value);
            return Query.EQ(serializationInfo.ElementName, serializedValue);
        }

        /// <summary>
        /// Tests that an element of that name does or does not exist (see $exists).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery Exists<TMember>(Expression<Func<TDocument, TMember>> memberExpression)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            return Query.Exists(serializationInfo.ElementName);
        }

        /// <summary>
        /// Tests that a location element specified by name intersects with the geometry (see $geoIntersects).
        /// </summary>
        /// <typeparam name="TMember">The type of the member.</typeparam>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="geometry">The geometry.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery GeoIntersects<TMember, TCoordinates>(Expression<Func<TDocument, TMember>> memberExpression, GeoJsonGeometry<TCoordinates> geometry)
            where TCoordinates : GeoJsonCoordinates
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }
            if (geometry == null)
            {
                throw new ArgumentNullException("geometry");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            return Query.GeoIntersects<TCoordinates>(serializationInfo.ElementName, geometry);
        }

        /// <summary>
        /// Tests that the value of the named element is greater than some value (see $gt).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery GT<TMember>(Expression<Func<TDocument, TMember>> memberExpression, TMember value)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            var serializedValue = _serializationInfoHelper.SerializeValue(serializationInfo, value);
            return Query.GT(serializationInfo.ElementName, serializedValue);
        }

        /// <summary>
        /// Tests that any of the values in the named array element is greater than some value (see $lt).
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery GT<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, TValue value)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            var itemSerializationInfo = _serializationInfoHelper.GetItemSerializationInfo("GT", serializationInfo);
            var serializedValue = _serializationInfoHelper.SerializeValue(itemSerializationInfo, value);
            return Query.GT(serializationInfo.ElementName, serializedValue);
        }

        /// <summary>
        /// Tests that the value of the named element is greater than or equal to some value (see $gte).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery GTE<TMember>(Expression<Func<TDocument, TMember>> memberExpression, TMember value)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            var serializedValue = _serializationInfoHelper.SerializeValue(serializationInfo, value);
            return Query.GTE(serializationInfo.ElementName, serializedValue);
        }

        /// <summary>
        /// Tests that any of the values in the named array element is greater than or equal to some value (see $gte).
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery GTE<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, TValue value)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            var itemSerializationInfo = _serializationInfoHelper.GetItemSerializationInfo("GTE", serializationInfo);
            var serializedValue = _serializationInfoHelper.SerializeValue(itemSerializationInfo, value);
            return Query.GTE(serializationInfo.ElementName, serializedValue);
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery In<TMember>(Expression<Func<TDocument, TMember>> memberExpression, IEnumerable<TMember> values)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            var serializedValues = _serializationInfoHelper.SerializeValues(serializationInfo, values);
            return Query.In(serializationInfo.ElementName, serializedValues);
        }

        /// <summary>
        /// Tests that any of the values in the named array element are equal to one of a list of values (see $in).
        /// </summary>
        /// <typeparam name="TValue">The type of the enumerable member values.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery In<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, IEnumerable<TValue> values)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            var itemSerializationInfo = _serializationInfoHelper.GetItemSerializationInfo("In", serializationInfo);
            var serializedValues = _serializationInfoHelper.SerializeValues(itemSerializationInfo, values);
            return Query.In(serializationInfo.ElementName, serializedValues);
        }

        /// <summary>
        /// Tests that the value of the named element is less than some value (see $lt).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery LT<TMember>(Expression<Func<TDocument, TMember>> memberExpression, TMember value)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            var serializedValue = _serializationInfoHelper.SerializeValue(serializationInfo, value);
            return Query.LT(serializationInfo.ElementName, serializedValue);
        }

        /// <summary>
        /// Tests that any of the values in the named array element is less than some value (see $lt).
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery LT<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, TValue value)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            var itemSerializationInfo = _serializationInfoHelper.GetItemSerializationInfo("LT", serializationInfo);
            var serializedValue = _serializationInfoHelper.SerializeValue(itemSerializationInfo, value);
            return Query.LT(serializationInfo.ElementName, serializedValue);
        }

        /// <summary>
        /// Tests that the value of the named element is less than or equal to some value (see $lte).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery LTE<TMember>(Expression<Func<TDocument, TMember>> memberExpression, TMember value)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            var serializedValue = _serializationInfoHelper.SerializeValue(serializationInfo, value);
            return Query.LTE(serializationInfo.ElementName, serializedValue);
        }

        /// <summary>
        /// Tests that any of the values in the named array element is less than or equal to some value (see $lte).
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery LTE<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, TValue value)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            var itemSerializationInfo = _serializationInfoHelper.GetItemSerializationInfo("LTE", serializationInfo);
            var serializedValue = _serializationInfoHelper.SerializeValue(itemSerializationInfo, value);
            return Query.LTE(serializationInfo.ElementName, serializedValue);
        }

        /// <summary>
        /// Tests that the value of the named element matches a regular expression (see $regex).
        /// </summary>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="regex">The regex.</param>
        /// <returns>
        /// A query.
        /// </returns>
        public IMongoQuery Matches(Expression<Func<TDocument, string>> memberExpression, BsonRegularExpression regex)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }
            if (regex == null)
            {
                throw new ArgumentNullException("regex");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            return Query.Matches(serializationInfo.ElementName, regex);
        }

        /// <summary>
        /// Tests that any of the values in the named array element matches a regular expression (see $regex).
        /// </summary>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="regex">The regex.</param>
        /// <returns>
        /// A query.
        /// </returns>
        public IMongoQuery Matches(Expression<Func<TDocument, IEnumerable<string>>> memberExpression, BsonRegularExpression regex)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }
            if (regex == null)
            {
                throw new ArgumentNullException("regex");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            return Query.Matches(serializationInfo.ElementName, regex);
        }

        /// <summary>
        /// Tests that the modulus of the value of the named element matches some value (see $mod).
        /// </summary>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="modulus">The modulus.</param>
        /// <param name="value">The value.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery Mod(Expression<Func<TDocument, int>> memberExpression, long modulus, long value)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            return Query.Mod(serializationInfo.ElementName, modulus, value);
        }

        /// <summary>
        /// Tests that the any of the values in the named array element match some value (see $mod).
        /// </summary>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="modulus">The modulus.</param>
        /// <param name="value">The value.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery Mod(Expression<Func<TDocument, IEnumerable<int>>> memberExpression, long modulus, long value)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            return Query.Mod(serializationInfo.ElementName, modulus, value);
        }

        /// <summary>
        /// Tests that the value of the named element is near a point (see $near).
        /// </summary>
        /// <typeparam name="TMember">The type of the member.</typeparam>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="point">The point.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery Near<TMember, TCoordinates>(Expression<Func<TDocument, TMember>> memberExpression, GeoJsonPoint<TCoordinates> point)
            where TCoordinates : GeoJsonCoordinates
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }
            if (point == null)
            {
                throw new ArgumentNullException("point");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            return Query.Near(serializationInfo.ElementName, point);
        }

        /// <summary>
        /// Tests that the value of the named element is near some location (see $near).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="point">The point.</param>
        /// <param name="maxDistance">The max distance.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery Near<TMember, TCoordinates>(Expression<Func<TDocument, TMember>> memberExpression, GeoJsonPoint<TCoordinates> point, double maxDistance)
            where TCoordinates : GeoJsonCoordinates
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }
            if (point == null)
            {
                throw new ArgumentNullException("point");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            return Query.Near(serializationInfo.ElementName, point, maxDistance);
        }

        /// <summary>
        /// Tests that the value of the named element is near some location (see $near).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="point">The point.</param>
        /// <param name="maxDistance">The max distance.</param>
        /// <param name="spherical">if set to <c>true</c> then the query will be translated to $nearSphere.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery Near<TMember, TCoordinates>(Expression<Func<TDocument, TMember>> memberExpression, GeoJsonPoint<TCoordinates> point, double maxDistance, bool spherical)
            where TCoordinates : GeoJsonCoordinates
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }
            if (point == null)
            {
                throw new ArgumentNullException("point");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            return Query.Near(serializationInfo.ElementName, point, maxDistance, spherical);
        }

        /// <summary>
        /// Tests that the value of the named element is near some location (see $near).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="x">The x value of the origin.</param>
        /// <param name="y">The y value of the origin.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery Near<TMember>(Expression<Func<TDocument, TMember>> memberExpression, double x, double y)
        {
            return Near(memberExpression, x, y, double.MaxValue);
        }

        /// <summary>
        /// Tests that the value of the named element is near some location (see $near).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="x">The x value of the origin.</param>
        /// <param name="y">The y value of the origin.</param>
        /// <param name="maxDistance">The max distance.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery Near<TMember>(Expression<Func<TDocument, TMember>> memberExpression, double x, double y, double maxDistance)
        {
            return Near(memberExpression, x, y, maxDistance, false);
        }

        /// <summary>
        /// Tests that the value of the named element is near some location (see $near).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="x">The x value of the origin.</param>
        /// <param name="y">The y value of the origin.</param>
        /// <param name="maxDistance">The max distance.</param>
        /// <param name="spherical">if set to <c>true</c> [spherical].</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery Near<TMember>(Expression<Func<TDocument, TMember>> memberExpression, double x, double y, double maxDistance, bool spherical)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            return Query.Near(serializationInfo.ElementName, x, y, maxDistance, spherical);
        }

        /// <summary>
        /// Tests that the inverse of the query is true (see $not).
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery Not(IMongoQuery query)
        {
            return Query.Not(query);
        }

        /// <summary>
        /// Tests that an element does not equal the value (see $ne).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="value">The value.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery NE<TMember>(Expression<Func<TDocument, TMember>> memberExpression, TMember value)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            var serializedValue = _serializationInfoHelper.SerializeValue(serializationInfo, value);
            return Query.NE(serializationInfo.ElementName, serializedValue);
        }

        /// <summary>
        /// Tests that none of the values in the named array element is equal to some value (see $ne).
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery NE<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, TValue value)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            var itemSerializationInfo = _serializationInfoHelper.GetItemSerializationInfo("NE", serializationInfo);
            var serializedValue = _serializationInfoHelper.SerializeValue(itemSerializationInfo, value);
            return Query.NE(serializationInfo.ElementName, serializedValue);
        }

        /// <summary>
        /// Tests that an element of that name does not exist (see $exists).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery NotExists<TMember>(Expression<Func<TDocument, TMember>> memberExpression)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            return Query.NotExists(serializationInfo.ElementName);
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any item in a list of values (see $nin).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery NotIn<TMember>(Expression<Func<TDocument, TMember>> memberExpression, IEnumerable<TMember> values)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            var serializedValues = _serializationInfoHelper.SerializeValues(serializationInfo, values);
            return Query.NotIn(serializationInfo.ElementName, serializedValues);
        }

        /// <summary>
        /// Tests that the none of the values of the named array element is equal to any item in a list of values (see $nin).
        /// </summary>
        /// <typeparam name="TValue">The type of the enumerable member values.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="values">The values to compare.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery NotIn<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, IEnumerable<TValue> values)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            var itemSerializationInfo = _serializationInfoHelper.GetItemSerializationInfo("NotIn", serializationInfo);
            var serializedValues = _serializationInfoHelper.SerializeValues(itemSerializationInfo, values);
            return Query.NotIn(serializationInfo.ElementName, serializedValues);
        }

        /// <summary>
        /// Tests that at least one of the subqueries is true (see $or).
        /// </summary>
        /// <param name="queries">The subqueries.</param>
        /// <returns>
        /// A query.
        /// </returns>
        public IMongoQuery Or(IEnumerable<IMongoQuery> queries)
        {
            return Query.Or(queries);
        }

        /// <summary>
        /// Tests that at least one of the subqueries is true (see $or).
        /// </summary>
        /// <param name="queries">The subqueries.</param>
        /// <returns>
        /// A query.
        /// </returns>
        public IMongoQuery Or(params IMongoQuery[] queries)
        {
            return Or((IEnumerable<IMongoQuery>)queries);
        }

        /// <summary>
        /// Tests that the size of the named array is equal to some value (see $size).
        /// </summary>
        /// <typeparam name="TValue">The type of the enumerable member values.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="size">The size to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery Size<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, int size)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            return Query.Size(serializationInfo.ElementName, size);
        }

        /// <summary>
        /// Tests that the type of the named element is equal to some type (see $type).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="type">The type to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery Type<TMember>(Expression<Func<TDocument, TMember>> memberExpression, BsonType type)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            return Query.Type(serializationInfo.ElementName, type);
        }

        /// <summary>
        /// Tests that the type of the named element is equal to some type (see $type).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="type">The type to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery Type<TMember>(Expression<Func<TDocument, TMember>> memberExpression, string type)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            return Query.Type(serializationInfo.ElementName, type);
        }

        /// <summary>
        /// Tests that any of the values in the named array element is equal to some type (see $type).
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="type">The type to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery Type<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, BsonType type)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            return Query.Type(serializationInfo.ElementName, type);
        }

        /// <summary>
        /// Tests that any of the values in the named array element is equal to some type (see $type).
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="type">The type to compare to.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery Type<TValue>(Expression<Func<TDocument, IEnumerable<TValue>>> memberExpression, string type)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            return Query.Type(serializationInfo.ElementName, type);
        }

        /// <summary>
        /// Builds a query from an expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery Where(Expression<Func<TDocument, bool>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            var evaluatedExpression = PartialEvaluator.Evaluate(expression.Body);
            return _predicateTranslator.BuildQuery(evaluatedExpression);
        }

        /// <summary>
        /// Tests that the value of the named element is within the specified geometry (see $within).
        /// </summary>
        /// <typeparam name="TMember">The type of the member.</typeparam>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="polygon">The polygon.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery Within<TMember, TCoordinates>(Expression<Func<TDocument, TMember>> memberExpression, GeoJsonPolygon<TCoordinates> polygon)
            where TCoordinates : GeoJsonCoordinates
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }
            if (polygon == null)
            {
                throw new ArgumentNullException("polygon");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            return Query.Within(serializationInfo.ElementName, polygon);
        }

        /// <summary>
        /// Tests that the value of the named element is within a circle (see $within and $center).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="centerX">The x coordinate of the origin.</param>
        /// <param name="centerY">The y coordinate of the origin.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery WithinCircle<TMember>(Expression<Func<TDocument, TMember>> memberExpression, double centerX, double centerY, double radius)
        {
            return WithinCircle(memberExpression, centerX, centerY, radius, false);
        }

        /// <summary>
        /// Tests that the value of the named element is within a circle (see $within and $center).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="centerX">The x coordinate of the origin.</param>
        /// <param name="centerY">The y coordinate of the origin.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="spherical">if set to <c>true</c> [spherical].</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery WithinCircle<TMember>(Expression<Func<TDocument, TMember>> memberExpression, double centerX, double centerY, double radius, bool spherical)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            return Query.WithinCircle(serializationInfo.ElementName, centerX, centerY, radius, spherical);
        }

        /// <summary>
        /// Tests that the value of the named element is within a polygon (see $within and $polygon).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="points">An array of points that defines the polygon (the second dimension must be of length 2).</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery WithinPolygon<TMember>(Expression<Func<TDocument, TMember>> memberExpression, double[,] points)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            return Query.WithinPolygon(serializationInfo.ElementName, points);
        }

        /// <summary>
        /// Tests that the value of the named element is within a rectangle (see $within and $box).
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression representing the element to test.</param>
        /// <param name="lowerLeftX">The x coordinate of the lower left corner.</param>
        /// <param name="lowerLeftY">The y coordinate of the lower left corner.</param>
        /// <param name="upperRightX">The x coordinate of the upper right corner.</param>
        /// <param name="upperRightY">The y coordinate of the upper right corner.</param>
        /// <returns>An IMongoQuery.</returns>
        public IMongoQuery WithinRectangle<TMember>(Expression<Func<TDocument, TMember>> memberExpression, double lowerLeftX, double lowerLeftY, double upperRightX, double upperRightY)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            return Query.WithinRectangle(serializationInfo.ElementName, lowerLeftX, lowerLeftY, upperRightX, upperRightY);
        }
    }
}
