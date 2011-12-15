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
using MongoDB.Driver.Linq;
using MongoDB.Driver.Wrappers;

namespace MongoDB.Driver.Builders
{
    /// <summary>
    /// A builder for creating update modifiers.
    /// </summary>
    [Serializable]
    public class UpdateBuilder : BuilderBase, IMongoUpdate
    {
        // private fields
        private BsonDocument document;

        // constructors
        /// <summary>
        /// Initializes a new instance of the UpdateBuilder class.
        /// </summary>
        public UpdateBuilder()
        {
            document = new BsonDocument();
        }

        // internal properties
        internal BsonDocument Document
        {
            get { return document; }
        }

        // public methods
        /// <summary>
        /// Adds a value to a named array element if the value is not already in the array (see $addToSet).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="value">The value to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder AddToSet(
            string name,
            object value)
        {
            var bsonValue = BsonValue.Create(value);
            BsonElement element;
            if (document.TryGetElement("$addToSet", out element))
            {
                element.Value.AsBsonDocument.Add(name, bsonValue);
            }
            else
            {
                document.Add("$addToSet", new BsonDocument(name, bsonValue));
            }
            return this;
        }

        /// <summary>
        /// Adds a value to a named array element if the value is not already in the array (see $addToSet).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The value to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder AddToSet<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            object value)
        {
            return this.AddToSet(memberExpression.GetElementName(), value);
        }

        /// <summary>
        /// Adds a list of values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder AddToSetEach(
            string name,
            BsonArray values)
        {
            var arg = new BsonDocument("$each", values);
            BsonElement element;
            if (document.TryGetElement("$addToSet", out element))
            {
                element.Value.AsBsonDocument.Add(name, arg);
            }
            else
            {
                document.Add("$addToSet", new BsonDocument(name, arg));
            }
            return this;
        }

        /// <summary>
        /// Adds a list of values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="values">The values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder AddToSetEach<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            BsonArray values)
        {
            return this.AddToSetEach(memberExpression.GetElementName(), values);
        }

        /// <summary>
        /// Adds a list of values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder AddToSetEach(
            string name,
            IEnumerable<BsonValue> values)
        {
            return this.AddToSetEach(name, new BsonArray(values));
        }

        /// <summary>
        /// Adds a list of values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="values">The values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder AddToSetEach<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            IEnumerable<BsonValue> values)
        {
            return this.AddToSetEach(memberExpression.GetElementName(), values);
        }

        /// <summary>
        /// Adds a list of values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="arg1">The first value to add to the set.</param>
        /// <param name="arg2">The second value to add to the set.</param>
        /// <param name="args">The additional values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder AddToSetEach(
            string name,
            BsonValue arg1,
            BsonValue arg2,
            params BsonValue[] args)
        {
            return this.AddToSetEach(name, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
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
        public UpdateBuilder AddToSetEach<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            BsonValue arg1,
            BsonValue arg2,
            params BsonValue[] args)
        {
            return this.AddToSetEach(memberExpression, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Adds a list of values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder AddToSetEach(
            string name,
            IEnumerable values)
        {
            return this.AddToSetEach(name, new BsonArray(values));
        }

        /// <summary>
        /// Adds a list of values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="values">The values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder AddToSetEach<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            IEnumerable values)
        {
            return this.AddToSetEach(memberExpression.GetElementName(), values);
        }

        /// <summary>
        /// Adds a list of values to a named array element adding each value only if it not already in the array (see $addToSet and $each).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="arg1">The first value to add to the set.</param>
        /// <param name="arg2">The second value to add to the set.</param>
        /// <param name="args">The additional values to add to the set.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder AddToSetEach(
            string name,
            object arg1,
            object arg2,
            params object[] args)
        {
            return this.AddToSetEach(name, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
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
        public UpdateBuilder AddToSetEach<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            object arg1,
            object arg2,
            params object[] args)
        {
            return this.AddToSetEach(memberExpression, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Sets the named element to the bitwise and of its value with another value (see $bit with "and").
        /// </summary>
        /// <param name="name">The name of the element to be modified.</param>
        /// <param name="value">The value to be and-ed with the current value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder BitwiseAnd(string name, int value)
        {
            BitwiseOperation(name, "and", value);
            return this;
        }

        /// <summary>
        /// Sets the named element to the bitwise and of its value with another value (see $bit with "and").
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The value to be and-ed with the current value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder BitwiseAnd<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            int value)
        {
            return this.BitwiseAnd(memberExpression.GetElementName(), value);
        }

        /// <summary>
        /// Sets the named element to the bitwise and of its value with another value (see $bit with "and").
        /// </summary>
        /// <param name="name">The name of the element to be modified.</param>
        /// <param name="value">The value to be and-ed with the current value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder BitwiseAnd(string name, long value)
        {
            BitwiseOperation(name, "and", value);
            return this;
        }

        /// <summary>
        /// Sets the named element to the bitwise and of its value with another value (see $bit with "and").
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The value to be and-ed with the current value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder BitwiseAnd<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            long value)
        {
            return this.BitwiseAnd(memberExpression.GetElementName(), value);
        }

        /// <summary>
        /// Sets the named element to the bitwise or of its value with another value (see $bit with "or").
        /// </summary>
        /// <param name="name">The name of the element to be modified.</param>
        /// <param name="value">The value to be or-ed with the current value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder BitwiseOr(string name, int value)
        {
            BitwiseOperation(name, "or", value);
            return this;
        }

        /// <summary>
        /// Sets the named element to the bitwise or of its value with another value (see $bit with "or").
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The value to be or-ed with the current value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder BitwiseOr<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            int value)
        {
            return this.BitwiseOr(memberExpression.GetElementName(), value);
        }

        /// <summary>
        /// Sets the named element to the bitwise or of its value with another value (see $bit with "or").
        /// </summary>
        /// <param name="name">The name of the element to be modified.</param>
        /// <param name="value">The value to be or-ed with the current value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder BitwiseOr(string name, long value)
        {
            BitwiseOperation(name, "or", value);
            return this;
        }

        /// <summary>
        /// Sets the named element to the bitwise or of its value with another value (see $bit with "or").
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The value to be or-ed with the current value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder BitwiseOr<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            long value)
        {
            return this.BitwiseOr(memberExpression.GetElementName(), value);
        }

        /// <summary>
        /// Combines another UpdateBuilder into this one.
        /// </summary>
        /// <param name="otherUpdate">The UpdateBuilder to combine into this one.</param>
        /// <returns>A combined UpdateBuilder.</returns>
        public UpdateBuilder Combine(UpdateBuilder otherUpdate)
        {
            foreach (var otherOperation in otherUpdate.Document)
            {
                var otherOperationName = otherOperation.Name;
                var otherTargets = otherOperation.Value.AsBsonDocument;
                BsonElement operation;
                if (document.TryGetElement(otherOperationName, out operation))
                {
                    operation.Value.AsBsonDocument.Add(otherTargets);
                }
                else
                {
                    document.Add(otherOperationName, otherTargets);
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
            Inc(name, BsonValue.Create(value));
            return this;
        }

        /// <summary>
        /// Increments the named element by a value (see $inc).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The value to increment by.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder Inc<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            double value)
        {
            return this.Inc(memberExpression.GetElementName(), value);
        }

        /// <summary>
        /// Increments the named element by a value (see $inc).
        /// </summary>
        /// <param name="name">The name of the element to be incremented.</param>
        /// <param name="value">The value to increment by.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder Inc(string name, int value)
        {
            Inc(name, BsonValue.Create(value));
            return this;
        }

        /// <summary>
        /// Increments the named element by a value (see $inc).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The value to increment by.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder Inc<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            int value)
        {
            return this.Inc(memberExpression.GetElementName(), value);
        }

        /// <summary>
        /// Increments the named element by a value (see $inc).
        /// </summary>
        /// <param name="name">The name of the element to be incremented.</param>
        /// <param name="value">The value to increment by.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder Inc(string name, long value)
        {
            Inc(name, BsonValue.Create(value));
            return this;
        }

        /// <summary>
        /// Increments the named element by a value (see $inc).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The value to increment by.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder Inc<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            long value)
        {
            return this.Inc(memberExpression.GetElementName(), value);
        }

        /// <summary>
        /// Removes the first value from the named array element (see $pop).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PopFirst(string name)
        {
            BsonElement element;
            if (document.TryGetElement("$pop", out element))
            {
                element.Value.AsBsonDocument.Add(name, -1);
            }
            else
            {
                document.Add("$pop", new BsonDocument(name, -1));
            }
            return this;
        }

        /// <summary>
        /// Removes the first value from the named array element (see $pop).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PopFirst<TDocument>(
            Expression<Func<TDocument, object>> memberExpression)
        {
            return this.PopFirst(memberExpression.GetElementName());
        }

        /// <summary>
        /// Removes the last value from the named array element (see $pop).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PopLast(string name)
        {
            BsonElement element;
            if (document.TryGetElement("$pop", out element))
            {
                element.Value.AsBsonDocument.Add(name, 1);
            }
            else
            {
                document.Add("$pop", new BsonDocument(name, 1));
            }
            return this;
        }

        /// <summary>
        /// Removes the last value from the named array element (see $pop).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PopLast<TDocument>(
            Expression<Func<TDocument, object>> memberExpression)
        {
            return this.PopLast(memberExpression.GetElementName());
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to some value (see $pull).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="value">The value to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder Pull(
            string name,
            object value)
        {
            var bsonValue = BsonValue.Create(value);
            BsonElement element;
            if (document.TryGetElement("$pull", out element))
            {
                element.Value.AsBsonDocument.Add(name, bsonValue);
            }
            else
            {
                document.Add("$pull", new BsonDocument(name, bsonValue));
            }
            return this;
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to some value (see $pull).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The value to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder Pull<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            object value)
        {
            return this.Pull(memberExpression.GetElementName(), value);
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of values (see $pullAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PullAll(string name, BsonArray values)
        {
            BsonElement element;
            if (document.TryGetElement("$pullAll", out element))
            {
                element.Value.AsBsonDocument.Add(name, values);
            }
            else
            {
                document.Add("$pullAll", new BsonDocument(name, values));
            }
            return this;
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of values (see $pullAll).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="values">The values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PullAll<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            BsonArray values)
        {
            return this.PullAll(memberExpression.GetElementName(), values);
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of values (see $pullAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PullAll(
            string name,
            IEnumerable<BsonValue> values)
        {
            return this.PullAll(name, new BsonArray(values));
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of values (see $pullAll).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="values">The values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PullAll<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            IEnumerable<BsonValue> values)
        {
            return this.PullAll(memberExpression.GetElementName(), values);
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of values (see $pullAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="arg1">The first value to remove.</param>
        /// <param name="arg2">The second value to remove.</param>
        /// <param name="args">The additional values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PullAll(
            string name,
            BsonValue arg1,
            BsonValue arg2,
            params BsonValue[] args)
        {
            return this.PullAll(name, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
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
        public UpdateBuilder PullAll<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            BsonValue arg1,
            BsonValue arg2,
            params BsonValue[] args)
        {
            return this.PullAll(memberExpression, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of values (see $pullAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PullAll(
            string name,
            IEnumerable values)
        {
            return this.PullAll(name, new BsonArray(values));
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of values (see $pullAll).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="values">The values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PullAll<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            IEnumerable values)
        {
            return this.PullAll(memberExpression, values);
        }

        /// <summary>
        /// Removes all values from the named array element that are equal to any of a list of values (see $pullAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="arg1">The first value to remove.</param>
        /// <param name="arg2">The second value to remove.</param>
        /// <param name="args">The additional values to remove.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PullAll(
            string name,
            object arg1,
            object arg2,
            params object[] args)
        {
            return PullAll(name, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
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
        public UpdateBuilder PullAll<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            object arg1,
            object arg2,
            params object[] args)
        {
            return this.PullAll(memberExpression, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Adds a value to the end of the named array element (see $push).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="value">The value to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder Push(
            string name,
            object value)
        {
            var bsonValue = BsonValue.Create(value);
            BsonElement element;
            if (document.TryGetElement("$push", out element))
            {
                element.Value.AsBsonDocument.Add(name, bsonValue);
            }
            else
            {
                document.Add("$push", new BsonDocument(name, bsonValue));
            }
            return this;
        }

        /// <summary>
        /// Adds a value to the end of the named array element (see $push).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The value to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder Push<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            object value)
        {
            return this.Push(memberExpression.GetElementName(), value);
        }

        /// <summary>
        /// Adds a list of values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PushAll(string name, BsonArray values)
        {
            BsonElement element;
            if (document.TryGetElement("$pushAll", out element))
            {
                element.Value.AsBsonDocument.Add(name, values);
            }
            else
            {
                document.Add("$pushAll", new BsonDocument(name, values));
            }
            return this;
        }

        /// <summary>
        /// Adds a list of values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="values">The values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PushAll<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            BsonArray values)
        {
            return this.PushAll(memberExpression.GetElementName(), values);
        }

        /// <summary>
        /// Adds a list of values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PushAll(
            string name,
            IEnumerable<BsonValue> values)
        {
            return this.PushAll(name, new BsonArray(values));
        }

        /// <summary>
        /// Adds a list of values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="values">The values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PushAll<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            IEnumerable<BsonValue> values)
        {
            return this.PushAll(memberExpression.GetElementName(), values);
        }

        /// <summary>
        /// Adds a list of values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="arg1">The first value to add to the end of the array.</param>
        /// <param name="arg2">The second value to add to the end of the array.</param>
        /// <param name="args">The additional values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PushAll(
            string name,
            BsonValue arg1,
            BsonValue arg2,
            params BsonValue[] args)
        {
            return this.PushAll(name, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
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
        public UpdateBuilder PushAll<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            BsonValue arg1,
            BsonValue arg2,
            params BsonValue[] args)
        {
            return this.PushAll(memberExpression, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Adds a list of values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="values">The values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PushAll(
            string name,
            IEnumerable values)
        {
            return this.PushAll(name, new BsonArray(values));
        }

        /// <summary>
        /// Adds a list of values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="values">The values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PushAll<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            IEnumerable values)
        {
            return this.PushAll(memberExpression.GetElementName(), values);
        }

        /// <summary>
        /// Adds a list of values to the end of the named array element (see $pushAll).
        /// </summary>
        /// <param name="name">The name of the array element.</param>
        /// <param name="arg1">The first value to add to the end of the array.</param>
        /// <param name="arg2">The second value to add to the end of the array.</param>
        /// <param name="args">The additional values to add to the end of the array.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder PushAll(
            string name,
            object arg1,
            object arg2,
            params object[] args)
        {
            return this.PushAll(name, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
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
        public UpdateBuilder PushAll<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            object arg1,
            object arg2,
            params object[] args)
        {
            return this.PushAll(memberExpression, ParameterHelpers.ConvertToBsonValues(arg1, arg2, args));
        }

        /// <summary>
        /// Renames an element (see $rename).
        /// </summary>
        /// <param name="oldElementName">The old element name.</param>
        /// <param name="newElementName">The new element name.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder Rename(string oldElementName, string newElementName)
        {
            BsonElement element;
            if (document.TryGetElement("$rename", out element))
            {
                element.Value.AsBsonDocument.Add(oldElementName, newElementName);
            }
            else
            {
                document.Add("$rename", new BsonDocument(oldElementName, newElementName));
            }
            return this;
        }

        /// <summary>
        /// Sets the value of the named element to a new value (see $set).
        /// </summary>
        /// <param name="name">The name of the element to be set.</param>
        /// <param name="value">The new value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder Set(
            string name,
            object value)
        {
            var bsonValue = BsonValue.Create(value);
            BsonElement element;
            if (document.TryGetElement("$set", out element))
            {
                element.Value.AsBsonDocument.Add(name, bsonValue);
            }
            else
            {
                document.Add("$set", new BsonDocument(name, bsonValue));
            }
            return this;
        }

        /// <summary>
        /// Sets the value of the named element to a new value (see $set).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <param name="value">The new value.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder Set<TDocument>(
            Expression<Func<TDocument, object>> memberExpression,
            object value)
        {
            return this.Set(memberExpression.GetElementName(), value);
        }

        /// <summary>
        /// Returns the result of the builder as a BsonDocument.
        /// </summary>
        /// <returns>A BsonDocument.</returns>
        public override BsonDocument ToBsonDocument()
        {
            return document;
        }

        /// <summary>
        /// Removes the named element from the document (see $unset).
        /// </summary>
        /// <param name="name">The name of the element to be removed.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder Unset(string name)
        {
            BsonElement element;
            if (document.TryGetElement("$unset", out element))
            {
                element.Value.AsBsonDocument.Add(name, 1);
            }
            else
            {
                document.Add("$unset", new BsonDocument(name, 1));
            }
            return this;
        }

        /// <summary>
        /// Removes the named element from the document (see $unset).
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="memberExpression">A lambda expression specifying the member.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public UpdateBuilder Unset<TDocument>(
            Expression<Func<TDocument, object>> memberExpression)
        {
            return this.Unset(memberExpression.GetElementName());
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
            document.Serialize(bsonWriter, nominalType, options);
        }

        // private methods
        private void BitwiseOperation(string name, string operation, BsonValue value)
        {
            BsonElement bitElement;
            if (!document.TryGetElement("$bit", out bitElement))
            {
                bitElement = new BsonElement("$bit", new BsonDocument());
                document.Add(bitElement);
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
            if (!document.TryGetElement("$inc", out incElement))
            {
                incElement = new BsonElement("$inc", new BsonDocument());
                document.Add(incElement);
            }
            var incDocument = incElement.Value.AsBsonDocument;

            incDocument.Add(name, value);
        }
    }
}
