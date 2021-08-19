﻿/* Copyright 2019-present MongoDB Inc.
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
using System.Runtime.InteropServices;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.NativeLibraryLoader
{
    internal class DarwinLibraryLoader : INativeLibraryLoader
    {
        // See dlfcn.h
        // #define RTLD_LAZY       0x1
        // #define RTLD_NOW        0x2
        // #define RTLD_LOCAL      0x4
        // #define RTLD_GLOBAL     0x8
        private const int RTLD_GLOBAL = 0x8;
        private const int RTLD_NOW = 0x2;

        private readonly IntPtr _handle;

        public DarwinLibraryLoader(string path)
        {
            Ensure.IsNotNullOrEmpty(path, nameof(path));

            _handle = NativeMethods.dlopen(path, RTLD_GLOBAL | RTLD_NOW);
            if (_handle == IntPtr.Zero)
            {
                throw new FileNotFoundException(path);
            }
        }

        // public methods
        public IntPtr GetFunctionPointer(string name)
        {
            Ensure.IsNotNullOrEmpty(name, nameof(name));

            return NativeMethods.dlsym(_handle, name);
        }

        // nested types
        private static class NativeMethods
        {
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments
            [DllImport("libdl", CharSet = CharSet.Ansi)]
            public static extern IntPtr dlopen(string filename, int flags);

            [DllImport("libdl", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern IntPtr dlsym(IntPtr handle, string symbol);
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
        }
    }
}
