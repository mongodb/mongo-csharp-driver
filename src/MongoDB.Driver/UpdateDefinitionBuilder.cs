/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Extension methods for UpdateDefinition.
    /// </summary>
    public static class UpdateDefinitionExtensions
    {
        private static class BuilderCache<TDocument>
        {
            public static Update2Builder<TDocument> Instance = new Update2Builder<TDocument>();
        }

        /// <summary>
        /// Combines an existing update with an add to set operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> AddToSet<TDocument, TField, TItem>(this UpdateDefinition<TDocument> update, FieldName<TDocument, TField> fieldName, TItem value)
            where TField : IEnumerable<TItem>
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.AddToSet<TField, TItem>(fieldName, value));
        }

        /// <summary>
        /// Combines an existing update with an add to set operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> AddToSet<TDocument, TItem>(this UpdateDefinition<TDocument> update, string fieldName, TItem value)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.AddToSet<TItem>(fieldName, value));
        }

        /// <summary>
        /// Combines an existing update with an add to set operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> AddToSet<TDocument, TField, TItem>(this UpdateDefinition<TDocument> update, Expression<Func<TDocument, TField>> fieldName, TItem value)
            where TField : IEnumerable<TItem>
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.AddToSet<TField, TItem>(fieldName, value));
        }

        /// <summary>
        /// Combines an existing update with an add to set operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> AddToSetEach<TDocument, TField, TItem>(this UpdateDefinition<TDocument> update, FieldName<TDocument, TField> fieldName, IEnumerable<TItem> values)
            where TField : IEnumerable<TItem>
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.AddToSetEach<TField, TItem>(fieldName, values));
        }

        /// <summary>
        /// Combines an existing update with an add to set operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> AddToSetEach<TDocument, TItem>(this UpdateDefinition<TDocument> update, string fieldName, IEnumerable<TItem> values)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.AddToSetEach<TItem>(fieldName, values));
        }

        /// <summary>
        /// Combines an existing update with an add to set operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> AddToSetEach<TDocument, TField, TItem>(this UpdateDefinition<TDocument> update, Expression<Func<TDocument, TField>> fieldName, IEnumerable<TItem> values)
            where TField : IEnumerable<TItem>
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.AddToSetEach<TField, TItem>(fieldName, values));
        }

        /// <summary>
        /// Combines an existing update with a bitwise and operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> BitwiseAnd<TDocument, TField>(this UpdateDefinition<TDocument> update, FieldName<TDocument, TField> fieldName, TField value)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.BitwiseAnd(fieldName, value));
        }

        /// <summary>
        /// Combines an existing update with a bitwise and operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> BitwiseAnd<TDocument, TField>(this UpdateDefinition<TDocument> update, Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.BitwiseAnd(fieldName, value));
        }

        /// <summary>
        /// Combines an existing update with a bitwise or operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> BitwiseOr<TDocument, TField>(this UpdateDefinition<TDocument> update, FieldName<TDocument, TField> fieldName, TField value)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.BitwiseOr(fieldName, value));
        }

        /// <summary>
        /// Combines an existing update with a bitwise or operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> BitwiseOr<TDocument, TField>(this UpdateDefinition<TDocument> update, Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.BitwiseOr(fieldName, value));
        }

        /// <summary>
        /// Combines an existing update with a bitwise xor operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> BitwiseXor<TDocument, TField>(this UpdateDefinition<TDocument> update, FieldName<TDocument, TField> fieldName, TField value)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.BitwiseXor(fieldName, value));
        }

        /// <summary>
        /// Combines an existing update with a bitwise xor operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> BitwiseXor<TDocument, TField>(this UpdateDefinition<TDocument> update, Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.BitwiseXor(fieldName, value));
        }

        /// <summary>
        /// Combines an existing update with a current date operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="type">The type.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> CurrentDate<TDocument>(this UpdateDefinition<TDocument> update, FieldName<TDocument> fieldName, Update2CurrentDateType? type = null)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.CurrentDate(fieldName, type));
        }

        /// <summary>
        /// Combines an existing update with a current date operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="type">The type.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> CurrentDate<TDocument>(this UpdateDefinition<TDocument> update, Expression<Func<TDocument, object>> fieldName, Update2CurrentDateType? type = null)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.CurrentDate(fieldName, type));
        }

        /// <summary>
        /// Combines an existing update with an increment operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> Inc<TDocument, TField>(this UpdateDefinition<TDocument> update, FieldName<TDocument, TField> fieldName, TField value)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.Inc(fieldName, value));
        }

        /// <summary>
        /// Combines an existing update with an increment operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> Inc<TDocument, TField>(this UpdateDefinition<TDocument> update, Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.Inc(fieldName, value));
        }

        /// <summary>
        /// Combines an existing update with a max operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> Max<TDocument, TField>(this UpdateDefinition<TDocument> update, FieldName<TDocument, TField> fieldName, TField value)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.Max(fieldName, value));
        }

        /// <summary>
        /// Combines an existing update with a max operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> Max<TDocument, TField>(this UpdateDefinition<TDocument> update, Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.Max(fieldName, value));
        }

        /// <summary>
        /// Combines an existing update with a min operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> Min<TDocument, TField>(this UpdateDefinition<TDocument> update, FieldName<TDocument, TField> fieldName, TField value)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.Min(fieldName, value));
        }

        /// <summary>
        /// Combines an existing update with a min operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> Min<TDocument, TField>(this UpdateDefinition<TDocument> update, Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.Min(fieldName, value));
        }

        /// <summary>
        /// Combines an existing update with a multiply operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> Mul<TDocument, TField>(this UpdateDefinition<TDocument> update, FieldName<TDocument, TField> fieldName, TField value)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.Mul(fieldName, value));
        }

        /// <summary>
        /// Combines an existing update with a multiply operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> Mul<TDocument, TField>(this UpdateDefinition<TDocument> update, Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.Mul(fieldName, value));
        }

        /// <summary>
        /// Combines an existing update with a pop operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> PopFirst<TDocument>(this UpdateDefinition<TDocument> update, FieldName<TDocument> fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.PopFirst(fieldName));
        }

        /// <summary>
        /// Combines an existing update with a pop operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> PopFirst<TDocument>(this UpdateDefinition<TDocument> update, Expression<Func<TDocument, object>> fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.PopFirst(fieldName));
        }

        /// <summary>
        /// Combines an existing update with a pop operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> PopLast<TDocument>(this UpdateDefinition<TDocument> update, FieldName<TDocument> fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.PopLast(fieldName));
        }

        /// <summary>
        /// Combines an existing update with a pop operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> PopLast<TDocument>(this UpdateDefinition<TDocument> update, Expression<Func<TDocument, object>> fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.PopLast(fieldName));
        }

        /// <summary>
        /// Combines an existing update with a pull operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> Pull<TDocument, TField, TItem>(this UpdateDefinition<TDocument> update, FieldName<TDocument, TField> fieldName, TItem value)
            where TField : IEnumerable<TItem>
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.Pull(fieldName, value));
        }

        /// <summary>
        /// Combines an existing update with a pull operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> Pull<TDocument, TItem>(this UpdateDefinition<TDocument> update, string fieldName, TItem value)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.Pull(fieldName, value));
        }

        /// <summary>
        /// Combines an existing update with a pull operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> Pull<TDocument, TField, TItem>(this UpdateDefinition<TDocument> update, Expression<Func<TDocument, TField>> fieldName, TItem value)
            where TField : IEnumerable<TItem>
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.Pull(fieldName, value));
        }

        /// <summary>
        /// Combines an existing update with a pull operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> PullAll<TDocument, TField, TItem>(this UpdateDefinition<TDocument> update, FieldName<TDocument, TField> fieldName, IEnumerable<TItem> values)
            where TField : IEnumerable<TItem>
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.PullAll(fieldName, values));
        }

        /// <summary>
        /// Combines an existing update with a pull operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> PullAll<TDocument, TItem>(this UpdateDefinition<TDocument> update, string fieldName, IEnumerable<TItem> values)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.PullAll(fieldName, values));
        }

        /// <summary>
        /// Combines an existing update with a pull operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> PullAll<TDocument, TField, TItem>(this UpdateDefinition<TDocument> update, Expression<Func<TDocument, TField>> fieldName, IEnumerable<TItem> values)
            where TField : IEnumerable<TItem>
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.PullAll(fieldName, values));
        }

        /// <summary>
        /// Combines an existing update with a pull operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> PullFilter<TDocument, TField, TItem>(this UpdateDefinition<TDocument> update, FieldName<TDocument, TField> fieldName, FilterDefinition<TItem> filter)
            where TField : IEnumerable<TItem>
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.PullFilter(fieldName, filter));
        }

        /// <summary>
        /// Combines an existing update with a pull operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> PullFilter<TDocument, TItem>(this UpdateDefinition<TDocument> update, string fieldName, FilterDefinition<TItem> filter)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.PullFilter(fieldName, filter));
        }

        /// <summary>
        /// Combines an existing update with a pull operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> PullFilter<TDocument, TField, TItem>(this UpdateDefinition<TDocument> update, Expression<Func<TDocument, TField>> fieldName, FilterDefinition<TItem> filter)
            where TField : IEnumerable<TItem>
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.PullFilter(fieldName, filter));
        }

        /// <summary>
        /// Combines an existing update with a pull operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> PullFilter<TDocument, TField, TItem>(this UpdateDefinition<TDocument> update, Expression<Func<TDocument, TField>> fieldName, Expression<Func<TItem, bool>> filter)
            where TField : IEnumerable<TItem>
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.PullFilter(fieldName, filter));
        }

        /// <summary>
        /// Combines an existing update with a push operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> Push<TDocument, TField, TItem>(this UpdateDefinition<TDocument> update, FieldName<TDocument, TField> fieldName, TItem value)
            where TField : IEnumerable<TItem>
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.Push(fieldName, value));
        }

        /// <summary>
        /// Combines an existing update with a push operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> Push<TDocument, TItem>(this UpdateDefinition<TDocument> update, string fieldName, TItem value)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.Push(fieldName, value));
        }

        /// <summary>
        /// Combines an existing update with a push operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> Push<TDocument, TField, TItem>(this UpdateDefinition<TDocument> update, Expression<Func<TDocument, TField>> fieldName, TItem value)
            where TField : IEnumerable<TItem>
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.Push(fieldName, value));
        }

        /// <summary>
        /// Combines an existing update with a push operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <param name="slice">The slice.</param>
        /// <param name="position">The position.</param>
        /// <param name="sort">The sort.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> PushEach<TDocument, TField, TItem>(this UpdateDefinition<TDocument> update, FieldName<TDocument, TField> fieldName, IEnumerable<TItem> values, int? slice = null, int? position = null, SortDefinition<TItem> sort = null)
            where TField : IEnumerable<TItem>
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.PushEach(fieldName, values, slice, position, sort));
        }

        /// <summary>
        /// Combines an existing update with a push operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <param name="slice">The slice.</param>
        /// <param name="position">The position.</param>
        /// <param name="sort">The sort.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> PushEach<TDocument, TItem>(this UpdateDefinition<TDocument> update, string fieldName, IEnumerable<TItem> values, int? slice = null, int? position = null, SortDefinition<TItem> sort = null)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.PushEach(fieldName, values, slice, position, sort));
        }

        /// <summary>
        /// Combines an existing update with a push operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <param name="slice">The slice.</param>
        /// <param name="position">The position.</param>
        /// <param name="sort">The sort.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> PushEach<TDocument, TField, TItem>(this UpdateDefinition<TDocument> update, Expression<Func<TDocument, TField>> fieldName, IEnumerable<TItem> values, int? slice = null, int? position = null, SortDefinition<TItem> sort = null)
            where TField : IEnumerable<TItem>
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.PushEach(fieldName, values, slice, position, sort));
        }

        /// <summary>
        /// Combines an existing update with a field renaming operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="newName">The new name.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> Rename<TDocument>(this UpdateDefinition<TDocument> update, FieldName<TDocument> fieldName, string newName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.Rename(fieldName, newName));
        }

        /// <summary>
        /// Combines an existing update with a field renaming operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="newName">The new name.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> Rename<TDocument>(this UpdateDefinition<TDocument> update, Expression<Func<TDocument, object>> fieldName, string newName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.Rename(fieldName, newName));
        }

        /// <summary>
        /// Combines an existing update with a set operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> Set<TDocument, TField>(this UpdateDefinition<TDocument> update, FieldName<TDocument, TField> fieldName, TField value)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.Set(fieldName, value));
        }

        /// <summary>
        /// Combines an existing update with a set operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> Set<TDocument, TField>(this UpdateDefinition<TDocument> update, Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.Set(fieldName, value));
        }

        /// <summary>
        /// Combines an existing update with a set on insert operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> SetOnInsert<TDocument, TField>(this UpdateDefinition<TDocument> update, FieldName<TDocument, TField> fieldName, TField value)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.SetOnInsert(fieldName, value));
        }

        /// <summary>
        /// Combines an existing update with a set on insert operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> SetOnInsert<TDocument, TField>(this UpdateDefinition<TDocument> update, Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.SetOnInsert(fieldName, value));
        }

        /// <summary>
        /// Combines an existing update with an unset operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> Unset<TDocument>(this UpdateDefinition<TDocument> update, FieldName<TDocument> fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.Unset(fieldName));
        }

        /// <summary>
        /// Combines an existing update with an unset operator.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="update">The update.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// A combined update.
        /// </returns>
        public static UpdateDefinition<TDocument> Unset<TDocument>(this UpdateDefinition<TDocument> update, Expression<Func<TDocument, object>> fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(update, builder.Unset(fieldName));
        }
    }

    /// <summary>
    /// The type to use for a $currentDate operator.
    /// </summary>
    public enum Update2CurrentDateType
    {
        /// <summary>
        /// A date.
        /// </summary>
        Date,
        /// <summary>
        /// A timestamp.
        /// </summary>
        Timestamp
    }

    /// <summary>
    /// A builder for an <see cref="UpdateDefinition{TDocument}"/>.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class Update2Builder<TDocument>
    {
        /// <summary>
        /// Creates an add to set operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>An add to set operator.</returns>
        public UpdateDefinition<TDocument> AddToSet<TField, TItem>(FieldName<TDocument, TField> fieldName, TItem value)
            where TField : IEnumerable<TItem>
        {
            return new AddToSetUpdateDefinition<TDocument, TField, TItem>(
                fieldName,
                new[] { value });
        }

        /// <summary>
        /// Creates an add to set operator.
        /// </summary>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>An add to set operator.</returns>
        public UpdateDefinition<TDocument> AddToSet<TItem>(string fieldName, TItem value)
        {
            return AddToSet<IEnumerable<TItem>, TItem>(
                new StringFieldName<TDocument, IEnumerable<TItem>>(fieldName),
                value);
        }

        /// <summary>
        /// Creates an add to set operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>An add to set operator.</returns>
        public UpdateDefinition<TDocument> AddToSet<TField, TItem>(Expression<Func<TDocument, TField>> fieldName, TItem value)
            where TField : IEnumerable<TItem>
        {
            return AddToSet<TField, TItem>(new ExpressionFieldName<TDocument, TField>(fieldName), value);
        }

        /// <summary>
        /// Creates an add to set operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <returns>An add to set operator.</returns>
        public UpdateDefinition<TDocument> AddToSetEach<TField, TItem>(FieldName<TDocument, TField> fieldName, IEnumerable<TItem> values)
            where TField : IEnumerable<TItem>
        {
            return new AddToSetUpdateDefinition<TDocument, TField, TItem>(fieldName, values);
        }

        /// <summary>
        /// Creates an add to set operator.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <returns>An add to set operator.</returns>
        public UpdateDefinition<TDocument> AddToSetEach<TItem>(string fieldName, IEnumerable<TItem> values)
        {
            return AddToSetEach<IEnumerable<TItem>, TItem>(
                new StringFieldName<TDocument, IEnumerable<TItem>>(fieldName),
                values);
        }

        /// <summary>
        /// Creates an add to set operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <returns>An add to set operator.</returns>
        public UpdateDefinition<TDocument> AddToSetEach<TField, TItem>(Expression<Func<TDocument, TField>> fieldName, IEnumerable<TItem> values)
            where TField : IEnumerable<TItem>
        {
            return AddToSetEach(new ExpressionFieldName<TDocument, TField>(fieldName), values);
        }

        /// <summary>
        /// Creates a bitwise and operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A bitwise and operator.</returns>
        public UpdateDefinition<TDocument> BitwiseAnd<TField>(FieldName<TDocument, TField> fieldName, TField value)
        {
            return new BitwiseOperatorUpdateDefinition<TDocument, TField>("and", fieldName, value);
        }

        /// <summary>
        /// Creates a bitwise and operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A bitwise and operator.</returns>
        public UpdateDefinition<TDocument> BitwiseAnd<TField>(Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            return BitwiseAnd(new ExpressionFieldName<TDocument, TField>(fieldName), value);
        }

        /// <summary>
        /// Creates a bitwise or operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A bitwise or operator.</returns>
        public UpdateDefinition<TDocument> BitwiseOr<TField>(FieldName<TDocument, TField> fieldName, TField value)
        {
            return new BitwiseOperatorUpdateDefinition<TDocument, TField>("or", fieldName, value);
        }

        /// <summary>
        /// Creates a bitwise or operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A bitwise or operator.</returns>
        public UpdateDefinition<TDocument> BitwiseOr<TField>(Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            return BitwiseOr(new ExpressionFieldName<TDocument, TField>(fieldName), value);
        }

        /// <summary>
        /// Creates a bitwise xor operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A bitwise xor operator.</returns>
        public UpdateDefinition<TDocument> BitwiseXor<TField>(FieldName<TDocument, TField> fieldName, TField value)
        {
            return new BitwiseOperatorUpdateDefinition<TDocument, TField>("xor", fieldName, value);
        }

        /// <summary>
        /// Creates a bitwise xor operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A bitwise xor operator.</returns>
        public UpdateDefinition<TDocument> BitwiseXor<TField>(Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            return BitwiseXor(new ExpressionFieldName<TDocument, TField>(fieldName), value);
        }

        /// <summary>
        /// Creates a combined update.
        /// </summary>
        /// <param name="updates">The updates.</param>
        /// <returns>A combined update.</returns>
        public UpdateDefinition<TDocument> Combine(params UpdateDefinition<TDocument>[] updates)
        {
            return Combine((IEnumerable<UpdateDefinition<TDocument>>)updates);
        }

        /// <summary>
        /// Creates a combined update.
        /// </summary>
        /// <param name="updates">The updates.</param>
        /// <returns>A combined update.</returns>
        public UpdateDefinition<TDocument> Combine(IEnumerable<UpdateDefinition<TDocument>> updates)
        {
            return new CombinedUpdateDefinition<TDocument>(updates);
        }

        /// <summary>
        /// Creates a current date operator.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="type">The type.</param>
        /// <returns>A current date operator.</returns>
        public UpdateDefinition<TDocument> CurrentDate(FieldName<TDocument> fieldName, Update2CurrentDateType? type = null)
        {
            BsonValue value;
            if (type.HasValue)
            {
                switch (type.Value)
                {
                    case Update2CurrentDateType.Date:
                        value = new BsonDocument("$type", "date");
                        break;
                    case Update2CurrentDateType.Timestamp:
                        value = new BsonDocument("$type", "timestamp");
                        break;
                    default:
                        throw new InvalidOperationException("Unknown value for " + typeof(Update2CurrentDateType));
                }
            }
            else
            {
                value = true;
            }

            return new OperatorUpdateDefinition<TDocument>("$currentDate", fieldName, value);
        }

        /// <summary>
        /// Creates a current date operator.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="type">The type.</param>
        /// <returns>A current date operator.</returns>
        public UpdateDefinition<TDocument> CurrentDate(Expression<Func<TDocument, object>> fieldName, Update2CurrentDateType? type = null)
        {
            return CurrentDate(new ExpressionFieldName<TDocument>(fieldName), type);
        }

        /// <summary>
        /// Creates an increment operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>An increment operator.</returns>
        public UpdateDefinition<TDocument> Inc<TField>(FieldName<TDocument, TField> fieldName, TField value)
        {
            return new OperatorUpdateDefinition<TDocument, TField>("$inc", fieldName, value);
        }

        /// <summary>
        /// Creates an increment operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>An increment operator.</returns>
        public UpdateDefinition<TDocument> Inc<TField>(Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            return Inc(new ExpressionFieldName<TDocument, TField>(fieldName), value);
        }

        /// <summary>
        /// Creates a max operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A max operator.</returns>
        public UpdateDefinition<TDocument> Max<TField>(FieldName<TDocument, TField> fieldName, TField value)
        {
            return new OperatorUpdateDefinition<TDocument, TField>("$max", fieldName, value);
        }

        /// <summary>
        /// Creates a max operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A max operator.</returns>
        public UpdateDefinition<TDocument> Max<TField>(Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            return Max(new ExpressionFieldName<TDocument, TField>(fieldName), value);
        }

        /// <summary>
        /// Creates a min operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A min operator.</returns>
        public UpdateDefinition<TDocument> Min<TField>(FieldName<TDocument, TField> fieldName, TField value)
        {
            return new OperatorUpdateDefinition<TDocument, TField>("$min", fieldName, value);
        }

        /// <summary>
        /// Creates a min operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A min operator.</returns>
        public UpdateDefinition<TDocument> Min<TField>(Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            return Min(new ExpressionFieldName<TDocument, TField>(fieldName), value);
        }

        /// <summary>
        /// Creates a multiply operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A multiply operator.</returns>
        public UpdateDefinition<TDocument> Mul<TField>(FieldName<TDocument, TField> fieldName, TField value)
        {
            return new OperatorUpdateDefinition<TDocument, TField>("$mul", fieldName, value);
        }

        /// <summary>
        /// Creates a multiply operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A multiply operator.</returns>
        public UpdateDefinition<TDocument> Mul<TField>(Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            return Mul(new ExpressionFieldName<TDocument, TField>(fieldName), value);
        }

        /// <summary>
        /// Creates a pop operator.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>A pop operator.</returns>
        public UpdateDefinition<TDocument> PopFirst(FieldName<TDocument> fieldName)
        {
            return new OperatorUpdateDefinition<TDocument>("$pop", fieldName, -1);
        }

        /// <summary>
        /// Creates a pop first operator.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>A pop first operator.</returns>
        public UpdateDefinition<TDocument> PopFirst(Expression<Func<TDocument, object>> fieldName)
        {
            return PopFirst(new ExpressionFieldName<TDocument>(fieldName));
        }

        /// <summary>
        /// Creates a pop operator.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>A pop operator.</returns>
        public UpdateDefinition<TDocument> PopLast(FieldName<TDocument> fieldName)
        {
            return new OperatorUpdateDefinition<TDocument>("$pop", fieldName, 1);
        }

        /// <summary>
        /// Creates a pop first operator.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>A pop first operator.</returns>
        public UpdateDefinition<TDocument> PopLast(Expression<Func<TDocument, object>> fieldName)
        {
            return PopLast(new ExpressionFieldName<TDocument>(fieldName));
        }

        /// <summary>
        /// Creates a pull operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A pull operator.</returns>
        public UpdateDefinition<TDocument> Pull<TField, TItem>(FieldName<TDocument, TField> fieldName, TItem value)
            where TField : IEnumerable<TItem>
        {
            return new PullUpdateDefinition<TDocument, TField, TItem>(fieldName, new[] { value });
        }

        /// <summary>
        /// Creates a pull operator.
        /// </summary>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A pull operator.</returns>
        public UpdateDefinition<TDocument> Pull<TItem>(string fieldName, TItem value)
        {
            return Pull(new StringFieldName<TDocument, IEnumerable<TItem>>(fieldName), value);
        }

        /// <summary>
        /// Creates a pull operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A pull operator.</returns>
        public UpdateDefinition<TDocument> Pull<TField, TItem>(Expression<Func<TDocument, TField>> fieldName, TItem value)
            where TField : IEnumerable<TItem>
        {
            return Pull<TField, TItem>(new ExpressionFieldName<TDocument, TField>(fieldName), value);
        }

        /// <summary>
        /// Creates a pull operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <returns>A pull operator.</returns>
        public UpdateDefinition<TDocument> PullAll<TField, TItem>(FieldName<TDocument, TField> fieldName, IEnumerable<TItem> values)
            where TField : IEnumerable<TItem>
        {
            return new PullUpdateDefinition<TDocument, TField, TItem>(fieldName, values);
        }

        /// <summary>
        /// Creates a pull operator.
        /// </summary>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <returns>A pull operator.</returns>
        public UpdateDefinition<TDocument> PullAll<TItem>(string fieldName, IEnumerable<TItem> values)
        {
            return PullAll<IEnumerable<TItem>, TItem>(new StringFieldName<TDocument, IEnumerable<TItem>>(fieldName), values);
        }

        /// <summary>
        /// Creates a pull operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <returns>A pull operator.</returns>
        public UpdateDefinition<TDocument> PullAll<TField, TItem>(Expression<Func<TDocument, TField>> fieldName, IEnumerable<TItem> values)
            where TField : IEnumerable<TItem>
        {
            return PullAll(new ExpressionFieldName<TDocument, TField>(fieldName), values);
        }

        /// <summary>
        /// Creates a pull operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>A pull operator.</returns>
        public UpdateDefinition<TDocument> PullFilter<TField, TItem>(FieldName<TDocument, TField> fieldName, FilterDefinition<TItem> filter)
            where TField : IEnumerable<TItem>
        {
            return new PullUpdateDefinition<TDocument, TField, TItem>(fieldName, filter);
        }

        /// <summary>
        /// Creates a pull operator.
        /// </summary>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>A pull operator.</returns>
        public UpdateDefinition<TDocument> PullFilter<TItem>(string fieldName, FilterDefinition<TItem> filter)
        {
            return PullFilter<IEnumerable<TItem>, TItem>(new StringFieldName<TDocument, IEnumerable<TItem>>(fieldName), filter);
        }

        /// <summary>
        /// Creates a pull operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>A pull operator.</returns>
        public UpdateDefinition<TDocument> PullFilter<TField, TItem>(Expression<Func<TDocument, TField>> fieldName, FilterDefinition<TItem> filter)
            where TField : IEnumerable<TItem>
        {
            return PullFilter(new ExpressionFieldName<TDocument, TField>(fieldName), filter);
        }

        /// <summary>
        /// Creates a pull operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>A pull operator.</returns>
        public UpdateDefinition<TDocument> PullFilter<TField, TItem>(Expression<Func<TDocument, TField>> fieldName, Expression<Func<TItem, bool>> filter)
            where TField : IEnumerable<TItem>
        {
            return PullFilter(new ExpressionFieldName<TDocument, TField>(fieldName), new ExpressionFilterDefinition<TItem>(filter));
        }

        /// <summary>
        /// Creates a push operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A push operator.</returns>
        public UpdateDefinition<TDocument> Push<TField, TItem>(FieldName<TDocument, TField> fieldName, TItem value)
            where TField : IEnumerable<TItem>
        {
            return new PushUpdateDefinition<TDocument, TField, TItem>(fieldName, new[] { value });
        }

        /// <summary>
        /// Creates a push operator.
        /// </summary>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A push operator.</returns>
        public UpdateDefinition<TDocument> Push<TItem>(string fieldName, TItem value)
        {
            return Push<IEnumerable<TItem>, TItem>(
                new StringFieldName<TDocument, IEnumerable<TItem>>(fieldName),
                value);
        }

        /// <summary>
        /// Creates a push operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A push operator.</returns>
        public UpdateDefinition<TDocument> Push<TField, TItem>(Expression<Func<TDocument, TField>> fieldName, TItem value)
            where TField : IEnumerable<TItem>
        {
            return Push(new ExpressionFieldName<TDocument, TField>(fieldName), value);
        }

        /// <summary>
        /// Creates a push operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <param name="slice">The slice.</param>
        /// <param name="position">The position.</param>
        /// <param name="sort">The sort.</param>
        /// <returns>A push operator.</returns>
        public UpdateDefinition<TDocument> PushEach<TField, TItem>(FieldName<TDocument, TField> fieldName, IEnumerable<TItem> values, int? slice = null, int? position = null, SortDefinition<TItem> sort = null)
            where TField : IEnumerable<TItem>
        {
            return new PushUpdateDefinition<TDocument, TField, TItem>(fieldName, values, slice, position, sort);
        }

        /// <summary>
        /// Creates a push operator.
        /// </summary>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <param name="slice">The slice.</param>
        /// <param name="position">The position.</param>
        /// <param name="sort">The sort.</param>
        /// <returns>A push operator.</returns>
        public UpdateDefinition<TDocument> PushEach<TItem>(string fieldName, IEnumerable<TItem> values, int? slice = null, int? position = null, SortDefinition<TItem> sort = null)
        {
            return PushEach<IEnumerable<TItem>, TItem>(
                new StringFieldName<TDocument, IEnumerable<TItem>>(fieldName),
                values, 
                slice, 
                position, 
                sort);
        }

        /// <summary>
        /// Creates a push operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <param name="slice">The slice.</param>
        /// <param name="position">The position.</param>
        /// <param name="sort">The sort.</param>
        /// <returns>A push operator.</returns>
        public UpdateDefinition<TDocument> PushEach<TField, TItem>(Expression<Func<TDocument, TField>> fieldName, IEnumerable<TItem> values, int? slice = null, int? position = null, SortDefinition<TItem> sort = null)
            where TField : IEnumerable<TItem>
        {
            return PushEach(new ExpressionFieldName<TDocument, TField>(fieldName), values, slice, position, sort);
        }

        /// <summary>
        /// Creates a field renaming operator.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="newName">The new name.</param>
        /// <returns>A field rename operator.</returns>
        public UpdateDefinition<TDocument> Rename(FieldName<TDocument> fieldName, string newName)
        {
            return new OperatorUpdateDefinition<TDocument>("$rename", fieldName, newName);
        }

        /// <summary>
        /// Creates a field renaming operator.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="newName">The new name.</param>
        /// <returns>A field rename operator.</returns>
        public UpdateDefinition<TDocument> Rename(Expression<Func<TDocument, object>> fieldName, string newName)
        {
            return Rename(new ExpressionFieldName<TDocument>(fieldName), newName);
        }

        /// <summary>
        /// Creates a set operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A set operator.</returns>
        public UpdateDefinition<TDocument> Set<TField>(FieldName<TDocument, TField> fieldName, TField value)
        {
            return new OperatorUpdateDefinition<TDocument, TField>("$set", fieldName, value);
        }

        /// <summary>
        /// Creates a set operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A set operator.</returns>
        public UpdateDefinition<TDocument> Set<TField>(Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            return Set(new ExpressionFieldName<TDocument, TField>(fieldName), value);
        }

        /// <summary>
        /// Creates a set on insert operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A set on insert operator.</returns>
        public UpdateDefinition<TDocument> SetOnInsert<TField>(FieldName<TDocument, TField> fieldName, TField value)
        {
            return new OperatorUpdateDefinition<TDocument, TField>("$setOnInsert", fieldName, value);
        }

        /// <summary>
        /// Creates a set on insert operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A set on insert operator.</returns>
        public UpdateDefinition<TDocument> SetOnInsert<TField>(Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            return SetOnInsert(new ExpressionFieldName<TDocument, TField>(fieldName), value);
        }

        /// <summary>
        /// Creates an unset operator.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>An unset operator.</returns>
        public UpdateDefinition<TDocument> Unset(FieldName<TDocument> fieldName)
        {
            return new OperatorUpdateDefinition<TDocument>("$unset", fieldName, 1);
        }

        /// <summary>
        /// Creates an unset operator.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>An unset operator.</returns>
        public UpdateDefinition<TDocument> Unset(Expression<Func<TDocument, object>> fieldName)
        {
            return Unset(new ExpressionFieldName<TDocument>(fieldName));
        }
    }

    internal sealed class AddToSetUpdateDefinition<TDocument, TField, TItem> : UpdateDefinition<TDocument>
    {
        private readonly FieldName<TDocument, TField> _fieldName;
        private readonly List<TItem> _values;

        public AddToSetUpdateDefinition(FieldName<TDocument, TField> fieldName, IEnumerable<TItem> values)
        {
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _values = Ensure.IsNotNull(values, "values").ToList();
        }

        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var renderedFieldName = _fieldName.Render(documentSerializer, serializerRegistry);

            var arraySerializer = renderedFieldName.FieldSerializer as IBsonArraySerializer;
            if (arraySerializer == null)
            {
                var message = string.Format("The serializer for field '{0}' must implement IBsonArraySerializer.", renderedFieldName.FieldName);
                throw new InvalidOperationException(message);
            }
            var itemSerializer = arraySerializer.GetItemSerializationInfo().Serializer;

            var document = new BsonDocument();
            using (var bsonWriter = new BsonDocumentWriter(document))
            {
                var context = BsonSerializationContext.CreateRoot(bsonWriter);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName("$addToSet");
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName(renderedFieldName.FieldName);
                if (_values.Count == 1)
                {
                    itemSerializer.Serialize(context, _values[0]);
                }
                else
                {
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteName("$each");
                    bsonWriter.WriteStartArray();
                    foreach (var value in _values)
                    {
                        itemSerializer.Serialize(context, value);
                    }
                    bsonWriter.WriteEndArray();
                    bsonWriter.WriteEndDocument();
                }
                bsonWriter.WriteEndDocument();
                bsonWriter.WriteEndDocument();
            }

            return document;
        }
    }

    internal sealed class CombinedUpdateDefinition<TDocument> : UpdateDefinition<TDocument>
    {
        private readonly List<UpdateDefinition<TDocument>> _updates;

        public CombinedUpdateDefinition(IEnumerable<UpdateDefinition<TDocument>> updates)
        {
            _updates = Ensure.IsNotNull(updates, "updates").ToList();
        }

        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var document = new BsonDocument();

            foreach (var update in _updates)
            {
                var renderedUpdate = update.Render(documentSerializer, serializerRegistry);

                foreach (var element in renderedUpdate.Elements)
                {
                    BsonValue currentOperatorValue;
                    if (document.TryGetValue(element.Name, out currentOperatorValue))
                    {
                        // last one wins
                        document[element.Name] = ((BsonDocument)currentOperatorValue)
                            .Merge((BsonDocument)element.Value, overwriteExistingElements: true);
                    }
                    else
                    {
                        document.Add(element);
                    }
                }
            }
            return document;
        }
    }

    internal sealed class BitwiseOperatorUpdateDefinition<TDocument, TField> : UpdateDefinition<TDocument>
    {
        private readonly string _operatorName;
        private readonly FieldName<TDocument, TField> _fieldName;
        private readonly TField _value;

        public BitwiseOperatorUpdateDefinition(string operatorName, FieldName<TDocument, TField> fieldName, TField value)
        {
            _operatorName = Ensure.IsNotNull(operatorName, "operatorName");
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _value = value;
        }

        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var renderedFieldName = _fieldName.Render(documentSerializer, serializerRegistry);

            var document = new BsonDocument();
            using (var bsonWriter = new BsonDocumentWriter(document))
            {
                var context = BsonSerializationContext.CreateRoot(bsonWriter);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName("$bit");
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName(renderedFieldName.FieldName);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName(_operatorName);
                renderedFieldName.FieldSerializer.Serialize(context, _value);
                bsonWriter.WriteEndDocument();
                bsonWriter.WriteEndDocument();
                bsonWriter.WriteEndDocument();
            }

            return document;
        }
    }

    internal sealed class OperatorUpdateDefinition<TDocument> : UpdateDefinition<TDocument>
    {
        private readonly string _operatorName;
        private readonly FieldName<TDocument> _fieldName;
        private readonly BsonValue _value;

        public OperatorUpdateDefinition(string operatorName, FieldName<TDocument> fieldName, BsonValue value)
        {
            _operatorName = Ensure.IsNotNull(operatorName, "operatorName");
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _value = value;
        }

        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var renderedFieldName = _fieldName.Render(documentSerializer, serializerRegistry);
            return new BsonDocument(_operatorName, new BsonDocument(renderedFieldName, _value));
        }
    }

    internal sealed class OperatorUpdateDefinition<TDocument, TField> : UpdateDefinition<TDocument>
    {
        private readonly string _operatorName;
        private readonly FieldName<TDocument, TField> _fieldName;
        private readonly TField _value;

        public OperatorUpdateDefinition(string operatorName, FieldName<TDocument, TField> fieldName, TField value)
        {
            _operatorName = Ensure.IsNotNull(operatorName, "operatorName");
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _value = value;
        }

        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var renderedFieldName = _fieldName.Render(documentSerializer, serializerRegistry);

            var document = new BsonDocument();
            using (var bsonWriter = new BsonDocumentWriter(document))
            {
                var context = BsonSerializationContext.CreateRoot(bsonWriter);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName(_operatorName);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName(renderedFieldName.FieldName);
                renderedFieldName.FieldSerializer.Serialize(context, _value);
                bsonWriter.WriteEndDocument();
                bsonWriter.WriteEndDocument();
            }

            return document;
        }
    }

    internal sealed class PullUpdateDefinition<TDocument, TField, TItem> : UpdateDefinition<TDocument>
    {
        private readonly FieldName<TDocument, TField> _fieldName;
        private readonly FilterDefinition<TItem> _filter;
        private readonly List<TItem> _values;

        public PullUpdateDefinition(FieldName<TDocument, TField> fieldName, FilterDefinition<TItem> filter)
        {
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _filter = Ensure.IsNotNull(filter, "filter");
        }

        public PullUpdateDefinition(FieldName<TDocument, TField> fieldName, IEnumerable<TItem> values)
        {
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _values = Ensure.IsNotNull(values, "values").ToList();
        }

        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var renderedFieldName = _fieldName.Render(documentSerializer, serializerRegistry);

            var arraySerializer = renderedFieldName.FieldSerializer as IBsonArraySerializer;
            if (arraySerializer == null)
            {
                var message = string.Format("The serializer for field '{0}' must implement IBsonArraySerializer.", renderedFieldName.FieldName);
                throw new InvalidOperationException(message);
            }
            var itemSerializer = arraySerializer.GetItemSerializationInfo().Serializer;

            if (_filter != null)
            {
                var renderedFilter = _filter.Render((IBsonSerializer<TItem>)itemSerializer, serializerRegistry);
                return new BsonDocument("$pull", new BsonDocument(renderedFieldName.FieldName, renderedFilter));
            }
            else
            {
                var document = new BsonDocument();
                using (var bsonWriter = new BsonDocumentWriter(document))
                {
                    var context = BsonSerializationContext.CreateRoot(bsonWriter);
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteName(_values.Count == 1 ? "$pull" : "$pullAll");
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteName(renderedFieldName.FieldName);
                    if (_values.Count == 1)
                    {
                        itemSerializer.Serialize(context, _values[0]);
                    }
                    else
                    {
                        bsonWriter.WriteStartArray();
                        foreach (var value in _values)
                        {
                            itemSerializer.Serialize(context, value);
                        }
                        bsonWriter.WriteEndArray();
                    }
                    bsonWriter.WriteEndDocument();
                    bsonWriter.WriteEndDocument();
                }
                return document;
            }
        }
    }

    internal sealed class PushUpdateDefinition<TDocument, TField, TItem> : UpdateDefinition<TDocument>
    {
        private readonly FieldName<TDocument, TField> _fieldName;
        private readonly int? _position;
        private readonly int? _slice;
        private SortDefinition<TItem> _sort;
        private readonly List<TItem> _values;

        public PushUpdateDefinition(FieldName<TDocument, TField> fieldName, IEnumerable<TItem> values, int? slice = null, int? position = null, SortDefinition<TItem> sort = null)
        {
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _values = Ensure.IsNotNull(values, "values").ToList();
            _slice = slice;
            _position = position;
            _sort = sort;
        }

        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var renderedFieldName = _fieldName.Render(documentSerializer, serializerRegistry);

            var arraySerializer = renderedFieldName.FieldSerializer as IBsonArraySerializer;
            if (arraySerializer == null)
            {
                var message = string.Format("The serializer for field '{0}' must implement IBsonArraySerializer.", renderedFieldName.FieldName);
                throw new InvalidOperationException(message);
            }
            var itemSerializer = arraySerializer.GetItemSerializationInfo().Serializer;

            var document = new BsonDocument();
            using (var bsonWriter = new BsonDocumentWriter(document))
            {
                var context = BsonSerializationContext.CreateRoot(bsonWriter);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName("$push");
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName(renderedFieldName.FieldName);
                if (!_slice.HasValue && !_position.HasValue && _sort == null && _values.Count == 1)
                {
                    itemSerializer.Serialize(context, _values[0]);
                }
                else
                {
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteName("$each");
                    bsonWriter.WriteStartArray();
                    foreach (var value in _values)
                    {
                        itemSerializer.Serialize(context, value);
                    }
                    bsonWriter.WriteEndArray();
                    if (_slice.HasValue)
                    {
                        bsonWriter.WriteName("$slice");
                        bsonWriter.WriteInt32(_slice.Value);
                    }
                    if (_position.HasValue)
                    {
                        bsonWriter.WriteName("$position");
                        bsonWriter.WriteInt32(_position.Value);
                    }
                    bsonWriter.WriteEndDocument();
                }
                bsonWriter.WriteEndDocument();
                bsonWriter.WriteEndDocument();
            }

            if (_sort != null)
            {
                document["$push"][renderedFieldName.FieldName]["$sort"] = _sort.Render((IBsonSerializer<TItem>)itemSerializer, serializerRegistry);
            }

            return document;
        }
    }

}
