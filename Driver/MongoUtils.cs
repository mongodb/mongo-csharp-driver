/* Copyright 2010-2011 10gen Inc.
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
using System.Security.Cryptography;
using System.Text;

namespace MongoDB.Driver {
    public static class MongoUtils {
        #region public static methods
        public static string Hash(
            string text
        ) {
            var md5 = MD5.Create();
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(text));
            var hash = BitConverter.ToString(bytes).Replace("-", "").ToLower();
            return hash;
        }

        public static string ToCamelCase(
            string value
        ) {
            return value.Substring(0, 1).ToLower() + value.Substring(1);
        }
        #endregion
    }
}
