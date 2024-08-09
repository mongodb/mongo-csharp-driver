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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Logging
{
    internal static class LogCategoryHelper
    {
        private static readonly IDictionary<Type, string> __catergories = new ConcurrentDictionary<Type, string>();

        private static readonly string[] __driverNamespaces = new []
        {
            "MongoDB.Bson",
            "MongoDB.Driver",
            "MongoDB.Driver.Core"
        };

        private static readonly string[] __driverTestsNamespaces = new[]
        {
            "MongoDB.Bson.Tests",
            "MongoDB.Bson.TestHelpers",
            "MongoDB.Driver.Tests",
            "MongoDB.Driver.TestHelpers",
            "MongoDB.Driver.Core.Tests",
            "MongoDB.Driver.Core.TestHelpers"
        };

        private static readonly string __specCategoryPrefix = typeof(LogCategories).FullName;

        private const string PrefixSpec = "MongoDB";
        private const string PrefixInternal = "MongoDB.Internal";
        private const string PrefixTests = "MongoDB.Tests";

        public static string DecorateCategoryName(string categoryName)
        {
            Ensure.IsNotNullOrEmpty(categoryName, nameof(categoryName));

            var prefixOverride = categoryName switch
            {
                _ when categoryName.StartsWith(__specCategoryPrefix) => PrefixSpec,
                _ when __driverTestsNamespaces.Any(n => categoryName.StartsWith(n)) => PrefixTests,
                _ when __driverNamespaces.Any(n => categoryName.StartsWith(n)) => PrefixInternal,
                _ => null,
            };

            var result = categoryName;

            if (prefixOverride != null)
            {
                var pathComponents = categoryName.Split('.');
                result = $"{prefixOverride}.{pathComponents.Last()}";
            }

            return result;
        }

        public static string GetCategoryName<T>() where T : LogCategories.BaseCategory
        {
            var type = typeof(T);
            if (!__catergories.TryGetValue(type, out var result))
            {
                result = DecorateCategoryName(type.FullName.Replace('+', '.'));
                __catergories.Add(type, result);
            }

            return result;
        }
    }
}
