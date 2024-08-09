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

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MongoDB.Driver.Authentication.Gssapi.Libgssapi
{
    internal static class Gss
    {
        public static void ThrowIfError(uint majorStatus, uint minorStatus)
        {
            var majorMessages = new List<string>();
            var minorMessages = new List<string>();

            if (majorStatus != (uint)GssStatus.GSS_S_COMPLETE && majorStatus != (uint)GssStatus.GSS_S_CONTINUE_NEEDED)
            {
                uint messageContext;
                do
                {
                    using var outputBuffer = new GssOutputBuffer();
                    var localMajorStatus = NativeMethods.gss_display_status(out _, majorStatus, GssCode.GSS_C_GSS_CODE, in Oid.GSS_C_NO_OID, out messageContext, outputBuffer);
                    if (localMajorStatus != 0)
                    {
                        throw new LibgssapiException($"Error encountered while attempting to convert majorStatus to textual description. majorStatus: {majorStatus} minorStatus: {minorStatus}.");
                    }
                    majorMessages.Add(Marshal.PtrToStringAnsi(outputBuffer.Value));
                } while (messageContext != 0);
            }

            if (minorStatus != 0)
            {
                uint messageContext;
                do
                {
                    using var outputBuffer = new GssOutputBuffer();
                    var localMajorStatus = NativeMethods.gss_display_status(out _, minorStatus, GssCode.GSS_C_MECH_CODE, in Oid.GSS_C_NO_OID, out messageContext, outputBuffer);
                    if (localMajorStatus != 0)
                    {
                        throw new LibgssapiException($"Error encountered while attempting to convert minorStatus to textual description. majorStatus: {majorStatus} minorStatus: {minorStatus}.");
                    }
                    minorMessages.Add(Marshal.PtrToStringAnsi(outputBuffer.Value));
                } while (messageContext != 0);
            }

            if (majorMessages.Count > 0 || minorMessages.Count > 0)
            {
                var message = $"Libgssapi failure - majorStatus: {string.Join("; ", majorMessages)}; minorStatus: {string.Join("; ", minorMessages)}";
                throw new LibgssapiException(message);
            }
        }
    }
}
