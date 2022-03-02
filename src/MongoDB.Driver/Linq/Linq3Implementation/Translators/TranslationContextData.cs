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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators
{
    /// <summary>
    /// Represents arbitrary data for a LINQ3 translation context.
    /// </summary>
    public sealed class TranslationContextData
    {
        private readonly Dictionary<string, object> _data;

        /// <summary>
        /// Initializes a new instance of a TranslationContextData.
        /// </summary>
        public TranslationContextData()
            : this(new Dictionary<string, object>())
        {
        }

        private TranslationContextData(Dictionary<string, object> data)
        {
            _data = Ensure.IsNotNull(data, nameof(data));
        }

        /// <summary>
        /// Gets a value.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>The value.</returns>
        public TValue GetValue<TValue>(string key)
        {
            return (TValue)_data[key];
        }

        /// <summary>
        /// Gets a value or a default value if they key is not present.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The value.</returns>
        public TValue GetValueOrDefault<TValue>(string key, TValue defaultValue)
        {
            return _data.TryGetValue(key, out var value) ? (TValue)value : defaultValue;
        }

        /// <summary>
        /// Returns a new TranslationContextData with an additional value.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>A new TranslationContextData with an additional value</returns>
        public TranslationContextData With<TValue>(string key, TValue value)
        {
            var clonedData = new Dictionary<string, object>(_data);
            clonedData.Add(key, value);
            return new TranslationContextData(clonedData);
        }
    }
}
