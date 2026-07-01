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

namespace MongoDB.Bson.Serialization.Conventions
{
    internal interface IConventionRegistry
    {
        /// <summary>
        /// Looks up the effective set of conventions that apply to a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The conventions for that type.</returns>
        IConventionPack Lookup(Type type);

        /// <summary>
        /// Registers the conventions.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="conventions">The conventions.</param>
        /// <param name="filter">The filter.</param>
        void Register(string name, IConventionPack conventions, Func<Type, bool> filter);

        /// <summary>
        /// Removes the conventions specified by the given name.
        /// </summary>
        /// <param name="name">The name.</param>
        void Remove(string name);
    }
}
