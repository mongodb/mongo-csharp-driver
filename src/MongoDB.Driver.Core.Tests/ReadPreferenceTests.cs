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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace MongoDB.Driver
{
    [TestFixture]
    public class ReadPreferenceTests
    {
        [Test]
        public void Constructor_should_assume_empty_tagSets_when_tagSets_parameter_is_missing()
        {
            var readPreference = new ReadPreference(ReadPreferenceMode.Primary);
            readPreference.TagSets.Count.Should().Be(0);
        }

        [Test]
        public void Constructor_should_copy_tagSets_list()
        {
            var tagSets = new[] { new TagSet(new[] { new Tag("name", "value") }) };
            var readPreference = new ReadPreference(ReadPreferenceMode.Secondary, tagSets);
            readPreference.TagSets.Should().NotBeSameAs(tagSets);
        }

        [Test]
        public void Constructor_should_set_mode_correctly()
        {
            var readPreference = new ReadPreference(ReadPreferenceMode.Secondary); // use a value that is not the default
            readPreference.ReadPreferenceMode.Should().Be(ReadPreferenceMode.Secondary);
        }

        [Test]
        public void Constructor_should_set_tagSets_correctly()
        {
            var tagSets = new[] { new TagSet(new[] { new Tag("name1", "value1") }), new TagSet(new[] { new Tag("name2", "value2") }) };
            var readPreference = new ReadPreference(ReadPreferenceMode.Secondary, tagSets);
            readPreference.TagSets.Should().Equal(tagSets);
        }

        [Test]
        public void Constructor_should_throw_when_mode_is_primary_and_tagSets_is_not_empty()
        {
            var tagSets = new[] { new TagSet(new[] { new Tag("name1", "value1") }), new TagSet(new[] { new Tag("name2", "value2") }) };
            Action action = () => new ReadPreference(ReadPreferenceMode.Primary, tagSets);
            action.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void Constructor_should_throw_when_tagSets_is_null()
        {
            Action action = () => new ReadPreference(ReadPreferenceMode.Primary, null);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Equals_should_be_correct()
        {
            var a = ReadPreference.Primary;
            var b = ReadPreference.Primary;
            var c = ReadPreference.Secondary;
            var d = new ReadPreference(ReadPreferenceMode.Secondary, new[] { new TagSet(new[] { new Tag("dc", "ny"), new Tag("dc", "ca") }) });
            var e = new ReadPreference(ReadPreferenceMode.Secondary, new[] { new TagSet(new[] { new Tag("dc", "ny"), new Tag("dc", "ca") }) });
            var f = new ReadPreference(ReadPreferenceMode.Secondary, new[] { new TagSet(new[] { new Tag("dc", "ny"), new Tag("dc", "ac") }) });
            var g = new ReadPreference(ReadPreferenceMode.PrimaryPreferred, new[] { new TagSet(new[] { new Tag("dc", "ny"), new Tag("dc", "ac") }) });

            a.Should().Be(b);
            b.Should().NotBe(c);
            c.Should().NotBe(d);
            d.Should().Be(e);
            e.Should().NotBe(f);
            f.Should().NotBe(g);
        }

        [Test]
        public void Nearest_should_return_a_correctly_initialized_read_preference()
        {
            var readPreference = ReadPreference.Nearest;
            readPreference.ReadPreferenceMode.Should().Be(ReadPreferenceMode.Nearest);
            readPreference.TagSets.Count.Should().Be(0);
        }

        [Test]
        public void Primary_should_return_a_correctly_initialized_read_preference()
        {
            var readPreference = ReadPreference.Primary;
            readPreference.ReadPreferenceMode.Should().Be(ReadPreferenceMode.Primary);
            readPreference.TagSets.Count.Should().Be(0);
        }

        [Test]
        public void PrimaryPreferred_should_return_a_correctly_initialized_read_preference()
        {
            var readPreference = ReadPreference.PrimaryPreferred;
            readPreference.ReadPreferenceMode.Should().Be(ReadPreferenceMode.PrimaryPreferred);
            readPreference.TagSets.Count.Should().Be(0);
        }

        [Test]
        public void Secondary_should_return_a_correctly_initialized_read_preference()
        {
            var readPreference = ReadPreference.Secondary;
            readPreference.ReadPreferenceMode.Should().Be(ReadPreferenceMode.Secondary);
            readPreference.TagSets.Count.Should().Be(0);
        }

        [Test]
        public void SecondaryPreferred_should_return_a_correctly_initialized_read_preference()
        {
            var readPreference = ReadPreference.SecondaryPreferred;
            readPreference.ReadPreferenceMode.Should().Be(ReadPreferenceMode.SecondaryPreferred);
            readPreference.TagSets.Count.Should().Be(0);
        }

        [Test]
        public void WithMode_should_return_new_instance_when_the_value_is_not_the_same()
        {
            var readPreference1 = new ReadPreference(ReadPreferenceMode.Secondary);
            var readPreference2 = readPreference1.WithMode(ReadPreferenceMode.SecondaryPreferred);
            readPreference2.Should().NotBeSameAs(readPreference1);
            readPreference2.ReadPreferenceMode.Should().Be(ReadPreferenceMode.SecondaryPreferred);
        }

        [Test]
        public void WithMode_should_return_same_instance_when_the_value_is_the_same()
        {
            var readPreference1 = new ReadPreference(ReadPreferenceMode.Secondary);
            var readPreference2 = readPreference1.WithMode(ReadPreferenceMode.Secondary);
            readPreference2.Should().BeSameAs(readPreference1);
        }

        [Test]
        public void WithTagSets_should_return_new_instance_when_the_value_is_not_the_same()
        {
            var tagSets1 = new[] { new TagSet(new[] { new Tag("name1", "value1") }) };
            var tagSets2 = new[] { new TagSet(new[] { new Tag("name2", "value2") }) };
            var readPreference1 = new ReadPreference(ReadPreferenceMode.Secondary, tagSets1);
            var readPreference2 = readPreference1.WithTagSets(tagSets2);
            readPreference2.Should().NotBeSameAs(readPreference1);
            readPreference2.TagSets.Should().Equal(tagSets2);
        }

        [Test]
        public void WithTagSets_should_return_same_instance_when_the_value_is_the_same()
        {
            var tagSets1 = new[] { new TagSet(new[] { new Tag("name", "value") }) };
            var tagSets2 = new[] { new TagSet(new[] { new Tag("name", "value") }) };
            var readPreference1 = new ReadPreference(ReadPreferenceMode.Secondary, tagSets1);
            var readPreference2 = readPreference1.WithTagSets(tagSets2);
            readPreference2.Should().BeSameAs(readPreference1);
        }
    }
}
