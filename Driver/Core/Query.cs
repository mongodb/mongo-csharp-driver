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
using System.Text;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Wrappers;

namespace MongoDB.Driver
{
    /// <summary>
    /// A helper class for creating queries.
    /// </summary>
    public static class Query
    {
        #region public static properties
        /// <summary>
        /// Gets a null value with a type of IMongoQuery.
        /// </summary>
        public static IMongoQuery Null
        {
            get { return null; }
        }
        #endregion

        #region public static methods
        /// <summary>
        /// Creates a query from an object.
        /// </summary>
        /// <param name="query">The wrapped object.</param>
        /// <returns>A new instance of a QueryWrapper or null.</returns>
        public static QueryWrapper Create(
            object query
        ) {
            return QueryWrapper.Create(query);
        }

        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList All(
            string name,
            BsonArray values
        ) {
            return new QueryConditionList(name).All(values);
        }

        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList All<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            BsonArray values
        ) {
            return Query.All(memberExpression.GetElementName(), values);
        }

        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList All(
            string name,
            IEnumerable<BsonValue> values
        ) {
            return new QueryConditionList(name).All(values);
        }

        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList All<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            IEnumerable<BsonValue> values
        ) {
            return Query.All(memberExpression.GetElementName(), values);
        }

        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="arg1">The first value to compare to.</param>
        /// <param name="arg2">The second value to compare to.</param>
        /// <param name="args">The additional values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList All(
            string name,
            BsonValue arg1,
            BsonValue arg2,
            params BsonValue[] args
        ) {
            return Query.All(name, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="arg1">The first value to compare to.</param>
        /// <param name="arg2">The second value to compare to.</param>
        /// <param name="args">The additional values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList All<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            BsonValue arg1,
            BsonValue arg2,
            params BsonValue[] args
        ) {
            return Query.All(memberExpression.GetElementName(), ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList All(
            string name,
            IEnumerable values
        ) {
            return new QueryConditionList(name).All(values);
        }

        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList All<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            IEnumerable values
        ) {
            return Query.All(memberExpression.GetElementName(), values);
        }

        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="arg1">The first value to compare to.</param>
        /// <param name="arg2">The second value to compare to.</param>
        /// <param name="args">The additional values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList All(
            string name,
            object arg1,
            object arg2,
            params object[] args
        ) {
            return Query.All(name, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Tests that the named array element contains all of the values (see $all).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="arg1">The first value to compare to.</param>
        /// <param name="arg2">The second value to compare to.</param>
        /// <param name="args">The additional values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList All<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            object arg1,
            object arg2,
            params object[] args
        ) {
            return Query.All(memberExpression.GetElementName(), ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Tests that all the subqueries are true (see $and in newer versions of the server).
        /// </summary>
        /// <param name="clauses">A list of subqueries.</param>
        /// <returns>A query.</returns>
        public static QueryComplete And(
            params IMongoQuery[] clauses
        ) {
            var query = new BsonDocument();
            foreach (var clause in clauses) {
                if (clause != null) {
                    foreach (var clauseElement in clause.ToBsonDocument()) {
                        AddAndClause(query, clauseElement);
                    }
                }
            }

            return query.ElementCount > 0 ? new QueryComplete(query) : null;
        }

        /// <summary>
        /// Tests that at least one item of the named array element matches a query (see $elemMatch).
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
        /// Tests that at least one item of the named array element matches a query (see $elemMatch).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="query">The query to match elements with.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList ElemMatch<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            IMongoQuery query
        ) {
            return Query.ElemMatch(memberExpression.GetElementName(), query);
        }

        /// <summary>
        /// Tests that the value of the named element is equal to some value.
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>A query.</returns>
        public static QueryComplete EQ(
            string name,
            object value
        ) {
            return new QueryComplete(new BsonDocument(name, BsonValue.Create(value)));
        }

        /// <summary>
        /// Tests that the value of the named element is equal to some value.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>A query.</returns>
        public static QueryComplete EQ<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            object value
        ) {
            return Query.EQ(memberExpression.GetElementName(), value);
        }

        /// <summary>
        /// Tests that an element of that name does or does not exist (see $exists).
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
        /// Tests that an element of that name does or does not exist (see $exists).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="exists">Whether to test for the existence or absence of an element.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList Exists<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            bool exists
        ) {
            return Query.Exists(memberExpression.GetElementName(), exists);
        }

        /// <summary>
        /// Tests that the value of the named element is greater than some value (see $gt).
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
        /// Tests that the value of the named element is greater than some value (see $gt).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList GT<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            BsonValue value
        ) {
            return Query.GT(memberExpression.GetElementName(), value);
        }

        /// <summary>
        /// Tests that the value of the named element is greater than or equal to some value (see $gte).
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
        /// Tests that the value of the named element is greater than or equal to some value (see $gte).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList GTE<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            BsonValue value
        ) {
            return Query.GTE(memberExpression.GetElementName(), value);
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList In(
            string name,
            BsonArray values
        ) {
            return new QueryConditionList(name).In(values);
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList In<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            BsonArray values
        ) {
            return Query.In(memberExpression.GetElementName(), values);
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList In(
            string name,
            IEnumerable<BsonValue> values
        ) {
            return new QueryConditionList(name).In(values);
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList In<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            IEnumerable<BsonValue> values
        ) {
            return In(memberExpression.GetElementName(), values);
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="arg1">The first value to compare to.</param>
        /// <param name="arg2">The second value to compare to.</param>
        /// <param name="args">The additional values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList In(
            string name,
            BsonValue arg1,
            BsonValue arg2,
            params BsonValue[] args
        ) {
            return In(name, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="arg1">The first value to compare to.</param>
        /// <param name="arg2">The second value to compare to.</param>
        /// <param name="args">The additional values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList In<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            BsonValue arg1,
            BsonValue arg2,
            params BsonValue[] args
        ) {
            return In(memberExpression.GetElementName(), ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList In(
            string name,
            IEnumerable values
        ) {
            return new QueryConditionList(name).In(values);
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList In<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            IEnumerable values
        ) {
            return In(memberExpression.GetElementName(), values);
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="arg1">The first value to compare to.</param>
        /// <param name="arg2">The second value to compare to.</param>
        /// <param name="args">The additional values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList In(
            string name,
            object arg1,
            object arg2,
            params object[] args
        ) {
            return In(name, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Tests that the value of the named element is equal to one of a list of values (see $in).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="arg1">The first value to compare to.</param>
        /// <param name="arg2">The second value to compare to.</param>
        /// <param name="args">The additional values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList In<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            object arg1,
            object arg2,
            params object[] args
        ) {
            return In(memberExpression.GetElementName(), ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Tests that the value of the named element is less than some value (see $lt).
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
        /// Tests that the value of the named element is less than some value (see $lt).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList LT<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            BsonValue value
        ) {
            return Query.LT(memberExpression.GetElementName(), value);
        }

        /// <summary>
        /// Tests that the value of the named element is less than or equal to some value (see $lte).
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
        /// Tests that the value of the named element is less than or equal to some value (see $lte).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList LTE<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            BsonValue value
        ) {
            return Query.LTE(memberExpression.GetElementName(), value);
        }

        /// <summary>
        /// Tests that the value of the named element matches a regular expression (see $regex).
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
        /// Tests that the value of the named element matches a regular expression (see $regex).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="regex">The regular expression to match against.</param>
        /// <returns>A query.</returns>
        public static QueryComplete Matches<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            BsonRegularExpression regex
        ) {
            return Query.Matches(memberExpression.GetElementName(), regex);
        }

        /// <summary>
        /// Tests that the modulus of the value of the named element matches some value (see $mod).
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
        /// Tests that the modulus of the value of the named element matches some value (see $mod).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="modulus">The modulus.</param>
        /// <param name="equals">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList Mod<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            int modulus,
            int equals
        ) {
            return Query.Mod(memberExpression.GetElementName(), modulus, equals);
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to some value (see $ne).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList NE(
            string name,
            object value
        ) {
            return new QueryConditionList(name).NE(value);
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to some value (see $ne).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The value to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList NE<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            object value
        ) {
            return Query.NE(memberExpression.GetElementName(), value);
        }

        /// <summary>
        /// Tests that the value of the named element is near some location (see $near).
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
        /// Tests that the value of the named element is near some location (see $near).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="x">The x value of the origin.</param>
        /// <param name="y">The y value of the origin.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList Near<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            double x,
            double y
        ) {
            return Query.Near(memberExpression.GetElementName(), x, y);
        }

        /// <summary>
        /// Tests that the value of the named element is near some location (see $near).
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
        /// Tests that the value of the named element is near some location (see $near).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="x">The x value of the origin.</param>
        /// <param name="y">The y value of the origin.</param>
        /// <param name="maxDistance">The max distance for a document to be included in the results.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList Near<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            double x,
            double y,
            double maxDistance
        ) {
            return Query.Near(memberExpression.GetElementName(), x, y, maxDistance);
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
        /// Tests that the value of the named element is near some location (see $near and $nearSphere).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="x">The x value of the origin.</param>
        /// <param name="y">The y value of the origin.</param>
        /// <param name="maxDistance">The max distance for a document to be included in the results.</param>
        /// <param name="spherical">Whether to do a spherical search.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList Near<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            double x,
            double y,
            double maxDistance,
            bool spherical
        ) {
            return Query.Near(memberExpression.GetElementName(), x, y, maxDistance, spherical);
        }

        /// <summary>
        /// Tests that none of the subqueries is true (see $nor).
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
        /// Tests that the value of the named element does not match any of the tests that follow (see $not).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryNot Not(
            string name
        ) {
            return new QueryNot(name);
        }

        /// <summary>
        /// Tests that the value of the named element does not match any of the tests that follow (see $not).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryNot Not<TDocument>(
            Expression<Func<TDocument, object>> memberExpression
        ) {
            return Query.Not(memberExpression.GetElementName());
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList NotIn(
            string name,
            BsonArray values
        ) {
            return new QueryConditionList(name).NotIn(values);
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList NotIn<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            BsonArray values
        ) {
            return Query.NotIn(memberExpression.GetElementName(), values);
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList NotIn(
            string name,
            IEnumerable<BsonValue> values
        ) {
            return new QueryConditionList(name).NotIn(values);
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList NotIn<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            IEnumerable<BsonValue> values
        ) {
            return Query.NotIn(memberExpression.GetElementName(), values);
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="arg1">The first value to compare to.</param>
        /// <param name="arg2">The second value to compare to.</param>
        /// <param name="args">The additional values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList NotIn(
            string name,
            BsonValue arg1,
            BsonValue arg2,
            params BsonValue[] args
        ) {
            return NotIn(name, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="arg1">The first value to compare to.</param>
        /// <param name="arg2">The second value to compare to.</param>
        /// <param name="args">The additional values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList NotIn<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            BsonValue arg1,
            BsonValue arg2,
            params BsonValue[] args
        ) {
            return NotIn(memberExpression.GetElementName(), ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList NotIn(
            string name,
            IEnumerable values
        ) {
            return new QueryConditionList(name).NotIn(values);
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="values">The values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList NotIn<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            IEnumerable values
        ) {
            return NotIn(memberExpression.GetElementName(), values);
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="arg1">The first value to compare to.</param>
        /// <param name="arg2">The second value to compare to.</param>
        /// <param name="args">The additional values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList NotIn(
            string name,
            object arg1,
            object arg2,
            params object[] args
        ) {
            return NotIn(name, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Tests that the value of the named element is not equal to any of a list of values (see $nin).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="arg1">The first value to compare to.</param>
        /// <param name="arg2">The second value to compare to.</param>
        /// <param name="args">The additional values to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList NotIn<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            object arg1,
            object arg2,
            params object[] args
        ) {
            return NotIn(memberExpression.GetElementName(), ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Tests that at least one of the subqueries is true (see $or).
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
        /// Tests that the size of the named array is equal to some value (see $size).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="size">The size to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList Size(
            string name,
            int size
        ) {
            return new QueryConditionList(name).Size(size);
        }

        /// <summary>
        /// Tests that the size of the named array is equal to some value (see $size).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="size">The size to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList Size<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            int size
        ) {
            return Query.Size(memberExpression.GetElementName(), size);
        }

        /// <summary>
        /// Tests that the type of the named element is equal to some type (see $type).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="type">The type to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList Type(
            string name,
            BsonType type
        ) {
            return new QueryConditionList(name).Type(type);
        }

        /// <summary>
        /// Tests that the type of the named element is equal to some type (see $type).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="type">The type to compare to.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList Type<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            BsonType type
        ) {
            return Query.Type(memberExpression.GetElementName(), type);
        }

        /// <summary>
        /// Tests that a JavaScript expression is true (see $where).
        /// </summary>
        /// <param name="javaScript">The where clause.</param>
        /// <returns>A query.</returns>
        public static QueryComplete Where(
            BsonJavaScript javaScript
        ) {
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
        public static QueryConditionList WithinCircle(
            string name,
            double centerX,
            double centerY,
            double radius
        ) {
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
            bool spherical
        ) {
            return new QueryConditionList(name).WithinCircle(centerX, centerY, radius, spherical);
        }

        /// <summary>
        /// Tests that the value of the named element is within a circle (see $within and $center).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="centerX">The x coordinate of the origin.</param>
        /// <param name="centerY">The y coordinate of the origin.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList WithinCircle<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            double centerX,
            double centerY,
            double radius
        ) {
            return Query.WithinCircle(memberExpression.GetElementName(), centerX, centerY, radius);
        }

        /// <summary>
        /// Tests that the value of the named element is within a circle (see $within and $center/$centerSphere).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="centerX">The x coordinate of the origin.</param>
        /// <param name="centerY">The y coordinate of the origin.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="spherical">Whether to do a spherical search.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList WithinCircle<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            double centerX,
            double centerY,
            double radius,
            bool spherical
        ) {
            return Query.WithinCircle(memberExpression.GetElementName(), centerX, centerY, radius, spherical);
        }

        /// <summary>
        /// Tests that the value of the named element is within a polygon (see $within and $polygon).
        /// </summary>
        /// <param name="name">The name of the element to test.</param>
        /// <param name="points">An array of points that defines the polygon (the second dimension must be of length 2).</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList WithinPolygon(
            string name,
            double[,] points
        ) {
            return new QueryConditionList(name).WithinPolygon(points);
        }

        /// <summary>
        /// Tests that the value of the named element is within a polygon (see $within and $polygon).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="points">An array of points that defines the polygon (the second dimension must be of length 2).</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList WithinPolygon<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            double[,] points
        ) {
            return Query.WithinPolygon(memberExpression.GetElementName(), points);
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
            double upperRightY
        ) {
            return new QueryConditionList(name).WithinRectangle(lowerLeftX, lowerLeftY, upperRightX, upperRightY);
        }

        /// <summary>
        /// Tests that the value of the named element is within a rectangle (see $within and $box).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="lowerLeftX">The x coordinate of the lower left corner.</param>
        /// <param name="lowerLeftY">The y coordinate of the lower left corner.</param>
        /// <param name="upperRightX">The x coordinate of the upper right corner.</param>
        /// <param name="upperRightY">The y coordinate of the upper right corner.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static QueryConditionList WithinRectangle<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            double lowerLeftX,
            double lowerLeftY,
            double upperRightX,
            double upperRightY
        ) {
            return Query.WithinRectangle(memberExpression.GetElementName(), lowerLeftX, lowerLeftY, upperRightX, upperRightY);
        }
        #endregion

        #region private static methods
        // try to keey the query in simple form and only promote to $and form if necessary
        private static void AddAndClause(
            BsonDocument query,
            BsonElement clause
        ) {
            if (query.ElementCount == 1 && query.GetElement(0).Name == "$and") {
                query[0].AsBsonArray.Add(new BsonDocument(clause));
            }
            else
            {
                if (clause.Name == "$and") {
                    PromoteQueryToDollarAndForm(query, clause);
                }
                else
                {
                    if (query.Contains(clause.Name)) {
                        var existingClause = query.GetElement(clause.Name);
                        if (existingClause.Value.IsBsonDocument && clause.Value.IsBsonDocument) {
                            var clauseValue = clause.Value.AsBsonDocument;
                            var existingClauseValue = existingClause.Value.AsBsonDocument;
                            if (clauseValue.Names.Any(op => existingClauseValue.Contains(op))) {
                                PromoteQueryToDollarAndForm(query, clause);
                            }
                            else
                            {
                                foreach (var element in clauseValue) {
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

        private static void PromoteQueryToDollarAndForm(
            BsonDocument query,
            BsonElement clause
        ) {
            var clauses = new BsonArray();
            foreach (var queryElement in query) {
                clauses.Add(new BsonDocument(queryElement));
            }
            clauses.Add(new BsonDocument(clause));
            query.Clear();
            query.Add("$and", clauses);
        }
        #endregion
    }
}
