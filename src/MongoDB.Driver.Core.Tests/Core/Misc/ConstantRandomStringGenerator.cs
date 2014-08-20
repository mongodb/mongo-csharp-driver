/* Copyright 2013-2014 MongoDB Inc.
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

using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Misc
{
    internal class ConstantRandomStringGenerator : IRandomStringGenerator
    {
        private readonly string _randomString;

        public ConstantRandomStringGenerator(string randomString)
        {
            _randomString = randomString;
        }

        public string Generate(int length, string legalCharacters)
        {
            return _randomString;
        }
    }
}