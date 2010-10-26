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
using System.Reflection;
using System.Text;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.DefaultSerializer.Conventions;
using System.Linq.Expressions;

namespace MongoDB.Bson.DefaultSerializer {
    public abstract class BsonMemberMap {
        #region protected fields
        protected ConventionProfile conventions;
        protected string elementName;
        protected int order = int.MaxValue;
        protected MemberInfo memberInfo;
        protected Type memberType;
        protected Func<object, object> getter;
        protected Action<object, object> setter;
        protected IBsonSerializer serializer;
        protected IBsonIdGenerator idGenerator;
        protected bool useCompactRepresentation;
        protected bool isRequired;
        protected bool hasDefaultValue;
        protected bool serializeDefaultValue = true;
        protected bool ignoreIfNull;
        protected object defaultValue;
        #endregion

        #region constructors
        protected BsonMemberMap(
            MemberInfo memberInfo,
            string elementName,
            ConventionProfile conventions
        ) {
            this.elementName = elementName;
            this.memberInfo = memberInfo;
            this.memberType = BsonUtils.GetMemberInfoType(memberInfo);
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
            get { return elementName; }
        }

        public int Order {
            get { return order; }
        }

        public MemberInfo MemberInfo {
            get { return memberInfo; }
        }

        public abstract Func<object, object> Getter {
            get;
        }

        public abstract Action<object, object> Setter {
            get;
        }

        public IBsonIdGenerator IdGenerator {
            get {
                if (idGenerator == null) {
                    idGenerator = conventions.BsonIdGeneratorConvention.GetBsonIdGenerator(memberInfo);
                }
                return idGenerator;
            }
        }

        public bool UseCompactRepresentation {
            get { return useCompactRepresentation; }
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
            IBsonIdGenerator idGenerator
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

        public BsonMemberMap SetUseCompactRepresentation(
            bool useCompactRepresentation
        ) {
            this.useCompactRepresentation = useCompactRepresentation;
            return this;
        }
        #endregion
    }

    public class BsonMemberMap<TClass, TMember> : BsonMemberMap {
        #region constructors
        public BsonMemberMap(
            MemberInfo memberInfo,
            string elementName,
            ConventionProfile conventions
        )
            : base(memberInfo, elementName, conventions) {
        }
        #endregion

        #region public properties
        public override Func<object, object> Getter {
            get {
                if (getter == null) {
                    var instance = Expression.Parameter(typeof(object), "obj");
                    var lambda = Expression.Lambda<Func<object, object>>(
                        Expression.Convert(
                            Expression.MakeMemberAccess(
                                Expression.Convert(instance, memberInfo.DeclaringType),
                                memberInfo),
                            typeof(object)),
                        instance);

                    getter = lambda.Compile();
                }
                return getter;
            }
        }

        public override Action<object, object> Setter {
            get {
                if (setter == null) {
                    if (memberInfo.MemberType == MemberTypes.Field) {
                        setter = GetFieldSetter();
                    }
                    else {
                        setter = GetPropertySetter();
                    }
                }
                return setter;
            }
        }
        #endregion

        #region private methods
        private Action<object, object> GetFieldSetter() {
            throw new NotImplementedException();
        }

        private Action<object, object> GetPropertySetter() {
            var setMethodInfo = ((PropertyInfo)memberInfo).GetSetMethod(true);
            var instance = Expression.Parameter(typeof(object), "obj");
            var argument = Expression.Parameter(typeof(object), "a");
            var lambda = Expression.Lambda<Action<object, object>>(
                Expression.Call(
                    Expression.Convert(instance, memberInfo.DeclaringType), 
                    setMethodInfo,
                    Expression.Convert(argument, memberType)),
                instance, 
                argument);

            return lambda.Compile();
        }
        #endregion
    }
}
