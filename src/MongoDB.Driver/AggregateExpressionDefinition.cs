﻿/* Copyright 2016-present MongoDB Inc.
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
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Translators;

namespace MongoDB.Driver
{
    /// <summary>
    /// An aggregation expression.
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public abstract class AggregateExpressionDefinition<TSource, TResult>
    {
        #region static
        // public implicit conversions
        /// <summary>
        /// Performs an implicit conversion from <see cref="BsonValue"/> to <see cref="AggregateExpressionDefinition{TSource, TResult}"/>.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator AggregateExpressionDefinition<TSource, TResult>(BsonValue expression)
        {
            Ensure.IsNotNull(expression, nameof(expression));
            return new BsonValueAggregateExpressionDefinition<TSource, TResult>(expression);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String"/> to <see cref="AggregateExpressionDefinition{TSource, TResult}"/>.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator AggregateExpressionDefinition<TSource, TResult>(string expression)
        {
            Ensure.IsNotNullOrEmpty(expression, nameof(expression));
            if (expression[0] == '{')
            {
                return new BsonValueAggregateExpressionDefinition<TSource, TResult>(BsonDocument.Parse(expression));
            }
            else
            {
                return new BsonValueAggregateExpressionDefinition<TSource, TResult>(new BsonString(expression));
            }
        }
        #endregion

        /// <summary>
        /// Renders the aggregation expression.
        /// </summary>
        /// <param name="sourceSerializer">The source serializer.</param>
        /// <param name="serializerRegistry">The serializer registry.</param>
        /// <param name="linqProvider">The LINQ provider.</param>
        /// <returns>The rendered aggregation expression.</returns>
        public abstract BsonValue Render(IBsonSerializer<TSource> sourceSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider);
    }

    /// <summary>
    /// A <see cref="BsonValue"/> based aggregate expression.
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <seealso cref="MongoDB.Driver.AggregateExpressionDefinition{TSource, TResult}" />
    public sealed class BsonValueAggregateExpressionDefinition<TSource, TResult> : AggregateExpressionDefinition<TSource, TResult>
    {
        // private fields
        private readonly BsonValue _expression;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BsonValueAggregateExpressionDefinition{TSource, TResult}"/> class.
        /// </summary>
        /// <param name="expression">The expression.</param>
        public BsonValueAggregateExpressionDefinition(BsonValue expression)
        {
            _expression = Ensure.IsNotNull(expression, nameof(expression));
        }

        // public methods
        /// <inheritdoc/>
        public override BsonValue Render(IBsonSerializer<TSource> sourceSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider)
        {
            return _expression;
        }
    }

    /// <summary>
    /// A <see cref="BsonValue"/> based aggregate expression.
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <seealso cref="MongoDB.Driver.AggregateExpressionDefinition{TSource, TResult}" />
    public sealed class ExpressionAggregateExpressionDefinition<TSource, TResult> : AggregateExpressionDefinition<TSource, TResult>
    {
        // private fields
        private readonly TranslationContextData _contextData;
        private readonly Expression<Func<TSource, TResult>> _expression;
        private readonly ExpressionTranslationOptions _translationOptions;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionAggregateExpressionDefinition{TSource, TResult}" /> class.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="translationOptions">The translation options.</param>
        public ExpressionAggregateExpressionDefinition(Expression<Func<TSource, TResult>> expression, ExpressionTranslationOptions translationOptions)
            : this(expression, translationOptions, null)
        {
        }

        internal ExpressionAggregateExpressionDefinition(
            Expression<Func<TSource, TResult>> expression,
            ExpressionTranslationOptions translationOptions,
            TranslationContextData contextData)
        {
            _expression = Ensure.IsNotNull(expression, nameof(expression));
            _translationOptions = translationOptions; // can be null
            _contextData = contextData; // can be null
        }

        // public methods
        /// <inheritdoc/>
        public override BsonValue Render(IBsonSerializer<TSource> sourceSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider)
        {
            var contextData = _contextData?.With("SerializerRegistry", serializerRegistry);
            return linqProvider.GetAdapter().TranslateExpressionToAggregateExpression(_expression, sourceSerializer, serializerRegistry, _translationOptions, contextData);
        }
    }

    /// <summary>
    /// An aggregate expression for the $documents stage.
    /// </summary>
    /// <typeparam name="TDocument">The type of the documents.</typeparam>
    /// <seealso cref="MongoDB.Driver.AggregateExpressionDefinition{TSource, TResult}" />
    public sealed class DocumentsAggregateExpressionDefinition<TDocument> : AggregateExpressionDefinition<NoPipelineInput, IEnumerable<TDocument>>
    {
        // private fields
        private readonly IReadOnlyList<TDocument> _documents;
        private readonly IBsonSerializer<TDocument> _documentSerializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionAggregateExpressionDefinition{TSource, TResult}" /> class.
        /// </summary>
        /// <param name="documents">The documents.</param>
        /// <param name="documentSerializer">The document serializer.</param>
        public DocumentsAggregateExpressionDefinition(
            IEnumerable<TDocument> documents,
            IBsonSerializer<TDocument> documentSerializer = null)
        {
            _documents = Ensure.IsNotNull(documents, nameof(documents)).AsReadOnlyList();
            _documentSerializer = documentSerializer; // can be null
        }

        // public methods
        /// <inheritdoc/>
        public override BsonValue Render(IBsonSerializer<NoPipelineInput> sourceSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider)
        {
            var documentSerializer = _documentSerializer ?? serializerRegistry.GetSerializer<TDocument>();
            return SerializationHelper.SerializeValues(documentSerializer, _documents);
        }
    }
}
