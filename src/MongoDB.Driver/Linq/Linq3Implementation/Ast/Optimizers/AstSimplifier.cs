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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
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

        public override AstNode VisitCondExpression(AstCondExpression node)
        {
            // { $cond : [{ $eq : [expr1, null] }, null, expr2] }
            if (node.If is AstBinaryExpression binaryIfpression &&
                binaryIfpression.Operator == AstBinaryOperator.Eq &&
                binaryIfpression.Arg1 is AstExpression expr1 &&
                binaryIfpression.Arg2 is AstConstantExpression constantComparandExpression &&
                constantComparandExpression.Value == BsonNull.Value &&
                node.Then is AstConstantExpression constantThenExpression &&
                constantThenExpression.Value == BsonNull.Value &&
                node.Else is AstExpression expr2)
            {
                // { $cond : [{ $eq : [expr, null] }, null, expr] } => expr
                if (expr1 == expr2)
                {
                    return Visit(expr2);
                }

                // { $cond : [{ $eq : [expr, null] }, null, { $toT : expr }] } => { $toT : expr } for operators that map null to null
                if (expr2 is AstUnaryExpression unaryElseExpression &&
                    OperatorMapsNullToNull(unaryElseExpression.Operator) &&
                    unaryElseExpression.Arg == expr1)
                {
                    return Visit(expr2);
                }
            }

            return base.VisitCondExpression(node);

            static bool OperatorMapsNullToNull(AstUnaryOperator @operator)
            {
                return @operator switch
                {
                    AstUnaryOperator.ToDecimal => true,
                    AstUnaryOperator.ToDouble => true,
                    AstUnaryOperator.ToInt => true,
                    AstUnaryOperator.ToLong => true,
                    _ => false
                };
            }
        }

        public override AstNode VisitFieldOperationFilter(AstFieldOperationFilter node)
        {
            node = (AstFieldOperationFilter)base.VisitFieldOperationFilter(node);

            if (node.Field.Path != "@<elem>")
            {
                // { field : { $eq : value } } => { field : value } where value is not a regex
                if (IsFieldEqValue(node, out var value))
                {
                    var impliedOperation = new AstImpliedOperationFilterOperation(value);
                    return new AstFieldOperationFilter(node.Field, impliedOperation);
                }

                // { field : { $regex : "pattern", $options : "options" } } => { field : /pattern/options }
                if (IsFieldRegex(node, out var regex))
                {
                    var impliedOperation = new AstImpliedOperationFilterOperation(regex);
                    return new AstFieldOperationFilter(node.Field, impliedOperation);
                }

                // { field : { $not : { $regex : "pattern", $options : "options" } } } => { field : { $not : /pattern/options } }
                if (IsFieldNotRegex(node, out regex))
                {
                    var notImpliedOperation = new AstNotFilterOperation(new AstImpliedOperationFilterOperation(regex));
                    return new AstFieldOperationFilter(node.Field, notImpliedOperation);
                }

                // { field : { $elemMatch : { $eq : value } } } => { field : value } where value is not regex
                if (IsFieldElemMatchEqValue(node, out value))
                {
                    var impliedOperation = new AstImpliedOperationFilterOperation(value);
                    return new AstFieldOperationFilter(node.Field, impliedOperation);
                }

                // { field : { $elemMatch : { $regex : "pattern", $options : "options" } } } => { field : /pattern/options }
                if (IsFieldElemMatchRegex(node, out regex))
                {
                    var impliedOperation = new AstImpliedOperationFilterOperation(regex);
                    return new AstFieldOperationFilter(node.Field, impliedOperation);
                }

                // { field : { $elemMatch : { $not : { $regex : "pattern", $options : "options" } } } } => { field : { $elemMatch : { $not : /pattern/options } } }
                if (IsFieldElemMatchNotRegex(node, out var elemField, out regex))
                {
                    var notRegexOperation = new AstNotFilterOperation(new AstImpliedOperationFilterOperation(regex));
                    var elemFilter = new AstFieldOperationFilter(elemField, notRegexOperation);
                    var elemMatchOperation = new AstElemMatchFilterOperation(elemFilter);
                    return new AstFieldOperationFilter(node.Field, elemMatchOperation);
                }

                // { field : { $not : { $elemMatch : { $eq : value } } } } => { field : { $ne : value } } where value is not regex
                if (IsFieldNotElemMatchEqValue(node, out value))
                {
                    var impliedOperation = new AstComparisonFilterOperation(AstComparisonFilterOperator.Ne, value);
                    return new AstFieldOperationFilter(node.Field, impliedOperation);
                }

                // { field : { $not : { $elemMatch : { $regex : "pattern", $options : "options" } } } } => { field : { $not : /pattern/options } }
                if (IsFieldNotElemMatchRegex(node, out regex))
                {
                    var notImpliedOperation = new AstNotFilterOperation(new AstImpliedOperationFilterOperation(regex));
                    return new AstFieldOperationFilter(node.Field, notImpliedOperation);
                }
            }

            return node;

            static bool IsFieldEqValue(AstFieldOperationFilter node, out BsonValue value)
            {
                // { field : { $eq : value } } where value is not a a regex
                if (node.Operation is AstComparisonFilterOperation comparisonOperation &&
                    comparisonOperation.Operator == AstComparisonFilterOperator.Eq &&
                    comparisonOperation.Value.BsonType != BsonType.RegularExpression)
                {
                    value = comparisonOperation.Value;
                    return true;
                }

                value = null;
                return false;
            }

            static bool IsFieldRegex(AstFieldOperationFilter node, out BsonRegularExpression regex)
            {
                // { field : { $regex : "pattern", $options : "options" } }
                if (node.Operation is AstRegexFilterOperation regexOperation)
                {
                    regex = new BsonRegularExpression(regexOperation.Pattern, regexOperation.Options);
                    return true;
                }

                regex = null;
                return false;
            }

            static bool IsFieldNotRegex(AstFieldOperationFilter node, out BsonRegularExpression regex)
            {
                // { field : { $not : { $regex : "pattern", $options : "options" } } }
                if (node.Operation is AstNotFilterOperation notOperation &&
                    notOperation.Operation is AstRegexFilterOperation regexOperation)
                {
                    regex = new BsonRegularExpression(regexOperation.Pattern, regexOperation.Options);
                    return true;
                }

                regex = null;
                return false;
            }

            static bool IsFieldElemMatchEqValue(AstFieldOperationFilter node, out BsonValue value)
            {
                // { field : { $elemMatch : { $eq : value } } } where value is not regex
                if (node.Operation is AstElemMatchFilterOperation elemMatchOperation &&
                    elemMatchOperation.Filter is AstFieldOperationFilter elemFilter &&
                    elemFilter.Field.Path == "@<elem>" &&
                    elemFilter.Operation is AstComparisonFilterOperation comparisonOperation &&
                    comparisonOperation.Operator == AstComparisonFilterOperator.Eq &&
                    comparisonOperation.Value.BsonType != BsonType.RegularExpression)
                {
                    value = comparisonOperation.Value;
                    return true;
                }

                value = null;
                return false;
            }

            static bool IsFieldElemMatchRegex(AstFieldOperationFilter node, out BsonRegularExpression regex)
            {
                // { field : { $elemMatch : { $regex : "pattern", $options : "options" } } }
                if (node.Operation is AstElemMatchFilterOperation elemMatchOperation &&
                    elemMatchOperation.Filter is AstFieldOperationFilter elemFilter &&
                    elemFilter.Field.Path == "@<elem>" &&
                    elemFilter.Operation is AstRegexFilterOperation regexOperation)
                {
                    regex = new BsonRegularExpression(regexOperation.Pattern, regexOperation.Options);
                    return true;
                }

                regex = null;
                return false;
            }

            static bool IsFieldElemMatchNotRegex(AstFieldOperationFilter node, out AstFilterField elemField, out BsonRegularExpression regex)
            {
                // { field : { $elemMatch : { $not : { $regex : "pattern", $options : "options" } } } }
                if (node.Operation is AstElemMatchFilterOperation elemMatch &&
                    elemMatch.Filter is AstFieldOperationFilter elemFilter &&
                    elemFilter.Field.Path == "@<elem>" &&
                    elemFilter.Operation is AstNotFilterOperation notOperation &&
                    notOperation.Operation is AstRegexFilterOperation regexOperation)
                {
                    elemField = elemFilter.Field;
                    regex = new BsonRegularExpression(regexOperation.Pattern, regexOperation.Options);
                    return true;
                }

                elemField = null;
                regex = null;
                return false;
            }

            static bool IsFieldNotElemMatchEqValue(AstFieldOperationFilter node, out BsonValue value)
            {
                // { field : { $not : { $elemMatch : { $eq : value } } } } where value is not regex
                if (node.Operation is AstNotFilterOperation notFilterOperation &&
                    notFilterOperation.Operation is AstElemMatchFilterOperation elemMatchOperation &&
                    elemMatchOperation.Filter is AstFieldOperationFilter elemFilter &&
                    elemFilter.Field.Path == "@<elem>" &&
                    elemFilter.Operation is AstComparisonFilterOperation comparisonOperation &&
                    comparisonOperation.Operator == AstComparisonFilterOperator.Eq &&
                    comparisonOperation.Value.BsonType != BsonType.RegularExpression)
                {
                    value = comparisonOperation.Value;
                    return true;
                }

                value = null;
                return false;
            }

            static bool IsFieldNotElemMatchRegex(AstFieldOperationFilter node, out BsonRegularExpression regex)
            {
                // { field : { $not : { $elemMatch : { $regex : "pattern", $options : "options" } } } }
                if (node.Operation is AstNotFilterOperation notFilterOperation &&
                    notFilterOperation.Operation is AstElemMatchFilterOperation elemMatchOperation &&
                    elemMatchOperation.Filter is AstFieldOperationFilter elemFilter &&
                    elemFilter.Field.Path == "@<elem>" &&
                    elemFilter.Operation is AstRegexFilterOperation regexOperation)
                {
                    regex = new BsonRegularExpression(regexOperation.Pattern, regexOperation.Options);
                    return true;
                }

                regex = null;
                return false;
            }
        }

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

        public override AstNode VisitLetExpression(AstLetExpression node)
        {
            node = (AstLetExpression)base.VisitLetExpression(node);

            // { $let : { vars : { var : expr }, in : "$$var" } } => expr
            if (node.Vars.Count == 1 &&
                node.Vars[0].Var.Name is string varName &&
                node.In is AstVarExpression varExpression &&
                varExpression.Name == varName)
            {
                return node.Vars[0].Value;
            }

            return node;
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

        public override AstNode VisitNotFilterOperation(AstNotFilterOperation node)
        {
            if (node.Operation is AstExistsFilterOperation existsFilterOperation)
            {
                return new AstExistsFilterOperation(!existsFilterOperation.Exists);
            }

            return base.VisitNotFilterOperation(node);
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
