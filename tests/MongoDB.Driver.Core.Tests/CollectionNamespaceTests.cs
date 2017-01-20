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
    public class CollectionNamespaceTests
    {
        [Theory]
        [InlineData("", false)]
        [InlineData(" ", false)]
        [InlineData(".", true)]
        [InlineData(".ab", true)]
        [InlineData("ab.", true)]
        [InlineData("a.b", true)]
        [InlineData("\0sdf", false)]
        [InlineData("lkjsf\0", false)]
        [InlineData("lkjsf\0d32", false)]
        [InlineData("test", true)]
        [InlineData("foobar", true)]
        public void IsValid_should_return_the_correct_result(string name, bool valid)
        {
            CollectionNamespace.IsValid(name).Should().Be(valid);
        }

        [Fact]
        public void FromFullName_should_throw_an_argument_null_exception_if_full_name_is_null()
        {
            Action act = () => CollectionNamespace.FromFullName(null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_an_argument_null_exception_if_database_name_is_null()
        {
            Action act = () => new CollectionNamespace((string)null, "bar\0baz");

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_an_argument_null_exception_if_database_namespace_is_null()
        {
            Action act = () => new CollectionNamespace((DatabaseNamespace)null, "bar\0baz");

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_an_argument_exception_on_an_invalid_database_name()
        {
            Action act = () => new CollectionNamespace("foo", "bar\0baz");

            act.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void FromFullName_should_handle_multiple_periods()
        {
            var subject = CollectionNamespace.FromFullName("test.foo.bar");

            subject.DatabaseNamespace.DatabaseName.Should().Be("test");
            subject.CollectionName.Should().Be("foo.bar");
            subject.FullName.Should().Be("test.foo.bar");
        }

        [Fact]
        public void CollectionName_should_report_the_provided_name()
        {
            var subject = new CollectionNamespace("test", "foo");

            subject.CollectionName.Should().Be("foo");
        }

        [Fact]
        public void DatabaseName_should_report_the_provided_name()
        {
            var subject = new CollectionNamespace("test", "foo");

            subject.DatabaseNamespace.DatabaseName.Should().Be("test");
        }

        [Fact]
        public void FullName_should_report_the_provided_name()
        {
            var subject = new CollectionNamespace("test", "foo");

            subject.FullName.Should().Be("test.foo");
        }

        [Theory]
        [InlineData("foo.bar", "foo.bar", true)]
        [InlineData("foo.bar", "foo.baz", false)]
        public void Equals_should_be_correct(string a, string b, bool equals)
        {
            var subject = CollectionNamespace.FromFullName(a);
            var compared = CollectionNamespace.FromFullName(b);

            subject.Equals(compared).Should().Be(equals);
        }

        [Fact]
        public void ToString_should_return_the_name()
        {
            var subject = CollectionNamespace.FromFullName("test.foo");

            subject.ToString().Should().Be("test.foo");
        }
    }
}
