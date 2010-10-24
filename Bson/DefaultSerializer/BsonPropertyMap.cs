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

namespace MongoDB.Bson.DefaultSerializer {
    public abstract class BsonPropertyMap {
        #region protected fields
        protected ConventionProfile conventions;
        protected string propertyName;
        protected string elementName;
        protected int order = int.MaxValue;
        protected PropertyInfo propertyInfo;
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
        protected BsonPropertyMap(
            PropertyInfo propertyInfo,
            string elementName,
            ConventionProfile conventions
        ) {
            this.propertyName = propertyInfo.Name;
            this.elementName = elementName;
            this.propertyInfo = propertyInfo;
            this.conventions = conventions;
        }
        #endregion

        #region public properties
        public string PropertyName {
            get { return propertyName; }
        }

        public Type PropertyType {
            get { return propertyInfo.PropertyType; }
        }

        public string ElementName {
            get { return elementName; }
        }

        public int Order {
            get { return order; }
        }

        public PropertyInfo PropertyInfo {
            get { return propertyInfo; }
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
                    idGenerator = conventions.BsonIdGeneratorConvention.GetBsonIdGenerator(this.PropertyInfo);
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
                throw new InvalidOperationException("BsonPropertyMap has no default value");
            }
            this.Setter(obj, defaultValue);
        }

        public IBsonSerializer GetSerializerForActualType(
            Type actualType
        ) {
            if (actualType == PropertyType) {
                if (serializer == null) {
                    serializer = BsonSerializer.LookupSerializer(propertyInfo.PropertyType);
                }
                return serializer;
            } else {
                return BsonSerializer.LookupSerializer(actualType);
            }
        }

        public BsonPropertyMap SetDefaultValue(
            object defaultValue
        ) {
            return SetDefaultValue(defaultValue, true); // serializeDefaultValue
        }

        public BsonPropertyMap SetDefaultValue(
            object defaultValue,
            bool serializeDefaultValue
        ) {
            this.hasDefaultValue = true;
            this.serializeDefaultValue = serializeDefaultValue;
            this.defaultValue = defaultValue;
            return this;
        }

        public BsonPropertyMap SetIdGenerator(
            IBsonIdGenerator idGenerator
        ) {
            this.idGenerator = idGenerator;
            return this;
        }

        public BsonPropertyMap SetIgnoreIfNull(
            bool ignoreIfNull
        ) {
            this.ignoreIfNull = ignoreIfNull;
            return this;
        }

        public BsonPropertyMap SetIsRequired(
            bool isRequired
        ) {
            this.isRequired = isRequired;
            return this;
        }

        public BsonPropertyMap SetOrder(
            int order
        ) {
            this.order = order;
            return this;
        }

        public BsonPropertyMap SetSerializer(
            IBsonSerializer serializer
        ) {
            this.serializer = serializer;
            return this;
        }

        public BsonPropertyMap SetSerializeDefaultValue(
            bool serializeDefaultValue
        ) {
            this.serializeDefaultValue = serializeDefaultValue;
            return this;
        }

        public BsonPropertyMap SetUseCompactRepresentation(
            bool useCompactRepresentation
        ) {
            this.useCompactRepresentation = useCompactRepresentation;
            return this;
        }
        #endregion
    }

    public class BsonPropertyMap<TClass, TProperty> : BsonPropertyMap {
        #region constructors
        public BsonPropertyMap(
            PropertyInfo propertyInfo,
            string elementName,
            ConventionProfile conventions
        )
            : base(propertyInfo, elementName, conventions) {
        }
        #endregion

        #region public properties
        public override Func<object, object> Getter {
            get {
                if (getter == null) {
                    var getMethodInfo = propertyInfo.GetGetMethod();
                    var getMethodDelegate = (Func<TClass, TProperty>) Delegate.CreateDelegate(typeof(Func<TClass, TProperty>), getMethodInfo);
                    getter = obj => getMethodDelegate((TClass) obj);
                }
                return getter;
            }
        }

        public override Action<object, object> Setter {
            get {
                if (setter == null) {
                    var setMethodInfo = propertyInfo.GetSetMethod();
                    var setMethodDelegate = (Action<TClass, TProperty>) Delegate.CreateDelegate(typeof(Action<TClass, TProperty>), setMethodInfo);
                    setter = (obj, value) => setMethodDelegate((TClass) obj, (TProperty) value);
                }
                return setter;
            }
        }
        #endregion
    }
}
