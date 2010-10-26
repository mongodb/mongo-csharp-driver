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
using System.Text;
using System.Text.RegularExpressions;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.DefaultSerializer.Conventions;

namespace MongoDB.Bson.DefaultSerializer
{
    public abstract class BsonClassMap
    {
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
        protected ConventionProfile conventions;
        protected string discriminator;
        protected bool discriminatorIsRequired;
        protected bool isAnonymous;
        protected bool idMemberMapLoaded; // lazy load idMemberMap
        protected BsonMemberMap idMemberMap;
        protected List<BsonMemberMap> memberMaps = new List<BsonMemberMap>();
        protected bool ignoreExtraElements = true;
        protected bool useCompactRepresentation;
        #endregion

        #region constructors
        protected BsonClassMap(
            Type classType
        )
        {
            this.classType = classType;
            this.conventions = LookupConventions(classType);
            this.discriminator = classType.Name;
            this.isAnonymous = IsAnonymousType(classType);
        }
        #endregion

        #region public properties
        public BsonClassMap BaseClassMap
        {
            get
            {
                if (!baseClassMapLoaded) { LoadBaseClassMap(); }
                return baseClassMap;
            }
        }

        public Type ClassType
        {
            get { return classType; }
        }

        public string Discriminator
        {
            get { return discriminator; }
        }

        public bool DiscriminatorIsRequired
        {
            get { return discriminatorIsRequired; }
        }

        public bool IsAnonymous
        {
            get { return isAnonymous; }
        }

        public BsonMemberMap IdMemberMap
        {
            get
            {
                if (!idMemberMapLoaded) { LoadIdMemberMap(); }
                return idMemberMap;
            }
        }

        public IEnumerable<BsonMemberMap> MemberMaps
        {
            get
            {
                var baseClassMap = BaseClassMap; // call property for lazy loading
                if (baseClassMap != null)
                {
                    return baseClassMap.MemberMaps.Concat(memberMaps);
                }
                else
                {
                    return memberMaps;
                }
            }
        }

        public bool IgnoreExtraElements
        {
            get { return ignoreExtraElements; }
        }

        public bool UseCompactRepresentation
        {
            get { return useCompactRepresentation; }
        }
        #endregion

        #region public static methods
        // this is like the AssemblyQualifiedName but shortened where possible
        public static string GetTypeNameDiscriminator(
            Type type
        )
        {
            string typeName;
            if (type.IsGenericType)
            {
                var genericTypeNames = "";
                foreach (var genericType in type.GetGenericArguments())
                {
                    var genericTypeName = GetTypeNameDiscriminator(genericType);
                    if (genericTypeName.Contains(','))
                    {
                        genericTypeName = "[" + genericTypeName + "]";
                    }
                    if (genericTypeNames != "")
                    {
                        genericTypeNames += ",";
                    }
                    genericTypeNames += genericTypeName;
                }
                typeName = type.GetGenericTypeDefinition().FullName + "[" + genericTypeNames + "]";
            }
            else
            {
                typeName = type.FullName;
            }

            string assemblyName = type.Assembly.FullName;
            Match match = Regex.Match(assemblyName, "(?<dll>[^,]+), Version=[^,]+, Culture=[^,]+, PublicKeyToken=(?<token>[^,]+)");
            if (match.Success)
            {
                var dll = match.Groups["dll"].Value;
                var publicKeyToken = match.Groups["token"].Value;
                if (dll == "mscorlib")
                {
                    assemblyName = null;
                }
                else if (publicKeyToken == "null")
                {
                    assemblyName = dll;
                }
            }

            if (assemblyName == null)
            {
                return typeName;
            }
            else
            {
                return typeName + ", " + assemblyName;
            }
        }

        public static Type LookupActualType(
            Type nominalType,
            string discriminator
        )
        {
            if (discriminator == null)
            {
                return nominalType;
            }

            // TODO: will there be too much contention on staticLock?
            lock (staticLock)
            {
                Type actualType = null;

                LookupClassMap(nominalType); // make sure any "known types" of nominal type have been registered
                List<Type> typeList;
                if (discriminatedTypes.TryGetValue(discriminator, out typeList))
                {
                    foreach (var type in typeList)
                    {
                        if (nominalType.IsAssignableFrom(type))
                        {
                            if (actualType == null)
                            {
                                actualType = type;
                            }
                            else
                            {
                                string message = string.Format("Ambiguous discriminator: {0}", discriminator);
                                throw new BsonSerializationException(message);
                            }
                        }
                    }
                }

                if (actualType == null)
                {
                    actualType = Type.GetType(discriminator); // see if it's a Type name
                }

                if (actualType == null)
                {
                    string message = string.Format("Unknown discriminator value: {0}", discriminator);
                    throw new BsonSerializationException(message);
                }

                if (!nominalType.IsAssignableFrom(actualType))
                {
                    string message = string.Format("Actual type {0} is not assignable to expected type {1}", actualType.FullName, nominalType.FullName);
                    throw new FileFormatException(message);
                }

                return actualType;
            }
        }

        public static BsonClassMap LookupClassMap(
            Type classType
        )
        {
            lock (staticLock)
            {
                BsonClassMap classMap;
                if (classMaps.TryGetValue(classType, out classMap))
                {
                    return classMap;
                }
                else
                {
                    // automatically register a class map for classType
                    var registerClassMapMethodDefinition = typeof(BsonClassMap).GetMethod(
                        "RegisterClassMap", // name
                        BindingFlags.Public | BindingFlags.Static, // bindingAttr
                        null, // binder
                        new Type[] { }, // types
                        null // modifiers
                    );
                    var registerClassMapMethodInfo = registerClassMapMethodDefinition.MakeGenericMethod(classType);
                    return (BsonClassMap)registerClassMapMethodInfo.Invoke(null, new object[] { });
                }
            }
        }

        public static ConventionProfile LookupConventions(
            Type type
        )
        {
            for (int i = 0; i < profiles.Count; i++)
            {
                if (profiles[i].Filter(type))
                {
                    return profiles[i].Profile;
                }
            }

            return defaultProfile;
        }

        public static BsonClassMap<TClass> RegisterClassMap<TClass>()
        {
            return RegisterClassMap<TClass>(cm => { cm.AutoMap(); });
        }

        public static BsonClassMap<TClass> RegisterClassMap<TClass>(
            Action<BsonClassMap<TClass>> classMapInitializer
        )
        {
            var classMap = new BsonClassMap<TClass>(classMapInitializer);
            RegisterClassMap(classMap);
            return classMap;
        }

        public static void RegisterClassMap(
            BsonClassMap classMap
        )
        {
            lock (staticLock)
            {
                // note: class maps can NOT be replaced (because derived classes refer to existing instance)
                classMaps.Add(classMap.ClassType, classMap);
            }
        }

        public static void RegisterConventions(
            ConventionProfile conventions,
            Func<Type, bool> filter
        )
        {
            conventions.Merge(defaultProfile); // make sure all conventions exists
            var filtered = new FilteredConventionProfile
            {
                Filter = filter,
                Profile = conventions
            };
            profiles.Add(filtered);
        }

        public static void RegisterDiscriminator(
            Type type,
            string discriminator
        )
        {
            lock (staticLock)
            {
                List<Type> typeList;
                if (!discriminatedTypes.TryGetValue(discriminator, out typeList))
                {
                    typeList = new List<Type>();
                    discriminatedTypes.Add(discriminator, typeList);
                }
                if (!typeList.Contains(type))
                {
                    typeList.Add(type);
                }
            }
        }

        public static void UnregisterClassMap(
            Type classType
        )
        {
            lock (staticLock)
            {
                classMaps.Remove(classType);
            }
        }

        public static void UnregisterConventions(
            ConventionProfile conventions
        )
        {
            for (int i = 0; i < profiles.Count; i++)
            {
                if (profiles[i].Profile == conventions)
                {
                    profiles.RemoveAt(i);
                    return;
                }
            }
        }
        #endregion

        #region public methods
        public void AutoMap() {
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

            var useCompactRepresentationAttribute = (BsonUseCompactRepresentationAttribute) classType.GetCustomAttributes(typeof(BsonUseCompactRepresentationAttribute), false).FirstOrDefault();
            if (useCompactRepresentationAttribute != null) {
                useCompactRepresentation = useCompactRepresentationAttribute.UseCompactRepresentation;
            } else {
                useCompactRepresentation = conventions.UseCompactRepresentationConvention.UseCompactRepresentation(classType);
            }

            // only auto map properties declared in this class (and not in base classes)
            var hasOrderedElements = false;
            var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            foreach (var memberInfo in FindMembers()) {
                var mapMemberDefinition = this.GetType().GetMethod(
                    "MapMember", // name
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null, // binder
                    new Type[] { typeof(MemberInfo), typeof(string) },
                    null // modifiers
                );
                var mapMethodInfo = mapMemberDefinition.MakeGenericMethod(BsonUtils.GetMemberInfoType(memberInfo));

                var elementName = conventions.ElementNameConvention.GetElementName(memberInfo);
                var order = int.MaxValue;
                IBsonIdGenerator idGenerator = null;

                var idAttribute = (BsonIdAttribute) memberInfo.GetCustomAttributes(typeof(BsonIdAttribute), false).FirstOrDefault();
                if (idAttribute != null) {
                    elementName = "_id"; // if BsonIdAttribute is present ignore BsonElementAttribute
                    var idGeneratorType = idAttribute.IdGenerator;
                    if (idGeneratorType != null) {
                        idGenerator = (IBsonIdGenerator) Activator.CreateInstance(idGeneratorType);
                    }
                } else {
                    var elementAttribute = (BsonElementAttribute) memberInfo.GetCustomAttributes(typeof(BsonElementAttribute), false).FirstOrDefault();
                    if (elementAttribute != null) {
                        elementName = elementAttribute.ElementName;
                        order = elementAttribute.Order;
                    }
                }

                var memberMap = (BsonMemberMap) mapMethodInfo.Invoke(this, new object[] { memberInfo, elementName });
                if (order != int.MaxValue) {
                    memberMap.SetOrder(order);
                    hasOrderedElements = true;
                }
                if (idAttribute != null) {
                    idMemberMap = memberMap;
                    idMemberMap.SetIdGenerator(idGenerator);
                }

                var defaultValueAttribute = (BsonDefaultValueAttribute) memberInfo.GetCustomAttributes(typeof(BsonDefaultValueAttribute), false).FirstOrDefault();
                if (defaultValueAttribute != null) {
                    memberMap.SetDefaultValue(defaultValueAttribute.DefaultValue);
                    memberMap.SetSerializeDefaultValue(defaultValueAttribute.SerializeDefaultValue);
                } else {
                    var defaultValue = conventions.DefaultValueConvention.GetDefaultValue(memberMap.MemberInfo);
                    if (defaultValue != null) {
                        memberMap.SetDefaultValue(defaultValue);
                    }
                    memberMap.SetSerializeDefaultValue(conventions.SerializeDefaultValueConvention.SerializeDefaultValue(memberMap.MemberInfo));
                }

                var ignoreIfNullAttribute = (BsonIgnoreIfNullAttribute) memberInfo.GetCustomAttributes(typeof(BsonIgnoreIfNullAttribute), false).FirstOrDefault();
                if (ignoreIfNullAttribute != null) {
                    memberMap.SetIgnoreIfNull(true);
                } else {
                    memberMap.SetIgnoreIfNull(conventions.IgnoreIfNullConvention.IgnoreIfNull(memberMap.MemberInfo));
                }

                var requiredAttribute = (BsonRequiredAttribute) memberInfo.GetCustomAttributes(typeof(BsonRequiredAttribute), false).FirstOrDefault();
                if (requiredAttribute != null) {
                    memberMap.SetIsRequired(true);
                }

                memberMap.SetUseCompactRepresentation(useCompactRepresentation);
                useCompactRepresentationAttribute = (BsonUseCompactRepresentationAttribute) memberInfo.GetCustomAttributes(typeof(BsonUseCompactRepresentationAttribute), false).FirstOrDefault();
                if (useCompactRepresentationAttribute != null) {
                    memberMap.SetUseCompactRepresentation(useCompactRepresentationAttribute.UseCompactRepresentation);
                } else {
                    // default useCompactRepresentation to true for primitive property types
                    if (memberMap.MemberType.IsPrimitive) {
                        memberMap.SetUseCompactRepresentation(true);
                    }
                }
            }

            if (hasOrderedElements) {
                // split out the items with a value for Order and sort them separately (because Sort is unstable, see online help)
                // and then concatenate any items with no value for Order at the end (in their original order)
                var ordered = new List<BsonMemberMap>(memberMaps.Where(pm => pm.Order != int.MaxValue));
                ordered.Sort((x, y) => x.Order.CompareTo(y.Order));
                memberMaps = new List<BsonMemberMap>(ordered.Concat(memberMaps.Where(pm => pm.Order == int.MaxValue)));
            }

            RegisterDiscriminator(classType, discriminator);
        }

        public BsonMemberMap GetMemberMap(
            string memberName
        )
        {
            return MemberMaps.FirstOrDefault(pm => pm.MemberName == memberName);
        }

        public BsonMemberMap GetMemberMapForElement(
            string elementName
        )
        {
            return MemberMaps.FirstOrDefault(pm => pm.ElementName == elementName);
        }

        public BsonClassMap SetDiscriminator(
            string discriminator
        )
        {
            this.discriminator = discriminator;
            return this;
        }

        public BsonClassMap SetDiscriminatorIsRequired(
            bool discriminatorIsRequired
        )
        {
            this.discriminatorIsRequired = discriminatorIsRequired;
            return this;
        }

        public BsonClassMap SetIgnoreExtraElements(
            bool ignoreExtraElements
        )
        {
            this.ignoreExtraElements = ignoreExtraElements;
            return this;
        }

        public BsonClassMap SetUseCompactRepresentation(
            bool useCompactRepresentation
        )
        {
            this.useCompactRepresentation = useCompactRepresentation;
            return this;
        }
        #endregion

        #region private methods
        private IEnumerable<MemberInfo> FindMembers() {
            var memberInfos = new HashSet<MemberInfo>();
            foreach(var fieldInfo in classType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)) {
                if (fieldInfo.IsInitOnly || fieldInfo.IsLiteral) {//we can't write
                    continue;
                }

                memberInfos.Add(fieldInfo);
            }

            foreach(var propertyInfo in classType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (!propertyInfo.CanRead || (!propertyInfo.CanWrite && !isAnonymous)) {
                    continue;
                }

                memberInfos.Add(propertyInfo);
            }

            //Let other fields opt-in if they have a BsonElement attribute
            foreach (var fieldInfo in classType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                var elementAttribute = (BsonElementAttribute)fieldInfo.GetCustomAttributes(typeof(BsonElementAttribute), false).FirstOrDefault();
                if (elementAttribute == null || fieldInfo.IsInitOnly || fieldInfo.IsLiteral)
                {//we can't write
                    continue;
                }

                memberInfos.Add(fieldInfo);
            }

            //Let other properties opt-in if they have a BsonElement attribute
            foreach (var propertyInfo in classType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                var elementAttribute = (BsonElementAttribute)propertyInfo.GetCustomAttributes(typeof(BsonElementAttribute), false).FirstOrDefault();
                if (elementAttribute == null || !propertyInfo.CanRead || (!propertyInfo.CanWrite && !isAnonymous)) {
                    continue;
                }

                memberInfos.Add(propertyInfo);
            }

            foreach(var memberInfo in memberInfos)
            {
                var ignoreAttribute = (BsonIgnoreAttribute) memberInfo.GetCustomAttributes(typeof(BsonIgnoreAttribute), false).FirstOrDefault();
                if (ignoreAttribute != null) {
                    continue; // ignore this property
                }

                yield return memberInfo;
            }
        }

        private bool IsAnonymousType(
            Type type
        )
        {
            // TODO: figure out if this is a reliable test
            return type.Namespace == null;
        }

        private void LoadBaseClassMap()
        {
            var baseType = classType.BaseType;
            if (baseType != null)
            {
                baseClassMap = LookupClassMap(baseType);
                if (baseClassMap.DiscriminatorIsRequired)
                {
                    discriminatorIsRequired = true; // only inherit true values
                }
            }
            baseClassMapLoaded = true;
        }

        private void LoadIdMemberMap()
        {
            if (idMemberMap == null)
            {
                // the IdMemberMap should be provided by the highest class possible in the inheritance hierarchy
                var baseClassMap = BaseClassMap; // call BaseClassMap property for lazy loading
                if (baseClassMap != null)
                {
                    idMemberMap = baseClassMap.IdMemberMap;
                }

                // if no base class provided an idMemberMap maybe we have one?
                if (idMemberMap == null)
                {
                    var memberName = conventions.IdMemberConvention.FindIdMember(classType);
                    if (memberName != null)
                    {
                        idMemberMap = GetMemberMap(memberName);
                        if (idMemberMap != null)
                        {
                            idMemberMap.SetElementName("_id");
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

    public class BsonClassMap<TClass> : BsonClassMap
    {
        #region constructors
        public BsonClassMap(
            Action<BsonClassMap<TClass>> classMapInitializer
        )
            : base(typeof(TClass))
        {
            classMapInitializer(this);
        }
        #endregion

        #region public methods
        public BsonMemberMap GetMemberMap<TMember>(
            Expression<Func<TClass, TMember>> memberLambda
        )
        {
            var memberName = GetMemberNameFromLambda(memberLambda);
            return memberMaps.FirstOrDefault(mm => mm.MemberInfo.Name == memberName);
        }

        public BsonMemberMap MapId<TMember>(
            Expression<Func<TClass, TMember>> memberLambda
        )
        {
            var memberInfo = GetMemberInfoFromLambda(memberLambda);
            var elementName = "_id";
            idMemberMap = MapMember<TMember>(memberInfo, elementName);
            return idMemberMap;
        }

        public BsonMemberMap MapMember<TMember>(
            Expression<Func<TClass, TMember>> memberLambda
        )
        {
            var memberInfo = GetMemberInfoFromLambda(memberLambda);
            var elementName = memberInfo.Name;
            return MapMember<TMember>(memberInfo, elementName);
        }

        public BsonMemberMap MapMember<TMember>(
            Expression<Func<TClass, TMember>> memberLambda,
            string elementName
        )
        {
            var memberInfo = GetMemberInfoFromLambda(memberLambda);
            return MapMember<TMember>(memberInfo, elementName);
        }
        #endregion

        #region private methods
        private MemberInfo GetMemberInfoFromLambda<TMember>(
            Expression<Func<TClass, TMember>> memberLambda
        )
        {
            var memberName = GetMemberNameFromLambda(memberLambda);
            return classType.GetMember(memberName).SingleOrDefault(x => x.MemberType == MemberTypes.Field || x.MemberType == MemberTypes.Property);
        }

        private string GetMemberNameFromLambda<TMember>(
            Expression<Func<TClass, TMember>> memberLambda
        )
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
                    throw new BsonSerializationException("Invalid propertyLambda");
            }
            return memberExpression.Member.Name;
        }

        private BsonMemberMap MapMember<TMember>(
            MemberInfo memberInfo,
            string elementName
        )
        {
            var memberMap = new BsonMemberMap<TClass, TMember>(memberInfo, elementName, conventions);
            memberMaps.Add(memberMap);
            return memberMap;
        }
        #endregion
    }
}
