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
using Xunit;

namespace MongoDB.Driver.Core.Connections;

public class Socks5AuthenticationSettingsTests
{
    // 3 bytes * 86 = 258 bytes length when UTF8 encoded
    private const string TooLong =
        "€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€€";

    [Fact]
    public void None_should_return_NoAuthenticationSettings_instance()
    {
        var none = Socks5AuthenticationSettings.None;
        none.Should().BeOfType<Socks5AuthenticationSettings.NoAuthenticationSettings>();
    }

    [Fact]
    public void UsernamePassword_should_return_UsernamePasswordAuthenticationSettings_instance_with_correct_values()
    {
        var up = Socks5AuthenticationSettings.UsernamePassword("user", "pass");
        var upcast = up.Should() .BeOfType<Socks5AuthenticationSettings.UsernamePasswordAuthenticationSettings>().Subject;
        upcast.Username.Should().Be("user");
        upcast.Password.Should().Be("pass");
    }

    [Theory]
    [InlineData(null, "pass")]
    [InlineData("user", null)]
    [InlineData("", "pass")]
    [InlineData("user", "")]
    public void UsernamePassword_should_throw_when_username_or_password_is_null_or_empty(string username, string password)
    {
        var ex = Record.Exception(() => Socks5AuthenticationSettings.UsernamePassword(username, password));
        ex.Should().BeAssignableTo<ArgumentException>();
    }

    [Theory]
    [InlineData(TooLong, "pass")]
    [InlineData("user", TooLong)]
    public void UsernamePassword_should_throw_when_username_or_password_are_too_long(string username, string password)
    {
        var ex = Record.Exception(() => Socks5AuthenticationSettings.UsernamePassword(username, password));
        ex.Should().BeAssignableTo<ArgumentException>();
    }

    [Fact]
    public void NoAuthenticationSettings_Equals_and_GetHashCode_should_work_correctly()
    {
        var none = Socks5AuthenticationSettings.None;
        none.Equals(Socks5AuthenticationSettings.None).Should().BeTrue();
        none.GetHashCode().Should().Be(Socks5AuthenticationSettings.None.GetHashCode());

        var up = Socks5AuthenticationSettings.UsernamePassword("a", "b");
        none.Equals(up).Should().BeFalse();
        none.GetHashCode().Should().NotBe(up.GetHashCode());
    }

    [Fact]
    public void UsernamePasswordAuthenticationSettings_Equals_and_GetHashCode_should_work_correctly()
    {
        var up1 = Socks5AuthenticationSettings.UsernamePassword("u", "p");

        var none = Socks5AuthenticationSettings.None;
        up1.Equals(none).Should().BeFalse();
        up1.GetHashCode().Should().NotBe(none.GetHashCode());

        var up2 = Socks5AuthenticationSettings.UsernamePassword("u", "p");
        up1.Equals(up2).Should().BeTrue();
        up1.GetHashCode().Should().Be(up2.GetHashCode());

        var up3 = Socks5AuthenticationSettings.UsernamePassword("u", "x");
        up1.Equals(up3).Should().BeFalse();
        up1.GetHashCode().Should().NotBe(up3.GetHashCode());
    }
}