/* Copyright 2010-2015 MongoDB Inc.
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
* 
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver.Linq.Expressions;
using MongoDB.Driver.Linq.Processors;
using MongoDB.Driver.Support;

namespace MongoDB.Driver.Linq.Translators
{
    internal class AggregateLanguageTranslator
    {
        public static BsonValue Translate(Expression node)
        {
            var builder = new AggregateLanguageTranslator();
            return builder.BuildValue(node);
        }

        private AggregateLanguageTranslator()
        {
        }

        private BsonValue BuildValue(Expression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    return BuildAdd((BinaryExpression)node);
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return BuildOperation((BinaryExpression)node, "$and", true);
                case ExpressionType.ArrayLength:
                    return new BsonDocument("$size", BuildValue(((UnaryExpression)node).Operand));
                case ExpressionType.Call:
                    return BuildMethodCall((MethodCallExpression)node);
                case ExpressionType.Coalesce:
                    return BuildOperation((BinaryExpression)node, "$ifNull", false);
                case ExpressionType.Conditional:
                    return BuildConditional((ConditionalExpression)node);
                case ExpressionType.Constant:
                    var value = BsonValue.Create(((ConstantExpression)node).Value);
                    var stringValue = value as BsonString;
                    if (stringValue != null && stringValue.Value.StartsWith("$"))
                    {
                        value = new BsonDocument("$literal", value);
                    }
                    // TODO: there may be other instances where we should use a literal...
                    // but I can't think of any yet.
                    return value;
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    return BuildValue(((UnaryExpression)node).Operand);
                case ExpressionType.Divide:
                    return BuildOperation((BinaryExpression)node, "$divide", false);
                case ExpressionType.Equal:
                    return BuildOperation((BinaryExpression)node, "$eq", false);
                case ExpressionType.GreaterThan:
                    return BuildOperation((BinaryExpression)node, "$gt", false);
                case ExpressionType.GreaterThanOrEqual:
                    return BuildOperation((BinaryExpression)node, "$gte", false);
                case ExpressionType.LessThan:
                    return BuildOperation((BinaryExpression)node, "$lt", false);
                case ExpressionType.LessThanOrEqual:
                    return BuildOperation((BinaryExpression)node, "$lte", false);
                case ExpressionType.MemberAccess:
                    return BuildMemberAccess((MemberExpression)node);
                case ExpressionType.MemberInit:
                    return BuildMemberInit((MemberInitExpression)node);
                case ExpressionType.Modulo:
                    return BuildOperation((BinaryExpression)node, "$mod", false);
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    return BuildOperation((BinaryExpression)node, "$multiply", true);
                case ExpressionType.New:
                    return BuildNew((NewExpression)node);
                case ExpressionType.Not:
                    return BuildNot((UnaryExpression)node);
                case ExpressionType.NotEqual:
                    return BuildOperation((BinaryExpression)node, "$ne", false);
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return BuildOperation((BinaryExpression)node, "$or", true);
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return BuildOperation((BinaryExpression)node, "$subtract", false);
                case ExpressionType.Extension:
                    var mongoExpression = node as ExtensionExpression;
                    if (mongoExpression != null)
                    {
                        switch (mongoExpression.ExtensionType)
                        {
                            case ExtensionExpressionType.Accumulator:
                                return BuildAccumulator((AccumulatorExpression)node);
                            case ExtensionExpressionType.GroupId:
                                return BuildValue(((GroupIdExpression)node).Expression);
                            case ExtensionExpressionType.Serialization:
                                return BuildSerialization((SerializationExpression)node);
                        }
                    }
                    break;
            }

            var message = string.Format("$project or $group does not support {0}.",
                node.ToString());
            throw new NotSupportedException(message);
        }

        private BsonValue BuildSerialization(SerializationExpression node)
        {
            if (string.IsNullOrWhiteSpace(node.SerializationInfo.ElementName))
            {
                return BuildValue(node.Expression);
            }

            return "$" + node.SerializationInfo.ElementName;
        }

        private BsonValue BuildAdd(BinaryExpression node)
        {
            var op = "$add";
            if (node.Left.Type == typeof(string))
            {
                op = "$concat";
            }

            return BuildOperation(node, op, true);
        }

        private BsonValue BuildAccumulator(AccumulatorExpression node)
        {
            switch (node.AccumulatorType)
            {
                case AccumulatorType.AddToSet:
                    return new BsonDocument("$addToSet", BuildValue(node.Argument));
                case AccumulatorType.Average:
                    return new BsonDocument("$avg", BuildValue(node.Argument));
                case AccumulatorType.Count:
                    return new BsonDocument("$sum", 1);
                case AccumulatorType.First:
                    return new BsonDocument("$first", BuildValue(node.Argument));
                case AccumulatorType.Last:
                    return new BsonDocument("$last", BuildValue(node.Argument));
                case AccumulatorType.Max:
                    return new BsonDocument("$max", BuildValue(node.Argument));
                case AccumulatorType.Min:
                    return new BsonDocument("$min", BuildValue(node.Argument));
                case AccumulatorType.Push:
                    return new BsonDocument("$push", BuildValue(node.Argument));
                case AccumulatorType.Sum:
                    return new BsonDocument("$sum", BuildValue(node.Argument));
            }

            // we should never ever get here.
            var message = string.Format("Unrecognized aggregation type in the expression tree {0}.",
                node.ToString());
            throw new MongoInternalException(message);
        }

        private BsonValue BuildConditional(ConditionalExpression node)
        {
            var condition = BuildValue(node.Test);
            var truePart = BuildValue(node.IfTrue);
            var falsePart = BuildValue(node.IfFalse);

            return new BsonDocument("$cond", new BsonArray(new[] { condition, truePart, falsePart }));
        }

        private BsonValue BuildMemberAccess(MemberExpression node)
        {
            BsonValue result;
            if (node.Expression.Type == typeof(DateTime)
                && TryBuildDateTimeMemberAccess(node, out result))
            {
                return result;
            }

            if (node.Expression != null
                && (node.Expression.Type.ImplementsInterface(typeof(ICollection<>))
                    || node.Expression.Type.ImplementsInterface(typeof(ICollection)))
                && node.Member.Name == "Count")
            {
                return new BsonDocument("$size", BuildValue(node.Expression));
            }

            var message = string.Format("Member {0} of type {1} in the expression tree {2}.",
                node.Member.Name,
                node.Member.DeclaringType,
                node.ToString());
            throw new NotSupportedException(message);
        }

        private BsonValue BuildMethodCall(MethodCallExpression node)
        {
            BsonValue result;
            if (ExtensionExpressionVisitor.IsLinqMethod(node) && TryBuildLinqMethodCall(node, out result))
            {
                return result;
            }

            if (node.Object == null
                && node.Method.DeclaringType == typeof(string)
                && TryBuildStaticStringMethodCall(node, out result))
            {
                return result;
            }

            if (node.Object != null
                && node.Object.Type == typeof(string)
                && TryBuildStringMethodCall(node, out result))
            {
                return result;
            }

            if (node.Object != null
                && node.Object.Type.IsGenericType
                && node.Object.Type.GetGenericTypeDefinition() == typeof(HashSet<>)
                && TryBuildHashSetMethodCall(node, out result))
            {
                return result;
            }

            if (node.Object != null
                && node.Method.Name == "CompareTo"
                && (node.Object.Type.ImplementsInterface(typeof(IComparable<>))
                    || node.Object.Type.ImplementsInterface(typeof(IComparable))))
            {
                return new BsonDocument("$cmp", new BsonArray(new[] { BuildValue(node.Object), BuildValue(node.Arguments[0]) }));
            }

            if (node.Object != null
                && node.Method.Name == "Equals"
                && node.Arguments.Count == 1)
            {
                return new BsonDocument("$eq", new BsonArray(new[] { BuildValue(node.Object), BuildValue(node.Arguments[0]) }));
            }

            var message = string.Format("{0} of type {1} is not supported in the expression tree {2}.",
                node.Method.Name,
                node.Method.DeclaringType,
                node.ToString());
            throw new NotSupportedException(message);
        }

        private BsonValue BuildMemberInit(MemberInitExpression node)
        {
            var mapping = ProjectionMapper.Map(node);
            return BuildMapping(mapping);
        }

        private BsonValue BuildNew(NewExpression node)
        {
            var mapping = ProjectionMapper.Map(node);
            return BuildMapping(mapping);
        }

        private BsonValue BuildMapping(ProjectionMapping mapping)
        {
            BsonDocument doc = new BsonDocument();
            bool hasId = false;
            foreach (var memberMapping in mapping.Members)
            {
                var value = BuildValue(memberMapping.Expression);
                string name = memberMapping.Member.Name;
                if (!hasId && memberMapping.Expression is GroupIdExpression)
                {
                    name = "_id";
                    hasId = true;
                    doc.InsertAt(0, new BsonElement(name, value));
                }
                else
                {
                    doc.Add(name, value);
                }
            }

            return doc;
        }

        private BsonValue BuildNot(UnaryExpression node)
        {
            var operand = BuildValue(node.Operand);
            if (operand.IsBsonDocument)
            {
                operand = new BsonArray().Add(operand);
            }
            return new BsonDocument("$not", operand);
        }

        private BsonValue BuildOperation(BinaryExpression node, string op, bool canBeFlattened)
        {
            var left = BuildValue(node.Left);
            var right = BuildValue(node.Right);

            // some operations take an array as the argument.
            // we want to flatten binary values into the top-level 
            // array if they are flattenable :).
            if (canBeFlattened && left.IsBsonDocument && left.AsBsonDocument.Contains(op) && left[op].IsBsonArray)
            {
                left[op].AsBsonArray.Add(right);
                return left;
            }

            return new BsonDocument(op, new BsonArray(new[] { left, right }));
        }

        private bool TryBuildDateTimeMemberAccess(MemberExpression node, out BsonValue result)
        {
            result = null;
            var field = BuildValue(node.Expression);
            switch (node.Member.Name)
            {
                case "Day":
                    result = new BsonDocument("$dayOfMonth", field);
                    return true;
                case "DayOfWeek":
                    // The server's day of week values are 1 greater than
                    // .NET's DayOfWeek enum values
                    result = new BsonDocument("$subtract", new BsonArray
                        {
                            new BsonDocument("$dayOfWeek", field),
                            (BsonInt32)1
                        });
                    return true;
                case "DayOfYear":
                    result = new BsonDocument("$dayOfYear", field);
                    return true;
                case "Hour":
                    result = new BsonDocument("$hour", field);
                    return true;
                case "Millisecond":
                    result = new BsonDocument("$millisecond", field);
                    return true;
                case "Minute":
                    result = new BsonDocument("$minute", field);
                    return true;
                case "Month":
                    result = new BsonDocument("$month", field);
                    return true;
                case "Second":
                    result = new BsonDocument("$second", field);
                    return true;
                case "Year":
                    result = new BsonDocument("$year", field);
                    return true;
            }

            return false;
        }

        private bool TryBuildHashSetMethodCall(MethodCallExpression node, out BsonValue result)
        {
            result = null;
            switch (node.Method.Name)
            {
                case "IsSubsetOf":
                    result = new BsonDocument("$setIsSubset", new BsonArray(new[] 
                        { 
                            BuildValue(node.Object), 
                            BuildValue(node.Arguments[0])
                        }));
                    return true;
                case "SetEquals":
                    result = new BsonDocument("$setEquals", new BsonArray(new[] 
                        { 
                            BuildValue(node.Object), 
                            BuildValue(node.Arguments[0])
                        }));
                    return true;
            }

            return false;
        }

        private bool TryBuildLinqMethodCall(MethodCallExpression node, out BsonValue result)
        {
            result = null;
            switch (node.Method.Name)
            {
                case "All":
                    if (TryBuildMap(node, out result))
                    {
                        result = new BsonDocument("$allElementsTrue", result);
                        return true;
                    }
                    break;
                case "Any":
                    if (node.Arguments.Count == 1)
                    {
                        result = new BsonDocument("$gt", new BsonArray(new BsonValue[] 
                            {
                                new BsonDocument("$size", BuildValue(node.Arguments[0])),
                                0
                            }));
                        return true;
                    }
                    else if (TryBuildMap(node, out result))
                    {
                        result = new BsonDocument("$anyElementTrue", result);
                        return true;
                    }
                    break;
                case "Count":
                case "LongCount":
                    if (node.Arguments.Count == 1)
                    {
                        result = new BsonDocument("$size", BuildValue(node.Arguments[0]));
                        return true;
                    }
                    break;
                case "Except":
                    if (node.Arguments.Count == 2)
                    {
                        result = new BsonDocument("$setDifference", new BsonArray(new[] 
                            { 
                                BuildValue(node.Arguments[0]), 
                                BuildValue(node.Arguments[1]) 
                            }));
                        return true;
                    }
                    break;
                case "Intersect":
                    if (node.Arguments.Count == 2)
                    {
                        result = new BsonDocument("$setIntersection", new BsonArray(new[] 
                            { 
                                BuildValue(node.Arguments[0]), 
                                BuildValue(node.Arguments[1]) 
                            }));
                        return true;
                    }
                    break;
                case "Select":
                    if (TryBuildMap(node, out result))
                    {
                        return true;
                    }
                    break;
                case "Union":
                    if (node.Arguments.Count == 2)
                    {
                        result = new BsonDocument("$setUnion", new BsonArray(new[] 
                            { 
                                BuildValue(node.Arguments[0]), 
                                BuildValue(node.Arguments[1])
                            }));
                        return true;
                    }
                    break;
            }

            return false;
        }

        private bool TryBuildMap(MethodCallExpression node, out BsonValue result)
        {
            result = null;
            var sourceSerializationExpression = node.Arguments[0] as ISerializationExpression;
            if (sourceSerializationExpression != null)
            {
                var lambda = ExtensionExpressionVisitor.GetLambda(node.Arguments[1]);
                if (lambda.Body is ISerializationExpression)
                {
                    result = BuildValue(lambda.Body);
                    return true;
                }

                var inputValue = BuildValue(node.Arguments[0]);
                var asValue = lambda.Parameters[0].Name;

                // HACK: need to add a leading $ sign to the replacement because of how we resolve values.
                var body = FieldNameReplacer.Replace(lambda.Body, sourceSerializationExpression.SerializationInfo.ElementName, "$" + asValue);
                var inValue = BuildValue(body);

                result = new BsonDocument("$map", new BsonDocument
                            {
                                { "input", inputValue },
                                { "as", asValue },
                                { "in", inValue }
                            });
                return true;
            }

            return false;
        }

        private bool TryBuildStaticStringMethodCall(MethodCallExpression node, out BsonValue result)
        {
            result = null;
            switch (node.Method.Name)
            {
                case "IsNullOrEmpty":
                    var field = BuildValue(node.Arguments[0]);
                    result = new BsonDocument("$or",
                        new BsonArray
                        {
                            new BsonDocument("$eq", new BsonArray { field, BsonNull.Value }),
                            new BsonDocument("$eq", new BsonArray { field, BsonString.Empty })
                        });
                    return true;
            }

            return false;
        }

        private bool TryBuildStringMethodCall(MethodCallExpression node, out BsonValue result)
        {
            result = null;
            var field = BuildValue(node.Object);
            switch (node.Method.Name)
            {
                case "Equals":
                    if (node.Arguments.Count == 2 && node.Arguments[1].NodeType == ExpressionType.Constant)
                    {
                        var comparisonType = (StringComparison)((ConstantExpression)node.Arguments[1]).Value;
                        switch (comparisonType)
                        {
                            case StringComparison.OrdinalIgnoreCase:
                                result = new BsonDocument("$eq",
                                    new BsonArray(new BsonValue[] 
                                        {
                                            new BsonDocument("$strcasecmp", new BsonArray(new[] { field, BuildValue(node.Arguments[0]) })),
                                            0
                                        }));
                                return true;
                            case StringComparison.Ordinal:
                                result = new BsonDocument("$eq", new BsonArray(new[] { field, BuildValue(node.Arguments[0]) }));
                                return true;
                            default:
                                throw new NotSupportedException("Only Ordinal and OrdinalIgnoreCase are supported for string comparisons.");
                        }
                    }
                    break;
                case "Substring":
                    if (node.Arguments.Count == 2)
                    {
                        result = new BsonDocument("$substr", new BsonArray(new[] 
                            { 
                                field, 
                                BuildValue(node.Arguments[0]), 
                                BuildValue(node.Arguments[1])
                            }));
                        return true;
                    }
                    break;
                case "ToLower":
                case "ToLowerInvariant":
                    if (node.Arguments.Count == 0)
                    {
                        result = new BsonDocument("$toLower", field);
                        return true;
                    }
                    break;
                case "ToUpper":
                case "ToUpperInvariant":
                    if (node.Arguments.Count == 0)
                    {
                        result = new BsonDocument("$toUpper", field);
                        return true;
                    }
                    break;
            }

            return false;
        }

        private class FieldNameReplacer : ExtensionExpressionVisitor
        {
            public static Expression Replace(Expression node, string oldName, string newName)
            {
                var replacer = new FieldNameReplacer(oldName, newName);
                return replacer.Visit(node);
            }

            private readonly string _oldName;
            private readonly string _newName;

            private FieldNameReplacer(string oldName, string newName)
            {
                _oldName = oldName;
                _newName = newName;
            }

            protected internal override Expression VisitSerialization(SerializationExpression node)
            {
                if (node.SerializationInfo.ElementName != null && node.SerializationInfo.ElementName.StartsWith(_oldName))
                {
                    return new SerializationExpression(
                        node.Expression,
                        node.SerializationInfo.WithNewName(GetReplacementName(node.SerializationInfo.ElementName)));
                }

                return base.VisitSerialization(node);
            }

            private string GetReplacementName(string elementName)
            {
                var suffix = elementName.Substring(_oldName.Length);
                return _newName + suffix;
            }
        }
    }
}
