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

internal sealed class AstSimilarityFunctionExpression : AstExpression
{
    private readonly AstExpression _vectors1;
    private readonly AstExpression _vectors2;
    private readonly AstExpression _normalize;
    private readonly string _operator;

    public AstSimilarityFunctionExpression(
        string @operator, AstExpression vectors1, AstExpression vectors2, AstExpression normalize)
    {
        _vectors1 = Ensure.IsNotNull(vectors1, nameof(vectors1));
        _vectors2 = Ensure.IsNotNull(vectors2, nameof(vectors2));
        _normalize = Ensure.IsNotNull(normalize, nameof(normalize));
        _operator = @operator;
    }

    public AstExpression Normalize => _normalize;
    public AstExpression Vectors1 => _vectors1;
    public AstExpression Vectors2 => _vectors2;
    public override AstNodeType NodeType => AstNodeType.SimilarityFunctionExpression;

    public override AstNode Accept(AstNodeVisitor visitor)
        => visitor.VisitSimilarityFunctionExpression(this);

    public override BsonValue Render()
        => new BsonDocument(_operator,
            new BsonDocument
            {
                { "vectors", new BsonArray { _vectors1.Render(), _vectors2.Render() } },
                { "score", _normalize.Render() }
            });

    public AstSimilarityFunctionExpression Update(
        AstExpression vectors1,
        AstExpression vectors2,
        AstExpression normalize)
    {
        if (vectors1 == _vectors1 &&
            vectors2 == _vectors2 &&
            normalize == _normalize)
        {
            return this;
        }

        return new AstSimilarityFunctionExpression(_operator, vectors1, vectors2, normalize);
    }
}
