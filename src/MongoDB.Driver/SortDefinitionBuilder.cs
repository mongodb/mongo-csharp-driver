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
    /// Extension methods for Sort.
    /// </summary>
    public static class SortExtensions
    {
        private static class BuilderCache<TDocument>
        {
            public static SortBuilder<TDocument> Instance = new SortBuilder<TDocument>();
        }

        /// <summary>
        /// Combines an existing sort with an ascending field.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="sort">The sort.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// A combined sort.
        /// </returns>
        public static SortDefinition<TDocument> Ascending<TDocument>(this SortDefinition<TDocument> sort, FieldName<TDocument> fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(sort, builder.Ascending(fieldName));
        }

        /// <summary>
        /// Combines an existing sort with an ascending field.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="sort">The sort.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// A combined sort.
        /// </returns>
        public static SortDefinition<TDocument> Ascending<TDocument>(this SortDefinition<TDocument> sort, Expression<Func<TDocument, object>> fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(sort, builder.Ascending(fieldName));
        }

        /// <summary>
        /// Combines an existing sort with an descending field.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="sort">The sort.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// A combined sort.
        /// </returns>
        public static SortDefinition<TDocument> Descending<TDocument>(this SortDefinition<TDocument> sort, FieldName<TDocument> fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(sort, builder.Descending(fieldName));
        }

        /// <summary>
        /// Combines an existing sort with an descending field.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="sort">The sort.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// A combined sort.
        /// </returns>
        public static SortDefinition<TDocument> Descending<TDocument>(this SortDefinition<TDocument> sort, Expression<Func<TDocument, object>> fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(sort, builder.Descending(fieldName));
        }

        /// <summary>
        /// Combines an existing sort with a descending sort on the computed relevance score of a text search.
        /// The field name should be the name of the projected relevance score field.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="sort">The sort.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// A combined sort.
        /// </returns>
        public static SortDefinition<TDocument> MetaTextScore<TDocument>(this SortDefinition<TDocument> sort, string fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(sort, builder.MetaTextScore(fieldName));
        }
    }

    /// <summary>
    /// A builder for a <see cref="SortDefinition{TDocument}"/>.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class SortBuilder<TDocument>
    {
        /// <summary>
        /// Creates an ascending sort.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>An ascending sort.</returns>
        public SortDefinition<TDocument> Ascending(FieldName<TDocument> fieldName)
        {
            return new DirectionalSortDefinition<TDocument>(fieldName, SortDirection.Ascending);
        }

        /// <summary>
        /// Creates an ascending sort.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>An ascending sort.</returns>
        public SortDefinition<TDocument> Ascending(Expression<Func<TDocument, object>> fieldName)
        {
            return Ascending(new ExpressionFieldName<TDocument>(fieldName));
        }

        /// <summary>
        /// Creates a combined sort.
        /// </summary>
        /// <param name="sorts">The sorts.</param>
        /// <returns>A combined sort.</returns>
        public SortDefinition<TDocument> Combine(params SortDefinition<TDocument>[] sorts)
        {
            return Combine((IEnumerable<SortDefinition<TDocument>>)sorts);
        }

        /// <summary>
        /// Creates a combined sort.
        /// </summary>
        /// <param name="sorts">The sorts.</param>
        /// <returns>A combined sort.</returns>
        public SortDefinition<TDocument> Combine(IEnumerable<SortDefinition<TDocument>> sorts)
        {
            return new CombinedSortDefinition<TDocument>(sorts);
        }

        /// <summary>
        /// Creates a descending sort.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>A descending sort.</returns>
        public SortDefinition<TDocument> Descending(FieldName<TDocument> fieldName)
        {
            return new DirectionalSortDefinition<TDocument>(fieldName, SortDirection.Descending);
        }

        /// <summary>
        /// Creates a descending sort.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>A descending sort.</returns>
        public SortDefinition<TDocument> Descending(Expression<Func<TDocument, object>> fieldName)
        {
            return Descending(new ExpressionFieldName<TDocument>(fieldName));
        }

        /// <summary>
        /// Creates a descending sort on the computed relevance score of a text search.
        /// The name of the key should be the name of the projected relevence score field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>A meta text score sort.</returns>
        public SortDefinition<TDocument> MetaTextScore(string fieldName)
        {
            return new BsonDocumentSortDefinition<TDocument>(new BsonDocument(fieldName, new BsonDocument("$meta", "textScore")));
        }
    }

    internal sealed class CombinedSortDefinition<TDocument> : SortDefinition<TDocument>
    {
        private readonly List<SortDefinition<TDocument>> _sorts;

        public CombinedSortDefinition(IEnumerable<SortDefinition<TDocument>> sorts)
        {
            _sorts = Ensure.IsNotNull(sorts, "sorts").ToList();
        }

        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var document = new BsonDocument();

            foreach (var sort in _sorts)
            {
                var renderedSort = sort.Render(documentSerializer, serializerRegistry);

                foreach (var element in renderedSort.Elements)
                {
                    // the last sort always wins, and we need to make sure that order is preserved.
                    document.Remove(element.Name);
                    document.Add(element);
                }
            }

            return document;
        }
    }

    internal sealed class DirectionalSortDefinition<TDocument> : SortDefinition<TDocument>
    {
        private readonly FieldName<TDocument> _fieldName;
        private readonly SortDirection _direction;

        public DirectionalSortDefinition(FieldName<TDocument> fieldName, SortDirection direction)
        {
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _direction = direction;
        }

        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var renderedFieldName = _fieldName.Render(documentSerializer, serializerRegistry);

            BsonValue value;
            switch(_direction)
            {
                case SortDirection.Ascending:
                    value = 1;
                    break;
                case SortDirection.Descending:
                    value = -1;
                    break;
                default:
                    throw new InvalidOperationException("Unknown value for " + typeof(SortDirection) + ".");
            }

            return new BsonDocument(renderedFieldName, value);
        }
    }
}
