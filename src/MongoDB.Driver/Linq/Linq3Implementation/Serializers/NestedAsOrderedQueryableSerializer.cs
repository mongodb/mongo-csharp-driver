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
using System.Linq;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Linq.Linq3Implementation.Serializers
{
    internal interface INestedAsOrderedQueryableSerializer : INestedAsQueryableSerializer
    {
    }

    internal class NestedAsOrderedQueryableSerializer<TITem> : IOrderedQueryableSerializer<TITem>, INestedAsOrderedQueryableSerializer
    {
        public NestedAsOrderedQueryableSerializer(IBsonSerializer<TITem> itemSerializer)
            : base(itemSerializer)
        {
        }
    }

    internal static class NestedAsOrderedQueryableSerializer
    {
        public static IBsonSerializer Create(IBsonSerializer itemSerializer)
        {
            var itemType = itemSerializer.ValueType;
            var serializerType = typeof(NestedAsOrderedQueryableSerializer<>).MakeGenericType(itemType);
            return (IBsonSerializer)Activator.CreateInstance(serializerType, itemSerializer);
        }

        public static IBsonSerializer CreateIOrderedEnumerableOrNestedAsOrderedQueryableSerializer(Type resultType, IBsonSerializer itemSerializer)
        {
            if (resultType.IsConstructedGenericType && resultType.GetGenericTypeDefinition() == typeof(IOrderedQueryable<>))
            {
                return NestedAsOrderedQueryableSerializer.Create(itemSerializer);
            }
            else
            {
                return IOrderedEnumerableSerializer.Create(itemSerializer);
            }
        }
    }
}
