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
using System.Linq.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators
{
    // thrown instead of ExpressionNotSupportedException so that the translators that catch the latter and retry with
    // another strategy (aggregation operators, regex, client-side projection) cannot turn a rejected name into a
    // query that silently targets something else
    internal sealed class UnsafeFilterFieldNameException : NotSupportedException
    {
        public UnsafeFilterFieldNameException(Expression expression, string because)
            : base($"Expression not supported: {expression} because {because}.")
        {
        }
    }

    internal static class FilterFieldNameValidator
    {
        // a segment taken from application data (a dictionary or BsonDocument key, an Mql.Field name) is joined onto
        // the filter field path with "." and emitted as an element name, which cannot be quoted. only the names the
        // server would reinterpret are rejected, everything else renders as it always has:
        // - a name containing "." is a path to a nested field, not an element with that name
        // - a "$" prefixed name is read as an operator when it is the first path component
        // - "$", "$[]" and "$[identifier]" are positional operators in an update path
        // - "@<current>" and "@<elem>" are the markers of the filter AST, so as the first path component they would
        //   be read as the implied document or element instead of an element with that name
        // any other nested "$" prefixed name is matched and written literally by the server
        public static void EnsureSegmentIsSafe(Expression expression, TranslatedFilterField parent, string segment)
        {
            if (segment == null)
            {
                throw new UnsafeFilterFieldNameException(expression, "the field name is null");
            }

            if (segment.IndexOf('.') >= 0)
            {
                throw new UnsafeFilterFieldNameException(expression, $"the name \"{segment}\" contains \".\", which the server would read as a path to a nested field; access each level separately instead, for example x[\"a\"][\"b\"]");
            }

            if (parent.Ast.IsImpliedDocument && AstFilterField.IsImpliedDocumentMarker(segment))
            {
                throw new UnsafeFilterFieldNameException(expression, $"the name \"{segment}\" is reserved by the driver as an internal path marker and would target the wrong field");
            }

            if (segment.Length > 0 && segment[0] == '$')
            {
                if (parent.Ast.IsImpliedDocument)
                {
                    throw new UnsafeFilterFieldNameException(expression, $"the name \"{segment}\" starts with \"$\" and would be read by the server as an operator");
                }

                if (segment == "$" || (segment.StartsWith("$[", StringComparison.Ordinal) && segment.EndsWith("]", StringComparison.Ordinal)))
                {
                    throw new UnsafeFilterFieldNameException(expression, $"the name \"{segment}\" would be read by the server as a positional operator");
                }
            }
        }
    }
}
