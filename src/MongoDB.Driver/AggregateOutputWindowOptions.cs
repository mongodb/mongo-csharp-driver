/* Copyright 2021-present MongoDB Inc.
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

namespace MongoDB.Driver
{
    /// <summary>
    /// Options for the aggregate $setWindowFields stage.
    /// </summary>
    /// <typeparam name="TOutput">The type of the output document.</typeparam>
    public abstract class AggregateOutputWindowOptionsBase<TOutput>
    {
        /// <summary>
        /// The output window field.
        /// </summary>
        public abstract FieldDefinition<TOutput> OutputWindowField { get; }

        /// <summary>
        /// The window documents.
        /// </summary>
        public WindowRange Documents { get; set; }

        /// <summary>
        /// The window range. 
        /// </summary>
        public WindowRange Range { get; set; }

        /// <summary>
        /// The window time unit. 
        /// </summary>
        public WindowTimeUnit? Unit { get; set; }
    }


    /// <summary>
    /// Options for the aggregate $setWindowFields stage.
    /// </summary>
    public class AggregateOutputWindowOptions<TOutput> : AggregateOutputWindowOptionsBase<TOutput>
    {
        private readonly FieldDefinition<TOutput> _outputFieldDefinition;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateOutputWindowOptions{TOutput}"/> class.
        /// </summary>
        /// <param name="outputFieldDefinition">The output field.</param>
        public AggregateOutputWindowOptions(FieldDefinition<TOutput> outputFieldDefinition)
        {
            _outputFieldDefinition = Ensure.IsNotNull(outputFieldDefinition, nameof(outputFieldDefinition));
        }

        /// <inheritdoc/>
        public override FieldDefinition<TOutput> OutputWindowField => _outputFieldDefinition;
    }

    /// <summary>
    /// Options for the aggregate $setWindowFields stage with a typed key.
    /// </summary>
    public class AggregateOutputWindowOptions<TOutput, TOutputKey> : AggregateOutputWindowOptionsBase<TOutput>
    {
        private readonly FieldDefinition<TOutput> _outputFieldDefinition;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateOutputWindowOptions{TOutput, TOutputKey}"/> class.
        /// </summary>
        /// <param name="outputFieldDefinition">The output field.</param>
        public AggregateOutputWindowOptions(FieldDefinition<TOutput, TOutputKey> outputFieldDefinition)
        {
            _outputFieldDefinition = Ensure.IsNotNull(outputFieldDefinition, nameof(outputFieldDefinition));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateOutputWindowOptions{TOutput}"/> class.
        /// </summary>
        /// <param name="outputFieldDefinition">The output field expression.</param>
        public AggregateOutputWindowOptions(Expression<Func<TOutput, TOutputKey>> outputFieldDefinition)
        {
            _outputFieldDefinition = new ExpressionFieldDefinition<TOutput>(Ensure.IsNotNull(outputFieldDefinition, nameof(outputFieldDefinition)));
        }

        /// <inheritdoc/>
        public override FieldDefinition<TOutput> OutputWindowField => _outputFieldDefinition;
    }

     /// <summary>
    /// Represents bound options for $setWindowFields stage.
    /// </summary>
    public class WindowBound
    {
        #region static
        /// <summary>
        /// Represents the first or last document position in the partition.
        /// </summary>
        public static WindowBound Unbounded => new WindowBound(Mode.Unbounded);

        /// <summary>
        /// Represents the current document position in the output.
        /// </summary>
        public static WindowBound Current => new WindowBound(Mode.Current);

        /// <summary>
        /// Set the position value.
        /// </summary>
        /// <param name="position">The position value/</param>
        /// <returns>
        /// The WindowBound value.
        /// </returns>
        public static WindowBound SetPosition(int position) => new WindowBound(Mode.Position, position);
        #endregion

        private readonly Mode _mode;
        private readonly int? _position;

        private WindowBound(Mode mode, int? position = default)
        {
            _mode = mode;
            _position = position;
        }

        /// <summary>
        /// The bound value.
        /// </summary>
        public BsonValue Value
        {
            get
            {
                return _mode == Mode.Position ? BsonValue.Create(_position) : BsonValue.Create(_mode.ToString().ToLowerInvariant());
            }
        }

        // nested types
        private enum Mode
        {
            Unbounded,
            Current,
            Position
        }
    }

    /// <summary>
    /// Represents range options for $setWindowFields stage.
    /// </summary>
    public class WindowRange
    {
        #region static
        /// <summary>
        /// Create a window range.
        /// </summary>
        /// <param name="left">The left bound.</param>
        /// <param name="right">The right bound.</param>
        /// <returns></returns>
        public static WindowRange Create(WindowBound left, WindowBound right)
        {
            return new WindowRange(left, right);
        }
        #endregion

        private readonly WindowBound _left;
        private readonly WindowBound _right;

        private WindowRange(WindowBound left, WindowBound right)
        {
            _left = Ensure.IsNotNull(left, nameof(left));
            _right = Ensure.IsNotNull(right, nameof(right));
        }

        /// <summary>
        /// The left bound.
        /// </summary>
        public WindowBound Left => _left;

        /// <summary>
        /// The right bound.
        /// </summary>
        public WindowBound Right => _right;
    }

    /// <summary>
    /// Represents the units for time range window boundaries.
    /// </summary>
    public enum WindowTimeUnit
    {
        /// <summary>
        /// An year.
        /// </summary>
        Year,
        /// <summary>
        /// A quarter.
        /// </summary>
        Quarter,
        /// <summary>
        /// A month.
        /// </summary>
        Month,
        /// <summary>
        /// A week.
        /// </summary>
        Week,
        /// <summary>
        /// A day.
        /// </summary>
        Day,
        /// <summary>
        /// A hour.
        /// </summary>
        Hour,
        /// <summary>
        /// A minute.
        /// </summary>
        Minute,
        /// <summary>
        /// A second.
        /// </summary>
        Second,
        /// <summary>
        /// A millisecond.
        /// </summary>
        Millisecond
    }
}
