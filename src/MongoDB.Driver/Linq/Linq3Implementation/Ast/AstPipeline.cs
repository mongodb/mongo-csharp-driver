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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast
{
    internal sealed class AstPipeline : AstNode
    {
        #region static
        private static readonly AstPipeline __empty = new AstPipeline([]);

        public static AstPipeline Empty => __empty;
        #endregion

        private readonly IReadOnlyList<AstStage> _stages;

        public AstPipeline(IEnumerable<AstStage> stages)
        {
            _stages = Ensure.IsNotNull(stages, nameof(stages)).AsReadOnlyList();
        }

        public override AstNodeType NodeType => AstNodeType.Pipeline;
        public IReadOnlyList<AstStage> Stages => _stages;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitPipeline(this);
        }

       public override BsonValue Render()
        {
            return new BsonArray(_stages.Select(s => s.Render()));
        }

        public AstPipeline Update(IEnumerable<AstStage> stages)
        {
            if (stages == _stages)
            {
                return this;
            }

            return new AstPipeline(stages);
        }
    }
}
