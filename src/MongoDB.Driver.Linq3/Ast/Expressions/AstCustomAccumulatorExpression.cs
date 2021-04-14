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

namespace MongoDB.Driver.Linq3.Ast.Expressions
{
    public sealed class AstCustomAccumulatorExpression : AstExpression
    {
        private readonly string _accumulate;
        private readonly IReadOnlyList<AstExpression> _accumulateArgs;
        private readonly string _finalize;
        private readonly string _init;
        private readonly IReadOnlyList<AstExpression> _initArgs;
        private readonly string _lang;
        private readonly string _merge;

        public AstCustomAccumulatorExpression(
            string lang,
            string init,
            IEnumerable<AstExpression> initArgs,
            string accumulate,
            IEnumerable<AstExpression> accumulateArgs,
            string merge,
            string finalize = null)
        {
            _lang = Ensure.IsNotNullOrEmpty(lang, nameof(lang));
            _init = Ensure.IsNotNullOrEmpty(init, nameof(init));
            _initArgs = initArgs?.ToList().AsReadOnly();
            _accumulate = Ensure.IsNotNullOrEmpty(accumulate, nameof(accumulate));
            _accumulateArgs = Ensure.IsNotNull(accumulateArgs, nameof(accumulateArgs)).ToList().AsReadOnly();
            _merge = Ensure.IsNotNullOrEmpty(merge, nameof(merge));
            _finalize = finalize;
        }

        public string Accumulate => _accumulate;
        public IReadOnlyList<AstExpression> AccumulateArgs => _accumulateArgs;
        public string Finalize => _finalize;
        public string Init => _init;
        public IReadOnlyList<AstExpression> InitArgs => _initArgs;
        public string Lang => _lang;
        public string Merge => _merge;
        public override AstNodeType NodeType => AstNodeType.CustomAccumulatorExpression;

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$accumulator", new BsonDocument
                    {
                        { "init", _init },
                        { "initArgs", () => new BsonArray(_accumulateArgs.Select(a => a.Render())), _initArgs != null },
                        { "accumulate", _accumulate },
                        { "accumulateArgs", new BsonArray(_initArgs.Select(a => a.Render())) },
                        { "merge", _merge },
                        { "finalize", _finalize, _finalize != null },
                        { "lang", _lang }
                    }
                }
            };
        }
    }
}
