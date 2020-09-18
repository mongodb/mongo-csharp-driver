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
using System.Linq.Expressions;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators
{
    public class TranslationContext
    {
        // private fields
        private readonly SymbolTable _symbolTable;

        // constructors
        public TranslationContext()
            : this(new SymbolTable())
        {
        }

        public TranslationContext(SymbolTable symbolTable)
        {
            _symbolTable = Throw.IfNull(symbolTable, nameof(symbolTable));
        }

        // public properties
        public SymbolTable SymbolTable => _symbolTable;

        // public methods
        public TranslationContext WithSymbol(ParameterExpression parameter, Symbol symbol)
        {
            var newSymbolTable = _symbolTable.WithSymbol(parameter, symbol);
            var newContext = new TranslationContext(newSymbolTable);
            return newContext;
        }

        public TranslationContext WithSymbolAsCurrent(ParameterExpression parameter, Symbol symbol)
        {
            var newSymbolTable = _symbolTable.WithSymbolAsCurrent(parameter, symbol);
            var newContext = new TranslationContext(newSymbolTable);
            return newContext;
        }

        public TranslationContext WithSymbols(params ValueTuple<ParameterExpression, Symbol>[] tuples)
        {
            var newSymbolTable = _symbolTable.WithSymbols(tuples);
            var newContext = new TranslationContext(newSymbolTable);
            return newContext;
        }
    }
}
