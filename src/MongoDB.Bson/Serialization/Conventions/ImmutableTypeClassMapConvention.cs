/* Copyright 2016-present MongoDB Inc.
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
* 
*/

using System;
using System.Linq;
using System.Reflection;

namespace MongoDB.Bson.Serialization.Conventions
{
    /// <summary>
    /// Maps a fully immutable type. This will include anonymous types.
    /// </summary>
    public class ImmutableTypeClassMapConvention : ConventionBase, IClassMapConvention
    {
        /// <inheritdoc />
        public void Apply(BsonClassMap classMap)
        {
            var typeInfo = classMap.ClassType.GetTypeInfo();

            if (typeInfo.GetConstructor(Type.EmptyTypes) != null)
            {
                return;
            }

            var propertyBindingFlags = BindingFlags.Public | BindingFlags.Instance;
            var properties = typeInfo.GetProperties(propertyBindingFlags);
            if (properties.Any(CanWrite))
            {
                return; // a type that has any writable properties is not immutable
            }

            var anyConstructorsWereFound = false;
            var constructorBindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
            foreach (var ctor in typeInfo.GetConstructors(constructorBindingFlags))
            {
                if (ctor.IsPrivate)
                {
                    continue; // do not consider private constructors
                }

                var parameters = ctor.GetParameters();
                if (parameters.Length != properties.Length)
                {
                    continue; // only consider constructors that have sufficient parameters to initialize all properties
                }

                var matches = parameters
                    .GroupJoin(properties,
                        parameter => parameter.Name,
                        property => property.Name,
                        (parameter, props) => new { Parameter = parameter, Properties = props },
                        StringComparer.OrdinalIgnoreCase);

                if (matches.Any(m => m.Properties.Count() != 1))
                {
                    continue;
                }

                if (ctor.IsPublic && !typeInfo.IsAbstract)
                {
                    // we need to save constructorInfo only for public constructors in non abstract classes
                    classMap.MapConstructor(ctor);
                }

                anyConstructorsWereFound = true;
            }

            if (anyConstructorsWereFound)
            {
                // if any constructors were found by this convention
                // then map all the properties from the ClassType inheritance level also
                foreach (var property in properties)
                {
                    if (property.DeclaringType != classMap.ClassType)
                    {
                        continue;
                    }

                    var memberMap = classMap.MapMember(property);
                    if (classMap.IsAnonymous)
                    {
                        var defaultValue = memberMap.DefaultValue;
                        memberMap.SetDefaultValue(defaultValue);
                    }
                }
            }
        }

        // private methods
        private bool CanWrite(PropertyInfo propertyInfo)
        {
            // CanWrite gets true even if a property has only a private setter
            return propertyInfo.CanWrite && (propertyInfo.SetMethod?.IsPublic ?? false);
        }
    }
}
