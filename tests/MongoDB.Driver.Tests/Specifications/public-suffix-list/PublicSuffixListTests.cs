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

using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.public_suffix_list;

public class PublicSuffixListTests
{
    // https://github.com/mongodb/specifications/blob/master/source/public-suffix-list/tests/README.md#1-a-multi-label-ordinary-rule
    [Theory]
    [InlineData("com.ac", true)]
    [InlineData("foo.com.ac", false)]
    public void Multi_label_ordinary_rule(string domain, bool expectedResult)
    {
        var result = PublicSuffixList.IsPublicSuffix(domain);

        result.Should().Be(expectedResult);
    }

    // https://github.com/mongodb/specifications/blob/master/source/public-suffix-list/tests/README.md#2-a-shorter-wildcard-rule
    [Theory]
    [InlineData("b.ck", true)]
    [InlineData("a.b.ck", false)]
    public void Shorter_wildcard_rule(string domain, bool expectedResult)
    {
        var result = PublicSuffixList.IsPublicSuffix(domain);

        result.Should().Be(expectedResult);
    }

    // https://github.com/mongodb/specifications/blob/master/source/public-suffix-list/tests/README.md#3-a-longer-wildcard-rule
    // the shorter "br" rule must not win over "*.nom.br"
    [Theory]
    [InlineData("abc.nom.br", true)]
    [InlineData("x.abc.nom.br", false)]
    public void Longer_wildcard_rule(string domain, bool expectedResult)
    {
        var result = PublicSuffixList.IsPublicSuffix(domain);

        result.Should().Be(expectedResult);
    }

    // https://github.com/mongodb/specifications/blob/master/source/public-suffix-list/tests/README.md#4-an-exception-rule
    // "!www.ck" overrides "*.ck"
    [Theory]
    [InlineData("ck", true)]
    [InlineData("www.ck", false)]
    public void Exception_rule(string domain, bool expectedResult)
    {
        var result = PublicSuffixList.IsPublicSuffix(domain);

        result.Should().Be(expectedResult);
    }

    // https://github.com/mongodb/specifications/blob/master/source/public-suffix-list/tests/README.md#5-no-rule-matches
    [Theory]
    [InlineData("nosuchtld", true)]
    [InlineData("foo.nosuchtld", false)]
    public void No_rule_matches(string domain, bool expectedResult)
    {
        var result = PublicSuffixList.IsPublicSuffix(domain);

        result.Should().Be(expectedResult);
    }

    // https://github.com/mongodb/specifications/blob/master/source/public-suffix-list/tests/README.md#6-an-internationalized-rule
    // the list stores internationalized rules as Unicode while the domains compared against them
    // are Punycode, so the two must be brought into the same form before comparing. The last two
    // cases go beyond the three the prose test asserts: one completes the true/false pair for
    // "公司.cn", the other covers a third rule.
    [Theory]
    [InlineData("xn--p1ai", true)]           // рф
    [InlineData("example.xn--p1ai", false)]  // example.рф
    [InlineData("xn--55qx5d.cn", true)]      // 公司.cn
    [InlineData("example.xn--55qx5d.cn", false)]
    [InlineData("xn--aroport-bya.ci", true)] // aéroport.ci
    public void Internationalized_rule_matches_punycode_domain(string domain, bool expectedResult)
    {
        var result = PublicSuffixList.IsPublicSuffix(domain);

        result.Should().Be(expectedResult);
    }
}
