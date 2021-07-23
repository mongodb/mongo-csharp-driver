/* Copyright 2021-present MongoDB Inc.
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

namespace MongoDB.Driver.Core.Misc
{
    internal static class OppressiveLanguageConstants
    {
        public const string LegacyHelloCommandName = "isMaster";
        public const string LegacyHelloCommandNameLowerCase = "ismaster";
        // The isMaster command response contains a boolean field ismaster (all lowercase)
        // to indicate whether the current node is the primary.
        public const string LegacyHelloResponseIsWritablePrimaryFieldName = "ismaster";

        public const string LegacyNotPrimaryErrorMessage = "not master";
        public const string LegacyNotPrimaryOrSecondaryErrorMessage = "not master or secondary";
    }
}
