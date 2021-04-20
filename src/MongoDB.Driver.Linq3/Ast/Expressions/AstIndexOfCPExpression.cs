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

namespace MongoDB.Driver.Linq3.Ast.Expressions
{
    internal sealed class AstIndexOfCPExpression : AstExpression
    {
        private readonly AstExpression _end;
        private readonly AstExpression _start;
        private readonly AstExpression _string;
        private readonly AstExpression _value;

        public AstIndexOfCPExpression(
            AstExpression @string,
            AstExpression value,
            AstExpression start = null,
            AstExpression end = null)
        {
            _string = Ensure.IsNotNull(@string, nameof(@string));
            _value = Ensure.IsNotNull(value, nameof(value));
            _start = start;
            _end = Ensure.That(end, _ => end == null || start != null, nameof(end), "If end is specified then start must be specified also.");
        }

        public AstExpression End => _end;
        public override AstNodeType NodeType => AstNodeType.IndexOfCPExpression;
        public AstExpression Start => _start;
        public AstExpression String => _string;
        public AstExpression Value => _value;

        public override BsonValue Render()
        {
            var args = new BsonArray { _string.Render(), _value.Render() };
            if (_start != null)
            {
                args.Add(_start.Render());
                if (_end != null)
                {
                    args.Add(_end.Render());
                }
            }

            return new BsonDocument("$indexOfCP", args);
        }
    }
}
