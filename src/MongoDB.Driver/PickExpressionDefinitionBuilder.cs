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
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// A builder for pick aggregation expression definitions ($top, $bottom, $topN, $bottomN).
    /// These operators sort an array and return the top or bottom element(s). Requires MongoDB 8.3+.
    /// </summary>
    public static class PickExpressionDefinitionBuilder
    {
        /// <summary>
        /// Creates a $top expression that returns the first element of the input array after applying the sort.
        /// </summary>
        /// <typeparam name="TDocument">The type of the source document.</typeparam>
        /// <typeparam name="TElement">The type of the array elements.</typeparam>
        /// <param name="input">The array field.</param>
        /// <param name="sortBy">The sort order.</param>
        /// <returns>An expression definition that renders as a $top expression.</returns>
        public static AggregateExpressionDefinition<TDocument, TElement> Top<TDocument, TElement>(
            Expression<Func<TDocument, IEnumerable<TElement>>> input,
            SortDefinition<TElement> sortBy)
        {
            Ensure.IsNotNull(input, nameof(input));
            Ensure.IsNotNull(sortBy, nameof(sortBy));
            return new SinglePickExpressionDefinition<TDocument, TElement>(
                "$top",
                new ExpressionFieldDefinition<TDocument, IEnumerable<TElement>>(input),
                sortBy);
        }

        /// <summary>
        /// Creates a $top expression that returns the first element of the input array after applying the sort.
        /// </summary>
        /// <typeparam name="TDocument">The type of the source document.</typeparam>
        /// <typeparam name="TElement">The type of the array elements.</typeparam>
        /// <param name="input">The array field definition.</param>
        /// <param name="sortBy">The sort order.</param>
        /// <returns>An expression definition that renders as a $top expression.</returns>
        public static AggregateExpressionDefinition<TDocument, TElement> Top<TDocument, TElement>(
            FieldDefinition<TDocument, IEnumerable<TElement>> input,
            SortDefinition<TElement> sortBy)
        {
            Ensure.IsNotNull(input, nameof(input));
            Ensure.IsNotNull(sortBy, nameof(sortBy));
            return new SinglePickExpressionDefinition<TDocument, TElement>("$top", input, sortBy);
        }

        /// <summary>
        /// Creates a $bottom expression that returns the last element of the input array after applying the sort.
        /// </summary>
        /// <typeparam name="TDocument">The type of the source document.</typeparam>
        /// <typeparam name="TElement">The type of the array elements.</typeparam>
        /// <param name="input">The array field.</param>
        /// <param name="sortBy">The sort order.</param>
        /// <returns>An expression definition that renders as a $bottom expression.</returns>
        public static AggregateExpressionDefinition<TDocument, TElement> Bottom<TDocument, TElement>(
            Expression<Func<TDocument, IEnumerable<TElement>>> input,
            SortDefinition<TElement> sortBy)
        {
            Ensure.IsNotNull(input, nameof(input));
            Ensure.IsNotNull(sortBy, nameof(sortBy));
            return new SinglePickExpressionDefinition<TDocument, TElement>(
                "$bottom",
                new ExpressionFieldDefinition<TDocument, IEnumerable<TElement>>(input),
                sortBy);
        }

        /// <summary>
        /// Creates a $bottom expression that returns the last element of the input array after applying the sort.
        /// </summary>
        /// <typeparam name="TDocument">The type of the source document.</typeparam>
        /// <typeparam name="TElement">The type of the array elements.</typeparam>
        /// <param name="input">The array field definition.</param>
        /// <param name="sortBy">The sort order.</param>
        /// <returns>An expression definition that renders as a $bottom expression.</returns>
        public static AggregateExpressionDefinition<TDocument, TElement> Bottom<TDocument, TElement>(
            FieldDefinition<TDocument, IEnumerable<TElement>> input,
            SortDefinition<TElement> sortBy)
        {
            Ensure.IsNotNull(input, nameof(input));
            Ensure.IsNotNull(sortBy, nameof(sortBy));
            return new SinglePickExpressionDefinition<TDocument, TElement>("$bottom", input, sortBy);
        }

        /// <summary>
        /// Creates a $topN expression that returns the first n elements of the input array after applying the sort.
        /// </summary>
        /// <typeparam name="TDocument">The type of the source document.</typeparam>
        /// <typeparam name="TElement">The type of the array elements.</typeparam>
        /// <param name="input">The array field.</param>
        /// <param name="sortBy">The sort order.</param>
        /// <param name="n">The number of elements to return.</param>
        /// <returns>An expression definition that renders as a $topN expression.</returns>
        public static AggregateExpressionDefinition<TDocument, IEnumerable<TElement>> TopN<TDocument, TElement>(
            Expression<Func<TDocument, IEnumerable<TElement>>> input,
            SortDefinition<TElement> sortBy,
            int n)
        {
            Ensure.IsNotNull(input, nameof(input));
            Ensure.IsNotNull(sortBy, nameof(sortBy));
            Ensure.IsGreaterThanZero(n, nameof(n));
            return new MultiplePickExpressionDefinition<TDocument, TElement>(
                "$topN",
                new ExpressionFieldDefinition<TDocument, IEnumerable<TElement>>(input),
                sortBy,
                n);
        }

        /// <summary>
        /// Creates a $topN expression that returns the first n elements of the input array after applying the sort.
        /// </summary>
        /// <typeparam name="TDocument">The type of the source document.</typeparam>
        /// <typeparam name="TElement">The type of the array elements.</typeparam>
        /// <param name="input">The array field definition.</param>
        /// <param name="sortBy">The sort order.</param>
        /// <param name="n">The number of elements to return.</param>
        /// <returns>An expression definition that renders as a $topN expression.</returns>
        public static AggregateExpressionDefinition<TDocument, IEnumerable<TElement>> TopN<TDocument, TElement>(
            FieldDefinition<TDocument, IEnumerable<TElement>> input,
            SortDefinition<TElement> sortBy,
            int n)
        {
            Ensure.IsNotNull(input, nameof(input));
            Ensure.IsNotNull(sortBy, nameof(sortBy));
            Ensure.IsGreaterThanZero(n, nameof(n));
            return new MultiplePickExpressionDefinition<TDocument, TElement>("$topN", input, sortBy, n);
        }

        /// <summary>
        /// Creates a $bottomN expression that returns the last n elements of the input array after applying the sort.
        /// </summary>
        /// <typeparam name="TDocument">The type of the source document.</typeparam>
        /// <typeparam name="TElement">The type of the array elements.</typeparam>
        /// <param name="input">The array field.</param>
        /// <param name="sortBy">The sort order.</param>
        /// <param name="n">The number of elements to return.</param>
        /// <returns>An expression definition that renders as a $bottomN expression.</returns>
        public static AggregateExpressionDefinition<TDocument, IEnumerable<TElement>> BottomN<TDocument, TElement>(
            Expression<Func<TDocument, IEnumerable<TElement>>> input,
            SortDefinition<TElement> sortBy,
            int n)
        {
            Ensure.IsNotNull(input, nameof(input));
            Ensure.IsNotNull(sortBy, nameof(sortBy));
            Ensure.IsGreaterThanZero(n, nameof(n));
            return new MultiplePickExpressionDefinition<TDocument, TElement>(
                "$bottomN",
                new ExpressionFieldDefinition<TDocument, IEnumerable<TElement>>(input),
                sortBy,
                n);
        }

        /// <summary>
        /// Creates a $bottomN expression that returns the last n elements of the input array after applying the sort.
        /// </summary>
        /// <typeparam name="TDocument">The type of the source document.</typeparam>
        /// <typeparam name="TElement">The type of the array elements.</typeparam>
        /// <param name="input">The array field definition.</param>
        /// <param name="sortBy">The sort order.</param>
        /// <param name="n">The number of elements to return.</param>
        /// <returns>An expression definition that renders as a $bottomN expression.</returns>
        public static AggregateExpressionDefinition<TDocument, IEnumerable<TElement>> BottomN<TDocument, TElement>(
            FieldDefinition<TDocument, IEnumerable<TElement>> input,
            SortDefinition<TElement> sortBy,
            int n)
        {
            Ensure.IsNotNull(input, nameof(input));
            Ensure.IsNotNull(sortBy, nameof(sortBy));
            Ensure.IsGreaterThanZero(n, nameof(n));
            return new MultiplePickExpressionDefinition<TDocument, TElement>("$bottomN", input, sortBy, n);
        }

        // -- private implementation classes --

        private sealed class SinglePickExpressionDefinition<TDocument, TElement>
            : AggregateExpressionDefinition<TDocument, TElement>
        {
            private readonly string _operator;
            private readonly FieldDefinition<TDocument, IEnumerable<TElement>> _input;
            private readonly SortDefinition<TElement> _sortBy;

            public SinglePickExpressionDefinition(
                string @operator,
                FieldDefinition<TDocument, IEnumerable<TElement>> input,
                SortDefinition<TElement> sortBy)
            {
                _operator = @operator;
                _input = input;
                _sortBy = sortBy;
            }

            public override BsonValue Render(RenderArgs<TDocument> args)
            {
                var renderedInput = _input.Render(args);
                var elementSerializer = GetElementSerializer(renderedInput.ValueSerializer, args.SerializerRegistry);
                var sortDocument = _sortBy.Render(args.WithNewDocumentType(elementSerializer));

                return new BsonDocument(_operator, new BsonDocument
                {
                    { "input", "$" + renderedInput.FieldName },
                    { "sortBy", sortDocument }
                });
            }
        }

        private sealed class MultiplePickExpressionDefinition<TDocument, TElement>
            : AggregateExpressionDefinition<TDocument, IEnumerable<TElement>>
        {
            private readonly string _operator;
            private readonly FieldDefinition<TDocument, IEnumerable<TElement>> _input;
            private readonly SortDefinition<TElement> _sortBy;
            private readonly int _n;

            public MultiplePickExpressionDefinition(
                string @operator,
                FieldDefinition<TDocument, IEnumerable<TElement>> input,
                SortDefinition<TElement> sortBy,
                int n)
            {
                _operator = @operator;
                _input = input;
                _sortBy = sortBy;
                _n = n;
            }

            public override BsonValue Render(RenderArgs<TDocument> args)
            {
                var renderedInput = _input.Render(args);
                var elementSerializer = GetElementSerializer(renderedInput.ValueSerializer, args.SerializerRegistry);
                var sortDocument = _sortBy.Render(args.WithNewDocumentType(elementSerializer));

                return new BsonDocument(_operator, new BsonDocument
                {
                    { "input", "$" + renderedInput.FieldName },
                    { "sortBy", sortDocument },
                    { "n", _n }
                });
            }
        }

        private static IBsonSerializer<TElement> GetElementSerializer<TElement>(
            IBsonSerializer<IEnumerable<TElement>> valueSerializer,
            IBsonSerializerRegistry registry)
        {
            if (valueSerializer is IBsonArraySerializer arraySerializer &&
                arraySerializer.TryGetItemSerializationInfo(out var itemInfo) &&
                itemInfo.Serializer is IBsonSerializer<TElement> elementSerializer)
            {
                return elementSerializer;
            }

            return registry.GetSerializer<TElement>();
        }
    }
}
