/* Copyright 2010-2012 10gen Inc.
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
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Linq.Utils
{
    /// <summary>
    /// Provides serialization info based on an expression.
    /// </summary>
    internal class BsonSerializationInfoHelper
    {
        private readonly Dictionary<Expression, IBsonSerializer> _serializerCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonSerializationInfoHelper"/> class.
        /// </summary>
        public BsonSerializationInfoHelper()
        {
            _serializerCache = new Dictionary<Expression, IBsonSerializer>();
        }

        /// <summary>
        /// Gets the serialization info for an expression if it exists.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>
        /// The serialization info or null if it does not exist.
        /// </returns>
        public BsonSerializationInfo GetSerializationInfo(Expression expression)
        {
            var lambdaExpression = expression as LambdaExpression;
            if (lambdaExpression != null)
            {
                return GetSerializationInfo(lambdaExpression.Body);
            }

            var parameterExpression = expression as ParameterExpression;
            if (parameterExpression != null)
            {
                IBsonSerializer serializer;
                if (!_serializerCache.TryGetValue(parameterExpression, out serializer))
                    _serializerCache[parameterExpression] = serializer = BsonSerializer.LookupSerializer(parameterExpression.Type);
                return new BsonSerializationInfo(
                    null, // elementName
                    serializer,
                    parameterExpression.Type, // nominalType
                    null); // serialization options
            }

            parameterExpression = ExpressionParameterFinder.FindParameter(expression);
            if (parameterExpression != null)
            {
                var info = GetSerializationInfo(parameterExpression);
                info = GetSerializationInfo(info.Serializer, expression);
                if (info != null)
                {
                    _serializerCache[expression] = info.Serializer;
                }
                return info;
            }

            string message = string.Format("Unable to determine the serialization information for the expression {0}.",
                ExpressionFormatter.ToString(expression));
            throw new NotSupportedException(message);
        }

        /// <summary>
        /// Gets the item serialization info.
        /// </summary>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="serializationInfo">The serialization info.</param>
        /// <returns>The item BsonSerializationInfo for the expression.</returns>
        public BsonSerializationInfo GetItemSerializationInfo(string methodName, BsonSerializationInfo serializationInfo)
        {
            var itemSerializationInfoProvider = serializationInfo.Serializer as IBsonItemSerializationInfoProvider;
            if (itemSerializationInfoProvider != null)
            {
                var itemSerializationInfo = itemSerializationInfoProvider.GetItemSerializationInfo();
                if (itemSerializationInfo != null)
                {
                    return itemSerializationInfo;
                }
            }

            string message = string.Format("{0} requires that the serializer specified for {1} support items by implementing {2} and returning a non-null result. {3} is the current serializer.",
                methodName,
                serializationInfo.ElementName,
                typeof(IBsonItemSerializationInfoProvider),
                serializationInfo.Serializer.GetType());
            throw new NotSupportedException(message);
        }

        /// <summary>
        /// Registers a serializer with the given expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="serializer">The serializer.</param>
        public void RegisterExpressionSerializer(Expression expression, IBsonSerializer serializer)
        {
            _serializerCache[expression] = serializer;
        }

        /// <summary>
        /// Serializes the value given the serialization information.
        /// </summary>
        /// <param name="serializationInfo">The serialization info.</param>
        /// <param name="value">The value.</param>
        /// <returns>A BsonValue representing the value serialized using the serializer.</returns>
        public BsonValue SerializeValue(BsonSerializationInfo serializationInfo, object value)
        {
            var bsonDocument = new BsonDocument();
            var bsonWriter = BsonWriter.Create(bsonDocument);
            bsonWriter.WriteStartDocument();
            bsonWriter.WriteName("value");
            serializationInfo.Serializer.Serialize(bsonWriter, serializationInfo.NominalType, value, serializationInfo.SerializationOptions);
            bsonWriter.WriteEndDocument();
            return bsonDocument[0];
        }

        /// <summary>
        /// Serializes the values given the serialization information.
        /// </summary>
        /// <param name="serializationInfo">The serialization info.</param>
        /// <param name="values">The values.</param>
        /// <returns>A BsonArray representing the values serialized using the serializer.</returns>
        public BsonArray SerializeValues(BsonSerializationInfo serializationInfo, IEnumerable values)
        {
            var bsonDocument = new BsonDocument();
            var bsonWriter = BsonWriter.Create(bsonDocument);
            bsonWriter.WriteStartDocument();
            bsonWriter.WriteName("values");
            bsonWriter.WriteStartArray();
            foreach (var value in values)
            {
                serializationInfo.Serializer.Serialize(bsonWriter, serializationInfo.NominalType, value, serializationInfo.SerializationOptions);
            }
            bsonWriter.WriteEndArray();
            bsonWriter.WriteEndDocument();
            return bsonDocument[0].AsBsonArray;
        }

        private BsonSerializationInfo GetSerializationInfo(IBsonSerializer serializer, Expression expression)
        {
            // when looking to get a member's serialization info, we will ignore a top-level return conversion because 
            // it is inconsequential
            if (expression.NodeType == ExpressionType.Convert || expression.NodeType == ExpressionType.ConvertChecked)
            {
                return GetSerializationInfo(serializer, ((UnaryExpression)expression).Operand);
            }

            var memberExpression = expression as MemberExpression;
            if (memberExpression != null)
            {
                return GetSerializationInfoMember(serializer, memberExpression);
            }

            var binaryExpression = expression as BinaryExpression;
            if (binaryExpression != null && binaryExpression.NodeType == ExpressionType.ArrayIndex)
            {
                return GetSerializationInfoArrayIndex(serializer, binaryExpression);
            }

            var methodCallExpression = expression as MethodCallExpression;
            if (methodCallExpression != null && methodCallExpression.Method.Name == "get_Item")
            {
                return GetSerializationInfoGetItem(serializer, methodCallExpression);
            }

            return null;
        }

        private BsonSerializationInfo GetSerializationInfoArrayIndex(IBsonSerializer serializer, BinaryExpression binaryExpression)
        {
            var arraySerializationInfo = GetSerializationInfo(serializer, binaryExpression.Left);
            if (arraySerializationInfo != null)
            {
                var itemSerializationInfoProvider = arraySerializationInfo.Serializer as IBsonItemSerializationInfoProvider;
                if (itemSerializationInfoProvider == null)
                {
                    var message = string.Format(
                        "Queries using an array index cannot be run against a member whose serializer does not implement {0}. The current serializer is {1}.",
                        BsonUtils.GetFriendlyTypeName(typeof(IBsonItemSerializationInfoProvider)),
                        BsonUtils.GetFriendlyTypeName(arraySerializationInfo.Serializer.GetType()));
                    throw new NotSupportedException(message);
                }
                var itemSerializationInfo = itemSerializationInfoProvider.GetItemSerializationInfo();
                var indexEpression = binaryExpression.Right as ConstantExpression;
                if (indexEpression != null)
                {
                    var index = Convert.ToInt32(indexEpression.Value);
                    return new BsonSerializationInfo(
                        arraySerializationInfo.ElementName + "." + index.ToString(),
                        itemSerializationInfo.Serializer,
                        itemSerializationInfo.NominalType,
                        itemSerializationInfo.SerializationOptions);
                }
            }

            return null;
        }

        private BsonSerializationInfo GetSerializationInfoGetItem(IBsonSerializer serializer, MethodCallExpression methodCallExpression)
        {
            var arguments = methodCallExpression.Arguments.ToArray();
            if (arguments.Length == 1)
            {
                var indexEpression = arguments[0] as ConstantExpression;
                if (indexEpression == null)
                {
                    return null;
                }
                var index = Convert.ToInt32(indexEpression.Value);

                var arraySerializationInfo = GetSerializationInfo(serializer, methodCallExpression.Object);
                if (arraySerializationInfo != null)
                {
                    var itemSerializationInfoProvider = arraySerializationInfo.Serializer as IBsonItemSerializationInfoProvider;
                    if (itemSerializationInfoProvider != null)
                    {
                        var itemSerializationInfo = itemSerializationInfoProvider.GetItemSerializationInfo();
                        return new BsonSerializationInfo(
                            arraySerializationInfo.ElementName + "." + index.ToString(),
                            itemSerializationInfo.Serializer,
                            itemSerializationInfo.NominalType,
                            itemSerializationInfo.SerializationOptions);
                    }
                }
            }

            return null;
        }

        private BsonSerializationInfo GetSerializationInfoMember(IBsonSerializer serializer, MemberExpression memberExpression)
        {
            var declaringType = memberExpression.Expression.Type;
            var memberName = memberExpression.Member.Name;

            var containingExpression = memberExpression.Expression;
            if (containingExpression.NodeType == ExpressionType.Parameter)
            {
                var memberSerializationInfoProvider = serializer as IBsonMemberSerializationInfoProvider;
                if (memberSerializationInfoProvider != null)
                {
                    return memberSerializationInfoProvider.GetMemberSerializationInfo(memberName);
                }
                else
                {
                    var message = string.Format("LINQ queries on fields or properties of class {0} are not supported because the serializer for {0} does not implement the GetMemberSerializationInfo method.", declaringType.Name);
                    throw new NotSupportedException(message);
                }
            }
            else
            {
                var containingSerializationInfo = GetSerializationInfo(serializer, containingExpression);
                var memberSerializationInfoProvider = containingSerializationInfo.Serializer as IBsonMemberSerializationInfoProvider;
                if (memberSerializationInfoProvider != null)
                {
                    var memberSerializationInfo = memberSerializationInfoProvider.GetMemberSerializationInfo(memberName);
                    return new BsonSerializationInfo(
                        containingSerializationInfo.ElementName + "." + memberSerializationInfo.ElementName,
                        memberSerializationInfo.Serializer,
                        memberSerializationInfo.NominalType,
                        memberSerializationInfo.SerializationOptions);
                }
                else
                {
                    var message = string.Format("LINQ queries on fields or properties of class {0} are not supported because the serializer for {0} does not implement the GetMemberSerializationInfo method.", declaringType.Name);
                    throw new NotSupportedException(message);
                }
            }
        }
    }
}
