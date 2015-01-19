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
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Expressions;
using MongoDB.Driver.Linq.Processors;

namespace MongoDB.Driver.Linq.Translators
{
    internal class AggregateProjectionTranslator
    {
        public static ProjectionInfo<TResult> TranslateProject<TDocument, TResult>(Expression<Func<TDocument, TResult>> projector, IBsonSerializer<TDocument> parameterSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            if (projector.Body.NodeType != ExpressionType.New)
            {
                throw new NotSupportedException("Must use an anonymous type for constructing $project pipeline operators.");
            }

            var binder = new SerializationInfoBinder(BsonSerializer.SerializerRegistry);
            var boundExpression = BindSerializationInfo(binder, projector, parameterSerializer);
            var projectionSerializer = (IBsonSerializer<TResult>)SerializerBuilder.Build(boundExpression, serializerRegistry);
            var projection = ProjectionBuilder.Build(boundExpression).AsBsonDocument;

            if (!projection.Contains("_id"))
            {
                projection.Add("_id", 0); // we don't want the id back unless we asked for it...
            }

            return new ProjectionInfo<TResult>(projection, projectionSerializer);
        }

        public static ProjectionInfo<TResult> TranslateGroup<TKey, TDocument, TResult>(Expression<Func<TDocument, TKey>> idProjector, Expression<Func<IGrouping<TKey, TDocument>, TResult>> groupProjector, IBsonSerializer<TDocument> parameterSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            if (groupProjector.Body.NodeType != ExpressionType.New)
            {
                throw new NotSupportedException("Must use an anonymous type for constructing $group pipeline operators.");
            }

            var keyBinder = new SerializationInfoBinder(serializerRegistry);
            var boundKeyExpression = BindSerializationInfo(keyBinder, idProjector, parameterSerializer);

            var groupBinder = new GroupSerializationInfoBinder(BsonSerializer.SerializerRegistry);
            groupBinder.RegisterMemberReplacement(typeof(IGrouping<TKey, TDocument>).GetProperty("Key"), boundKeyExpression);
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

            return new ProjectionInfo<TResult>(projection, projectionSerializer);
        }

        private static Expression BindSerializationInfo(SerializationInfoBinder binder, LambdaExpression node, IBsonSerializer parameterSerializer)
        {
            var parameterSerializationInfo = new BsonSerializationInfo(null, parameterSerializer, parameterSerializer.ValueType);
            var parameterExpression = new DocumentExpression(node.Parameters[0], parameterSerializationInfo, false);
            binder.RegisterParameterReplacement(node.Parameters[0], parameterExpression);
            return binder.Bind(node.Body);
        }

        private class ProjectionBuilder
        {
            public static BsonValue Build(Expression projector)
            {
                var builder = new ProjectionBuilder();
                return builder.ResolveValue(projector);
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
                    case ExpressionType.Call:
                        return BuildMethodCall((MethodCallExpression)node);
                    case ExpressionType.Coalesce:
                        return BuildOperation((BinaryExpression)node, "$ifNull", false);
                    case ExpressionType.Conditional:
                        return BuildConditional((ConditionalExpression)node);
                    case ExpressionType.Constant:
                        return BsonValue.Create(((ConstantExpression)node).Value);
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
                            }
                        }
                        break;
                }

                var message = string.Format("{0} is an unsupported node type in an $project or $group pipeline operator.", node.NodeType);
                throw new NotSupportedException(message);
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
                    case AggregationType.Average:
                        return new BsonDocument("$avg", ResolveValue(node.Argument));
                    case AggregationType.First:
                        return new BsonDocument("$first", ResolveValue(node.Argument));
                    case AggregationType.Last:
                        return new BsonDocument("$last", ResolveValue(node.Argument));
                    case AggregationType.Max:
                        return new BsonDocument("$max", ResolveValue(node.Argument));
                    case AggregationType.Min:
                        return new BsonDocument("$min", ResolveValue(node.Argument));
                    case AggregationType.Sum:
                        return new BsonDocument("$sum", ResolveValue(node.Argument));
                }

                // we should never ever get here.
                throw new MongoInternalException("Unrecognized aggregation type.");
            }

            private BsonValue BuildConditional(ConditionalExpression node)
            {
                var condition = ResolveValue(node.Test);
                var truePart = ResolveValue(node.IfTrue);
                var falsePart = ResolveValue(node.IfFalse);

                return new BsonDocument("$cond", new BsonArray(new[] { condition, truePart, falsePart }));
            }

            private BsonValue BuildDateTime(MemberExpression node)
            {
                var field = ResolveValue(node.Expression);
                switch (node.Member.Name)
                {
                    case "Day":
                        return new BsonDocument("$dayOfMonth", field);
                    case "DayOfWeek":
                        // The server's day of week values are 1 greater than
                        // .NET's DayOfWeek enum values
                        return new BsonDocument("$subtract", new BsonArray
                        {
                            new BsonDocument("$dayOfWeek", field),
                            new BsonInt32(1)
                        });
                    case "DayOfYear":
                        return new BsonDocument("$dayOfYear", field);
                    case "Hour":
                        return new BsonDocument("$hour", field);
                    case "Millisecond":
                        return new BsonDocument("$millisecond", field);
                    case "Minute":
                        return new BsonDocument("$minute", field);
                    case "Month":
                        return new BsonDocument("$month", field);
                    case "Second":
                        return new BsonDocument("$second", field);
                    case "Year":
                        return new BsonDocument("$year", field);
                }

                var message = string.Format("{0} is an unsupported DateTime member in an $project or $group pipeline operator.", node.Member.Name);
                throw new NotSupportedException(message);
            }

            private BsonValue BuildMemberAccess(MemberExpression node)
            {
                if (node.Expression.Type == typeof(DateTime))
                {
                    return BuildDateTime(node);
                }

                var message = string.Format("Members of {0} are not supported in a $project or $group pipeline operator.", node.Expression.Type);
                throw new NotSupportedException(message);
            }

            private BsonValue BuildMethodCall(MethodCallExpression node)
            {
                if (MongoExpressionVisitor.IsLinqMethod(node, "Select"))
                {
                    var serializationExpression = node.Arguments[0] as IBsonSerializationInfoExpression;
                    if (serializationExpression != null)
                    {
                        var body = MongoExpressionVisitor.GetLambda(node.Arguments[1]).Body;
                        return ResolveValue(body);
                    }
                }

                if (node.Type == typeof(string))
                {
                    return BuildStringMethodCall(node);
                }

                var message = string.Format("{0} is an unsupported method in a $project or $group pipeline operator.", node.Method.Name);
                throw new NotSupportedException(message);
            }

            private BsonValue BuildNew(NewExpression node)
            {
                BsonDocument doc = new BsonDocument();
                var parameters = node.Constructor.GetParameters();
                for (int i = 0; i < node.Arguments.Count; i++)
                {
                    var value = ResolveValue(node.Arguments[i]);
                    doc.Add(parameters[i].Name, value);
                }

                return doc;
            }

            private BsonValue BuildNot(UnaryExpression node)
            {
                var operand = ResolveValue(node.Operand);
                if (operand.IsBsonDocument)
                {
                    operand = new BsonArray().Add(operand);
                }
                return new BsonDocument("$not", operand);
            }

            private BsonValue BuildOperation(BinaryExpression node, string op, bool canBeFlattened)
            {
                var left = ResolveValue(node.Left);
                var right = ResolveValue(node.Right);

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

            private BsonValue BuildStringMethodCall(MethodCallExpression node)
            {
                var field = ResolveValue(node.Object);
                switch (node.Method.Name)
                {
                    case "Substring":
                        if (node.Arguments.Count == 2)
                        {
                            var start = ResolveValue(node.Arguments[0]);
                            var end = ResolveValue(node.Arguments[1]);
                            return new BsonDocument("$substr", new BsonArray(new[] { field, start, end }));
                        }
                        break;
                    case "ToLower":
                    case "ToLowerInvariant":
                        if (node.Arguments.Count == 0)
                        {
                            return new BsonDocument("$toLower", field);
                        }
                        break;
                    case "ToUpper":
                    case "ToUpperInvariant":
                        if (node.Arguments.Count == 0)
                        {
                            return new BsonDocument("$toUpper", field);
                        }
                        break;
                }

                var message = string.Format("{0} is an unsupported String method in an $project or $group pipeline operator.", node.Method.Name);
                throw new NotSupportedException(message);
            }

            private BsonValue ResolveValue(Expression node)
            {
                var serializationExpression = node as IBsonSerializationInfoExpression;
                if (serializationExpression != null)
                {
                    return "$" + serializationExpression.SerializationInfo.ElementName;
                }

                return BuildValue(node);
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
                if (node is IBsonSerializationInfoExpression)
                {
                    return ((IBsonSerializationInfoExpression)node).SerializationInfo.Serializer;
                }

                if (node.NodeType == ExpressionType.New)
                {
                    return BuildNew((NewExpression)node);
                }

                return _serializerRegistry.GetSerializer(node.Type);
            }

            protected IBsonSerializer BuildNew(NewExpression node)
            {
                if (node.Members != null)
                {
                    return BuildSerializerForAnonymousType(node);
                }

                throw new NotSupportedException("Only new anomymous type expressions are allowed in $project or $group pipeline operators.");
            }

            private IBsonSerializer BuildSerializerForAnonymousType(NewExpression node)
            {
                // We are building a serializer specifically for an anonymous type based 
                // on serialization information collected from other serializers.
                // We cannot cache this because the compiler reuses the same anonymous type
                // definition in different contexts as long as they are structurally equatable.
                // As such, it might be that two different queries projecting the same shape
                // might need to be deserialized differently.
                var classMapType = typeof(BsonClassMap<>).MakeGenericType(node.Type);
                BsonClassMap classMap = (BsonClassMap)Activator.CreateInstance(classMapType);

                var properties = node.Type.GetProperties();
                var parameterToPropertyMap = from parameter in node.Constructor.GetParameters()
                                             join property in properties on parameter.Name equals property.Name
                                             select new { Parameter = parameter, Property = property };

                foreach (var parameterToProperty in parameterToPropertyMap)
                {
                    var argument = node.Arguments[parameterToProperty.Parameter.Position];
                    var field = argument as FieldExpression;
                    if (field == null)
                    {
                        var serializer = Build(argument);
                        var serializationInfo = new BsonSerializationInfo(parameterToProperty.Property.Name, serializer, parameterToProperty.Property.PropertyType);
                        field = new FieldExpression(
                            node.Arguments[parameterToProperty.Parameter.Position],
                            serializationInfo,
                            true);
                    }

                    classMap.MapMember(parameterToProperty.Property)
                        .SetSerializer(field.SerializationInfo.Serializer)
                        .SetElementName(parameterToProperty.Property.Name);

                    //TODO: Need to set default value as well...
                }

                // Anonymous types are immutable and have all their values passed in via a ctor.
                classMap.MapConstructor(node.Constructor, properties.Select(x => x.Name).ToArray());
                classMap.Freeze();

                var serializerType = typeof(BsonClassMapSerializer<>).MakeGenericType(node.Type);
                return (IBsonSerializer)Activator.CreateInstance(serializerType, classMap);
            }
        }
    }
}
