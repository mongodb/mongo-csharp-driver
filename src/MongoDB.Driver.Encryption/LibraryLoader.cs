/*
 * Copyright 2019–present MongoDB, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MongoDB.Driver.Encryption
{
    /// <summary>
    /// LibraryLoader abstracts loading C functions from a shared library across OS
    /// </summary>
    internal class LibraryLoader
    {
        private readonly ISharedLibraryLoader _loader;
        private static readonly string __libmongocryptLibPath = Environment.GetEnvironmentVariable("LIBMONGOCRYPT_PATH");

        public LibraryLoader()
        {
            if (!Environment.Is64BitProcess)
            {
                throw new PlatformNotSupportedException($"{this.GetType().Namespace} needs to be run in a 64-bit process.");
            }

            // Windows:
            // https://stackoverflow.com/questions/2864673/specify-the-search-path-for-dllimport-in-net
            //
            // See for better ways
            // https://github.com/dotnet/coreclr/issues/930
            // https://github.com/dotnet/corefx/issues/32015
            List<string> candidatePaths = new List<string>();

            // In the nuget package, get the shared library from a relative path of this assembly
            // Also, when running locally, get the shared library from a relative path of this assembly
            var assembly = typeof(LibraryLoader).GetTypeInfo().Assembly;
            var location = assembly.Location;
            string basepath = Path.GetDirectoryName(location);
            candidatePaths.Add(basepath);

            switch (OperatingSystemHelper.CurrentOperatingSystem)
            {
                case OperatingSystemPlatform.MacOS:
                    _loader = new DarwinLibraryLoader(candidatePaths);
                    break;
                case OperatingSystemPlatform.Linux:
                    _loader = new LinuxLibrary(candidatePaths);
                    break;
                case OperatingSystemPlatform.Windows:
                    _loader = new WindowsLibrary(candidatePaths);
                    break;
                default:
                    // should not be reached. If we're here, then there is a bug in OperatingSystemHelper
                    throw new PlatformNotSupportedException("Unsupported operating system.");
            }
        }

        private static string FindLibrary(IList<string> basePaths, string[] suffixPaths, string library)
        {
            var candidates = new List<string>();
            foreach (var basePath in basePaths)
            {
                foreach (var suffix in suffixPaths)
                {
                    string path = Path.Combine(basePath, suffix, library);
                    if (File.Exists(path))
                    {
                        return path;
                    }
                    candidates.Add(path);
                }
            }

            throw new FileNotFoundException("Could not find: " + library + " --\n Tried: " + string.Join(",", candidates));
        }

        public T GetFunction<T>(string name)
        {
            IntPtr ptr = _loader.GetFunction(name);
            if (ptr == IntPtr.Zero)
            {
                throw new FunctionNotFoundException(name);
            }

            return Marshal.GetDelegateForFunctionPointer<T>(ptr);

        }

#pragma warning disable CA1032
        public class FunctionNotFoundException : Exception
#pragma warning restore CA1032
        {
            public FunctionNotFoundException(string message) : base(message) { }
        }

        private interface ISharedLibraryLoader
        {
            IntPtr GetFunction(string name);
        }

        /// <summary>
        /// macOS Dynamic Library loader using dlsym
        /// </summary>
        private class DarwinLibraryLoader : ISharedLibraryLoader
        {

            // See dlfcn.h
            // #define RTLD_LAZY       0x1
            // #define RTLD_NOW        0x2
            // #define RTLD_LOCAL      0x4
            // #define RTLD_GLOBAL     0x8
            public const int RTLD_GLOBAL = 0x8;
            public const int RTLD_NOW = 0x2;

            private static readonly string[] __suffixPaths =
            {
                "../../runtimes/osx/native/",
                "runtimes/osx/native/",
                string.Empty
            };

            private readonly IntPtr _handle;
            public DarwinLibraryLoader(List<string> candidatePaths)
            {
                var path = __libmongocryptLibPath ?? FindLibrary(candidatePaths, __suffixPaths, "libmongocrypt.dylib");
                _handle = dlopen(path, RTLD_GLOBAL | RTLD_NOW);
                if (_handle == IntPtr.Zero)
                {
                    throw new FileNotFoundException(path);
                }
            }

            public IntPtr GetFunction(string name)
            {
                return dlsym(_handle, name);
            }

#pragma warning disable IDE1006 // Naming Styles
            [DllImport("libdl")]
            public static extern IntPtr dlopen(string filename, int flags);

            [DllImport("libdl", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern IntPtr dlsym(IntPtr handle, string symbol);
#pragma warning restore IDE1006 // Naming Styles
        }

        /// <summary>
        /// Linux Shared Object loader using dlsym
        /// </summary>
        private class LinuxLibrary : ISharedLibraryLoader
        {
            // See dlfcn.h
            // #define RTLD_LAZY       0x1
            // #define RTLD_NOW        0x2
            // #define RTLD_LOCAL      0x4
            // #define RTLD_GLOBAL     0x100
            public const int RTLD_GLOBAL = 0x100;
            public const int RTLD_NOW = 0x2;
            private static readonly bool _use_libdl1;
            private static readonly string[] __suffixPaths;

#pragma warning disable CA1810
            static LinuxLibrary()
#pragma warning restore CA1810
            {
#pragma warning disable CA1304
                var osArchitecture = RuntimeInformation.OSArchitecture.ToString().ToLower();
#pragma warning restore CA1304

                var finalpath = IsAlpine() ? $"alpine/{osArchitecture}" : $"{osArchitecture}";

                __suffixPaths = new []{
                    $"../../runtimes/linux/native/{finalpath}",
                    $"runtimes/linux/native/{finalpath}",
                    string.Empty
                };

                try
                {
                    Libdl1.dlerror();
                    _use_libdl1 = true;
                }
                catch
                {
                    _use_libdl1 = false;
                }
            }

            private readonly IntPtr _handle;
            public LinuxLibrary(List<string> candidatePaths)
            {
                var path = __libmongocryptLibPath ?? FindLibrary(candidatePaths, __suffixPaths, "libmongocrypt.so");

                _handle = _use_libdl1
                    ? Libdl1.dlopen(path, RTLD_GLOBAL | RTLD_NOW)
                    : Libdl2.dlopen(path, RTLD_GLOBAL | RTLD_NOW);

                if (_handle == IntPtr.Zero)
                {
                    throw new FileNotFoundException(path);
                }
            }

            public IntPtr GetFunction(string name)
            {
                return _use_libdl1 ? Libdl1.dlsym(_handle, name) : Libdl2.dlsym(_handle, name);
            }

            private static bool IsAlpine()
            {
                var osRealesePath = "/etc/os-release";
                var prettyName = "PRETTY_NAME";

                if (File.Exists(osRealesePath))
                {
                    foreach(var line in File.ReadAllLines(osRealesePath))
                    {
                        if (line.StartsWith(prettyName))
                        {
                            return line.Contains("Alpine");
                        }
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Windows DLL loader using GetProcAddress
        /// </summary>
        private class WindowsLibrary : ISharedLibraryLoader
        {
            private static readonly string[] __suffixPaths =
            {
                @"..\..\runtimes\win\native\",
                @".\runtimes\win\native\",
                string.Empty
            };

            private readonly IntPtr _handle;
            public WindowsLibrary(List<string> candidatePaths)
            {
                var path = __libmongocryptLibPath ?? FindLibrary(candidatePaths, __suffixPaths, "mongocrypt.dll");
                _handle = LoadLibrary(path);
                if (_handle == IntPtr.Zero)
                {
                    var gle = Marshal.GetLastWin32Error();

                    // error code 193 indicates that a 64-bit OS has tried to load a 32-bit dll
                    // https://docs.microsoft.com/en-us/windows/win32/debug/system-error-codes--0-499-
                    throw new LibraryLoadingException(path + ", Windows Error: " + gle);
                }
            }

            public IntPtr GetFunction(string name)
            {
                var ptr = GetProcAddress(_handle, name);
                if (ptr == IntPtr.Zero)
                {
                    var gle = Marshal.GetLastWin32Error();
                    throw new FunctionNotFoundException(name + ", Windows Error: " + gle);
                }

                return ptr;
            }

            [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
            public static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);

            [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
            public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        }

        private static class Libdl1
        {
            private const string LibName = "libdl";

#pragma warning disable IDE1006 // Naming Styles
            [DllImport(LibName)]
            public static extern IntPtr dlopen(string filename, int flags);

            [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern IntPtr dlsym(IntPtr handle, string symbol);

            [DllImport(LibName)]
            public static extern string dlerror();
#pragma warning restore IDE1006 // Naming Styles
        }

        private static class Libdl2
        {
            private const string LibName = "libdl.so.2";

#pragma warning disable IDE1006 // Naming Styles
            [DllImport(LibName)]
            public static extern IntPtr dlopen(string filename, int flags);

            [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern IntPtr dlsym(IntPtr handle, string symbol);
#pragma warning restore IDE1006 // Naming Styles
        }
    }
}
