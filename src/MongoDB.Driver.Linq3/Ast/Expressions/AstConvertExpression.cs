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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq3.Ast.Expressions
{
    internal sealed class AstConvertExpression : AstExpression
    {
        private readonly AstExpression _input;
        private readonly AstExpression _onError;
        private readonly AstExpression _onNull;
        private readonly AstExpression _to;

        public AstConvertExpression(
            AstExpression input,
            AstExpression to,
            AstExpression onError = null,
            AstExpression onNull = null)
        {
            _input = Ensure.IsNotNull(input, nameof(input));
            _to = Ensure.IsNotNull(to, nameof(to));
            _onError = onError;
            _onNull = onNull;
        }

        public AstExpression Input => _input;
        public override AstNodeType NodeType => AstNodeType.ConvertExpression;
        public AstExpression OnError => _onError;
        public AstExpression OnNull => _onNull;
        public AstExpression To => _to;

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$convert", new BsonDocument
                    {
                        { "input", _input.Render() },
                        { "to", _to.Render() },
                        { "onError", () => _onError.Render(), _onError != null },
                        { "onNull", () => _onNull.Render(), _onNull != null }
                    }
                }
            };
        }
    }
}
