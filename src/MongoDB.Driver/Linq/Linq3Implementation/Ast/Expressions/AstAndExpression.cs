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

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions
{
    internal sealed class AstAndExpression : AstExpression
    {
        private readonly IReadOnlyList<AstExpression> _args;

        public AstAndExpression(IEnumerable<AstExpression> args)
        {
            _args = Ensure.IsNotNull(args, nameof(args)).ToList().AsReadOnly();
            Ensure.That(_args.Count > 0, "args cannot be empty.", nameof(args));
            Ensure.That(!_args.Contains(null), "args cannot contain null.", nameof(args));
        }

        public IReadOnlyList<AstExpression> Args => _args;
        public override AstNodeType NodeType => AstNodeType.AndExpression;

        public override BsonValue Render()
        {
            return new BsonDocument("$and", new BsonArray(_args.Select(a => a.Render())));
        }
    }
}
