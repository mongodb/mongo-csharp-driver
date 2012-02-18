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

using System.Reflection;
using System;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Conventions
{
    /// <summary>
    /// Represents a bson serialization options convention.
    /// </summary>
    public interface ISerializationOptionsConvention
    {
        /// <summary>
        /// Gets the bson serialization options for a member.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <returns>The bson serialization options for the member; or null to use defaults.</returns>
        IBsonSerializationOptions GetSerializationOptions(MemberInfo member);
    }

    /// <summary>
    /// Represents bson serialiation options that use default values.
    /// </summary>
    public class NullSerializationOptionsConvention : ISerializationOptionsConvention
    {
        /// <summary>
        /// Gets the bson serialization options for a member.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <returns>
        /// The bson serialization options for the member; or null to use defaults.
        /// </returns>
        public IBsonSerializationOptions GetSerializationOptions(MemberInfo member)
        {
            return null;   
        }
    }

    /// <summary>
    /// Sets serialiation options for a member of a given type.
    /// </summary>
    public class TypeRepresentationSerializationOptionsConvention : ISerializationOptionsConvention
    {
        private readonly Type memberType;
        private readonly BsonType bsonType;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeRepresentationSerializationOptionsConvention"/> class.
        /// </summary>
        /// <param name="memberType">Type of the member.</param>
        /// <param name="bsonType">Type of the bson.</param>
        public TypeRepresentationSerializationOptionsConvention(Type memberType, BsonType bsonType)
        {
            this.memberType = memberType;
            this.bsonType = bsonType;
        }

        /// <summary>
        /// Gets the bson serialization options for a member.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <returns>
        /// The bson serialization options for the member; or null to use defaults.
        /// </returns>
        public IBsonSerializationOptions GetSerializationOptions(MemberInfo member)
        {
            var propertyInfo = member as PropertyInfo;
            if (propertyInfo != null && propertyInfo.PropertyType == this.memberType)
            {
                return new RepresentationSerializationOptions(this.bsonType);
            }

            var fieldInfo = member as FieldInfo;
            if (fieldInfo != null && fieldInfo.FieldType == this.memberType)
            {
                return new RepresentationSerializationOptions(this.bsonType);
            }

            return null;
        }
    }

}