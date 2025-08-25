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
    [Fact]
    public void Socks5AuthenticationSettings_None_should_return_NoAuthenticationSettings_instance()
    {
        var none = Socks5AuthenticationSettings.None;
        none.Should().BeOfType<Socks5AuthenticationSettings.NoAuthenticationSettings>();
    }

    [Fact]
    public void Socks5AuthenticationSettings_UsernamePassword_should_return_UsernamePasswordAuthenticationSettings_instance_with_correct_values()
    {
        var up = Socks5AuthenticationSettings.UsernamePassword("user", "pass");
        up.Should().BeOfType<Socks5AuthenticationSettings.UsernamePasswordAuthenticationSettings>();
        var upcast = (Socks5AuthenticationSettings.UsernamePasswordAuthenticationSettings)up;
        upcast.Username.Should().Be("user");
        upcast.Password.Should().Be("pass");
    }

    [Theory]
    [InlineData(null, "pass")]
    [InlineData("user", null)]
    [InlineData("", "pass")]
    [InlineData("user", "")]
    public void Socks5AuthenticationSettings_UsernamePassword_should_throw_when_username_or_password_is_null_or_empty(string username, string password)
    {
        var ex = Record.Exception(() => Socks5AuthenticationSettings.UsernamePassword(username, password));
        ex.Should().BeAssignableTo<ArgumentException>();
    }

    [Fact]
    public void Socks5AuthenticationSettings_NoAuthenticationSettings_Equals_should_return_true_for_any_Socks5AuthenticationSettings()
    {
        var none = Socks5AuthenticationSettings.None;
        none.Equals(Socks5AuthenticationSettings.None).Should().BeTrue();
        none.Equals(Socks5AuthenticationSettings.UsernamePassword("a", "b")).Should().BeFalse();
    }

    [Fact]
    public void Socks5AuthenticationSettings_UsernamePasswordAuthenticationSettings_Equals_and_GetHashCode_should_work()
    {
        var a = Socks5AuthenticationSettings.UsernamePassword("u", "p");
        var b = Socks5AuthenticationSettings.UsernamePassword("u", "p");
        a.Equals(b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
        var c = Socks5AuthenticationSettings.UsernamePassword("u", "x");
        a.Equals(c).Should().BeFalse();
    }
}