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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages
{
    internal static class AstProject
    {
        public static AstProjectStageSpecification Exclude(string path)
        {
            return new AstProjectStageExcludeFieldSpecification(path);
        }

        public static AstProjectStageSpecification ExcludeId()
        {
            return new AstProjectStageExcludeFieldSpecification("_id");
        }

        public static AstProjectStageSpecification Include(string path)
        {
            return new AstProjectStageIncludeFieldSpecification(path);
        }

        public static AstProjectStageSpecification Set(string path, AstExpression value)
        {
            return new AstProjectStageSetFieldSpecification(path, value);
        }
    }

    internal abstract class AstProjectStageSpecification : AstNode
    {
        public override BsonValue Render()
        {
            return new BsonDocument(RenderAsElement());
        }

        public abstract BsonElement RenderAsElement();
    }

    internal sealed class AstProjectStageExcludeFieldSpecification : AstProjectStageSpecification
    {
        private readonly string _path;

        public AstProjectStageExcludeFieldSpecification(string path)
        {
            _path = Ensure.IsNotNullOrEmpty(path, nameof(path));
        }

        public override AstNodeType NodeType => AstNodeType.ProjectStageExcludeFieldSpecification;
        public string Path => _path;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitProjectStageExcludeFieldSpecification(this);
        }

        public override BsonElement RenderAsElement()
        {
            return new BsonElement(_path, 0);
        }
    }

    internal sealed class AstProjectStageIncludeFieldSpecification : AstProjectStageSpecification
    {
        private readonly string _path;

        public AstProjectStageIncludeFieldSpecification(string path)
        {
            _path = Ensure.IsNotNullOrEmpty(path, nameof(path));
        }

        public override AstNodeType NodeType => AstNodeType.ProjectStageIncludeFieldSpecification;
        public string Path => _path;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitProjectStageIncludeFieldSpecification(this);
        }

        public override BsonElement RenderAsElement()
        {
            return new BsonElement(_path, 1);
        }
    }

    internal sealed class AstProjectStageSetFieldSpecification : AstProjectStageSpecification
    {
        private readonly string _path;
        private readonly AstExpression _value;

        public AstProjectStageSetFieldSpecification(string path, AstExpression value)
        {
            _path = Ensure.IsNotNull(path, nameof(path));
            _value = Ensure.IsNotNull(value, nameof(value));
        }

        public override AstNodeType NodeType => AstNodeType.ProjectStageSetFieldSpecification;
        public string Path => _path;
        public AstExpression Value => _value;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitProjectStageSetFieldSpecification(this);
        }

        public override BsonElement RenderAsElement()
        {
            return new BsonElement(_path, _value.Render());
        }

        public AstProjectStageSetFieldSpecification Update(AstExpression value)
        {
            if (value == _value)
            {
                return this;
            }

            return new AstProjectStageSetFieldSpecification(_path, value);
        }
    }

    internal sealed class AstProjectStage : AstStage
    {
        private readonly IReadOnlyList<AstProjectStageSpecification> _specifications;

        public AstProjectStage(IEnumerable<AstProjectStageSpecification> specifications)
        {
            _specifications = Ensure.IsNotNull(specifications, nameof(specifications)).AsReadOnlyList();
        }

        public override AstNodeType NodeType => AstNodeType.ProjectStage;
        public IReadOnlyList<AstProjectStageSpecification> Specifications => _specifications;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitProjectStage(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument("$project", new BsonDocument(_specifications.Select(s => s.RenderAsElement())));
        }

        public AstProjectStage Update(IEnumerable<AstProjectStageSpecification> specifications)
        {
            if (specifications == _specifications)
            {
                return this;
            }

            if (specifications.SequenceEqual(_specifications, ReferenceEqualityComparer<AstProjectStageSpecification>.Instance))
            {
                return this;
            }

            return new AstProjectStage(specifications);
        }
    }
}
