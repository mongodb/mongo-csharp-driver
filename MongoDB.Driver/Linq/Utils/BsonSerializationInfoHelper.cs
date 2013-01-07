/* Copyright 2010-2013 10gen Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Linq.Utils
{
    internal class BsonSerializationInfoHelper
    {
        // private fields
        private readonly Dictionary<Expression, BsonSerializationInfo> _serializationInfoCache;

        // constructors
        public BsonSerializationInfoHelper()
        {
            _serializationInfoCache = new Dictionary<Expression, BsonSerializationInfo>();
        }

        // public methods
        /// <summary>
        /// Gets the serialization info for the given expression.
        /// </summary>
        /// <param name="node">The expression.</param>
        /// <returns>The serialization info.</returns>
        public BsonSerializationInfo GetSerializationInfo(Expression node)
        {
            var evaluatedNode = PartialEvaluator.Evaluate(node);
            return BsonSerializationInfoFinder.GetSerializationInfo(evaluatedNode, _serializationInfoCache);
        }

        /// <summary>
        /// Gets the item serialization info.
        /// </summary>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="serializationInfo">The serialization info.</param>
        /// <returns>The item BsonSerializationInfo for the expression.</returns>
        public BsonSerializationInfo GetItemSerializationInfo(string methodName, BsonSerializationInfo serializationInfo)
        {
            var arraySerializer = serializationInfo.Serializer as IBsonArraySerializer;
            if (arraySerializer != null)
            {
                var itemSerializationInfo = arraySerializer.GetItemSerializationInfo();
                if (itemSerializationInfo != null)
                {
                    return itemSerializationInfo;
                }
            }

            string message = string.Format("{0} requires that the serializer specified for {1} support items by implementing {2} and returning a non-null result. {3} is the current serializer.",
                methodName,
                serializationInfo.ElementName,
                typeof(IBsonArraySerializer),
                serializationInfo.Serializer.GetType());
            throw new NotSupportedException(message);
        }

        /// <summary>
        /// Registers a serializer with the given expression.
        /// </summary>
        /// <param name="node">The expression.</param>
        /// <param name="serializer">The serializer.</param>
        public void RegisterExpressionSerializer(Expression node, IBsonSerializer serializer)
        {
            _serializationInfoCache[node] = new BsonSerializationInfo(
                null,
                serializer,
                node.Type,
                serializer.GetDefaultSerializationOptions());
        }

        /// <summary>
        /// Serializes the value given the serialization information.
        /// </summary>
        /// <param name="serializationInfo">The serialization info.</param>
        /// <param name="value">The value.</param>
        /// <returns>A BsonValue representing the value serialized using the serializer.</returns>
        public BsonValue SerializeValue(BsonSerializationInfo serializationInfo, object value)
        {
            return serializationInfo.SerializeValue(value);
        }

        /// <summary>
        /// Serializes the values given the serialization information.
        /// </summary>
        /// <param name="serializationInfo">The serialization info.</param>
        /// <param name="values">The values.</param>
        /// <returns>A BsonArray representing the values serialized using the serializer.</returns>
        public BsonArray SerializeValues(BsonSerializationInfo serializationInfo, IEnumerable values)
        {
            return serializationInfo.SerializeValues(values);
        }
    }
}