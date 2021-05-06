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

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters
{
    internal sealed class AstRegexFilterOperation : AstFilterOperation
    {
        private readonly string _options;
        private readonly string _pattern;

        public AstRegexFilterOperation(string pattern, string options)
        {
            _pattern = Ensure.IsNotNull(pattern, nameof(pattern));
            _options = Ensure.IsNotNull(options, nameof(options));
        }

        public override AstNodeType NodeType => AstNodeType.RegexFilterOperation;
        public string Options => _options;
        public string Pattern => _pattern;

        public override BsonValue Render()
        {
            return new BsonRegularExpression(_pattern, _options);
        }
    }
}
