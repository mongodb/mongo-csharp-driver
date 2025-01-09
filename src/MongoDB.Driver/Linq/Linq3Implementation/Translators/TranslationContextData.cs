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
    internal sealed class TranslationContextData
    {
        private readonly Dictionary<string, object> _data;

        public TranslationContextData()
            : this(new Dictionary<string, object>())
        {
        }

        private TranslationContextData(Dictionary<string, object> data)
        {
            _data = Ensure.IsNotNull(data, nameof(data));
        }

        public TValue GetValue<TValue>(string key)
        {
            return (TValue)_data[key];
        }

        public TValue GetValueOrDefault<TValue>(string key, TValue defaultValue)
        {
            return _data.TryGetValue(key, out var value) ? (TValue)value : defaultValue;
        }

        public TranslationContextData With<TValue>(string key, TValue value)
        {
            var clonedData = new Dictionary<string, object>(_data);
            clonedData.Add(key, value);
            return new TranslationContextData(clonedData);
        }
    }
}
