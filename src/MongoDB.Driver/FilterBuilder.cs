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
    /// A builder for a <see cref="Filter{TDocument}"/>.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class FilterBuilder<TDocument>
    {
        /// <summary>
        /// Creates an all filter.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <returns>An all filter.</returns>
        public Filter<TDocument> All<TField, TItem>(FieldName<TDocument, TField> fieldName, IEnumerable<TItem> values)
            where TField : IEnumerable<TItem>
        {
            return new ArrayOperatorFilter<TDocument, TField, TItem>("$all", fieldName, values);
        }

        /// <summary>
        /// Creates an all filter.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <returns>An all filter.</returns>
        public Filter<TDocument> All<TItem>(string fieldName, IEnumerable<TItem> values)
        {
            return new ArrayOperatorFilter<TDocument, IEnumerable<TItem>, TItem>(
                "$all",
                new StringFieldName<TDocument, IEnumerable<TItem>>(fieldName),
                values);
        }

        /// <summary>
        /// Creates an all filter.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <returns>An all filter.</returns>
        public Filter<TDocument> All<TField, TItem>(Expression<Func<TDocument, TField>> fieldName, IEnumerable<TItem> values)
            where TField : IEnumerable<TItem>
        {
            return All(new ExpressionFieldName<TDocument, TField>(fieldName), values);
        }

        /// <summary>
        /// Creates an and filter.
        /// </summary>
        /// <param name="filters">The filters.</param>
        /// <returns>A filter.</returns>
        public Filter<TDocument> And(params Filter<TDocument>[] filters)
        {
            return And((IEnumerable<Filter<TDocument>>)filters);
        }

        /// <summary>
        /// Creates an and filter.
        /// </summary>
        /// <param name="filters">The filters.</param>
        /// <returns>An and filter.</returns>
        public Filter<TDocument> And(IEnumerable<Filter<TDocument>> filters)
        {
            return new AndFilter<TDocument>(filters);
        }

        /// <summary>
        /// Creates an element match filter.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>An element match filter.</returns>
        public Filter<TDocument> ElementMatch<TItem>(string fieldName, Filter<TItem> filter)
        {
            return new ElementMatchFilter<TDocument, IEnumerable<TItem>, TItem>(
                new StringFieldName<TDocument, IEnumerable<TItem>>(fieldName),
                filter);
        }

        /// <summary>
        /// Creates an element match filter.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>An element match filter.</returns>
        public Filter<TDocument> ElementMatch<TField, TItem>(FieldName<TDocument, TField> fieldName, Filter<TItem> filter)
            where TField : IEnumerable<TItem>
        {
            return new ElementMatchFilter<TDocument, TField, TItem>(fieldName, filter);
        }

        /// <summary>
        /// Creates an element match filter.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>An element match filter.</returns>
        public Filter<TDocument> ElementMatch<TField, TItem>(Expression<Func<TDocument, TField>> fieldName, Filter<TItem> filter)
            where TField : IEnumerable<TItem>
        {
            return ElementMatch(new ExpressionFieldName<TDocument, TField>(fieldName), filter);
        }

        /// <summary>
        /// Creates an element match filter.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>An element match filter.</returns>
        public Filter<TDocument> ElementMatch<TField, TItem>(Expression<Func<TDocument, TField>> fieldName, Expression<Func<TItem, bool>> filter)
            where TField : IEnumerable<TItem>
        {
            return ElementMatch(new ExpressionFieldName<TDocument, TField>(fieldName), new ExpressionFilter<TItem>(filter));
        }

        /// <summary>
        /// Creates an equality filter.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>An equality filter.</returns>
        public Filter<TDocument> Equal<TField>(FieldName<TDocument, TField> fieldName, TField value)
        {
            return new EqualFilter<TDocument, TField>(fieldName, value);
        }

        /// <summary>
        /// Creates an equality filter.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>An equality filter.</returns>
        public Filter<TDocument> Equal<TField>(Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            return Equal(new ExpressionFieldName<TDocument, TField>(fieldName), value);
        }

        /// <summary>
        /// Creates an exists filter.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="exists">if set to <c>true</c> [exists].</param>
        /// <returns>An exists filter.</returns>
        public Filter<TDocument> Exists(FieldName<TDocument> fieldName, bool exists = true)
        {
            return new OperatorFilter<TDocument>("$exists", fieldName, exists);
        }

        /// <summary>
        /// Creates an exists filter.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="exists">if set to <c>true</c> [exists].</param>
        /// <returns>An exists filter.</returns>
        public Filter<TDocument> Exists(Expression<Func<TDocument, object>> fieldName, bool exists = true)
        {
            return Exists(new ExpressionFieldName<TDocument>(fieldName), exists);
        }

        // TODO: GeoIntersects

        /// <summary>
        /// Creates a greater than filter.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A greater than filter.</returns>
        public Filter<TDocument> GreaterThan<TField>(FieldName<TDocument, TField> fieldName, TField value)
        {
            return new OperatorFilter<TDocument, TField>("$gt", fieldName, value);
        }

        /// <summary>
        /// Creates a greater than filter.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A greater than filter.</returns>
        public Filter<TDocument> GreaterThan<TField>(Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            return GreaterThan(new ExpressionFieldName<TDocument, TField>(fieldName), value);
        }

        /// <summary>
        /// Creates a greater than or equal filter.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A greater than or equal filter.</returns>
        public Filter<TDocument> GreaterThanOrEqual<TField>(FieldName<TDocument, TField> fieldName, TField value)
        {
            return new OperatorFilter<TDocument, TField>("$gte", fieldName, value);
        }

        /// <summary>
        /// Creates a greater than or equal filter.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A greater than or equal filter.</returns>
        public Filter<TDocument> GreaterThanOrEqual<TField>(Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            return GreaterThanOrEqual(new ExpressionFieldName<TDocument, TField>(fieldName), value);
        }

        /// <summary>
        /// Creates an in filter.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <returns>An in filter.</returns>
        public Filter<TDocument> In<TField, TItem>(FieldName<TDocument, TField> fieldName, IEnumerable<TItem> values)
            where TField : IEnumerable<TItem>
        {
            return new ArrayOperatorFilter<TDocument, TField, TItem>("$in", fieldName, values);
        }

        /// <summary>
        /// Creates an in filter.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <returns>An in filter.</returns>
        public Filter<TDocument> In<TItem>(string fieldName, IEnumerable<TItem> values)
        {
            return new ArrayOperatorFilter<TDocument, IEnumerable<TItem>, TItem>(
                "$in",
                new StringFieldName<TDocument, IEnumerable<TItem>>(fieldName),
                values);
        }

        /// <summary>
        /// Creates an in filter.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <returns>An in filter.</returns>
        public Filter<TDocument> In<TField, TItem>(Expression<Func<TDocument, TField>> fieldName, IEnumerable<TItem> values)
            where TField : IEnumerable<TItem>
        {
            return In(new ExpressionFieldName<TDocument, TField>(fieldName), values);
        }

        /// <summary>
        /// Creates a less than filter.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A less than filter.</returns>
        public Filter<TDocument> LessThan<TField>(FieldName<TDocument, TField> fieldName, TField value)
        {
            return new OperatorFilter<TDocument, TField>("$lt", fieldName, value);
        }

        /// <summary>
        /// Creates a less than filter.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A less than filter.</returns>
        public Filter<TDocument> LessThan<TField>(Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            return LessThan(new ExpressionFieldName<TDocument, TField>(fieldName), value);
        }

        /// <summary>
        /// Creates a less than or equal filter.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A less than or equal filter.</returns>
        public Filter<TDocument> LessThanOrEqual<TField>(FieldName<TDocument, TField> fieldName, TField value)
        {
            return new OperatorFilter<TDocument, TField>("$lte", fieldName, value);
        }

        /// <summary>
        /// Creates a less than or equal filter.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A less than or equal filter.</returns>
        public Filter<TDocument> LessThanOrEqual<TField>(Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            return LessThanOrEqual(new ExpressionFieldName<TDocument, TField>(fieldName), value);
        }

        // TODO: Modulo

        // TODO: Near

        /// <summary>
        /// Creates a not filter.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns>A not filter.</returns>
        public Filter<TDocument> Not(Filter<TDocument> filter)
        {
            return new NotFilter<TDocument>(filter);
        }

        /// <summary>
        /// Creates a not equal filter.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A not equal filter.</returns>
        public Filter<TDocument> NotEqual<TField>(FieldName<TDocument, TField> fieldName, TField value)
        {
            return new OperatorFilter<TDocument, TField>("$ne", fieldName, value);
        }

        /// <summary>
        /// Creates a not equal filter.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A not equal filter.</returns>
        public Filter<TDocument> NotEqual<TField>(Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            return NotEqual(new ExpressionFieldName<TDocument, TField>(fieldName), value);
        }

        /// <summary>
        /// Creates a not in filter.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <returns>A not in filter.</returns>
        public Filter<TDocument> NotIn<TField, TItem>(FieldName<TDocument, TField> fieldName, IEnumerable<TItem> values)
            where TField : IEnumerable<TItem>
        {
            return new ArrayOperatorFilter<TDocument, TField, TItem>("$nin", fieldName, values);
        }

        /// <summary>
        /// Creates a not in filter.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <returns>A not in filter.</returns>
        public Filter<TDocument> NotIn<TItem>(string fieldName, IEnumerable<TItem> values)
        {
            return new ArrayOperatorFilter<TDocument, IEnumerable<TItem>, TItem>(
                "$nin",
                new StringFieldName<TDocument, IEnumerable<TItem>>(fieldName),
                values);
        }

        /// <summary>
        /// Creates a not in filter.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <returns>A not in filter.</returns>
        public Filter<TDocument> NotIn<TField, TItem>(Expression<Func<TDocument, TField>> fieldName, IEnumerable<TItem> values)
            where TField : IEnumerable<TItem>
        {
            return NotIn(new ExpressionFieldName<TDocument, TField>(fieldName), values);
        }

        /// <summary>
        /// Creates an or filter.
        /// </summary>
        /// <param name="filters">The filters.</param>
        /// <returns>An or filter.</returns>
        public Filter<TDocument> Or(params Filter<TDocument>[] filters)
        {
            return Or((IEnumerable<Filter<TDocument>>)filters);
        }

        /// <summary>
        /// Creates an or filter.
        /// </summary>
        /// <param name="filters">The filters.</param>
        /// <returns>An or filter.</returns>
        public Filter<TDocument> Or(IEnumerable<Filter<TDocument>> filters)
        {
            return new OrFilter<TDocument>(filters);
        }

        /// <summary>
        /// Creates a regular expression filter.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="regex">The regex.</param>
        /// <returns>A regular expression filter.</returns>
        public Filter<TDocument> Regex(FieldName<TDocument> fieldName, BsonRegularExpression regex)
        {
            return new EqualFilter<TDocument>(fieldName, regex);
        }

        /// <summary>
        /// Creates a regular expression filter.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="regex">The regex.</param>
        /// <returns>A regular expression filter.</returns>
        public Filter<TDocument> Regex(Expression<Func<TDocument, object>> fieldName, BsonRegularExpression regex)
        {
            return Regex(new ExpressionFieldName<TDocument>(fieldName), regex);
        }

        /// <summary>
        /// Creates a size filter.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="size">The size.</param>
        /// <returns>A size filter.</returns>
        public Filter<TDocument> Size(FieldName<TDocument> fieldName, int size)
        {
            return new OperatorFilter<TDocument>("$size", fieldName, size);
        }

        /// <summary>
        /// Creates a size filter.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="size">The size.</param>
        /// <returns>A size filter.</returns>
        public Filter<TDocument> Size(Expression<Func<TDocument, object>> fieldName, int size)
        {
            return Size(new ExpressionFieldName<TDocument>(fieldName), size);
        }


        // TODO: SizeGreaterThan?
        // TODO: SizeLessThan?

        /// <summary>
        /// Creates a type filter.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="type">The type.</param>
        /// <returns>A type filter.</returns>
        public Filter<TDocument> Type(FieldName<TDocument> fieldName, BsonType type)
        {
            return new OperatorFilter<TDocument>("$type", fieldName, (int)type);
        }

        /// <summary>
        /// Creates a type filter.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="type">The type.</param>
        /// <returns>A type filter.</returns>
        public Filter<TDocument> Type(Expression<Func<TDocument, object>> fieldName, BsonType type)
        {
            return Type(new ExpressionFieldName<TDocument>(fieldName), type);
        }

        // TODO: Within
        // TODO: WithinCircle
        // TODO: WithinPolygon
        // TODO: WithinRectangle

        // TODO: Text
    }

    /// <summary>
    /// An and filter.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class AndFilter<TDocument> : Filter<TDocument>
    {
        private readonly List<Filter<TDocument>> _filters;

        /// <summary>
        /// Initializes a new instance of the <see cref="AndFilter{TDocument}"/> class.
        /// </summary>
        /// <param name="filters">The filters.</param>
        public AndFilter(IEnumerable<Filter<TDocument>> filters)
        {
            _filters = Ensure.IsNotNull(filters, "filters").ToList();
        }

        /// <inheritdoc />
        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var document = new BsonDocument();
            foreach (var filter in _filters)
            {
                var renderedFilter = filter.Render(documentSerializer, serializerRegistry);
                foreach (var clause in renderedFilter)
                {
                    AddClause(document, clause);
                }
            }

            return document;
        }

        private static void AddClause(BsonDocument document, BsonElement clause)
        {
            if (clause.Name == "$and")
            {
                // flatten out nested $and
                foreach (var item in (BsonArray)clause.Value)
                {
                    foreach (var element in (BsonDocument)item)
                    {
                        AddClause(document, element);
                    }
                }
            }

            if (document.ElementCount == 1 && document.GetElement(0).Name == "$and")
            {
                ((BsonArray)document[0]).Add(new BsonDocument(clause));
            }
            else if (document.Contains(clause.Name))
            {
                var existingClause = document.GetElement(clause.Name);
                if (existingClause.Value is BsonDocument && clause.Value is BsonDocument)
                {
                    var clauseValue = (BsonDocument)clause.Value;
                    var existingClauseValue = (BsonDocument)existingClause.Value;
                    if (clauseValue.Names.Any(op => existingClauseValue.Contains(op)))
                    {
                        PromoteFilterToDollarForm(document, clause);
                    }
                    else
                    {
                        existingClauseValue.AddRange(clauseValue);
                    }
                }
                else
                {
                    PromoteFilterToDollarForm(document, clause);
                }
            }
            else
            {
                document.Add(clause);
            }
        }

        private static void PromoteFilterToDollarForm(BsonDocument document, BsonElement clause)
        {
            var clauses = new BsonArray();
            foreach (var queryElement in document)
            {
                clauses.Add(new BsonDocument(queryElement));
            }
            clauses.Add(new BsonDocument(clause));
            document.Clear();
            document.Add("$and", clauses);
        }
    }

    /// <summary>
    /// An operator filter that renderes to an array of values.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TField">The type of the field.</typeparam>
    /// <typeparam name="TItem">The type of the item.</typeparam>
    public sealed class ArrayOperatorFilter<TDocument, TField, TItem> : Filter<TDocument>
        where TField : IEnumerable<TItem>
    {
        private string _operatorName;
        private readonly FieldName<TDocument, TField> _fieldName;
        private readonly IEnumerable<TItem> _values;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayOperatorFilter{TDocument, TField, TItem}"/> class.
        /// </summary>
        /// <param name="operatorName">Name of the operator.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The value.</param>
        public ArrayOperatorFilter(string operatorName, FieldName<TDocument, TField> fieldName, IEnumerable<TItem> values)
        {
            _operatorName = Ensure.IsNotNull(operatorName, operatorName);
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _values = values;
        }

        /// <inheritdoc />
        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var renderedField = _fieldName.Render(documentSerializer, serializerRegistry);
            var arraySerializer = renderedField.Serializer as IBsonArraySerializer;
            if (arraySerializer == null)
            {
                var message = string.Format("The serializer for field '{0}' must implement IBsonArraySerializer.", renderedField.FieldName);
                throw new InvalidOperationException(message);
            }
            var itemSerializer = arraySerializer.GetItemSerializationInfo().Serializer;

            var document = new BsonDocument();
            using (var bsonWriter = new BsonDocumentWriter(document))
            {
                var context = BsonSerializationContext.CreateRoot(bsonWriter);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName(renderedField.FieldName);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName(_operatorName);
                bsonWriter.WriteStartArray();
                foreach (var value in _values)
                {
                    itemSerializer.Serialize(context, value);
                }
                bsonWriter.WriteEndArray();
                bsonWriter.WriteEndDocument();
                bsonWriter.WriteEndDocument();
            }

            return document;
        }
    }

    /// <summary>
    /// An element match filter.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TField">The type of the field.</typeparam>
    /// <typeparam name="TItem">The type of the item.</typeparam>
    public sealed class ElementMatchFilter<TDocument, TField, TItem> : Filter<TDocument>
    {
        private readonly FieldName<TDocument, TField> _fieldName;
        private readonly Filter<TItem> _filter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ElementMatchFilter{TDocument, TField, TItem}" /> class.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="filter">The filter.</param>
        public ElementMatchFilter(FieldName<TDocument, TField> fieldName, Filter<TItem> filter)
        {
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _filter = filter;
        }

        /// <inheritdoc />
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

    /// <summary>
    /// An equality filter where the type doesn't matter.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class EqualFilter<TDocument> : Filter<TDocument>
    {
        private readonly FieldName<TDocument> _fieldName;
        private readonly BsonValue _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="EqualFilter{TDocument}"/> class.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        public EqualFilter(FieldName<TDocument> fieldName, BsonValue value)
        {
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _value = value;
        }

        /// <inheritdoc />
        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var renderedField = _fieldName.Render(documentSerializer, serializerRegistry);
            return new BsonDocument(renderedField, _value);
        }
    }

    /// <summary>
    /// An equality filter where the type matters.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TField">The type of the field.</typeparam>
    public sealed class EqualFilter<TDocument, TField> : Filter<TDocument>
    {
        private readonly FieldName<TDocument, TField> _fieldName;
        private readonly TField _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="EqualFilter{TDocument, TField}"/> class.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        public EqualFilter(FieldName<TDocument, TField> fieldName, TField value)
        {
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _value = value;
        }

        /// <inheritdoc />
        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var renderedField = _fieldName.Render(documentSerializer, serializerRegistry);
            var document = new BsonDocument();
            using (var bsonWriter = new BsonDocumentWriter(document))
            {
                var context = BsonSerializationContext.CreateRoot(bsonWriter);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName(renderedField.FieldName);
                renderedField.Serializer.Serialize(context, _value);
                bsonWriter.WriteEndDocument();
            }

            return document;
        }
    }

    /// <summary>
    /// A not filter.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class NotFilter<TDocument> : Filter<TDocument>
    {
        private readonly Filter<TDocument> _filter;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotFilter{TDocument}"/> class.
        /// </summary>
        /// <param name="filter">The filter.</param>
        public NotFilter(Filter<TDocument> filter)
        {
            _filter = Ensure.IsNotNull(filter, "filter");
        }

        /// <inheritdoc />
        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var renderedFilter = _filter.Render(documentSerializer, serializerRegistry);
            if (renderedFilter.ElementCount == 1)
            {
                return NegateSingleElementFilter(renderedFilter, renderedFilter.GetElement(0));
            }

            return NegateArbitraryFilter(renderedFilter);
        }

        private static BsonDocument NegateArbitraryFilter(BsonDocument filter)
        {
            // $not only works as a meta operator on a single operator so simulate Not using $nor
            return new BsonDocument("$nor", new BsonArray { filter });
        }

        private static BsonDocument NegateSingleElementFilter(BsonDocument filter, BsonElement element)
        {
            if (element.Name[0] == '$')
            {
                return NegateSingleElementTopLevelOperatorFilter(filter, element);
            }

            if (element.Value is BsonDocument)
            {
                var selector = (BsonDocument)element.Value;
                if (selector.ElementCount >= 1)
                {
                    var operatorName = selector.GetElement(0).Name;
                    if (operatorName[0] == '$' && operatorName != "$ref")
                    {
                        if (selector.ElementCount == 1)
                        {
                            return NegateSingleFieldOperatorFilter(element.Name, selector.GetElement(0));
                        }

                        return NegateArbitraryFilter(filter);
                    }
                }
            }

            if (element.Value is BsonRegularExpression)
            {
                return new BsonDocument(element.Name, new BsonDocument("$not", element.Value));
            }

            return new BsonDocument(element.Name, new BsonDocument("$ne", element.Value));
        }

        private static BsonDocument NegateSingleFieldOperatorFilter(string fieldName, BsonElement element)
        {
            switch (element.Name)
            {
                case "$exists":
                    return new BsonDocument(fieldName, new BsonDocument("$exists", !element.Value.ToBoolean()));
                case "$in":
                    return new BsonDocument(fieldName, new BsonDocument("$nin", (BsonArray)element.Value));
                case "$ne":
                case "$not":
                    return new BsonDocument(fieldName, element.Value);
                case "$nin":
                    return new BsonDocument(fieldName, new BsonDocument("$in", (BsonArray)element.Value));
                default:
                    return new BsonDocument(fieldName, new BsonDocument("$not", new BsonDocument(element)));
            }
        }

        private static BsonDocument NegateSingleElementTopLevelOperatorFilter(BsonDocument filter, BsonElement element)
        {
            switch (element.Name)
            {
                case "$or":
                    return new BsonDocument("$nor", element.Value);
                case "$nor":
                    return new BsonDocument("$or", element.Value);
                default:
                    return NegateArbitraryFilter(filter);
            }
        }
    }

    /// <summary>
    /// A general operator filter where the field type doesn't matter.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class OperatorFilter<TDocument> : Filter<TDocument>
    {
        private readonly string _operatorName;
        private readonly FieldName<TDocument> _fieldName;
        private readonly BsonValue _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperatorFilter{TDocument}"/> class.
        /// </summary>
        /// <param name="operatorName">Name of the operator.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        public OperatorFilter(string operatorName, FieldName<TDocument> fieldName, BsonValue value)
        {
            _operatorName = Ensure.IsNotNull(operatorName, operatorName);
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _value = value;
        }

        /// <inheritdoc />
        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var renderedField = _fieldName.Render(documentSerializer, serializerRegistry);
            return new BsonDocument(renderedField, new BsonDocument(_operatorName, _value));
        }
    }

    /// <summary>
    /// A general operator filter where the field type matters.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TField">The type of the field.</typeparam>
    public sealed class OperatorFilter<TDocument, TField> : Filter<TDocument>
    {
        private readonly string _operatorName;
        private readonly FieldName<TDocument, TField> _fieldName;
        private readonly TField _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperatorFilter{TDocument, TField}"/> class.
        /// </summary>
        /// <param name="operatorName">Name of the operator.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        public OperatorFilter(string operatorName, FieldName<TDocument, TField> fieldName, TField value)
        {
            _operatorName = Ensure.IsNotNull(operatorName, operatorName);
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _value = value;
        }

        /// <inheritdoc />
        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var renderedField = _fieldName.Render(documentSerializer, serializerRegistry);
            var document = new BsonDocument();
            using (var bsonWriter = new BsonDocumentWriter(document))
            {
                var context = BsonSerializationContext.CreateRoot(bsonWriter);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName(renderedField.FieldName);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName(_operatorName);
                renderedField.Serializer.Serialize(context, _value);
                bsonWriter.WriteEndDocument();
                bsonWriter.WriteEndDocument();
            }

            return document;
        }
    }

    /// <summary>
    /// An or filter.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class OrFilter<TDocument> : Filter<TDocument>
    {
        private readonly List<Filter<TDocument>> _filters;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrFilter{TDocument}"/> class.
        /// </summary>
        /// <param name="filters">The filters.</param>
        public OrFilter(IEnumerable<Filter<TDocument>> filters)
        {
            _filters = Ensure.IsNotNull(filters, "filters").ToList();
        }

        /// <inheritdoc />
        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var clauses = new BsonArray();
            foreach (var filter in _filters)
            {
                var renderedFilter = filter.Render(documentSerializer, serializerRegistry);
                AddClause(clauses, renderedFilter);
            }

            return new BsonDocument("$or", clauses);
        }

        private static void AddClause(BsonArray clauses, BsonDocument filter)
        {
            if (filter.ElementCount == 1 && filter.GetElement(0).Name == "$or")
            {
                // flatten nested $or
                clauses.AddRange((BsonArray)filter[0]);
            }
            else
            {
                // we could shortcut the user's query if there are no elements in the filter, but
                // I'd rather be literal and let them discover the problem on their own.
                clauses.Add(filter);
            }
        }
    }
}
