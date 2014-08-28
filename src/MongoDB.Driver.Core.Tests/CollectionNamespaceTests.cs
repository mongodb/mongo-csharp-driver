/* Copyright 2013-2014 MongoDB Inc.
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
using NUnit.Framework;

namespace MongoDB.Driver
{
    [TestFixture]
    public class CollectionNamespaceTests
    {
        [Test]
        [TestCase("", false)]
        [TestCase(" ", false)]
        [TestCase(".", true)]
        [TestCase(".ab", true)]
        [TestCase("ab.", true)]
        [TestCase("a.b", true)]
        [TestCase("\0sdf", false)]
        [TestCase("lkjsf\0", false)]
        [TestCase("lkjsf\0d32", false)]
        [TestCase("test", true)]
        [TestCase("foobar", true)]
        public void IsValid_should_return_the_correct_result(string name, bool valid)
        {
            CollectionNamespace.IsValid(name).Should().Be(valid);
        }

        [Test]
        public void FromFullName_should_throw_an_argument_null_exception_if_full_name_is_null()
        {
            Action act = () => CollectionNamespace.FromFullName(null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_an_argument_null_exception_if_database_name_is_null()
        {
            Action act = () => new CollectionNamespace((string)null, "bar\0baz");

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_an_argument_null_exception_if_database_namespace_is_null()
        {
            Action act = () => new CollectionNamespace((DatabaseNamespace)null, "bar\0baz");

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_an_argument_exception_on_an_invalid_database_name()
        {
            Action act = () => new CollectionNamespace("foo", "bar\0baz");

            act.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void FromFullName_should_handle_multiple_periods()
        {
            var subject = CollectionNamespace.FromFullName("test.foo.bar");

            subject.DatabaseNamespace.DatabaseName.Should().Be("test");
            subject.CollectionName.Should().Be("foo.bar");
            subject.FullName.Should().Be("test.foo.bar");
        }

        [Test]
        public void CollectionName_should_report_the_provided_name()
        {
            var subject = new CollectionNamespace("test", "foo");

            subject.CollectionName.Should().Be("foo");
        }

        [Test]
        public void DatabaseName_should_report_the_provided_name()
        {
            var subject = new CollectionNamespace("test", "foo");

            subject.DatabaseNamespace.DatabaseName.Should().Be("test");
        }

        [Test]
        public void FullName_should_report_the_provided_name()
        {
            var subject = new CollectionNamespace("test", "foo");

            subject.FullName.Should().Be("test.foo");
        }

        [Test]
        [TestCase("foo.bar", "foo.bar", true)]
        [TestCase("foo.bar", "foo.baz", false)]
        public void Equals_should_be_correct(string a, string b, bool equals)
        {
            var subject = CollectionNamespace.FromFullName(a);
            var compared = CollectionNamespace.FromFullName(b);

            subject.Equals(compared).Should().Be(equals);
        }

        [Test]
        public void ToString_should_return_the_name()
        {
            var subject = CollectionNamespace.FromFullName("test.foo");

            subject.ToString().Should().Be("test.foo");
        }
    }
}
