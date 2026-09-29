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

namespace MongoDB.Bson
{
    /// <summary>
    /// Represents a factory for BsonSymbols.
    /// </summary>
    [Obsolete("BsonSymbolTable no longer interns symbols. Use (BsonSymbol)name or BsonSymbol.Create(value) instead.")]
    public static class BsonSymbolTable
    {
        // public static methods
        /// <summary>
        /// Creates a new symbol with the specified name.
        /// </summary>
        /// <param name="name">The name of the symbol.</param>
        /// <returns>The symbol.</returns>
        public static BsonSymbol Lookup(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            return new BsonSymbol(name);
        }
    }
}
