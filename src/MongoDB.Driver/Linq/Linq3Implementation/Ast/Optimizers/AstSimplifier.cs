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

using System;
using System.Collections.Generic;
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
                if (arg1.IsConstant(out var arg1Constant))
                {
                    // { $ifNull : [expr1, expr2] } => expr2 when expr1 == null
                    // { $ifNull : [expr1, expr2] } => expr1 when expr1 != null
                    return arg1Constant.IsBsonNull ? arg2 : arg1;
                }

                if (arg2.IsBsonNull())
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
                binaryIfpression.Arg2.IsBsonNull() &&
                node.Then.IsBsonNull() &&
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
                mapExpression.Input.IsConstant<BsonArray>(out var inputArrayConstant) &&
                mapExpression.In is AstBinaryExpression inBinaryExpression &&
                inBinaryExpression.Operator == AstBinaryOperator.Eq &&
                TryGetBinaryExpressionArguments(inBinaryExpression, out AstFieldPathExpression fieldPathExpression, out AstVarExpression varExpression) &&
                fieldPathExpression.Path.Length > 1 && fieldPathExpression.Path[0] == '$' && fieldPathExpression.Path[1] != '$' &&
                varExpression == mapExpression.As)
            {
                // { $expr : { $anyElementTrue : { $map : { input : <constantArray>, as : "<var>", in : { $eq : ["$<dottedFieldName>", "$$<var>"] } } } } }
                //      => { "<dottedFieldName>" : { $in : <constantArray> } }
                return AstFilter.In(AstFilter.Field(fieldPathExpression.Path.Substring(1)), inputArrayConstant);
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

            if (condExpression.IsBooleanConstant(out var booleanConstant))
            {
                if (booleanConstant)
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

            // { $map : { input : { $map : { input : <innerInput>, as : "inner", in : { A : <exprA>, B : <exprB>, ... } } }, as: "outer", in : { F : '$$outer.A', G : "$$outer.B", ... } } }
            // => { $map : { input : <innerInput>, as: "inner", in : { F : <exprA>, G : <exprB>, ... } } }
            {
                if (node.Input is AstMapExpression innerMapExpression &&
                    node.As is var outerVar &&
                    node.In is AstComputedDocumentExpression outerComputedDocumentExpression &&
                    innerMapExpression.Input is var innerInput &&
                    innerMapExpression.As is var innerVar &&
                    innerMapExpression.In is AstComputedDocumentExpression innerComputedDocumentExpression &&
                    outerComputedDocumentExpression.Fields.All(outerField =>
                        outerField.Value is AstGetFieldExpression outerGetFieldExpression &&
                        outerGetFieldExpression.Input == outerVar &&
                        outerGetFieldExpression.FieldName is AstConstantExpression { Value : BsonString { Value : var matchingFieldName } } &&
                        innerComputedDocumentExpression.Fields.Any(innerField => innerField.Path == matchingFieldName)))
                {
                    var rewrittenOuterFields = new List<AstComputedField>();
                    foreach (var outerField in outerComputedDocumentExpression.Fields)
                    {
                        var outerGetFieldExpression = (AstGetFieldExpression)outerField.Value;
                        var matchingFieldName = ((AstConstantExpression)outerGetFieldExpression.FieldName).Value.AsString;
                        var matchingInnerField = innerComputedDocumentExpression.Fields.Single(innerField => innerField.Path == matchingFieldName);
                        var rewrittenOuterField = AstExpression.ComputedField(outerField.Path, matchingInnerField.Value);
                        rewrittenOuterFields.Add(rewrittenOuterField);
                    }

                    var simplified = AstExpression.Map(
                        input: innerInput,
                        @as: innerVar,
                        @in: AstExpression.ComputedDocument(rewrittenOuterFields));

                    return Visit(simplified);
                }
            }

            // { $map : { input : [{ A : <exprA1>, B : <exprB1>, ... }, { A : <exprA2>, B : <exprB2>, ... }, ...], as : "item", in: { F : "$$item.A", G : "$$item.B", ... } } }
            // => [{ F : <exprA1>, G : <exprB1>", ... }, { F : <exprA2>, G : <exprB2>, ... }, ...]
            if (node.Input is AstComputedArrayExpression inputComputedArray &&
                inputComputedArray.Items.Count >= 1 &&
                inputComputedArray.Items[0] is AstComputedDocumentExpression firstComputedDocument &&
                firstComputedDocument.Fields.Select(inputField => inputField.Path).ToArray() is var inputFieldNames &&
                inputComputedArray.Items.Skip(1).All(otherItem =>
                    otherItem is AstComputedDocumentExpression otherComputedDocument &&
                    otherComputedDocument.Fields.Select(otherField => otherField.Path).SequenceEqual(inputFieldNames)) &&
                node.As is var itemVar &&
                node.In is AstComputedDocumentExpression mappedDocument &&
                mappedDocument.Fields.All(mappedField =>
                    mappedField.Value is AstGetFieldExpression mappedGetField &&
                    mappedGetField.Input == itemVar &&
                    mappedGetField.FieldName is AstConstantExpression { Value : BsonString { Value : var matchingFieldName } } &&
                    inputFieldNames.Contains(matchingFieldName)))
            {
                var rewrittenItems = new List<AstExpression>();
                foreach (var inputItem in inputComputedArray.Items)
                {
                    var inputDocument = (AstComputedDocumentExpression)inputItem;

                    var rewrittenFields = new List<AstComputedField>();
                    foreach (var mappedField in mappedDocument.Fields)
                    {
                        var mappedGetField = (AstGetFieldExpression)mappedField.Value;
                        var matchingFieldName = ((AstConstantExpression)mappedGetField.FieldName).Value.AsString;
                        var matchingInputField = inputDocument.Fields.Single(inputField => inputField.Path == matchingFieldName);
                        var rewrittenField = AstExpression.ComputedField(mappedField.Path, matchingInputField.Value);
                        rewrittenFields.Add(rewrittenField);
                    }

                    var rewrittenItem = AstExpression.ComputedDocument(rewrittenFields);
                    rewrittenItems.Add(rewrittenItem);
                }

                var simplified = AstExpression.ComputedArray(rewrittenItems);

                return Visit(simplified);
            }

            // { $map : { input : { $map : { input : <array>Expr, as : <innerVar>, in : { ..., fieldName : <fieldExpr>, ... } } }, as : <outerVar>, in : "$$<outerVar>.fieldName" } }
            // => { $map : { input : <arrayExpr>, as : <innerVar>, in : <fieldExpr> } }
            {
                if (node.Input is AstMapExpression innerMap &&
                    node.As is var outerVar &&
                    node.In is AstGetFieldExpression outerGetFieldExpression &&
                    outerGetFieldExpression.Input == outerVar &&
                    outerGetFieldExpression.FieldName is AstConstantExpression { Value : { BsonType : BsonType.String } } fieldNameExpression &&
                    fieldNameExpression.Value.AsString is var fieldName &&
                    innerMap.Input is var arrayExpression &&
                    innerMap.As is var innerVar &&
                    innerMap.In is AstComputedDocumentExpression innerComputedDocument &&
                    innerComputedDocument.Fields.SingleOrDefault(f => f.Path == fieldName) is var computedField &&
                    computedField != null &&
                    computedField.Value is var fieldExpr)
                {
                    var simplified =
                        AstExpression.Map(
                            input: arrayExpression,
                            @as: innerVar,
                            @in: fieldExpr);

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

            if (array.IsConstant<BsonArray>(out var arrayConstant) &&
                position.IsInt32Constant(out var positionConstant) && positionConstant >= 0 &&
                n.IsInt32Constant(out var nConstant) && nConstant >= 0)
            {
                // { slice : [array, position, n] } => array.Skip(position).Take(n) when all arguments are non-negative constants
                return AstExpression.Constant(new BsonArray(arrayConstant.Skip(positionConstant).Take(nConstant)));
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
                arg.IsBooleanConstant(out var booleanConstant))
            {
                return AstExpression.Constant(!booleanConstant);
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

            // { $arrayToObject : [[{ k : 'A', v : <exprA> }, { k : 'B', v : <exprB> }, ...]] } => { A : <exprA>, B : <exprB>, ... }
            if (node.Operator == AstUnaryOperator.ArrayToObject &&
                arg is AstComputedArrayExpression computedArrayExpression &&
                computedArrayExpression.Items.All(
                    item =>
                        item is AstComputedDocumentExpression computedDocumentExpression &&
                        computedDocumentExpression.Fields.Count == 2 &&
                        computedDocumentExpression.Fields[0].Path == "k" &&
                        computedDocumentExpression.Fields[1].Path == "v" &&
                        computedDocumentExpression.Fields[0].Value is AstConstantExpression { Value : { IsString : true } }))
            {
                var computedFields = computedArrayExpression.Items.Select(KeyValuePairDocumentToComputedField);
                return AstExpression.ComputedDocument(computedFields);
            }

            return node.Update(arg);

            static AstComputedField KeyValuePairDocumentToComputedField(AstExpression expression)
            {
                // caller has verified that expression is of the form: { k : <stringConstant>, v : <valueExpression> }
                var keyValuePairDocumentExpression = (AstComputedDocumentExpression)expression;
                var keyConstantExpression = (AstConstantExpression)keyValuePairDocumentExpression.Fields[0].Value;
                var valueExpression = keyValuePairDocumentExpression.Fields[1].Value;

                return AstExpression.ComputedField(keyConstantExpression.Value.AsString, valueExpression);
            }
        }
    }
}
