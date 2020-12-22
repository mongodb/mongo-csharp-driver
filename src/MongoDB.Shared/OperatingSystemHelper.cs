/* Copyright 2020-present MongoDB Inc.
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

namespace MongoDB.Shared
{
    internal enum OperatingSystemPlatform
    {
        Windows,
        Linux,
        MacOS
    }

    internal static class OperatingSystemHelper
    {
        public static OperatingSystemPlatform CurrentOperatingSystem
        {
            get
            {
#if NET452
                return OperatingSystemPlatform.Windows;
#else
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return OperatingSystemPlatform.MacOS;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return OperatingSystemPlatform.Linux;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return OperatingSystemPlatform.Windows;
                }

                // should not be reached. If we're here, then there is a bug in the library
                throw new PlatformNotSupportedException($"Unsupported platform '{RuntimeInformation.OSDescription}'.");
#endif
            }
        }
    }
}
