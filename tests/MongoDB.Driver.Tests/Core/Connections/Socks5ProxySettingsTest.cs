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

public class Socks5ProxySettingsTest
{
    [Fact]
    public void Constructor_should_set_properties_correctly_with_host_only()
    {
        var settings = new Socks5ProxySettings("localhost");
        settings.Host.Should().Be("localhost");
        settings.Port.Should().Be(1080);
        settings.Authentication.Should().Be(Socks5AuthenticationSettings.None);
    }

    [Fact]
    public void Constructor_should_set_properties_correctly_with_host_and_port()
    {
        var settings = new Socks5ProxySettings("localhost", 1234);
        settings.Host.Should().Be("localhost");
        settings.Port.Should().Be(1234);
        settings.Authentication.Should().Be(Socks5AuthenticationSettings.None);
    }

    [Fact]
    public void Constructor_should_set_properties_correctly_with_host_and_authentication()
    {
        var auth = Socks5AuthenticationSettings.UsernamePassword("user", "pass");
        var settings = new Socks5ProxySettings("localhost", auth);
        settings.Host.Should().Be("localhost");
        settings.Port.Should().Be(1080);
        settings.Authentication.Should().Be(auth);
    }

    [Fact]
    public void Constructor_should_set_properties_correctly_with_host_port_and_authentication()
    {
        var auth = Socks5AuthenticationSettings.UsernamePassword("user", "pass");
        var settings = new Socks5ProxySettings("localhost", 1234, auth);
        settings.Host.Should().Be("localhost");
        settings.Port.Should().Be(1234);
        settings.Authentication.Should().Be(auth);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_should_throw_when_host_is_null_or_empty(string host)
    {
        var ex = Record.Exception(() => new Socks5ProxySettings(host));
        ex.Should().BeAssignableTo<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65536)]
    public void Constructor_should_throw_when_port_is_out_of_range(int port)
    {
        var ex = Record.Exception(() => new Socks5ProxySettings("localhost", port));
        ex.Should().BeOfType<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_should_throw_when_authentication_is_null()
    {
        var ex = Record.Exception(() => new Socks5ProxySettings("localhost", 1080, null));
        ex.Should().BeOfType<ArgumentNullException>();
    }

    [Fact]
    public void Equals_and_GetHashCode_should_work_for_Socks5ProxySettings()
    {
        var auth1 = Socks5AuthenticationSettings.UsernamePassword("user", "pass");
        var auth2 = Socks5AuthenticationSettings.UsernamePassword("user", "pass");
        var s1 = new Socks5ProxySettings("host", 1234, auth1);
        var s2 = new Socks5ProxySettings("host", 1234, auth2);
        s1.Equals(s2).Should().BeTrue();
        s1.GetHashCode().Should().Be(s2.GetHashCode());
    }

    [Fact]
    public void ToString_should_return_expected_string_for_no_auth()
    {
        var s = new Socks5ProxySettings("host");
        var expected = "{ Host : host, Port : 1080, Authentication : None }";
        s.ToString().Should().Be(expected);
    }

    [Fact]
    public void ToString_should_return_expected_string_for_username_password_auth()
    {
        var s = new Socks5ProxySettings("host", 1234, Socks5AuthenticationSettings.UsernamePassword("u", "p"));
        var expected = "{ Host : host, Port : 1234, Authentication : UsernamePassword }";
        s.ToString().Should().Be(expected);
    }

    [Fact]
    public void Create_should_return_expected_settings()
    {
        var s = Socks5ProxySettings.Create("host", 1234, "u", "p");
        s.Host.Should().Be("host");
        s.Port.Should().Be(1234);
        s.Authentication.Should().BeOfType<Socks5AuthenticationSettings.UsernamePasswordAuthenticationSettings>();
        var up = (Socks5AuthenticationSettings.UsernamePasswordAuthenticationSettings)s.Authentication;
        up.Username.Should().Be("u");
        up.Password.Should().Be("p");
    }

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