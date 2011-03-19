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
using System.Text;

namespace MongoDB.Bson {
    /// <summary>
    /// Represents the symbol table of BsonSymbols.
    /// </summary>
    public static class BsonSymbolTable {
        #region private static fields
        private static object staticLock = new object();
        private static Dictionary<string, BsonSymbol> symbolTable = new Dictionary<string, BsonSymbol>();
        #endregion

        #region public static methods
        /// <summary>
        /// Looks up a symbol (and creates a new one if necessary).
        /// </summary>
        /// <param name="name">The name of the symbol.</param>
        /// <returns>The symbol.</returns>
        public static BsonSymbol Lookup(
            string name
        ) {
            lock (staticLock) {
                BsonSymbol symbol;
                if (!symbolTable.TryGetValue(name, out symbol)) {
                    symbol = new BsonSymbol(name);
                    symbolTable[name] = symbol;
                }
                return symbol;
            }
        }
        #endregion
    }
}
