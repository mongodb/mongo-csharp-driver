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
* 
*/

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// Represents how values are weighted when computing the exponential moving average.
    /// </summary>
    public abstract class ExponentialMovingAverageWeighting
    {
        #region static
        /// <summary>
        /// Returns an alpha weighting.
        /// </summary>
        /// <param name="alpha">The alpha value.</param>
        /// <returns>An alpha weighting.</returns>
        public static ExponentialMovingAverageAlphaWeighting Alpha(double alpha) => new ExponentialMovingAverageAlphaWeighting(alpha);

        /// <summary>
        /// Returns an positional weighting.
        /// </summary>
        /// <param name="n">The n value.</param>
        /// <returns>An n weighting.</returns>
        public static ExponentialMovingAveragePositionalWeighting N(int n) => new ExponentialMovingAveragePositionalWeighting(n);
        #endregion

        internal ExponentialMovingAverageWeighting() { } // disallow user defined subclasses
    }

    /// <summary>
    /// Represents an alpha weighting for an exponential moving average.
    /// </summary>
    public sealed class ExponentialMovingAverageAlphaWeighting : ExponentialMovingAverageWeighting
    {
        private readonly double _alpha;

        /// <summary>
        /// Initializes an instance of ExponentialMovingAverageAlphaWeighting.
        /// </summary>
        /// <param name="alpha">The alpha value.</param>
        internal ExponentialMovingAverageAlphaWeighting(double alpha)
        {
            _alpha = alpha;
        }

        /// <summary>
        /// The alpha value.
        /// </summary>
        public new double Alpha => _alpha;
    }

    /// <summary>
    /// Represents a positional weighting for an exponential moving average.
    /// </summary>
    public sealed class ExponentialMovingAveragePositionalWeighting : ExponentialMovingAverageWeighting
    {
        private readonly int _n;

        /// <summary>
        /// Initializes an instance of ExponentialMovingAveragePositionalWeighting.
        /// </summary>
        /// <param name="n">The n value.</param>
        internal ExponentialMovingAveragePositionalWeighting(int n)
        {
            _n = n;
        }

        /// <summary>
        /// The n value.
        /// </summary>
        public new int N => _n;
    }
}
