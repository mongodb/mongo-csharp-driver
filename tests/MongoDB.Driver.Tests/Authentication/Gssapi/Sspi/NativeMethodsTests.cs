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

using Xunit;
using MongoDB.Driver.Authentication.Gssapi.Sspi;

namespace MongoDB.Driver.Tests.Authentication.Gssapi.Sspi
{
    public class NativeMethodsTests
    {
        [Theory]
        [InlineData(NativeMethods.SEC_E_LOGON_DENIED, "The logon failed.")]
        [InlineData(NativeMethods.SEC_E_NO_CREDENTIALS, "No credentials are available in the security package.")]
        [InlineData(NativeMethods.SEC_I_RENEGOTIATE, "The remote party requires a new handshake sequence or the application has just initiated a shutdown.")]
        public void CreateException_with_known_error_code_does_not_use_default_message(long errorCode, string message)
        {
            var exception = NativeMethods.CreateException(errorCode, "default");
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void CreateException_with_unknown_error_code_uses_default_message()
        {
            var errorCode = 0x99999999L; // arbitrary long not in the switch
            var exception = NativeMethods.CreateException(errorCode, "Default message.");
            Assert.Equal("Default message. Error code 0x99999999.", exception.Message);
        }
    }
}
