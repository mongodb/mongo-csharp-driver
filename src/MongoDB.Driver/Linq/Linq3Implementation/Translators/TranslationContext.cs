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
using System.CodeDom;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.KnownSerializerFinders;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators
{
    internal class TranslationContext
    {
        #region static
        public static TranslationContext Create(
            Expression expression,
            ExpressionTranslationOptions translationOptions,
            TranslationContextData data = null)
        {
            var knownSerializers = KnownSerializerFinder.FindKnownSerializers(expression, translationOptions);
            return Create(translationOptions, knownSerializers, data);
        }

        public static TranslationContext Create(
            Expression expression,
            IMongoQueryProviderInternal provider,
            ExpressionTranslationOptions translationOptions,
            TranslationContextData data = null)
        {
            var ultimateSourceExpression = GetUltimateSource(expression);
            var ultimateSourceSerializer = IQueryableSerializer.Create(provider.PipelineInputSerializer);
            return Create(expression, ultimateSourceExpression, ultimateSourceSerializer, translationOptions, data);
        }

        public static TranslationContext Create(
            Expression expression,
            Expression initialNode,
            IBsonSerializer initialSerializer,
            ExpressionTranslationOptions translationOptions,
            TranslationContextData data = null)
        {
            var knownSerializers = KnownSerializerFinder.FindKnownSerializers(expression, translationOptions, initialNode, initialSerializer);
            return Create(translationOptions, knownSerializers, data);
        }

        public static TranslationContext Create(
            Expression expression,
            (Expression Node, IBsonSerializer Serializer)[] initialKnownSerializers,
            ExpressionTranslationOptions translationOptions,
            TranslationContextData data = null)
        {
            var knownSerializers = KnownSerializerFinder.FindKnownSerializers(expression, translationOptions, initialKnownSerializers);
            return Create(translationOptions, knownSerializers, data);
        }

        public static TranslationContext Create(
            ExpressionTranslationOptions translationOptions,
            KnownSerializerMap knownSerializers,
            TranslationContextData data = null)
        {
            var symbolTable = new SymbolTable();
            var nameGenerator = new NameGenerator();
            return new TranslationContext(translationOptions, knownSerializers, data, symbolTable, nameGenerator);
        }

        private static Expression GetUltimateSource(Expression expression)
        {
            if (expression is ConstantExpression constantExpression &&
                constantExpression.Value is IQueryable queryable &&
                queryable.Provider is IMongoQueryProvider provider &&
                queryable.Expression is ConstantExpression queryableConstantExpression &&
                queryableConstantExpression.Value == constantExpression.Value)
            {
                return expression;
            }

            if (expression is MethodCallExpression methodCallExpression &&
                methodCallExpression.Method is var method &&
                method.GetParameters() is var parameters &&
                parameters.Length >= 1)
            {
                var sourceParameter = parameters[0];
                var sourceParameterType = sourceParameter.ParameterType;
                if (sourceParameterType.IsConstructedGenericType)
                {
                    sourceParameterType = sourceParameterType.GetGenericTypeDefinition();
                }

                if (sourceParameterType == typeof(IQueryable) ||
                    sourceParameterType == typeof(IQueryable<>) ||
                    sourceParameterType == typeof(IOrderedQueryable) ||
                    sourceParameterType == typeof(IOrderedQueryable<>))
                {
                    return GetUltimateSource(methodCallExpression.Arguments[0]);
                }
            }

            throw new ArgumentException($"No ultimate source found: {expression}.");
        }
        #endregion

        // private fields
        private readonly TranslationContextData _data;
        private readonly KnownSerializerMap _knownSerializers;
        private readonly NameGenerator _nameGenerator;
        private readonly SymbolTable _symbolTable;
        private readonly ExpressionTranslationOptions _translationOptions;

        private TranslationContext(
            ExpressionTranslationOptions translationOptions,
            KnownSerializerMap knownSerializers,
            TranslationContextData data,
            SymbolTable symbolTable,
            NameGenerator nameGenerator)
        {
            _translationOptions = translationOptions ?? new ExpressionTranslationOptions();
            _knownSerializers = Ensure.IsNotNull(knownSerializers, nameof(knownSerializers));
            _data = data; // can be null
            _symbolTable = Ensure.IsNotNull(symbolTable, nameof(symbolTable));
            _nameGenerator = Ensure.IsNotNull(nameGenerator, nameof(nameGenerator));
        }

        // public properties
        public TranslationContextData Data => _data;
        public KnownSerializerMap KnownSerializers => _knownSerializers;
        public NameGenerator NameGenerator => _nameGenerator;
        public SymbolTable SymbolTable => _symbolTable;
        public ExpressionTranslationOptions TranslationOptions => _translationOptions;

        // public methods
        public Symbol CreateRootSymbol(ParameterExpression parameter, IBsonSerializer serializer)
        {
            return CreateSymbolWithVarName(parameter, varName: "ROOT", serializer, isCurrent: true);
        }

        public Symbol CreateSymbol(ParameterExpression parameter, IBsonSerializer serializer, bool isCurrent = false)
        {
            var parameterName = _nameGenerator.GetParameterName(parameter);
            return CreateSymbol(parameter, name: parameterName, serializer, isCurrent);
        }

        public Symbol CreateSymbol(ParameterExpression parameter, string name, IBsonSerializer serializer, bool isCurrent = false)
        {
            var varName = _nameGenerator.GetVarName(name);
            return CreateSymbol(parameter, name, varName, serializer, isCurrent);
        }

        public Symbol CreateSymbol(ParameterExpression parameter, string name, string varName, IBsonSerializer serializer, bool isCurrent = false)
        {
            var varAst = AstExpression.Var(varName, isCurrent);
            return CreateSymbol(parameter, name, varAst, serializer, isCurrent);
        }

        public Symbol CreateSymbol(ParameterExpression parameter, AstExpression ast, IBsonSerializer serializer, bool isCurrent = false)
        {
            var parameterName = _nameGenerator.GetParameterName(parameter);
            return CreateSymbol(parameter, name: parameterName, ast, serializer, isCurrent);
        }

        public Symbol CreateSymbol(ParameterExpression parameter, string name, AstExpression ast, IBsonSerializer serializer, bool isCurrent = false)
        {
            return new Symbol(parameter, name, ast, serializer, isCurrent);
        }

        public Symbol CreateSymbolWithVarName(ParameterExpression parameter, string varName, IBsonSerializer serializer, bool isCurrent = false)
        {
            var parameterName = _nameGenerator.GetParameterName(parameter);
            return CreateSymbol(parameter, name: parameterName, varName, serializer, isCurrent);
        }

        public IBsonSerializer GetKnownSerializer(Expression parameter)
        {
            return _knownSerializers.GetSerializer(parameter);
        }

        public override string ToString()
        {
            return $"{{ SymbolTable : {_symbolTable} }}";
        }

        public TranslationContext WithSingleSymbol(Symbol newSymbol)
        {
            var newSymbolTable = new SymbolTable(newSymbol);
            return WithSymbolTable(newSymbolTable);
        }

        public TranslationContext WithSymbol(Symbol newSymbol)
        {
            var newSymbolTable = _symbolTable.WithSymbol(newSymbol);
            return WithSymbolTable(newSymbolTable);
        }

        public TranslationContext WithSymbols(params Symbol[] newSymbols)
        {
            var newSymbolTable = _symbolTable.WithSymbols(newSymbols);
            return WithSymbolTable(newSymbolTable);
        }

        public TranslationContext WithSymbolTable(SymbolTable symbolTable)
        {
            return new TranslationContext(_translationOptions, _knownSerializers, _data, symbolTable, _nameGenerator);
        }
    }
}
