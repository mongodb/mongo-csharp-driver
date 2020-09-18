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

namespace MongoDB.Driver.Linq3.Ast.Stages
{
    public sealed class AstUnwindStage : AstPipelineStage
    {
        private readonly string _field;
        private readonly string _includeArrayIndex;
        private readonly bool? _preserveNullAndEmptyArrays;

        public AstUnwindStage(
            string field,
            string includeArrayIndex = null,
            bool? preserveNullAndEmptyArrays = null)
        {
            _field = Ensure.IsNotNull(field, nameof(field));
            _includeArrayIndex = includeArrayIndex;
            _preserveNullAndEmptyArrays = preserveNullAndEmptyArrays;
        }

        public string Field => _field;
        public string IncludeArrayIndex => _includeArrayIndex;
        public override AstNodeType NodeType => AstNodeType.UnwindStage;
        public bool? PreserveNullAndEmptyArrays => _preserveNullAndEmptyArrays;

        public override BsonValue Render()
        {
            return new BsonDocument("$unwind", RenderUnwind());
        }

        private BsonValue RenderUnwind()
        {
            if (_includeArrayIndex == null && _preserveNullAndEmptyArrays == null)
            {
                return _field;
            }
            else
            {
                return new BsonDocument
                {
                    { "path", _field },
                    { "includeArrayIndex", _includeArrayIndex, _includeArrayIndex != null },
                    { "preserveNullAndEmptyArrays", () => _preserveNullAndEmptyArrays.Value, _preserveNullAndEmptyArrays != null }
                };
            }
        }
    }
}
