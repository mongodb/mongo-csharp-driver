/* Copyright 2015 MongoDB Inc.
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
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Expressions;

namespace MongoDB.Driver.Linq.Processors
{
    internal sealed class PipelineBindingContext : IBindingContext
    {
        private readonly Dictionary<Expression, Guid> _correlationMapping;
        private readonly Dictionary<Expression, Expression> _expressionMapping;
        private readonly Dictionary<MemberInfo, Expression> _memberMapping;
        private readonly IBsonSerializerRegistry _serializerRegistry;

        public PipelineBindingContext(IBsonSerializerRegistry serializerRegistry)
        {
            _serializerRegistry = Ensure.IsNotNull(serializerRegistry, nameof(serializerRegistry));
            _correlationMapping = new Dictionary<Expression, Guid>();
            _expressionMapping = new Dictionary<Expression, Expression>();
            _memberMapping = new Dictionary<MemberInfo, Expression>();
        }

        public void AddCorrelatingId(Expression node, Guid correlatingId)
        {
            Ensure.IsNotNull(node, nameof(node));

            _correlationMapping.Add(node, correlatingId);
        }

        public void AddExpressionMapping(Expression original, Expression replacement)
        {
            Ensure.IsNotNull(original, nameof(original));
            Ensure.IsNotNull(replacement, nameof(replacement));

            _expressionMapping[original] = replacement;
        }

        public void AddMemberMapping(MemberInfo member, Expression replacement)
        {
            Ensure.IsNotNull(member, nameof(member));
            Ensure.IsNotNull(replacement, nameof(replacement));

            _memberMapping[member] = replacement;
        }

        public Expression Bind(Expression node)
        {
            return Bind(node, false);
        }

        public Expression Bind(Expression node, bool isClientSideProjection)
        {
            Ensure.IsNotNull(node, nameof(node));

            return SerializationBinder.Bind(node, this, isClientSideProjection);
        }

        public IBsonSerializer GetSerializer(Type type, Expression node)
        {
            Ensure.IsNotNull(type, nameof(type));

            IBsonSerializer serializer;
            if (node != null && PreviouslyUsedSerializerFinder.TryFindSerializer(node, type, out serializer))
            {
                return serializer;
            }
            else if (node == null || type != node.Type)
            {
                return _serializerRegistry.GetSerializer(type);
            }

            return SerializerBuilder.Build(node, _serializerRegistry);
        }

        public bool TryGetCorrelatingId(Expression node, out Guid correlatingId)
        {
            return _correlationMapping.TryGetValue(node, out correlatingId);
        }

        public bool TryGetExpressionMapping(Expression original, out Expression replacement)
        {
            Ensure.IsNotNull(original, nameof(original));

            return _expressionMapping.TryGetValue(original, out replacement);
        }

        public bool TryGetMemberMapping(MemberInfo member, out Expression replacement)
        {
            Ensure.IsNotNull(member, nameof(member));

            return _memberMapping.TryGetValue(member, out replacement);
        }

        public FieldAsDocumentExpression WrapField(Expression node, string fieldName, IBsonSerializer serializer = null)
        {
            if (serializer == null)
            {
                serializer = GetSerializer(node.Type, node);
            }

            return new FieldAsDocumentExpression(node, fieldName, serializer);
        }
    }
}
