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

using MongoDB.BsonLibrary.IO;

namespace MongoDB.BsonLibrary.Serialization {
    public abstract class BsonClassMap {
        #region private static fields
        private static object staticLock = new object();
        private static Dictionary<Type, BsonClassMap> classMaps = new Dictionary<Type, BsonClassMap>();
        private static Dictionary<Type, IBsonPropertySerializer> propertySerializers = new Dictionary<Type, IBsonPropertySerializer>();
        #endregion

        #region protected fields
        protected Type classType;
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
        }
        #endregion

        #region public properties
        public Type ClassType {
            get { return classType; }
        }

        public string CollectionName {
            get { return collectionName; }
        }

        public BsonPropertyMap IdPropertyMap {
            get { return idPropertyMap; }
        }

        public IEnumerable<BsonPropertyMap> PropertyMaps {
            get { return propertyMaps; }
        }
        #endregion

        #region public static methods
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
                IBsonPropertySerializer propertySerializer;
                if (propertySerializers.TryGetValue(propertyType, out propertySerializer)) {
                    return propertySerializer;
                } else {
                    string message = string.Format("No property serializer found property type: {0}", propertyType.FullName);
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
                classMaps[classMap.ClassType] = classMap;
            }
        }

        public static void RegisterPropertySerializer(
            IBsonPropertySerializer propertySerializer
        ) {
            lock (staticLock) {
                propertySerializers[propertySerializer.PropertyType] = propertySerializer;
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
                if (typeof(IBsonPropertySerializer).IsAssignableFrom(type) && !type.IsInterface) {
                    var singletonPropertyInfo = type.GetProperty("Singleton", BindingFlags.Public | BindingFlags.Static);
                    var singleton = (IBsonPropertySerializer) singletonPropertyInfo.GetValue(null, null);
                    RegisterPropertySerializer(singleton);
                }
            }
        }
        #endregion

        #region public methods
        public void AutoMap() {
            foreach (var propertyInfo in classType.GetProperties()) {
                if (propertyInfo.CanRead && (propertyInfo.CanWrite || IsAnonymousType(classType))) {
                    var genericMapPropertyInfo = this.GetType().GetMethod(
                        "MapProperty", // name
                        BindingFlags.NonPublic | BindingFlags.Instance,
                        null, // binder
                        new Type[] { typeof(PropertyInfo), typeof(string) },
                        null // modifiers
                    );
                    var mapPropertyInfo = genericMapPropertyInfo.MakeGenericMethod(propertyInfo.PropertyType);

                    var elementName = propertyInfo.Name;
                    mapPropertyInfo.Invoke(this, new object[] { propertyInfo, elementName });
                }
            }
        }

        public object CreateObject(
            BsonReader bsonReader
        ) {
            var obj = Activator.CreateInstance(classType); // TODO: peek at discriminator
            return obj;
        }

        public BsonPropertyMap GetPropertyMapForElement(
            string elementName
        ) {
            return PropertyMaps.FirstOrDefault(pm => pm.ElementName == elementName);
        }
        #endregion

        #region private methods
        private bool IsAnonymousType(
            Type type
        ) {
            // TODO: figure out if this is a reliable test
            return type.Namespace == null;
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
