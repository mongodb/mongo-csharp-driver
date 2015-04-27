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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Expressions;
using MongoDB.Driver.Linq.Processors;
using MongoDB.Driver.Support;

namespace MongoDB.Driver.Linq.Translators
{
    internal class FindProjectionTranslator : ExtensionExpressionVisitor
    {
        public static RenderedProjectionDefinition<TProjection> Translate<TDocument, TProjection>(Expression<Func<TDocument, TProjection>> projector, IBsonSerializer<TDocument> parameterSerializer)
        {
            var parameterSerializationInfo = new BsonSerializationInfo(null, parameterSerializer, parameterSerializer.ValueType);
            var parameterExpression = new SerializationExpression(projector.Parameters[0], parameterSerializationInfo);
            var binder = new SerializationInfoBinder(BsonSerializer.SerializerRegistry);
            binder.RegisterParameterReplacement(projector.Parameters[0], parameterExpression);
            var normalizedBody = Transformer.Transform(projector.Body);
            var evaluatedBody = PartialEvaluator.Evaluate(normalizedBody);
            var boundExpression = binder.Bind(evaluatedBody);
            var candidateFields = SerializationExpressionGatherer.Gather(boundExpression);

            var fields = GetUniqueFieldsByHierarchy(candidateFields);

            var serializationInfo = fields.Select(x => x.SerializationInfo).ToList();

            var replacementParameter = Expression.Parameter(typeof(ProjectedObject), "document");

            var translator = new FindProjectionTranslator(projector.Parameters[0], replacementParameter, fields);
            var newProjector = Expression.Lambda<Func<ProjectedObject, TProjection>>(
                translator.Visit(boundExpression),
                replacementParameter);

            BsonDocument projectionDocument;
            IBsonSerializer<TProjection> serializer;
            if (translator._fullDocument)
            {
                projectionDocument = null;
                serializer = new ProjectingDeserializer<TDocument, TProjection>(parameterSerializer, projector.Compile());
            }
            else
            {
                projectionDocument = GetProjectionDocument(serializationInfo);
                var projectedObjectSerializer = new ProjectedObjectDeserializer(serializationInfo);
                serializer = new ProjectingDeserializer<ProjectedObject, TProjection>(projectedObjectSerializer, newProjector.Compile());
            }

            return new RenderedProjectionDefinition<TProjection>(projectionDocument, serializer);
        }

        private readonly IReadOnlyList<SerializationExpression> _fields;
        private bool _fullDocument;
        private readonly ParameterExpression _originalParameter;
        private readonly ParameterExpression _replacementParameter;

        private FindProjectionTranslator(ParameterExpression originalParameter, ParameterExpression replacementParameter, IReadOnlyList<SerializationExpression> fields)
        {
            _originalParameter = originalParameter;
            _replacementParameter = replacementParameter;
            _fields = fields;
        }

        protected internal override Expression VisitSerialization(SerializationExpression node)
        {
            if (!_fields.Any(x => x.SerializationInfo.ElementName == node.SerializationInfo.ElementName
                && x.SerializationInfo.NominalType.Equals(node.SerializationInfo.NominalType)))
            {
                return Visit(node.Expression);
            }

            return Expression.Call(
                _replacementParameter,
                "GetValue",
                new[] { node.Type },
                Expression.Constant(node.SerializationInfo.ElementName),
                Expression.Constant(node.SerializationInfo.NominalType.GetDefaultValue(), typeof(object)));
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (!IsLinqMethod(node, "Select", "SelectMany"))
            {
                return base.VisitMethodCall(node);
            }

            var source = node.Arguments[0] as SerializationExpression;
            if (source != null && !_fields.Any(x => x.SerializationInfo.ElementName == source.SerializationInfo.ElementName))
            {
                // We are projecting off an embedded array, but we have selected the entire
                // array and not just values within it.
                var selector = (LambdaExpression)Visit((LambdaExpression)node.Arguments[1]);
                var nestedParameter = Expression.Parameter(_replacementParameter.Type, selector.Parameters[0].Name);
                var nestedBody = new ProjectedObjectFieldReplacer().Replace(selector.Body, source.SerializationInfo.ElementName, nestedParameter);

                var newSourceType = typeof(IEnumerable<>).MakeGenericType(nestedParameter.Type);
                var newSource =
                    Expression.Call(
                        typeof(Enumerable),
                        "Cast",
                        new[] { typeof(ProjectedObject) },
                        Expression.Call(
                            _replacementParameter,
                            "GetValue",
                            new[] { typeof(IEnumerable<object>) },
                            Expression.Constant(source.SerializationInfo.ElementName),
                            Expression.Constant(newSourceType.GetDefaultValue(), typeof(object))));

                return Expression.Call(
                    typeof(Enumerable),
                    node.Method.Name,
                    new Type[] { nestedParameter.Type, node.Method.GetGenericArguments()[1] },
                    newSource,
                    Expression.Lambda(
                        nestedBody,
                        nestedParameter));
            }

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            // if we have made it all the way down to the original parameter,
            // it means that we have projected the original entity and have
            // no need to actually project anything server side.
            if (node == _originalParameter)
            {
                _fullDocument = true;
            }

            return base.VisitParameter(node);
        }

        private static BsonDocument GetProjectionDocument(IEnumerable<BsonSerializationInfo> used)
        {
            var includeId = false;
            var document = new BsonDocument();
            foreach (var u in used)
            {
                if (u.ElementName == "_id")
                {
                    includeId = true;
                }
                document.Add(u.ElementName, 1);
            }

            if (!includeId)
            {
                document.Add("_id", 0);
            }
            return document;
        }

        private static IReadOnlyList<SerializationExpression> GetUniqueFieldsByHierarchy(IEnumerable<SerializationExpression> usedFields)
        {
            // we want to leave out subelements when the parent element exists
            // for instance, if we have asked for both "d" and "d.e", we only want to send { "d" : 1 } to the server
            // 1) group all the used fields by their element name.
            // 2) order them by their element name in ascending order
            // 3) if any groups are prefixed by the current groups element, then skip it.

            var uniqueFields = new List<SerializationExpression>();
            var skippedFields = new List<string>();
            var referenceGroups = new Queue<IGrouping<string, SerializationExpression>>(usedFields.GroupBy(x => x.SerializationInfo.ElementName).OrderBy(x => x.Key));
            while (referenceGroups.Count > 0)
            {
                var referenceGroup = referenceGroups.Dequeue();
                if (!skippedFields.Contains(referenceGroup.Key))
                {
                    var hierarchicalReferenceGroups = referenceGroups.Where(x => x.Key.StartsWith(referenceGroup.Key));
                    uniqueFields.AddRange(referenceGroup);
                    skippedFields.AddRange(hierarchicalReferenceGroups.Select(x => x.Key));
                }
            }

            return uniqueFields.GroupBy(x => x.SerializationInfo.ElementName).Select(x => x.First()).ToList();
        }

        private class SerializationExpressionGatherer : ExtensionExpressionVisitor
        {
            public static IReadOnlyList<SerializationExpression> Gather(Expression node)
            {
                var gatherer = new SerializationExpressionGatherer();
                gatherer.Visit(node);
                return gatherer._serializationExpressions;
            }

            private List<SerializationExpression> _serializationExpressions;

            private SerializationExpressionGatherer()
            {
                _serializationExpressions = new List<SerializationExpression>();
            }

            protected internal override Expression VisitSerialization(SerializationExpression node)
            {
                if (node.SerializationInfo.ElementName != null)
                {
                    _serializationExpressions.Add(node);
                }
                return node;
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (!IsLinqMethod(node, "Select", "SelectMany"))
                {
                    return base.VisitMethodCall(node);
                }

                var source = node.Arguments[0] as SerializationExpression;
                if (source != null)
                {
                    var fields = SerializationExpressionGatherer.Gather(node.Arguments[1]);
                    if (fields.Any(x => x.SerializationInfo.ElementName.StartsWith(source.SerializationInfo.ElementName)))
                    {
                        _serializationExpressions.AddRange(fields);
                        return node;
                    }
                }

                return base.VisitMethodCall(node);
            }
        }

        /// <summary>
        /// This guy is going to replace calls like store.GetValue("d.y") with nestedStore.GetValue("y").
        /// </summary>
        private class ProjectedObjectFieldReplacer : ExtensionExpressionVisitor
        {
            private string _keyPrefix;
            private Expression _source;

            public Expression Replace(Expression node, string keyPrefix, Expression source)
            {
                _keyPrefix = keyPrefix;
                _source = source;
                return Visit(node);
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node.Object == null || node.Object.Type != typeof(ProjectedObject) || node.Method.Name != "GetValue")
                {
                    return base.VisitMethodCall(node);
                }

                var currentKey = (string)((ConstantExpression)node.Arguments[0]).Value;

                if (!currentKey.StartsWith(_keyPrefix))
                {
                    return base.VisitMethodCall(node);
                }

                var newElementName = currentKey;
                if (currentKey.Length > _keyPrefix.Length)
                {
                    newElementName = currentKey.Remove(0, _keyPrefix.Length + 1);
                }

                var defaultValue = ((ConstantExpression)node.Arguments[1]).Value;
                return Expression.Call(
                    _source,
                    "GetValue",
                    new[] { node.Type },
                    Expression.Constant(newElementName),
                    Expression.Constant(defaultValue, typeof(object)));
            }
        }
    }
}