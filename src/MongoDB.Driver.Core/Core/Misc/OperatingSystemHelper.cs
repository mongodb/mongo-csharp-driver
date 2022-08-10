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

using System;
using System.Runtime.InteropServices;

namespace MongoDB.Driver.Core.Misc
{
    internal enum OperatingSystemPlatform
    {
        Unsupported = -1,
        Windows,
        Linux,
        MacOS
    }

    internal static class OperatingSystemHelper
    {
        private static readonly OperatingSystemPlatform __currentOperatingSystem;

        static OperatingSystemHelper()
        {
#if NET472
            __currentOperatingSystem = OperatingSystemPlatform.Windows;
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                __currentOperatingSystem = OperatingSystemPlatform.Linux;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                __currentOperatingSystem = OperatingSystemPlatform.MacOS;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                __currentOperatingSystem = OperatingSystemPlatform.Windows;
            }
            else
            {
                __currentOperatingSystem = OperatingSystemPlatform.Unsupported;
            }
#endif
        }

        public static OperatingSystemPlatform CurrentOperatingSystem => __currentOperatingSystem switch
        {
            OperatingSystemPlatform.Unsupported => throw new PlatformNotSupportedException($"Unsupported platform '{RuntimeInformation.OSDescription}'."),
            _ => __currentOperatingSystem
        };
    }
}
