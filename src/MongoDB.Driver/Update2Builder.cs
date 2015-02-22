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
    /// A builder for a <see cref="Update2{TDocument}"/>.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class Update2Builder<TDocument>
    {
        // TODO: AddToSet
        // TODO: AddToSetEach

        /// <summary>
        /// Creates a bitwise and operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A bitwise and operator.</returns>
        public Update2<TDocument> BitwiseAnd<TField>(FieldName<TDocument, TField> fieldName, TField value)
        {
            return new BitwiseOperatorUpdate<TDocument, TField>("and", fieldName, value);
        }

        /// <summary>
        /// Creates a bitwise and operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A bitwise and operator.</returns>
        public Update2<TDocument> BitwiseAnd<TField>(Expression<Func<TDocument, TField>> fieldName, TField value)
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
        public Update2<TDocument> BitwiseOr<TField>(FieldName<TDocument, TField> fieldName, TField value)
        {
            return new BitwiseOperatorUpdate<TDocument, TField>("or", fieldName, value);
        }

        /// <summary>
        /// Creates a bitwise or operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A bitwise or operator.</returns>
        public Update2<TDocument> BitwiseOr<TField>(Expression<Func<TDocument, TField>> fieldName, TField value)
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
        public Update2<TDocument> BitwiseXor<TField>(FieldName<TDocument, TField> fieldName, TField value)
        {
            return new BitwiseOperatorUpdate<TDocument, TField>("xor", fieldName, value);
        }

        /// <summary>
        /// Creates a bitwise xor operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A bitwise xor operator.</returns>
        public Update2<TDocument> BitwiseXor<TField>(Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            return BitwiseXor(new ExpressionFieldName<TDocument, TField>(fieldName), value);
        }

        /// <summary>
        /// Creates a combined update.
        /// </summary>
        /// <param name="updates">The updates.</param>
        /// <returns>A combined update.</returns>
        public Update2<TDocument> Combine(params Update2<TDocument>[] updates)
        {
            return Combine((IEnumerable<Update2<TDocument>>)updates);
        }

        /// <summary>
        /// Creates a combined update.
        /// </summary>
        /// <param name="updates">The updates.</param>
        /// <returns>A combined update.</returns>
        public Update2<TDocument> Combine(IEnumerable<Update2<TDocument>> updates)
        {
            return new CombineUpdate<TDocument>(updates);
        }

        /// <summary>
        /// Creates a current date operator.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="type">The type.</param>
        /// <returns>A current date operator.</returns>
        public Update2<TDocument> CurrentDate(FieldName<TDocument> fieldName, Update2CurrentDateType? type = null)
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

            return new OperatorUpdate<TDocument>("$currentDate", fieldName, value);
        }

        /// <summary>
        /// Creates a current date operator.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="type">The type.</param>
        /// <returns>A current date operator.</returns>
        public Update2<TDocument> CurrentDate(Expression<Func<TDocument, object>> fieldName, Update2CurrentDateType? type = null)
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
        public Update2<TDocument> Increment<TField>(FieldName<TDocument, TField> fieldName, TField value)
        {
            return new OperatorUpdate<TDocument, TField>("$inc", fieldName, value);
        }

        /// <summary>
        /// Creates an increment operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>An increment operator.</returns>
        public Update2<TDocument> Increment<TField>(Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            return Increment(new ExpressionFieldName<TDocument, TField>(fieldName), value);
        }

        /// <summary>
        /// Creates a max operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A max operator.</returns>
        public Update2<TDocument> Max<TField>(FieldName<TDocument, TField> fieldName, TField value)
        {
            return new OperatorUpdate<TDocument, TField>("$max", fieldName, value);
        }

        /// <summary>
        /// Creates a max operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A max operator.</returns>
        public Update2<TDocument> Max<TField>(Expression<Func<TDocument, TField>> fieldName, TField value)
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
        public Update2<TDocument> Min<TField>(FieldName<TDocument, TField> fieldName, TField value)
        {
            return new OperatorUpdate<TDocument, TField>("$min", fieldName, value);
        }

        /// <summary>
        /// Creates a min operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A min operator.</returns>
        public Update2<TDocument> Min<TField>(Expression<Func<TDocument, TField>> fieldName, TField value)
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
        public Update2<TDocument> Multiply<TField>(FieldName<TDocument, TField> fieldName, TField value)
        {
            return new OperatorUpdate<TDocument, TField>("$mul", fieldName, value);
        }

        /// <summary>
        /// Creates a multiply operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A multiply operator.</returns>
        public Update2<TDocument> Multiply<TField>(Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            return Multiply(new ExpressionFieldName<TDocument, TField>(fieldName), value);
        }

        /// <summary>
        /// Creates a pop operator.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>A pop operator.</returns>
        public Update2<TDocument> PopFirst(FieldName<TDocument> fieldName)
        {
            return new OperatorUpdate<TDocument>("$pop", fieldName, -1);
        }

        /// <summary>
        /// Creates a pop first operator.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>A pop first operator.</returns>
        public Update2<TDocument> PopFirst(Expression<Func<TDocument, object>> fieldName)
        {
            return PopFirst(new ExpressionFieldName<TDocument>(fieldName));
        }

        /// <summary>
        /// Creates a pop operator.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>A pop operator.</returns>
        public Update2<TDocument> PopLast(FieldName<TDocument> fieldName)
        {
            return new OperatorUpdate<TDocument>("$pop", fieldName, 1);
        }

        /// <summary>
        /// Creates a pop first operator.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>A pop first operator.</returns>
        public Update2<TDocument> PopLast(Expression<Func<TDocument, object>> fieldName)
        {
            return PopLast(new ExpressionFieldName<TDocument>(fieldName));
        }

        // TODO: Pull
        // TODO: PullAll

        // TODO: Push
        // TODO: PushAll
        // TODO: PushEach

        /// <summary>
        /// Creates a field renaming operator.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="newName">The new name.</param>
        /// <returns>A field rename operator.</returns>
        public Update2<TDocument> Rename(FieldName<TDocument> fieldName, string newName)
        {
            return new OperatorUpdate<TDocument>("$rename", fieldName, newName);
        }

        /// <summary>
        /// Creates a field renaming operator.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="newName">The new name.</param>
        /// <returns>A field rename operator.</returns>
        public Update2<TDocument> Rename(Expression<Func<TDocument, object>> fieldName, string newName)
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
        public Update2<TDocument> Set<TField>(FieldName<TDocument, TField> fieldName, TField value)
        {
            return new OperatorUpdate<TDocument, TField>("$set", fieldName, value);
        }

        /// <summary>
        /// Creates a set operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A set operator.</returns>
        public Update2<TDocument> Set<TField>(Expression<Func<TDocument, TField>> fieldName, TField value)
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
        public Update2<TDocument> SetOnInsert<TField>(FieldName<TDocument, TField> fieldName, TField value)
        {
            return new OperatorUpdate<TDocument, TField>("$setOnInsert", fieldName, value);
        }

        /// <summary>
        /// Creates a set on insert operator.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        /// <returns>A set on insert operator.</returns>
        public Update2<TDocument> SetOnInsert<TField>(Expression<Func<TDocument, TField>> fieldName, TField value)
        {
            return SetOnInsert(new ExpressionFieldName<TDocument, TField>(fieldName), value);
        }

        /// <summary>
        /// Creates an unset operator.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>An unset operator.</returns>
        public Update2<TDocument> Unset(FieldName<TDocument> fieldName)
        {
            return new OperatorUpdate<TDocument>("$unset", fieldName, 1);
        }

        /// <summary>
        /// Creates an unset operator.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>An unset operator.</returns>
        public Update2<TDocument> Unset(Expression<Func<TDocument, object>> fieldName)
        {
            return Unset(new ExpressionFieldName<TDocument>(fieldName));
        }
    }

    /// <summary>
    /// A combining update.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class CombineUpdate<TDocument> : Update2<TDocument>
    {
        private readonly List<Update2<TDocument>> _updates;

        /// <summary>
        /// Initializes a new instance of the <see cref="CombineUpdate{TDocument}"/> class.
        /// </summary>
        /// <param name="updates">The updates.</param>
        public CombineUpdate(IEnumerable<Update2<TDocument>> updates)
        {
            _updates = Ensure.IsNotNull(updates, "updates").ToList();
        }

        /// <inheritdoc />
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

    /// <summary>
    /// A bitwise update operator.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TField">The type of the field.</typeparam>
    public sealed class BitwiseOperatorUpdate<TDocument, TField> : Update2<TDocument>
    {
        private readonly string _bitwiseOperatorName;
        private readonly FieldName<TDocument, TField> _fieldName;
        private readonly TField _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="BitwiseOperatorUpdate{TDocument, TField}"/> class.
        /// </summary>
        /// <param name="bitwiseOperatorName">Name of the bitwise operator.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        public BitwiseOperatorUpdate(string bitwiseOperatorName, FieldName<TDocument, TField> fieldName, TField value)
        {
            _bitwiseOperatorName = Ensure.IsNotNull(bitwiseOperatorName, "bitwiseOperatorName");
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
                bsonWriter.WriteName("$bit");
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName(renderedField.FieldName);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName(_bitwiseOperatorName);
                renderedField.Serializer.Serialize(context, _value);
                bsonWriter.WriteEndDocument();
                bsonWriter.WriteEndDocument();
                bsonWriter.WriteEndDocument();
            }

            return document;
        }
    }


    /// <summary>
    /// A general operator update where the type of field doesn't matter.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class OperatorUpdate<TDocument> : Update2<TDocument>
    {
        private readonly string _operatorName;
        private readonly FieldName<TDocument> _fieldName;
        private readonly BsonValue _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperatorUpdate{TDocument}"/> class.
        /// </summary>
        /// <param name="operatorName">Name of the operator.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        public OperatorUpdate(string operatorName, FieldName<TDocument> fieldName, BsonValue value)
        {
            _operatorName = Ensure.IsNotNull(operatorName, "operatorName");
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _value = value;
        }

        /// <inheritdoc />
        public override BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var renderedField = _fieldName.Render(documentSerializer, serializerRegistry);

            return new BsonDocument(_operatorName, new BsonDocument(renderedField, _value));
        }
    }

    /// <summary>
    /// A general operator update where the type of field matters.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TField">The type of the field.</typeparam>
    public sealed class OperatorUpdate<TDocument, TField> : Update2<TDocument>
    {
        private readonly string _operatorName;
        private readonly FieldName<TDocument, TField> _fieldName;
        private readonly TField _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperatorUpdate{TDocument, TField}"/> class.
        /// </summary>
        /// <param name="operatorName">Name of the operator.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>
        public OperatorUpdate(string operatorName, FieldName<TDocument, TField> fieldName, TField value)
        {
            _operatorName = Ensure.IsNotNull(operatorName, "operatorName");
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
                bsonWriter.WriteName(_operatorName);
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName(renderedField.FieldName);
                renderedField.Serializer.Serialize(context, _value);
                bsonWriter.WriteEndDocument();
                bsonWriter.WriteEndDocument();
            }

            return document;
        }
    }

}
