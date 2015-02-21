using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class FilterBuilder<TDocument>
    {
        /// <summary>
        /// Ands the specified filters.
        /// </summary>
        /// <param name="filters">The filters.</param>
        /// <returns></returns>
        public Filter<TDocument> And(params Filter<TDocument>[] filters)
        {
            return new AndFilter<TDocument>(filters);
        }

        /// <summary>
        /// Ors the specified filters.
        /// </summary>
        /// <param name="filters">The filters.</param>
        /// <returns></returns>
        public Filter<TDocument> Or(params Filter<TDocument>[] filters)
        {
            return new OrFilter<TDocument>(filters);
        }

        /// <summary>
        /// Eqs the specified field name.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public Filter<TDocument> EQ<TField>(FieldName<TDocument, TField> fieldName, TField value)
        {
            return new EQFilter<TDocument, TField>(fieldName, value);
        }

        /// <summary>
        /// Eqs the specified field name.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public Filter<TDocument> EQ<TField>(Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            return EQ(new ExpressionFieldName<TDocument, TField>(fieldName), value);
        }

        /// <summary>
        /// Existses the specified field name.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="exists">if set to <c>true</c> [exists].</param>
        /// <returns></returns>
        public Filter<TDocument> Exists(FieldName<TDocument> fieldName, bool exists = true)
        {
            return new OperatorFilter<TDocument>("$exists", fieldName, exists);
        }

        /// <summary>
        /// Existses the specified field name.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="exists">if set to <c>true</c> [exists].</param>
        /// <returns></returns>
        public Filter<TDocument> Exists(Expression<Func<TDocument, object>> fieldName, bool exists = true)
        {
            return Exists(new ExpressionFieldName<TDocument>(fieldName), exists);
        }

        /// <summary>
        /// Gts the specified field name.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public Filter<TDocument> GT<TField>(FieldName<TDocument, TField> fieldName, TField value)
        {
            return new OperatorFilter<TDocument, TField>("$gt", fieldName, value);
        }

        /// <summary>
        /// Eqs the specified field name.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public Filter<TDocument> GT<TField>(Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            return GT(new ExpressionFieldName<TDocument, TField>(fieldName), value);
        }

        /// <summary>
        /// Gtes the specified field name.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public Filter<TDocument> GTE<TField>(FieldName<TDocument, TField> fieldName, TField value)
        {
            return new OperatorFilter<TDocument, TField>("$gte", fieldName, value);
        }

        /// <summary>
        /// Gtes the specified field name.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public Filter<TDocument> GTE<TField>(Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            return GTE(new ExpressionFieldName<TDocument, TField>(fieldName), value);
        }

        /// <summary>
        /// Ins the specified field name.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <returns></returns>
        public Filter<TDocument> In<TField, TItem>(FieldName<TDocument, TField> fieldName, IEnumerable<TItem> values)
            where TField : IEnumerable<TItem>
        {
            return new ArrayOperatorFilter<TDocument, TField, TItem>("$in", fieldName, values);
        }

        /// <summary>
        /// Ins the specified field name.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <returns></returns>
        public Filter<TDocument> In<TItem>(string fieldName, IEnumerable<TItem> values)
        {
            return new ArrayOperatorFilter<TDocument, IEnumerable<TItem>, TItem>(
                "$in",
                new StringFieldName<TDocument, IEnumerable<TItem>>(fieldName),
                values);
        }

        /// <summary>
        /// Ins the specified field name.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="values">The values.</param>
        /// <returns></returns>
        public Filter<TDocument> In<TField, TItem>(Expression<Func<TDocument, TField>> fieldName, IEnumerable<TItem> values)
            where TField : IEnumerable<TItem>
        {
            return In(new ExpressionFieldName<TDocument, TField>(fieldName), values);
        }
    }

    /// <summary>
    /// 
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
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 
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
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TField">The type of the field.</typeparam>
    public sealed class EQFilter<TDocument, TField> : Filter<TDocument>
    {
        private readonly FieldName<TDocument, TField> _fieldName;
        private readonly TField _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="EQFilter{TDocument, TField}"/> class.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        public EQFilter(FieldName<TDocument, TField> fieldName, TField value)
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
    /// 
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class OperatorFilter<TDocument> : Filter<TDocument>
    {
        private string _operatorName;
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
    /// 
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TField">The type of the field.</typeparam>
    public sealed class OperatorFilter<TDocument, TField> : Filter<TDocument>
    {
        private string _operatorName;
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
    /// 
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
                throw new NotSupportedException();
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
                foreach(var value in _values)
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
}
