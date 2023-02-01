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
using System.Collections.Concurrent;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Bson.TestHelpers
{
    public sealed class RegisterObjectSerializerFixture
    {
        public const string CollectionName = "RegisterObjectSerializer";

        public RegisterObjectSerializerFixture()
        {
            // note: because xUnit has no way of letting us run some initialization code before any tests are run
            // and because xUnit runs tests in random order it's very likely some other test has had the side effect
            // of already registering a standard ObjectSerializer, so we have to use reflection below to replace
            // any existing ObjectSerializer with one configured with AllowedTypes for testing

            var testObjectSerializer = new ObjectSerializer(TestAllowedTypes);
            RegisterOrReplaceExistingSerializer(testObjectSerializer);
        }

        public static bool TestAllowedTypes(Type type)
        {
            return
                ObjectSerializer.DefaultAllowedTypes(type) ||
                type.FullName.StartsWith("MongoDB");
        }

        private static void RegisterOrReplaceExistingSerializer(IBsonSerializer<object> serializer)
        {
            // we have to use reflection because the serialization API specifically prohibits this operation
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var cacheInfo = typeof(BsonSerializerRegistry).GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance);
            var cache = (ConcurrentDictionary<Type, IBsonSerializer>)cacheInfo.GetValue(serializerRegistry);
            cache.AddOrUpdate(typeof(object), serializer, (objectType, existingObjectSerializer) => serializer); // might replace existing serializer!
        }
    }
}
