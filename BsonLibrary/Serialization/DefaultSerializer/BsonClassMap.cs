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

using MongoDB.BsonLibrary.IO;
using MongoDB.BsonLibrary.Serialization;

namespace MongoDB.BsonLibrary.DefaultSerializer {
    public abstract class BsonClassMap {
        #region private static fields
        private static object staticLock = new object();
        private static Dictionary<Type, BsonClassMap> classMaps = new Dictionary<Type, BsonClassMap>();
        private static Dictionary<string, List<Type>> discriminatedTypes = new Dictionary<string, List<Type>>();
        #endregion

        #region protected fields
        protected bool baseClassMapLoaded; // lazy load baseClassMap so class maps can be constructed out of order
        protected BsonClassMap baseClassMap; // null for class object and interfaces
        protected Type classType;
        protected string discriminator;
        protected bool discriminatorIsRequired;
        protected bool isAnonymous;
        protected BsonPropertyMap idPropertyMap;
        protected List<BsonPropertyMap> propertyMaps = new List<BsonPropertyMap>();
        protected bool ignoreExtraElements = true;
        protected bool useCompactRepresentation;
        #endregion

        #region constructors
        protected BsonClassMap(
            Type classType
        ) {
            this.classType = classType;
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

        public BsonPropertyMap IdPropertyMap {
            get {
                if (idPropertyMap != null) {
                    return idPropertyMap;
                } else {
                    var baseClassMap = BaseClassMap; // call property for lazy loading
                    if (baseClassMap != null) {
                        return baseClassMap.IdPropertyMap;
                    } else {
                        return null;
                    }
                }
            }
        }

        public IEnumerable<BsonPropertyMap> PropertyMaps {
            get {
                var baseClassMap = BaseClassMap; // call property for lazy loading
                if (baseClassMap != null) {
                    return baseClassMap.PropertyMaps.Concat(propertyMaps);
                } else {
                    return propertyMaps;
                }
            }
        }

        public bool IgnoreExtraElements {
            get { return ignoreExtraElements; }
        }

        public bool UseCompactRepresentation {
            get { return useCompactRepresentation; }
        }
        #endregion

        #region public static methods
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
            }

            var useCompactRepresentationAttribute = (BsonUseCompactRepresentationAttribute) classType.GetCustomAttributes(typeof(BsonUseCompactRepresentationAttribute), false).FirstOrDefault();
            if (useCompactRepresentationAttribute != null) {
                useCompactRepresentation = useCompactRepresentationAttribute.UseCompactRepresentation;
            }

            // only auto map properties declared in this class (and not in base classes)
            var hasOrderedElements = false;
            var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            foreach (var propertyInfo in classType.GetProperties(bindingFlags)) {
                if (propertyInfo.CanRead && (propertyInfo.CanWrite || isAnonymous)) {
                    var ignoreAttribute = (BsonIgnoreAttribute) propertyInfo.GetCustomAttributes(typeof(BsonIgnoreAttribute), false).FirstOrDefault();
                    if (ignoreAttribute != null) {
                        continue; // ignore this property
                    }

                    var mapPropertyDefinition = this.GetType().GetMethod(
                        "MapProperty", // name
                        BindingFlags.NonPublic | BindingFlags.Instance,
                        null, // binder
                        new Type[] { typeof(PropertyInfo), typeof(string) },
                        null // modifiers
                    );
                    var mapPropertyInfo = mapPropertyDefinition.MakeGenericMethod(propertyInfo.PropertyType);

                    var elementName = propertyInfo.Name;
                    var order = int.MaxValue;
                    var idAttribute = (BsonIdAttribute) propertyInfo.GetCustomAttributes(typeof(BsonIdAttribute), false).FirstOrDefault();
                    if (idAttribute != null) {
                        elementName = "_id"; // if BsonIdAttribute is present ignore BsonElementAttribute
                    } else {
                        var elementAttribute = (BsonElementAttribute) propertyInfo.GetCustomAttributes(typeof(BsonElementAttribute), false).FirstOrDefault();
                        if (elementAttribute != null) {
                            elementName = elementAttribute.ElementName;
                            order = elementAttribute.Order;
                        }
                    }

                    var propertyMap = (BsonPropertyMap) mapPropertyInfo.Invoke(this, new object[] { propertyInfo, elementName });
                    if (order != int.MaxValue) {
                        propertyMap.SetOrder(order);
                        hasOrderedElements = true;
                    }
                    if (idAttribute != null) {
                        idPropertyMap = propertyMap;
                    }

                    var defaultValueAttribute = (BsonDefaultValueAttribute) propertyInfo.GetCustomAttributes(typeof(BsonDefaultValueAttribute), false).FirstOrDefault();
                    if (defaultValueAttribute != null) {
                        propertyMap.SetDefaultValue(defaultValueAttribute.DefaultValue);
                        propertyMap.SetSerializeDefaultValue(defaultValueAttribute.SerializeDefaultValue);
                    }

                    var ignoreIfNullAttribute = (BsonIgnoreIfNullAttribute) propertyInfo.GetCustomAttributes(typeof(BsonIgnoreIfNullAttribute), false).FirstOrDefault();
                    if (ignoreIfNullAttribute != null) {
                        propertyMap.SetIgnoreIfNull(true);
                    }

                    var requiredAttribute = (BsonRequiredAttribute) propertyInfo.GetCustomAttributes(typeof(BsonRequiredAttribute), false).FirstOrDefault();
                    if (requiredAttribute != null) {
                        propertyMap.SetIsRequired(true);
                    }

                    propertyMap.SetUseCompactRepresentation(useCompactRepresentation);
                    useCompactRepresentationAttribute = (BsonUseCompactRepresentationAttribute) propertyInfo.GetCustomAttributes(typeof(BsonUseCompactRepresentationAttribute), false).FirstOrDefault();
                    if (useCompactRepresentationAttribute != null) {
                        propertyMap.SetUseCompactRepresentation(useCompactRepresentationAttribute.UseCompactRepresentation);
                    }
                }
            }

            if (hasOrderedElements) {
                // split out the items with a value for Order and sort them separately (because Sort is unstable, see online help)
                // and then concatenate any items with no value for Order at the end (in their original order)
                var ordered = new List<BsonPropertyMap>(propertyMaps.Where(pm => pm.Order != int.MaxValue));
                ordered.Sort((x, y) => x.Order.CompareTo(y.Order));
                propertyMaps = new List<BsonPropertyMap>(ordered.Concat(propertyMaps.Where(pm => pm.Order == int.MaxValue)));
            }

            if (idPropertyMap == null) {
                idPropertyMap = propertyMaps.Where(pm => pm.PropertyName.EndsWith("Id")).FirstOrDefault();
            }

            RegisterDiscriminator(classType, discriminator);
        }

        public BsonPropertyMap GetPropertyMap(
            string propertyName
        ) {
            return PropertyMaps.FirstOrDefault(pm => pm.PropertyName == propertyName);
        }

        public BsonPropertyMap GetPropertyMapForElement(
            string elementName
        ) {
            return PropertyMaps.FirstOrDefault(pm => pm.ElementName == elementName);
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

        public BsonClassMap SetUseCompactRepresentation(
            bool useCompactRepresentation
        ) {
            this.useCompactRepresentation = useCompactRepresentation;
            return this;
        }
        #endregion

        #region private methods
        private bool IsAnonymousType(
            Type type
        ) {
            // TODO: figure out if this is a reliable test
            return type.Namespace == null;
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
        #endregion
    }

    public class BsonClassMap<TClass> : BsonClassMap {
        #region constructors
        public BsonClassMap(
            Action<BsonClassMap<TClass>> classMapInitializer
        )
            : base(typeof(TClass)) {
            classMapInitializer(this);
        }
        #endregion

        #region public methods
        public BsonPropertyMap GetPropertyMap<TProperty>(
            Expression<Func<TClass, TProperty>> propertyLambda
        ) {
            var propertyName = GetPropertyNameFromLambda(propertyLambda);
            return propertyMaps.FirstOrDefault(pm => pm.PropertyInfo.Name == propertyName);
        }

        public BsonPropertyMap MapId<TProperty>(
            Expression<Func<TClass, TProperty>> propertyLambda
        ) {
            var propertyInfo = GetPropertyInfoFromLambda(propertyLambda);
            var elementName = "_id";
            idPropertyMap = MapProperty<TProperty>(propertyInfo, elementName);
            return idPropertyMap;
        }

        public BsonPropertyMap MapProperty<TProperty>(
            Expression<Func<TClass, TProperty>> propertyLambda
        ) {
            var propertyInfo = GetPropertyInfoFromLambda(propertyLambda);
            var elementName = propertyInfo.Name;
            return MapProperty<TProperty>(propertyInfo, elementName);
        }

        public BsonPropertyMap MapProperty<TProperty>(
            Expression<Func<TClass, TProperty>> propertyLambda,
            string elementName
        ) {
            var propertyInfo = GetPropertyInfoFromLambda(propertyLambda);
            return MapProperty<TProperty>(propertyInfo, elementName);
        }
        #endregion

        #region private methods
        private PropertyInfo GetPropertyInfoFromLambda<TProperty>(
            Expression<Func<TClass, TProperty>> propertyLambda
        ) {
            var propertyName = GetPropertyNameFromLambda(propertyLambda);
            return classType.GetProperty(propertyName);
        }

        private string GetPropertyNameFromLambda<TProperty>(
            Expression<Func<TClass, TProperty>> propertyLambda
        ) {
            var body = propertyLambda.Body;
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

        private BsonPropertyMap MapProperty<TProperty>(
            PropertyInfo propertyInfo,
            string elementName
        ) {
            var propertyMap = new BsonPropertyMap<TClass, TProperty>(propertyInfo, elementName);
            propertyMaps.Add(propertyMap);
            return propertyMap;
        }
        #endregion
    }
}
