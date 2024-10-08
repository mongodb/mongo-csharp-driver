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

namespace MongoDB.Driver.Encryption.Tests
{
    public class HmacShaCallbacksTests
    {
        [Theory]
        [InlineData(256, "74657374206f66206d6163", "37626663386235656333306537336439386565666133653263633334643635376535323734623537656633326661353862663638313534396535663737303138", "ebfaa874ff7fcf5b48637a4aff49ed60f48b53a0802719d6ad85f96d315b2df2")]
        [InlineData(512, "74657374206f6620686d61630a", "06645237ece5638d1dcb66c70d8158c6ba5922dce3ae9f95242147fce0f989d9", "c8bc88465593980da5ed9bd213dcc4594106f6573d08eddc2b7cead3a642ef37dd848e8901a8c340f2a5d909057d28b1355fc9c82e7f7710e688f8c0c7635e9a")]
        public void HmacShaTest(int bitness, string inputHex, string keyHex, string expectedHex)
        {
            var keyBytes = CallbackUtils.GetBytesFromHex(keyHex);
            var inputBytes = CallbackUtils.GetBytesFromHex(inputHex);
            var expectedBytes = CallbackUtils.GetBytesFromHex(expectedHex);

            var resultBytes = HmacShaCallbacks.CalculateHash(keyBytes, inputBytes, bitness);
            resultBytes.Should().Equal(expectedBytes);
        }
    }
}
