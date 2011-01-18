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
using System.Reflection;
using System.Text;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.DefaultSerializer.Conventions;
using System.Linq.Expressions;
using System.Reflection.Emit;

namespace MongoDB.Bson.DefaultSerializer {
    public class BsonMemberMap {
        #region private fields
        private ConventionProfile conventions;
        private string elementName;
        private int order = int.MaxValue;
        private MemberInfo memberInfo;
        private Type memberType;
        private Func<object, object> getter;
        private Action<object, object> setter;
        private IBsonSerializationOptions serializationOptions;
        private IBsonSerializer serializer;
        private IIdGenerator idGenerator;
        private bool isRequired;
        private bool hasDefaultValue;
        private bool serializeDefaultValue = true;
        private bool ignoreIfNull;
        private object defaultValue;
        #endregion

        #region constructors
        public BsonMemberMap(
            MemberInfo memberInfo,
            ConventionProfile conventions
        ) {
            this.memberInfo = memberInfo;
            this.memberType = BsonClassMap.GetMemberInfoType(memberInfo);
            this.conventions = conventions;
        }
        #endregion

        #region public properties
        public string MemberName {
            get { return memberInfo.Name; }
        }

        public Type MemberType {
            get { return memberType; }
        }

        public string ElementName {
            get {
                if (elementName == null) {
                    elementName = conventions.ElementNameConvention.GetElementName(memberInfo);
                }
                return elementName;
            }
        }

        public int Order {
            get { return order; }
        }

        public MemberInfo MemberInfo {
            get { return memberInfo; }
        }

        public Func<object, object> Getter {
            get {
                if (getter == null) {
                    getter = GetGetter();
                }
                return getter;
            }
        }

        public IBsonSerializationOptions SerializationOptions {
            get { return serializationOptions; }
        }

        public Action<object, object> Setter {
            get {
                if (setter == null) {
                    if (memberInfo.MemberType == MemberTypes.Field) {
                        setter = GetFieldSetter();
                    } else {
                        setter = GetPropertySetter();
                    }
                }
                return setter;
            }
        }

        public IIdGenerator IdGenerator {
            get {
                if (idGenerator == null) {
                    idGenerator = conventions.IdGeneratorConvention.GetIdGenerator(memberInfo);
                }
                return idGenerator;
            }
        }

        public bool IsRequired {
            get { return isRequired; }
        }

        public bool HasDefaultValue {
            get { return hasDefaultValue; }
        }

        public bool SerializeDefaultValue {
            get { return serializeDefaultValue; }
        }

        public bool IgnoreIfNull {
            get { return ignoreIfNull; }
        }

        public object DefaultValue {
            get { return defaultValue; }
        }
        #endregion

        #region public methods
        public void ApplyDefaultValue(
            object obj
        ) {
            if (!hasDefaultValue) {
                throw new InvalidOperationException("BsonMemberMap has no default value");
            }
            this.Setter(obj, defaultValue);
        }

        public IBsonSerializer GetSerializerForActualType(
            Type actualType
        ) {
            if (actualType == memberType) {
                if (serializer == null) {
                    serializer = BsonSerializer.LookupSerializer(memberType);
                }
                return serializer;
            } else {
                return BsonSerializer.LookupSerializer(actualType);
            }
        }

        public BsonMemberMap SetDefaultValue(
            object defaultValue
        ) {
            return SetDefaultValue(defaultValue, true); // serializeDefaultValue
        }

        public BsonMemberMap SetDefaultValue(
            object defaultValue,
            bool serializeDefaultValue
        ) {
            this.hasDefaultValue = true;
            this.serializeDefaultValue = serializeDefaultValue;
            this.defaultValue = defaultValue;
            return this;
        }

        public BsonMemberMap SetElementName(
            string elementName
        ) {
            this.elementName = elementName;
            return this;
        }

        public BsonMemberMap SetIdGenerator(
            IIdGenerator idGenerator
        ) {
            this.idGenerator = idGenerator;
            return this;
        }

        public BsonMemberMap SetIgnoreIfNull(
            bool ignoreIfNull
        ) {
            this.ignoreIfNull = ignoreIfNull;
            return this;
        }

        public BsonMemberMap SetIsRequired(
            bool isRequired
        ) {
            this.isRequired = isRequired;
            return this;
        }

        public BsonMemberMap SetOrder(
            int order
        ) {
            this.order = order;
            return this;
        }

        public BsonMemberMap SetRepresentation(
            BsonType representation
        ) {
            this.serializationOptions = new RepresentationSerializationOptions(representation);
            return this;
        }

        public BsonMemberMap SetSerializationOptions(
            IBsonSerializationOptions serializationOptions
        ) {
            this.serializationOptions = serializationOptions;
            return this;
        }

        public BsonMemberMap SetSerializer(
            IBsonSerializer serializer
        ) {
            this.serializer = serializer;
            return this;
        }

        public BsonMemberMap SetSerializeDefaultValue(
            bool serializeDefaultValue
        ) {
            this.serializeDefaultValue = serializeDefaultValue;
            return this;
        }
        #endregion

        #region private methods
        private Action<object, object> GetFieldSetter() {
            var fieldInfo = (FieldInfo) memberInfo;

            if (fieldInfo.IsInitOnly || fieldInfo.IsLiteral) {
                var message = string.Format("The field '{0} {1}' of class '{2}' is readonly", fieldInfo.FieldType.FullName, fieldInfo.Name, fieldInfo.DeclaringType.FullName);
                throw new BsonSerializationException(message);
            }

            var sourceType = fieldInfo.DeclaringType;
            var method = new DynamicMethod("Set" + fieldInfo.Name, null, new[] { typeof(object), typeof(object) }, true);
            var gen = method.GetILGenerator();

            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Castclass, sourceType);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Unbox_Any, fieldInfo.FieldType);
            gen.Emit(OpCodes.Stfld, fieldInfo);
            gen.Emit(OpCodes.Ret);

            return (Action<object, object>) method.CreateDelegate(typeof(Action<object, object>));
        }

        private Func<object, object> GetGetter() {
            if (memberInfo is PropertyInfo) {
                var propertyInfo = (PropertyInfo) memberInfo;
                var getMethodInfo = propertyInfo.GetGetMethod(true);
                if (getMethodInfo == null) {
                    var message = string.Format("The property '{0} {1}' of class '{2}' has no 'get' accessor", propertyInfo.PropertyType.FullName, propertyInfo.Name, propertyInfo.DeclaringType.FullName);
                    throw new BsonSerializationException(message);
                }
            }

            var instance = Expression.Parameter(typeof(object), "obj");
            var lambda = Expression.Lambda<Func<object, object>>(
                Expression.Convert(
                    Expression.MakeMemberAccess(
                        Expression.Convert(instance, memberInfo.DeclaringType),
                        memberInfo
                    ),
                    typeof(object)
                ),
                instance
            );

            return lambda.Compile();
        }

        private Action<object, object> GetPropertySetter() {
            var propertyInfo = (PropertyInfo) memberInfo;
            var setMethodInfo = propertyInfo.GetSetMethod(true);
            if (setMethodInfo == null) {
                var message = string.Format("The property '{0} {1}' of class '{2}' has no 'set' accessor", propertyInfo.PropertyType.FullName, propertyInfo.Name, propertyInfo.DeclaringType.FullName);
                throw new BsonSerializationException(message);
            }

            var instance = Expression.Parameter(typeof(object), "obj");
            var argument = Expression.Parameter(typeof(object), "a");
            var lambda = Expression.Lambda<Action<object, object>>(
                Expression.Call(
                    Expression.Convert(instance, memberInfo.DeclaringType),
                    setMethodInfo,
                    Expression.Convert(argument, memberType)
                ),
                instance,
                argument
            );

            return lambda.Compile();
        }
        #endregion
    }
}
