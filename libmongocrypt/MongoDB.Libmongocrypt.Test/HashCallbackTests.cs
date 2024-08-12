/*
 * Copyright 2020–present MongoDB, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using FluentAssertions;
using Xunit;

namespace MongoDB.Libmongocrypt.Test
{
    public class HashCallbackTests
    {
        [Fact]
        public void HashTest()
        {
            var inputHex = "74657374206f66206d6163";
            var expectedHex = "9ff3e52fa31c9e0fa0b08e19c40591553ea64b73709633271975bfab2db9d980";
            
            var inputBytes = CallbackUtils.GetBytesFromHex(inputHex);
            var expectedBytes = CallbackUtils.GetBytesFromHex(expectedHex);

            var resultBytes = HashCallback.CalculateHash(inputBytes);
            resultBytes.Should().Equal(expectedBytes);
        }
    }
}
