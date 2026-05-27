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

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;

internal sealed class AstEncStrExpression : AstExpression
{
    public AstEncStrExpression(AstEncStrOperator @operator, AstExpression input, AstExpression value)
    {
        Operator = @operator;
        Input = Ensure.IsNotNull(input, nameof(input));
        Value = Ensure.IsNotNull(value, nameof(value));
    }

    public AstEncStrOperator Operator { get; }
    public AstExpression Input { get; }
    public AstExpression Value { get; }

    public override AstNodeType NodeType => AstNodeType.EncStrExpression;

    public override AstNode Accept(AstNodeVisitor visitor)
    {
        return visitor.VisitEncStrExpression(this);
    }

    public override BsonValue Render()
    {
        return new BsonDocument
        {
            { Operator.Render(), new BsonDocument
                {
                    { "input", Input.Render() },
                    { Operator.ValueArgName(), Value.Render() }
                }
            }
        };
    }

    public AstEncStrExpression Update(AstExpression input, AstExpression value)
    {
        if (Input == input && Value == value)
        {
            return this;
        }

        return new AstEncStrExpression(Operator, input, value);
    }
}
