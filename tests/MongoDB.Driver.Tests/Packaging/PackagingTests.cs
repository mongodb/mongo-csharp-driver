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
    [Trait("Category", "CSFLE")]
    public class PackagingTests
    {
        // keep these tests in sync with CONSOLE_TEST's Main method
        [Fact]
        public static void Libmongocrypt_library_should_provide_library_version()
        {
            var version = Library.Version;

            version.Should().Be("1.7.0");
        }

#pragma warning disable IDE0051 // Remove unused private members
        private static void RunAllTests()
        {
            // Left it outside Main method to protect us from losing sync between xunit and console modes
            Libmongocrypt_library_should_provide_library_version();
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
    }
}
