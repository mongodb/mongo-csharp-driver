﻿/* Copyright 2010-present MongoDB Inc.
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

using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

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

        public override AstNode VisitBinaryExpression(AstBinaryExpression node)
        {
            var arg1 = VisitAndConvert(node.Arg1);
            var arg2 = VisitAndConvert(node.Arg2);

            if (node.Operator == AstBinaryOperator.IfNull)
            {
                if (arg1 is AstConstantExpression arg1ConstantExpression)
                {
                    // { $ifNull : [expr1, expr2] } => expr2 when expr1 == null
                    // { $ifNull : [expr1, expr2] } => expr1 when expr1 != null
                    return arg1ConstantExpression.Value == BsonNull.Value ? arg2 : arg1;
                }

                if (arg2 is AstConstantExpression arg2ConstantExpression &&
                    arg2ConstantExpression.Value == BsonNull.Value)
                {
                    // { $ifNull : [expr1, expr2] } => expr1 when expr2 == null
                    return arg1;
                }
            }

            return node.Update(arg1, arg2);
        }

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

        public override AstNode VisitExprFilter(AstExprFilter node)
        {
            var optimizedNode = base.VisitExprFilter(node);

            if (optimizedNode is AstExprFilter exprFilter &&
                exprFilter.Expression is AstUnaryExpression unaryExpression &&
                unaryExpression.Operator == AstUnaryOperator.AnyElementTrue &&
                unaryExpression.Arg is AstMapExpression mapExpression &&
                mapExpression.Input is AstConstantExpression inputConstant &&
                inputConstant.Value is BsonArray inputArrayValue &&
                mapExpression.In is AstBinaryExpression inBinaryExpression &&
                inBinaryExpression.Operator == AstBinaryOperator.Eq &&
                TryGetBinaryExpressionArguments(inBinaryExpression, out AstFieldPathExpression fieldPathExpression, out AstVarExpression varExpression) &&
                fieldPathExpression.Path.Length > 1 && fieldPathExpression.Path[0] == '$' && fieldPathExpression.Path[1] != '$' &&
                varExpression == mapExpression.As)
            {
                // { $expr : { $anyElementTrue : { $map : { input : <constantArray>, as : "<var>", in : { $eq : ["$<dottedFieldName>", "$$<var>"] } } } } }
                //      => { "<dottedFieldName>" : { $in : <constantArray> } }
                return AstFilter.In(AstFilter.Field(fieldPathExpression.Path.Substring(1)), inputArrayValue);
            }

            return optimizedNode;

            static bool TryGetBinaryExpressionArguments<T1, T2>(AstBinaryExpression binaryExpression, out T1 arg1, out T2 arg2)
                where T1 : AstNode
                where T2 : AstNode
            {
                if (binaryExpression.Arg1 is T1 arg1AsT1 && binaryExpression.Arg2 is T2 arg2AsT2)
                {
                    arg1 = arg1AsT1;
                    arg2 = arg2AsT2;
                    return true;
                }

                if (binaryExpression.Arg1 is T2 arg1AsT2 && binaryExpression.Arg1 is T1 arg2AsT1)
                {
                    arg1 = arg2AsT1;
                    arg2 = arg1AsT2;
                    return true;
                }

                arg1 = null;
                arg2 = null;
                return false;
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

        public override AstNode VisitFilterExpression(AstFilterExpression node)
        {
            var inputExpression = VisitAndConvert(node.Input);
            var condExpression = VisitAndConvert(node.Cond);
            var limitExpression = VisitAndConvert(node.Limit);

            if (condExpression is AstConstantExpression condConstantExpression &&
                condConstantExpression.Value is BsonBoolean condBsonBoolean)
            {
                if (condBsonBoolean.Value)
                {
                    // { $filter : { input : <input>, as : "x", cond : true } } => <input>
                    if (limitExpression == null)
                    {
                        return inputExpression;
                    }
                }
                else
                {
                    // { $filter : { input : <input>, as : "x", cond : false, optional-limit } } => []
                    return AstExpression.Constant(new BsonArray());
                }
            }

            return node.Update(inputExpression, condExpression, limitExpression);
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

        public override AstNode VisitPipeline(AstPipeline node)
        {
            var stages = VisitAndConvert(node.Stages);

            // { $match : { } } => remove redundant stage
            if (stages.Any(stage => IsMatchEverythingStage(stage)))
            {
                stages = stages.Where(stage => !IsMatchEverythingStage(stage)).AsReadOnlyList();
            }

            return node.Update(stages);

            static bool IsMatchEverythingStage(AstStage stage)
            {
                return
                    stage is AstMatchStage matchStage &&
                    matchStage.Filter is AstMatchesEverythingFilter;
            }
        }

        public override AstNode VisitSliceExpression(AstSliceExpression node)
        {
            node = (AstSliceExpression)base.VisitSliceExpression(node);
            var array = node.Array;
            var position = node.Position ?? 0; // map null to zero
            var n = node.N;

            if (position.IsZero() && n.IsMaxInt32())
            {
                // { $slice : [array, 0, maxint] } => array
                return array;
            }

            if (array is AstConstantExpression arrayConstant &&
                arrayConstant.Value is BsonArray bsonArrayConstant &&
                position.IsInt32Constant(out var positionValue) && positionValue >= 0 &&
                n.IsInt32Constant(out var nValue) && nValue >= 0)
            {
                // { slice : [array, position, n] } => array.Skip(position).Take(n) when all arguments are non-negative constants
                return AstExpression.Constant(new BsonArray(bsonArrayConstant.Skip(positionValue).Take(nValue)));
            }

            if (array is AstSliceExpression inner &&
                (inner.Position ?? 0).IsInt32Constant(out var innerPosition) && innerPosition >= 0 &&
                inner.N.IsInt32Constant(out var innerN) && innerN >= 0 &&
                position.IsInt32Constant(out var outerPosition) && outerPosition >= 0 &&
                n.IsInt32Constant(out var outerN) && outerN >= 0)
            {
                // the following simplifcations are only valid when all position and n values are known to be non-negative (so they have to be constants)
                // { $slice : [{ $slice : [inner.Array, innerPosition, maxint] }, outerPosition, maxint] } => { $slice : [inner.Array, innerPosition + outerPosition, maxint] }
                // { $slice : [{ $slice : [inner.Array, innerPosition, maxint] }, outerPosition, outerN] } => { $slice : [inner.Array, innerPosition + outerPosition, outerN] }
                // { $slice : [{ $slice : [inner.Array, innerPosition, innerN] }, outerPosition, maxint] } => { $slice : [inner.Array, innerPosition + outerPosition, max(innerN - outerPosition, 0)] }
                // { $slice : [{ $slice : [inner.Array, innerPosition, innerN] }, outerPosition, outerN] } => { $slice : [inner.Array, innerPosition + outerPosition, min(max(innerN - outerPosition, 0), outerN)] }
                var combinedPosition = AstExpression.Add(innerPosition, outerPosition);
                var combinedN = (innerN, outerN) switch
                {
                    (int.MaxValue, int.MaxValue) => int.MaxValue, // check whether both are int.MaxValue before checking one at a time
                    (int.MaxValue, _) => outerN,
                    (_, int.MaxValue) => Math.Max(innerN - outerPosition, 0),
                    _ => Math.Min(Math.Max(innerN - outerPosition, 0), outerN)
                };

                return AstExpression.Slice(inner.Array, combinedPosition, combinedN);
            }

            return node;
        }

        public override AstNode VisitUnaryExpression(AstUnaryExpression node)
        {
            var arg = VisitAndConvert(node.Arg);

            // { $first : <arg> } => { $arrayElemAt : [<arg>, 0] } (or -1 for $last)
            if (node.Operator == AstUnaryOperator.First || node.Operator == AstUnaryOperator.Last)
            {
                var index = node.Operator == AstUnaryOperator.First ? 0 : -1;
                return AstExpression.ArrayElemAt(arg, index);
            }

            // { $not : booleanConstant } => !booleanConstant
            if (node.Operator is AstUnaryOperator.Not &&
                arg is AstConstantExpression argConstantExpression &&
                argConstantExpression.Value is BsonBoolean argBsonBoolean)
            {
                return AstExpression.Constant(!argBsonBoolean.Value);
            }

            // { $not : { $eq : [expr1, expr2] } } => { $ne : [expr1, expr2] }
            // { $not : { $ne : [expr1, expr2] } } => { $eq : [expr1, expr2] }
            if (node.Operator is AstUnaryOperator.Not &&
                arg is AstBinaryExpression argBinaryExpression &&
                argBinaryExpression.Operator is AstBinaryOperator.Eq or AstBinaryOperator.Ne)
            {
                var oppositeComparisonOperator = argBinaryExpression.Operator == AstBinaryOperator.Eq ? AstBinaryOperator.Ne : AstBinaryOperator.Eq;
                return AstExpression.Binary(oppositeComparisonOperator, argBinaryExpression.Arg1, argBinaryExpression.Arg2);
            }

            return node.Update(arg);
        }
    }
}
