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
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Utils;

namespace MongoDB.Driver
{
    /// <summary>
    /// A rendered field name.
    /// </summary>
    /// <typeparam name="TField">The type of the field.</typeparam>
    public sealed class RenderedFieldName<TField>
    {
        private readonly string _fieldName;
        private readonly IBsonSerializer<TField> _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderedFieldName{TField}" /> class.
        /// </summary>
        /// <param name="fieldName">The document.</param>
        /// <param name="serializer">The serializer.</param>
        public RenderedFieldName(string fieldName, IBsonSerializer<TField> serializer)
        {
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _serializer = Ensure.IsNotNull(serializer, "serializer");
        }

        /// <summary>
        /// Gets the field name.
        /// </summary>
        public string FieldName
        {
            get { return _fieldName; }
        }

        /// <summary>
        /// Gets the serializer.
        /// </summary>
        public IBsonSerializer<TField> Serializer
        {
            get { return _serializer; }
        }
    }

    /// <summary>
    /// Base class for field names.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TField">The type of the field.</typeparam>
    public abstract class FieldName<TDocument, TField>
    {
        /// <summary>
        /// Renders the field to a <see cref="String"/>.
        /// </summary>
        /// <param name="documentSerializer">The document serializer.</param>
        /// <param name="serializerRegistry">The serializer registry.</param>
        /// <returns>A <see cref="String"/>.</returns>
        public abstract RenderedFieldName<TField> Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry);

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String" /> to <see cref="FieldName{TDocument, TField}" />.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator FieldName<TDocument, TField>(string fieldName)
        {
            return new StringFieldName<TDocument, TField>(fieldName, null);
        }
    }

    /// <summary>
    /// An <see cref="Expression" /> based field.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TField">The type of the field.</typeparam>
    public sealed class ExpressionFieldName<TDocument, TField> : FieldName<TDocument, TField>
    {
        private readonly Expression<Func<TDocument, TField>> _expression;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionFieldName{TDocument, TField}" /> class.
        /// </summary>
        /// <param name="expression">The expression.</param>
        public ExpressionFieldName(Expression<Func<TDocument, TField>> expression)
        {
            _expression = Ensure.IsNotNull(expression, "expression");
        }

        /// <summary>
        /// Gets the expression.
        /// </summary>
        public Expression<Func<TDocument, TField>> Expression
        {
            get { return _expression; }
        }

        /// <inheritdoc />
        public override RenderedFieldName<TField> Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var helper = new BsonSerializationInfoHelper();
            helper.RegisterExpressionSerializer(_expression.Parameters[0], documentSerializer);
            var serializationInfo = helper.GetSerializationInfo(_expression.Body);
            return new RenderedFieldName<TField>(serializationInfo.ElementName, (IBsonSerializer<TField>)serializationInfo.Serializer);
        }
    }

    /// <summary>
    /// A <see cref="String" /> based field name.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TField">The type of the field.</typeparam>
    public sealed class StringFieldName<TDocument, TField> : FieldName<TDocument, TField>
    {
        private readonly string _fieldName;
        private readonly IBsonSerializer<TField> _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="StringFieldName{TDocument, TField}" /> class.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="serializer">The serializer.</param>
        public StringFieldName(string fieldName, IBsonSerializer<TField> serializer = null)
        {
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _serializer = serializer;
        }

        /// <inheritdoc />
        public override RenderedFieldName<TField> Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            // TODO: if we had reverse mapping from field name to member name, we could get the serializer
            // because we know the type of document we are using.

            return new RenderedFieldName<TField>(
                _fieldName,
                _serializer ?? serializerRegistry.GetSerializer<TField>());
        }
    }
}
