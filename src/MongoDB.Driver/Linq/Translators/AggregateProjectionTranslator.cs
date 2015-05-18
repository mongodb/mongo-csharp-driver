/* Copyright 2010-2014 MongoDB Inc.
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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Expressions;
using MongoDB.Driver.Linq.Processors;
using MongoDB.Driver.Linq.Utils;

namespace MongoDB.Driver.Linq.Translators
{
    internal class AggregateProjectionTranslator
    {
        public static RenderedProjectionDefinition<TResult> TranslateProject<TDocument, TResult>(Expression<Func<TDocument, TResult>> projector, IBsonSerializer<TDocument> parameterSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var binder = new SerializationInfoBinder(BsonSerializer.SerializerRegistry);
            var boundExpression = BindSerializationInfo(binder, projector, parameterSerializer);
            var projectionSerializer = (IBsonSerializer<TResult>)SerializerBuilder.Build(boundExpression, serializerRegistry);
            var projection = ProjectionBuilder.Build(boundExpression).AsBsonDocument;

            if (!projection.Contains("_id"))
            {
                projection.Add("_id", 0); // we don't want the id back unless we asked for it...
            }

            return new RenderedProjectionDefinition<TResult>(projection, projectionSerializer);
        }

        public static RenderedProjectionDefinition<TResult> TranslateGroup<TKey, TDocument, TResult>(Expression<Func<TDocument, TKey>> idProjector, Expression<Func<IGrouping<TKey, TDocument>, TResult>> groupProjector, IBsonSerializer<TDocument> parameterSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var keyBinder = new SerializationInfoBinder(serializerRegistry);
            var boundKeyExpression = BindSerializationInfo(keyBinder, idProjector, parameterSerializer);
            if (!(boundKeyExpression is ISerializationExpression))
            {
                var keySerializer = SerializerBuilder.Build(boundKeyExpression, serializerRegistry);
                boundKeyExpression = new SerializationExpression(
                    boundKeyExpression,
                    new BsonSerializationInfo(null, keySerializer, typeof(TKey)));
            }

            var idExpression = new IdExpression(boundKeyExpression, ((ISerializationExpression)boundKeyExpression).SerializationInfo);

            var groupBinder = new GroupSerializationInfoBinder(BsonSerializer.SerializerRegistry);
            groupBinder.RegisterMemberReplacement(typeof(IGrouping<TKey, TDocument>).GetProperty("Key"), idExpression);
            var groupSerializer = new ArraySerializer<TDocument>(parameterSerializer);
            var boundGroupExpression = BindSerializationInfo(groupBinder, groupProjector, groupSerializer);
            var projectionSerializer = (IBsonSerializer<TResult>)SerializerBuilder.Build(boundGroupExpression, serializerRegistry);
            var projection = ProjectionBuilder.Build(boundGroupExpression).AsBsonDocument;

            // must have an "_id" in a group document
            if (!projection.Contains("_id"))
            {
                var idProjection = ProjectionBuilder.Build(boundKeyExpression);
                projection.InsertAt(0, new BsonElement("_id", idProjection));
            }

            return new RenderedProjectionDefinition<TResult>(projection, projectionSerializer);
        }

        private static Expression BindSerializationInfo(SerializationInfoBinder binder, LambdaExpression node, IBsonSerializer parameterSerializer)
        {
            var parameterSerializationInfo = new BsonSerializationInfo(null, parameterSerializer, parameterSerializer.ValueType);
            var parameterExpression = new SerializationExpression(node.Parameters[0], parameterSerializationInfo);
            binder.RegisterParameterReplacement(node.Parameters[0], parameterExpression);
            var normalizedBody = Normalizer.Normalize(node.Body);
            var evaluatedBody = PartialEvaluator.Evaluate(normalizedBody);
            return binder.Bind(evaluatedBody);
        }

        private class ProjectionBuilder
        {
            public static BsonValue Build(Expression projector)
            {
                var builder = new ProjectionBuilder();
                return builder.BuildValue(projector);
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
                        var mongoExpression = node as MongoExpression;
                        if (mongoExpression != null)
                        {
                            switch (mongoExpression.MongoNodeType)
                            {
                                case MongoExpressionType.Aggregation:
                                    return BuildAggregation((AggregationExpression)node);
                                case MongoExpressionType.Serialization:
                                    return BuildSerialization((SerializationExpression)node);
                            }
                        }
                        else if (node is IdExpression)
                        {
                            return BuildValue(((IdExpression)node).Expression);
                        }
                        break;
                }

                var message = string.Format("{0} is an unsupported node type in an $project or $group pipeline operator.", node.NodeType);
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

            private BsonValue BuildAggregation(AggregationExpression node)
            {
                switch (node.AggregationType)
                {
                    case AggregationType.AddToSet:
                        return new BsonDocument("$addToSet", BuildValue(node.Argument));
                    case AggregationType.Average:
                        return new BsonDocument("$avg", BuildValue(node.Argument));
                    case AggregationType.First:
                        return new BsonDocument("$first", BuildValue(node.Argument));
                    case AggregationType.Last:
                        return new BsonDocument("$last", BuildValue(node.Argument));
                    case AggregationType.Max:
                        return new BsonDocument("$max", BuildValue(node.Argument));
                    case AggregationType.Min:
                        return new BsonDocument("$min", BuildValue(node.Argument));
                    case AggregationType.Push:
                        return new BsonDocument("$push", BuildValue(node.Argument));
                    case AggregationType.Sum:
                        return new BsonDocument("$sum", BuildValue(node.Argument));
                }

                // we should never ever get here.
                throw new MongoInternalException("Unrecognized aggregation type.");
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
                    && (TypeHelper.ImplementsInterface(node.Expression.Type, typeof(ICollection<>))
                        || TypeHelper.ImplementsInterface(node.Expression.Type, typeof(ICollection)))
                    && node.Member.Name == "Count")
                {
                    return new BsonDocument("$size", BuildValue(node.Expression));
                }

                var message = string.Format("Member {0} of type {1} are not supported in a $project or $group pipeline operator.", node.Member.Name, node.Member.DeclaringType);
                throw new NotSupportedException(message);
            }

            private BsonValue BuildMethodCall(MethodCallExpression node)
            {
                BsonValue result;
                if (MongoExpressionVisitor.IsLinqMethod(node) && TryBuildLinqMethodCall(node, out result))
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
                    && (TypeHelper.ImplementsInterface(node.Object.Type, typeof(IComparable<>))
                        || TypeHelper.ImplementsInterface(node.Object.Type, typeof(IComparable))))
                {
                    return new BsonDocument("$cmp", new BsonArray(new[] { BuildValue(node.Object), BuildValue(node.Arguments[0]) }));
                }

                if (node.Object != null
                    && node.Method.Name == "Equals"
                    && node.Arguments.Count == 1)
                {
                    return new BsonDocument("$eq", new BsonArray(new[] { BuildValue(node.Object), BuildValue(node.Arguments[0]) }));
                }

                var message = string.Format("{0} of type {1} is an unsupported method in a $project or $group pipeline operator.", node.Method.Name, node.Method.DeclaringType);
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
                    if (!hasId && memberMapping.Expression is IdExpression)
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
                            new BsonInt32(1)
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
                    var lambda = MongoExpressionVisitor.GetLambda(node.Arguments[1]);
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
        }

        private class SerializerBuilder
        {
            public static IBsonSerializer Build(Expression node, IBsonSerializerRegistry serializerRegistry)
            {
                var builder = new SerializerBuilder(serializerRegistry);
                return builder.Build(node);
            }

            private IBsonSerializerRegistry _serializerRegistry;

            private SerializerBuilder(IBsonSerializerRegistry serializerRegistry)
            {
                _serializerRegistry = serializerRegistry;
            }

            public IBsonSerializer Build(Expression node)
            {
                if (node is ISerializationExpression)
                {
                    return ((ISerializationExpression)node).SerializationInfo.Serializer;
                }

                switch (node.NodeType)
                {
                    case ExpressionType.MemberInit:
                        return BuildMemberInit((MemberInitExpression)node);
                    case ExpressionType.New:
                        return BuildNew((NewExpression)node);
                    default:
                        return _serializerRegistry.GetSerializer(node.Type);
                }
            }

            protected IBsonSerializer BuildMemberInit(MemberInitExpression node)
            {
                var mapping = ProjectionMapper.Map(node);
                return BuildProjectedSerializer(mapping);
            }

            protected IBsonSerializer BuildNew(NewExpression node)
            {
                var mapping = ProjectionMapper.Map(node);
                return BuildProjectedSerializer(mapping);
            }

            private IBsonSerializer BuildProjectedSerializer(ProjectionMapping mapping)
            {
                // We are building a serializer specifically for a projected type based
                // on serialization information collected from other serializers.
                // We cannot cache this in the serializer registry because the compiler reuses 
                // the same anonymous type definition in different contexts as long as they 
                // are structurally equatable. As such, it might be that two different queries 
                // projecting the same shape might need to be deserialized differently.
                var classMap = BuildClassMap(mapping.Expression.Type, mapping);

                var mappedParameters = mapping.Members
                    .Where(x => x.Parameter != null)
                    .OrderBy(x => x.Parameter.Position)
                    .Select(x => x.Member)
                    .ToList();

                if (mappedParameters.Count > 0)
                {
                    classMap.MapConstructor(mapping.Constructor)
                        .SetArguments(mappedParameters);
                }

                var serializerType = typeof(BsonClassMapSerializer<>).MakeGenericType(mapping.Expression.Type);
                return (IBsonSerializer)Activator.CreateInstance(serializerType, classMap.Freeze());
            }

            private BsonClassMap BuildClassMap(Type type, ProjectionMapping mapping)
            {
                if (type == null || type == typeof(object))
                {
                    return null;
                }

                var baseClassMap = BuildClassMap(type.BaseType, mapping);
                if (baseClassMap != null)
                {
                    baseClassMap.Freeze();
                }
                var classMap = new BsonClassMap(type, baseClassMap);

                foreach (var memberMapping in mapping.Members.Where(x => x.Member.DeclaringType == type))
                {
                    var serializationExpression = memberMapping.Expression as ISerializationExpression;
                    if (serializationExpression == null)
                    {
                        var serializer = Build(memberMapping.Expression);
                        var serializationInfo = new BsonSerializationInfo(
                            memberMapping.Member.Name,
                            serializer,
                            TypeHelper.GetMemberType(memberMapping.Member));
                        serializationExpression = new SerializationExpression(
                            memberMapping.Expression,
                            serializationInfo);
                    }

                    var memberMap = classMap.MapMember(memberMapping.Member)
                        .SetSerializer(serializationExpression.SerializationInfo.Serializer)
                        .SetElementName(memberMapping.Member.Name);

                    if (classMap.IdMemberMap == null && serializationExpression is IdExpression)
                    {
                        classMap.SetIdMember(memberMap);
                    }
                }

                return classMap;
            }
        }

        private class FieldNameReplacer : MongoExpressionVisitor
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

            protected override Expression VisitSerialization(SerializationExpression node)
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

        private class ProjectionMapping
        {
            public ConstructorInfo Constructor;
            public Expression Expression;
            public List<ProjectionMemberMapping> Members;
        }

        private class ProjectionMemberMapping
        {
            public MemberInfo Member;
            public Expression Expression;
            public ParameterInfo Parameter;
        }

        private class ProjectionMapper
        {
            public static ProjectionMapping Map(Expression node)
            {
                var mapper = new ProjectionMapper();
                mapper.Visit(node);
                return new ProjectionMapping
                {
                    Constructor = mapper._constructor,
                    Expression = node,
                    Members = mapper._mappings
                };
            }

            private ConstructorInfo _constructor;
            private List<ProjectionMemberMapping> _mappings;

            private ProjectionMapper()
            {
                _mappings = new List<ProjectionMemberMapping>();
            }

            public void Visit(Expression node)
            {
                switch (node.NodeType)
                {
                    case ExpressionType.MemberInit:
                        VisitMemberInit((MemberInitExpression)node);
                        break;
                    case ExpressionType.New:
                        VisitNew((NewExpression)node);
                        break;
                    default:
                        throw new NotSupportedException("Only new expressions are supported in $project and $group.");
                }
            }

            private void VisitMemberInit(MemberInitExpression node)
            {
                foreach (var memberBinding in node.Bindings)
                {
                    var memberAssignment = memberBinding as MemberAssignment;
                    if (memberAssignment != null)
                    {
                        _mappings.Add(new ProjectionMemberMapping
                        {
                            Expression = memberAssignment.Expression,
                            Member = memberAssignment.Member
                        });
                    }
                    else
                    {
                        throw new NotSupportedException("Only member assignments are supported in a new expression in $project and $group.");
                    }
                }

                VisitNew(node.NewExpression);
            }

            private void VisitNew(NewExpression node)
            {
                _constructor = node.Constructor;

                var type = node.Type;
                foreach (var parameter in node.Constructor.GetParameters())
                {
                    MemberInfo member;
                    if (node.Members != null)
                    {
                        // anonymous types will have this set...
                        member = node.Members[parameter.Position];
                    }
                    else
                    {
                        var members = type.GetMember(
                            parameter.Name,
                            MemberTypes.Field | MemberTypes.Property,
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

                        if (members.Length != 1)
                        {
                            var message = string.Format("Could not find a member match for constructor parameter {0} on type {1}.", parameter.Name, type.Name);
                            throw new NotSupportedException(message);
                        }

                        member = members[0];
                    }

                    _mappings.Add(new ProjectionMemberMapping
                    {
                        Expression = node.Arguments[parameter.Position],
                        Member = member,
                        Parameter = parameter
                    });
                }
            }
        }

        private class IdExpression : Expression, ISerializationExpression
        {
            public IdExpression(Expression expression, BsonSerializationInfo serializationInfo)
            {
                Expression = expression;
                SerializationInfo = serializationInfo;
            }

            public Expression Expression { get; private set; }

            public override ExpressionType NodeType
            {
                get { return ExpressionType.Extension; }
            }

            public BsonSerializationInfo SerializationInfo { get; private set; }

            public override Type Type
            {
                get { return Expression.Type; }
            }
        }
    }
}
