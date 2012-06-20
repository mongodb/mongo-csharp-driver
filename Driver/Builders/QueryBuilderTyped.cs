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
using System.Linq.Expressions;
using System.Text;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Utils;

namespace MongoDB.Driver.Builders
{
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
        public static IMongoQuery Mod(Expression<Func<TDocument, int>> memberExpression, int modulus, int value)
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
        public static IMongoQuery Mod(Expression<Func<TDocument, IEnumerable<int>>> memberExpression, int modulus, int value)
        {
            return new QueryBuilder<TDocument>().Mod(memberExpression, modulus, value);
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
        /// <typeparam name="TMember"></typeparam>
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
        /// Builds a query from an expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>An IMongoQuery.</returns>
        public static IMongoQuery Where(Expression<Func<TDocument, bool>> expression)
        {
            return new QueryBuilder<TDocument>().Where(expression);
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
        /// Initializes a new instance of the <see cref="QueryBuilder&lt;TDocument&gt;"/> class.
        /// </summary>
        public QueryBuilder()
            : this(new BsonSerializationInfoHelper())
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryBuilder&lt;TDocument&gt;"/> class.
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
            var itemSerializationInfo = _serializationInfoHelper.GetItemSerializationInfo("ElemMatch", serializationInfo);
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
        public IMongoQuery Mod(Expression<Func<TDocument, int>> memberExpression, int modulus, int value)
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
        public IMongoQuery Mod(Expression<Func<TDocument, IEnumerable<int>>> memberExpression, int modulus, int value)
        {
            if (memberExpression == null)
            {
                throw new ArgumentNullException("memberExpression");
            }

            var serializationInfo = _serializationInfoHelper.GetSerializationInfo(memberExpression);
            return Query.Mod(serializationInfo.ElementName, modulus, value);
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
        /// <returns></returns>
        public IMongoQuery Not(IMongoQuery query)
        {
            return Query.Not(query);
        }

        /// <summary>
        /// Tests that an element does not equal the value (see $ne).
        /// </summary>
        /// <typeparam name="TMember"></typeparam>
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