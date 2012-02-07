/* Copyright 2010-2012 10gen Inc.
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
using System.Reflection.Emit;
using System.Text;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Represents the mapping between a field or property and a BSON element.
    /// </summary>
    public class BsonMemberMap
    {
        // private fields
        private ConventionProfile _conventions;
        private string _elementName;
        private int _order = int.MaxValue;
        private MemberInfo _memberInfo;
        private Type _memberType;
        private Func<object, object> _getter;
        private Action<object, object> _setter;
        private IBsonSerializationOptions _serializationOptions;
        private IBsonSerializer _serializer;
        private IIdGenerator _idGenerator;
        private bool _isRequired;
        private bool _hasDefaultValue;
        private bool _serializeDefaultValue = true; // for backwards compatibility if a caller specifies a default value
        private Func<object, bool> _shouldSerializeMethod = (obj) => true;
        private bool _ignoreIfDefault;
        private bool _ignoreIfNull; // for backwards compatibility if a caller specifies IgnoreIfNull on a value type
        private object _defaultValue;
        private Func<object, bool> _defaultComparer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonMemberMap class.
        /// </summary>
        /// <param name="memberInfo">The member info.</param>
        /// <param name="conventions">The conventions to use with this member.</param>
        public BsonMemberMap(MemberInfo memberInfo, ConventionProfile conventions)
        {
            _memberInfo = memberInfo;
            _memberType = BsonClassMap.GetMemberInfoType(memberInfo);
            _conventions = conventions;
        }

        // public properties
        /// <summary>
        /// Gets the name of the member.
        /// </summary>
        public string MemberName
        {
            get { return _memberInfo.Name; }
        }

        /// <summary>
        /// Gets the type of the member.
        /// </summary>
        public Type MemberType
        {
            get { return _memberType; }
        }

        /// <summary>
        /// Gets the name of the element.
        /// </summary>
        public string ElementName
        {
            get
            {
                if (_elementName == null)
                {
                    _elementName = _conventions.ElementNameConvention.GetElementName(_memberInfo);
                }
                return _elementName;
            }
        }

        /// <summary>
        /// Gets the serialization order.
        /// </summary>
        public int Order
        {
            get { return _order; }
        }

        /// <summary>
        /// Gets the member info.
        /// </summary>
        public MemberInfo MemberInfo
        {
            get { return _memberInfo; }
        }

        /// <summary>
        /// Gets the getter function.
        /// </summary>
        public Func<object, object> Getter
        {
            get
            {
                if (_getter == null)
                {
                    _getter = GetGetter();
                }
                return _getter;
            }
        }

        /// <summary>
        /// Gets the serialization options.
        /// </summary>
        public IBsonSerializationOptions SerializationOptions
        {
            get { return _serializationOptions; }
        }

        /// <summary>
        /// Gets the setter function.
        /// </summary>
        public Action<object, object> Setter
        {
            get
            {
                if (_setter == null)
                {
                    if (_memberInfo.MemberType == MemberTypes.Field)
                    {
                        _setter = GetFieldSetter();
                    }
                    else
                    {
                        _setter = GetPropertySetter();
                    }
                }
                return _setter;
            }
        }

        /// <summary>
        /// Gets the Id generator.
        /// </summary>
        public IIdGenerator IdGenerator
        {
            get
            {
                if (_idGenerator == null)
                {
                    // special case IdGenerator for strings represented externally as ObjectId
                    var memberInfoType = BsonClassMap.GetMemberInfoType(_memberInfo);
                    var representationOptions = _serializationOptions as RepresentationSerializationOptions;
                    if (memberInfoType == typeof(string) && representationOptions != null && representationOptions.Representation == BsonType.ObjectId)
                    {
                        _idGenerator = StringObjectIdGenerator.Instance;
                    }
                    else
                    {
                        _idGenerator = _conventions.IdGeneratorConvention.GetIdGenerator(_memberInfo);
                    }
                }
                return _idGenerator;
            }
        }

        /// <summary>
        /// Gets whether an element is required for this member when deserialized.
        /// </summary>
        public bool IsRequired
        {
            get { return _isRequired; }
        }

        /// <summary>
        /// Gets whether this member has a default value.
        /// </summary>
        public bool HasDefaultValue
        {
            get { return _hasDefaultValue; }
        }

        /// <summary>
        /// Gets whether the default value should be serialized.
        /// </summary>
        [Obsolete("SerializeDefaultValue is obsolete and will be removed in a future release of the MongoDB CSharp Driver. Please use IgnoreIfDefault and SetIgnoreIfDefault instead.")]
        public bool SerializeDefaultValue
        {
            get { return _serializeDefaultValue; }
        }

        /// <summary>
        /// Gets the method that will be called to determine whether the member should be serialized.
        /// </summary>
        public Func<object, bool> ShouldSerializeMethod
        {
            get { return _shouldSerializeMethod; }
        }

        /// <summary>
        /// Gets whether default values should be ignored when serialized.
        /// </summary>
        public bool IgnoreIfDefault
        {
            get { return _ignoreIfDefault; }
        }

        /// <summary>
        /// Gets whether null values should be ignored when serialized.
        /// </summary>
        [Obsolete("IgnoreIfNull is obsolete and will be removed in a future release of the MongoDB CSharp Driver. Please use IgnoreIfDefault instead.")]
        public bool IgnoreIfNull
        {
            get { return _ignoreIfNull; }
        }

        /// <summary>
        /// Gets the default value.
        /// </summary>
        public object DefaultValue
        {
            get { return _defaultValue; }
        }

        // public methods
        /// <summary>
        /// Determines whether a value should be serialized
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="value">The value.</param>
        /// <returns>True if the value should be serialized.</returns>
        public bool ShouldSerialize(object obj, object value)
        {
            // _ignoreIfDefault overrides the legacy _serializeDefaultValue
            if (_ignoreIfDefault ||
                (_hasDefaultValue && !_serializeDefaultValue))
            {
                if (_defaultComparer == null)
                {
                    _defaultComparer = GetDefaultComparer();
                }

                if (_defaultComparer(value))
                {
                    // don't serialize default value
                    return false;
                }
            }
            
            if (!_shouldSerializeMethod(obj))
            {
                // the _shouldSerializeMethod determined that the member shouldn't be serialized
                return false;
            }

            return true;
        }

        /// <summary>
        /// Applies the default value to the member of an object.
        /// </summary>
        /// <param name="obj">The object.</param>
        public void ApplyDefaultValue(object obj)
        {
            if (!_hasDefaultValue)
            {
                throw new InvalidOperationException("BsonMemberMap has no default value.");
            }
            this.Setter(obj, _defaultValue);
        }

        /// <summary>
        /// Gets the serializer.
        /// </summary>
        /// <param name="actualType">The actual type of the member's value.</param>
        /// <returns>The member map.</returns>
        public IBsonSerializer GetSerializer(Type actualType)
        {
            if (_serializer != null)
            {
                return _serializer;
            }
            else
            {
                return BsonSerializer.LookupSerializer(actualType);
            }
        }

        /// <summary>
        /// Sets the default value.
        /// </summary>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The member map.</returns>
        public BsonMemberMap SetDefaultValue(object defaultValue)
        {
            _defaultValue = defaultValue;
            _hasDefaultValue = true;
            return this;
        }

        /// <summary>
        /// Sets the default value.
        /// </summary>
        /// <param name="defaultValue">The default value.</param>
        /// <param name="serializeDefaultValue">Whether the default value shoudl be serialized.</param>
        /// <returns>The member map.</returns>
        [Obsolete("SetDefaultValue(defaultValue, serializeDefaultValue) is obsolete and will be removed in a future release of the MongoDB CSharp Driver. Please use SetDefaultValue(defaultValue) and SetIgnoreIfDefault instead.")]
        public BsonMemberMap SetDefaultValue(object defaultValue, bool serializeDefaultValue)
        {
            SetDefaultValue(defaultValue);
            SetSerializeDefaultValue(serializeDefaultValue);
            return this;
        }        

        /// <summary>
        /// Sets the name of the element.
        /// </summary>
        /// <param name="elementName">The name of the element.</param>
        /// <returns>The member map.</returns>
        public BsonMemberMap SetElementName(string elementName)
        {
            _elementName = elementName;
            return this;
        }

        /// <summary>
        /// Sets the Id generator.
        /// </summary>
        /// <param name="idGenerator">The Id generator.</param>
        /// <returns>The member map.</returns>
        public BsonMemberMap SetIdGenerator(IIdGenerator idGenerator)
        {
            _idGenerator = idGenerator;
            return this;
        }

        /// <summary>
        /// Sets whether default values should be ignored when serialized.
        /// </summary>
        /// <param name="ignoreIfDefault">Whether default values should be ignored when serialized.</param>
        /// <returns>The member map.</returns>
        public BsonMemberMap SetIgnoreIfDefault(bool ignoreIfDefault)
        {
            _ignoreIfDefault = ignoreIfDefault;
            return this;
        }

        /// <summary>
        /// Sets whether null values should be ignored when serialized.
        /// </summary>
        /// <param name="ignoreIfNull">Wether null values should be ignored when serialized.</param>
        /// <returns>The member map.</returns>
        [Obsolete("SetIgnoreIfNull is obsolete and will be removed in a future release of the MongoDB CSharp Driver. Please use SetIgnoreIfDefault instead.")]
        public BsonMemberMap SetIgnoreIfNull(bool ignoreIfNull)
        {
            _ignoreIfNull = ignoreIfNull;

            if (!_memberType.IsValueType ||
                (_memberType.IsGenericType &&
                _memberType.GetGenericTypeDefinition() == typeof(Nullable<>)))
            {
                _ignoreIfDefault = ignoreIfNull;
            }

            return this;
        }

        /// <summary>
        /// Sets whether an element is required for this member when deserialized
        /// </summary>
        /// <param name="isRequired">Whether an element is required for this member when deserialized</param>
        /// <returns>The member map.</returns>
        public BsonMemberMap SetIsRequired(bool isRequired)
        {
            _isRequired = isRequired;
            return this;
        }

        /// <summary>
        /// Sets the serialization order.
        /// </summary>
        /// <param name="order">The serialization order.</param>
        /// <returns>The member map.</returns>
        public BsonMemberMap SetOrder(int order)
        {
            _order = order;
            return this;
        }

        /// <summary>
        /// Sets the external representation.
        /// </summary>
        /// <param name="representation">The external representation.</param>
        /// <returns>The member map.</returns>
        public BsonMemberMap SetRepresentation(BsonType representation)
        {
            _serializationOptions = new RepresentationSerializationOptions(representation);
            return this;
        }

        /// <summary>
        /// Sets the serialization options.
        /// </summary>
        /// <param name="serializationOptions">The serialization options.</param>
        /// <returns>The member map.</returns>
        public BsonMemberMap SetSerializationOptions(IBsonSerializationOptions serializationOptions)
        {
            _serializationOptions = serializationOptions;
            return this;
        }

        /// <summary>
        /// Sets the serializer.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <returns>The member map.</returns>
        public BsonMemberMap SetSerializer(IBsonSerializer serializer)
        {
            _serializer = serializer;
            return this;
        }

        /// <summary>
        /// Sets whether the default value should be serialized.
        /// </summary>
        /// <param name="serializeDefaultValue">Whether the default value should be serialized.</param>
        /// <returns>The member map.</returns>
        [Obsolete("SetSerializeDefaultValue is obsolete and will be removed in a future release of the MongoDB CSharp Driver. Please use SetIgnoreIfDefault and IgnoreIfDefault instead.")]
        public BsonMemberMap SetSerializeDefaultValue(bool serializeDefaultValue)
        {
            _serializeDefaultValue = serializeDefaultValue;
            return this;
        }

        /// <summary>
        /// Sets the method that will be called to determine whether the member should be serialized.
        /// </summary>
        /// <param name="shouldSerializeMethod">The method.</param>
        /// <returns>The member map.</returns>
        public BsonMemberMap SetShouldSerializeMethod(Func<object, bool> shouldSerializeMethod)
        {
            if (shouldSerializeMethod != null)
            {
                _shouldSerializeMethod = shouldSerializeMethod;
            }
            else
            {
                _shouldSerializeMethod = (obj) => true;
            }
            return this;
        }

        // private methods
        private Action<object, object> GetFieldSetter()
        {
            var fieldInfo = (FieldInfo)_memberInfo;

            if (fieldInfo.IsInitOnly || fieldInfo.IsLiteral)
            {
                var message = string.Format(
                    "The field '{0} {1}' of class '{2}' is readonly.",
                    fieldInfo.FieldType.FullName, fieldInfo.Name, fieldInfo.DeclaringType.FullName);
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

            return (Action<object, object>)method.CreateDelegate(typeof(Action<object, object>));
        }

        private Func<object, bool> GetDefaultComparer()
        {
            Type memberType;

            var propertyInfo = _memberInfo as PropertyInfo;
            if (propertyInfo != null)
            {
                var getMethodInfo = propertyInfo.GetGetMethod(true);
                if (getMethodInfo == null)
                {
                    var message = string.Format("The property '{0} {1}' of class '{2}' has no 'get' accessor.", propertyInfo.PropertyType.FullName, propertyInfo.Name, propertyInfo.DeclaringType.FullName);
                    throw new BsonSerializationException(message);
                }
                memberType = propertyInfo.PropertyType;
            }
            else
            {
                memberType = ((FieldInfo)_memberInfo).FieldType;
            }

            // lambdaExpression = (obj) => (TMember)obj == comparand
            var objParameter = Expression.Parameter(typeof(object), "obj");

            Expression comparand;
            if (_hasDefaultValue)
            {
                comparand = Expression.Constant(_defaultValue);
            }
            else
            {
                comparand = GetDefaultExpression(memberType);
            }

            var lambdaExpression = Expression.Lambda<Func<object, bool>>(
                Expression.Equal(
                    Expression.Convert(objParameter, memberType),
                    comparand
                ),
                objParameter
            );

            return lambdaExpression.Compile();
        }

        private Func<object, object> GetGetter()
        {
            if (_memberInfo is PropertyInfo)
            {
                var propertyInfo = (PropertyInfo)_memberInfo;
                var getMethodInfo = propertyInfo.GetGetMethod(true);
                if (getMethodInfo == null)
                {
                    var message = string.Format(
                        "The property '{0} {1}' of class '{2}' has no 'get' accessor.",
                        propertyInfo.PropertyType.FullName, propertyInfo.Name, propertyInfo.DeclaringType.FullName);
                    throw new BsonSerializationException(message);
                }
            }

            // lambdaExpression = (obj) => (object) ((TClass) obj).Member
            var objParameter = Expression.Parameter(typeof(object), "obj");
            var lambdaExpression = Expression.Lambda<Func<object, object>>(
                Expression.Convert(
                    Expression.MakeMemberAccess(
                        Expression.Convert(objParameter, _memberInfo.DeclaringType),
                        _memberInfo
                    ),
                    typeof(object)
                ),
                objParameter
            );

            return lambdaExpression.Compile();
        }

        private Action<object, object> GetPropertySetter()
        {
            var propertyInfo = (PropertyInfo)_memberInfo;
            var setMethodInfo = propertyInfo.GetSetMethod(true);
            if (setMethodInfo == null)
            {
                var message = string.Format(
                    "The property '{0} {1}' of class '{2}' has no 'set' accessor.",
                    propertyInfo.PropertyType.FullName, propertyInfo.Name, propertyInfo.DeclaringType.FullName);
                throw new BsonSerializationException(message);
            }

            // lambdaExpression = (obj, value) => ((TClass) obj).SetMethod((TMember) value)
            var objParameter = Expression.Parameter(typeof(object), "obj");
            var valueParameter = Expression.Parameter(typeof(object), "value");
            var lambdaExpression = Expression.Lambda<Action<object, object>>(
                Expression.Call(
                    Expression.Convert(objParameter, _memberInfo.DeclaringType),
                    setMethodInfo,
                    Expression.Convert(valueParameter, _memberType)
                ),
                objParameter,
                valueParameter
            );

            return lambdaExpression.Compile();
        }

        // TODO: replace this with System.Linq.Expressions.DefaultExpressions when moving to .Net 4.0+
        private static Expression GetDefaultExpression(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Empty:
                case TypeCode.DBNull:
                case TypeCode.String:
                    return Expression.Constant(null, type);

                case TypeCode.Object:
                    if (!type.IsValueType)
                    {
                        return Expression.Constant(null, type);
                    }
                    return Expression.New(type);

                case TypeCode.Boolean:
                    return Expression.Constant(false, type);

                case TypeCode.Char:
                    return Expression.Constant('\0', type);

                case TypeCode.SByte:
                    return Expression.Constant((sbyte)0, type);

                case TypeCode.Byte:
                    return Expression.Constant((byte)0, type);

                case TypeCode.Int16:
                    return Expression.Constant((short)0, type);

                case TypeCode.UInt16:
                    return Expression.Constant((ushort)0, type);

                case TypeCode.Int32:
                    return Expression.Constant(0, type);

                case TypeCode.UInt32:
                    return Expression.Constant(0U, type);

                case TypeCode.Int64:
                    return Expression.Constant(0L, type);

                case TypeCode.UInt64:
                    return Expression.Constant(0UL, type);

                case TypeCode.Single:
                    return Expression.Constant(0F, type);

                case TypeCode.Double:
                    return Expression.Constant(0D, type);

                case TypeCode.Decimal:
                    return Expression.Constant(0M, type);

                case TypeCode.DateTime:
                    return Expression.New(type);
            }
            throw new InvalidOperationException("Unreachable code");
        }
    }
}
