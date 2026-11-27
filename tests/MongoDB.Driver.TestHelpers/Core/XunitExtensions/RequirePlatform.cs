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
using System.Linq;
using System.Runtime.InteropServices;
using Xunit.Sdk;

namespace MongoDB.Driver.TestHelpers
{
    public enum SupportedTargetFramework
    {
        Net472,
        Net60,
        Net80,
        Net100,
        Net110
    }

    public class RequirePlatform
    {
        #region static
        public static RequirePlatform Check() => new();

        public static SupportedTargetFramework GetCurrentTargetFramework() => TargetFramework.Moniker switch
        {
            "net472" => SupportedTargetFramework.Net472,
            "net60" => SupportedTargetFramework.Net60,
            "net80" => SupportedTargetFramework.Net80,
            "net100" => SupportedTargetFramework.Net100,
            _ => throw new InvalidOperationException($"Unable to determine current target framework: {TargetFramework.Moniker}.")
        };

        #endregion

        public RequirePlatform SkipWhen(OSPlatform operatingSystem, params SupportedTargetFramework[] targetFrameworks)
        {
            if (RuntimeInformation.IsOSPlatform(operatingSystem))
            {
                var currentTargetFramework = GetCurrentTargetFramework();
                if ((targetFrameworks?.Length ?? 0) == 0 || targetFrameworks.Contains(currentTargetFramework))
                {
                    throw new SkipException($"Test skipped because it's not supported on {operatingSystem} with {currentTargetFramework}.");
                }
            }

            return this;
        }

        public RequirePlatform SkipWhen(Func<bool> condition, OSPlatform operatingSystem, params SupportedTargetFramework[] targetFrameworks)
        {
            if (condition())
            {
                SkipWhen(operatingSystem, targetFrameworks);
            }

            return this;
        }
    }
}
