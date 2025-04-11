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

using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions
{
    internal sealed class AstGetFieldExpression : AstExpression
    {
        private readonly AstExpression _fieldName;
        private readonly AstExpression _input;

        public AstGetFieldExpression(AstExpression input, AstExpression fieldName)
        {
            _input = Ensure.IsNotNull(input, nameof(input));
            _fieldName = Ensure.IsNotNull(fieldName, nameof(fieldName));
        }

        public AstExpression FieldName => _fieldName;
        public AstExpression Input => _input;
        public override AstNodeType NodeType => AstNodeType.GetFieldExpression;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitGetFieldExpression(this);
        }

        public override bool CanBeConvertedToFieldPath()
        {
            return HasSafeFieldName(out _) && _input.CanBeConvertedToFieldPath();
        }

        public override string ConvertToFieldPath()
        {
            if (HasSafeFieldName(out var fieldName))
            {
                if (_input is AstVarExpression var)
                {
                    return var.IsCurrent ? $"${fieldName}" : $"$${var.Name}.{fieldName}";
                }

                var inputPath = _input.ConvertToFieldPath();
                return $"{inputPath}.{fieldName}";
            }

            return base.ConvertToFieldPath();
        }

        public bool HasSafeFieldName(out string fieldName)
        {
            if (_fieldName.IsStringConstant(out var constantFieldName))
            {
                fieldName = constantFieldName;
                if (fieldName.Length > 0 && fieldName[0] != '$' && !fieldName.Contains('.'))
                {
                    return true;
                }
            }

            fieldName = null;
            return false;
        }

        public override BsonValue Render()
        {
            return new BsonDocument("$getField", new BsonDocument { { "field", _fieldName.Render() }, { "input", _input.Render() } });
        }

        public AstGetFieldExpression Update(AstExpression input, AstExpression fieldName)
        {
            if (input == _input && fieldName == _fieldName)
            {
                return this;
            }

            return new AstGetFieldExpression(input, fieldName);
        }
    }
}
