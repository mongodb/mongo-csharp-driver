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

using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors
{
    internal sealed class AstNodeReplacer : AstNodeVisitor
    {
        #region static
        public static AstNode Replace(AstNode node, params (AstNode Original, AstNode Replacement)[] mappings)
        {
            var replacer = new AstNodeReplacer(mappings);
            return replacer.Visit(node);
        }
        #endregion

        private readonly (AstNode Original, AstNode Replacement)[] _mappings;

        public AstNodeReplacer((AstNode Original, AstNode Replacement)[] mappings)
        {
            _mappings = Ensure.IsNotNull(mappings, nameof(mappings));
        }

        public override AstNode Visit(AstNode node)
        {
            if (TryFindReplacement(_mappings, node, out var replacement))
            {
                return replacement;
            }

            return base.Visit(node);

            static bool TryFindReplacement((AstNode Original, AstNode Replacement)[] mappings, AstNode original, out AstNode replacement)
            {
                // because the number of mappings is expected to be small a linear search is good enough
                foreach (var mapping in mappings)
                {
                    if (mapping.Original == original)
                    {
                        replacement = mapping.Replacement;
                        return true;
                    }
                }

                replacement = null;
                return false;
            }
        }
    }
}
