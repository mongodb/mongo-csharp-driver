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
using MongoDB.Driver.Linq.Expressions;
using MongoDB.Driver.Linq.Processors;
using MongoDB.Driver.Linq.Utils;

namespace MongoDB.Driver
{
    /// <summary>
    /// A rendered field name.
    /// </summary>
    /// <typeparam name="TField">The type of the field.</typeparam>
    public sealed class RenderedFieldDefinition<TField>
    {
        private readonly string _fieldName;
        private readonly IBsonSerializer<TField> _fieldSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderedFieldDefinition{TField}" /> class.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="fieldSerializer">The field serializer.</param>
        public RenderedFieldDefinition(string fieldName, IBsonSerializer<TField> fieldSerializer)
        {
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _fieldSerializer = Ensure.IsNotNull(fieldSerializer, "fieldSerializer");
        }

        /// <summary>
        /// Gets the field name.
        /// </summary>
        public string FieldName
        {
            get { return _fieldName; }
        }

        /// <summary>
        /// Gets the field serializer.
        /// </summary>
        public IBsonSerializer<TField> FieldSerializer
        {
            get { return _fieldSerializer; }
        }
    }

    /// <summary>
    /// Base class for field names.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public abstract class FieldDefinition<TDocument>
    {
        /// <summary>
        /// Renders the field to a <see cref="String"/>.
        /// </summary>
        /// <param name="documentSerializer">The document serializer.</param>
        /// <param name="serializerRegistry">The serializer registry.</param>
        /// <returns>A <see cref="String"/>.</returns>
        public abstract string Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry);

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String"/> to <see cref="FieldDefinition{TDocument}"/>.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator FieldDefinition<TDocument>(string fieldName)
        {
            if (fieldName == null)
            {
                return null;
            }

            return new StringFieldDefinition<TDocument>(fieldName);
        }
    }

    /// <summary>
    /// Base class for field names.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TField">The type of the field.</typeparam>
    public abstract class FieldDefinition<TDocument, TField>
    {
        /// <summary>
        /// Renders the field to a <see cref="String"/>.
        /// </summary>
        /// <param name="documentSerializer">The document serializer.</param>
        /// <param name="serializerRegistry">The serializer registry.</param>
        /// <returns>A <see cref="String"/>.</returns>
        public abstract RenderedFieldDefinition<TField> Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry);

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String" /> to <see cref="FieldDefinition{TDocument, TField}" />.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator FieldDefinition<TDocument, TField>(string fieldName)
        {
            if (fieldName == null)
            {
                return null;
            }

            return new StringFieldDefinition<TDocument, TField>(fieldName, null);
        }
    }

    /// <summary>
    /// An <see cref="Expression" /> based field.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class ExpressionFieldDefinition<TDocument> : FieldDefinition<TDocument>
    {
        private readonly Expression<Func<TDocument, object>> _expression;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionFieldDefinition{TDocument}" /> class.
        /// </summary>
        /// <param name="expression">The expression.</param>
        public ExpressionFieldDefinition(Expression<Func<TDocument, object>> expression)
        {
            _expression = Ensure.IsNotNull(expression, "expression");
        }

        /// <summary>
        /// Gets the expression.
        /// </summary>
        public Expression<Func<TDocument, object>> Expression
        {
            get { return _expression; }
        }

        /// <inheritdoc />
        public override string Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var binder = new SerializationInfoBinder(serializerRegistry);
            var parameterSerializationInfo = new BsonSerializationInfo(null, documentSerializer, documentSerializer.ValueType);
            var parameterExpression = new SerializationExpression(_expression.Parameters[0], parameterSerializationInfo);
            binder.RegisterParameterReplacement(_expression.Parameters[0], parameterExpression);
            var bound = binder.Bind(_expression.Body) as ISerializationExpression;
            if (bound == null)
            {
                var message = string.Format("Unable to determine the serialization information for {0}.", _expression);
                throw new InvalidOperationException(message);
            }

            return bound.SerializationInfo.ElementName;
        }
    }

    /// <summary>
    /// An <see cref="Expression" /> based field.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TField">The type of the field.</typeparam>
    public sealed class ExpressionFieldDefinition<TDocument, TField> : FieldDefinition<TDocument, TField>
    {
        private readonly Expression<Func<TDocument, TField>> _expression;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionFieldDefinition{TDocument, TField}" /> class.
        /// </summary>
        /// <param name="expression">The expression.</param>
        public ExpressionFieldDefinition(Expression<Func<TDocument, TField>> expression)
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
        public override RenderedFieldDefinition<TField> Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var binder = new SerializationInfoBinder(serializerRegistry);
            var parameterSerializationInfo = new BsonSerializationInfo(null, documentSerializer, documentSerializer.ValueType);
            var parameterExpression = new SerializationExpression(_expression.Parameters[0], parameterSerializationInfo);
            binder.RegisterParameterReplacement(_expression.Parameters[0], parameterExpression);
            var bound = binder.Bind(_expression.Body) as ISerializationExpression;
            if (bound == null)
            {
                var message = string.Format("Unable to determine the serialization information for {0}.", _expression);
                throw new InvalidOperationException(message);
            }

            return new RenderedFieldDefinition<TField>(bound.SerializationInfo.ElementName, (IBsonSerializer<TField>)bound.SerializationInfo.Serializer);
        }
    }

    /// <summary>
    /// A <see cref="String" /> based field name.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class StringFieldDefinition<TDocument> : FieldDefinition<TDocument>
    {
        private readonly string _fieldName;

        /// <summary>
        /// Initializes a new instance of the <see cref="StringFieldDefinition{TDocument}" /> class.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        public StringFieldDefinition(string fieldName)
        {
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
        }

        /// <inheritdoc />
        public override string Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            return _fieldName;
        }
    }

    /// <summary>
    /// A <see cref="String" /> based field name.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TField">The type of the field.</typeparam>
    public sealed class StringFieldDefinition<TDocument, TField> : FieldDefinition<TDocument, TField>
    {
        private readonly string _fieldName;
        private readonly IBsonSerializer<TField> _fieldSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="StringFieldDefinition{TDocument, TField}" /> class.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="fieldSerializer">The field serializer.</param>
        public StringFieldDefinition(string fieldName, IBsonSerializer<TField> fieldSerializer = null)
        {
            _fieldName = Ensure.IsNotNull(fieldName, "fieldName");
            _fieldSerializer = fieldSerializer;
        }

        /// <inheritdoc />
        public override RenderedFieldDefinition<TField> Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            // TODO: if we had reverse mapping from field name to member name, we could get the serializer
            // because we know the type of document we are using.

            return new RenderedFieldDefinition<TField>(
                _fieldName,
                _fieldSerializer ?? serializerRegistry.GetSerializer<TField>());
        }
    }
}
