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
    /// Extension methods for projections.
    /// </summary>
    public static class ProjectionExtensions
    {
        private static class BuilderCache<TDocument>
        {
            public static ProjectionBuilder<TDocument> Instance = new ProjectionBuilder<TDocument>();
        }

        /// <summary>
        /// Creates a projection that filters the contents of an array.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>
        /// An array filtering projection.
        /// </returns>
        public static Projection<TDocument> ElemMatch<TDocument, TField, TItem>(this Projection<TDocument> source, FieldName<TDocument, TField> fieldName, Filter<TItem> filter)
            where TField : IEnumerable<TItem>
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(source, builder.ElemMatch(fieldName, filter));
        }

        /// <summary>
        /// Creates a projection that filters the contents of an array.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>
        /// An array filtering projection.
        /// </returns>
        public static Projection<TDocument> ElemMatch<TDocument, TItem>(this Projection<TDocument> source, string fieldName, Filter<TItem> filter)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(source, builder.ElemMatch(fieldName, filter));
        }

        /// <summary>
        /// Creates a projection that filters the contents of an array.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>
        /// An array filtering projection.
        /// </returns>
        public static Projection<TDocument> ElemMatch<TDocument, TField, TItem>(this Projection<TDocument> source, Expression<Func<TDocument, TField>> fieldName, Filter<TItem> filter)
            where TField : IEnumerable<TItem>
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(source, builder.ElemMatch(fieldName, filter));
        }

        /// <summary>
        /// Creates a projection that filters the contents of an array.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>
        /// An array filtering projection.
        /// </returns>
        public static Projection<TDocument> ElemMatch<TDocument, TField, TItem>(this Projection<TDocument> source, Expression<Func<TDocument, TField>> fieldName, Expression<Func<TItem, bool>> filter)
            where TField : IEnumerable<TItem>
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(source, builder.ElemMatch(fieldName, filter));
        }

        /// <summary>
        /// Creates a projection that excludes a field.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// An exclusion projection.
        /// </returns>
        public static Projection<TDocument> Exclude<TDocument>(this Projection<TDocument> source, FieldName<TDocument> fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(source, builder.Exclude(fieldName));
        }

        /// <summary>
        /// Creates a projection that excludes a field.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// An exclusion projection.
        /// </returns>
        public static Projection<TDocument> Exclude<TDocument>(this Projection<TDocument> source, Expression<Func<TDocument, object>> fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(source, builder.Exclude(fieldName));
        }

        /// <summary>
        /// Creates a projection that includes a field.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// An inclusion projection.
        /// </returns>
        public static Projection<TDocument> Include<TDocument>(this Projection<TDocument> source, FieldName<TDocument> fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(source, builder.Include(fieldName));
        }

        /// <summary>
        /// Creates a projection that includes a field.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// An inclusion projection.
        /// </returns>
        public static Projection<TDocument> Include<TDocument>(this Projection<TDocument> source, Expression<Func<TDocument, object>> fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(source, builder.Include(fieldName));
        }

        /// <summary>
        /// Creates a text score projection.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// A text score projection.
        /// </returns>
        public static Projection<TDocument> MetaTextScore<TDocument>(this Projection<TDocument> source, string fieldName)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(source, builder.MetaTextScore(fieldName));
        }

        /// <summary>
        /// Creates an array slice projection.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="skip">The skip.</param>
        /// <param name="limit">The limit.</param>
        /// <returns>
        /// An array slice projection.
        /// </returns>
        public static Projection<TDocument> Slice<TDocument>(this Projection<TDocument> source, FieldName<TDocument> fieldName, int skip, int? limit = null)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(source, builder.Slice(fieldName, skip, limit));
        }

        /// <summary>
        /// Creates an array slice projection.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="skip">The skip.</param>
        /// <param name="limit">The limit.</param>
        /// <returns>
        /// An array slice projection.
        /// </returns>
        public static Projection<TDocument> Slice<TDocument>(this Projection<TDocument> source, Expression<Func<TDocument, object>> fieldName, int skip, int? limit = null)
        {
            var builder = BuilderCache<TDocument>.Instance;
            return builder.Combine(source, builder.Slice(fieldName, skip, limit));
        }
    }

    /// <summary>
    /// A builder for a projection.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class ProjectionBuilder<TDocument>
    {
        /// <summary>
        /// Combines the specified projections.
        /// </summary>
        /// <param name="projections">The projections.</param>
        /// <returns>
        /// A combined projection.
        /// </returns>
        public Projection<TDocument> Combine(params Projection<TDocument>[] projections)
        {
            return Combine((IEnumerable<Projection<TDocument>>)projections);
        }

        /// <summary>
        /// Combines the specified projections.
        /// </summary>
        /// <param name="projections">The projections.</param>
        /// <returns>
        /// A combined projection.
        /// </returns>
        public Projection<TDocument> Combine(IEnumerable<Projection<TDocument>> projections)
        {
            return new CombinedProjection<TDocument>(projections);
        }

        /// <summary>
        /// Creates a projection that filters the contents of an array.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>
        /// An array filtering projection.
        /// </returns>
        public Projection<TDocument> ElemMatch<TField, TItem>(FieldName<TDocument, TField> fieldName, Filter<TItem> filter)
            where TField : IEnumerable<TItem>
        {
            return new ElementMatchProjection<TDocument, TField, TItem>(fieldName, filter);
        }

        /// <summary>
        /// Creates a projection that filters the contents of an array.
        /// </summary>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>
        /// An array filtering projection.
        /// </returns>
        public Projection<TDocument> ElemMatch<TItem>(string fieldName, Filter<TItem> filter)
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
        /// <returns>
        /// An array filtering projection.
        /// </returns>
        public Projection<TDocument> ElemMatch<TField, TItem>(Expression<Func<TDocument, TField>> fieldName, Filter<TItem> filter)
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
        /// <returns>
        /// An array filtering projection.
        /// </returns>
        public Projection<TDocument> ElemMatch<TField, TItem>(Expression<Func<TDocument, TField>> fieldName, Expression<Func<TItem, bool>> filter)
            where TField : IEnumerable<TItem>
        {
            return ElemMatch(new ExpressionFieldName<TDocument, TField>(fieldName), new ExpressionFilter<TItem>(filter));
        }

        /// <summary>
        /// Creates a projection that excludes a field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// An exclusion projection.
        /// </returns>
        public Projection<TDocument> Exclude(FieldName<TDocument> fieldName)
        {
            return new SingleFieldProjection<TDocument>(fieldName, 0);
        }

        /// <summary>
        /// Creates a projection that excludes a field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// An exclusion projection.
        /// </returns>
        public Projection<TDocument> Exclude(Expression<Func<TDocument, object>> fieldName)
        {
            return Exclude(new ExpressionFieldName<TDocument>(fieldName));
        }

        /// <summary>
        /// Creates a projection based on the expression.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="expression">The expression.</param>
        /// <returns>
        /// An expression projection.
        /// </returns>
        public Projection<TDocument, TResult> Expression<TResult>(Expression<Func<TDocument, TResult>> expression)
        {
            return new FindExpressionProjection<TDocument, TResult>(expression);
        }

        /// <summary>
        /// Creates a projection that includes a field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// An inclusion projection.
        /// </returns>
        public Projection<TDocument> Include(FieldName<TDocument> fieldName)
        {
            return new SingleFieldProjection<TDocument>(fieldName, 1);
        }

        /// <summary>
        /// Creates a projection that includes a field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// An inclusion projection.
        /// </returns>
        public Projection<TDocument> Include(Expression<Func<TDocument, object>> fieldName)
        {
            return Include(new ExpressionFieldName<TDocument>(fieldName));
        }

        /// <summary>
        /// Creates a text score projection.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// A text score projection.
        /// </returns>
        public Projection<TDocument> MetaTextScore(string fieldName)
        {
            return new SingleFieldProjection<TDocument>(fieldName, new BsonDocument("$meta", "textScore"));
        }

        /// <summary>
        /// Creates an array slice projection.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="skip">The skip.</param>
        /// <param name="limit">The limit.</param>
        /// <returns>
        /// An array slice projection.
        /// </returns>
        public Projection<TDocument> Slice(FieldName<TDocument> fieldName, int skip, int? limit = null)
        {
            var value = limit.HasValue ? (BsonValue)new BsonArray { skip, limit.Value } : skip;
            return new SingleFieldProjection<TDocument>(fieldName, new BsonDocument("$slice", value));
        }

        /// <summary>
        /// Creates an array slice projection.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="skip">The skip.</param>
        /// <param name="limit">The limit.</param>
        /// <returns>
        /// An array slice projection.
        /// </returns>
        public Projection<TDocument> Slice(Expression<Func<TDocument, object>> fieldName, int skip, int? limit = null)
        {
            return Slice(new ExpressionFieldName<TDocument>(fieldName), skip, limit);
        }
    }

    internal sealed class CombinedProjection<TDocument> : Projection<TDocument>
    {
        private readonly List<Projection<TDocument>> _projections;

        public CombinedProjection(IEnumerable<Projection<TDocument>> projections)
        {
            _projections = Ensure.IsNotNull(projections, "projections").ToList();
        }

        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var document = new BsonDocument();
            foreach(var projection in _projections)
            {
                var renderedProjection = projection.Render(documentSerializer, serializerRegistry);

                foreach(var element in renderedProjection.Elements)
                {
                    // last one wins
                    document.Remove(element.Name);
                    document.Add(element);
                }
            }

            return document;
        }
    }

    internal sealed class ElementMatchProjection<TDocument, TField, TItem> : Projection<TDocument>
    {
        private readonly FieldName<TDocument, TField> _fieldName;
        private readonly Filter<TItem> _filter;

        public ElementMatchProjection(FieldName<TDocument, TField> fieldName, Filter<TItem> filter)
        {
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _filter = filter;
        }

        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
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

            return new BsonDocument(renderedField.FieldName, new BsonDocument("$elemMatch", renderedFilter));
        }
    }

    internal sealed class SingleFieldProjection<TDocument> : Projection<TDocument>
    {
        private readonly FieldName<TDocument> _fieldName;
        private readonly BsonValue _value;

        public SingleFieldProjection(FieldName<TDocument> fieldName, BsonValue value)
        {
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _value = Ensure.IsNotNull(value, "value");
        }

        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var renderedFieldName = _fieldName.Render(documentSerializer, serializerRegistry);
            return new BsonDocument(renderedFieldName, _value);
        }
    }
}
