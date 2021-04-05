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
    public sealed class AstLetExpression : AstExpression
    {
        private readonly AstExpression _in;
        private readonly IReadOnlyList<AstComputedField> _vars;

        public AstLetExpression(
            IEnumerable<AstComputedField> vars,
            AstExpression @in)
        {
            _vars = Ensure.IsNotNull(vars, nameof(vars)).ToList().AsReadOnly();
            _in = Ensure.IsNotNull(@in, nameof(@in));
        }

        public new AstExpression In => _in;
        public override AstNodeType NodeType => AstNodeType.LetExpression;
        public IReadOnlyList<AstComputedField> Vars => _vars;

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$let", new BsonDocument
                    {
                        { "vars", new BsonDocument(_vars.Select(v => v.Render())) },
                        { "in", _in.Render() }
                    }
                }
            };
        }
    }
}
