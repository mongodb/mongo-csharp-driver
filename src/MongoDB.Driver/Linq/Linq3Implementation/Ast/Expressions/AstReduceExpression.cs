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
    internal sealed class AstReduceExpression : AstExpression
    {
        private readonly AstVarExpression _arrayIndexAs;
        private readonly AstVarExpression _as;
        private readonly AstExpression _in;
        private readonly AstExpression _initialValue;
        private readonly AstExpression _input;
        private readonly AstVarExpression _valueAs;

        public AstReduceExpression(
            AstExpression input,
            AstExpression initialValue,
            AstExpression @in,
            AstVarExpression @as = null,
            AstVarExpression arrayIndexAs = null,
            AstVarExpression valueAs = null)
        {
            _input = Ensure.IsNotNull(input, nameof(input));
            _initialValue = Ensure.IsNotNull(initialValue, nameof(initialValue));
            _in = Ensure.IsNotNull(@in, nameof(@in));
            _as = @as;
            _arrayIndexAs = arrayIndexAs;
            _valueAs = valueAs;
        }

        public AstVarExpression ArrayIndexAs => _arrayIndexAs;
        public AstVarExpression As => _as;
        public new AstExpression In => _in;
        public AstExpression InitialValue => _initialValue;
        public AstExpression Input => _input;
        public override AstNodeType NodeType => AstNodeType.ReduceExpression;
        public AstVarExpression ValueAs => _valueAs;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitReduceExpression(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$reduce", new BsonDocument
                    {
                        { "input", _input.Render() },
                        { "initialValue", _initialValue.Render() },
                        { "in", _in.Render() },
                        { "as", _as?.Name, _as != null },
                        { "arrayIndexAs", _arrayIndexAs?.Name, _arrayIndexAs != null },
                        { "valueAs", _valueAs?.Name, _valueAs != null }
                    }
                }
            };
        }

        public AstReduceExpression Update(
            AstExpression input,
            AstExpression initialValue,
            AstExpression @in,
            AstVarExpression @as = null,
            AstVarExpression arrayIndexAs = null,
            AstVarExpression valueAs = null)
        {
            if (input == _input && initialValue == _initialValue && @in == _in && @as == _as && arrayIndexAs == _arrayIndexAs && valueAs == _valueAs)
            {
                return this;
            }

            return new AstReduceExpression(input, initialValue, @in, @as, arrayIndexAs, valueAs);
        }
    }
}
