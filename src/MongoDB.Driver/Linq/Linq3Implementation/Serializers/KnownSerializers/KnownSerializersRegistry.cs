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
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Linq.Linq3Implementation.Serializers.KnownSerializers
{
    internal class KnownSerializersRegistry
    {
        // private fields
        private readonly Dictionary<Expression, KnownSerializersNode> _registry = new();

        // public methods
        public void Add(Expression expression, KnownSerializersNode knownSerializers)
        {
            if (knownSerializers.Expression != expression)
            {
                throw new ArgumentException($"Expression {expression} does not match knownSerializers.Expression {knownSerializers.Expression}.");
            }

            if (_registry.ContainsKey(expression))
            {
                return;
            }

            _registry.Add(expression, knownSerializers);
        }

        public void SetNodeSerializer(Expression expression, IBsonSerializer nodeSerializer)
        {
            if (nodeSerializer.ValueType != expression.Type)
            {
                throw new ArgumentException($"Serializer value type {nodeSerializer.ValueType} does not match expresion type {expression.Type}.", nameof(nodeSerializer));
            }

            if (!_registry.TryGetValue(expression, out var knownSerializers))
            {
                throw new InvalidOperationException("KnownSerializersNode does not exist yet for expression: {expression}.");
            }

            knownSerializers.SetNodeSerializer(nodeSerializer);
        }

        public IBsonSerializer GetSerializer(Expression expression, IBsonSerializer defaultSerializer = null)
        {
            var expressionType = expression is LambdaExpression lambdaExpression ? lambdaExpression.ReturnType : expression.Type;
            return GetSerializer(expression, expressionType, defaultSerializer);
        }

        public IBsonSerializer GetSerializer(Expression expression, Type type, IBsonSerializer defaultSerializer = null)
        {
            var possibleSerializers = _registry.TryGetValue(expression, out var knownSerializers) ? knownSerializers.GetPossibleSerializers(type) : new HashSet<IBsonSerializer>();
            return possibleSerializers.Count switch
            {
                0 => defaultSerializer ?? LookupSerializer(expression, type), // sometimes there is no known serializer from the context (e.g. CSHARP-4062)
                1 => possibleSerializers.First(),
                _ => throw new InvalidOperationException($"More than one possible serializer found for {type} in {expression}.")
            };
        }

        public IBsonSerializer GetSerializerAtThisLevel(Expression expression)
        {
            var expressionType = expression is LambdaExpression lambdaExpression ? lambdaExpression.ReturnType : expression.Type;
            return GetSerializerAtThisLevel(expression, expressionType);
        }

        public IBsonSerializer GetSerializerAtThisLevel(Expression expression, Type type)
        {
            var possibleSerializers = _registry.TryGetValue(expression, out var knownSerializers) ? knownSerializers.GetPossibleSerializers(type) : new HashSet<IBsonSerializer>();
            return possibleSerializers.Count == 1 ? possibleSerializers.Single() : null;
        }

        private IBsonSerializer LookupSerializer(Expression expression, Type type)
        {
            if (type.IsConstructedGenericType &&
                type.GetGenericTypeDefinition() == typeof(IGrouping<,>))
            {
                var genericArguments = type.GetGenericArguments();
                var keyType = genericArguments[0];
                var elementType = genericArguments[1];

                var keySerializer = GetSerializer(expression, keyType);
                var elementSerializer = GetSerializer(expression, elementType);
                return IGroupingSerializer.Create(keySerializer, elementSerializer);
            }

            return BsonSerializer.LookupSerializer(type);
        }
    }
}
