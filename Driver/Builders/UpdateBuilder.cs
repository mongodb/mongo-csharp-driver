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
using MongoDB.Driver;
using MongoDB.Driver.Wrappers;

namespace MongoDB.Driver.Builders
{
    /// <summary>
    /// A builder for creating update modifiers.
    /// </summary>
    public static class Update
    {
        // public static methods
        /// <summary>
        /// Adds a value to a named array element if the value is not already in the array (see $addToSet).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="value">The value to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder AddToSet(string name, BsonValue value)
        {
            return new UpdateBuilder().AddToSet(name, value);
        }

        /// <summary>
        /// Adds a list of values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder AddToSetEach(string name, BsonArray values)
        {
            return new UpdateBuilder().AddToSetEach(name, values);
        }

        /// <summary>
        /// Adds a list of values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder AddToSetEach(string name, IEnumerable<BsonValue> values)
        {
            return new UpdateBuilder().AddToSetEach(name, values);
        }

        /// <summary>
        /// Adds a list of values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder AddToSetEach(string name, params BsonValue[] values)
        {
            return new UpdateBuilder().AddToSetEach(name, values);
        }

        /// <summary>
        /// Adds a list of wrapped values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <typeparam name="T">The type of wrapped values.</typeparam>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The wrapped values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder AddToSetEachWrapped<T>(string name, IEnumerable<T> values)
        {
            return new UpdateBuilder().AddToSetEachWrapped<T>(name, values);
        }

        /// <summary>
        /// Adds a list of wrapped values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <typeparam name="T">The type of wrapped values.</typeparam>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The wrapped values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder AddToSetEachWrapped<T>(string name, params T[] values)
        {
            return new UpdateBuilder().AddToSetEachWrapped<T>(name, values);
        }

        /// <summary>
        /// Adds a wrapped value to a named array element if the value is not already in the array (see $addToSet).
        /// </summary>
        /// <typeparam name="T">The type of wrapped value.</typeparam>
        /// <param name="name">The name of the array element.</param>
        /// <param name="value">The wrapped value to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder AddToSetWrapped<T>(string name, T value)
        {
            return new UpdateBuilder().AddToSetWrapped<T>(name, value);
        }

        /// <summary>
        /// Sets the named element to the bitwise and of its value with another value (see $bit with "and").
        /// </summary>
        /// <param name="name">The name of the element to be modified.</param>
        /// <param name="value">The value to be and-ed with the current value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder BitwiseAnd(string name, int value)
        {
            return new UpdateBuilder().BitwiseAnd(name, value);
        }

        /// <summary>
        /// Sets the named element to the bitwise and of its value with another value (see $bit with "and").
        /// </summary>
        /// <param name="name">The name of the element to be modified.</param>
        /// <param name="value">The value to be and-ed with the current value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder BitwiseAnd(string name, long value)
        {
            return new UpdateBuilder().BitwiseAnd(name, value);
        }

        /// <summary>
        /// Sets the named element to the bitwise or of its value with another value (see $bit with "or").
        /// </summary>
        /// <param name="name">The name of the element to be modified.</param>
        /// <param name="value">The value to be or-ed with the current value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder BitwiseOr(string name, int value)
        {
            return new UpdateBuilder().BitwiseOr(name, value);
        }

        /// <summary>
        /// Sets the named element to the bitwise or of its value with another value (see $bit with "or").
        /// </summary>
        /// <param name="name">The name of the element to be modified.</param>
        /// <param name="value">The value to be or-ed with the current value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder BitwiseOr(string name, long value)
        {
            return new UpdateBuilder().BitwiseOr(name, value);
        }

        /// <summary>
        /// Combines several UpdateBuilders into a single UpdateBuilder.
        /// </summary>
        /// <param name="updates">The UpdateBuilders to combine.</param>
        /// <returns>A combined UpdateBuilder.</returns>
        public static UpdateBuilder Combine(IEnumerable<UpdateBuilder> updates)
        {
            if (updates == null) { throw new ArgumentNullException("updates"); }
            var combined = new UpdateBuilder();
            foreach (var update in updates)
            {
                combined.Combine(update);
            }
            return combined;
        }

        /// <summary>
        /// Combines several UpdateBuilders into a single UpdateBuilder.
        /// </summary>
        /// <param name="updates">The UpdateBuilders to combine.</param>
        /// <returns>A combined UpdateBuilder.</returns>
        public static UpdateBuilder Combine(params UpdateBuilder[] updates)
        {
            return Combine((IEnumerable<UpdateBuilder>)updates);
        }

        /// <summary>
        /// Increments the named element by a value (see $inc).
        /// </summary>
        /// <param name="name">The name of the element to be incremented.</param>
        /// <param name="value">The value to increment by.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder Inc(string name, double value)
        {
            return new UpdateBuilder().Inc(name, value);
        }

        /// <summary>
        /// Increments the named element by a value (see $inc).
        /// </summary>
        /// <param name="name">The name of the element to be incremented.</param>
        /// <param name="value">The value to increment by.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder Inc(string name, int value)
        {
            return new UpdateBuilder().Inc(name, value);
        }

        /// <summary>
        /// Increments the named element by a value (see $inc).
        /// </summary>
        /// <param name="name">The name of the element to be incremented.</param>
        /// <param name="value">The value to increment by.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder Inc(string name, long value)
        {
            return new UpdateBuilder().Inc(name, value);
        }

        /// <summary>
        /// Removes the first value from the named array element (see $pop).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PopFirst(string name)
        {
            return new UpdateBuilder().PopFirst(name);
        }

        /// <summary>
        /// Removes the last value from the named array element (see $pop).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PopLast(string name)
        {
            return new UpdateBuilder().PopLast(name);
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to some value (see $pull).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="value">The value to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder Pull(string name, BsonValue value)
        {
            return new UpdateBuilder().Pull(name, value);
        }

        /// <summary>
        /// Removes all values from the named array element that match some query (see $pull).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="query">A query that specifies which elements to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder Pull(string name, IMongoQuery query)
        {
            return new UpdateBuilder().Pull(name, query);
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of values (see $pullAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PullAll(string name, BsonArray values)
        {
            return new UpdateBuilder().PullAll(name, values);
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of values (see $pullAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PullAll(string name, IEnumerable<BsonValue> values)
        {
            return new UpdateBuilder().PullAll(name, values);
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of values (see $pullAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PullAll(string name, params BsonValue[] values)
        {
            return new UpdateBuilder().PullAll(name, values);
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of wrapped values (see $pullAll).
        /// </summary>
        /// <typeparam name="T">The type of wrapped values.</typeparam>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The wrapped values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PullAllWrapped<T>(string name, IEnumerable<T> values)
        {
            return new UpdateBuilder().PullAllWrapped<T>(name, values);
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of wrapped values (see $pullAll).
        /// </summary>
        /// <typeparam name="T">The type of wrapped values.</typeparam>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The wrapped values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PullAllWrapped<T>(string name, params T[] values)
        {
            return new UpdateBuilder().PullAllWrapped<T>(name, values);
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to some wrapped value (see $pull).
        /// </summary>
        /// <typeparam name="T">The type of wrapped value.</typeparam>
        /// <param name="name">The name of the array element.</param>
        /// <param name="value">The wrapped value to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PullWrapped<T>(string name, T value)
        {
            return new UpdateBuilder().PullWrapped<T>(name, value);
        }

        /// <summary>
        /// Adds a value to the end of the named array element (see $push).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="value">The value to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder Push(string name, BsonValue value)
        {
            return new UpdateBuilder().Push(name, value);
        }

        /// <summary>
        /// Adds a list of values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PushAll(string name, BsonArray values)
        {
            return new UpdateBuilder().PushAll(name, values);
        }

        /// <summary>
        /// Adds a list of values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PushAll(string name, IEnumerable<BsonValue> values)
        {
            return new UpdateBuilder().PushAll(name, values);
        }

        /// <summary>
        /// Adds a list of values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PushAll(string name, params BsonValue[] values)
        {
            return new UpdateBuilder().PushAll(name, values);
        }

        /// <summary>
        /// Adds a list of wrapped values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <typeparam name="T">The type of wrapped values.</typeparam>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The wrapped values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PushAllWrapped<T>(string name, IEnumerable<T> values)
        {
            return new UpdateBuilder().PushAllWrapped<T>(name, values);
        }

        /// <summary>
        /// Adds a list of wrapped values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <typeparam name="T">The type of wrapped values.</typeparam>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The wrapped values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PushAllWrapped<T>(string name, params T[] values)
        {
            return new UpdateBuilder().PushAllWrapped<T>(name, values);
        }

        /// <summary>
        /// Adds a wrapped value to the end of the named array element (see $push).
        /// </summary>
        /// <typeparam name="T">The type of wrapped value.</typeparam>
        /// <param name="name">The name of the array element.</param>
        /// <param name="value">The wrapped value to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder PushWrapped<T>(string name, T value)
        {
            return new UpdateBuilder().PushWrapped<T>(name, value);
        }

        /// <summary>
        /// Renames an element (see $rename).
        /// </summary>
        /// <param name="oldElementName">The name of the element to be renamed.</param>
        /// <param name="newElementName">The new name of the element.</param>
        /// <returns>An UpdateDocuemnt.</returns>
        public static UpdateBuilder Rename(string oldElementName, string newElementName)
        {
            return new UpdateBuilder().Rename(oldElementName, newElementName);
        }

        /// <summary>
        /// Replaces the entire document with a new document (the _id must remain the same).
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the replacement document</typeparam>
        /// <param name="document">The replacement document.</param>
        /// <returns>An UpdateWrapper.</returns>
        public static IMongoUpdate Replace<TNominalType>(TNominalType document)
        {
            if (document == null) { throw new ArgumentNullException("document"); }
            return UpdateWrapper.Create<TNominalType>(document);
        }

        /// <summary>
        /// Replaces the entire document with a new document (the _id must remain the same).
        /// </summary>
        /// <param name="nominalType">The nominal type of the replacement document</param>
        /// <param name="document">The replacement document.</param>
        /// <returns>An UpdateWrapper.</returns>
        public static IMongoUpdate Replace(Type nominalType, object document)
        {
            if (nominalType == null) { throw new ArgumentNullException("nominalType"); }
            if (document == null) { throw new ArgumentNullException("document"); }
            return UpdateWrapper.Create(nominalType, document);
        }

        /// <summary>
        /// Sets the value of the named element to a new value (see $set).
        /// </summary>
        /// <param name="name">The name of the element to be set.</param>
        /// <param name="value">The new value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder Set(string name, BsonValue value)
        {
            return new UpdateBuilder().Set(name, value);
        }

        /// <summary>
        /// Sets the value of the named element to a new wrapped value (see $set).
        /// </summary>
        /// <typeparam name="T">The type of wrapped value.</typeparam>
        /// <param name="name">The name of the element to be set.</param>
        /// <param name="value">The new wrapped value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder SetWrapped<T>(string name, T value)
        {
            return new UpdateBuilder().SetWrapped<T>(name, value);
        }

        /// <summary>
        /// Removes the named element from the document (see $unset).
        /// </summary>
        /// <param name="name">The name of the element to be removed.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static UpdateBuilder Unset(string name)
        {
            return new UpdateBuilder().Unset(name);
        }
    }

    /// <summary>
    /// A builder for creating update modifiers.
    /// </summary>
    [Serializable]
    public class UpdateBuilder : BuilderBase, IMongoUpdate
    {
        // private fields
        private BsonDocument _document;

        // constructors
        /// <summary>
        /// Initializes a new instance of the UpdateBuilder class.
        /// </summary>
        public UpdateBuilder()
        {
            _document = new BsonDocument();
        }

        // internal properties
        internal BsonDocument Document
        {
            get { return _document; }
        }

        // public methods
        /// <summary>
        /// Adds a value to a named array element if the value is not already in the array (see $addToSet).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="value">The value to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder AddToSet(string name, BsonValue value)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (value == null) { throw new ArgumentNullException("value"); }
            BsonElement element;
            if (_document.TryGetElement("$addToSet", out element))
            {
                element.Value.AsBsonDocument.Add(name, value);
            }
            else
            {
                _document.Add("$addToSet", new BsonDocument(name, value));
            }
            return this;
        }

        /// <summary>
        /// Adds a list of values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder AddToSetEach(string name, BsonArray values)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (values == null) { throw new ArgumentNullException("values"); }
            var arg = new BsonDocument("$each", values);
            BsonElement element;
            if (_document.TryGetElement("$addToSet", out element))
            {
                element.Value.AsBsonDocument.Add(name, arg);
            }
            else
            {
                _document.Add("$addToSet", new BsonDocument(name, arg));
            }
            return this;
        }

        /// <summary>
        /// Adds a list of values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder AddToSetEach(string name, IEnumerable<BsonValue> values)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (values == null) { throw new ArgumentNullException("values"); }
            return AddToSetEach(name, new BsonArray(values));
        }

        /// <summary>
        /// Adds a list of values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder AddToSetEach(string name, params BsonValue[] values)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (values == null) { throw new ArgumentNullException("values"); }
            return AddToSetEach(name, (IEnumerable<BsonValue>)values);
        }

        /// <summary>
        /// Adds a list of wrapped values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <typeparam name="T">The type of wrapped values.</typeparam>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The wrapped values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder AddToSetEachWrapped<T>(string name, IEnumerable<T> values)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (values == null) { throw new ArgumentNullException("values"); }
            var wrappedValues = BsonDocumentWrapper.CreateMultiple(values).Cast<BsonValue>(); // the cast to BsonValue is required
            return AddToSetEach(name, wrappedValues);
        }

        /// <summary>
        /// Adds a list of wrapped values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <typeparam name="T">The type of wrapped values.</typeparam>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The wrapped values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder AddToSetEachWrapped<T>(string name, params T[] values)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (values == null) { throw new ArgumentNullException("values"); }
            return AddToSetEachWrapped(name, (IEnumerable<T>)values);
        }

        /// <summary>
        /// Adds a wrapped value to a named array element if the value is not already in the array (see $addToSet).
        /// </summary>
        /// <typeparam name="T">The type of wrapped value.</typeparam>
        /// <param name="name">The name of the array element.</param>
        /// <param name="value">The wrapped value to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder AddToSetWrapped<T>(string name, T value)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (value == null) { throw new ArgumentNullException("value"); }
            var wrappedValue = (BsonValue)BsonDocumentWrapper.Create(value); // the cast to BsonValue is required
            return AddToSet(name, wrappedValue);
        }

        /// <summary>
        /// Sets the named element to the bitwise and of its value with another value (see $bit with "and").
        /// </summary>
        /// <param name="name">The name of the element to be modified.</param>
        /// <param name="value">The value to be and-ed with the current value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder BitwiseAnd(string name, int value)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            BitwiseOperation(name, "and", value);
            return this;
        }

        /// <summary>
        /// Sets the named element to the bitwise and of its value with another value (see $bit with "and").
        /// </summary>
        /// <param name="name">The name of the element to be modified.</param>
        /// <param name="value">The value to be and-ed with the current value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder BitwiseAnd(string name, long value)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            BitwiseOperation(name, "and", value);
            return this;
        }

        /// <summary>
        /// Sets the named element to the bitwise or of its value with another value (see $bit with "or").
        /// </summary>
        /// <param name="name">The name of the element to be modified.</param>
        /// <param name="value">The value to be or-ed with the current value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder BitwiseOr(string name, int value)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            BitwiseOperation(name, "or", value);
            return this;
        }

        /// <summary>
        /// Sets the named element to the bitwise or of its value with another value (see $bit with "or").
        /// </summary>
        /// <param name="name">The name of the element to be modified.</param>
        /// <param name="value">The value to be or-ed with the current value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder BitwiseOr(string name, long value)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            BitwiseOperation(name, "or", value);
            return this;
        }

        /// <summary>
        /// Combines another UpdateBuilder into this one.
        /// </summary>
        /// <param name="otherUpdateBuilder">The UpdateBuilder to combine into this one.</param>
        /// <returns>A combined UpdateBuilder.</returns>
        public UpdateBuilder Combine(UpdateBuilder otherUpdateBuilder)
        {
            if (otherUpdateBuilder == null) { throw new ArgumentNullException("otherUpdateBuilder"); }
            foreach (var otherOperation in otherUpdateBuilder.Document)
            {
                var otherOperationName = otherOperation.Name;
                var otherTargets = otherOperation.Value.AsBsonDocument;
                BsonElement operation;
                if (_document.TryGetElement(otherOperationName, out operation))
                {
                    operation.Value.AsBsonDocument.Add(otherTargets);
                }
                else
                {
                    _document.Add(otherOperationName, otherTargets);
                }
            }
            return this;
        }

        /// <summary>
        /// Increments the named element by a value (see $inc).
        /// </summary>
        /// <param name="name">The name of the element to be incremented.</param>
        /// <param name="value">The value to increment by.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder Inc(string name, double value)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            Inc(name, BsonValue.Create(value));
            return this;
        }

        /// <summary>
        /// Increments the named element by a value (see $inc).
        /// </summary>
        /// <param name="name">The name of the element to be incremented.</param>
        /// <param name="value">The value to increment by.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder Inc(string name, int value)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            Inc(name, BsonValue.Create(value));
            return this;
        }

        /// <summary>
        /// Increments the named element by a value (see $inc).
        /// </summary>
        /// <param name="name">The name of the element to be incremented.</param>
        /// <param name="value">The value to increment by.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder Inc(string name, long value)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            Inc(name, BsonValue.Create(value));
            return this;
        }

        /// <summary>
        /// Removes the first value from the named array element (see $pop).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PopFirst(string name)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            BsonElement element;
            if (_document.TryGetElement("$pop", out element))
            {
                element.Value.AsBsonDocument.Add(name, -1);
            }
            else
            {
                _document.Add("$pop", new BsonDocument(name, -1));
            }
            return this;
        }

        /// <summary>
        /// Removes the last value from the named array element (see $pop).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PopLast(string name)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            BsonElement element;
            if (_document.TryGetElement("$pop", out element))
            {
                element.Value.AsBsonDocument.Add(name, 1);
            }
            else
            {
                _document.Add("$pop", new BsonDocument(name, 1));
            }
            return this;
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to some value (see $pull).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="value">The value to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder Pull(string name, BsonValue value)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (value == null) { throw new ArgumentNullException("value"); }
            BsonElement element;
            if (_document.TryGetElement("$pull", out element))
            {
                element.Value.AsBsonDocument.Add(name, value);
            }
            else
            {
                _document.Add("$pull", new BsonDocument(name, value));
            }
            return this;
        }

        /// <summary>
        /// Removes all values from the named array element that match some query (see $pull).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="query">A query that specifies which elements to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder Pull(string name, IMongoQuery query)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (query == null) { throw new ArgumentNullException("query"); }
            BsonValue wrappedQuery = BsonDocumentWrapper.Create(query);
            BsonElement element;
            if (_document.TryGetElement("$pull", out element))
            {
                element.Value.AsBsonDocument.Add(name, wrappedQuery);
            }
            else
            {
                _document.Add("$pull", new BsonDocument(name, wrappedQuery));
            }
            return this;
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of values (see $pullAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PullAll(string name, BsonArray values)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (values == null) { throw new ArgumentNullException("values"); }
            BsonElement element;
            if (_document.TryGetElement("$pullAll", out element))
            {
                element.Value.AsBsonDocument.Add(name, values);
            }
            else
            {
                _document.Add("$pullAll", new BsonDocument(name, values));
            }
            return this;
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of values (see $pullAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PullAll(string name, IEnumerable<BsonValue> values)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (values == null) { throw new ArgumentNullException("values"); }
            return PullAll(name, new BsonArray(values));
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of values (see $pullAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PullAll(string name, params BsonValue[] values)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (values == null) { throw new ArgumentNullException("values"); }
            return PullAll(name, (IEnumerable<BsonValue>)values);
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of wrapped values (see $pullAll).
        /// </summary>
        /// <typeparam name="T">The type of wrapped values.</typeparam>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The wrapped values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PullAllWrapped<T>(string name, IEnumerable<T> values)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (values == null) { throw new ArgumentNullException("values"); }
            var wrappedValues = new BsonArray(BsonDocumentWrapper.CreateMultiple(values).Cast<BsonValue>()); // the cast to BsonValue is required
            BsonElement element;
            if (_document.TryGetElement("$pullAll", out element))
            {
                element.Value.AsBsonDocument.Add(name, wrappedValues);
            }
            else
            {
                _document.Add("$pullAll", new BsonDocument(name, wrappedValues));
            }
            return this;
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of wrapped values (see $pullAll).
        /// </summary>
        /// <typeparam name="T">The type of wrapped values.</typeparam>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The wrapped values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PullAllWrapped<T>(string name, params T[] values)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (values == null) { throw new ArgumentNullException("values"); }
            return PullAllWrapped<T>(name, (IEnumerable<T>)values);
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to some wrapped value (see $pull).
        /// </summary>
        /// <typeparam name="T">The type of wrapped value.</typeparam>
        /// <param name="name">The name of the array element.</param>
        /// <param name="value">The wrapped value to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PullWrapped<T>(string name, T value)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (value == null) { throw new ArgumentNullException("value"); }
            var wrappedValue = BsonDocumentWrapper.Create(value);
            BsonElement element;
            if (_document.TryGetElement("$pull", out element))
            {
                element.Value.AsBsonDocument.Add(name, wrappedValue);
            }
            else
            {
                _document.Add("$pull", new BsonDocument(name, wrappedValue));
            }
            return this;
        }

        /// <summary>
        /// Adds a value to the end of the named array element (see $push).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="value">The value to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder Push(string name, BsonValue value)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (value == null) { throw new ArgumentNullException("value"); }
            BsonElement element;
            if (_document.TryGetElement("$push", out element))
            {
                element.Value.AsBsonDocument.Add(name, value);
            }
            else
            {
                _document.Add("$push", new BsonDocument(name, value));
            }
            return this;
        }

        /// <summary>
        /// Adds a list of values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PushAll(string name, BsonArray values)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (values == null) { throw new ArgumentNullException("values"); }
            BsonElement element;
            if (_document.TryGetElement("$pushAll", out element))
            {
                element.Value.AsBsonDocument.Add(name, values);
            }
            else
            {
                _document.Add("$pushAll", new BsonDocument(name, values));
            }
            return this;
        }

        /// <summary>
        /// Adds a list of values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PushAll(string name, IEnumerable<BsonValue> values)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (values == null) { throw new ArgumentNullException("values"); }
            return PushAll(name, new BsonArray(values));
        }

        /// <summary>
        /// Adds a list of values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PushAll(string name, params BsonValue[] values)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (values == null) { throw new ArgumentNullException("values"); }
            return PushAll(name, (IEnumerable<BsonValue>)values);
        }

        /// <summary>
        /// Adds a list of wrapped values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <typeparam name="T">The type of wrapped values.</typeparam>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The wrapped values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PushAllWrapped<T>(string name, IEnumerable<T> values)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (values == null) { throw new ArgumentNullException("values"); }
            var wrappedValues = new BsonArray(BsonDocumentWrapper.CreateMultiple(values).Cast<BsonValue>()); // the cast to BsonValue is required
            BsonElement element;
            if (_document.TryGetElement("$pushAll", out element))
            {
                element.Value.AsBsonDocument.Add(name, wrappedValues);
            }
            else
            {
                _document.Add("$pushAll", new BsonDocument(name, wrappedValues));
            }
            return this;
        }

        /// <summary>
        /// Adds a list of wrapped values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <typeparam name="T">The type of wrapped values.</typeparam>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The wrapped values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PushAllWrapped<T>(string name, params T[] values)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (values == null) { throw new ArgumentNullException("values"); }
            return PushAllWrapped(name, (IEnumerable<T>)values);
        }

        /// <summary>
        /// Adds a wrapped value to the end of the named array element (see $push).
        /// </summary>
        /// <typeparam name="T">The type of wrapped value.</typeparam>
        /// <param name="name">The name of the array element.</param>
        /// <param name="value">The wrapped value to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PushWrapped<T>(string name, T value)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (value == null) { throw new ArgumentNullException("value"); }
            var wrappedValue = BsonDocumentWrapper.Create<T>(value);
            BsonElement element;
            if (_document.TryGetElement("$push", out element))
            {
                element.Value.AsBsonDocument.Add(name, wrappedValue);
            }
            else
            {
                _document.Add("$push", new BsonDocument(name, wrappedValue));
            }
            return this;
        }

        /// <summary>
        /// Renames an element (see $rename).
        /// </summary>
        /// <param name="oldElementName">The old element name.</param>
        /// <param name="newElementName">The new element name.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder Rename(string oldElementName, string newElementName)
        {
            if (oldElementName == null) { throw new ArgumentNullException("oldElementName"); }
            if (newElementName == null) { throw new ArgumentNullException("newElementName"); }
            BsonElement element;
            if (_document.TryGetElement("$rename", out element))
            {
                element.Value.AsBsonDocument.Add(oldElementName, newElementName);
            }
            else
            {
                _document.Add("$rename", new BsonDocument(oldElementName, newElementName));
            }
            return this;
        }

        /// <summary>
        /// Sets the value of the named element to a new value (see $set).
        /// </summary>
        /// <param name="name">The name of the element to be set.</param>
        /// <param name="value">The new value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder Set(string name, BsonValue value)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (value == null) { throw new ArgumentNullException("value"); }
            BsonElement element;
            if (_document.TryGetElement("$set", out element))
            {
                element.Value.AsBsonDocument.Add(name, value);
            }
            else
            {
                _document.Add("$set", new BsonDocument(name, value));
            }
            return this;
        }

        /// <summary>
        /// Sets the value of the named element to a new wrapped value (see $set).
        /// </summary>
        /// <typeparam name="T">The type of wrapped value.</typeparam>
        /// <param name="name">The name of the element to be set.</param>
        /// <param name="value">The new wrapped value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder SetWrapped<T>(string name, T value)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (value == null) { throw new ArgumentNullException("value"); }
            var wrappedValue = BsonDocumentWrapper.Create<T>(value);
            BsonElement element;
            if (_document.TryGetElement("$set", out element))
            {
                element.Value.AsBsonDocument.Add(name, wrappedValue);
            }
            else
            {
                _document.Add("$set", new BsonDocument(name, wrappedValue));
            }
            return this;
        }

        /// <summary>
        /// Returns the result of the builder as a BsonDocument.
        /// </summary>
        /// <returns>A BsonDocument.</returns>
        public override BsonDocument ToBsonDocument()
        {
            return _document;
        }

        /// <summary>
        /// Removes the named element from the document (see $unset).
        /// </summary>
        /// <param name="name">The name of the element to be removed.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder Unset(string name)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            BsonElement element;
            if (_document.TryGetElement("$unset", out element))
            {
                element.Value.AsBsonDocument.Add(name, 1);
            }
            else
            {
                _document.Add("$unset", new BsonDocument(name, 1));
            }
            return this;
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

        // private methods
        private void BitwiseOperation(string name, string operation, BsonValue value)
        {
            BsonElement bitElement;
            if (!_document.TryGetElement("$bit", out bitElement))
            {
                bitElement = new BsonElement("$bit", new BsonDocument());
                _document.Add(bitElement);
            }
            var bitDocument = bitElement.Value.AsBsonDocument;

            BsonElement fieldElement;
            if (!bitDocument.TryGetElement(name, out fieldElement))
            {
                fieldElement = new BsonElement(name, new BsonDocument());
                bitDocument.Add(fieldElement);
            }
            var fieldDocument = fieldElement.Value.AsBsonDocument;

            fieldDocument.Add(operation, value);
        }

        private void Inc(string name, BsonValue value)
        {
            BsonElement incElement;
            if (!_document.TryGetElement("$inc", out incElement))
            {
                incElement = new BsonElement("$inc", new BsonDocument());
                _document.Add(incElement);
            }
            var incDocument = incElement.Value.AsBsonDocument;

            incDocument.Add(name, value);
        }
    }
}
