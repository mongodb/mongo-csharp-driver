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

internal sealed class AstMinMaxScalerWindowExpression : AstWindowExpression
{
    private readonly AstExpression _input;
    private readonly AstExpression _min;
    private readonly AstExpression _max;
    private readonly AstWindow _window;

    public AstMinMaxScalerWindowExpression(AstExpression input, AstExpression min, AstExpression max, AstWindow window)
    {
        _input = Ensure.IsNotNull(input, nameof(input));
        _min = Ensure.IsNotNull(min, nameof(min));
        _max = Ensure.IsNotNull(max, nameof(max));
        _window = window;
    }

    public AstExpression Input => _input;
    public AstExpression MinValue => _min;
    public AstExpression MaxValue => _max;
    public AstWindow Window => _window;

    public override AstNodeType NodeType => AstNodeType.MinMaxScalerWindowExpression;

    public override AstNode Accept(AstNodeVisitor visitor)
    {
        return visitor.VisitMinMaxScalerWindowExpression(this);
    }

    public override BsonValue Render()
    {
        return new BsonDocument
        {
            {
                "$minMaxScaler", new BsonDocument
                {
                    { "input", _input.Render() },
                    { "min", _min.Render() },
                    { "max", _max.Render() }
                }
            },
            { "window", _window?.Render(), _window != null }
        };
    }

    public AstMinMaxScalerWindowExpression Update(AstExpression input, AstExpression min, AstExpression max, AstWindow window)
    {
        if (input == _input && min == _min && max == _max && window == _window)
        {
            return this;
        }

        return new AstMinMaxScalerWindowExpression(input, min, max, window);
    }
}
