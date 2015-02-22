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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// A builder for a projection.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class ProjectionBuilder<TDocument>
    {
        /// <summary>
        /// Combines the specified projections.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="projections">The projections.</param>
        /// <returns></returns>
        public Projection<TDocument, TResult> Combine<TResult>(params Projection<TDocument, BsonDocument>[] projections)
        {
            return Combine<TResult>((IEnumerable<Projection<TDocument, BsonDocument>>)projections);
        }

        /// <summary>
        /// Combines the specified projections.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="projections">The projections.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        /// <returns></returns>
        public Projection<TDocument, TResult> Combine<TResult>(IEnumerable<Projection<TDocument, BsonDocument>> projections, IBsonSerializer<TResult> resultSerializer = null)
        {
            return new CombinedProjection<TDocument, TResult>(projections, null);
        }

        // TODO: Element Match

        /// <summary>
        /// Creates a projection that excludes a field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>An exclusion projection.</returns>
        public Projection<TDocument, BsonDocument> Exclude(FieldName<TDocument> fieldName)
        {
            return new SingleFieldProjection<TDocument, BsonDocument>(fieldName, false);
        }

        /// <summary>
        /// Creates a projection that excludes a field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>An exclusion projection.</returns>
        public Projection<TDocument, BsonDocument> Exclude(Expression<Func<TDocument, object>> fieldName)
        {
            return Exclude(new ExpressionFieldName<TDocument>(fieldName));
        }

        /// <summary>
        /// Creates a projection that includes a field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>An inclusion projection.</returns>
        public Projection<TDocument, BsonDocument> Include(FieldName<TDocument> fieldName)
        {
            return new SingleFieldProjection<TDocument, BsonDocument>(fieldName, true);
        }

        /// <summary>
        /// Creates a projection that includes a field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>An inclusion projection.</returns>
        public Projection<TDocument, BsonDocument> Include(Expression<Func<TDocument, object>> fieldName)
        {
            return Include(new ExpressionFieldName<TDocument>(fieldName));
        }

        // TODO: MetaTextScore

        // TODO: Slice
    }

    /// <summary>
    /// A combined projection.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public sealed class CombinedProjection<TDocument, TResult> : Projection<TDocument, TResult>
    {
        private readonly List<Projection<TDocument, BsonDocument>> _projections;
        private readonly IBsonSerializer<TResult> _resultSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="CombinedProjection{TDocument, TResult}"/> class.
        /// </summary>
        /// <param name="projections">The projections.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        public CombinedProjection(IEnumerable<Projection<TDocument, BsonDocument>> projections, IBsonSerializer<TResult> resultSerializer = null)
        {
            _projections = Ensure.IsNotNull(projections, "projections").ToList();
            _resultSerializer = resultSerializer;
        }

        /// <inheritdoc />
        public override RenderedProjection<TResult> Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var document = new BsonDocument();
            foreach(var projection in _projections)
            {
                var renderedProjection = projection.Render(documentSerializer, serializerRegistry);

                foreach(var element in renderedProjection.Document.Elements)
                {
                    // last one wins
                    document.Remove(element.Name);
                    document.Add(element);
                }
            }

            return new RenderedProjection<TResult>(
                document,
                _resultSerializer ?? serializerRegistry.GetSerializer<TResult>());
        }
    }

    /// <summary>
    /// A single field projection.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public sealed class SingleFieldProjection<TDocument, TResult> : Projection<TDocument, TResult>
    {
        private readonly FieldName<TDocument> _fieldName;
        private readonly bool _include;

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleFieldProjection{TDocument, TResult}" /> class.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="include">if set to <c>true</c> [include].</param>
        public SingleFieldProjection(FieldName<TDocument> fieldName, bool include)
        {
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _include = include;
        }

        /// <inheritdoc />
        public override RenderedProjection<TResult> Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var renderedFieldName = _fieldName.Render(documentSerializer, serializerRegistry);
            return new RenderedProjection<TResult>(
                new BsonDocument(renderedFieldName, _include ? 1 : 0),
                serializerRegistry.GetSerializer<TResult>());
        }
    }
}
