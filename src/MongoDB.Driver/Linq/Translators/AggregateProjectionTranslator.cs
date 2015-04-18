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
    internal static class AggregateProjectionTranslator
    {
        public static RenderedProjectionDefinition<TResult> TranslateProject<TDocument, TResult>(Expression<Func<TDocument, TResult>> projector, IBsonSerializer<TDocument> parameterSerializer, IBsonSerializerRegistry serializerRegistry)
        {
            var binder = new SerializationInfoBinder(BsonSerializer.SerializerRegistry);
            var boundExpression = BindSerializationInfo(binder, projector, parameterSerializer);

            var projectionSerializer = (IBsonSerializer<TResult>)SerializerBuilder.Build(boundExpression, serializerRegistry);
            var projection = TranslateProject(boundExpression);

            return new RenderedProjectionDefinition<TResult>(projection, projectionSerializer);
        }

        public static BsonDocument TranslateProject(Expression expression)
        {
            var value = AggregateLanguageTranslator.Translate(expression);
            var projection = value as BsonDocument;
            if (projection == null)
            {
                projection = new BsonDocument(value.ToString().Substring(1), 1);
            }
            else if (expression.NodeType != ExpressionType.New && expression.NodeType != ExpressionType.MemberInit)
            {
                // this means we are scalar projection
                projection = new BsonDocument("__fld0", value);
            }

            if (!projection.Contains("_id"))
            {
                projection.Add("_id", 0);
            }

            return projection;
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

            var idExpression = new GroupIdExpression(boundKeyExpression, ((ISerializationExpression)boundKeyExpression).SerializationInfo);

            var groupBinder = new AccumulatorBinder(serializerRegistry);
            groupBinder.RegisterMemberReplacement(typeof(IGrouping<TKey, TDocument>).GetProperty("Key"), idExpression);
            var groupSerializer = new ArraySerializer<TDocument>(parameterSerializer);
            var boundGroupExpression = BindSerializationInfo(groupBinder, groupProjector, groupSerializer);
            var projectionSerializer = (IBsonSerializer<TResult>)SerializerBuilder.Build(boundGroupExpression, serializerRegistry);
            var projection = AggregateLanguageTranslator.Translate(boundGroupExpression).AsBsonDocument;

            // must have an "_id" in a group document
            if (!projection.Contains("_id"))
            {
                var idProjection = AggregateLanguageTranslator.Translate(boundKeyExpression);
                projection.InsertAt(0, new BsonElement("_id", idProjection));
            }

            return new RenderedProjectionDefinition<TResult>(projection, projectionSerializer);
        }

        private static Expression BindSerializationInfo(SerializationInfoBinder binder, LambdaExpression node, IBsonSerializer parameterSerializer)
        {
            var parameterSerializationInfo = new BsonSerializationInfo(null, parameterSerializer, parameterSerializer.ValueType);
            var parameterExpression = new SerializationExpression(node.Parameters[0], parameterSerializationInfo);
            binder.RegisterParameterReplacement(node.Parameters[0], parameterExpression);
            var normalizedBody = Transformer.Transform(node.Body);
            var evaluatedBody = PartialEvaluator.Evaluate(normalizedBody);
            return binder.Bind(evaluatedBody);
        }
    }
}
