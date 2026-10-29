/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.ObjectModel;

public class BsonSymbolTests
{
    [Fact]
    public void CompareTo_should_order_by_name()
    {
        var subject = new BsonSymbol("abc");

        subject.CompareTo(new BsonSymbol("abc")).Should().Be(0);
        subject.CompareTo(new BsonSymbol("xyz")).Should().BeNegative();
        subject.CompareTo(new BsonSymbol("aaa")).Should().BePositive();
    }

    [Fact]
    public void CompareTo_should_return_zero_for_a_BsonString_with_the_same_value()
    {
        var subject = new BsonSymbol("abc");

        subject.CompareTo(new BsonString("abc")).Should().Be(0);
    }

    [Fact]
    public void Constructor_should_throw_when_name_is_null()
    {
        var exception = Record.Exception(() => new BsonSymbol(null));

        exception.Should().BeOfType<ArgumentNullException>()
            .Subject.ParamName.Should().Be("name");
    }

    [Fact]
    public void Deserialization_should_not_share_symbol_instances_between_documents()
    {
        var bytes = new BsonDocument("x", new BsonSymbol("abc")).ToBson();

        var first = BsonSerializer.Deserialize<BsonDocument>(bytes)["x"];
        var second = BsonSerializer.Deserialize<BsonDocument>(bytes)["x"];

        first.Should().BeOfType<BsonSymbol>();
        first.Should().Be(second);
        first.Should().NotBeSameAs(second);
    }

    [Fact]
    public void Documents_containing_symbols_should_compare_equal_across_a_round_trip()
    {
        var document = new BsonDocument("x", new BsonSymbol("abc"));

        var rehydrated = BsonSerializer.Deserialize<BsonDocument>(document.ToBson());

        rehydrated.Should().Be(document);
    }

    [Fact]
    public void Equals_should_return_false_for_a_BsonString_with_the_same_value()
    {
        var subject = new BsonSymbol("abc");

        subject.Equals(new BsonString("abc")).Should().BeFalse();
    }

    [Fact]
    public void Equals_should_return_false_for_symbols_with_different_names()
    {
        var subject = new BsonSymbol("abc");
        var other = new BsonSymbol("xyz");

        subject.Equals(other).Should().BeFalse();
    }

    [Fact]
    public void Equals_should_return_false_when_other_is_null()
    {
        var subject = new BsonSymbol("abc");

        subject.Equals((BsonSymbol)null).Should().BeFalse();
        subject.Equals((object)null).Should().BeFalse();
        (subject == null).Should().BeFalse();
    }

    [Fact]
    public void Equals_should_return_true_for_two_symbols_with_the_same_name()
    {
        var subject = new BsonSymbol("abc");
        var other = new BsonSymbol("abc");

        subject.Equals(other).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_should_return_the_same_value_for_symbols_with_the_same_name()
    {
        var subject = new BsonSymbol("abc");
        var other = new BsonSymbol("abc");

        subject.GetHashCode().Should().Be(other.GetHashCode());
    }

    [Fact]
    public void Implicit_conversion_from_string_should_return_a_new_instance()
    {
        BsonSymbol first = "abc";
        BsonSymbol second = "abc";

        first.Name.Should().Be("abc");
        second.Should().Be(first);
        second.Should().NotBeSameAs(first);
    }

    [Fact]
    public void Operator_equals_should_return_true_for_two_symbols_with_the_same_name()
    {
        var subject = new BsonSymbol("abc");
        var other = new BsonSymbol("abc");

        (subject == other).Should().BeTrue();
        (subject != other).Should().BeFalse();
    }
}
