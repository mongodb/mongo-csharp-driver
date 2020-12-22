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
using Xunit;

namespace MongoDB.Driver.TestHelpers
{
    public enum SupportedTargetFramework
    {
        Net452,
        NetStandard15,
        NetStandard20,
        NetStandard21
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


        public static SupportedTargetFramework GetCurrentTargetFramework()
        {
#if NET452
            return SupportedTargetFramework.Net452;
#endif
#if NETSTANDARD1_5
            return SupportedTargetFramework.NetStandard15;
#endif
#if NETSTANDARD2_0
            return SupportedTargetFramework.NetStandard20;
#endif
#if NETSTANDARD2_1
            return SupportedTargetFramework.NetStandard21;
#endif

            throw new InvalidOperationException($"Unable to determine current target framework.");
        }
        #endregion

        public RequirePlatform SkipWhen(SupportedOperatingSystem operatingSystem, params SupportedTargetFramework[] targetFrameworks)
        {
            var currentOperatingSystem = GetCurrentOperatingSystem();
            var currentTargetFramework = GetCurrentTargetFramework();
            if (operatingSystem == currentOperatingSystem && (targetFrameworks == null || targetFrameworks.Contains(currentTargetFramework)))
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
