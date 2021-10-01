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

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc
{
    internal class SymbolTable
    {
        // private fields
        private readonly IReadOnlyList<Symbol> _symbols;

        // constructors
        public SymbolTable()
        {
            _symbols = Enumerable.Empty<Symbol>().AsReadOnlyList();
        }

        public SymbolTable(Symbol symbol)
        {
            Ensure.IsNotNull(symbol, nameof(symbol));
            _symbols = new[] { symbol }.AsReadOnlyList();
        }

        public SymbolTable(IEnumerable<Symbol> symbols)
        {
            Ensure.IsNotNullAndDoesNotContainAnyNulls(symbols, nameof(symbols));
            Ensure.That(symbols.Where(s => s.IsCurrent).Count() <= 1, "Only one symbol can be the current symbol.", nameof(symbols));
            _symbols = symbols.AsReadOnlyList();
        }

        // public properties
        public IReadOnlyList<Symbol> Symbols => _symbols;

        // public methods
        public override string ToString()
        {
            return $"{{ Symbols : [{string.Join(", ", _symbols)}] }}";
        }

        public bool TryGetSymbol(ParameterExpression parameter, out Symbol symbol)
        {
            foreach (var s in _symbols)
            {
                if (s.Parameter == parameter)
                {
                    symbol = s;
                    return true;
                }
            }

            symbol = null;
            return false;
        }

        public SymbolTable WithSymbol(Symbol newSymbol)
        {
            Ensure.IsNotNull(newSymbol, nameof(newSymbol));

            var symbols = new List<Symbol>(capacity: _symbols.Count + 1);
            if (newSymbol.IsCurrent)
            {
                symbols.AddRange(_symbols.Select(s => s.AsNotCurrent()));
            }
            else
            {
                symbols.AddRange(_symbols);
            }
            symbols.Add(newSymbol);

            return new SymbolTable(symbols);
        }

        public SymbolTable WithSymbols(params Symbol[] newSymbols)
        {
            Ensure.IsNotNullAndDoesNotContainAnyNulls(newSymbols, nameof(newSymbols));
            Ensure.That(newSymbols.Where(s => s.IsCurrent).Count() <= 1, "Only one symbol can be the current symbol.", nameof(newSymbols));

            var symbols = new List<Symbol>(capacity: _symbols.Count + newSymbols.Length);
            if (newSymbols.Any(s => s.IsCurrent))
            {
                symbols.AddRange(_symbols.Select(s => s.AsNotCurrent()));
            }
            else
            {
                symbols.AddRange(_symbols);
            }
            symbols.AddRange(newSymbols);

            return new SymbolTable(symbols);
        }
    }
}
