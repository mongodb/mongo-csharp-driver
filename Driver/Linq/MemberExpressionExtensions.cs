/* Copyright 2010-2011 10gen Inc.
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
using System.Text;
using System.Threading;
using System.Reflection;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// A helper class for member lambda expressions.
    /// </summary>
    public static class MemberExpressionExtensions {
        #region private static fields
        private static readonly ReaderWriterLockSlim memberExpressionLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private static readonly Dictionary<MemberInfo, string> elementNameCache = new Dictionary<MemberInfo, string>();
        #endregion

        #region public static methods
        /// <summary>
        /// Gets the element name of a class member expression.
        /// </summary>
        /// <typeparam name="TClass">The class type.</typeparam>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <returns>The member element name.</returns>
        public static string GetElementName<TClass, TMember>(
            this Expression<Func<TClass, TMember>> memberExpression)
        {
            var memberInfo = GetMemberInfoFromLambda(memberExpression); // expressions themselves are instances and not cacheable

            memberExpressionLock.EnterReadLock();
            try {
                string elementName;
                if (elementNameCache.TryGetValue(memberInfo, out elementName))
                {
                    return elementName;
                }
            } finally {
                memberExpressionLock.ExitReadLock();
            }

            var memberMap = BsonClassMap.LookupClassMap(memberInfo.DeclaringType).GetMemberMap(memberInfo);
            if (memberMap == null) {
                throw new BsonSerializationException("Invalid lambda expression");
            }

            memberExpressionLock.EnterWriteLock();
            try {
                string elementName;
                if (!elementNameCache.TryGetValue(memberInfo, out elementName))
                {
                    elementName = memberMap.ElementName;
                    elementNameCache.Add(memberInfo, elementName);
                }
                return elementName;
            } finally {
                memberExpressionLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets the element names of multiple class members expressions.
        /// </summary>
        /// <typeparam name="TClass">The class type.</typeparam>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberExpressions">The member expressions.</param>
        /// <returns>The member element names.</returns>
        public static string[] GetElementNames<TClass, TMember>(
            this Expression<Func<TClass, TMember>>[] memberExpressions)
        {
            string[] elementNames = new string[memberExpressions.Length];
            for (int i = 0; i < elementNames.Length; ++i) {
                elementNames[i] = memberExpressions[i].GetElementName();
            }
            return elementNames;
        }
        #endregion

        #region private static methods
        private static MemberInfo GetMemberInfoFromLambda<TClass, TMember>(
            Expression<Func<TClass, TMember>> memberLambda
        ) {
            var body = memberLambda.Body;
            MemberExpression memberExpression;
            switch (body.NodeType)
            {
                case ExpressionType.MemberAccess:
                    memberExpression = (MemberExpression)body;
                    break;
                case ExpressionType.Convert:
                    var convertExpression = (UnaryExpression)body;
                    memberExpression = (MemberExpression)convertExpression.Operand;
                    break;
                default:
                    throw new BsonSerializationException("Invalid lambda expression");
            }
            var memberInfo = memberExpression.Member;
            if (memberInfo == null ||
                (memberInfo.MemberType != MemberTypes.Field &&
                memberInfo.MemberType != MemberTypes.Property)) {
                throw new BsonSerializationException("Invalid lambda expression");
            }
            return memberInfo;
        }
        #endregion
    }
}
