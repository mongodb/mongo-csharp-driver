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

using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc
{
    internal class Symbol
    {
        // private fields
        private readonly AstExpression _ast;
        private readonly bool _isCurrent;
        private readonly string _name;
        private readonly ParameterExpression _parameter;
        private readonly IBsonSerializer _serializer;

        // constructors
        public Symbol(ParameterExpression parameter, string name, AstExpression ast, IBsonSerializer serializer, bool isCurrent)
        {
            _parameter = Ensure.IsNotNull(parameter, nameof(parameter));
            _name = Ensure.IsNotNullOrEmpty(name, nameof(name));
            _ast = Ensure.IsNotNull(ast, nameof(ast));
            _serializer = Ensure.IsNotNull(serializer, nameof(serializer));
            _isCurrent = isCurrent;
        }

        // public properties
        public AstExpression Ast => _ast;
        public bool IsCurrent => _isCurrent;
        public string Name => _name;
        public ParameterExpression Parameter => _parameter;
        public IBsonSerializer Serializer => _serializer;
        public AstVarExpression Var => (AstVarExpression)_ast;

        // public methods
        public Symbol AsNotCurrent()
        {
            if (_isCurrent)
            {
                if (_ast is AstVarExpression varExpression)
                {
                    return new Symbol(_parameter, _name, varExpression.AsNotCurrent(), _serializer, isCurrent: false);
                }
                else
                {
                    return new Symbol(_parameter, _name, _ast, _serializer, isCurrent: false);
                }
            }

            return this;
        }

        public override string ToString() => _name;
    }
}
