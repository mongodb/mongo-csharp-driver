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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions
{
    internal sealed class AstMedianWindowExpression : AstWindowExpression
    {
        private readonly AstExpression _input;
        private readonly AstWindow _window;

        public AstMedianWindowExpression(AstExpression input, AstWindow window)
        {
            _input = Ensure.IsNotNull(input, nameof(input));
            _window = window;
        }

        public AstExpression Input => _input;

        public AstWindow Window => _window;

        public override AstNodeType NodeType => AstNodeType.MedianWindowExpression;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitMedianWindowExpression(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                {
                    "$median", new BsonDocument
                    {
                        { "input", _input.Render() },
                        { "method", "approximate" } // server requires this parameter but currently only allows this value
                    }
                },
                { "window", _window?.Render(), _window != null }
            };
        }

        public AstMedianWindowExpression Update(AstExpression input,  AstWindow window)
        {
            if (input == _input && window == _window)
            {
                return this;
            }

            return new AstMedianWindowExpression(input, window);
        }
    }
}