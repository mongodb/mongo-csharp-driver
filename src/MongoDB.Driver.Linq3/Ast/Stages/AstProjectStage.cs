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
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Driver.Linq3.Ast.Stages
{
    public abstract class AstProjectStageSpecification
    {
        public abstract BsonElement Render();
    }

    public sealed class AstProjectStageComputedFieldSpecification : AstProjectStageSpecification
    {
        private readonly AstComputedField _field;

        public AstProjectStageComputedFieldSpecification(AstComputedField field)
        {
            _field = Ensure.IsNotNull(field, nameof(field));
        }

        public override BsonElement Render()
        {
            var rendered = _field.Render();
            switch (rendered.Value.BsonType)
            {
                case BsonType.Boolean:
                case BsonType.Int32:
                case BsonType.Int64:
                case BsonType.Double:
                    rendered = new BsonElement(rendered.Name, new BsonDocument("$literal", rendered.Value));
                    break;
            }
            return rendered;
        }
    }

    public sealed class AstProjectStageExcludeFieldSpecification : AstProjectStageSpecification
    {
        private readonly string _field;

        public AstProjectStageExcludeFieldSpecification(string field)
        {
            _field = Ensure.IsNotNullOrEmpty(field, nameof(field));
        }

        public string Field => _field;

        public override BsonElement Render()
        {
            return new BsonElement(_field, 0);
        }
    }

    public sealed class AstProjectStageExcludeIdSpecification : AstProjectStageSpecification
    {
        public override BsonElement Render()
        {
            return new BsonElement("_id", 0);
        }
    }

    public sealed class AstProjectStageIncludeFieldSpecification : AstProjectStageSpecification
    {
        private readonly string _field;

        public AstProjectStageIncludeFieldSpecification(string field)
        {
            _field = Ensure.IsNotNullOrEmpty(field, nameof(field));
        }

        public string Field => _field;

        public override BsonElement Render()
        {
            return new BsonElement(_field, 1);
        }
    }

    public sealed class AstProjectStage : AstPipelineStage
    {
        private readonly IReadOnlyList<AstProjectStageSpecification> _specifications;

        public AstProjectStage(IEnumerable<AstProjectStageSpecification> specifications)
        {
            _specifications = Ensure.IsNotNull(specifications, nameof(specifications)).ToList().AsReadOnly();
        }

        public AstProjectStage(params AstProjectStageSpecification[] specifications)
            : this((IEnumerable<AstProjectStageSpecification>)specifications)
        {
        }

        public override AstNodeType NodeType => AstNodeType.ProjectStage;
        public IReadOnlyList<AstProjectStageSpecification> Specifications => _specifications;

        public override BsonValue Render()
        {
            return new BsonDocument("$project", new BsonDocument(_specifications.Select(s => s.Render())));
        }
    }
}
