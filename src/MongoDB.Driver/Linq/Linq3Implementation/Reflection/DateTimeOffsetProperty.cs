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
using System.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Reflection
{
    internal static class DateTimeOffsetProperty
    {
        // private static fields
        private static readonly PropertyInfo __now;
        private static readonly PropertyInfo __utcNow;

        // static constructor
        static DateTimeOffsetProperty()
        {
            __now = ReflectionInfo.Property(() => DateTimeOffset.Now);
            __utcNow = ReflectionInfo.Property(() => DateTimeOffset.UtcNow);
        }

        // public properties
        public static PropertyInfo Now => __now;
        public static PropertyInfo UtcNow => __utcNow;
    }
}
