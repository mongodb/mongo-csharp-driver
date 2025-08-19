/* Copyright 2010-present MongoDB Inc.
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
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq;

namespace MongoDB.Driver
{
    /// <summary>
    /// A rendered field.
    /// </summary>
    public sealed class RenderedFieldDefinition
    {
        private readonly string _fieldName;
        private readonly IBsonSerializer _fieldSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderedFieldDefinition{TField}" /> class.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="fieldSerializer">The field serializer.</param>
        public RenderedFieldDefinition(string fieldName, IBsonSerializer fieldSerializer = null)
        {
            _fieldName = Ensure.IsNotNull(fieldName, nameof(fieldName));
            _fieldSerializer = fieldSerializer;
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
        public IBsonSerializer FieldSerializer
        {
            get { return _fieldSerializer; }
        }
    }

    /// <summary>
    /// A rendered field.
    /// </summary>
    /// <typeparam name="TField">The type of the field.</typeparam>
    public sealed class RenderedFieldDefinition<TField>
    {
        private readonly string _fieldName;
        private readonly IBsonSerializer<TField> _fieldSerializer;
        private readonly IBsonSerializer _underlyingSerializer;
        private readonly IBsonSerializer<TField> _valueSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderedFieldDefinition{TField}" /> class.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="fieldSerializer">The field serializer.</param>
        [Obsolete("Use the constructor that takes 4 arguments instead.")]
        public RenderedFieldDefinition(string fieldName, IBsonSerializer<TField> fieldSerializer)
            : this(fieldName, fieldSerializer, fieldSerializer, fieldSerializer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderedFieldDefinition{TField}" /> class.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="fieldSerializer">The field serializer.</param>
        /// <param name="valueSerializer">The value serializer.</param>
        /// <param name="underlyingSerializer">The underlying serializer.</param>
        public RenderedFieldDefinition(string fieldName, IBsonSerializer<TField> fieldSerializer, IBsonSerializer<TField> valueSerializer, IBsonSerializer underlyingSerializer)
        {
            _fieldName = Ensure.IsNotNull(fieldName, nameof(fieldName));
            _fieldSerializer = fieldSerializer;
            _valueSerializer = Ensure.IsNotNull(valueSerializer, nameof(valueSerializer));
            _underlyingSerializer = underlyingSerializer;
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

        /// <summary>
        /// Gets the underlying serializer.
        /// </summary>
        public IBsonSerializer UnderlyingSerializer
        {
            get { return _underlyingSerializer; }
        }

        /// <summary>
        /// Gets the value serializer.
        /// </summary>
        public IBsonSerializer<TField> ValueSerializer
        {
            get { return _valueSerializer; }
        }
    }

    /// <summary>
    /// Base class for field names.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public abstract class FieldDefinition<TDocument>
    {
        /// <summary>
        /// Renders the field to a <see cref="RenderedFieldDefinition"/>.
        /// </summary>
        /// <param name="args">The render arguments.</param>
        /// <returns>A <see cref="RenderedFieldDefinition"/>.</returns>
        public abstract RenderedFieldDefinition Render(RenderArgs<TDocument> args);

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
        /// Renders the field to a <see cref="RenderedFieldDefinition{TField}"/>.
        /// </summary>
        /// <param name="args">The render arguments.</param>
        /// <returns>A <see cref="RenderedFieldDefinition{TField}"/>.</returns>
        public abstract RenderedFieldDefinition<TField> Render(RenderArgs<TDocument> args);

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

        /// <summary>
        /// Performs an implicit conversion from <see cref="FieldDefinition{TDocument, TField}"/> to <see cref="FieldDefinition{TDocument}"/>.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator FieldDefinition<TDocument>(FieldDefinition<TDocument, TField> field)
        {
            return new UntypedFieldDefinitionAdapter<TDocument, TField>(field);
        }
    }

    /// <summary>
    /// An <see cref="Expression" /> based field.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class ExpressionFieldDefinition<TDocument> : FieldDefinition<TDocument>
    {
        private readonly LambdaExpression _expression;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionFieldDefinition{TDocument}" /> class.
        /// </summary>
        /// <param name="expression">The expression.</param>
        public ExpressionFieldDefinition(LambdaExpression expression)
        {
            _expression = Ensure.IsNotNull(expression, nameof(expression));

            if (expression.Parameters.Count != 1)
            {
                throw new ArgumentException("Only a single parameter lambda expression is allowed.", "expression");
            }
            if (expression.Parameters[0].Type != typeof(TDocument))
            {
                var message = string.Format("The lambda expression parameter must be of type {0}.", typeof(TDocument));
                throw new ArgumentException(message, "expression");
            }
        }

        /// <summary>
        /// Gets the expression.
        /// </summary>
        public LambdaExpression Expression
        {
            get { return _expression; }
        }

        /// <inheritdoc />
        public override RenderedFieldDefinition Render(RenderArgs<TDocument> args)
        {
            return LinqProviderAdapter.TranslateExpressionToField(_expression, args.DocumentSerializer, args.SerializationDomain, args.TranslationOptions);
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
            _expression = Ensure.IsNotNull(expression, nameof(expression));
        }

        /// <summary>
        /// Gets the expression.
        /// </summary>
        public Expression<Func<TDocument, TField>> Expression
        {
            get { return _expression; }
        }

        /// <inheritdoc />
        public override RenderedFieldDefinition<TField> Render(RenderArgs<TDocument> args)
        {
            return LinqProviderAdapter.TranslateExpressionToField(_expression, args.DocumentSerializer, args.SerializationDomain, args.TranslationOptions, args.PathRenderArgs.AllowScalarValueForArray);
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
            _fieldName = Ensure.IsNotNull(fieldName, nameof(fieldName));
        }

        /// <inheritdoc />
        public override RenderedFieldDefinition Render(RenderArgs<TDocument> args)
        {
            string resolvedName;
            IBsonSerializer resolvedSerializer;
            StringFieldDefinitionHelper.Resolve<TDocument>(_fieldName, args.DocumentSerializer, out resolvedName, out resolvedSerializer);

            return new RenderedFieldDefinition(resolvedName, resolvedSerializer);
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
            _fieldName = Ensure.IsNotNull(fieldName, nameof(fieldName));
            _fieldSerializer = fieldSerializer;
        }

        /// <inheritdoc />
        public override RenderedFieldDefinition<TField> Render(RenderArgs<TDocument> args)
        {
            string resolvedName;
            IBsonSerializer underlyingSerializer;
            StringFieldDefinitionHelper.Resolve<TDocument>(_fieldName, args.DocumentSerializer, out resolvedName, out underlyingSerializer);

            var fieldSerializer = underlyingSerializer as IBsonSerializer<TField>;

            IBsonSerializer<TField> valueSerializer;
            if (_fieldSerializer != null)
            {
                valueSerializer = _fieldSerializer;
            }
            else if (underlyingSerializer != null)
            {
                valueSerializer = (IBsonSerializer<TField>)FieldValueSerializerHelper.GetSerializerForValueType(underlyingSerializer, args.SerializerRegistry, typeof(TField), args.PathRenderArgs.AllowScalarValueForArray);
            }
            else
            {
                valueSerializer = args.SerializerRegistry.GetSerializer<TField>();
            }

            return new RenderedFieldDefinition<TField>(resolvedName, fieldSerializer, valueSerializer, underlyingSerializer);
        }
    }

    internal static class StringFieldDefinitionHelper
    {
        public static void Resolve<TDocument>(string fieldName, IBsonSerializer<TDocument> serializer, out string resolvedFieldName, out IBsonSerializer resolvedFieldSerializer)
        {
            resolvedFieldName = fieldName;
            resolvedFieldSerializer = null;

            var documentSerializer = serializer as IBsonDocumentSerializer;
            if (documentSerializer == null)
            {
                return;
            }

            // shortcut BsonDocumentSerializer since it is so common
            if (serializer.GetType() == typeof(BsonDocumentSerializer))
            {
                return;
            }

            BsonSerializationInfo serializationInfo;

            // first, lets try the quick and easy one, which will be a majority of cases
            if (documentSerializer.TryGetMemberSerializationInfo(fieldName, out serializationInfo))
            {
                resolvedFieldName = serializationInfo.ElementName;
                resolvedFieldSerializer = serializationInfo.Serializer;
                return;
            }

            // now lets go and do the more difficult variant
            var nameParts = fieldName.Split('.');
            if (nameParts.Length <= 1)
            {
                // if we only have 1, then it's no different than what we did above
                // when we found nothing.
                return;
            }

            IBsonArraySerializer arraySerializer;
            resolvedFieldSerializer = documentSerializer;
            for (int i = 0; i < nameParts.Length; i++)
            {
                if (nameParts[i] == "$" || nameParts[i].All(char.IsDigit))
                {
                    arraySerializer = resolvedFieldSerializer as IBsonArraySerializer;
                    if (arraySerializer != null && arraySerializer.TryGetItemSerializationInfo(out serializationInfo))
                    {
                        resolvedFieldSerializer = serializationInfo.Serializer;
                        continue;
                    }

                    resolvedFieldSerializer = null;
                    break;
                }

                documentSerializer = resolvedFieldSerializer as IBsonDocumentSerializer;
                if (documentSerializer == null || !documentSerializer.TryGetMemberSerializationInfo(nameParts[i], out serializationInfo))
                {
                    // need to check if this is an any element array match
                    arraySerializer = resolvedFieldSerializer as IBsonArraySerializer;
                    if (arraySerializer != null && arraySerializer.TryGetItemSerializationInfo(out serializationInfo))
                    {
                        documentSerializer = serializationInfo.Serializer as IBsonDocumentSerializer;
                        if (documentSerializer == null || !documentSerializer.TryGetMemberSerializationInfo(nameParts[i], out serializationInfo))
                        {
                            resolvedFieldSerializer = null;
                            break;
                        }
                    }
                    else
                    {
                        resolvedFieldSerializer = null;
                        break;
                    }
                }

                nameParts[i] = serializationInfo.ElementName;
                resolvedFieldSerializer = serializationInfo.Serializer;
            }

            resolvedFieldName = string.Join(".", nameParts);
        }
    }

    internal sealed class UntypedFieldDefinitionAdapter<TDocument, TField> : FieldDefinition<TDocument>
    {
        private readonly FieldDefinition<TDocument, TField> _adaptee;

        public UntypedFieldDefinitionAdapter(FieldDefinition<TDocument, TField> adaptee)
        {
            _adaptee = Ensure.IsNotNull(adaptee, nameof(adaptee));
        }

        public override RenderedFieldDefinition Render(RenderArgs<TDocument> args)
        {
            var rendered = _adaptee.Render(args);
            return new RenderedFieldDefinition(rendered.FieldName, rendered.UnderlyingSerializer);
        }
    }
}
