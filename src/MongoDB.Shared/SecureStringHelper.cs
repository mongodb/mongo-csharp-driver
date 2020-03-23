/* Copyright 2020-present MongoDB Inc.
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

using System.Runtime.InteropServices;
using System.Security;

namespace MongoDB.Shared
{
    internal static class SecureStringHelper
    {
        /// <summary>
        /// Should only be used when the safety of the data cannot be guaranteed.
        /// For instance, when the secure string is a password used in a plain text protocol.
        /// </summary>
        /// <param name="secureString">The secure string.</param>
        /// <returns>The CLR string.</returns>
        public static string ToInsecureString(SecureString secureString)
        {
            if (secureString == null || secureString.Length == 0)
            {
                return "";
            }
            else
            {
#if NET452
                var secureStringIntPtr = Marshal.SecureStringToGlobalAllocUnicode(secureString);
#else
                var secureStringIntPtr = SecureStringMarshal.SecureStringToGlobalAllocUnicode(secureString);
#endif
                try
                {
                    return Marshal.PtrToStringUni(secureStringIntPtr, secureString.Length);
                }
                finally
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(secureStringIntPtr);
                }
            }
        }

        /// <summary>
        /// Converts <see cref="System.String"/> to <see cref="SecureString"/>.
        /// </summary>
        /// <param name="value">The string value.</param>
        /// <returns>
        /// The secure string.
        /// </returns>
        public static SecureString ToSecureString(string value)
        {
            var secureString = new SecureString();
            foreach (var c in value)
            {
                secureString.AppendChar(c);
            }
            secureString.MakeReadOnly();

            return secureString;
        }
    }
}
