/* Copyright 2013-2016 MongoDB Inc.
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
using FluentAssertions;
using Xunit;

namespace MongoDB.Driver
{
    public class DatabaseNamespaceTests
    {
        [Theory]
        [InlineData("", false)]
        [InlineData(" ", false)]
        [InlineData(".", false)]
        [InlineData(".ab", false)]
        [InlineData("ab.", false)]
        [InlineData("a.b", false)]
        [InlineData("\0sdf", false)]
        [InlineData("lkjsf\0", false)]
        [InlineData("lkjsf\0d32", false)]
        [InlineData("test", true)]
        [InlineData("foobar", true)]
        public void IsValid_should_return_the_correct_result(string name, bool valid)
        {
            DatabaseNamespace.IsValid(name).Should().Be(valid);
        }

        [Fact]
        public void Constructor_should_throw_an_argument_exception_on_an_invalid_database_name()
        {
            Action act = () => new DatabaseNamespace("foo.bar");

            act.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void Admin_should_be_the_admin_database()
        {
            var subject = DatabaseNamespace.Admin;

            subject.DatabaseName.Should().Be("admin");
        }

        [Fact]
        public void CommandCollection_should_return_the_command_collection()
        {
            var subject = new DatabaseNamespace("test");

            var commandCollection = subject.CommandCollection;
            commandCollection.FullName.Should().Be("test.$cmd");
        }

        [Fact]
        public void DatabaseName_should_report_the_provided_database_name()
        {
            var subject = new DatabaseNamespace("test");

            subject.DatabaseName.Should().Be("test");
        }

        [Fact]
        public void SystemIndexesCollection_should_return_the_system_indexes_collection()
        {
            var subject = new DatabaseNamespace("test");

            var commandCollection = subject.SystemIndexesCollection;
            commandCollection.FullName.Should().Be("test.system.indexes");
        }

        [Fact]
        public void SystemNamespacesCollection_should_return_the_system_namespaces_collection()
        {
            var subject = new DatabaseNamespace("test");

            var commandCollection = subject.SystemNamespacesCollection;
            commandCollection.FullName.Should().Be("test.system.namespaces");
        }

        [Theory]
        [InlineData("one", "one", true)]
        [InlineData("one", "two", false)]
        public void Equals_should_be_correct(string a, string b, bool equals)
        {
            var subject = new DatabaseNamespace(a);
            var compared = new DatabaseNamespace(b);

            subject.Equals(compared).Should().Be(equals);
        }

        [Fact]
        public void ToString_should_return_the_name()
        {
            var subject = new DatabaseNamespace("test");

            subject.ToString().Should().Be("test");
        }
    }
}
