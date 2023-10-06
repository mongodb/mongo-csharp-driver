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

namespace MongoDB.Driver
{
    /// <summary>
    /// A builder for SetFieldDefinitions.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public class SetFieldDefinitionsBuilder<TDocument>
    {
        /// <summary>
        /// Set a field to a value using a constant.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="field">The field.</param>
        /// <param name="value">The value.</param>
        /// <returns>An instance of ListSetFieldDefinitions to which further set field definitions can be added.</returns>
        public ListSetFieldDefinitions<TDocument> Set<TField>(FieldDefinition<TDocument, TField> field, TField value)
        {
            var setFieldsDefinitions = new ListSetFieldDefinitions<TDocument>(Enumerable.Empty<SetFieldDefinition<TDocument>>());
            return setFieldsDefinitions.Set(field, value);
        }

        /// <summary>
        /// Set a field to a value using an expression to specify the field and a constant to specify the value.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="field">The field.</param>
        /// <param name="value">The value.</param>
        /// <returns>An instance of ListSetFieldDefinitions to which further set field definitions can be added.</returns>
        public ListSetFieldDefinitions<TDocument> Set<TField>(Expression<Func<TDocument, TField>> field, TField value)
        {
            var setFieldsDefinitions = new ListSetFieldDefinitions<TDocument>(Enumerable.Empty<SetFieldDefinition<TDocument>>());
            return setFieldsDefinitions.Set(field, value);
        }
    }

    /// <summary>
    /// Extension methods to add additional set field definitions to an existing instance of ListSetFieldDefinitions.
    /// </summary>
    public static class ListSetFieldDefinitionsExtensions
    {
        /// <summary>
        /// Set an additional field to value using a constant.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fields">The existing ListSetFieldDefinitions.</param>
        /// <param name="field">The field.</param>
        /// <param name="value">The value.</param>
        /// <returns>The ListSetFieldDefinitions instance with a SetFieldDefinition added.</returns>
        public static ListSetFieldDefinitions<TDocument> Set<TDocument, TField>(this ListSetFieldDefinitions<TDocument> fields, FieldDefinition<TDocument, TField> field, TField value)
        {
            var setFieldDefinition = new ConstantSetFieldDefinition<TDocument, TField>(field, value);
            return fields.Set(setFieldDefinition);
        }

        /// <summary>
        /// Set an additional field to value using an Expression to specify the field and a constant to specify the value.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="fields">The existing ListSetFieldDefinitions.</param>
        /// <param name="field">The field.</param>
        /// <param name="value">The value.</param>
        /// <returns>The ListSetFieldDefinitions instance with a SetFieldDefinition added.</returns>
        public static ListSetFieldDefinitions<TDocument> Set<TDocument, TField>(this ListSetFieldDefinitions<TDocument> fields, Expression<Func<TDocument, TField>> field, TField value)
        {
            var fieldDefinition = new ExpressionFieldDefinition<TDocument, TField>(field);
            var setFieldDefinition = new ConstantSetFieldDefinition<TDocument, TField>(fieldDefinition, value);
            return fields.Set(setFieldDefinition);
        }

        internal static ListSetFieldDefinitions<TDocument> Set<TDocument>(this ListSetFieldDefinitions<TDocument> fields, SetFieldDefinition<TDocument> setFieldDefinition)
        {
            return new ListSetFieldDefinitions<TDocument>(fields.List.Concat(new[] { setFieldDefinition }));
        }
    }
}
