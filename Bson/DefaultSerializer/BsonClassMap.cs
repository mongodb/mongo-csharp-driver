/* Copyright 2010 10gen Inc.
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
using System.Text;
using System.Text.RegularExpressions;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.DefaultSerializer.Conventions;
using System.Runtime.Serialization;

namespace MongoDB.Bson.DefaultSerializer {
    public abstract class BsonClassMap {
        #region private static fields
        private static object staticLock = new object();
        private static List<FilteredConventionProfile> profiles = new List<FilteredConventionProfile>();
        private static ConventionProfile defaultProfile = ConventionProfile.GetDefault();
        private static Dictionary<Type, BsonClassMap> classMaps = new Dictionary<Type, BsonClassMap>();
        private static int freezeNestingLevel = 0;
        private static Queue<Type> knownTypesQueue = new Queue<Type>();
        #endregion

        #region protected fields
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
        protected List<Type> knownTypes = new List<Type>();
        #endregion

        #region constructors
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
        public BsonClassMap BaseClassMap {
            get { return baseClassMap; }
        }

        public Type ClassType {
            get { return classType; }
        }

        public string Discriminator {
            get { return discriminator; }
        }

        public bool DiscriminatorIsRequired {
            get { return discriminatorIsRequired; }
        }

        public bool HasRootClass {
            get { return hasRootClass; }
        }

        public bool IsAnonymous {
            get { return isAnonymous; }
        }

        public bool IsRootClass {
            get { return isRootClass; }
        }

        public BsonMemberMap IdMemberMap {
            get { return idMemberMap; }
        }

        public IEnumerable<Type> KnownTypes {
            get { return knownTypes; }
        }

        public IEnumerable<BsonMemberMap> MemberMaps {
            get { return allMemberMaps; }
        }

        public bool IgnoreExtraElements {
            get { return ignoreExtraElements; }
        }
        #endregion

        #region public static methods
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

        // this is like the AssemblyQualifiedName but shortened where possible
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

        public static BsonClassMap LookupClassMap(
            Type classType
        ) {
            lock (staticLock) {
                BsonClassMap classMap;
                if (!classMaps.TryGetValue(classType, out classMap)) {
                    // automatically create a classMap for classType and register it
                    var classMapDefinition = typeof(BsonClassMap<>);
                    var classMapType = classMapDefinition.MakeGenericType(classType);
                    classMap = (BsonClassMap) Activator.CreateInstance(classMapType);
                    classMap.AutoMap();
                    RegisterClassMap(classMap);
                }
                classMap.Freeze();
                return classMap;
            }
        }

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

        public static BsonClassMap<TClass> RegisterClassMap<TClass>() {
            return RegisterClassMap<TClass>(cm => { cm.AutoMap(); });
        }

        public static BsonClassMap<TClass> RegisterClassMap<TClass>(
            Action<BsonClassMap<TClass>> classMapInitializer
        ) {
            var classMap = new BsonClassMap<TClass>(classMapInitializer);
            RegisterClassMap(classMap);
            return classMap;
        }

        public static void RegisterClassMap(
            BsonClassMap classMap
        ) {
            lock (staticLock) {
                // note: class maps can NOT be replaced (because derived classes refer to existing instance)
                classMaps.Add(classMap.ClassType, classMap);
                BsonDefaultSerializer.RegisterDiscriminator(classMap.ClassType, classMap.Discriminator);
            }
        }

        public static void RegisterConventions(
            ConventionProfile conventions,
            Func<Type, bool> filter
        ) {
            conventions.Merge(defaultProfile); // make sure all conventions exists
            var filtered = new FilteredConventionProfile {
                Filter = filter,
                Profile = conventions
            };
            profiles.Add(filtered);
        }

        public static void UnregisterConventions(
            ConventionProfile conventions
        ) {
            for (int i = 0; i < profiles.Count; i++) {
                if (profiles[i].Profile == conventions) {
                    profiles.RemoveAt(i);
                    return;
                }
            }
        }
        #endregion

        #region public methods
        public void AutoMap() {
            if (frozen) { ThrowFrozenException(); }
            AutoMapClass();
        }

        public object CreateInstance() {
            if (!frozen) { ThrowNotFrozenException(); }
            var creator = GetCreator();
            return creator.Invoke();
        }

        public void Freeze() {
            lock (staticLock) {
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

                        foreach (var memberMap in allMemberMaps) {
                            if (!elementDictionary.ContainsKey(memberMap.ElementName)) {
                                elementDictionary.Add(memberMap.ElementName, memberMap);
                            } else {
                                var message = string.Format("Duplicate element name '{0}' in class '{1}'", memberMap.MemberName, classType.FullName);
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
            }
        }

        public BsonMemberMap GetMemberMap(
            string memberName
        ) {
            // can be called whether frozen or not
            return declaredMemberMaps.Find(m => m.MemberName == memberName);
        }

        public BsonMemberMap GetMemberMapForElement(
            string elementName
        ) {
            if (!frozen) { ThrowNotFrozenException(); }
            BsonMemberMap memberMap;
            elementDictionary.TryGetValue(elementName, out memberMap);
            return memberMap;
        }

        public BsonMemberMap MapField(
            string fieldName
        ) {
            if (frozen) { ThrowFrozenException(); }
            var fieldInfo = classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (fieldInfo == null) {
                var message = string.Format("The class '{0}' does not have a field named '{1}'", classType.FullName, fieldName);
                throw new BsonSerializationException(message);
            }
            return MapMember(fieldInfo);
        }

        public BsonMemberMap MapIdField(
            string fieldName
        ) {
            if (frozen) { ThrowFrozenException(); }
            var fieldMap = MapField(fieldName);
            SetIdMember(fieldMap);
            return fieldMap;
        }

        public BsonMemberMap MapIdMember(
            MemberInfo memberInfo
        ) {
            if (frozen) { ThrowFrozenException(); }
            var memberMap = MapMember(memberInfo);
            SetIdMember(memberMap);
            return memberMap;
        }

        public BsonMemberMap MapIdProperty(
            string propertyName
        ) {
            if (frozen) { ThrowFrozenException(); }
            var propertyMap = MapProperty(propertyName);
            SetIdMember(propertyMap);
            return propertyMap;
        }

        public BsonMemberMap MapMember(
            MemberInfo memberInfo
        ) {
            if (frozen) { ThrowFrozenException(); }
            if (memberInfo == null) {
                throw new ArgumentNullException("memberInfo");
            }
            if (memberInfo.DeclaringType != classType) {
                throw new ArgumentException("MemberInfo is not for this class");
            }
            var memberMap = declaredMemberMaps.Find(m => m.MemberInfo == memberInfo);
            if (memberMap == null) {
                memberMap = new BsonMemberMap(memberInfo, conventions);
                declaredMemberMaps.Add(memberMap);
            }
            return memberMap;
        }

        public BsonMemberMap MapProperty(
            string propertyName
        ) {
            if (frozen) { ThrowFrozenException(); }
            var propertyInfo = classType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (propertyInfo == null) {
                var message = string.Format("The class '{0}' does not have a property named '{1}'", classType.FullName, propertyName);
                throw new BsonSerializationException(message);
            }
            return MapMember(propertyInfo);
        }

        public BsonClassMap SetDiscriminator(
            string discriminator
        ) {
            if (frozen) { ThrowFrozenException(); }
            this.discriminator = discriminator;
            return this;
        }

        public BsonClassMap SetDiscriminatorIsRequired(
            bool discriminatorIsRequired
        ) {
            if (frozen) { ThrowFrozenException(); }
            this.discriminatorIsRequired = discriminatorIsRequired;
            return this;
        }

        public void SetIdMember(
            BsonMemberMap memberMap
        ) {
            if (frozen) { ThrowFrozenException(); }
            if (idMemberMap != null) {
                var message = string.Format("Class {0} already has an Id", classType.FullName);
                throw new InvalidOperationException(message);
            }
            if (!declaredMemberMaps.Contains(memberMap)) {
                throw new BsonInternalException("Invalid memberMap");
            }

            memberMap.SetElementName("_id");
            idMemberMap = memberMap;
        }

        public BsonClassMap SetIgnoreExtraElements(
            bool ignoreExtraElements
        ) {
            if (frozen) { ThrowFrozenException(); }
            this.ignoreExtraElements = ignoreExtraElements;
            return this;
        }

        public BsonClassMap SetIsRootClass(
            bool isRootClass
        ) {
            if (frozen) { ThrowFrozenException(); }
            this.isRootClass = isRootClass;
            return this;
        }

        public void UnmapField(
            string fieldName
        ) {
            if (frozen) { ThrowFrozenException(); }
            var fieldInfo = classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (fieldInfo == null) {
                var message = string.Format("The class '{0}' does not have a field named '{1}'", classType.FullName, fieldName);
                throw new BsonSerializationException(message);
            }
            UnmapMember(fieldInfo);
        }

        public void UnmapMember(
            MemberInfo memberInfo
        ) {
            if (frozen) { ThrowFrozenException(); }
            if (memberInfo == null) {
                throw new ArgumentNullException("memberInfo");
            }
            if (memberInfo.DeclaringType != classType) {
                throw new ArgumentException("MemberInfo is not for this class");
            }
            var memberMap = declaredMemberMaps.Find(m => m.MemberInfo == memberInfo);
            if (memberMap != null) {
                declaredMemberMaps.Remove(memberMap);
                if (idMemberMap == memberMap) {
                    idMemberMap = null;
                }
            }
        }

        public void UnmapProperty(
            string propertyName
        ) {
            if (frozen) { ThrowFrozenException(); }
            var propertyInfo = classType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (propertyInfo == null) {
                var message = string.Format("The class '{0}' does not have a property named '{1}'", classType.FullName, propertyName);
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
            var message = string.Format("Class map for {0} has been frozen and no further changes are allowed", classType.FullName);
            throw new InvalidOperationException(message);
        }

        private void ThrowNotFrozenException() {
            var message = string.Format("Class map for {0} has been not been frozen yet", classType.FullName);
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

    public class BsonClassMap<TClass> : BsonClassMap {
        #region constructors
        public BsonClassMap()
            : base(typeof(TClass)) {
        }

        public BsonClassMap(
            Action<BsonClassMap<TClass>> classMapInitializer
        ) : base(typeof(TClass)) {
            classMapInitializer(this);
        }
        #endregion

        #region public methods
        public BsonMemberMap GetMemberMap<TMember>(
            Expression<Func<TClass, TMember>> memberLambda
        ) {
            var memberName = GetMemberNameFromLambda(memberLambda);
            return GetMemberMap(memberName);
        }

        public BsonMemberMap MapField<TMember>(
            Expression<Func<TClass, TMember>> fieldLambda
        ) {
            return MapMember(fieldLambda);
        }

        public BsonMemberMap MapIdField<TMember>(
            Expression<Func<TClass, TMember>> fieldLambda
        ) {
            var fieldMap = MapField(fieldLambda);
            SetIdMember(fieldMap);
            return fieldMap;
        }

        public BsonMemberMap MapIdMember<TMember>(
            Expression<Func<TClass, TMember>> memberLambda
        ) {
            var memberMap = MapMember(memberLambda);
            SetIdMember(memberMap);
            return memberMap;
        }

        public BsonMemberMap MapIdProperty<TMember>(
            Expression<Func<TClass, TMember>> propertyLambda
        ) {
            var propertyMap = MapProperty(propertyLambda);
            SetIdMember(propertyMap);
            return propertyMap;
        }

        public BsonMemberMap MapMember<TMember>(
            Expression<Func<TClass, TMember>> memberLambda
        ) {
            var memberInfo = GetMemberInfoFromLambda(memberLambda);
            return MapMember(memberInfo);
        }

        public BsonMemberMap MapProperty<TMember>(
            Expression<Func<TClass, TMember>> propertyLambda
        ) {
            return MapMember(propertyLambda);
        }

        public void UnmapField<TMember>(
            Expression<Func<TClass, TMember>> fieldLambda
        ) {
            UnmapMember(fieldLambda);
        }

        public void UnmapMember<TMember>(
            Expression<Func<TClass, TMember>> memberLambda
        ) {
            var memberInfo = GetMemberInfoFromLambda(memberLambda);
            UnmapMember(memberInfo);
        }

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
                    throw new BsonSerializationException("Invalid propertyLambda");
            }
            return memberExpression.Member.Name;
        }
        #endregion
    }
}
