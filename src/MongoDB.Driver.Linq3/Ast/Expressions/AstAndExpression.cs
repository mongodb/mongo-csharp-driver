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
    public sealed class AstAndExpression : AstExpression
    {
        private readonly IReadOnlyList<AstExpression> _args;

        public AstAndExpression(IEnumerable<AstExpression> args)
        {
            _args = Ensure.IsNotNull(args, nameof(args)).ToList().AsReadOnly();
        }

        public AstAndExpression(params AstExpression[] args)
            : this((IEnumerable<AstExpression>)args)
        {
        }

        public IReadOnlyList<AstExpression> Args => _args;
        public override AstNodeType NodeType => AstNodeType.AndExpression;

        public override BsonValue Render()
        {
            var flattenedArgs = new List<BsonValue>();

            foreach (var arg in _args)
            {
                var renderedArg = arg.Render();
                if (renderedArg is BsonDocument document && document.ElementCount == 1 && document.GetElement(0).Name == "$and")
                {
                    foreach (BsonDocument flattenedArg in document[0].AsBsonArray)
                    {
                        flattenedArgs.Add(flattenedArg);
                    }
                }
                else
                {
                    flattenedArgs.Add(renderedArg);
                }
            }

            return new BsonDocument("$and", new BsonArray(flattenedArgs));
        }
    }
}
