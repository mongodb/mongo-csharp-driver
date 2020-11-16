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

namespace MongoDB.Driver.Linq3.Translators.ExpressionToPipelineTranslators
{
    public class GroupByKeyValue<TKey, TValue>
    {
        // private fields
        private readonly TKey _key;
        private readonly TValue _value;

        // constructors
        public GroupByKeyValue(TKey key, TValue value)
        {
            _key = key;
            _value = value; // can be null
        }

        // public properties
        public TKey Key => _key;
        public TValue Value => _value;
    }
}
