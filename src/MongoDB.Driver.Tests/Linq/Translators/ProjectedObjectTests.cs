/* Copyright 2010-2015 MongoDB Inc.
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

using FluentAssertions;
using MongoDB.Driver.Linq.Translators;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Translators
{
    public class ProjectedObjectTests
    {
        [Fact]
        public void Should_retrieve_scalar_value_from_top_level()
        {
            var subject = CreateSubject();

            var a = subject.GetValue<int>("a", 10);

            a.Should().Be(1);
        }

        [Fact]
        public void Should_retrieve_dotted_value_from_top_level()
        {
            var subject = CreateSubject();

            var c = subject.GetValue<int>("b.c", 10);

            c.Should().Be(2);
        }

        [Fact]
        public void Should_retrieve_value_from_nested_level()
        {
            var subject = CreateSubject();

            var g = subject.GetValue<string>("d.e", "not");

            g.Should().Be("funny");
        }

        [Fact]
        public void Should_retrieve_dotted_value_from_nested_level()
        {
            var subject = CreateSubject();

            var g = subject.GetValue<int>("d.f.g", 20);

            g.Should().Be(10);
        }

        [Fact]
        public void Should_retrieve_dotted_value_from_dotted_nested_level()
        {
            var subject = CreateSubject();

            var g = subject.GetValue<int>("h.i.j.k", 10);

            g.Should().Be(30);
        }

        private ProjectedObject CreateSubject()
        {
            var root = new ProjectedObject();
            root.Add("a", 1);
            root.Add("b.c", 2);
            var d = new ProjectedObject();
            root.Add("d", d);
            d.Add("e", "funny");
            d.Add("f.g", 10);

            var h = new ProjectedObject();
            root.Add("h.i", h);
            h.Add("j.k", 30);

            return root;
        }
    }
}
