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
        private static readonly ProjectionBuilder<TDocument> __instance = new ProjectionBuilder<TDocument>();

        /// <summary>
        /// Combines the specified projections.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="projections">The projections.</param>
        /// <returns>A combined projection.</returns>
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
        /// <returns>A combined projection.</returns>
        public Projection<TDocument, TResult> Combine<TResult>(IEnumerable<Projection<TDocument, BsonDocument>> projections, IBsonSerializer<TResult> resultSerializer = null)
        {
            return new CombinedProjection<TDocument, TResult>(projections, null);
        }

        /// <summary>
        /// Creates a projection that filters the contents of an array.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>An array filtering projection.</returns>
        public Projection<TDocument, BsonDocument> ElemMatch<TField, TItem>(FieldName<TDocument, TField> fieldName, Filter<TItem> filter)
            where TField : IEnumerable<TItem>
        {
            return new ElementMatchProjection<TDocument, BsonDocument, TField, TItem>(fieldName, filter);
        }

        /// <summary>
        /// Creates a projection that filters the contents of an array.
        /// </summary>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>An array filtering projection.</returns>
        public Projection<TDocument, BsonDocument> ElemMatch<TItem>(string fieldName, Filter<TItem> filter)
        {
            return ElemMatch(
                new StringFieldName<TDocument, IEnumerable<TItem>>(fieldName),
                filter);
        }

        /// <summary>
        /// Creates a projection that filters the contents of an array.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>An array filtering projection.</returns>
        public Projection<TDocument, BsonDocument> ElemMatch<TField, TItem>(Expression<Func<TDocument, TField>> fieldName, Filter<TItem> filter)
            where TField : IEnumerable<TItem>
        {
            return ElemMatch(new ExpressionFieldName<TDocument, TField>(fieldName), filter);
        }

        /// <summary>
        /// Creates a projection that filters the contents of an array.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>An array filtering projection.</returns>
        public Projection<TDocument, BsonDocument> ElemMatch<TField, TItem>(Expression<Func<TDocument, TField>> fieldName, Expression<Func<TItem, bool>> filter)
            where TField : IEnumerable<TItem>
        {
            return ElemMatch(new ExpressionFieldName<TDocument, TField>(fieldName), new ExpressionFilter<TItem>(filter));
        }

        /// <summary>
        /// Creates a projection that excludes a field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>An exclusion projection.</returns>
        public Projection<TDocument, BsonDocument> Exclude(FieldName<TDocument> fieldName)
        {
            return new SingleFieldProjection<TDocument, BsonDocument>(fieldName, 0);
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
        /// Creates a projection based on the expression.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="expression">The expression.</param>
        /// <returns>An expression projection.</returns>
        public Projection<TDocument, TResult> Expression<TResult>(Expression<Func<TDocument, TResult>> expression)
        {
            return new FindExpressionProjection<TDocument, TResult>(expression);
        }

        /// <summary>
        /// Creates a projection that includes a field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>An inclusion projection.</returns>
        public Projection<TDocument, BsonDocument> Include(FieldName<TDocument> fieldName)
        {
            return new SingleFieldProjection<TDocument, BsonDocument>(fieldName, 1);
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

        /// <summary>
        /// Creates a text score projection.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>A text score projection.</returns>
        public Projection<TDocument, BsonDocument> MetaTextScore(string fieldName)
        {
            return new SingleFieldProjection<TDocument, BsonDocument>(fieldName, new BsonDocument("$meta", "textScore"));
        }

        /// <summary>
        /// Creates an array slice projection.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="skip">The skip.</param>
        /// <param name="limit">The limit.</param>
        /// <returns>An array slice projection.</returns>
        public Projection<TDocument, BsonDocument> Slice(FieldName<TDocument> fieldName, int skip, int? limit = null)
        {
            var value = limit.HasValue ? (BsonValue)new BsonArray { skip, limit.Value } : skip;
            return new SingleFieldProjection<TDocument, BsonDocument>(fieldName, new BsonDocument("$slice", value));
        }

        /// <summary>
        /// Creates an array slice projection.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="skip">The skip.</param>
        /// <param name="limit">The limit.</param>
        /// <returns>An array slice projection.</returns>
        public Projection<TDocument, BsonDocument> Slice(Expression<Func<TDocument, object>> fieldName, int skip, int? limit = null)
        {
            return Slice(new ExpressionFieldName<TDocument>(fieldName), skip, limit);
        }
    }

    internal sealed class CombinedProjection<TDocument, TResult> : Projection<TDocument, TResult>
    {
        private readonly List<Projection<TDocument, BsonDocument>> _projections;
        private readonly IBsonSerializer<TResult> _resultSerializer;

        public CombinedProjection(IEnumerable<Projection<TDocument, BsonDocument>> projections, IBsonSerializer<TResult> resultSerializer = null)
        {
            _projections = Ensure.IsNotNull(projections, "projections").ToList();
            _resultSerializer = resultSerializer;
        }

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
                _resultSerializer ?? (documentSerializer as IBsonSerializer<TResult>) ?? serializerRegistry.GetSerializer<TResult>());
        }
    }

    internal sealed class ElementMatchProjection<TDocument, TResult, TField, TItem> : Projection<TDocument, TResult>
    {
        private readonly FieldName<TDocument, TField> _fieldName;
        private readonly Filter<TItem> _filter;

        public ElementMatchProjection(FieldName<TDocument, TField> fieldName, Filter<TItem> filter)
        {
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _filter = filter;
        }

        public override RenderedProjection<TResult> Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var renderedField = _fieldName.Render(documentSerializer, serializerRegistry);
            var arraySerializer = renderedField.Serializer as IBsonArraySerializer;
            if (arraySerializer == null)
            {
                var message = string.Format("The serializer for field '{0}' must implement IBsonArraySerializer.", renderedField.FieldName);
                throw new InvalidOperationException(message);
            }
            var itemSerializer = (IBsonSerializer<TItem>)arraySerializer.GetItemSerializationInfo().Serializer;
            var renderedFilter = _filter.Render(itemSerializer, serializerRegistry);

            return new RenderedProjection<TResult>(
                new BsonDocument(renderedField.FieldName, new BsonDocument("$elemMatch", renderedFilter)),
                (documentSerializer as IBsonSerializer<TResult>) ?? serializerRegistry.GetSerializer<TResult>());
        }
    }

    internal sealed class SingleFieldProjection<TDocument, TResult> : Projection<TDocument, TResult>
    {
        private readonly FieldName<TDocument> _fieldName;
        private readonly BsonValue _value;

        public SingleFieldProjection(FieldName<TDocument> fieldName, BsonValue value)
        {
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _value = Ensure.IsNotNull(value, "value");
        }

        public override RenderedProjection<TResult> Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var renderedFieldName = _fieldName.Render(documentSerializer, serializerRegistry);
            return new RenderedProjection<TResult>(
                new BsonDocument(renderedFieldName, _value),
                (documentSerializer as IBsonSerializer<TResult>) ?? serializerRegistry.GetSerializer<TResult>());
        }
    }
}
