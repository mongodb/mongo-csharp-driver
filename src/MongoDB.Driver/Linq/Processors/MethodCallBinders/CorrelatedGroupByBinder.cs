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
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Expressions;

namespace MongoDB.Driver.Linq.Processors.MethodCallBinders
{
    internal class CorrelatedGroupByBinder : IMethodCallBinder
    {
        public Expression Bind(ProjectionExpression projection, ProjectionBindingContext context, MethodCallExpression node, IEnumerable<Expression> arguments)
        {
            var id = BindId(projection, context, arguments.Single());

            var iGroupingType = typeof(IGrouping<,>).MakeGenericType(id.Type, projection.Projector.Type);
            var group = new CorrelatedGroupByExpression(
                Guid.NewGuid(),
                typeof(IEnumerable<>).MakeGenericType(iGroupingType),
                projection.Source,
                id,
                Enumerable.Empty<Expression>());

            var groupingType = typeof(Grouping<,>).MakeGenericType(id.Type, projection.Projector.Type);
            Expression selector = Expression.Convert(
                Expression.New(
                    groupingType.GetConstructors()[0],
                    id),
                iGroupingType);

            var projector = BuildProjector(projection, context, id, selector);

            context.GroupMap.Add(projector, group.CorrelationId);

            return new ProjectionExpression(group, projector);
        }

        private GroupIdExpression BindId(ProjectionExpression projection, ProjectionBindingContext context, Expression node)
        {
            var lambda = ExtensionExpressionVisitor.GetLambda(node);
            var binder = new AccumulatorBinder(context.GroupMap, context.SerializerRegistry);
            binder.RegisterParameterReplacement(lambda.Parameters[0], projection.Projector);
            var selector = binder.Bind(lambda.Body);

            if (!(selector is ISerializationExpression))
            {
                var serializer = SerializerBuilder.Build(selector, context.SerializerRegistry);
                selector = new SerializationExpression(
                    selector,
                    new BsonSerializationInfo(null, serializer, serializer.ValueType));
            }

            return new GroupIdExpression(selector, ((ISerializationExpression)selector).SerializationInfo);
        }

        private SerializationExpression BuildProjector(ProjectionExpression projection, ProjectionBindingContext context, GroupIdExpression id, Expression selector)
        {
            BsonSerializationInfo projectorSerializationInfo;
            var projectorSerializationExpression = projection.Projector as ISerializationExpression;
            if (projectorSerializationExpression != null)
            {
                projectorSerializationInfo = projectorSerializationExpression.SerializationInfo;
            }
            else
            {
                var projectorSerializer = context.SerializerRegistry.GetSerializer(projection.Projector.Type);
                projectorSerializationInfo = new BsonSerializationInfo(
                    null,
                    projectorSerializer,
                    projectorSerializer.ValueType);
            }

            var serializerType = typeof(GroupingDeserializer<,>).MakeGenericType(id.Type, projection.Projector.Type);
            var serializer = (IBsonSerializer)Activator.CreateInstance(serializerType, id.SerializationInfo, projectorSerializationInfo);
            var info = new BsonSerializationInfo(null, serializer, serializer.ValueType);
            var projector = new SerializationExpression(selector, info);
            return projector;
        }

        private class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
        {
            // private fields
            private readonly TKey _key;

            // constructors
            public Grouping(TKey key)
            {
                _key = key;
            }

            // public properties
            public TKey Key
            {
                get { return _key; }
            }

            // public methods
            public IEnumerator<TElement> GetEnumerator()
            {
                yield break;
            }

            // explicit methods
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                yield break;
            }
        }

        private class GroupingDeserializer<TKey, TElement> : SerializerBase<IGrouping<TKey, TElement>>, IBsonDocumentSerializer, IBsonArraySerializer
        {
            private readonly BsonSerializationInfo _elementSerializationInfo;
            private readonly BsonSerializationInfo _idSerializationInfo;

            public GroupingDeserializer(BsonSerializationInfo idSerializationInfo, BsonSerializationInfo elementSerializationInfo)
            {
                _idSerializationInfo = idSerializationInfo;
                _elementSerializationInfo = elementSerializationInfo;
            }

            public override IGrouping<TKey, TElement> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                var reader = context.Reader;
                TKey key = default(TKey);
                reader.ReadStartDocument();
                while (context.Reader.ReadBsonType() != 0)
                {
                    var elementName = reader.ReadName();
                    if (elementName == "_id")
                    {
                        key = (TKey)_idSerializationInfo.Serializer.Deserialize(context);
                    }
                    else
                    {
                        reader.SkipValue();
                    }
                }
                reader.ReadEndDocument();
                return new Grouping<TKey, TElement>(key);
            }

            public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
            {
                if (memberName == "Key")
                {
                    serializationInfo = _idSerializationInfo.WithNewName("_id");
                    return true;
                }

                serializationInfo = null;
                return false;
            }

            public bool TryGetItemSerializationInfo(out BsonSerializationInfo serializationInfo)
            {
                serializationInfo = _elementSerializationInfo;
                return true;
            }
        }
    }
}
