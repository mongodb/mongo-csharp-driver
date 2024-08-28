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

using System;

namespace MongoDB.Driver.Authentication.Gssapi.Libgssapi
{
    internal enum GssCode
    {
        GSS_C_GSS_CODE = 1,
        GSS_C_MECH_CODE = 2
    }

    internal enum GssCredentialUsage
    {
        GSS_C_BOTH = 0,
        GSS_C_INITIATE = 1,
        GSS_C_ACCEPT = 2
    }

    [Flags]
    internal enum GssFlags
    {
        GSS_C_MUTUAL_FLAG = 2,
        GSS_C_SEQUENCE_FLAG = 8
    }

    internal enum GssStatus : uint
    {
        GSS_S_COMPLETE = 0,
        GSS_S_CONTINUE_NEEDED = 1
    }
}
