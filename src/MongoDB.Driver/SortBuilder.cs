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
    /// A builder for a <see cref="Sort{TDocument}"/>.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class SortBuilder<TDocument>
    {
        /// <summary>
        /// Creates an ascending sort.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>An ascending sort.</returns>
        public Sort<TDocument> Ascending(FieldName<TDocument> fieldName)
        {
            return new DirectionalSort<TDocument>(fieldName, SortDirection.Ascending);
        }

        /// <summary>
        /// Creates an ascending sort.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>An ascending sort.</returns>
        public Sort<TDocument> Ascending(Expression<Func<TDocument, object>> fieldName)
        {
            return Ascending(new ExpressionFieldName<TDocument>(fieldName));
        }

        /// <summary>
        /// Creates a combined sort.
        /// </summary>
        /// <param name="sorts">The sorts.</param>
        /// <returns>A combined sort.</returns>
        public Sort<TDocument> Combine(params Sort<TDocument>[] sorts)
        {
            return Combine((IEnumerable<Sort<TDocument>>)sorts);
        }

        /// <summary>
        /// Creates a combined sort.
        /// </summary>
        /// <param name="sorts">The sorts.</param>
        /// <returns>A combined sort.</returns>
        public Sort<TDocument> Combine(IEnumerable<Sort<TDocument>> sorts)
        {
            return new CombineSort<TDocument>(sorts);
        }

        /// <summary>
        /// Creates a descending sort.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>A descending sort.</returns>
        public Sort<TDocument> Descending(FieldName<TDocument> fieldName)
        {
            return new DirectionalSort<TDocument>(fieldName, SortDirection.Descending);
        }

        /// <summary>
        /// Creates a descending sort.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>A descending sort.</returns>
        public Sort<TDocument> Descending(Expression<Func<TDocument, object>> fieldName)
        {
            return Descending(new ExpressionFieldName<TDocument>(fieldName));
        }

        /// <summary>
        /// Creates a sort by the computed relevance score when using text search. The name
        /// of the key should be the name of the projected relevence score field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>A meta text score sort.</returns>
        public Sort<TDocument> MetaTextScore(string fieldName)
        {
            return new BsonDocumentSort<TDocument>(new BsonDocument(fieldName, new BsonDocument("$meta", "textScore")));
        }
    }

    /// <summary>
    /// A combining <see cref="Sort{TDocument}"/>.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class CombineSort<TDocument> : Sort<TDocument>
    {
        private readonly List<Sort<TDocument>> _sorts;

        /// <summary>
        /// Initializes a new instance of the <see cref="CombineSort{TDocument}"/> class.
        /// </summary>
        /// <param name="sorts">The sorts.</param>
        public CombineSort(IEnumerable<Sort<TDocument>> sorts)
        {
            _sorts = Ensure.IsNotNull(sorts, "sorts").ToList();
        }

        /// <inheritdoc />
        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var document = new BsonDocument();
            foreach(var sort in _sorts)
            {
                var renderedSort = sort.Render(documentSerializer, serializerRegistry);

                foreach(var element in renderedSort.Elements)
                {
                    // the last sort always wins, and we need to make sure that order is preserved.
                    document.Remove(element.Name);
                    document.Add(element);
                }
            }

            return document;
        }
    }

    /// <summary>
    /// A directional <see cref="Sort{TDocument}"/>.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class DirectionalSort<TDocument> : Sort<TDocument>
    {
        private readonly FieldName<TDocument> _fieldName;
        private readonly SortDirection _direction;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectionalSort{TDocument}"/> class.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="direction">The direction.</param>
        public DirectionalSort(FieldName<TDocument> fieldName, SortDirection direction)
        {
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _direction = direction;
        }

        /// <inheritdoc />
        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var renderedField = _fieldName.Render(documentSerializer, serializerRegistry);
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

            return new BsonDocument(renderedField, value);
        }
    }

}
