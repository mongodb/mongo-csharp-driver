/* Copyright 2021-present MongoDB Inc.
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

// this directive is configured/used only in cake packaging tests
#if CONSOLE_TEST
using System;
#endif
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using FluentAssertions;
using MongoDB.Libmongocrypt;
using Xunit;

namespace MongoDB.Driver.Tests.Packaging
{
    /// <summary>
    /// Be aware that this test file will be also called from packaging script-based tests that don't have references on our test helpers.
    /// So, don't add any complex tests here other than simple smoke ones that only validate that a required native library is available.
    /// Also, use only public classes or reflection here.
    /// </summary>
    [Trait("Category", "Packaging")]
    public class PackagingTests
    {
        // keep these tests in sync with CONSOLE_TEST's Main method
        [Fact]
        public static void Libmongocrypt_library_should_provide_library_version()
        {
            string version = null;
            var exception = Record.Exception(() => version = Library.Version);
            if (IsX64Bitness())
            {
                exception.Should().BeNull();
                version.Should().Be("1.2.1");
            }
            else
            {
                exception
                    .Should().BeOfType<PlatformNotSupportedException>()
                    .Subject.Message
                    .Should().Be("MongoDB.Libmongocrypt needs to be run in a 64-bit process.");
                version.Should().BeNull();
            }
        }

        [Fact]
        public static void Snappy_compression_should_provide_snappy_max_compressed_length_x32()
        {
            uint? result = null;
            var exception = Record.Exception(
                () => result = InvokeStaticMethod<uint>(
                    assemblyName: "MongoDB.Driver.Core",
                    className: "MongoDB.Driver.Core.Compression.Snappy.Snappy32NativeMethods",
                    methodName: "snappy_max_compressed_length",
                    arguments: (uint)10));

            if (IsX64Bitness())
            {
                var currentOperatingSystem = GetCurrentOperatingSystem();
                // validate packaging behavior, this code path should not be reached
                Exception ex;
                if (currentOperatingSystem == OperatingSystemPlatform.Windows)
                {
                    ex = exception.Should().BeOfType<LibraryLoadingException>().Subject;
                    ex.Message.Should().Contain("Error code: 193.").And.Contain("snappy32"); // this code means that we're trying to work with x32 built library in x64 mode
                }
                else
                {
                    ex = exception.Should().BeOfType<InvalidOperationException>().Subject;
                    ex.Message.Should().Be($"Snappy is not supported on the current platform: {currentOperatingSystem} and x32 bitness.");
                }
                result.Should().BeNull();
            }
            else
            {
                exception.Should().BeNull();
                result.Should().Be((uint)43);
            }
        }

        [Fact]
        public static void Snappy_compression_should_provide_snappy_max_compressed_length_x64()
        {
            ulong? result = null;
            var exception = Record.Exception(
                () => result = InvokeStaticMethod<ulong>(
                    assemblyName: "MongoDB.Driver.Core",
                    className: "MongoDB.Driver.Core.Compression.Snappy.Snappy64NativeMethods",
                    methodName: "snappy_max_compressed_length",
                    arguments: (ulong)10));

            if (IsX64Bitness())
            {
                exception.Should().BeNull();
                result.Should().Be((ulong)43);
            }
            else
            {
                exception
                    .Should().BeOfType<PlatformNotSupportedException>()
                    .Subject.Message
                    .Should().Be("Native libraries can be loaded only in a 64-bit process.");
                result.Should().BeNull();
            }
        }

        [Fact]
        public static void Zstandard_compression_should_provide_MaxCompressionLevel()
        {
            int? result = null;
            var exception = Record.Exception(
                () => result = GetStaticFieldValue<int>(
                        assemblyName: "MongoDB.Driver.Core",
                        className: "MongoDB.Driver.Core.Compression.Zstandard.ZstandardNativeWrapper",
                        fieldName: "MaxCompressionLevel"));

            if (IsX64Bitness())
            {
                exception.Should().BeNull();
                result.Should().Be(22);
            }
            else
            {
                exception
                    .Should().BeOfType<PlatformNotSupportedException>()
                    .Subject.Message
                    .Should().Be("Native libraries can be loaded only in a 64-bit process.");
                result.Should().NotHaveValue();
            }
        }

        // private methods
        private static OperatingSystemPlatform GetCurrentOperatingSystem()
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

        private static T GetStaticFieldValue<T>(string assemblyName, string className, string fieldName)
        {
            try
            {
                var assembly = Assembly.Load(new AssemblyName(assemblyName));
                var @class = assembly.GetType(className, throwOnError: true).GetTypeInfo();
                var property = @class.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Static);
                return (T)property.GetValue(null);
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        private static T InvokeStaticMethod<T>(string assemblyName, string className, string methodName, params object[] arguments)
        {
            try
            {
                var assembly = Assembly.Load(new AssemblyName(assemblyName));
                var @class = assembly.GetType(className, throwOnError: true).GetTypeInfo();
                var method = @class.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                return (T)method.Invoke(null, arguments);
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        private static bool IsX64Bitness()
        {
            return 
#if NETCOREAPP1_1
                IntPtr.Size == 8;
#else
                Environment.Is64BitProcess;
#endif
        }

#pragma warning disable IDE0051 // Remove unused private members
        private static void RunAllTests()
        {
            // Left it outside Main method to protect us from losing sync between xunit and console modes
            Libmongocrypt_library_should_provide_library_version();

            Snappy_compression_should_provide_snappy_max_compressed_length_x32();

            Snappy_compression_should_provide_snappy_max_compressed_length_x64();

            Zstandard_compression_should_provide_MaxCompressionLevel();
        }
#pragma warning restore IDE0051 // Remove unused private members

#if CONSOLE_TEST
        static void Main(string[] args)
        {
            RunAllTests();

            var defaultForegroundColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nAll packaging tests passed.");
            Console.ForegroundColor = defaultForegroundColor;
        }
#endif

        // nested types
        internal enum OperatingSystemPlatform
        {
            Windows,
            Linux,
            MacOS
        }
    }
}
