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

namespace MongoDB.Bson.DefaultSerializer {
    public abstract class BsonClassMap {
        #region private static fields
        private static object staticLock = new object();
        private static List<FilteredConventionProfile> profiles = new List<FilteredConventionProfile>();
        private static ConventionProfile defaultProfile = ConventionProfile.GetDefault();
        private static Dictionary<Type, BsonClassMap> classMaps = new Dictionary<Type, BsonClassMap>();
        private static Dictionary<string, List<Type>> discriminatedTypes = new Dictionary<string, List<Type>>();
        #endregion

        #region protected fields
        protected bool baseClassMapLoaded; // lazy load baseClassMap so class maps can be constructed out of order
        protected BsonClassMap baseClassMap; // null for class object and interfaces
        protected Type classType;
        private Func<object> creator;
        protected ConventionProfile conventions;
        protected string discriminator;
        protected bool discriminatorIsRequired;
        protected bool isAnonymous;
        protected bool idMemberMapLoaded; // lazy load idMemberMap
        protected BsonMemberMap idMemberMap;
        protected List<BsonMemberMap> memberMaps = new List<BsonMemberMap>();
        protected bool ignoreExtraElements = true;
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
            get {
                if (!baseClassMapLoaded) { LoadBaseClassMap(); }
                return baseClassMap;
            }
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

        public bool IsAnonymous {
            get { return isAnonymous; }
        }

        public BsonMemberMap IdMemberMap {
            get {
                if (!idMemberMapLoaded) { LoadIdMemberMap(); }
                return idMemberMap;
            }
        }

        public IEnumerable<BsonMemberMap> MemberMaps {
            get {
                var baseClassMap = BaseClassMap; // call property for lazy loading
                if (baseClassMap != null) {
                    return baseClassMap.MemberMaps.Concat(memberMaps);
                } else {
                    return memberMaps;
                }
            }
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

        public static Type LookupActualType(
            Type nominalType,
            string discriminator
        ) {
            if (discriminator == null) {
                return nominalType;
            }

            // TODO: will there be too much contention on staticLock?
            lock (staticLock) {
                Type actualType = null;

                LookupClassMap(nominalType); // make sure any "known types" of nominal type have been registered
                List<Type> typeList;
                if (discriminatedTypes.TryGetValue(discriminator, out typeList)) {
                    foreach (var type in typeList) {
                        if (nominalType.IsAssignableFrom(type)) {
                            if (actualType == null) {
                                actualType = type;
                            } else {
                                string message = string.Format("Ambiguous discriminator: {0}", discriminator);
                                throw new BsonSerializationException(message);
                            }
                        }
                    }
                }

                if (actualType == null) {
                    actualType = Type.GetType(discriminator); // see if it's a Type name
                }

                if (actualType == null) {
                    string message = string.Format("Unknown discriminator value: {0}", discriminator);
                    throw new BsonSerializationException(message);
                }

                if (!nominalType.IsAssignableFrom(actualType)) {
                    string message = string.Format("Actual type {0} is not assignable to expected type {1}", actualType.FullName, nominalType.FullName);
                    throw new FileFormatException(message);
                }

                return actualType;
            }
        }

        public static BsonClassMap LookupClassMap(
            Type classType
        ) {
            lock (staticLock) {
                BsonClassMap classMap;
                if (classMaps.TryGetValue(classType, out classMap)) {
                    return classMap;
                } else {
                    // automatically register a class map for classType
                    var registerClassMapMethodDefinition = typeof(BsonClassMap).GetMethod(
                        "RegisterClassMap", // name
                        BindingFlags.Public | BindingFlags.Static, // bindingAttr
                        null, // binder
                        new Type[] { }, // types
                        null // modifiers
                    );
                    var registerClassMapMethodInfo = registerClassMapMethodDefinition.MakeGenericMethod(classType);
                    return (BsonClassMap) registerClassMapMethodInfo.Invoke(null, new object[] { });
                }
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

        public static void RegisterDiscriminator(
            Type type,
            string discriminator
        ) {
            lock (staticLock) {
                List<Type> typeList;
                if (!discriminatedTypes.TryGetValue(discriminator, out typeList)) {
                    typeList = new List<Type>();
                    discriminatedTypes.Add(discriminator, typeList);
                }
                if (!typeList.Contains(type)) {
                    typeList.Add(type);
                }
            }
        }

        public static void UnregisterClassMap(
            Type classType
        ) {
            lock (staticLock) {
                classMaps.Remove(classType);
            }
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
            AutoMapClass();
            RegisterDiscriminator(classType, discriminator);
        }

        public object CreateInstance() {
            var creator = GetCreator();
            return creator.Invoke();
        }

        public BsonMemberMap GetMemberMap(
            string memberName
        ) {
            return MemberMaps.FirstOrDefault(pm => pm.MemberName == memberName);
        }

        public BsonMemberMap GetMemberMapForElement(
            string elementName
        ) {
            return MemberMaps.FirstOrDefault(pm => pm.ElementName == elementName);
        }

        public BsonMemberMap MapField(
            string fieldName
        ) {
            var fieldInfo = classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            return MapMember(fieldInfo);
        }

        public BsonMemberMap MapIdField(
            string fieldName
        ) {
            var fieldMap = MapField(fieldName);
            SetIdMember(fieldMap);
            return fieldMap;
        }

        public BsonMemberMap MapIdMember(
            MemberInfo memberInfo
        ) {
            var memberMap = MapMember(memberInfo);
            SetIdMember(memberMap);
            return memberMap;
        }

        public BsonMemberMap MapIdProperty(
            string propertyName
        ) {
            var propertyMap = MapProperty(propertyName);
            SetIdMember(propertyMap);
            return propertyMap;
        }

        public BsonMemberMap MapMember(
            MemberInfo memberInfo
        ) {
            var memberMap = new BsonMemberMap(memberInfo, conventions);
            memberMaps.Add(memberMap);
            return memberMap;
        }

        public BsonMemberMap MapProperty(
            string propertyName
        ) {
            var propertyInfo = classType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            return MapMember(propertyInfo);
        }

        public BsonClassMap SetDiscriminator(
            string discriminator
        ) {
            this.discriminator = discriminator;
            return this;
        }

        public BsonClassMap SetDiscriminatorIsRequired(
            bool discriminatorIsRequired
        ) {
            this.discriminatorIsRequired = discriminatorIsRequired;
            return this;
        }

        public BsonClassMap SetIgnoreExtraElements(
            bool ignoreExtraElements
        ) {
            this.ignoreExtraElements = ignoreExtraElements;
            return this;
        }
        #endregion

        #region protected methods
        protected void SetIdMember(
            BsonMemberMap memberMap
        ) {
            if (idMemberMap != null) {
                var message = string.Format("Class {0} already has an Id", classType.FullName);
                throw new InvalidOperationException(message);
            }
            if (!memberMaps.Contains(memberMap)) {
                throw new BsonInternalException("Invalid memberMap");
            }

            memberMap.SetElementName("_id");
            idMemberMap = memberMap;
        }
        #endregion

        #region private methods
        private void AutoMapClass() {
            foreach (BsonKnownTypeAttribute knownTypeAttribute in classType.GetCustomAttributes(typeof(BsonKnownTypeAttribute), false)) {
                BsonClassMap.LookupClassMap(knownTypeAttribute.KnownType); // will AutoMap KnownType if necessary
            }

            var discriminatorAttribute = (BsonDiscriminatorAttribute) classType.GetCustomAttributes(typeof(BsonDiscriminatorAttribute), false).FirstOrDefault();
            if (discriminatorAttribute != null) {
                discriminator = discriminatorAttribute.Discriminator;
                discriminatorIsRequired = discriminatorAttribute.Required;
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
                var ordered = new List<BsonMemberMap>(memberMaps.Where(pm => pm.Order != int.MaxValue));
                ordered.Sort((x, y) => x.Order.CompareTo(y.Order));
                memberMaps = new List<BsonMemberMap>(ordered.Concat(memberMaps.Where(pm => pm.Order == int.MaxValue)));
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
                        var idGenerator = (IBsonIdGenerator) Activator.CreateInstance(idGeneratorType);
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
            var memberInfos = new HashSet<MemberInfo>(
                conventions.MemberFinderConvention.FindMembers(classType)
            );
            
            // let other fields opt-in if they have a BsonElement attribute
            foreach (var fieldInfo in classType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
                var elementAttribute = (BsonElementAttribute)fieldInfo.GetCustomAttributes(typeof(BsonElementAttribute), false).FirstOrDefault();
                if (elementAttribute == null || fieldInfo.IsInitOnly || fieldInfo.IsLiteral) { 
                    continue;
                }

                if(!memberInfos.Contains(fieldInfo)) {
                    memberInfos.Add(fieldInfo);
                }
            }

            // let other properties opt-in if they have a BsonElement attribute
            foreach (var propertyInfo in classType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
                var elementAttribute = (BsonElementAttribute)propertyInfo.GetCustomAttributes(typeof(BsonElementAttribute), false).FirstOrDefault();
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
                var defaultConstructor = classType.GetConstructor(new Type[0]);
                if (defaultConstructor == null) {
                    throw new BsonSerializationException("Cannot find default constructor for type: " + classType.Name);
                }
                var expression = Expression.New(defaultConstructor);
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
                type.Name.Contains("AnonymousType") &&
                (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"));
        }        

        private void LoadBaseClassMap() {
            var baseType = classType.BaseType;
            if (baseType != null) {
                baseClassMap = LookupClassMap(baseType);
                if (baseClassMap.DiscriminatorIsRequired) {
                    discriminatorIsRequired = true; // only inherit true values
                }
            }
            baseClassMapLoaded = true;
        }

        private void LoadIdMemberMap() {
            if (idMemberMap == null) {
                // the IdMemberMap should be provided by the highest class possible in the inheritance hierarchy
                var baseClassMap = BaseClassMap; // call BaseClassMap property for lazy loading
                if (baseClassMap != null) {
                    idMemberMap = baseClassMap.IdMemberMap; // note: don't call SetIdMember because base class already did
                }

                // if no base class provided an idMemberMap maybe we have one?
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

            idMemberMapLoaded = true;
        }
        #endregion

        #region private class
        private class FilteredConventionProfile
        {
            public Func<Type, bool> Filter;
            public ConventionProfile Profile;
        }
        #endregion
    }

    public class BsonClassMap<TClass> : BsonClassMap {
        #region constructors
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
            return memberMaps.FirstOrDefault(mm => mm.MemberInfo.Name == memberName);
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
