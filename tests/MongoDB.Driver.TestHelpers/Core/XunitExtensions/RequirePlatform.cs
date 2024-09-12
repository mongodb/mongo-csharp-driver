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
using System.Linq;
using Xunit.Sdk;

namespace MongoDB.Driver.TestHelpers
{
    public enum SupportedTargetFramework
    {
        Net472,
        NetStandard21,
        Net60
    }

    public enum SupportedOperatingSystem
    {
        Windows,
        Linux,
        MacOS
    }

    public class RequirePlatform
    {
        #region static
        public static RequirePlatform Check() => new RequirePlatform();

        public static SupportedOperatingSystem GetCurrentOperatingSystem()
        {
#if WINDOWS
            return SupportedOperatingSystem.Windows;
#endif
#if LINUX
            return SupportedOperatingSystem.Linux;
#endif
#if MACOS
            return SupportedOperatingSystem.MacOS;
#endif

            throw new InvalidOperationException($"Unable to determine current operating system.");
        }

        public static SupportedTargetFramework GetCurrentTargetFramework() => TargetFramework.Moniker switch
        {
            "net472" => SupportedTargetFramework.Net472,
            "netstandard21" => SupportedTargetFramework.NetStandard21,
            "net60" => SupportedTargetFramework.Net60,
            _ => throw new InvalidOperationException($"Unable to determine current target framework: {TargetFramework.Moniker}.")
        };

        #endregion

        public RequirePlatform SkipWhen(SupportedOperatingSystem operatingSystem, params SupportedTargetFramework[] targetFrameworks)
        {
            var currentOperatingSystem = GetCurrentOperatingSystem();
            var currentTargetFramework = GetCurrentTargetFramework();
            if (operatingSystem == currentOperatingSystem && ((targetFrameworks?.Length ?? 0) == 0 || targetFrameworks.Contains(currentTargetFramework)))
            {
                throw new SkipException($"Test skipped because it's not supported on {currentOperatingSystem} with {currentTargetFramework}.");
            }

            return this;
        }

        public RequirePlatform SkipWhen(Func<bool> condition, SupportedOperatingSystem operatingSystem, params SupportedTargetFramework[] targetFrameworks)
        {
            if (condition())
            {
                SkipWhen(operatingSystem, targetFrameworks);
            }

            return this;
        }
    }
}
