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
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Represents a mapping between a class and a BSON document.
    /// </summary>
    public abstract class BsonClassMap
    {
        // private static fields
        private static List<FilteredConventionProfile> __profiles = new List<FilteredConventionProfile>();
        private static ConventionProfile __defaultProfile = ConventionProfile.GetDefault();
        private static Dictionary<Type, BsonClassMap> __classMaps = new Dictionary<Type, BsonClassMap>();
        private static int __freezeNestingLevel = 0;
        private static Queue<Type> __knownTypesQueue = new Queue<Type>();

        // private fields
        private bool _frozen; // once a class map has been frozen no further changes are allowed
        private BsonClassMap _baseClassMap; // null for class object and interfaces
        private Type _classType;
        private Func<object> _creator;
        private ConventionProfile _conventions;
        private string _discriminator;
        private bool _discriminatorIsRequired;
        private bool _hasRootClass;
        private bool _isRootClass;
        private bool _isAnonymous;
        private BsonMemberMap _idMemberMap;
        private List<BsonMemberMap> _allMemberMaps = new List<BsonMemberMap>(); // includes inherited member maps
        private List<BsonMemberMap> _declaredMemberMaps = new List<BsonMemberMap>(); // only the members declared in this class
        private Dictionary<string, BsonMemberMap> _elementDictionary = new Dictionary<string, BsonMemberMap>();
        private bool _ignoreExtraElements = true;
        private bool _ignoreExtraElementsIsInherited = false;
        private BsonMemberMap _extraElementsMemberMap;
        private List<Type> _knownTypes = new List<Type>();

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonClassMap class.
        /// </summary>
        /// <param name="classType">The class type.</param>
        protected BsonClassMap(Type classType)
        {
            _classType = classType;
            _conventions = LookupConventions(classType);
            _discriminator = classType.Name;
            _isAnonymous = IsAnonymousType(classType);
        }

        // public properties
        /// <summary>
        /// Gets all the member maps (including maps for inherited members).
        /// </summary>
        public IEnumerable<BsonMemberMap> AllMemberMaps
        {
            get { return _allMemberMaps; }
        }

        /// <summary>
        /// Gets the base class map.
        /// </summary>
        public BsonClassMap BaseClassMap
        {
            get { return _baseClassMap; }
        }

        /// <summary>
        /// Gets the class type.
        /// </summary>
        public Type ClassType
        {
            get { return _classType; }
        }

        /// <summary>
        /// Gets the conventions used with this class.
        /// </summary>
        public ConventionProfile Conventions
        {
            get { return _conventions; }
        }

        /// <summary>
        /// Gets the declared member maps (only for members declared in this class).
        /// </summary>
        public IEnumerable<BsonMemberMap> DeclaredMemberMaps
        {
            get { return _declaredMemberMaps; }
        }

        /// <summary>
        /// Gets the discriminator.
        /// </summary>
        public string Discriminator
        {
            get { return _discriminator; }
        }

        /// <summary>
        /// Gets whether a discriminator is required when serializing this class.
        /// </summary>
        public bool DiscriminatorIsRequired
        {
            get { return _discriminatorIsRequired; }
        }

        /// <summary>
        /// Gets the member map of the member used to hold extra elements.
        /// </summary>
        public BsonMemberMap ExtraElementsMemberMap
        {
            get { return _extraElementsMemberMap; }
        }

        /// <summary>
        /// Gets whether this class has a root class ancestor.
        /// </summary>
        public bool HasRootClass
        {
            get { return _hasRootClass; }
        }

        /// <summary>
        /// Gets the Id member map.
        /// </summary>
        public BsonMemberMap IdMemberMap
        {
            get { return _idMemberMap; }
        }

        /// <summary>
        /// Gets whether extra elements should be ignored when deserializing.
        /// </summary>
        public bool IgnoreExtraElements
        {
            get { return _ignoreExtraElements; }
        }

        /// <summary>
        /// Gets whether the IgnoreExtraElements value should be inherited by derived classes.
        /// </summary>
        public bool IgnoreExtraElementsIsInherited
        {
            get { return _ignoreExtraElementsIsInherited; }
        }

        /// <summary>
        /// Gets whether this class is anonymous.
        /// </summary>
        public bool IsAnonymous
        {
            get { return _isAnonymous; }
        }

        /// <summary>
        /// Gets whether the class map is frozen.
        /// </summary>
        public bool IsFrozen
        {
            get { return _frozen; }
        }

        /// <summary>
        /// Gets whether this class is a root class.
        /// </summary>
        public bool IsRootClass
        {
            get { return _isRootClass; }
        }

        /// <summary>
        /// Gets the known types of this class.
        /// </summary>
        public IEnumerable<Type> KnownTypes
        {
            get { return _knownTypes; }
        }

        /// <summary>
        /// Gets the member maps.
        /// </summary>
        [Obsolete("Use AllMemberMaps or DeclaredMemberMaps instead.")]
        public IEnumerable<BsonMemberMap> MemberMaps
        {
            get { return _allMemberMaps; }
        }

        // public static methods
        /// <summary>
        /// Gets the type of a member.
        /// </summary>
        /// <param name="memberInfo">The member info.</param>
        /// <returns>The type of the member.</returns>
        public static Type GetMemberInfoType(MemberInfo memberInfo)
        {
            if (memberInfo == null)
            {
                throw new ArgumentNullException("memberInfo");
            }

            if (memberInfo.MemberType == MemberTypes.Field)
            {
                return ((FieldInfo)memberInfo).FieldType;
            }
            else if (memberInfo.MemberType == MemberTypes.Property)
            {
                return ((PropertyInfo)memberInfo).PropertyType;
            }

            throw new NotSupportedException("Only field and properties are supported at this time.");
        }

        /// <summary>
        /// Checks whether a class map is registered for a type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if there is a class map registered for the type.</returns>
        public static bool IsClassMapRegistered(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            BsonSerializer.ConfigLock.EnterReadLock();
            try
            {
                return __classMaps.ContainsKey(type);
            }
            finally
            {
                BsonSerializer.ConfigLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Looks up a class map (will AutoMap the class if no class map is registered).
        /// </summary>
        /// <param name="classType">The class type.</param>
        /// <returns>The class map.</returns>
        public static BsonClassMap LookupClassMap(Type classType)
        {
            if (classType == null)
            {
                throw new ArgumentNullException("classType");
            }

            BsonSerializer.ConfigLock.EnterReadLock();
            try
            {
                BsonClassMap classMap;
                if (__classMaps.TryGetValue(classType, out classMap))
                {
                    if (classMap.IsFrozen)
                    {
                        return classMap;
                    }
                }
            }
            finally
            {
                BsonSerializer.ConfigLock.ExitReadLock();
            }

            BsonSerializer.ConfigLock.EnterWriteLock();
            try
            {
                BsonClassMap classMap;
                if (!__classMaps.TryGetValue(classType, out classMap))
                {
                    // automatically create a classMap for classType and register it
                    var classMapDefinition = typeof(BsonClassMap<>);
                    var classMapType = classMapDefinition.MakeGenericType(classType);
                    classMap = (BsonClassMap)Activator.CreateInstance(classMapType);
                    classMap.AutoMap();
                    RegisterClassMap(classMap);
                }
                return classMap.Freeze();
            }
            finally
            {
                BsonSerializer.ConfigLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Looks up the conventions profile for a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The conventions profile for that type.</returns>
        public static ConventionProfile LookupConventions(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            for (int i = 0; i < __profiles.Count; i++)
            {
                if (__profiles[i].Filter(type))
                {
                    return __profiles[i].Profile;
                }
            }

            return __defaultProfile;
        }

        /// <summary>
        /// Creates and registers a class map.
        /// </summary>
        /// <typeparam name="TClass">The class.</typeparam>
        /// <returns>The class map.</returns>
        public static BsonClassMap<TClass> RegisterClassMap<TClass>()
        {
            return RegisterClassMap<TClass>(cm => { cm.AutoMap(); });
        }

        /// <summary>
        /// Creates and registers a class map.
        /// </summary>
        /// <typeparam name="TClass">The class.</typeparam>
        /// <param name="classMapInitializer">The class map initializer.</param>
        /// <returns>The class map.</returns>
        public static BsonClassMap<TClass> RegisterClassMap<TClass>(Action<BsonClassMap<TClass>> classMapInitializer)
        {
            var classMap = new BsonClassMap<TClass>(classMapInitializer);
            RegisterClassMap(classMap);
            return classMap;
        }

        /// <summary>
        /// Registers a class map.
        /// </summary>
        /// <param name="classMap">The class map.</param>
        public static void RegisterClassMap(BsonClassMap classMap)
        {
            if (classMap == null)
            {
                throw new ArgumentNullException("classMap");
            }

            BsonSerializer.ConfigLock.EnterWriteLock();
            try
            {
                // note: class maps can NOT be replaced (because derived classes refer to existing instance)
                __classMaps.Add(classMap.ClassType, classMap);
                BsonDefaultSerializer.RegisterDiscriminator(classMap.ClassType, classMap.Discriminator);
            }
            finally
            {
                BsonSerializer.ConfigLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Registers a conventions profile.
        /// </summary>
        /// <param name="conventions">The conventions profile.</param>
        /// <param name="filter">The filter function that determines which types this profile applies to.</param>
        public static void RegisterConventions(ConventionProfile conventions, Func<Type, bool> filter)
        {
            if (conventions == null)
            {
                throw new ArgumentNullException("conventions");
            }
            if (filter == null)
            {
                throw new ArgumentNullException("filter");
            }

            conventions.Merge(__defaultProfile); // make sure all conventions exists
            var filtered = new FilteredConventionProfile
            {
                Filter = filter,
                Profile = conventions
            };
            // add new conventions to the front of the list
            __profiles.Insert(0, filtered);
        }

        // public methods
        /// <summary>
        /// Automaps the class.
        /// </summary>
        public void AutoMap()
        {
            if (_frozen) { ThrowFrozenException(); }
            AutoMapClass();
        }

        /// <summary>
        /// Creates an instance of the class.
        /// </summary>
        /// <returns>An object.</returns>
        public object CreateInstance()
        {
            if (!_frozen) { ThrowNotFrozenException(); }
            var creator = GetCreator();
            return creator.Invoke();
        }

        /// <summary>
        /// Freezes the class map.
        /// </summary>
        /// <returns>The frozen class map.</returns>
        public BsonClassMap Freeze()
        {
            BsonSerializer.ConfigLock.EnterReadLock();
            try
            {
                if (_frozen)
                {
                    return this;
                }
            }
            finally
            {
                BsonSerializer.ConfigLock.ExitReadLock();
            }

            BsonSerializer.ConfigLock.EnterWriteLock();
            try
            {
                if (!_frozen)
                {
                    __freezeNestingLevel++;
                    try
                    {
                        var baseType = _classType.BaseType;
                        if (baseType != null)
                        {
                            _baseClassMap = LookupClassMap(baseType);
                            _discriminatorIsRequired |= _baseClassMap._discriminatorIsRequired;
                            _hasRootClass |= (_isRootClass || _baseClassMap.HasRootClass);
                            _allMemberMaps.AddRange(_baseClassMap.AllMemberMaps);
                            if (_baseClassMap.IgnoreExtraElements && _baseClassMap.IgnoreExtraElementsIsInherited)
                            {
                                _ignoreExtraElements = true;
                                _ignoreExtraElementsIsInherited = true;
                            }
                        }
                        _allMemberMaps.AddRange(_declaredMemberMaps);

                        if (_idMemberMap == null)
                        {
                            // see if we can inherit the idMemberMap from our base class
                            if (_baseClassMap != null)
                            {
                                _idMemberMap = _baseClassMap.IdMemberMap;
                            }

                            // if our base class did not have an idMemberMap maybe we have one?
                            if (_idMemberMap == null)
                            {
                                var memberName = _conventions.IdMemberConvention.FindIdMember(_classType);
                                if (memberName != null)
                                {
                                    var memberMap = GetMemberMap(memberName);
                                    if (memberMap != null)
                                    {
                                        SetIdMember(memberMap);
                                    }
                                }
                            }
                        }

                        if (_extraElementsMemberMap == null)
                        {
                            // see if we can inherit the extraElementsMemberMap from our base class
                            if (_baseClassMap != null)
                            {
                                _extraElementsMemberMap = _baseClassMap.ExtraElementsMemberMap;
                            }

                            // if our base class did not have an extraElementsMemberMap maybe we have one?
                            if (_extraElementsMemberMap == null)
                            {
                                var memberName = _conventions.ExtraElementsMemberConvention.FindExtraElementsMember(_classType);
                                if (memberName != null)
                                {
                                    var memberMap = GetMemberMap(memberName);
                                    if (memberMap != null)
                                    {
                                        SetExtraElementsMember(memberMap);
                                    }
                                }
                            }
                        }

                        foreach (var memberMap in _allMemberMaps)
                        {
                            BsonMemberMap conflictingMemberMap;
                            if (!_elementDictionary.TryGetValue(memberMap.ElementName, out conflictingMemberMap))
                            {
                                _elementDictionary.Add(memberMap.ElementName, memberMap);
                            }
                            else
                            {
                                var fieldOrProperty = (memberMap.MemberInfo.MemberType == MemberTypes.Field) ? "field" : "property";
                                var conflictingFieldOrProperty = (conflictingMemberMap.MemberInfo.MemberType == MemberTypes.Field) ? "field" : "property";
                                var conflictingType = conflictingMemberMap.MemberInfo.DeclaringType;

                                string message;
                                if (conflictingType == _classType)
                                {
                                    message = string.Format(
                                        "The {0} '{1}' of type '{2}' cannot use element name '{3}' because it is already being used by {4} '{5}'.",
                                        fieldOrProperty, memberMap.MemberName, _classType.FullName, memberMap.ElementName, conflictingFieldOrProperty, conflictingMemberMap.MemberName);
                                }
                                else
                                {
                                    message = string.Format(
                                        "The {0} '{1}' of type '{2}' cannot use element name '{3}' because it is already being used by {4} '{5}' of type '{6}'.",
                                        fieldOrProperty, memberMap.MemberName, _classType.FullName, memberMap.ElementName, conflictingFieldOrProperty, conflictingMemberMap.MemberName, conflictingType.FullName);
                                }
                                throw new BsonSerializationException(message);
                            }
                        }

                        // mark this classMap frozen before we start working on knownTypes
                        // because we might get back to this same classMap while processing knownTypes
                        _frozen = true;

                        // use a queue to postpone processing of known types until we get back to the first level call to Freeze
                        // this avoids infinite recursion when going back down the inheritance tree while processing known types
                        foreach (var knownType in _knownTypes)
                        {
                            __knownTypesQueue.Enqueue(knownType);
                        }

                        // if we are back to the first level go ahead and process any queued known types
                        if (__freezeNestingLevel == 1)
                        {
                            while (__knownTypesQueue.Count != 0)
                            {
                                var knownType = __knownTypesQueue.Dequeue();
                                LookupClassMap(knownType); // will AutoMap and/or Freeze knownType if necessary
                            }
                        }
                    }
                    finally
                    {
                        __freezeNestingLevel--;
                    }
                }
            }
            finally
            {
                BsonSerializer.ConfigLock.ExitWriteLock();
            }
            return this;
        }

        /// <summary>
        /// Gets a member map (only considers members declared in this class).
        /// </summary>
        /// <param name="memberName">The member name.</param>
        /// <returns>The member map (or null if the member was not found).</returns>
        public BsonMemberMap GetMemberMap(string memberName)
        {
            if (memberName == null)
            {
                throw new ArgumentNullException("memberName");
            }

            // can be called whether frozen or not
            return _declaredMemberMaps.Find(m => m.MemberName == memberName);
        }

        /// <summary>
        /// Gets the member map for a BSON element.
        /// </summary>
        /// <param name="elementName">The name of the element.</param>
        /// <returns>The member map.</returns>
        public BsonMemberMap GetMemberMapForElement(string elementName)
        {
            if (elementName == null)
            {
                throw new ArgumentNullException("elementName");
            }

            if (!_frozen) { ThrowNotFrozenException(); }
            BsonMemberMap memberMap;
            _elementDictionary.TryGetValue(elementName, out memberMap);
            return memberMap;
        }

        /// <summary>
        /// Creates a member map for the extra elements field and adds it to the class map.
        /// </summary>
        /// <param name="fieldName">The name of the extra elements field.</param>
        /// <returns>The member map (so method calls can be chained).</returns>
        public BsonMemberMap MapExtraElementsField(string fieldName)
        {
            if (fieldName == null)
            {
                throw new ArgumentNullException("fieldName");
            }

            if (_frozen) { ThrowFrozenException(); }
            var fieldMap = MapField(fieldName);
            SetExtraElementsMember(fieldMap);
            return fieldMap;
        }

        /// <summary>
        /// Creates a member map for the extra elements member and adds it to the class map.
        /// </summary>
        /// <param name="memberInfo">The member info for the extra elements member.</param>
        /// <returns>The member map (so method calls can be chained).</returns>
        public BsonMemberMap MapExtraElementsMember(MemberInfo memberInfo)
        {
            if (memberInfo == null)
            {
                throw new ArgumentNullException("memberInfo");
            }

            if (_frozen) { ThrowFrozenException(); }
            var memberMap = MapMember(memberInfo);
            SetExtraElementsMember(memberMap);
            return memberMap;
        }

        /// <summary>
        /// Creates a member map for the extra elements property and adds it to the class map.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The member map (so method calls can be chained).</returns>
        public BsonMemberMap MapExtraElementsProperty(string propertyName)
        {
            if (propertyName == null)
            {
                throw new ArgumentNullException("propertyName");
            }

            if (_frozen) { ThrowFrozenException(); }
            var propertyMap = MapProperty(propertyName);
            SetExtraElementsMember(propertyMap);
            return propertyMap;
        }

        /// <summary>
        /// Creates a member map for a field and adds it to the class map.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        /// <returns>The member map (so method calls can be chained).</returns>
        public BsonMemberMap MapField(string fieldName)
        {
            if (fieldName == null)
            {
                throw new ArgumentNullException("fieldName");
            }

            if (_frozen) { ThrowFrozenException(); }
            var fieldInfo = _classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (fieldInfo == null)
            {
                var message = string.Format("The class '{0}' does not have a field named '{1}'.", _classType.FullName, fieldName);
                throw new BsonSerializationException(message);
            }
            return MapMember(fieldInfo);
        }

        /// <summary>
        /// Creates a member map for the Id field and adds it to the class map.
        /// </summary>
        /// <param name="fieldName">The name of the Id field.</param>
        /// <returns>The member map (so method calls can be chained).</returns>
        public BsonMemberMap MapIdField(string fieldName)
        {
            if (fieldName == null)
            {
                throw new ArgumentNullException("fieldName");
            }

            if (_frozen) { ThrowFrozenException(); }
            var fieldMap = MapField(fieldName);
            SetIdMember(fieldMap);
            return fieldMap;
        }

        /// <summary>
        /// Creates a member map for the Id member and adds it to the class map.
        /// </summary>
        /// <param name="memberInfo">The member info for the Id member.</param>
        /// <returns>The member map (so method calls can be chained).</returns>
        public BsonMemberMap MapIdMember(MemberInfo memberInfo)
        {
            if (memberInfo == null)
            {
                throw new ArgumentNullException("memberInfo");
            }

            if (_frozen) { ThrowFrozenException(); }
            var memberMap = MapMember(memberInfo);
            SetIdMember(memberMap);
            return memberMap;
        }

        /// <summary>
        /// Creates a member map for the Id property and adds it to the class map.
        /// </summary>
        /// <param name="propertyName">The name of the Id property.</param>
        /// <returns>The member map (so method calls can be chained).</returns>
        public BsonMemberMap MapIdProperty(string propertyName)
        {
            if (propertyName == null)
            {
                throw new ArgumentNullException("propertyName");
            }

            if (_frozen) { ThrowFrozenException(); }
            var propertyMap = MapProperty(propertyName);
            SetIdMember(propertyMap);
            return propertyMap;
        }

        /// <summary>
        /// Creates a member map for a member and adds it to the class map.
        /// </summary>
        /// <param name="memberInfo">The member info.</param>
        /// <returns>The member map (so method calls can be chained).</returns>
        public BsonMemberMap MapMember(MemberInfo memberInfo)
        {
            if (memberInfo == null)
            {
                throw new ArgumentNullException("memberInfo");
            }
            EnsureMemberInfoIsForThisClass(memberInfo);

            if (_frozen) { ThrowFrozenException(); }
            var memberMap = _declaredMemberMaps.Find(m => m.MemberInfo == memberInfo);
            if (memberMap == null)
            {
                memberMap = new BsonMemberMap(this, memberInfo);
                _declaredMemberMaps.Add(memberMap);
            }
            return memberMap;
        }

        /// <summary>
        /// Creates a member map for a property and adds it to the class map.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The member map (so method calls can be chained).</returns>
        public BsonMemberMap MapProperty(string propertyName)
        {
            if (propertyName == null)
            {
                throw new ArgumentNullException("propertyName");
            }

            if (_frozen) { ThrowFrozenException(); }
            var propertyInfo = _classType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (propertyInfo == null)
            {
                var message = string.Format("The class '{0}' does not have a property named '{1}'.", _classType.FullName, propertyName);
                throw new BsonSerializationException(message);
            }
            return MapMember(propertyInfo);
        }

        /// <summary>
        /// Sets the discriminator.
        /// </summary>
        /// <param name="discriminator">The discriminator.</param>
        public void SetDiscriminator(string discriminator)
        {
            if (discriminator == null)
            {
                throw new ArgumentNullException("discriminator");
            }

            if (_frozen) { ThrowFrozenException(); }
            _discriminator = discriminator;
        }

        /// <summary>
        /// Sets whether a discriminator is required when serializing this class.
        /// </summary>
        /// <param name="discriminatorIsRequired">Whether a discriminator is required.</param>
        public void SetDiscriminatorIsRequired(bool discriminatorIsRequired)
        {
            if (_frozen) { ThrowFrozenException(); }
            _discriminatorIsRequired = discriminatorIsRequired;
        }

        /// <summary>
        /// Sets the member map of the member used to hold extra elements.
        /// </summary>
        /// <param name="memberMap">The extra elements member map.</param>
        public void SetExtraElementsMember(BsonMemberMap memberMap)
        {
            if (memberMap == null)
            {
                throw new ArgumentNullException("memberMap");
            }
            EnsureMemberMapIsForThisClass(memberMap);

            if (_frozen) { ThrowFrozenException(); }
            if (_extraElementsMemberMap != null)
            {
                var message = string.Format("Class {0} already has an extra elements member.", _classType.FullName);
                throw new InvalidOperationException(message);
            }
            if (memberMap.MemberType != typeof(BsonDocument) && !typeof(IDictionary<string, object>).IsAssignableFrom(memberMap.MemberType))
            {
                var message = string.Format("Type of ExtraElements member must be BsonDocument or implement IDictionary<string, object>.");
                throw new InvalidOperationException(message);
            }

            _extraElementsMemberMap = memberMap;
        }

        /// <summary>
        /// Sets the Id member.
        /// </summary>
        /// <param name="memberMap">The Id member.</param>
        public void SetIdMember(BsonMemberMap memberMap)
        {
            if (memberMap == null)
            {
                throw new ArgumentNullException("memberMap");
            }
            EnsureMemberMapIsForThisClass(memberMap);

            if (_frozen) { ThrowFrozenException(); }
            if (_idMemberMap != null)
            {
                var message = string.Format("Class {0} already has an Id.", _classType.FullName);
                throw new InvalidOperationException(message);
            }

            memberMap.SetElementName("_id");
            _idMemberMap = memberMap;
        }

        /// <summary>
        /// Sets whether extra elements should be ignored when deserializing.
        /// </summary>
        /// <param name="ignoreExtraElements">Whether extra elements should be ignored when deserializing.</param>
        public void SetIgnoreExtraElements(bool ignoreExtraElements)
        {
            if (_frozen) { ThrowFrozenException(); }
            _ignoreExtraElements = ignoreExtraElements;
        }

        /// <summary>
        /// Sets whether the IgnoreExtraElements value should be inherited by derived classes.
        /// </summary>
        /// <param name="ignoreExtraElementsIsInherited">Whether the IgnoreExtraElements value should be inherited by derived classes.</param>
        public void SetIgnoreExtraElementsIsInherited(bool ignoreExtraElementsIsInherited)
        {
            if (_frozen) { ThrowFrozenException(); }
            _ignoreExtraElementsIsInherited = ignoreExtraElementsIsInherited;
        }

        /// <summary>
        /// Sets whether this class is a root class.
        /// </summary>
        /// <param name="isRootClass">Whether this class is a root class.</param>
        public void SetIsRootClass(bool isRootClass)
        {
            if (_frozen) { ThrowFrozenException(); }
            _isRootClass = isRootClass;
        }

        /// <summary>
        /// Removes the member map for a field from the class map.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        public void UnmapField(string fieldName)
        {
            if (fieldName == null)
            {
                throw new ArgumentNullException("fieldName");
            }

            if (_frozen) { ThrowFrozenException(); }
            var fieldInfo = _classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (fieldInfo == null)
            {
                var message = string.Format("The class '{0}' does not have a field named '{1}'.", _classType.FullName, fieldName);
                throw new BsonSerializationException(message);
            }
            UnmapMember(fieldInfo);
        }

        /// <summary>
        /// Removes a member map from the class map.
        /// </summary>
        /// <param name="memberInfo">The member info.</param>
        public void UnmapMember(MemberInfo memberInfo)
        {
            if (memberInfo == null)
            {
                throw new ArgumentNullException("memberInfo");
            }
            EnsureMemberInfoIsForThisClass(memberInfo);

            if (_frozen) { ThrowFrozenException(); }
            var memberMap = _declaredMemberMaps.Find(m => m.MemberInfo == memberInfo);
            if (memberMap != null)
            {
                _declaredMemberMaps.Remove(memberMap);
                if (_idMemberMap == memberMap)
                {
                    _idMemberMap = null;
                }
                if (_extraElementsMemberMap == memberMap)
                {
                    _extraElementsMemberMap = null;
                }
            }
        }

        /// <summary>
        /// Removes the member map for a property from the class map.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        public void UnmapProperty(string propertyName)
        {
            if (propertyName == null)
            {
                throw new ArgumentNullException("propertyName");
            }

            if (_frozen) { ThrowFrozenException(); }
            var propertyInfo = _classType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (propertyInfo == null)
            {
                var message = string.Format("The class '{0}' does not have a property named '{1}'.", _classType.FullName, propertyName);
                throw new BsonSerializationException(message);
            }
            UnmapMember(propertyInfo);
        }

        // private methods
        private void AutoMapClass()
        {
            foreach (BsonKnownTypesAttribute knownTypesAttribute in _classType.GetCustomAttributes(typeof(BsonKnownTypesAttribute), false))
            {
                foreach (var knownType in knownTypesAttribute.KnownTypes)
                {
                    _knownTypes.Add(knownType); // knownTypes will be processed when Freeze is called
                }
            }

            var discriminatorAttribute = (BsonDiscriminatorAttribute)_classType.GetCustomAttributes(typeof(BsonDiscriminatorAttribute), false).FirstOrDefault();
            if (discriminatorAttribute != null)
            {
                if (discriminatorAttribute.Discriminator != null)
                {
                    _discriminator = discriminatorAttribute.Discriminator;
                }
                _discriminatorIsRequired = discriminatorAttribute.Required;
                _isRootClass = discriminatorAttribute.RootClass;
            }

            var ignoreExtraElementsAttribute = (BsonIgnoreExtraElementsAttribute)_classType.GetCustomAttributes(typeof(BsonIgnoreExtraElementsAttribute), false).FirstOrDefault();
            if (ignoreExtraElementsAttribute != null)
            {
                _ignoreExtraElements = ignoreExtraElementsAttribute.IgnoreExtraElements;
                _ignoreExtraElementsIsInherited = ignoreExtraElementsAttribute.Inherited;
            }
            else
            {
                _ignoreExtraElements = _conventions.IgnoreExtraElementsConvention.IgnoreExtraElements(_classType);
            }

            AutoMapMembers();
        }

        private void AutoMapMembers()
        {
            // only auto map properties declared in this class (and not in base classes)
            var hasOrderedElements = false;
            foreach (var memberInfo in FindMembers())
            {
                var memberMap = AutoMapMember(memberInfo);
                hasOrderedElements = hasOrderedElements || memberMap.Order != int.MaxValue;
            }

            if (hasOrderedElements)
            {
                // split out the items with a value for Order and sort them separately (because Sort is unstable, see online help)
                // and then concatenate any items with no value for Order at the end (in their original order)
                var sorted = new List<BsonMemberMap>(_declaredMemberMaps.Where(pm => pm.Order != int.MaxValue));
                var unsorted = new List<BsonMemberMap>(_declaredMemberMaps.Where(pm => pm.Order == int.MaxValue));
                sorted.Sort((x, y) => x.Order.CompareTo(y.Order));
                _declaredMemberMaps = sorted.Concat(unsorted).ToList();
            }
        }

        private BsonMemberMap AutoMapMember(MemberInfo memberInfo)
        {
            var memberMap = MapMember(memberInfo);

            memberMap.SetElementName(_conventions.ElementNameConvention.GetElementName(memberInfo));
            bool ignoreIfDefault;
#pragma warning disable 618 // SerializeDefaultValueConvention is obsolete
            if (_conventions.SerializeDefaultValueConvention != null)
            {
                ignoreIfDefault = !_conventions.SerializeDefaultValueConvention.SerializeDefaultValue(memberInfo);
            }
#pragma warning restore 618
            else
            {
                ignoreIfDefault = _conventions.IgnoreIfDefaultConvention.IgnoreIfDefault(memberInfo);
            }
            memberMap.SetIgnoreIfDefault(ignoreIfDefault);
            memberMap.SetIgnoreIfNull(_conventions.IgnoreIfNullConvention.IgnoreIfNull(memberInfo));

            var defaultValue = _conventions.DefaultValueConvention.GetDefaultValue(memberInfo);
            if (defaultValue != null)
            {
                memberMap.SetDefaultValue(defaultValue);
            }

            // see if the class has a method called ShouldSerializeXyz where Xyz is the name of this member
            var shouldSerializeMethod = GetShouldSerializeMethod(memberInfo);
            if (shouldSerializeMethod != null)
            {
                memberMap.SetShouldSerializeMethod(shouldSerializeMethod);
            }

            var serializationOptions = _conventions.SerializationOptionsConvention.GetSerializationOptions(memberInfo);
            if (serializationOptions != null)
            {
                memberMap.SetSerializationOptions(serializationOptions);
            }

            foreach (Attribute attribute in memberInfo.GetCustomAttributes(false))
            {
                if (!(attribute is BsonSerializationOptionsAttribute))
                {
                    // ignore all attributes that aren't BSON serialization related
                    continue;
                }

                var defaultValueAttribute = attribute as BsonDefaultValueAttribute;
                if (defaultValueAttribute != null)
                {
                    memberMap.SetDefaultValue(defaultValueAttribute.DefaultValue);
#pragma warning disable 618 // SerializeDefaultValue is obsolete
                    if (defaultValueAttribute.SerializeDefaultValueWasSet)
                    {
                        memberMap.SetIgnoreIfNull(false);
                        memberMap.SetIgnoreIfDefault(!defaultValueAttribute.SerializeDefaultValue);
                    }
#pragma warning restore 618
                    continue;
                }

                var elementAttribute = attribute as BsonElementAttribute;
                if (elementAttribute != null)
                {
                    memberMap.SetElementName(elementAttribute.ElementName);
                    memberMap.SetOrder(elementAttribute.Order);
                    continue;
                }

                var extraElementsAttribute = attribute as BsonExtraElementsAttribute;
                if (extraElementsAttribute != null)
                {
                    SetExtraElementsMember(memberMap);
                    continue;
                }

                var idAttribute = attribute as BsonIdAttribute;
                if (idAttribute != null)
                {
                    memberMap.SetElementName("_id");
                    memberMap.SetOrder(idAttribute.Order);
                    var idGeneratorType = idAttribute.IdGenerator;
                    if (idGeneratorType != null)
                    {
                        var idGenerator = (IIdGenerator)Activator.CreateInstance(idGeneratorType); // public default constructor required
                        memberMap.SetIdGenerator(idGenerator);
                    }
                    SetIdMember(memberMap);
                    continue;
                }

                var ignoreIfDefaultAttribute = attribute as BsonIgnoreIfDefaultAttribute;
                if (ignoreIfDefaultAttribute != null)
                {
                    memberMap.SetIgnoreIfNull(false);
                    memberMap.SetIgnoreIfDefault(ignoreIfDefaultAttribute.Value);
                    continue;
                }

                var ignoreIfNullAttribute = attribute as BsonIgnoreIfNullAttribute;
                if (ignoreIfNullAttribute != null)
                {
                    memberMap.SetIgnoreIfDefault(false);
                    memberMap.SetIgnoreIfNull(ignoreIfNullAttribute.Value);
                    continue;
                }

                var requiredAttribute = attribute as BsonRequiredAttribute;
                if (requiredAttribute != null)
                {
                    memberMap.SetIsRequired(true);
                    continue;
                }

                // if none of the above apply then apply the attribute to the serialization options
                var memberSerializer = memberMap.GetSerializer(memberMap.MemberType);
                var memberSerializationOptions = memberMap.SerializationOptions;
                if (memberSerializationOptions == null)
                {
                    var memberDefaultSerializationOptions = memberSerializer.GetDefaultSerializationOptions();
                    if (memberDefaultSerializationOptions == null)
                    {
                        var message = string.Format(
                            "A serialization options attribute of type {0} cannot be used when the serializer is of type {1}.",
                            BsonUtils.GetFriendlyTypeName(attribute.GetType()),
                            BsonUtils.GetFriendlyTypeName(memberSerializer.GetType()));
                        throw new NotSupportedException(message);
                    }
                    memberSerializationOptions = memberDefaultSerializationOptions.Clone();
                    memberMap.SetSerializationOptions(memberSerializationOptions);
                }
                memberSerializationOptions.ApplyAttribute(memberSerializer, attribute);
            }

            return memberMap;
        }

        private void EnsureMemberInfoIsForThisClass(MemberInfo memberInfo)
        {
            if (memberInfo.DeclaringType != _classType)
            {
                var message = string.Format(
                    "The memberInfo argument must be for class {0}, but was for class {1}.",
                    _classType.Name,
                    memberInfo.DeclaringType.Name);
                throw new ArgumentOutOfRangeException("memberInfo", message);
            }
        }

        private void EnsureMemberMapIsForThisClass(BsonMemberMap memberMap)
        {
            if (memberMap.ClassMap != this)
            {
                var message = string.Format(
                    "The memberMap argument must be for class {0}, but was for class {1}.",
                    _classType.Name,
                    memberMap.ClassMap.ClassType.Name);
                throw new ArgumentOutOfRangeException("memberMap", message);
            }
        }

        private IEnumerable<MemberInfo> FindMembers()
        {
            // use a List instead of a HashSet to preserver order
            var memberInfos = new List<MemberInfo>(_conventions.MemberFinderConvention.FindMembers(_classType));

            // let other fields opt-in if they have a BsonElement attribute
            foreach (var fieldInfo in _classType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                var elementAttribute = (BsonElementAttribute)fieldInfo.GetCustomAttributes(typeof(BsonElementAttribute), false).FirstOrDefault();
                if (elementAttribute == null || fieldInfo.IsInitOnly || fieldInfo.IsLiteral)
                {
                    continue;
                }

                if (!memberInfos.Contains(fieldInfo))
                {
                    memberInfos.Add(fieldInfo);
                }
            }

            // let other properties opt-in if they have a BsonElement attribute
            foreach (var propertyInfo in _classType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                var elementAttribute = (BsonElementAttribute)propertyInfo.GetCustomAttributes(typeof(BsonElementAttribute), false).FirstOrDefault();
                if (elementAttribute == null || !propertyInfo.CanRead || (!propertyInfo.CanWrite && !_isAnonymous))
                {
                    continue;
                }

                if (!memberInfos.Contains(propertyInfo))
                {
                    memberInfos.Add(propertyInfo);
                }
            }

            foreach (var memberInfo in memberInfos)
            {
                var ignoreAttribute = (BsonIgnoreAttribute)memberInfo.GetCustomAttributes(typeof(BsonIgnoreAttribute), false).FirstOrDefault();
                if (ignoreAttribute != null)
                {
                    continue; // ignore this property
                }

                yield return memberInfo;
            }
        }

        private Func<object> GetCreator()
        {
            if (_creator == null)
            {
                Expression body;
                var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                var defaultConstructor = _classType.GetConstructor(bindingFlags, null, new Type[0], null);
                if (defaultConstructor != null)
                {
                    // lambdaExpression = () => (object) new TClass()
                    body = Expression.New(defaultConstructor);
                }
                else
                {
                    // lambdaExpression = () => FormatterServices.GetUninitializedObject(classType)
                    var getUnitializedObjectMethodInfo = typeof(FormatterServices).GetMethod("GetUninitializedObject", BindingFlags.Public | BindingFlags.Static);
                    body = Expression.Call(getUnitializedObjectMethodInfo, Expression.Constant(_classType));
                }
                var lambdaExpression = Expression.Lambda<Func<object>>(body);
                _creator = lambdaExpression.Compile();
            }
            return _creator;
        }

        private Func<object, bool> GetShouldSerializeMethod(MemberInfo memberInfo)
        {
            var shouldSerializeMethodName = "ShouldSerialize" + memberInfo.Name;
            var shouldSerializeMethodInfo = _classType.GetMethod(shouldSerializeMethodName, new Type[] { });
            if (shouldSerializeMethodInfo != null &&
                shouldSerializeMethodInfo.IsPublic &&
                shouldSerializeMethodInfo.ReturnType == typeof(bool))
            {
                // lambdaExpression = (obj) => ((TClass) obj).ShouldSerializeXyz()
                var objParameter = Expression.Parameter(typeof(object), "obj");
                var lambdaExpression = Expression.Lambda<Func<object, bool>>(Expression.Call(Expression.Convert(objParameter, _classType), shouldSerializeMethodInfo), objParameter);
                return lambdaExpression.Compile();
            }
            else
            {
                return null;
            }
        }

        private bool IsAnonymousType(Type type)
        {
            // don't test for too many things in case implementation details change in the future
            return
                Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false) &&
                type.IsGenericType &&
                type.Name.Contains("Anon"); // don't check for more than "Anon" so it works in mono also
        }

        private void ThrowFrozenException()
        {
            var message = string.Format("Class map for {0} has been frozen and no further changes are allowed.", _classType.FullName);
            throw new InvalidOperationException(message);
        }

        private void ThrowNotFrozenException()
        {
            var message = string.Format("Class map for {0} has been not been frozen yet.", _classType.FullName);
            throw new InvalidOperationException(message);
        }

        // private class
        private class FilteredConventionProfile
        {
            public Func<Type, bool> Filter;
            public ConventionProfile Profile;
        }
    }

    /// <summary>
    /// Represents a mapping between a class and a BSON document.
    /// </summary>
    /// <typeparam name="TClass">The class.</typeparam>
    public class BsonClassMap<TClass> : BsonClassMap
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonClassMap class.
        /// </summary>
        public BsonClassMap()
            : base(typeof(TClass))
        {
        }

        /// <summary>
        /// Initializes a new instance of the BsonClassMap class.
        /// </summary>
        /// <param name="classMapInitializer">The class map initializer.</param>
        public BsonClassMap(Action<BsonClassMap<TClass>> classMapInitializer)
            : base(typeof(TClass))
        {
            classMapInitializer(this);
        }

        // public methods
        /// <summary>
        /// Gets a member map.
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberLambda">A lambda expression specifying the member.</param>
        /// <returns>The member map.</returns>
        public BsonMemberMap GetMemberMap<TMember>(Expression<Func<TClass, TMember>> memberLambda)
        {
            var memberName = GetMemberNameFromLambda(memberLambda);
            return GetMemberMap(memberName);
        }

        /// <summary>
        /// Creates a member map for the extra elements field and adds it to the class map.
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="fieldLambda">A lambda expression specifying the extra elements field.</param>
        /// <returns>The member map.</returns>
        public BsonMemberMap MapExtraElementsField<TMember>(Expression<Func<TClass, TMember>> fieldLambda)
        {
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
        public BsonMemberMap MapExtraElementsMember<TMember>(Expression<Func<TClass, TMember>> memberLambda)
        {
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
        public BsonMemberMap MapExtraElementsProperty<TMember>(Expression<Func<TClass, TMember>> propertyLambda)
        {
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
        public BsonMemberMap MapField<TMember>(Expression<Func<TClass, TMember>> fieldLambda)
        {
            return MapMember(fieldLambda);
        }

        /// <summary>
        /// Creates a member map for the Id field and adds it to the class map.
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="fieldLambda">A lambda expression specifying the Id field.</param>
        /// <returns>The member map.</returns>
        public BsonMemberMap MapIdField<TMember>(Expression<Func<TClass, TMember>> fieldLambda)
        {
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
        public BsonMemberMap MapIdMember<TMember>(Expression<Func<TClass, TMember>> memberLambda)
        {
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
        public BsonMemberMap MapIdProperty<TMember>(Expression<Func<TClass, TMember>> propertyLambda)
        {
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
        public BsonMemberMap MapMember<TMember>(Expression<Func<TClass, TMember>> memberLambda)
        {
            var memberInfo = GetMemberInfoFromLambda(memberLambda);
            return MapMember(memberInfo);
        }

        /// <summary>
        /// Creates a member map for the Id property and adds it to the class map.
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="propertyLambda">A lambda expression specifying the Id property.</param>
        /// <returns>The member map.</returns>
        public BsonMemberMap MapProperty<TMember>(Expression<Func<TClass, TMember>> propertyLambda)
        {
            return MapMember(propertyLambda);
        }

        /// <summary>
        /// Removes the member map for a field from the class map.
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="fieldLambda">A lambda expression specifying the field.</param>
        public void UnmapField<TMember>(Expression<Func<TClass, TMember>> fieldLambda)
        {
            UnmapMember(fieldLambda);
        }

        /// <summary>
        /// Removes a member map from the class map.
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="memberLambda">A lambda expression specifying the member.</param>
        public void UnmapMember<TMember>(Expression<Func<TClass, TMember>> memberLambda)
        {
            var memberInfo = GetMemberInfoFromLambda(memberLambda);
            UnmapMember(memberInfo);
        }

        /// <summary>
        /// Removes a member map for a property from the class map.
        /// </summary>
        /// <typeparam name="TMember">The member type.</typeparam>
        /// <param name="propertyLambda">A lambda expression specifying the property.</param>
        public void UnmapProperty<TMember>(Expression<Func<TClass, TMember>> propertyLambda)
        {
            UnmapMember(propertyLambda);
        }

        // private methods
        private static MemberInfo GetMemberInfoFromLambda<TMember>(Expression<Func<TClass, TMember>> memberLambda)
        {
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
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Field:
                case MemberTypes.Property:
                    break;
                default:
                    throw new BsonSerializationException("Invalid lambda expression");
            }
            return memberInfo;
        }

        private static string GetMemberNameFromLambda<TMember>(Expression<Func<TClass, TMember>> memberLambda)
        {
            return GetMemberInfoFromLambda(memberLambda).Name;
        }
    }
}
