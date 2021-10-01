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

using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Optimizers
{
    internal class AstSimplifier : AstNodeVisitor
    {
        #region static
        public static AstNode Simplify(AstNode node)
        {
            var simplifier = new AstSimplifier();
            return simplifier.Visit(node);
        }

        public static TNode SimplifyAndConvert<TNode>(TNode node)
            where TNode : AstNode
        {
            return (TNode)Simplify(node);
        }
        #endregion

        public override AstNode VisitGetFieldExpression(AstGetFieldExpression node)
        {
            if (TrySimplifyAsFieldPath(node, out var simplified))
            {
                return simplified;
            }

            if (TrySimplifyAsLet(node, out simplified))
            {
                return simplified;
            }

            return base.VisitGetFieldExpression(node);

            // { $getField : { field : "y", input : "$x" } } => "$x.y"
            static bool TrySimplifyAsFieldPath(AstGetFieldExpression node, out AstExpression simplified)
            {
                if (node.CanBeConvertedToFieldPath())
                {
                    var path = node.ConvertToFieldPath();
                    simplified = AstExpression.FieldPath(path);
                    return true;
                }

                simplified = null;
                return false;
            }

            // { $getField : { field : "x", input : <input> } } => { $let : { vars : { this : <input> }, in : "$$this.x" } }
            bool TrySimplifyAsLet(AstGetFieldExpression node, out AstExpression simplified)
            {
                if (node.HasSafeFieldName(out var fieldName))
                {
                    var simplifiedInput = VisitAndConvert(node.Input);
                    var var = AstExpression.Var("this");
                    var binding = AstExpression.VarBinding(var, simplifiedInput);
                    simplified = AstExpression.Let(binding, AstExpression.FieldPath($"$$this.{fieldName}"));
                    return true;
                }

                simplified = null;
                return false;
            }
        }

        public override AstNode VisitMapExpression(AstMapExpression node)
        {
            // { $map : { input : <input>, as : "v", in : "$$v.x" } } => { $getField : { field : "x", input : <input> } }
            if (node.Input is AstGetFieldExpression inputGetField &&
                node.In is AstGetFieldExpression inGetField)
            {
                if (UltimateGetFieldInput(inGetField) == node.As)
                {
                    var simplified = AstNodeReplacer.Replace(node.In, (node.As, node.Input));
                    return Visit(simplified);
                }
            }

            return base.VisitMapExpression(node);

            static AstExpression UltimateGetFieldInput(AstGetFieldExpression getField)
            {
                if (getField.Input is AstGetFieldExpression nestedInputGetField)
                {
                    return UltimateGetFieldInput(nestedInputGetField);
                }

                return getField.Input;
            }
        }

        public override AstNode VisitUnaryExpression(AstUnaryExpression node)
        {
            // { $first : <arg> } => { $arrayElemAt : [<arg>, 0] } (or -1 for $last)
            if (node.Operator == AstUnaryOperator.First || node.Operator == AstUnaryOperator.Last)
            {
                var simplifiedArg = VisitAndConvert(node.Arg);
                var index = node.Operator == AstUnaryOperator.First ? 0 : -1;
                return AstExpression.ArrayElemAt(simplifiedArg, index);
            }

            return base.VisitUnaryExpression(node);
        }
    }
}
