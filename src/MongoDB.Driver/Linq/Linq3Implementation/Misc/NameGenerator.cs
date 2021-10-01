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

using System.Linq;
using System.Linq.Expressions;

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc
{
    internal class NameGenerator
    {
        private int _parameterCounter;
        private int _varCounter;

        public string GetParameterName(ParameterExpression parameter)
        {
            if (parameter.Name != null)
            {
                return parameter.Name;
            }

            return $"_p{_parameterCounter++}";
        }

        public string GetVarName(string symbolName)
        {
            if (IsValidVarName(symbolName))
            {
                return symbolName;
            }

            return $"_v{_varCounter++}";

            static bool IsValidVarName(string name)
            {
                return
                    name != null &&
                    name.Length >= 1 &&
                    IsValidFirstChar(name[0]) &&
                    name.Skip(1).All(c => IsValidSubsequentChar(c));

                static bool IsValidFirstChar(char c)
                {
                    return c == '_' || IsBetween(c, 'a', 'z') || IsBetween(c, 'A', 'Z');
                }

                static bool IsValidSubsequentChar(char c)
                {
                    return c == '_' || IsBetween(c, 'a', 'z') || IsBetween(c, 'A', 'Z') || IsBetween(c, '0', '9');
                }

                static bool IsBetween(char c, char x, char y)
                {
                    return c >= x && c <= y;
                }
            }
        }
    }
}
