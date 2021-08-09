/* Copyright 2019-present MongoDB Inc.
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
using System.IO;
using System.Reflection;
using MongoDB.Shared;

namespace MongoDB.Driver.Core.NativeLibraryLoader
{
    internal abstract class RelativeLibraryLocatorBase : ILibraryLocator
    {
        // public properties
        public virtual bool IsX32ModeSupported => false;

        // public methods
        public string GetBaseAssemblyDirectory()
        {
            var baseAssemblyPathUri = GetBaseAssemblyUri();
            var uri = new Uri(baseAssemblyPathUri);
            var absoluteAssemblyPath = Uri.UnescapeDataString(uri.AbsolutePath);
            return Path.GetDirectoryName(absoluteAssemblyPath);
        }

        public virtual string GetBaseAssemblyUri() => typeof(RelativeLibraryLocatorBase).GetTypeInfo().Assembly.CodeBase;

        public string GetLibraryAbsolutePath(OperatingSystemPlatform currentPlatform)
        {
            var relativePath = GetLibraryDirectoryRelativePath(currentPlatform);
            var libraryName = GetLibraryName(currentPlatform);
            return GetAbsolutePath(relativePath, libraryName);
        }

        public virtual string GetLibraryDirectoryRelativePath(OperatingSystemPlatform currentPlatform)
        {
            var rid = GetCurrentPlatformRuntimeIdentifier(currentPlatform);
            return Path.Combine("runtimes", rid, "native");
        }

        public abstract string GetLibraryName(OperatingSystemPlatform currentPlatform);

        public virtual string GetCurrentPlatformRuntimeIdentifier(OperatingSystemPlatform currentPlatform)
            => currentPlatform switch
            {
                OperatingSystemPlatform.Windows => "win",
                OperatingSystemPlatform.Linux => "linux",
                OperatingSystemPlatform.MacOS => "osx",
                _ => $"The provided platform {currentPlatform} is not currently supported.",
            };

        // private methods
        private string GetAbsolutePath(string relativeLibraryDirectoryPath, string libraryName)
        {
            var libraryBasePath = GetBaseAssemblyDirectory();

            var absolutePathsToCheck = new[]
            {
                Path.Combine(libraryBasePath, libraryName),  // look in the current assembly folder
                Path.Combine(libraryBasePath, "..", "..", relativeLibraryDirectoryPath, libraryName), // with step back on two folders
                Path.Combine(libraryBasePath, relativeLibraryDirectoryPath, libraryName)
            };

            foreach (var absolutePath in absolutePathsToCheck)
            {
                if (File.Exists(absolutePath))
                {
                    return absolutePath;
                }
            }

            throw new FileNotFoundException($"Could not find library {libraryName}. Checked {string.Join(";", absolutePathsToCheck)}.");
        }
    }
}
