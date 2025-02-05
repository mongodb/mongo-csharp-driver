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
using Xunit.Abstractions;
using Xunit.Sdk;

namespace MongoDB.TestHelpers.XunitExtensions
{
    [TraitDiscoverer("MongoDB.TestHelpers.XunitExtensions.CategoryTraitDiscoverer", "MongoDB.TestHelpers")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class CategoryAttribute : Attribute, ITraitAttribute
    {
        public CategoryAttribute(params string[] categories)
        {
            Categories = categories;
        }

        public string[] Categories { get; }
    }

    public sealed class CategoryTraitDiscoverer : ITraitDiscoverer
    {
        private const string Key = "Category";

        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            var attributeInfo = traitAttribute as ReflectionAttributeInfo;
            var testCaseAttribute = attributeInfo?.Attribute as CategoryAttribute;

            return testCaseAttribute.Categories.Select(category => new KeyValuePair<string, string>(Key, category));
        }
    }
}
