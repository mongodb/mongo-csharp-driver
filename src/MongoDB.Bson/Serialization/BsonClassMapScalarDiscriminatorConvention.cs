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
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    ///
    /// </summary>
    public class BsonClassMapScalarDiscriminatorConvention : IScalarDiscriminatorConvention
    {
        private readonly BsonClassMap _classMap;
        private readonly string _elementName;

        // cached map
        private readonly ConcurrentDictionary<Type, BsonValue[]> _typeToDiscriminatorsForTypeAndSubTypesMap = new();

        /// <summary>
        ///
        /// </summary>
        public BsonClassMap ClassMap => _classMap;

        /// <summary>
        ///
        /// </summary>
        public string ElementName => _elementName;

        /// <summary>
        ///
        /// </summary>
        /// <param name="elementName"></param>
        /// <param name="classMap"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public BsonClassMapScalarDiscriminatorConvention(BsonClassMap classMap, string elementName)
        {
            _classMap = classMap ?? throw new ArgumentNullException(nameof(classMap));
            _elementName = elementName ?? throw new ArgumentNullException(nameof(elementName));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="bsonReader"></param>
        /// <param name="nominalType"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Type GetActualType(IBsonReader bsonReader, Type nominalType)
        {
            var discriminator = ReadDiscriminator(bsonReader);
            if (discriminator == null)
            {
                return nominalType;
            }

            if (_classMap.DiscriminatorToTypeMap.TryGetValue(discriminator, out Type actualType))
            {
                return actualType;
            }

            throw new BsonSerializationException($"No type found for discriminator value: {discriminator}.");
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="nominalType"></param>
        /// <param name="actualType"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public BsonValue GetDiscriminator(Type nominalType, Type actualType)
        {
            if (actualType == nominalType && !_classMap.DiscriminatorIsRequired)
            {
                return null;
            }

            if (_classMap.TypeToDiscriminatorMap.TryGetValue(actualType, out BsonValue discriminator))
            {
                return discriminator;
            }

            throw new BsonSerializationException($"No discriminator value found for type: {actualType}.");
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public BsonValue[] GetDiscriminatorsForTypeAndSubTypes(Type type)
        {
            return _typeToDiscriminatorsForTypeAndSubTypesMap.GetOrAdd(type, MapTypeToDiscriminatorsForTypeAndSubTypes);
        }

        private BsonValue[] MapTypeToDiscriminatorsForTypeAndSubTypes(Type type)
        {
            var discriminators = new List<BsonValue>();
            foreach (var entry in _classMap.TypeToDiscriminatorMap)
            {
                var discriminatedType = entry.Key;
                var discriminator = entry.Value;
                if (type.IsAssignableFrom(discriminatedType))
                {
                    discriminators.Add(discriminator);
                }
            }

            return discriminators.OrderBy(x => x).ToArray();
        }

        private BsonValue ReadDiscriminator(IBsonReader bsonReader)
        {
            var bookmark = bsonReader.GetBookmark();
            try
            {
                bsonReader.ReadStartDocument();
                if (bsonReader.FindElement(_elementName))
                {
                    var context = BsonDeserializationContext.CreateRoot(bsonReader);
                    return BsonValueSerializer.Instance.Deserialize(context);
                }
            }
            finally
            {
                bsonReader.ReturnToBookmark(bookmark);
            }

            return null;
        }
    }
}
