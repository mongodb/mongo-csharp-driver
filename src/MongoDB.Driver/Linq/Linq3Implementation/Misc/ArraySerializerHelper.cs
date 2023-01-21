﻿/* Copyright 2010-present MongoDB Inc.
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
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc
{
    internal static class ArraySerializerHelper
    {
        public static IBsonSerializer GetItemSerializer(IBsonSerializer serializer)
        {
            if (serializer is IBsonArraySerializer arraySerializer)
            {
                if (arraySerializer.TryGetItemSerializationInfo(out var itemSerializationInfo))
                {
                    return itemSerializationInfo.Serializer;
                }
                else
                {
                    throw new InvalidOperationException($"{serializer.GetType().FullName}.TryGetItemSerializationInfo returned false.");
                }
            }
            else
            {
                throw new InvalidOperationException($"{serializer.GetType().FullName} must implement IBsonArraySerializer to be used with LINQ.");
            }
        }

        public static IBsonSerializer GetItemSerializer(Expression expression, IBsonSerializer serializer)
        {
            if (serializer is IBsonArraySerializer arraySerializer)
            {
                if (arraySerializer.TryGetItemSerializationInfo(out var itemSerializationInfo))
                {
                    return itemSerializationInfo.Serializer;
                }
                else
                {
                    throw new ExpressionNotSupportedException(expression, because: $"{serializer.GetType().FullName}.TryGetItemSerializationInfo returned false");
                }
            }
            else
            {
                throw new ExpressionNotSupportedException(expression, because: $"{serializer.GetType().FullName} must implement IBsonArraySerializer to be used with LINQ");
            }
        }
    }
}
