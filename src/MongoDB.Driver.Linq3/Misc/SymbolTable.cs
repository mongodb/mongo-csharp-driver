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
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace MongoDB.Driver.Linq3.Misc
{
    public class SymbolTable : IEnumerable<KeyValuePair<ParameterExpression, Symbol>>
    {
        // private fields
        private Symbol _current;
        private readonly Dictionary<ParameterExpression, Symbol> _symbols;

        // constructors
        public SymbolTable()
        {
            _symbols = new Dictionary<ParameterExpression, Symbol>();
        }

        public SymbolTable(ParameterExpression parameter, Symbol symbol)
        {
            Throw.IfNull(parameter, nameof(parameter));
            Throw.IfNull(symbol, nameof(symbol));
            _symbols = new Dictionary<ParameterExpression, Symbol>() { { parameter, symbol } };
        }

        public SymbolTable(SymbolTable other)
            : this()
        {
            Throw.IfNull(other, nameof(other));
            _current = other.Current;
            _symbols = new Dictionary<ParameterExpression, Symbol>(other._symbols);
        }

        // public properties
        public Symbol Current => _current;

        // public methods
        public IEnumerator<KeyValuePair<ParameterExpression, Symbol>> GetEnumerator()
        {
            return _symbols.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool TryGetSymbol(ParameterExpression parameter, out Symbol symbol)
        {
            return _symbols.TryGetValue(parameter, out symbol);
        }

        public SymbolTable WithSymbol(ParameterExpression parameter, Symbol symbol)
        {
            var newSymbolTable = new SymbolTable(this);
            newSymbolTable._symbols.Add(parameter, symbol);
            return newSymbolTable;
        }

        public SymbolTable WithSymbolAsCurrent(ParameterExpression parameter, Symbol symbol)
        {
            var newSymbolTable = new SymbolTable(this);
            newSymbolTable._symbols.Add(parameter, symbol);
            newSymbolTable._current = symbol;
            return newSymbolTable;
        }

        public SymbolTable WithSymbols(params ValueTuple<ParameterExpression, Symbol>[] symbols)
        {
            var newSymbolTable = new SymbolTable(this);
            foreach (var tuple in symbols)
            {
                newSymbolTable._symbols.Add(tuple.Item1, tuple.Item2);
            }
            return newSymbolTable;
        }
    }
}
