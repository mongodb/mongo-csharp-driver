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
    internal sealed class AstSampleStage : AstStage
    {
        private readonly long _size;

        public AstSampleStage(long size)
        {
            _size = Ensure.IsGreaterThanZero(size, nameof(size));
        }

        public override AstNodeType NodeType => AstNodeType.RedactStage;
        public long SampleSize => _size;

        public override BsonValue Render()
        {
            return new BsonDocument("$sample", new BsonDocument("size", _size));
        }
    }
}
