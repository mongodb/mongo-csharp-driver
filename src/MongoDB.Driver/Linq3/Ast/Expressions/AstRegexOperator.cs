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

namespace MongoDB.Driver.Linq3.Ast.Expressions
{
    internal enum AstRegexOperator
    {
        Find,
        FindAll,
        Match
    }

    internal static class AstRegexOperatorExtensions
    {
        public static string Render(this AstRegexOperator @operator)
        {
            switch (@operator)
            {
                case AstRegexOperator.Find: return "$regexFind";
                case AstRegexOperator.FindAll: return "$regexFindAll";
                case AstRegexOperator.Match: return "$regexMatch";
                default: throw new InvalidOperationException($"Unexpected regex operator: {@operator}.");
            }
        }
    }
}
