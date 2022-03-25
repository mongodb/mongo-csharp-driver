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
using System.Runtime.InteropServices;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.NativeLibraryLoader
{
    internal class LibraryLoader
    {
        private readonly INativeLibraryLoader _nativeLoader;

        public LibraryLoader(ILibraryLocator libraryLocator)
        {
            Ensure.IsNotNull(libraryLocator, nameof(libraryLocator));
            if (!libraryLocator.IsX32ModeSupported)
            {
                ThrowIfNot64BitProcess(libraryLocator);
            }
            _nativeLoader = CreateNativeLoader(libraryLocator);
        }

        // public methods
        public T GetDelegate<T>(string name)
        {
            IntPtr ptr = _nativeLoader.GetFunctionPointer(name);
            if (ptr == IntPtr.Zero)
            {
                throw new TypeLoadException($"The function {name} was not found.");
            }

            return Marshal.GetDelegateForFunctionPointer<T>(ptr);
        }

        // private methods
        private INativeLibraryLoader CreateNativeLoader(ILibraryLocator libraryLocator)
        {
            var currentPlatform = OperatingSystemHelper.CurrentOperatingSystem;
            var absolutePath = libraryLocator.GetLibraryAbsolutePath(currentPlatform);
            return CreateNativeLoader(currentPlatform, absolutePath, libraryLocator.LibraryName);
        }

        private INativeLibraryLoader CreateNativeLoader(OperatingSystemPlatform currentPlatform, string libraryPath, string libraryName)
        {
            switch (currentPlatform)
            {
                case OperatingSystemPlatform.Linux:
                    return new LinuxLibraryLoader(libraryPath);
                case OperatingSystemPlatform.MacOS:
                    return new DarwinLibraryLoader(libraryPath);
                case OperatingSystemPlatform.Windows:
                    return new WindowsLibraryLoader(libraryPath);
                default:
                    throw new PlatformNotSupportedException($"Error loading library {libraryName}: Unexpected platform {currentPlatform}.");
            }
        }

        private void ThrowIfNot64BitProcess(ILibraryLocator libraryLocator)
        {
            if (!Environment.Is64BitProcess)
            {
                throw new PlatformNotSupportedException($"{libraryLocator.LibraryName} can be loaded only in a 64-bit process.");
            }
        }
    }
}
