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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using MongoDB.BsonLibrary.IO;
using MongoDB.BsonLibrary.Serialization.PropertySerializers;

namespace MongoDB.BsonLibrary.Serialization {
    public abstract class BsonClassMap {
        #region private static fields
        private static object staticLock = new object();
        private static Dictionary<Type, BsonClassMap> classMaps = new Dictionary<Type, BsonClassMap>();
        private static Dictionary<Type, IBsonPropertySerializer> propertySerializers = new Dictionary<Type, IBsonPropertySerializer>();
        private static Dictionary<string, List<Type>> discriminatedTypes = new Dictionary<string, List<Type>>();
        #endregion

        #region protected fields
        protected bool baseClassMapLoaded; // lazy load baseClassMap so class maps can be constructed out of order
        protected BsonClassMap baseClassMap; // null for class object and interfaces
        protected Type classType;
        protected string discriminator;
        protected bool isAnonymous;
        protected string collectionName;
        protected BsonPropertyMap idPropertyMap;
        protected List<BsonPropertyMap> propertyMaps = new List<BsonPropertyMap>();
        #endregion

        #region static constructor
        static BsonClassMap() {
            RegisterPropertySerializers();
        }
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

        public bool IsAnonymous {
            get { return isAnonymous; }
        }

        public string CollectionName {
            get { return collectionName; }
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

        public static BsonClassMap LookupClassMap(
            Type classType
        ) {
            lock (staticLock) {
                BsonClassMap classMap;
                if (classMaps.TryGetValue(classType, out classMap)) {
                    return classMap;
                } else {
                    // automatically register a class map for classType
                    var genericRegisterClassMapMethodInfo = typeof(BsonClassMap).GetMethod(
                        "RegisterClassMap", // name
                        BindingFlags.Public | BindingFlags.Static, // bindingAttr
                        null, // binder
                        new Type[] { }, // types
                        null // modifiers
                    );
                    var registerClassMapMethodInfo = genericRegisterClassMapMethodInfo.MakeGenericMethod(classType);
                    return (BsonClassMap) registerClassMapMethodInfo.Invoke(null, new object[] { });
                }
            }
        }

        public static IBsonPropertySerializer LookupPropertySerializer(
            Type propertyType
        ) {
            lock (staticLock) {
                if (propertyType.IsGenericType) {
                    propertyType = propertyType.GetGenericTypeDefinition();
                }

                IBsonPropertySerializer propertySerializer;
                if (!propertySerializers.TryGetValue(propertyType, out propertySerializer)) {
                    if (!propertyType.IsPrimitive) {
                        propertySerializer = DefaultPropertySerializer.Singleton;
                        propertySerializers.Add(propertyType, propertySerializer);
                    } else {
                        string message = string.Format("No property serializer found for property type: {0}", propertyType.FullName);
                        throw new BsonSerializationException(message);
                    }
                }
                return propertySerializer;
            }
        }

        public static Type LookupTypeByDiscriminator(
            Type targetType,
            string discriminator
        ) {
            // TODO: will there be too much contention on staticLock?
            lock (staticLock) {
                Type type = null;

                List<Type> typeList;
                if (discriminatedTypes.TryGetValue(discriminator, out typeList)) {
                    foreach (var discriminatedType in typeList) {
                        if (targetType.IsAssignableFrom(discriminatedType)) {
                            if (type == null) {
                                type = discriminatedType;
                            } else {
                                string message = string.Format("Ambiguous discriminator: {0}", discriminator);
                                throw new BsonSerializationException(message);
                            }
                        }
                    }
                }

                if (type != null) {
                    return type;
                } else {
                    string message = string.Format("Unknown discriminator: {0}", discriminator);
                    throw new BsonSerializationException(message);
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

        public static void RegisterPropertySerializer(
            Type type,
            IBsonPropertySerializer propertySerializer
        ) {
            lock (staticLock) {
                // note: property serializers CAN be replaced
                propertySerializers[type] = propertySerializer;
            }
        }

        public static void UnregisterClassMap(
            Type classType
        ) {
            lock (staticLock) {
                classMaps.Remove(classType);
            }
        }

        public static void UnregisterPropertySerializer(
            Type propertyType
        ) {
            lock (staticLock) {
                propertySerializers.Remove(propertyType);
            }
        }
        #endregion

        #region private static methods
        // automatically register all property serializers found in the BsonLibrary
        private static void RegisterPropertySerializers() {
            var assembly = Assembly.GetExecutingAssembly();
            foreach (var type in assembly.GetTypes()) {
                if (typeof(IBsonPropertySerializer).IsAssignableFrom(type) && type != typeof(IBsonPropertySerializer)) {
                    var registerPropertySerializerInfo = type.GetMethod("RegisterPropertySerializer", BindingFlags.Public | BindingFlags.Static);
                    if (registerPropertySerializerInfo != null) {
                        registerPropertySerializerInfo.Invoke(null, null);
                    }
                }
            }
        }
        #endregion

        #region public methods
        public void AutoMap() {
            // only auto map properties declared in this class (and not in base classes)
            var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            foreach (var propertyInfo in classType.GetProperties(bindingFlags)) {
                if (propertyInfo.CanRead && (propertyInfo.CanWrite || isAnonymous)) {
                    var genericMapPropertyInfo = this.GetType().GetMethod(
                        "MapProperty", // name
                        BindingFlags.NonPublic | BindingFlags.Instance,
                        null, // binder
                        new Type[] { typeof(PropertyInfo), typeof(string) },
                        null // modifiers
                    );
                    var mapPropertyInfo = genericMapPropertyInfo.MakeGenericMethod(propertyInfo.PropertyType);

                    var elementName = propertyInfo.Name;
                    var propertyMap = (BsonPropertyMap) mapPropertyInfo.Invoke(this, new object[] { propertyInfo, elementName });

                    if (propertyInfo.GetCustomAttributes(typeof(BsonUseCompactRepresentationAttribute), false).Length != 0) {
                        propertyMap.SetUseCompactRepresentation(true);
                    }
                }
            }

            RegisterDiscriminator(classType, discriminator);
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
