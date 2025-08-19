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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq;

namespace MongoDB.Driver
{
    /// <summary>
    /// Defines the fields to be set by a $set stage.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public abstract class SetFieldDefinitions<TDocument>
    {
        /// <summary>
        /// Renders the set field definitions.
        /// </summary>
        /// <param name="args">The render arguments.</param>
        /// <returns>The rendered set field definitions.</returns>
        public abstract BsonDocument Render(RenderArgs<TDocument> args);
    }

    /// <summary>
    /// A subclass of SetFieldDefinitions containing a list of SetFieldDefinition instances to define the fields to be set.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class ListSetFieldDefinitions<TDocument> : SetFieldDefinitions<TDocument>
    {
        private readonly IReadOnlyList<SetFieldDefinition<TDocument>> _list;

        /// <summary>
        /// Initializes an instances ListSetFieldDefinitions.
        /// </summary>
        /// <param name="setFieldDefinitions">The set field definitions.</param>
        public ListSetFieldDefinitions(IEnumerable<SetFieldDefinition<TDocument>> setFieldDefinitions)
        {
            _list = Ensure.IsNotNull(setFieldDefinitions, nameof(setFieldDefinitions)).ToList();
        }

        /// <summary>
        /// Gets the list of SetFieldDefinition instances.
        /// </summary>
        public IReadOnlyList<SetFieldDefinition<TDocument>> List => _list;

        /// <inheritdoc/>
        public override BsonDocument Render(RenderArgs<TDocument> args)
        {
            var document = new BsonDocument();
            foreach (var setFieldDefinition in _list)
            {
                var renderedSetFieldDefinition = setFieldDefinition.Render(args);
                document[renderedSetFieldDefinition.Name] = renderedSetFieldDefinition.Value; // if same element name is set more than once last one wins
            }
            return document;
        }
    }

    /// <summary>
    /// A subclass of SetFieldDefinition that uses an Expression to define the fields to be set.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TFields">The type of object specifying the fields to set.</typeparam>
    public sealed class ExpressionSetFieldDefinitions<TDocument, TFields> : SetFieldDefinitions<TDocument>
    {
        private Expression<Func<TDocument, TFields>> _expression;

        /// <summary>
        /// Initializes an instance of ExpressionSetFieldDefinitions.
        /// </summary>
        /// <param name="expression">The expression.</param>
        public ExpressionSetFieldDefinitions(Expression<Func<TDocument, TFields>> expression)
        {
            _expression = Ensure.IsNotNull(expression, nameof(expression));
        }

        /// <inheritdoc/>
        public override BsonDocument Render(RenderArgs<TDocument> args)
        {
            var stage = LinqProviderAdapter.TranslateExpressionToSetStage(_expression, args.DocumentSerializer, args.SerializationDomain, args.TranslationOptions);
            return stage["$set"].AsBsonDocument;
        }
    }
}
