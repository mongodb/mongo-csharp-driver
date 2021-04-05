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
    public sealed class AstReduceExpression : AstExpression
    {
        private readonly AstExpression _in;
        private readonly AstExpression _initialValue;
        private readonly AstExpression _input;

        public AstReduceExpression(
            AstExpression input,
            AstExpression initialValue,
            AstExpression @in)
        {
            _input = Ensure.IsNotNull(input, nameof(input));
            _initialValue = Ensure.IsNotNull(initialValue, nameof(initialValue));
            _in = Ensure.IsNotNull(@in, nameof(@in));
        }

        public new AstExpression In => _in;
        public AstExpression InitialValue => _initialValue;
        public AstExpression Input => _input;
        public override AstNodeType NodeType => AstNodeType.ReduceExpression;

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$reduce", new BsonDocument
                    {
                        { "input", _input.Render() },
                        { "initialValue", _initialValue.Render() },
                        { "in", _in.Render() }
                    }
                }
            };
        }
    }
}
