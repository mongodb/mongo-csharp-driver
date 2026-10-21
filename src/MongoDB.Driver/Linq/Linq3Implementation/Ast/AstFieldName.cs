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

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast
{
    internal static class AstFieldName
    {
        // a field name is emitted into the pipeline verbatim and cannot be quoted, so it is only safe when the
        // server treats it as an ordinary field name. a name that starts with "$" is interpreted as an operator
        // when it is the first element name of a document, a name that contains "." is interpreted as a path to
        // a nested field, and an empty name is rejected outright.
        public static bool IsSafe(string name)
        {
            return name != null && name.Length > 0 && name[0] != '$' && name.IndexOf('.') < 0;
        }

        // an array filter identifier is emitted into a "$[identifier]" path segment, so a name containing "]" or "."
        // would restructure the segment and a null name would silently turn it into "$[]" (every element). the server
        // requires an identifier to be alphanumeric and to begin with a lowercase letter, so anything this rejects
        // the server would reject too.
        public static bool IsSafeArrayFilterIdentifier(string identifier)
        {
            if (identifier == null || identifier.Length == 0 || identifier[0] < 'a' || identifier[0] > 'z')
            {
                return false;
            }

            foreach (var c in identifier)
            {
                if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
