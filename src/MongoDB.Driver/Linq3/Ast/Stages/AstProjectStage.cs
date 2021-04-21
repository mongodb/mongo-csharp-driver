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
using MongoDB.Driver.Linq3.Ast.Expressions;
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Driver.Linq3.Ast.Stages
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

    internal abstract class AstProjectStageSpecification
    {
        public abstract BsonElement Render();
    }

    internal sealed class AstProjectStageExcludeFieldSpecification : AstProjectStageSpecification
    {
        private readonly string _path;

        public AstProjectStageExcludeFieldSpecification(string path)
        {
            _path = Ensure.IsNotNullOrEmpty(path, nameof(path));
        }

        public string Path => _path;

        public override BsonElement Render()
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

        public string Path => _path;

        public override BsonElement Render()
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

        public override BsonElement Render()
        {
            return new BsonElement(_path, _value.Render());
        }
    }

    internal sealed class AstProjectStage : AstStage
    {
        private readonly IReadOnlyList<AstProjectStageSpecification> _specifications;

        public AstProjectStage(IEnumerable<AstProjectStageSpecification> specifications)
        {
            _specifications = Ensure.IsNotNull(specifications, nameof(specifications)).ToList().AsReadOnly();
        }

        public override AstNodeType NodeType => AstNodeType.ProjectStage;
        public IReadOnlyList<AstProjectStageSpecification> Specifications => _specifications;

        public override BsonValue Render()
        {
            return new BsonDocument("$project", new BsonDocument(_specifications.Select(s => s.Render())));
        }
    }
}
