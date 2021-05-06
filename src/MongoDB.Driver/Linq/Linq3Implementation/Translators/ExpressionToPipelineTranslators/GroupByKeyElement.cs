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

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators
{
    internal class GroupByKeyElement<TKey, TElement>
    {
        // private fields
        private readonly TKey _key;
        private readonly TElement _element;

        // constructors
        public GroupByKeyElement(TKey key, TElement element)
        {
            _key = key;
            _element = element; // can be null
        }

        // public properties
        public TKey Key => _key;
        public TElement Element => _element;
    }
}
