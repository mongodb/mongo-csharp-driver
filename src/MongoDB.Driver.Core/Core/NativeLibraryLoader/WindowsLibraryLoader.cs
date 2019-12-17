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
using MongoDB.Libmongocrypt;

namespace MongoDB.Driver.Core.NativeLibraryLoader
{
    internal class WindowsLibraryLoader : INativeLibraryLoader
    {
        private readonly IntPtr _handle;

        public WindowsLibraryLoader(string path)
        {
            Ensure.IsNotNullOrEmpty(path, nameof(path));

            _handle = NativeMethods.LoadLibrary(path);
            if (_handle == IntPtr.Zero)
            {
                var gle = Marshal.GetLastWin32Error();

                // error code 193 indicates that a 64-bit OS has tried to load a 32-bit dll
                // https://docs.microsoft.com/en-us/windows/win32/debug/system-error-codes--0-499-
                throw new LibraryLoadingException($"Error loading library: {path}. Windows Error code: {gle}.");
            }
        }

        // public methods
        public IntPtr GetFunctionPointer(string name)
        {
            Ensure.IsNotNullOrEmpty(name, nameof(name));

            var ptr = NativeMethods.GetProcAddress(_handle, name);
            if (ptr == null)
            {
                var gle = Marshal.GetLastWin32Error();
                throw new TypeLoadException($"Error loading function: {name}. Windows Error code: {gle}.");
            }

            return ptr;
        }

        // nested types
        private static class NativeMethods
        {
#pragma warning disable CA2101
            [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
            public static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)]string lpFileName);
#pragma warning restore CA2101

#pragma warning disable CA2101
            [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
            public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
#pragma warning restore CA2101
        }
    }
}
