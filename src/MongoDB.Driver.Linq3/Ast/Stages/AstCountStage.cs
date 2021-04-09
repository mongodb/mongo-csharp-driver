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
    public sealed class AstCountStage : AstStage
    {
        private readonly string _outputField;

        public AstCountStage(string outputField)
        {
            _outputField = Ensure.IsNotNull(outputField, nameof(outputField));
        }

        public override AstNodeType NodeType => AstNodeType.CountStage;
        public string OutputField => _outputField;

        public override BsonValue Render()
        {
            return new BsonDocument("$count", _outputField);
        }
    }
}
