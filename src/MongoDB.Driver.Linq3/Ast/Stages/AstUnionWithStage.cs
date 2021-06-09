﻿/* Copyright 2010-present MongoDB Inc.
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

namespace MongoDB.Driver.Linq3.Ast.Stages
{
    public sealed class AstUnionWithStage : AstPipelineStage
    {
        private readonly string _collection;
        private readonly AstPipeline _pipeline;

        public AstUnionWithStage(string collection, AstPipeline pipeline)
        {
            _collection = Ensure.IsNotNull(collection, nameof(collection));
            _pipeline = pipeline;
        }

        public string Collection => _collection;
        public override AstNodeType NodeType => AstNodeType.UnionWithStage;
        public AstPipeline Pipeline => _pipeline;

        public override BsonValue Render()
        {
            return new BsonDocument("$unionWith", RenderWith());
        }

        private BsonValue RenderWith()
        {
            if (_pipeline == null)
            {
                return _collection;
            }
            else
            {
                return new BsonDocument
                {
                    { "coll", _collection },
                    { "pipeline", _pipeline.Render() }
                };
            }
        }
    }
}
