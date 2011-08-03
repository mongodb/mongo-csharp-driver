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
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;

namespace MongoDB.Bson.Serialization {
    /// <summary>
    /// Represents a mapping between a class and a BSON document.
    /// </summary>
    public abstract class BsonClassMap {
        #region private static fields
        private static List<FilteredConventionProfile> profiles = new List<FilteredConventionProfile>();
        private static ConventionProfile defaultProfile = ConventionProfile.GetDefault();
        private static Dictionary<Type, BsonClassMap> classMaps = new Dictionary<Type, BsonClassMap>();
        private static int freezeNestingLevel = 0;
        private static Queue<Type> knownTypesQueue = new Queue<Type>();
        #endregion

        #region protected fields
#pragma warning disable 1591 // missing XML comment (it's warning about protected members also)
        protected bool frozen; // once a class map has been frozen no further changes are allowed
        protected BsonClassMap baseClassMap; // null for class object and interfaces
        protected Type classType;
        private Func<object> creator;
        protected ConventionProfile conventions;
        protected string discriminator;
        protected bool discriminatorIsRequired;
        protected bool hasRootClass;
        protected bool isRootClass;
        protected bool isAnonymous;
        protected BsonMemberMap idMemberMap;
        protected List<BsonMemberMap> allMemberMaps = new List<BsonMemberMap>(); // includes inherited member maps
        protected List<BsonMemberMap> declaredMemberMaps = new List<BsonMemberMap>(); // only the members declared in this class
        protected Dictionary<string, BsonMemberMap> elementDictionary = new Dictionary<string, BsonMemberMap>();
        protected bool ignoreExtraElements = true;
        protected BsonMemberMap extraElementsMemberMap;
        protected List<Type> knownTypes = new List<Type>();
#pragma warning restore
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonClassMap class.
        /// </summary>
        /// <param name="classType">The class type.</param>
        protected BsonClassMap(
            Type classType
        ) {
            this.classType = classType;
            this.conventions = LookupConventions(classType);
            this.discriminator = classType.Name;
            this.isAnonymous = IsAnonymousType(classType);
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the base class map.
        /// </summary>
        public BsonClassMap BaseClassMap {
            get { return baseClassMap; }
        }

        /// <summary>
        /// Gets the class type.
        /// </summary>
        public Type ClassType {
            get { return classType; }
        }

        /// <summary>
        /// Gets the discriminator.
        /// </summary>
        public string Discriminator {
            get { return discriminator; }
        }

        /// <summary>
        /// Gets whether a discriminator is required when serializing this class.
        /// </summary>
        public bool DiscriminatorIsRequired {
            get { return discriminatorIsRequired; }
        }

        /// <summary>
        /// Gets the member map of the member used to hold extra elements.
        /// </summary>
        public BsonMemberMap ExtraElementsMemberMap {
            get { return extraElementsMemberMap; }
        }

        /// <summary>
        /// Gets whether this class has a root class ancestor.
        /// </summary>
        public bool HasRootClass {
            get { return hasRootClass; }
        }

        /// <summary>
        /// Gets the Id member map.
        /// </summary>
        public BsonMemberMap IdMemberMap {
            get { return idMemberMap; }
        }

        /// <summary>
        /// Gets whether extra elements should be ignored when deserializing.
        /// </summary>
        public bool IgnoreExtraElements {
            get { return ignoreExtraElements; }
        }

        /// <summary>
        /// Gets whether this class is anonymous.
        /// </summary>
        public bool IsAnonymous {
            get { return isAnonymous; }
        }

        /// <summary>
        /// Gets whether the class map is frozen.
        /// </summary>
        public bool IsFrozen {
            get { return frozen; }
        }

        /// <summary>
        /// Gets whether this class is a root class.
        /// </summary>
        public bool IsRootClass {
            get { return isRootClass; }
        }

        /// <summary>
        /// Gets the known types of this class.
        /// </summary>
        public IEnumerable<Type> KnownTypes {
            get { return knownTypes; }
        }

        /// <summary>
        /// Gets the member maps.
        /// </summary>
        public IEnumerable<BsonMemberMap> MemberMaps {
            get { return allMemberMaps; }
        }
        #endregion

        #region public static methods
        /// <summary>
        /// Gets the type of a member.
        /// </summary>
        /// <param name="memberInfo">The member info.</param>
        /// <returns>The type of the member.</returns>
        public static Type GetMemberInfoType(
            MemberInfo memberInfo
        ) {
            if (memberInfo.MemberType == MemberTypes.Field) {
                return ((FieldInfo) memberInfo).FieldType;
            } else if (memberInfo.MemberType == MemberTypes.Property) {
                return ((PropertyInfo) memberInfo).PropertyType;
            }

            throw new NotSupportedException("Only field and properties are supported at this time.");
        }

        /// <summary>
        /// Gets a loadable type name (like AssemblyQualifiedName but shortened when possible)
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The type name.</returns>
        public static string GetTypeNameDiscriminator(
            Type type
        ) {
            string typeName;
            if (type.IsGenericType) {
                var genericTypeNames = "";
                foreach (var genericType in type.GetGenericArguments()) {
                    var genericTypeName = GetTypeNameDiscriminator(genericType);
                    if (genericTypeName.Contains(',')) {
                        genericTypeName = "[" + genericTypeName + "]";
                    }
                    if (genericTypeNames != "") {
                        genericTypeNames += ",";
                    }
                    genericTypeNames += genericTypeName;
                }
                typeName = type.GetGenericTypeDefinition().FullName + "[" + genericTypeNames + "]";
            } else {
                typeName = type.FullName;
            }

            string assemblyName = type.Assembly.FullName;
            Match match = Regex.Match(assemblyName, "(?<dll>[^,]+), Version=[^,]+, Culture=[^,]+, PublicKeyToken=(?<token>[^,]+)");
            if (match.Success) {
                var dll = match.Groups["dll"].Value;
                var publicKeyToken = match.Groups["token"].Value;
                if (dll == "mscorlib") {
                    assemblyName = null;
                } else if (publicKeyToken == "null") {
                    assemblyName = dll;
                }
            }

            if (assemblyName == null) {
                return typeName;
            } else {
                return typeName + ", " + assemblyName;
            }
        }

        /// <summary>
        /// Checks whether a class map is registered for a type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if there is a class map registered for the type.</returns>
        public static bool IsClassMapRegistered(
            Type type
        ) {
            BsonSerializer.ConfigLock.EnterReadLock();
            try {
                return classMaps.ContainsKey(type);
            } finally {
                BsonSerializer.ConfigLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Looks up a class map (will AutoMap the class if no class map is registered).
        /// </summary>
        /// <param name="classType">The class type.</param>
        /// <returns>The class map.</returns>
        public static BsonClassMap LookupClassMap(
            Type classType
        ) {
            BsonSerializer.ConfigLock.EnterReadLock();
            try {
                BsonClassMap classMap;
                if (classMaps.TryGetValue(classType, out classMap)) {
                    if (classMap.IsFrozen) {
                        return classMap;
                    }
                }
            } finally {
                BsonSerializer.ConfigLock.ExitReadLock();
            }

            BsonSerializer.ConfigLock.EnterWriteLock();
            try {
                BsonClassMap classMap;
                if (!classMaps.TryGetValue(classType, out classMap)) {
                    // automatically create a classMap for classType and register it
                    var classMapDefinition = typeof(BsonClassMap<>);
                    var classMapType = classMapDefinition.MakeGenericType(classType);
                    classMap = (BsonClassMap) Activator.CreateInstance(classMapType);
                    classMap.AutoMap();
                    RegisterClassMap(classMap);
                }
                return classMap.Freeze();
            } finally {
                BsonSerializer.ConfigLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Looks up the conventions profile for a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The conventions profile for that type.</returns>
        public static ConventionProfile LookupConventions(
            Type type
        ) {
            for (int i = 0; i < profiles.Count; i++) {
                if (profiles[i].Filter(type)) {
                    return profiles[i].Profile;
                }
            }

            return defaultProfile;
        }

        /// <summary>
        /// Creates and registers a class map.
        /// </summary>
        /// <typeparam name="TClass">The class.</typeparam>
        /// <returns>The class map.</returns>
        public static BsonClassMap<TClass> RegisterClassMap<TClass>() {
            return RegisterClassMap<TClass>(cm => { cm.AutoMap(); });
        }

        /// <summary>
        /// Creates and registers a class map.
        /// </summary>
        /// <typeparam name="TClass">The class.</typeparam>
        /// <param name="classMapInitializer">The class map initializer.</param>
        /// <returns>The class map.</returns>
        public static BsonClassMap<TClass> RegisterClassMap<TClass>(
            Action<BsonClassMap<TClass>> classMapInitializer
        ) {
            var classMap = new BsonClassMap<TClass>(classMapInitializer);
            RegisterClassMap(classMap);
            return classMap;
        }

        /// <summary>
        /// Registers a class map.
        /// </summary>
        /// <param name="classMap">The class map.</param>
        public static void RegisterClassMap(
            BsonClassMap classMap
        ) {
            BsonSerializer.ConfigLock.EnterWriteLock();
            try {
                // note: class maps can NOT be replaced (because derived classes refer to existing instance)
                classMaps.Add(classMap.ClassType, classMap);
                BsonDefaultSerializer.RegisterDiscriminator(classMap.ClassType, classMap.Discriminator);
            } finally {
                BsonSerializer.ConfigLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Registers a conventions profile.
        /// </summary>
        /// <param name="conventions">The conventions profile.</param>
        /// <param name="filter">The filter function that determines which types this profile applies to.</param>
        public static void RegisterConventions(
            ConventionProfile conventions,
            Func<Type, bool> filter
        ) {
            conventions.Merge(defaultProfile); // make sure all conventions exists
            var filtered = new FilteredConventionProfile {
                Filter = filter,
                Profile = conventions
            };
            // add new conventions to the front of the list
            profiles.Insert(0, filtered);
        }
        #endregion

        #region public methods
        /// <summary>
        /// Automaps the class.
        /// </summary>
        public void AutoMap() {
            if (frozen) { ThrowFrozenException(); }
            AutoMapClass();
        }

        /// <summary>
        /// Creates an instance of the class.
        /// </summary>
        /// <returns>An object.</returns>
        public object CreateInstance() {
            if (!frozen) { ThrowNotFrozenException(); }
            var creator = GetCreator();
            return creator.Invoke();
        }

        /// <summary>
        /// Freezes the class map.
        /// </summary>
        /// <returns>The class map.</returns>
        public BsonClassMap Freeze() {
            BsonSerializer.ConfigLock.EnterReadLock();
            try {
                if (frozen) {
                    return this;
                }
            } finally {
                BsonSerializer.ConfigLock.ExitReadLock();
            }

            BsonSerializer.ConfigLock.EnterWriteLock();
            try {
                if (!frozen) {
                    freezeNestingLevel++;
                    try {
                        var baseType = classType.BaseType;
                        if (baseType != null) {
                            baseClassMap = LookupClassMap(baseType);
                            discriminatorIsRequired |= baseClassMap.discriminatorIsRequired;
                            hasRootClass |= (isRootClass || baseClassMap.HasRootClass);
                            allMemberMaps.AddRange(baseClassMap.MemberMaps);
                        }
                        allMemberMaps.AddRange(declaredMemberMaps);

                        if (idMemberMap == null) {
                            // see if we can inherit the idMemberMap from our base class
                            if (baseClassMap != null) {
                                idMemberMap = baseClassMap.IdMemberMap;
                            }

                            // if our base class did not have an idMemberMap maybe we have one?
                            if (idMemberMap == null) {
                                var memberName = conventions.IdMemberConvention.FindIdMember(classType);
                                if (memberName != null) {
                                    var memberMap = GetMemberMap(memberName);
                                    if (memberMap != null) {
                                        SetIdMember(memberMap);
                                    }
                                }
                            }
                        }

                        if (extraElementsMemberMap == null) {
                            // see if we can inherit the extraElementsMemberMap from our base class
                            if (baseClassMap != null) {
                                extraElementsMemberMap = baseClassMap.ExtraElementsMemberMap;
                            }

                            // if our base class did not have an extraElementsMemberMap maybe we have one?
                            if (extraElementsMemberMap == null) {
                                var memberName = conventions.ExtraElementsMemberConvention.FindExtraElementsMember(classType);
                                if (memberName != null) {
                                    var memberMap = GetMemberMap(memberName);
                                    if (memberMap != null) {
                                        SetExtraElementsMember(memberMap);
                                    }
                                }
                            }
                        }

                        foreach (var memberMap in allMemberMaps) {
                            BsonMemberMap conflictingMemberMap;
                            if (!elementDictionary.TryGetValue(memberMap.ElementName, out conflictingMemberMap)) {
                                elementDictionary.Add(memberMap.ElementName, memberMap);
                            } else {
                                var fieldOrProperty = (memberMap.MemberInfo.MemberType == MemberTypes.Field) ? "field" : "property";
                                var conflictingFieldOrProperty = (conflictingMemberMap.MemberInfo.MemberType == MemberTypes.Field) ? "field" : "property";
                                var conflictingType = conflictingMemberMap.MemberInfo.DeclaringType;

                                string message;
                                if (conflictingType == classType) {
                                    message = string.Format(
                                        "The {0} '{1}' of type '{2}' cannot use element name '{3}' because it is already being used by {4} '{5}'.",
                                        fieldOrProperty,
                                        memberMap.MemberName,
                                        classType.FullName,
                                        memberMap.ElementName,
                                        conflictingFieldOrProperty,
                                        conflictingMemberMap.MemberName
                                    );
                                } else {
                                    message = string.Format(
                                        "The {0} '{1}' of type '{2}' cannot use element name '{3}' because it is already being used by {4} '{5}' of type '{6}'.",
                                        fieldOrProperty,
                                        memberMap.MemberName,
                                        classType.FullName,
                                        memberMap.ElementName,
                                        conflictingFieldOrProperty,
                                        conflictingMemberMap.MemberName,
                                        conflictingType.FullName
                                    );
                                }
                                throw new BsonSerializationException(message);
                            }
                        }

                        // mark this classMap frozen before we start working on knownTypes
                        // because we might get back to this same classMap while processing knownTypes
                        frozen = true;

                        // use a queue to postpone processing of known types until we get back to the first level call to Freeze
                        // this avoids infinite recursion when going back down the inheritance tree while processing known types
                        foreach (var knownType in knownTypes) {
                            knownTypesQueue.Enqueue(knownType);
                        }

                        // if we are back to the first level go ahead and process any queued known types
                        if (freezeNestingLevel == 1) {
                            while (knownTypesQueue.Count != 0) {
                                var knownType = knownTypesQueue.Dequeue();
                                LookupClassMap(knownType); // will AutoMap and/or Freeze knownType if necessary
                            }
                        }
                    } finally {
                        freezeNestingLevel--;
                    }
                }
            } finally {
                BsonSerializer.ConfigLock.ExitWriteLock();
            }
            return this;
        }

        /// <summary>
        /// Gets a member map.
        /// </summary>
        /// <param name="memberName">The member name.</param>
        /// <returns>The member map.</returns>
        public BsonMemberMap GetMemberMap(
            string memberName
        ) {
            // can be called whether frozen or not
            return declaredMemberMaps.Find(m => m.MemberName == memberName);
        }

        /// <summary>
        /// Gets the member map for a BSON element.
        /// </summary>
        /// <param name="elementName">The name of the element.</param>
        /// <returns>The member map.</returns>
        public BsonMemberMap GetMemberMapForElement(
            string elementName
        ) {
            if (!frozen) { ThrowNotFrozenException(); }
            BsonMemberMap memberMap;
            elementDictionary.TryGetValue(elementName, out memberMap);
            return memberMap;
        }

        /// <summary>
        /// Creates a member map for the extra elements field and adds it to the class map.
        /// </summary>
        /// <param name="fieldName">The name of the extra elements field.</param>
        /// <returns>The member map (so method calls can be chained).</returns>
        public BsonMemberMap MapExtraElementsField(
            string fieldName
        ) {
            if (frozen) { ThrowFrozenException(); }
            var fieldMap = MapField(fieldName);
            SetExtraElementsMember(fieldMap);
            return fieldMap;
        }

        /// <summary>
        /// Creates a member map for the extra elements member and adds it to the class map.
        /// </summary>
        /// <param name="memberInfo">The member info for the extra elements member.</param>
        /// <returns>The member map (so method calls can be chained).</returns>
        public BsonMemberMap MapExtraElementsMember(
            MemberInfo memberInfo
        ) {
            if (frozen) { ThrowFrozenException(); }
            var memberMap = MapMember(memberInfo);
            SetExtraElementsMember(memberMap);
            return memberMap;
        }

        /// <summary>
        /// Creates a member map for the extra elements property and adds it to the class map.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The member map (so method calls can be chained).</returns>
        public BsonMemberMap MapExtraElementsProperty(
            string propertyName
        ) {
            if (frozen) { ThrowFrozenException(); }
            var propertyMap = MapProperty(propertyName);
            SetExtraElementsMember(propertyMap);
            return propertyMap;
        }

        /// <summary>
        /// Creates a member map for a field and adds it to the class map.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        /// <returns>The member map (so method calls can be chained).</returns>
        public BsonMemberMap MapField(
            string fieldName
        ) {
            if (frozen) { ThrowFrozenException(); }
            var fieldInfo = classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (fieldInfo == null) {
                var message = string.Format("The class '{0}' does not have a field named '{1}'.", classType.FullName, fieldName);
                throw new BsonSerializationException(message);
            }
            return MapMember(fieldInfo);
        }

        /// <summary>
        /// Creates a member map for the Id field and adds it to the class map.
        /// </summary>
        /// <param name="fieldName">The name of the Id field.</param>
        /// <returns>The member map (so method calls can be chained).</returns>
        public BsonMemberMap MapIdField(
            string fieldName
        ) {
            if (frozen) { ThrowFrozenException(); }
            var fieldMap = MapField(fieldName);
            SetIdMember(fieldMap);
            return fieldMap;
        }

        /// <summary>
        /// Creates a member map for the Id member and adds it to the class map.
        /// </summary>
        /// <param name="memberInfo">The member info for the Id member.</param>
        /// <returns>The member map (so method calls can be chained).</returns>
        public BsonMemberMap MapIdMember(
            MemberInfo memberInfo
        ) {
            if (frozen) { ThrowFrozenException(); }
            var memberMap = MapMember(memberInfo);
            SetIdMember(memberMap);
            return memberMap;
        }

        /// <summary>
        /// Creates a member map for the Id property and adds it to the class map.
        /// </summary>
        /// <param name="propertyName">The name of the Id property.</param>
        /// <returns>The member map (so method calls can be chained).</returns>
        public BsonMemberMap MapIdProperty(
            string propertyName
        ) {
            if (frozen) { ThrowFrozenException(); }
            var propertyMap = MapProperty(propertyName);
            SetIdMember(propertyMap);
            return propertyMap;
        }

        /// <summary>
        /// Creates a member map for a member and adds it to the class map.
        /// </summary>
        /// <param name="memberInfo">The member info.</param>
        /// <returns>The member map (so method calls can be chained).</returns>
        public BsonMemberMap MapMember(
            MemberInfo memberInfo
        ) {
            if (frozen) { ThrowFrozenException(); }
            if (memberInfo == null) {
                throw new ArgumentNullException("memberInfo");
            }
            if (memberInfo.DeclaringType != classType) {
                throw new ArgumentException("MemberInfo is not for this class.");
            }
            var memberMap = declaredMemberMaps.Find(m => m.MemberInfo == memberInfo);
            if (memberMap == null) {
                memberMap = new BsonMemberMap(memberInfo, conventions);
                declaredMemberMaps.Add(memberMap);
            }
            return memberMap;
        }

        /// <summary>
        /// Creates a member map for a property and adds it to the class map.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The member map (so method calls can be chained).</returns>
        public BsonMemberMap MapProperty(
            string propertyName
        ) {
            if (frozen) { ThrowFrozenException(); }
            var propertyInfo = classType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (propertyInfo == null) {
                var message = string.Format("The class '{0}' does not have a property named '{1}'.", classType.FullName, propertyName);
                throw new BsonSerializationException(message);
            }
            return MapMember(propertyInfo);
        }

        /// <summary>
        /// Sets the discriminator.
        /// </summary>
        /// <param name="discriminator">The discriminator.</param>
        public void SetDiscriminator(
            string discriminator
        ) {
            if (frozen) { ThrowFrozenException(); }
            this.discriminator = discriminator;
        }

        /// <summary>
        /// Sets whether a discriminator is required when serializing this class.
        /// </summary>
        /// <param name="discriminatorIsRequired">Whether a discriminator is required.</param>
        public void SetDiscriminatorIsRequired(
            bool discriminatorIsRequired
        ) {
            if (frozen) { ThrowFrozenException(); }
            this.discriminatorIsRequired = discriminatorIsRequired;
        }

        /// <summary>
        /// Sets the member map of the member used to hold extra elements.
        /// </summary>
        /// <param name="memberMap">The extra elements member map.</param>
        public void SetExtraElementsMember(
            BsonMemberMap memberMap
        ) {
            if (frozen) { ThrowFrozenException(); }
            if (extraElementsMemberMap != null) {
                var message = string.Format("Class {0} already has an extra elements member.", classType.FullName);
                throw new InvalidOperationException(message);
            }
            if (!declaredMemberMaps.Contains(memberMap)) {
                throw new BsonInternalException("Invalid memberMap.");
            }
            if (memberMap.MemberType != typeof(BsonDocument)) {
                var message = string.Format("Type of ExtraElements member must be BsonDocument.");
                throw new InvalidOperationException(message);
            }

            extraElementsMemberMap = memberMap;
        }

        /// <summary>
        /// Sets the Id member.
        /// </summary>
        /// <param name="memberMap">The Id member.</param>
        public void SetIdMember(
            BsonMemberMap memberMap
        ) {
            if (frozen) { ThrowFrozenException(); }
            if (idMemberMap != null) {
                var message = string.Format("Class {0} already has an Id.", classType.FullName);
                throw new InvalidOperationException(message);
            }
            if (!declaredMemberMaps.Contains(memberMap)) {
                throw new BsonInternalException("Invalid memberMap.");
            }

            memberMap.SetElementName("_id");
            idMemberMap = memberMap;
        }

        /// <summary>
        /// Sets whether extra elements should be ignored when deserializing.
        /// </summary>
        /// <param name="ignoreExtraElements">Whether extra elements should be ignored when deserializing.</param>
        public void SetIgnoreExtraElements(
            bool ignoreExtraElements
        ) {
            if (frozen) { ThrowFrozenException(); }
            this.ignoreExtraElements = ignoreExtraElements;
        }

        /// <summary>
        /// Sets whether this class is a root class.
        /// </summary>
        /// <param name="isRootClass">Whether this class is a root class.</param>
        public void SetIsRootClass(
            bool isRootClass
        ) {
            if (frozen) { ThrowFrozenException(); }
            this.isRootClass = isRootClass;
        }

        /// <summary>
        /// Removes the member map for a field from the class map.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        public void UnmapField(
            string fieldName
        ) {
            if (frozen) { ThrowFrozenException(); }
            var fieldInfo = classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (fieldInfo == null) {
                var message = string.Format("The class '{0}' does not have a field named '{1}'.", classType.FullName, fieldName);
                throw new BsonSerializationException(message);
            }
            UnmapMember(fieldInfo);
        }

        /// <summary>
        /// Removes a member map from the class map.
        /// </summary>
        /// <param name="memberInfo">The member info.</param>
        public void UnmapMember(
            MemberInfo memberInfo
        ) {
            if (frozen) { ThrowFrozenException(); }
            if (memberInfo == null) {
                throw new ArgumentNullException("memberInfo");
            }
            if (memberInfo.DeclaringType != classType) {
                throw new ArgumentException("MemberInfo is not for this class.");
            }
            var memberMap = declaredMemberMaps.Find(m => m.MemberInfo == memberInfo);
            if (memberMap != null) {
                declaredMemberMaps.Remove(memberMap);
                if (idMemberMap == memberMap) {
                    idMemberMap = null;
                }
                if (extraElementsMemberMap == memberMap) {
                    extraElementsMemberMap = null;
                }
            }
        }

        /// <summary>
        /// Removes the member map for a property from the class map.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        public void UnmapProperty(
            string propertyName
        ) {
            if (frozen) { ThrowFrozenException(); }
            var propertyInfo = classType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (propertyInfo == null) {
                var message = string.Format("The class '{0}' does not have a property named '{1}'.", classType.FullName, propertyName);
                throw new BsonSerializationException(message);
            }
            UnmapMember(propertyInfo);
        }
        #endregion

        #region private methods
        private void AutoMapClass() {
            foreach (BsonKnownTypesAttribute knownTypesAttribute in classType.GetCustomAttributes(typeof(BsonKnownTypesAttribute), false)) {
                foreach (var knownType in knownTypesAttribute.KnownTypes) {
                    knownTypes.Add(knownType); // knownTypes will be processed when Freeze is called
                }
            }

            var discriminatorAttribute = (BsonDiscriminatorAttribute) classType.GetCustomAttributes(typeof(BsonDiscriminatorAttribute), false).FirstOrDefault();
            if (discriminatorAttribute != null) {
                if (discriminatorAttribute.Discriminator != null) {
                    discriminator = discriminatorAttribute.Discriminator;
                }
                discriminatorIsRequired = discriminatorAttribute.Required;
                isRootClass = discriminatorAttribute.RootClass;
            }

            var ignoreExtraElementsAttribute = (BsonIgnoreExtraElementsAttribute) classType.GetCustomAttributes(typeof(BsonIgnoreExtraElementsAttribute), false).FirstOrDefault();
            if (ignoreExtraElementsAttribute != null) {
                ignoreExtraElements = ignoreExtraElementsAttribute.IgnoreExtraElements;
            } else {
                ignoreExtraElements = conventions.IgnoreExtraElementsConvention.IgnoreExtraElements(classType);
            }

            AutoMapMembers();
        }

        private void AutoMapMembers() {
            // only auto map properties declared in this class (and not in base classes)
            var hasOrderedElements = false;
            foreach (var memberInfo in FindMembers()) {
                var memberMap = AutoMapMember(memberInfo);
                hasOrderedElements = hasOrderedElements || memberMap.Order != int.MaxValue;
            }

            if (hasOrderedElements) {
                // split out the items with a value for Order and sort them separately (because Sort is unstable, see online help)
                // and then concatenate any items with no value for Order at the end (in their original order)
                var sorted = new List<BsonMemberMap>(declaredMemberMaps.Where(pm => pm.Order != int.MaxValue));
                var unsorted = new List<BsonMemberMap>(declaredMemberMaps.Where(pm => pm.Order == int.MaxValue));
                sorted.Sort((x, y) => x.Order.CompareTo(y.Order));
                declaredMemberMaps = sorted.Concat(unsorted).ToList();
            }
        }

        private BsonMemberMap AutoMapMember(
            MemberInfo memberInfo
        ) {
            var memberMap = MapMember(memberInfo);

            memberMap.SetElementName(conventions.ElementNameConvention.GetElementName(memberInfo));
            memberMap.SetIgnoreIfNull(conventions.IgnoreIfNullConvention.IgnoreIfNull(memberInfo));
            memberMap.SetSerializeDefaultValue(conventions.SerializeDefaultValueConvention.SerializeDefaultValue(memberInfo));

            var defaultValue = conventions.DefaultValueConvention.GetDefaultValue(memberInfo);
            if (defaultValue != null) {
                memberMap.SetDefaultValue(defaultValue);
            }

            // see if the class has a method called ShouldSerializeXyz where Xyz is the name of this member
            var shouldSerializeMethod = GetShouldSerializeMethod(memberInfo);
            if (shouldSerializeMethod != null) {
                memberMap.SetShouldSerializeMethod(shouldSerializeMethod);
            }

            foreach (var attribute in memberInfo.GetCustomAttributes(false)) {
                var defaultValueAttribute = attribute as BsonDefaultValueAttribute;
                if (defaultValueAttribute != null) {
                    memberMap.SetDefaultValue(defaultValueAttribute.DefaultValue);
                    memberMap.SetSerializeDefaultValue(defaultValueAttribute.SerializeDefaultValue);
                }

                var elementAttribute = attribute as BsonElementAttribute;
                if (elementAttribute != null) {
                    memberMap.SetElementName(elementAttribute.ElementName);
                    memberMap.SetOrder(elementAttribute.Order);
                    continue;
                }

                var extraElementsAttribute = attribute as BsonExtraElementsAttribute;
                if (extraElementsAttribute != null) {
                    SetExtraElementsMember(memberMap);
                    continue;
                }

                var idAttribute = attribute as BsonIdAttribute;
                if (idAttribute != null) {
                    memberMap.SetElementName("_id");
                    memberMap.SetOrder(idAttribute.Order);
                    var idGeneratorType = idAttribute.IdGenerator;
                    if (idGeneratorType != null) {
                        var idGenerator = (IIdGenerator) Activator.CreateInstance(idGeneratorType); // public default constructor required
                        memberMap.SetIdGenerator(idGenerator);
                    }
                    SetIdMember(memberMap);
                    continue;
                }

                var ignoreIfNullAttribute = attribute as BsonIgnoreIfNullAttribute;
                if (ignoreIfNullAttribute != null) {
                    memberMap.SetIgnoreIfNull(true);
                }

                var requiredAttribute = attribute as BsonRequiredAttribute;
                if (requiredAttribute != null) {
                    memberMap.SetIsRequired(true);
                }

                // note: this handles subclasses of BsonSerializationOptionsAttribute also
                var serializationOptionsAttribute = attribute as BsonSerializationOptionsAttribute;
                if (serializationOptionsAttribute != null) {
                    memberMap.SetSerializationOptions(serializationOptionsAttribute.GetOptions());
                }
            }

            return memberMap;
        }

        private IEnumerable<MemberInfo> FindMembers() {
            // use a List instead of a HashSet to preserver order
            var memberInfos = new List<MemberInfo>(
                conventions.MemberFinderConvention.FindMembers(classType)
            );
            
            // let other fields opt-in if they have a BsonElement attribute
            foreach (var fieldInfo in classType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
                var elementAttribute = (BsonElementAttribute) fieldInfo.GetCustomAttributes(typeof(BsonElementAttribute), false).FirstOrDefault();
                if (elementAttribute == null || fieldInfo.IsInitOnly || fieldInfo.IsLiteral) { 
                    continue;
                }

                if(!memberInfos.Contains(fieldInfo)) {
                    memberInfos.Add(fieldInfo);
                }
            }

            // let other properties opt-in if they have a BsonElement attribute
            foreach (var propertyInfo in classType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
                var elementAttribute = (BsonElementAttribute) propertyInfo.GetCustomAttributes(typeof(BsonElementAttribute), false).FirstOrDefault();
                if (elementAttribute == null || !propertyInfo.CanRead || (!propertyInfo.CanWrite && !isAnonymous)) {
                    continue;
                }

                if(!memberInfos.Contains(propertyInfo)) {
                    memberInfos.Add(propertyInfo);
                }
            }

            foreach(var memberInfo in memberInfos) {
                var ignoreAttribute = (BsonIgnoreAttribute) memberInfo.GetCustomAttributes(typeof(BsonIgnoreAttribute), false).FirstOrDefault();
                if (ignoreAttribute != null) {
                    continue; // ignore this property
                }

                yield return memberInfo;
            }
        }

        private Func<object> GetCreator() {
            if (creator == null) {
                Expression expression;
                var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                var defaultConstructor = classType.GetConstructor(bindingFlags, null, new Type[0], null);
                if (defaultConstructor != null) {
                    expression = Expression.New(defaultConstructor);
                } else {
                    var getUnitializedObjectInfo = typeof(FormatterServices).GetMethod("GetUninitializedObject", BindingFlags.Public | BindingFlags.Static);
                    expression = Expression.Call(getUnitializedObjectInfo, Expression.Constant(classType));
                }
                var lambda = Expression.Lambda<Func<object>>(expression);
                creator = lambda.Compile();
            }
            return creator;
        }

        private Func<object, bool> GetShouldSerializeMethod(
            MemberInfo memberInfo
        ) {
            var shouldSerializeMethodName = "ShouldSerialize" + memberInfo.Name;
            var shouldSerializeMethodInfo = classType.GetMethod(shouldSerializeMethodName, new Type[] { });
            if (
                shouldSerializeMethodInfo != null &&
                shouldSerializeMethodInfo.IsPublic &&
                shouldSerializeMethodInfo.ReturnType == typeof(bool)
            ) {
                // we need to construct a lambda wich does the following
                // (obj) => obj.ShouldSerializeXyz()
                var parameter = Expression.Parameter(typeof(object), "obj");
                var body = Expression.Call(
                    Expression.Convert(parameter, classType),
                    shouldSerializeMethodInfo
                );
                var lambdaExpression = Expression.Lambda<Func<object, bool>>(body, parameter);
                return lambdaExpression.Compile();
            } else {
                return null;
            }
        }

        private bool IsAnonymousType(
            Type type
        ) {
            // don't test for too many things in case implementation details change in the future
            return
                Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false) && 
                type.IsGenericType &&
                type.Name.Contains("Anon"); // don't check for more than "Anon" so it works in mono also
        }

        private void ThrowFrozenException() {
            var message = string.Format("Class map for {0} has been frozen and no further changes are allowed.", classType.FullName);
            throw new InvalidOperationException(message);
        }

        private void ThrowNotFrozenException() {
            var message = string.Format("Class map for {0} has been not been frozen yet.", classType.FullName);
            throw new InvalidOperationException(message);
        }
        #endregion

        #region private class
        private class FilteredConventionProfile {
            public Func<Type, bool> Filter;
            public ConventionProfile Profile;
        }
        #endregion
    }

    /// <summary>
    /// Represents a mapping between a class and a BSON document.
    /// </summary>
    /// <typeparam name="TClass">The class.</typeparam>
    public class BsonClassMap<TClass> : BsonClassMap {
        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonClassMap class.
        /// </summary>
        public BsonClassMap()
            : base(typeof(TClass)) {
        }

        /// <summary>
        /// Initializes a new instance of the BsonClassMap class.
        /// </summary>
        /// <param name="classMapInitializer">The class map initializer.</param>
        public BsonClassMap(
            Action<BsonClassMap<TClass>> classMapInitializer
        ) : base(typeof(TClass)) {
            classMapInitializer(this);
        }
        #endregion

        #region public methods
        /// <summary>
        /// Gets a member map.
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberLambda">A lambda expression specifying the member.</param>
        /// <returns>The member map.</returns>
        public BsonMemberMap GetMemberMap<TMember>(
            Expression<Func<TClass, TMember>> memberLambda
        ) {
            var memberName = GetMemberNameFromLambda(memberLambda);
            return GetMemberMap(memberName);
        }

        /// <summary>
        /// Creates a member map for the extra elements field and adds it to the class map.
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="fieldLambda">A lambda expression specifying the extra elements field.</param>
        /// <returns>The member map.</returns>
        public BsonMemberMap MapExtraElementsField<TMember>(
            Expression<Func<TClass, TMember>> fieldLambda
        ) {
            var fieldMap = MapField(fieldLambda);
            SetExtraElementsMember(fieldMap);
            return fieldMap;
        }

        /// <summary>
        /// Creates a member map for the extra elements member and adds it to the class map.
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberLambda">A lambda expression specifying the extra elements member.</param>
        /// <returns>The member map.</returns>
        public BsonMemberMap MapExtraElementsMember<TMember>(
            Expression<Func<TClass, TMember>> memberLambda
        ) {
            var memberMap = MapMember(memberLambda);
            SetExtraElementsMember(memberMap);
            return memberMap;
        }

        /// <summary>
        /// Creates a member map for the extra elements property and adds it to the class map.
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="propertyLambda">A lambda expression specifying the extra elements property.</param>
        /// <returns>The member map.</returns>
        public BsonMemberMap MapExtraElementsProperty<TMember>(
            Expression<Func<TClass, TMember>> propertyLambda
        ) {
            var propertyMap = MapProperty(propertyLambda);
            SetExtraElementsMember(propertyMap);
            return propertyMap;
        }

        /// <summary>
        /// Creates a member map for a field and adds it to the class map.
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="fieldLambda">A lambda expression specifying the field.</param>
        /// <returns>The member map.</returns>
        public BsonMemberMap MapField<TMember>(
            Expression<Func<TClass, TMember>> fieldLambda
        ) {
            return MapMember(fieldLambda);
        }

        /// <summary>
        /// Creates a member map for the Id field and adds it to the class map.
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="fieldLambda">A lambda expression specifying the Id field.</param>
        /// <returns>The member map.</returns>
        public BsonMemberMap MapIdField<TMember>(
            Expression<Func<TClass, TMember>> fieldLambda
        ) {
            var fieldMap = MapField(fieldLambda);
            SetIdMember(fieldMap);
            return fieldMap;
        }

        /// <summary>
        /// Creates a member map for the Id member and adds it to the class map.
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberLambda">A lambda expression specifying the Id member.</param>
        /// <returns>The member map.</returns>
        public BsonMemberMap MapIdMember<TMember>(
            Expression<Func<TClass, TMember>> memberLambda
        ) {
            var memberMap = MapMember(memberLambda);
            SetIdMember(memberMap);
            return memberMap;
        }

        /// <summary>
        /// Creates a member map for the Id property and adds it to the class map.
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="propertyLambda">A lambda expression specifying the Id property.</param>
        /// <returns>The member map.</returns>
        public BsonMemberMap MapIdProperty<TMember>(
            Expression<Func<TClass, TMember>> propertyLambda
        ) {
            var propertyMap = MapProperty(propertyLambda);
            SetIdMember(propertyMap);
            return propertyMap;
        }

        /// <summary>
        /// Creates a member map and adds it to the class map.
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberLambda">A lambda expression specifying the member.</param>
        /// <returns>The member map.</returns>
        public BsonMemberMap MapMember<TMember>(
            Expression<Func<TClass, TMember>> memberLambda
        ) {
            var memberInfo = GetMemberInfoFromLambda(memberLambda);
            return MapMember(memberInfo);
        }

        /// <summary>
        /// Creates a member map for the Id property and adds it to the class map.
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="propertyLambda">A lambda expression specifying the Id property.</param>
        /// <returns>The member map.</returns>
        public BsonMemberMap MapProperty<TMember>(
            Expression<Func<TClass, TMember>> propertyLambda
        ) {
            return MapMember(propertyLambda);
        }

        /// <summary>
        /// Removes the member map for a field from the class map.
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="fieldLambda">A lambda expression specifying the field.</param>
        public void UnmapField<TMember>(
            Expression<Func<TClass, TMember>> fieldLambda
        ) {
            UnmapMember(fieldLambda);
        }

        /// <summary>
        /// Removes a member map from the class map.
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberLambda">A lambda expression specifying the member.</param>
        public void UnmapMember<TMember>(
            Expression<Func<TClass, TMember>> memberLambda
        ) {
            var memberInfo = GetMemberInfoFromLambda(memberLambda);
            UnmapMember(memberInfo);
        }

        /// <summary>
        /// Removes a member map for a property from the class map.
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="propertyLambda">A lambda expression specifying the property.</param>
        public void UnmapProperty<TMember>(
            Expression<Func<TClass, TMember>> propertyLambda
        ) {
            UnmapMember(propertyLambda);
        }
        #endregion

        #region private methods
        private MemberInfo GetMemberInfoFromLambda<TMember>(
            Expression<Func<TClass, TMember>> memberLambda
        ) {
            var memberName = GetMemberNameFromLambda(memberLambda);
            return classType.GetMember(memberName).SingleOrDefault(x => x.MemberType == MemberTypes.Field || x.MemberType == MemberTypes.Property);
        }

        private string GetMemberNameFromLambda<TMember>(
            Expression<Func<TClass, TMember>> memberLambda
        ) {
            var body = memberLambda.Body;
            MemberExpression memberExpression;
            switch (body.NodeType) {
                case ExpressionType.MemberAccess:
                    memberExpression = (MemberExpression) body;
                    break;
                case ExpressionType.Convert:
                    var convertExpression = (UnaryExpression) body;
                    memberExpression = (MemberExpression) convertExpression.Operand;
                    break;
                default:
                    throw new BsonSerializationException("Invalid propertyLambda.");
            }
            return memberExpression.Member.Name;
        }
        #endregion
    }
}
