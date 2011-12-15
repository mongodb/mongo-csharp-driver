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
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Wrappers;

namespace MongoDB.Driver {
    /// <summary>
    /// A helper class for creating update modifiers.
    /// </summary>
    public static class Update {
        #region public static methods
        /// <summary>
        /// Creates an update replacement document from an object.
        /// </summary>
        /// <typeparam name="T">The nominal type of the wrapped object.</typeparam>
        /// <param name="update">The wrapped object.</param>
        /// <returns>A new instance of UpdateWrapper or null.</returns>
        public static UpdateWrapper Create<T>(
            T update
        ) {
            return UpdateWrapper.Create(update);
        }

        /// <summary>
        /// Creates an update replacement document from an object.
        /// </summary>
        /// <param name="nominalType">The nominal type of the wrapped object.</param>
        /// <param name="update">The wrapped object.</param>
        /// <returns>A new instance of UpdateWrapper or null.</returns>
        public static UpdateWrapper Create(
            Type nominalType,
            object update
        ) {
            return UpdateWrapper.Create(nominalType, update);
        }

        /// <summary>
        /// Adds a value to a named array element if the value is not already in the array (see $addToSet).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="value">The value to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder AddToSet(
            string name,
            object value
        ) {
            return new UpdateBuilder().AddToSet(name, value);
        }

        /// <summary>
        /// Adds a value to a named array element if the value is not already in the array (see $addToSet).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The value to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder AddToSet<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            object value
        ) {
            return new UpdateBuilder().AddToSet(memberExpression, value);
        }

        /// <summary>
        /// Adds a list of values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder AddToSetEach(
            string name,
            BsonArray values
        ) {
            return new UpdateBuilder().AddToSetEach(name, values);
        }

        /// <summary>
        /// Adds a list of values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="values">The values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder AddToSetEach<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            BsonArray values
        ) {
            return new UpdateBuilder().AddToSetEach(memberExpression, values);
        }

        /// <summary>
        /// Adds a list of values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder AddToSetEach(
            string name,
            IEnumerable<BsonValue> values
        ) {
            return new UpdateBuilder().AddToSetEach(name, values);
        }


        /// <summary>
        /// Adds a list of values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="values">The values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder AddToSetEach<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            IEnumerable<BsonValue> values
        ) {
            return new UpdateBuilder().AddToSetEach(memberExpression, values);
        }

        /// <summary>
        /// Adds a list of values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="arg1">The first value to add to the set.</param>
        /// <param name="arg2">The second value to add to the set.</param>
        /// <param name="args">The additional values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder AddToSetEach(
            string name,
            BsonValue arg1,
            BsonValue arg2,
            params BsonValue[] args
        ) {
            return new UpdateBuilder().AddToSetEach(name, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Adds a list of values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="arg1">The first value to add to the set.</param>
        /// <param name="arg2">The second value to add to the set.</param>
        /// <param name="args">The additional values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder AddToSetEach<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            BsonValue arg1,
            BsonValue arg2,
            params BsonValue[] args
        ) {
            return new UpdateBuilder().AddToSetEach(memberExpression, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Adds a list of values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder AddToSetEach(
            string name,
            IEnumerable values
        ) {
            return new UpdateBuilder().AddToSetEach(name, values);
        }

        /// <summary>
        /// Adds a list of values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="values">The values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder AddToSetEach<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            IEnumerable values
        ) {
            return new UpdateBuilder().AddToSetEach(memberExpression, values);
        }

        /// <summary>
        /// Adds a list of values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="arg1">The first value to add to the set.</param>
        /// <param name="arg2">The second value to add to the set.</param>
        /// <param name="args">The additional values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder AddToSetEach(
            string name,
            object arg1,
            object arg2,
            params object[] args
        ) {
            return new UpdateBuilder().AddToSetEach(name, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Adds a list of values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="arg1">The first value to add to the set.</param>
        /// <param name="arg2">The second value to add to the set.</param>
        /// <param name="args">The additional values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder AddToSetEach<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            object arg1,
            object arg2,
            params object[] args
        ) {
            return new UpdateBuilder().AddToSetEach(memberExpression, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Sets the named element to the bitwise and of its value with another value (see $bit with "and").
        /// </summary>
        /// <param name="name">The name of the element to be modified.</param>
        /// <param name="value">The value to be and-ed with the current value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder BitwiseAnd(
            string name,
            int value
        ) {
            return new UpdateBuilder().BitwiseAnd(name, value);
        }

        /// <summary>
        /// Sets the named element to the bitwise and of its value with another value (see $bit with "and").
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The value to be and-ed with the current value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder BitwiseAnd<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            int value
        ) {
            return new UpdateBuilder().BitwiseAnd(memberExpression, value);
        }

        /// <summary>
        /// Sets the named element to the bitwise and of its value with another value (see $bit with "and").
        /// </summary>
        /// <param name="name">The name of the element to be modified.</param>
        /// <param name="value">The value to be and-ed with the current value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder BitwiseAnd(
            string name,
            long value
        ) {
            return new UpdateBuilder().BitwiseAnd(name, value);
        }

        /// <summary>
        /// Sets the named element to the bitwise and of its value with another value (see $bit with "and").
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The value to be and-ed with the current value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder BitwiseAnd<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            long value
        ) {
            return new UpdateBuilder().BitwiseAnd(memberExpression, value);
        }

        /// <summary>
        /// Sets the named element to the bitwise or of its value with another value (see $bit with "or").
        /// </summary>
        /// <param name="name">The name of the element to be modified.</param>
        /// <param name="value">The value to be or-ed with the current value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder BitwiseOr(
            string name,
            int value
        ) {
            return new UpdateBuilder().BitwiseOr(name, value);
        }

        /// <summary>
        /// Sets the named element to the bitwise or of its value with another value (see $bit with "or").
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The value to be or-ed with the current value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder BitwiseOr<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            int value
        ) {
            return new UpdateBuilder().BitwiseOr(memberExpression, value);
        }

        /// <summary>
        /// Sets the named element to the bitwise or of its value with another value (see $bit with "or").
        /// </summary>
        /// <param name="name">The name of the element to be modified.</param>
        /// <param name="value">The value to be or-ed with the current value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder BitwiseOr(
            string name,
            long value
        ) {
            return new UpdateBuilder().BitwiseOr(name, value);
        }

        /// <summary>
        /// Sets the named element to the bitwise or of its value with another value (see $bit with "or").
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The value to be or-ed with the current value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder BitwiseOr<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            long value
        ) {
            return new UpdateBuilder().BitwiseOr(memberExpression, value);
        }

        /// <summary>
        /// Combines several UpdateBuilders into a single UpdateBuilder.
        /// </summary>
        /// <param name="updates">The UpdateBuilders to combine.</param>
        /// <returns>A combined UpdateBuilder.</returns>
        public static UpdateBuilder Combine(
            IEnumerable<UpdateBuilder> updates
        ) {
            var combined = new UpdateBuilder();
            foreach (var update in updates) {
                combined.Combine(update);
            }
            return combined;
        }

        /// <summary>
        /// Combines several UpdateBuilders into a single UpdateBuilder.
        /// </summary>
        /// <param name="updates">The UpdateBuilders to combine.</param>
        /// <returns>A combined UpdateBuilder.</returns>
        public static UpdateBuilder Combine(
            params UpdateBuilder[] updates
        ) {
            return Combine((IEnumerable<UpdateBuilder>) updates);
        }

        /// <summary>
        /// Increments the named element by a value (see $inc).
        /// </summary>
        /// <param name="name">The name of the element to be incremented.</param>
        /// <param name="value">The value to increment by.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder Inc(
            string name,
            double value
        ) {
            return new UpdateBuilder().Inc(name, value);
        }

        /// <summary>
        /// Increments the named element by a value (see $inc).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The value to increment by.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder Inc<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            double value
        ) {
            return new UpdateBuilder().Inc(memberExpression, value);
        }

        /// <summary>
        /// Increments the named element by a value (see $inc).
        /// </summary>
        /// <param name="name">The name of the element to be incremented.</param>
        /// <param name="value">The value to increment by.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder Inc(
            string name,
            int value
        ) {
            return new UpdateBuilder().Inc(name, value);
        }

        /// <summary>
        /// Increments the named element by a value (see $inc).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The value to increment by.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder Inc<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            int value
        ) {
            return new UpdateBuilder().Inc(memberExpression, value);
        }

        /// <summary>
        /// Increments the named element by a value (see $inc).
        /// </summary>
        /// <param name="name">The name of the element to be incremented.</param>
        /// <param name="value">The value to increment by.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder Inc(
            string name,
            long value
        ) {
            return new UpdateBuilder().Inc(name, value);
        }

        /// <summary>
        /// Increments the named element by a value (see $inc).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The value to increment by.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder Inc<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            long value
        ) {
            return new UpdateBuilder().Inc(memberExpression, value);
        }

        /// <summary>
        /// Removes the first value from the named array element (see $pop).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PopFirst(
            string name
        ) {
            return new UpdateBuilder().PopFirst(name);
        }

        /// <summary>
        /// Removes the first value from the named array element (see $pop).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PopFirst<TDocument>(
            Expression<Func<TDocument, object>> memberExpression
        ) {
            return new UpdateBuilder().PopFirst(memberExpression);
        }

        /// <summary>
        /// Removes the last value from the named array element (see $pop).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PopLast(
            string name
        ) {
            return new UpdateBuilder().PopLast(name);
        }

        /// <summary>
        /// Removes the last value from the named array element (see $pop).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PopLast<TDocument>(
            Expression<Func<TDocument, object>> memberExpression
        ) {
            return new UpdateBuilder().PopLast(memberExpression);
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to some value (see $pull).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="value">The value to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder Pull(
            string name,
            object value
        ) {
            return new UpdateBuilder().Pull(name, value);
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to some value (see $pull).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The value to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder Pull<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            object value
        ) {
            return new UpdateBuilder().Pull(memberExpression, value);
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of values (see $pullAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PullAll(
            string name,
            BsonArray values
        ) {
            return new UpdateBuilder().PullAll(name, values);
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of values (see $pullAll).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="values">The values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PullAll<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            BsonArray values
        ) {
            return new UpdateBuilder().PullAll(memberExpression, values);
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of values (see $pullAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PullAll(
            string name,
            IEnumerable<BsonValue> values
        ) {
            return new UpdateBuilder().PullAll(name, values);
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of values (see $pullAll).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="values">The values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PullAll<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            IEnumerable<BsonValue> values
        ) {
            return new UpdateBuilder().PullAll(memberExpression, values);
        }


        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of values (see $pullAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="arg1">The first value to remove.</param>
        /// <param name="arg2">The second value to remove.</param>
        /// <param name="args">The additional values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PullAll(
            string name,
            BsonValue arg1,
            BsonValue arg2,
            params BsonValue[] args
        ) {
            return new UpdateBuilder().PullAll(name, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of values (see $pullAll).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="arg1">The first value to remove.</param>
        /// <param name="arg2">The second value to remove.</param>
        /// <param name="args">The additional values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PullAll<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            BsonValue arg1,
            BsonValue arg2,
            params BsonValue[] args
        ) {
            return new UpdateBuilder().PullAll(memberExpression, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of values (see $pullAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PullAll(
            string name,
            IEnumerable values
        ) {
            return new UpdateBuilder().PullAll(name, values);
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of values (see $pullAll).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="values">The values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PullAll<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            IEnumerable values
        ) {
            return new UpdateBuilder().PullAll(memberExpression, values);
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of values (see $pullAll).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="arg1">The first value to remove.</param>
        /// <param name="arg2">The second value to remove.</param>
        /// <param name="args">The additional values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PullAll<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            object arg1,
            object arg2,
            params object[] args
        ) {
            return new UpdateBuilder().PullAll(memberExpression, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of values (see $pullAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="arg1">The first value to remove.</param>
        /// <param name="arg2">The second value to remove.</param>
        /// <param name="args">The additional values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PullAll(
            string name,
            object arg1,
            object arg2,
            params object[] args
        ) {
            return new UpdateBuilder().PullAll(name, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Adds a value to the end of the named array element (see $push).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="value">The value to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder Push(
            string name,
            object value
        ) {
            return new UpdateBuilder().Push(name, value);
        }

        /// <summary>
        /// Adds a value to the end of the named array element (see $push).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The value to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder Push<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            object value
        ) {
            return new UpdateBuilder().Push(memberExpression, value);
        }

        /// <summary>
        /// Adds a list of values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PushAll(
            string name,
            BsonArray values
        ) {
            return new UpdateBuilder().PushAll(name, values);
        }

        /// <summary>
        /// Adds a list of values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="values">The values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PushAll<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            BsonArray values
        ) {
            return new UpdateBuilder().PushAll(memberExpression, values);
        }

        /// <summary>
        /// Adds a list of values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PushAll(
            string name,
            IEnumerable<BsonValue> values
        ) {
            return new UpdateBuilder().PushAll(name, values);
        }

        /// <summary>
        /// Adds a list of values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="values">The values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PushAll<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            IEnumerable<BsonValue> values
        ) {
            return new UpdateBuilder().PushAll(memberExpression, values);
        }

        /// <summary>
        /// Adds a list of values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="arg1">The first value to add to the end of the array.</param>
        /// <param name="arg2">The second value to add to the end of the array.</param>
        /// <param name="args">The additional values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PushAll(
            string name,
            BsonValue arg1,
            BsonValue arg2,
            params BsonValue[] args
        ) {
            return new UpdateBuilder().PushAll(name, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Adds a list of values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="arg1">The first value to add to the end of the array.</param>
        /// <param name="arg2">The second value to add to the end of the array.</param>
        /// <param name="args">The additional values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PushAll<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            BsonValue arg1,
            BsonValue arg2,
            params BsonValue[] args
        ) {
            return new UpdateBuilder().PushAll(memberExpression, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Adds a list of values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PushAll(
            string name,
            IEnumerable values
        ) {
            return new UpdateBuilder().PushAll(name, values);
        }

        /// <summary>
        /// Adds a list of values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="values">The values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PushAll<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            IEnumerable values
        ) {
            return new UpdateBuilder().PushAll(memberExpression, values);
        }

        /// <summary>
        /// Adds a list of values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="arg1">The first value to add to the end of the array.</param>
        /// <param name="arg2">The second value to add to the end of the array.</param>
        /// <param name="args">The additional values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PushAll(
            string name,
            object arg1,
            object arg2,
            params object[] args
        ) {
            return new UpdateBuilder().PushAll(name, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Adds a list of values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="arg1">The first value to add to the end of the array.</param>
        /// <param name="arg2">The second value to add to the end of the array.</param>
        /// <param name="args">The additional values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PushAll<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            object arg1,
            object arg2,
            params object[] args
        ) {
            return new UpdateBuilder().PushAll(memberExpression, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Renames an element (see $rename).
        /// </summary>
        /// <param name="oldElementName">The name of the element to be renamed.</param>
        /// <param name="newElementName">The new name of the element.</param>
        /// <returns>An UpdateDocuemnt.</returns>
        public static UpdateBuilder Rename(
            string oldElementName,
            string newElementName
        ) {
            return new UpdateBuilder().Rename(oldElementName, newElementName);
        }

        /// <summary>
        /// Replaces the entire document with a new document (the _id must remain the same).
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the replacement document</typeparam>
        /// <param name="document">The replacement document.</param>
        /// <returns>An UpdateWrapper.</returns>
        public static IMongoUpdate Replace<TNominalType>(
            TNominalType document
        ) {
            return UpdateWrapper.Create<TNominalType>(document);
        }

        /// <summary>
        /// Replaces the entire document with a new document (the _id must remain the same).
        /// </summary>
        /// <param name="nominalType">The nominal type of the replacement document</param>
        /// <param name="document">The replacement document.</param>
        /// <returns>An UpdateWrapper.</returns>
        public static IMongoUpdate Replace(
            Type nominalType,
            object document
        ) {
            return UpdateWrapper.Create(nominalType, document);
        }

        /// <summary>
        /// Sets the value of the named element to a new value (see $set).
        /// </summary>
        /// <param name="name">The name of the element to be set.</param>
        /// <param name="value">The new value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder Set(
            string name,
            object value
        ) {
            return new UpdateBuilder().Set(name, value);
        }

        /// <summary>
        /// Sets the value of the named element to a new value (see $set).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The new value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder Set<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            object value
        ) {
            return new UpdateBuilder().Set(memberExpression, value);
        }

        /// <summary>
        /// Removes the named element from the document (see $unset).
        /// </summary>
        /// <param name="name">The name of the element to be removed.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder Unset(
            string name
        ) {
            return new UpdateBuilder().Unset(name);
        }

        /// <summary>
        /// Removes the named element from the document (see $unset).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder Unset<TDocument>(
            Expression<Func<TDocument, object>> memberExpression
        ) {
            return new UpdateBuilder().Unset(memberExpression);
        }
        #endregion
    }
}
