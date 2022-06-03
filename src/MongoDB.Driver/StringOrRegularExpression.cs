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
using System.Text.RegularExpressions;
using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a value that is either a string or a BsonRegularExpression.
    /// </summary>
    public class StringOrRegularExpression
    {
        #region static
        /// <summary>
        /// Implicit conversion from string to StringOrRegularExpression.
        /// </summary>
        /// <param name="value">A StringOrRegularExpression.</param>
        /// <returns>A StringOrRegularExpression.</returns>
        public static implicit operator StringOrRegularExpression(string value) => new StringOrRegularExpression(value);

        /// <summary>
        /// Implicit conversion from BsonRegularExpression to StringOrRegularExpression.
        /// </summary>
        /// <param name="value">A StringOrRegularExpression.</param>
        /// <returns>A StringOrRegularExpression.</returns>
        public static implicit operator StringOrRegularExpression(BsonRegularExpression value) => new StringOrRegularExpression(value);

        /// <summary>
        /// Implicit conversion from Regex to StringOrRegularExpression.
        /// </summary>
        /// <param name="value">A StringOrRegularExpression.</param>
        /// <returns>A StringOrRegularExpression.</returns>
        public static implicit operator StringOrRegularExpression(Regex value) => new StringOrRegularExpression(new BsonRegularExpression(value));
        #endregion

        private readonly Type _type;
        private readonly object _value;

        /// <summary>
        /// Initializes an instance of a StringOrRegularExpression.
        /// </summary>
        /// <param name="value">A string value.</param>
        public StringOrRegularExpression(string value)
        {
            _type = typeof(string);
            _value = value;
        }

        /// <summary>
        /// Initializes an instance of a StringOrRegularExpression.
        /// </summary>
        /// <param name="value">A BsonRegularExpression value.</param>
        public StringOrRegularExpression(BsonRegularExpression value)
        {
            _type = typeof(BsonRegularExpression);
            _value = value;
        }

        /// <summary>
        /// Gets the BsonRegularExpression value (returns null if value is not a BsonRegularExpression).
        /// </summary>
        public BsonRegularExpression RegularExpression => (BsonRegularExpression)_value;

        /// <summary>
        /// Gets the string value (returns null if value is not a string).
        /// </summary>
        public string String => (string)_value;

        /// <summary>
        /// Gets the type of the value.
        /// </summary>
        public Type Type => _type;
    }
}
