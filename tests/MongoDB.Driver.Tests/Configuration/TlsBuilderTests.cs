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
using System.Collections.Generic;
using System.Security.Authentication;
using FluentAssertions;
using MongoDB.Driver.Configuration;
using Xunit;

namespace MongoDB.Driver.Tests.Configuration;

public class TlsBuilderTests
{
    [Theory]
    [MemberData(nameof(ValidConfigurations))]
    public void Validate_should_accept_valid_configuration(string description, Action<TlsBuilder> configure)
    {
        var subject = new TlsBuilder();
        configure(subject);

        var exception = Record.Exception(() => subject.Validate());

        exception.Should().BeNull(because: description);
    }

    public static IEnumerable<object[]> ValidConfigurations =>
    [
        ["defaults", new Action<TlsBuilder>(_ => { })],
        ["enabled", new Action<TlsBuilder>(tls => tls.Enabled = true)],
        ["allow insecure", new Action<TlsBuilder>(tls => { tls.Enabled = true; tls.AllowInsecure = true; })],
        ["check revocation", new Action<TlsBuilder>(tls => { tls.Enabled = true; tls.CheckCertificateRevocation = true; })],
        ["allowed protocols", new Action<TlsBuilder>(tls => { tls.Enabled = true; tls.AllowedProtocols = SslProtocols.Tls12; })],
        [
            "validation callback",
            new Action<TlsBuilder>(tls => { tls.Enabled = true; tls.ServerCertificateValidationCallback = (_, _, _, _) => true; })
        ]
    ];

    [Theory]
    [MemberData(nameof(InvalidConfigurations))]
    public void Validate_should_throw_for_invalid_configuration(string expectedMessage, Action<TlsBuilder> configure)
    {
        var subject = new TlsBuilder();
        configure(subject);

        var exception = Record.Exception(() => subject.Validate());

        exception.Should().BeOfType<InvalidOperationException>();
        exception.Message.Should().Contain(expectedMessage);
    }

    public static IEnumerable<object[]> InvalidConfigurations =>
    [
        [
            "AllowInsecure and CheckCertificateRevocation cannot both be true",
            new Action<TlsBuilder>(tls => { tls.Enabled = true; tls.AllowInsecure = true; tls.CheckCertificateRevocation = true; })
        ],
        [
            "AllowInsecure cannot be used together with ServerCertificateValidationCallback",
            new Action<TlsBuilder>(tls =>
            {
                tls.Enabled = true;
                tls.AllowInsecure = true;
                tls.ServerCertificateValidationCallback = (_, _, _, _) => true;
            })
        ],
        [
            "TLS options are configured, but TLS is not enabled",
            new Action<TlsBuilder>(tls => tls.AllowInsecure = true)
        ],
        [
            "TLS options are configured, but TLS is not enabled",
            new Action<TlsBuilder>(tls => tls.AllowedProtocols = SslProtocols.Tls12)
        ],
        [
            "TLS options are configured, but TLS is not enabled",
            new Action<TlsBuilder>(tls => tls.CheckCertificateRevocation = true)
        ],
        [
            "TLS options are configured, but TLS is not enabled",
            new Action<TlsBuilder>(tls => tls.ClientCertificateSelectionCallback = (_, _, _, _, _) => null)
        ],
        [
            "TLS options are configured, but TLS is not enabled",
            new Action<TlsBuilder>(tls => tls.ServerCertificateValidationCallback = (_, _, _, _) => true)
        ]
    ];
}
