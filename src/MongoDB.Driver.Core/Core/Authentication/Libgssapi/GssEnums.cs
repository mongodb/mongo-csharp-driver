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

namespace MongoDB.Driver.Core.Authentication.Libgssapi
{
    internal enum GssCode
    {
        GSS_CODE = 1,
        MECH_CODE = 2
    }

    internal enum GssCredentialUsage
    {
        Both = 0,
        Initiate = 1,
        Accept = 2
    }

    [Flags]
    internal enum GssFlags
    {
        Mutual = 2,
        Sequence = 8
    }

    internal enum GssStatus : uint
    {
        Complete = 0,
        ContinueNeeded = 1
    }
}
