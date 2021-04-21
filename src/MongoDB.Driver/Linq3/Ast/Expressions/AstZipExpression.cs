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
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Driver.Linq3.Ast.Expressions
{
    internal sealed class AstZipExpression : AstExpression
    {
        private readonly AstExpression _defaults;
        private readonly IReadOnlyList<AstExpression> _inputs;
        private readonly bool? _useLongestLength;

        public AstZipExpression(
            IEnumerable<AstExpression> inputs,
            bool? useLongestLength = null,
            AstExpression defaults = null)
        {
            _inputs = Ensure.IsNotNull(inputs, nameof(inputs)).ToList().AsReadOnly();
            _useLongestLength = useLongestLength;
            _defaults = defaults;
        }

        public AstExpression Defaults => _defaults;
        public IReadOnlyList<AstExpression> Inputs => _inputs;
        public override AstNodeType NodeType => AstNodeType.ZipExpression;
        public bool? UseLongestLength => _useLongestLength;

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$zip", new BsonDocument
                    {
                        { "inputs", new BsonArray(_inputs.Select(i => i.Render())) },
                        { "useLongestLength", () => _useLongestLength.Value, _useLongestLength.HasValue },
                        { "defaults", () => _defaults.Render(), _defaults != null }
                    }
                }
            };
        }
    }
}
