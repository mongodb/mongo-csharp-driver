/* Copyright 2010-2013 10gen Inc.
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
using System.Security.Cryptography;
using System.Text;

namespace MongoDB.Driver
{
    /// <summary>
    /// Various static utility methods.
    /// </summary>
    public static class MongoUtils
    {
        // public static methods
        /// <summary>
        /// Gets the MD5 hash of a string.
        /// </summary>
        /// <param name="text">The string to get the MD5 hash of.</param>
        /// <returns>The MD5 hash.</returns>
        public static string Hash(string text)
        {
            var md5 = MD5.Create();
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(text));
            var hash = BitConverter.ToString(bytes).Replace("-", "").ToLower();
            return hash;
        }

        /// <summary>
        /// Converts a string to camel case by lower casing the first letter (only the first letter is modified).
        /// </summary>
        /// <param name="value">The string to camel case.</param>
        /// <returns>The camel cased string.</returns>
        public static string ToCamelCase(string value)
        {
            return value.Substring(0, 1).ToLower() + value.Substring(1);
        }
    }
}
