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

using MongoDB.BsonLibrary.IO;

namespace MongoDB.BsonLibrary.Serialization {
    public abstract class BsonPropertyMap {
        #region protected fields
        protected string propertyName;
        protected string elementName;
        protected PropertyInfo propertyInfo;
        protected Func<object, object> getter;
        protected Action<object, object> setter;
        protected IBsonPropertySerializer propertySerializer;
        protected bool useCompactRepresentation;
        protected bool isPolymorphicProperty;
        protected bool isRequired;
        protected bool hasDefaultValue;
        protected object defaultValue;
        #endregion

        #region constructors
        protected BsonPropertyMap(
            PropertyInfo propertyInfo,
            string elementName
        ) {
            this.propertyName = propertyInfo.Name;
            this.elementName = elementName;
            this.propertyInfo = propertyInfo;
            this.isPolymorphicProperty = IsPolymorphicType(propertyInfo.PropertyType);
        }
        #endregion

        #region public properties
        public string PropertyName {
            get { return propertyName; }
        }

        public string ElementName {
            get { return elementName; }
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

        public IBsonPropertySerializer PropertySerializer {
            get {
                if (propertySerializer == null) {
                    propertySerializer = BsonClassMap.LookupPropertySerializer(propertyInfo.PropertyType);
                }
                return propertySerializer;
            }
        }

        public bool UseCompactRepresentation {
            get { return useCompactRepresentation; }
        }

        public bool IsPolymorphicProperty {
            get { return isPolymorphicProperty; }
        }

        public bool IsRequired {
            get { return isRequired; }
        }

        public bool HasDefaultValue {
            get { return hasDefaultValue; }
        }

        public object DefaultValue {
            get { return defaultValue; }
        }
        #endregion

        #region public static methods
        public static bool IsPolymorphicType(
            Type type
        ) {
            if (type.IsAbstract) { return true; }
            if (type.IsInterface) { return true; }
            if (type == typeof(object)) { return true; }
            // TODO: return true if type has derived classes?
            return false;
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

        public BsonPropertyMap SetDefaultValue(
            object defaultValue
        ) {
            this.hasDefaultValue = true;
            this.defaultValue = defaultValue;
            return this;
        }

        public BsonPropertyMap SetIsRequired(
            bool isRequired
        ) {
            this.isRequired = isRequired;
            return this;
        }

        public BsonPropertyMap SetPropertySerializer(
            IBsonPropertySerializer propertySerializer
        ) {
            this.propertySerializer = propertySerializer;
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
            string elementName
        )
            : base(propertyInfo, elementName) {
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
