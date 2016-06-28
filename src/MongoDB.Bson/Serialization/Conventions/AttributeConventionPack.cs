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
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Bson.Serialization.Conventions
{
    /// <summary>
    /// Convention pack for applying attributes.
    /// </summary>
    public class AttributeConventionPack : IConventionPack
    {
        // private static fields
        private static readonly AttributeConventionPack __attributeConventionPack = new AttributeConventionPack();

        // private fields
        private readonly AttributeConvention _attributeConvention;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeConventionPack" /> class.
        /// </summary>
        private AttributeConventionPack()
        {
            _attributeConvention = new AttributeConvention();
        }

        // public static properties
        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static IConventionPack Instance
        {
            get { return __attributeConventionPack; }
        }

        // public properties
        /// <summary>
        /// Gets the conventions.
        /// </summary>
        public IEnumerable<IConvention> Conventions
        {
            get { yield return _attributeConvention; }
        }

        // nested classes
        private class AttributeConvention : ConventionBase, IClassMapConvention, ICreatorMapConvention, IMemberMapConvention, IPostProcessingConvention
        {
            // public methods
            public void Apply(BsonClassMap classMap)
            {
#if NETCORE50 || NETSTANDARD1_5
                foreach (IBsonClassMapAttribute attribute in classMap.ClassType.GetTypeInfo().CustomAttributes.Where(c=>c.AttributeType is IBsonClassMapAttribute))
#else
                foreach (IBsonClassMapAttribute attribute in classMap.ClassType.GetCustomAttributes(typeof(IBsonClassMapAttribute), false))
#endif
                {
                    attribute.Apply(classMap);
                }

                OptInMembersWithBsonMemberMapModifierAttribute(classMap);
                OptInMembersWithBsonCreatorMapModifierAttribute(classMap);
                IgnoreMembersWithBsonIgnoreAttribute(classMap);
                ThrowForDuplicateMemberMapAttributes(classMap);
            }

            public void Apply(BsonCreatorMap creatorMap)
            {
                if (creatorMap.MemberInfo != null)
                {
#if NETCORE50 || NETSTANDARD1_5
                    foreach (IBsonCreatorMapAttribute attribute in creatorMap.MemberInfo.CustomAttributes.Where(c => c.AttributeType is IBsonCreatorMapAttribute))
#else
                    foreach (IBsonCreatorMapAttribute attribute in creatorMap.MemberInfo.GetCustomAttributes(typeof(IBsonCreatorMapAttribute), false))
#endif
                    {
                        attribute.Apply(creatorMap);
                    }
                }
            }

            public void Apply(BsonMemberMap memberMap)
            {
#if NETCORE50 || NETSTANDARD1_5
                var attributes = memberMap.MemberInfo.CustomAttributes.Where(c => c.AttributeType is IBsonMemberMapAttribute).Select(c => c.AttributeType).Cast<IBsonMemberMapAttribute>();
#else
                var attributes = memberMap.MemberInfo.GetCustomAttributes(typeof(IBsonMemberMapAttribute), false).Cast<IBsonMemberMapAttribute>();
#endif
                var groupings = attributes.GroupBy(a => (a is BsonSerializerAttribute) ? 1 : 2);
                foreach (var grouping in groupings.OrderBy(g => g.Key))
                {
                    foreach (var attribute in grouping)
                    {
                        attribute.Apply(memberMap);
                    }
                }
            }

            public void PostProcess(BsonClassMap classMap)
            {
#if NETCORE50 || NETSTANDARD1_5
                foreach (IBsonPostProcessingAttribute attribute in classMap.ClassType.GetTypeInfo().CustomAttributes.Where(c => c.AttributeType is IBsonPostProcessingAttribute))
#else
                foreach (IBsonPostProcessingAttribute attribute in classMap.ClassType.GetCustomAttributes(typeof(IBsonPostProcessingAttribute), false))
#endif
                {
                    attribute.PostProcess(classMap);
                }
            }

            // private methods
            private bool AllowsDuplicate(Type type)
            {
#if NETCORE50 || NETSTANDARD1_5
                var usageAttribute = type.GetTypeInfo().GetCustomAttributes(typeof(BsonMemberMapAttributeUsageAttribute), true)
                    .OfType<BsonMemberMapAttributeUsageAttribute>()
                    .SingleOrDefault();
#else
                var usageAttribute = type.GetCustomAttributes(typeof(BsonMemberMapAttributeUsageAttribute), true)
                    .OfType<BsonMemberMapAttributeUsageAttribute>()
                    .SingleOrDefault();
#endif

                return usageAttribute == null || usageAttribute.AllowMultipleMembers;
            }

            private void OptInMembersWithBsonCreatorMapModifierAttribute(BsonClassMap classMap)
            {
                // let other constructors opt-in if they have any IBsonCreatorMapAttribute attributes
                foreach (var constructorInfo in classMap.ClassType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
#if NETCORE50 || NETSTANDARD1_5
                    var hasAttribute = constructorInfo.CustomAttributes.Any(c => c.AttributeType is IBsonCreatorMapAttribute);
#else
                    var hasAttribute = constructorInfo.GetCustomAttributes(typeof(IBsonCreatorMapAttribute), false).Any();
#endif
                    if (hasAttribute)
                    {
                        classMap.MapConstructor(constructorInfo);
                    }
                }

                // let other static factory methods opt-in if they have any IBsonCreatorMapAttribute attributes
                foreach (var methodInfo in classMap.ClassType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
#if NETCORE50 || NETSTANDARD1_5
                    var hasAttribute = methodInfo.CustomAttributes.Any(c => c.AttributeType is IBsonCreatorMapAttribute);
#else
                    var hasAttribute = methodInfo.GetCustomAttributes(typeof(IBsonCreatorMapAttribute), false).Any();
#endif
                    if (hasAttribute)
                    {
                        classMap.MapFactoryMethod(methodInfo);
                    }
                }
            }

            private void OptInMembersWithBsonMemberMapModifierAttribute(BsonClassMap classMap)
            {
                // let other fields opt-in if they have any IBsonMemberMapAttribute attributes
                foreach (var fieldInfo in classMap.ClassType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
#if NETCORE50 || NETSTANDARD1_5
                    var hasAttribute = fieldInfo.CustomAttributes.Any(c=>c.AttributeType is IBsonMemberMapAttribute);
#else
                    var hasAttribute = fieldInfo.GetCustomAttributes(typeof(IBsonMemberMapAttribute), false).Any();
#endif
                    if (hasAttribute)
                    {
                        classMap.MapMember(fieldInfo);
                    }
                }

                // let other properties opt-in if they have any IBsonMemberMapAttribute attributes
                foreach (var propertyInfo in classMap.ClassType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
#if NETCORE50 || NETSTANDARD1_5
                    var hasAttribute = propertyInfo.CustomAttributes.Any(c => c.AttributeType is IBsonMemberMapAttribute);
#else
                    var hasAttribute = propertyInfo.GetCustomAttributes(typeof(IBsonMemberMapAttribute), false).Any();
#endif
                    if (hasAttribute)
                    {
                        classMap.MapMember(propertyInfo);
                    }
                }
            }

            private void IgnoreMembersWithBsonIgnoreAttribute(BsonClassMap classMap)
            {
                foreach (var memberMap in classMap.DeclaredMemberMaps.ToList())
                {
                    var ignoreAttribute = (BsonIgnoreAttribute)memberMap.MemberInfo.GetCustomAttributes(typeof(BsonIgnoreAttribute), false).FirstOrDefault();
                    if (ignoreAttribute != null)
                    {
                        classMap.UnmapMember(memberMap.MemberInfo);
                    }
                }
            }

            private void ThrowForDuplicateMemberMapAttributes(BsonClassMap classMap)
            {
                var nonDuplicatesAlreadySeen = new List<Type>();
                foreach (var memberMap in classMap.DeclaredMemberMaps)
                {
#if NETCORE50 || NETSTANDARD1_5
                    var attributes = (IBsonMemberMapAttribute[])memberMap.MemberInfo.CustomAttributes.Where(c => c.AttributeType is IBsonMemberMapAttribute).Cast<IBsonMemberMapAttribute>().ToArray();
#else
                    var attributes = (IBsonMemberMapAttribute[])memberMap.MemberInfo.GetCustomAttributes(typeof(IBsonMemberMapAttribute), false);
#endif
                    // combine them only if the modifier isn't already in the attributes list...
                    var attributeTypes = attributes.Select(x => x.GetType());
                    foreach (var attributeType in attributeTypes)
                    {
                        if (nonDuplicatesAlreadySeen.Contains(attributeType))
                        {
                            var message = string.Format("Attributes of type {0} can only be applied to a single member.", attributeType);
                            throw new DuplicateBsonMemberMapAttributeException(message);
                        }

                        if (!AllowsDuplicate(attributeType))
                        {
                            nonDuplicatesAlreadySeen.Add(attributeType);
                        }
                    }
                }
            }
        }
    }
}