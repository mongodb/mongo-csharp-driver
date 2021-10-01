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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions
{
    internal sealed class AstFunctionExpression : AstExpression
    {
        private readonly string _body;
        private readonly IReadOnlyList<AstExpression> _args;
        private readonly string _lang;

        public AstFunctionExpression(
            string lang,
            string body,
            IEnumerable<AstExpression> args)
        {
            _lang = Ensure.IsNotNullOrEmpty(lang, nameof(lang));
            _body = Ensure.IsNotNullOrEmpty(body, nameof(body));
            _args = Ensure.IsNotNull(args, nameof(args)).AsReadOnlyList();
        }

        public string Body => _body;
        public IReadOnlyList<AstExpression> Args => _args;
        public string Lang => _lang;
        public override AstNodeType NodeType => AstNodeType.FunctionExpression;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitFunctionExpression(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$function", new BsonDocument
                    {
                        { "body", _body },
                        { "args", new BsonArray(_args.Select(a => a.Render())) },
                        { "lang", _lang }
                    }
                }
            };
        }

        public AstFunctionExpression Update(
            IEnumerable<AstExpression> args)
        {
            if (args == _args)
            {
                return this;
            }

            return new AstFunctionExpression(_lang, _body, args);
        }
    }
}
