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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// A builder for a score modifier.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class SearchScoreDefinitionBuilder<TDocument>
    {
        /// <summary>
        /// Creates a score modifier that multiplies a result's base score by a given number.
        /// </summary>
        /// <param name="value">The number to multiply the default base score by.</param>
        /// <returns>
        /// A boost score modifier.
        /// </returns>
        public SearchScoreDefinition<TDocument> Boost(double value) =>
            new BoostValueSearchScoreDefinition<TDocument>(value);

        /// <summary>
        /// Creates a score modifier that multiples a result's base score by the value of a numeric
        /// field in the documents.
        /// </summary>
        /// <param name="path">
        /// The path to the numeric field whose value to multiply the default base score by.
        /// </param>
        /// <param name="undefined">
        /// The numeric value to substitute if the numeric field is not found in the documents.
        /// </param>
        /// <returns>
        /// A boost score modifier.
        /// </returns>
        public SearchScoreDefinition<TDocument> Boost(SearchPathDefinition<TDocument> path, double undefined = 0) =>
            new BoostPathSearchScoreDefinition<TDocument>(path, undefined);

        /// <summary>
        /// Creates a score modifier that multiplies a result's base score by the value of a numeric
        /// field in the documents.
        /// </summary>
        /// <param name="path">
        /// The path to the numeric field whose value to multiply the default base score by.
        /// </param>
        /// <param name="undefined">
        /// The numeric value to substitute if the numeric field is not found in the documents.
        /// </param>
        /// <returns>
        /// A boost score modifier.
        /// </returns>
        public SearchScoreDefinition<TDocument> Boost(Expression<Func<TDocument, double>> path, double undefined = 0) =>
            Boost(new ExpressionFieldDefinition<TDocument>(path), undefined);

        /// <summary>
        /// Creates a score modifier that replaces the base score with a given number.
        /// </summary>
        /// <param name="value">The number to replace the base score with.</param>
        /// <returns>
        /// A constant score modifier.
        /// </returns>
        public SearchScoreDefinition<TDocument> Constant(double value) =>
            new ConstantSearchScoreDefinition<TDocument>(value);

        /// <summary>
        /// Creates a score modifier that computes the final score through an expression.
        /// </summary>
        /// <param name="function">The expression used to compute the score.</param>
        /// <returns>
        /// A function score modifier.
        /// </returns>
        public SearchScoreDefinition<TDocument> Function(SearchScoreFunction<TDocument> function) =>
            new FunctionSearchScoreDefinition<TDocument>(function);
    }

    internal sealed class BoostPathSearchScoreDefinition<TDocument> : SearchScoreDefinition<TDocument>
    {
        private readonly SearchPathDefinition<TDocument> _path;
        private readonly double _undefined;

        public BoostPathSearchScoreDefinition(SearchPathDefinition<TDocument> path, double undefined)
        {
            _path = Ensure.IsNotNull(path, nameof(path));
            _undefined = undefined;
        }

        public override BsonDocument Render(RenderArgs<TDocument> args) =>
            new("boost", new BsonDocument
            {
                { "path", _path.Render(args) },
                { "undefined", _undefined, _undefined != 0 }
            });
    }

    internal sealed class BoostValueSearchScoreDefinition<TDocument> : SearchScoreDefinition<TDocument>
    {
        private readonly double _value;

        public BoostValueSearchScoreDefinition(double value)
        {
            _value = Ensure.IsGreaterThanZero(value, nameof(value));
        }

        public override BsonDocument Render(RenderArgs<TDocument> args) =>
            new("boost",  new BsonDocument("value", _value));
    }

    internal sealed class ConstantSearchScoreDefinition<TDocument> : SearchScoreDefinition<TDocument>
    {
        private readonly double _value;

        public ConstantSearchScoreDefinition(double value)
        {
            _value = Ensure.IsGreaterThanZero(value, nameof(value));
        }

        public override BsonDocument Render(RenderArgs<TDocument> args) =>
            new("constant", new BsonDocument("value", _value));
    }

    internal sealed class FunctionSearchScoreDefinition<TDocument> : SearchScoreDefinition<TDocument>
    {
        private readonly SearchScoreFunction<TDocument> _function;

        public FunctionSearchScoreDefinition(SearchScoreFunction<TDocument> function)
        {
            _function = Ensure.IsNotNull(function, nameof(function));
        }

        public override BsonDocument Render(RenderArgs<TDocument> args) =>
            new("function", _function.Render(args));
    }
}
