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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq3.Ast.Filters
{
    public sealed class AstOrFilter : AstFilter
    {
        private readonly AstFilter[] _args;

        public AstOrFilter(params AstFilter[] args)
        {
            Ensure.IsNotNull(args, nameof(args));
            Ensure.That(args.Length > 0, "Args length cannot be zero.", nameof(args));
            _args = args;
        }

        public AstFilter[] Args => _args;
        public override AstNodeType NodeType => AstNodeType.OrFilter;

        public override BsonValue Render()
        {
            return new BsonDocument("$or", new BsonArray(_args.Select(a => a.Render())));
        }
    }
}
