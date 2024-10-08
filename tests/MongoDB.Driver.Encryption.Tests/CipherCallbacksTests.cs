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
using System.Security.Cryptography;
using Xunit;

namespace MongoDB.Driver.Encryption.Tests
{
    public class CipherCallbacksTests
    {
        [Theory]
        [InlineData(CipherMode.CBC, "671db60d464b09e9c3b03242dd29bdc5")]
        [InlineData(CipherMode.ECB, "ae6b200f30d6e8e424127e9c58affaf8")]
        public void CipherTest(CipherMode mode, string expectedHex)
        {
            var keyHex = "92faa793d717675e2be804584a8a98252083fe6bf16010546a92e2ef4bdd27fd";
            var ivHex = "31164b2f661e41fed5df60bfcfa40baa";
            var inputHex = "379ddb78c30e5e4bf19dd81ae705796f";
            var keyBytes = CallbackUtils.GetBytesFromHex(keyHex);
            var ivBytes = CallbackUtils.GetBytesFromHex(ivHex);
            var inputBytes = CallbackUtils.GetBytesFromHex(inputHex); // decryptedBytes
            var expectedEncryptedBytes = CallbackUtils.GetBytesFromHex(expectedHex);
            var encryptedBytes = CipherCallbacks.AesCrypt(keyBytes, ivBytes, inputBytes, CryptMode.Encrypt, mode);

            encryptedBytes.Should().Equal(expectedEncryptedBytes);

            var decryptedBytes = CipherCallbacks.AesCrypt(keyBytes, ivBytes, encryptedBytes, CryptMode.Decrypt, mode);

            decryptedBytes.Should().Equal(inputBytes);
        }
    }
}
