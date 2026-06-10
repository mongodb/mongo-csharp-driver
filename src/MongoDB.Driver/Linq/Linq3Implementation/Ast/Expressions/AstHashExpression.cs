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

internal class AstHashExpression : AstExpression
{
    public AstHashExpression(AstExpression value, MqlHashAlgorithm algorithm)
    {
        Value = Ensure.IsNotNull(value, nameof(value));
        Algorithm = algorithm;
    }

    public AstExpression Value { get; }
    public MqlHashAlgorithm Algorithm { get; }

    public override AstNodeType NodeType => AstNodeType.HashExpression;

    public override AstNode Accept(AstNodeVisitor visitor)
    {
        return visitor.VisitHashExpression(this);
    }

    public override BsonValue Render()
    {
        return new BsonDocument
        {
            { "$hash", new BsonDocument
                {
                    { "input", Value.Render() },
                    { "algorithm", Algorithm.ToAlgorithmName() }
                }
            }
        };
    }

    public AstHashExpression Update(AstExpression value)
    {
        if (Value == value)
        {
            return this;
        }

        return new AstHashExpression(value, Algorithm);
    }
}

