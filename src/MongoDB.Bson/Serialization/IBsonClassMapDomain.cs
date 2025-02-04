/* Copyright 2010-present MongoDB Inc.
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
using System.Reflection;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// //TODO
    /// </summary>
    public interface IBsonClassMapDomain
    {
        /// <summary>
        /// Gets all registered class maps.
        /// </summary>
        /// <returns>All registered class maps.</returns>
        IEnumerable<BsonClassMap> GetRegisteredClassMaps();

        /// <summary>
        /// Checks whether a class map is registered for a type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if there is a class map registered for the type.</returns>
        bool IsClassMapRegistered(Type type);

        /// <summary>
        /// Looks up a class map (will AutoMap the class if no class map is registered).
        /// </summary>
        /// <param name="classType">The class type.</param>
        /// <returns>The class map.</returns>
        BsonClassMap LookupClassMap(Type classType);

        /// <summary>
        /// Creates and registers a class map.
        /// </summary>
        /// <typeparam name="TClass">The class.</typeparam>
        /// <returns>The class map.</returns>
        BsonClassMap<TClass> RegisterClassMap<TClass>();

        /// <summary>
        /// Creates and registers a class map.
        /// </summary>
        /// <typeparam name="TClass">The class.</typeparam>
        /// <param name="classMapInitializer">The class map initializer.</param>
        /// <returns>The class map.</returns>
        BsonClassMap<TClass> RegisterClassMap<TClass>(Action<BsonClassMap<TClass>> classMapInitializer);

        /// <summary>
        /// Registers a class map.
        /// </summary>
        /// <param name="classMap">The class map.</param>
        void RegisterClassMap(BsonClassMap classMap);

        /// <summary>
        /// Registers a class map if it is not already registered.
        /// </summary>
        /// <typeparam name="TClass">The class.</typeparam>
        /// <returns>True if this call registered the class map, false if the class map was already registered.</returns>
        bool TryRegisterClassMap<TClass>();

        /// <summary>
        /// Registers a class map if it is not already registered.
        /// </summary>
        /// <typeparam name="TClass">The class.</typeparam>
        /// <param name="classMap">The class map.</param>
        /// <returns>True if this call registered the class map, false if the class map was already registered.</returns>
        bool TryRegisterClassMap<TClass>(BsonClassMap<TClass> classMap);

        /// <summary>
        /// Registers a class map if it is not already registered.
        /// </summary>
        /// <typeparam name="TClass">The class.</typeparam>
        /// <param name="classMapInitializer">The class map initializer (only called if the class map is not already registered).</param>
        /// <returns>True if this call registered the class map, false if the class map was already registered.</returns>
        bool TryRegisterClassMap<TClass>(Action<BsonClassMap<TClass>> classMapInitializer);

        /// <summary>
        /// Registers a class map if it is not already registered.
        /// </summary>
        /// <typeparam name="TClass">The class.</typeparam>
        /// <param name="classMapFactory">The class map factory (only called if the class map is not already registered).</param>
        /// <returns>True if this call registered the class map, false if the class map was already registered.</returns>
        bool TryRegisterClassMap<TClass>(Func<BsonClassMap<TClass>> classMapFactory);
    }
}