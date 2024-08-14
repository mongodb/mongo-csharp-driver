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

using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// Base class for search paths.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public abstract class SearchPathDefinition<TDocument>
    {
        /// <summary>
        /// Renders the path to a <see cref="BsonValue"/>.
        /// </summary>
        /// <param name="args">The render arguments.</param>
        /// <returns>A <see cref="BsonValue"/>.</returns>
        public abstract BsonValue Render(RenderArgs<TDocument> args);

        /// <summary>
        /// Performs an implicit conversion from <see cref="FieldDefinition{TDocument}"/> to
        /// <see cref="SearchPathDefinition{TDocument}"/>.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator SearchPathDefinition<TDocument>(FieldDefinition<TDocument> field) =>
            new SingleSearchPathDefinition<TDocument>(field);

        /// <summary>
        /// Performs an implicit conversion from a field name to <see cref="SearchPathDefinition{TDocument}"/>.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator SearchPathDefinition<TDocument>(string fieldName) =>
            new SingleSearchPathDefinition<TDocument>(new StringFieldDefinition<TDocument>(fieldName));

        /// <summary>
        /// Performs an implicit conversion from an array of <see cref="FieldDefinition{TDocument}"/> to
        /// <see cref="SearchPathDefinition{TDocument}"/>.
        /// </summary>
        /// <param name="fields">The array of fields.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator SearchPathDefinition<TDocument>(FieldDefinition<TDocument>[] fields) =>
            new MultiSearchPathDefinition<TDocument>(fields);

        /// <summary>
        /// Performs an implicit conversion from a list of <see cref="FieldDefinition{TDocument}"/> to
        /// <see cref="SearchPathDefinition{TDocument}"/>.
        /// </summary>
        /// <param name="fields">The list of fields.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator SearchPathDefinition<TDocument>(List<FieldDefinition<TDocument>> fields) =>
            new MultiSearchPathDefinition<TDocument>(fields);

        /// <summary>
        /// Performs an implicit conversion from an array of field names to 
        /// <see cref="SearchPathDefinition{TDocument}"/>.
        /// </summary>
        /// <param name="fieldNames">The array of field names.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator SearchPathDefinition<TDocument>(string[] fieldNames) =>
            new MultiSearchPathDefinition<TDocument>(fieldNames.Select(fieldName => new StringFieldDefinition<TDocument>(fieldName)));

        /// <summary>
        /// Performs an implicit conversion from an array of field names to 
        /// <see cref="SearchPathDefinition{TDocument}"/>.
        /// </summary>
        /// <param name="fieldNames">The list of field names.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator SearchPathDefinition<TDocument>(List<string> fieldNames) =>
            new MultiSearchPathDefinition<TDocument>(fieldNames.Select(fieldName => new StringFieldDefinition<TDocument>(fieldName)));

        /// <summary>
        /// Renders the field.
        /// </summary>
        /// <param name="fieldDefinition">The field definition.</param>
        /// <param name="args">The render arguments.</param>
        /// <returns>The rendered field.</returns>
        protected string RenderField(FieldDefinition<TDocument> fieldDefinition, RenderArgs<TDocument> args)
        {
            var renderedField = fieldDefinition.Render(args);
            var prefix = args.PathRenderArgs.PathPrefix;

            return prefix == null ? renderedField.FieldName : $"{prefix}.{renderedField.FieldName}";
        }
    }
}
