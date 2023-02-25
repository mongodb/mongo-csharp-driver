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

namespace MongoDB.TestHelpers
{
    public static class FluentAssertionsHelper
    {
        /// <summary>
        /// Is needed to workaround issues with inner string.Format logic inside FluentAssertions  and '{' / '}' in the original message.
        /// </summary>
        /// <param name="stringToEscape">The original message.</param>
        /// <returns>The escaped message.</returns>
        public static string EscapeBraces(string stringToEscape) => stringToEscape.Replace("{", "{{").Replace("}", "}}");
    }
}
