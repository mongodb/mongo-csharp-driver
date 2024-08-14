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

using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// A definition of a single field to be set.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public abstract class SetFieldDefinition<TDocument>
    {
        /// <summary>
        /// Renders the SetFieldDefinition.
        /// </summary>
        /// <param name="args">The render arguments.</param>
        /// <returns>The rendered SetFieldDefinition.</returns>
        public abstract BsonElement Render(RenderArgs<TDocument> args);
    }

    /// <summary>
    /// A SetFieldDefinition that uses a field and a a constant to define the field to be set.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TField">The type of the field.</typeparam>
    public sealed class ConstantSetFieldDefinition<TDocument, TField> : SetFieldDefinition<TDocument>
    {
        // private fields
        private readonly FieldDefinition<TDocument, TField> _field;
        private readonly TField _value;

        // public constructors
        /// <summary>
        /// Initializes an instance of ConstantSetFieldDefinition.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <param name="value">The value.</param>
        public ConstantSetFieldDefinition(FieldDefinition<TDocument, TField> field, TField value)
        {
            _field = Ensure.IsNotNull(field, nameof(field));
            _value = value;
        }

        // public methods
        /// <inheritdoc/>
        public override BsonElement Render(RenderArgs<TDocument> args)
        {
            var renderedField = _field.Render(args);
            var serializedValue = SerializationHelper.SerializeValue(renderedField.ValueSerializer, _value);

            return new BsonElement(renderedField.FieldName, serializedValue);
        }
    }
}
