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
using System.Reflection;
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
            Library.Version.Should().Be("1.2.1");
        }

        [Fact]
        public static void Zstandard_compression_should_provide_MaxCompressionLevel()
        {
            var result = GetStaticFieldValue<int>(
                assemblyName: "MongoDB.Driver.Core",
                className: "MongoDB.Driver.Core.Compression.Zstandard.ZstandardNativeWrapper",
                fieldName: "MaxCompressionLevel");

            result.Should().Be(22);
        }

        // private methods
        private static T GetStaticFieldValue<T>(string assemblyName, string className, string fieldName)
        {
            var assembly = Assembly.Load(new AssemblyName(assemblyName));
            var @class = assembly.GetType(className, throwOnError: true).GetTypeInfo();
            var property = @class.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Static);
            return (T)property.GetValue(null);
        }

        private static T InvokeStaticMethod<T>(string assemblyName, string className, string methodName, params object[] arguments)
        {
            var assembly = Assembly.Load(new AssemblyName(assemblyName));
            var @class = assembly.GetType(className, throwOnError: true).GetTypeInfo();
            var method = @class.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            return (T)method.Invoke(null, arguments);
        }

#if CONSOLE_TEST
        static void Main(string[] args)
        {
            Libmongocrypt_library_should_provide_library_version();

            Zstandard_compression_should_provide_MaxCompressionLevel();

            var defaultForegroundColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nAll packaging tests passed.");
            Console.ForegroundColor = defaultForegroundColor;
        }
#endif
    }
}
