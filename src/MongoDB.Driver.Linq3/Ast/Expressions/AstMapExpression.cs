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

namespace MongoDB.Driver.Linq3.Ast.Expressions
{
    public sealed class AstMapExpression : AstExpression
    {
        #region static
        public static AstExpression Create(AstExpression input, string @as, AstExpression @in)
        {
            var prefix = "$$" + @as + ".";
            if (input is AstFieldExpression inputField && @in is AstFieldExpression inField && inField.Field.StartsWith(prefix))
            {
                var combinedFieldName = inputField.Field + "." + inField.Field.Substring(prefix.Length);
                return new AstFieldExpression(combinedFieldName);
            }

            return new AstMapExpression(input, @as, @in);
        }
        #endregion

        private readonly string _as;
        private readonly AstExpression _in;
        private readonly AstExpression _input;

        public AstMapExpression(
            AstExpression input,
            string @as,
            AstExpression @in)
        {
            _input = Ensure.IsNotNull(input, nameof(input));
            _as = @as;
            _in = Ensure.IsNotNull(@in, nameof(@in));
        }

        public string As => _as;
        public AstExpression In => _in;
        public AstExpression Input => _input;
        public override AstNodeType NodeType => AstNodeType.MapExpression;

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$map", new BsonDocument
                    {
                        { "input", _input.Render() },
                        { "as", _as, _as != null },
                        { "in", _in.Render() }
                    }
                }
            };
        }
    }
}
