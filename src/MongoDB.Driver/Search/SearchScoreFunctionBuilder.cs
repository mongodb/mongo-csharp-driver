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
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// A builder for a score function.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public sealed class SearchScoreFunctionBuilder<TDocument>
    {
        /// <summary>
        /// Creates a function that adds a series of numbers.
        /// </summary>
        /// <param name="operands">An array of expressions, which can have negative values.</param>
        /// <returns>An addition score function.</returns>
        public SearchScoreFunction<TDocument> Add(IEnumerable<SearchScoreFunction<TDocument>> operands) =>
            new ArithmeticSearchScoreFunction<TDocument>("add", operands);

        /// <summary>
        /// Creates a function that adds a series of numbers.
        /// </summary>
        /// <param name="operands">An array of expressions, which can have negative values.</param>
        /// <returns>An addition score function.</returns>
        public SearchScoreFunction<TDocument> Add(params SearchScoreFunction<TDocument>[] operands) =>
            Add((IEnumerable<SearchScoreFunction<TDocument>>)operands);

        /// <summary>
        /// Creates a function that represents a constant number.
        /// </summary>
        /// <param name="value">Number that indicates a fixed value.</param>
        /// <returns>A constant score function.</returns>
        public SearchScoreFunction<TDocument> Constant(double value) =>
            new ConstantSearchScoreFunction<TDocument>(value);

        /// <summary>
        /// Creates a function that decays, or reduces by multiplying, the final scores of the
        /// documents based on the distance of a numeric field from a specified origin point.
        /// </summary>
        /// <param name="path">The path to the numeric field.</param>
        /// <param name="origin">The point of origin from which to calculate the distance.</param>
        /// <param name="scale">
        /// The distance from <paramref name="origin"/> plus or minus <paramref name="offset"/> at
        /// which scores must be multiplied.
        /// </param>
        /// <param name="decay">
        /// The rate at which to multiply score values, which must be a positive number between
        /// 0 and 1 exclusive.
        /// </param>
        /// <param name="offset">
        /// The number of use to determine the distance from <paramref name="origin"/>.
        /// </param>
        /// <returns>A Guassian score function.</returns>
        public SearchScoreFunction<TDocument> Gauss(
            SearchPathDefinition<TDocument> path,
            double origin,
            double scale,
            double decay = 0.5,
            double offset = 0)
                => new GaussSearchScoreFunction<TDocument>(path, origin, scale, decay, offset);

        /// <summary>
        /// Creates a function that decays, or reduces by multiplying, the final scores of the
        /// documents based on the distance of a numeric field from a specified origin point.
        /// </summary>
        /// <param name="path">The path to the numeric field.</param>
        /// <param name="origin">The point of origin from which to calculate the distance.</param>
        /// <param name="scale">
        /// The distance from <paramref name="origin"/> plus or minus <paramref name="offset"/> at
        /// which scores must be multiplied.
        /// </param>
        /// <param name="decay">
        /// The rate at which to multiply score values, which must be a positive number between
        /// 0 and 1 exclusive.
        /// </param>
        /// <param name="offset">
        /// The number of use to determine the distance from <paramref name="origin"/>.
        /// </param>
        /// <returns>A Guassian score function.</returns>
        public SearchScoreFunction<TDocument> Gauss(
            Expression<Func<TDocument, double>> path,
            double origin,
            double scale,
            double decay = 0.5,
            double offset = 0)
                => Gauss(new ExpressionFieldDefinition<TDocument>(path), origin, scale, decay, offset);

        /// <summary>
        /// Creates a function that calculates the base-10 logarithm of a number.
        /// </summary>
        /// <param name="operand">The number.</param>
        /// <returns>A logarithmic score function.</returns>
        public SearchScoreFunction<TDocument> Log(SearchScoreFunction<TDocument> operand) =>
            new UnarySearchScoreFunction<TDocument>("log", operand);

        /// <summary>
        /// Creates a function that adds 1 to a number and then calculates its base-10 logarithm.
        /// </summary>
        /// <param name="operand">The number.</param>
        /// <returns>A logarithmic score function.</returns>
        public SearchScoreFunction<TDocument> Log1p(SearchScoreFunction<TDocument> operand) =>
            new UnarySearchScoreFunction<TDocument>("log1p", operand);

        /// <summary>
        /// Creates a function that multiplies a series of numbers.
        /// </summary>
        /// <param name="operands">An array of expressions, which can have negative values.</param>
        /// <returns>A multiplication score function.</returns>
        public SearchScoreFunction<TDocument> Multiply(IEnumerable<SearchScoreFunction<TDocument>> operands) =>
            new ArithmeticSearchScoreFunction<TDocument>("multiply", operands);

        /// <summary>
        /// Creates a function that multiplies a series of numbers.
        /// </summary>
        /// <param name="operands">An array of expressions, which can have negative values.</param>
        /// <returns>A mulitplication score function.</returns>
        public SearchScoreFunction<TDocument> Multiply(params SearchScoreFunction<TDocument>[] operands) =>
            Multiply((IEnumerable<SearchScoreFunction<TDocument>>)operands);

        /// <summary>
        /// Creates a function that incorporates an indexed numeric field value into the score.
        /// </summary>
        /// <param name="path">The path to the numeric field.</param>
        /// <param name="undefined">
        /// The value to use if the numeric field specified using <paramref name="path"/> is
        /// missing in the document.
        /// </param>
        /// <returns>A path score function.</returns>
        public SearchScoreFunction<TDocument> Path(SearchPathDefinition<TDocument> path, double undefined = 0) =>
            new PathSearchScoreFunction<TDocument>(path, undefined);

        /// <summary>
        /// Creates a function that incorporates an indexed numeric field value into the score.
        /// </summary>
        /// <param name="path">The path to the numeric field.</param>
        /// <param name="undefined">
        /// The value to use if the numeric field specified using <paramref name="path"/> is
        /// missing in the document.
        /// </param>
        /// <returns>A path score function.</returns>
        public SearchScoreFunction<TDocument> Path(Expression<Func<TDocument, double>> path, double undefined = 0) =>
            Path(new ExpressionFieldDefinition<TDocument>(path), undefined);

        /// <summary>
        /// Creates a function that represents the relevance score, which is the score Atlas Search
        /// assigns documents based on relevance.
        /// </summary>
        /// <returns>A relevance score function.</returns>
        public SearchScoreFunction<TDocument> Relevance() => new RelevanceSearchScoreFunction<TDocument>();
    }

    internal sealed class ArithmeticSearchScoreFunction<TDocument> : SearchScoreFunction<TDocument>
    {
        private readonly SearchScoreFunction<TDocument>[] _operands;
        private readonly string _operatorName;

        public ArithmeticSearchScoreFunction(string operatorName, IEnumerable<SearchScoreFunction<TDocument>> operands)
        {
            _operatorName = operatorName;
            _operands = Ensure.IsNotNull(operands, nameof(operands)).ToArray();
        }

        public override BsonDocument Render(RenderArgs<TDocument> args) =>
            new(_operatorName, new BsonArray(_operands.Select(o => o.Render(args))));
    }

    internal sealed class ConstantSearchScoreFunction<TDocument> : SearchScoreFunction<TDocument>
    {
        private readonly double _value;

        public ConstantSearchScoreFunction(double value)
        {
            _value = value;
        }

        public override BsonDocument Render(RenderArgs<TDocument> args) =>
            new("constant", _value);
    }

    internal sealed class GaussSearchScoreFunction<TDocument> : SearchScoreFunction<TDocument>
    {
        private readonly double _decay;
        private readonly double _offset;
        private readonly double _origin;
        private readonly SearchPathDefinition<TDocument> _path;
        private readonly double _scale;

        public GaussSearchScoreFunction(
            SearchPathDefinition<TDocument> path,
            double origin,
            double scale,
            double decay,
            double offset)
        {
            _path = Ensure.IsNotNull(path, nameof(path));
            _origin = origin;
            _scale = scale;
            _decay = Ensure.IsBetween(decay, 0, 1, nameof(decay));
            _offset = offset;
        }

        public override BsonDocument Render(RenderArgs<TDocument> args) =>
            new("gauss", new BsonDocument()
            {
                { "path", _path.Render(args) },
                { "origin", _origin },
                { "scale", _scale },
                { "decay", _decay, _decay != 0.5 },
                { "offset", _offset, _offset != 0 },
            });
    }

    internal sealed class PathSearchScoreFunction<TDocument> : SearchScoreFunction<TDocument>
    {
        private readonly SearchPathDefinition<TDocument> _path;
        private readonly double _undefined;

        public PathSearchScoreFunction(SearchPathDefinition<TDocument> path, double undefined)
        {
            _path = Ensure.IsNotNull(path, nameof(path));
            _undefined = undefined;
        }

        public override BsonDocument Render(RenderArgs<TDocument> args)
        {
            var renderedPath = _path.Render(args);
            var pathDocument = _undefined == 0 ? renderedPath : new BsonDocument()
                {
                    { "value", renderedPath },
                    { "undefined", _undefined }
                };

            return new("path", pathDocument);
        }
    }

    internal sealed class RelevanceSearchScoreFunction<TDocument> : SearchScoreFunction<TDocument>
    {
        public override BsonDocument Render(RenderArgs<TDocument> args) =>
            new("score", "relevance");
    }

    internal sealed class UnarySearchScoreFunction<TDocument> : SearchScoreFunction<TDocument>
    {
        private readonly SearchScoreFunction<TDocument> _operand;
        private readonly string _operatorName;
        public UnarySearchScoreFunction(string operatorName, SearchScoreFunction<TDocument> operand)
        {
            _operatorName = operatorName;
            _operand = Ensure.IsNotNull(operand, nameof(operand));
        }

        public override BsonDocument Render(RenderArgs<TDocument> args) =>
            new(_operatorName, _operand.Render(args));
    }
}
