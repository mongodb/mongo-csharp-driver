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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using FluentAssertions;
using MongoDB.Driver.Configuration;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using Moq;
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
        ],
        [
            "TLS options are configured, but TLS is not enabled",
            new Action<TlsBuilder>(tls => tls.ClientCertificates = [CreateCertificate()])
        ]
    ];

    [Fact]
    public void Constructor_should_initialize_defaults()
    {
        var subject = new TlsBuilder();

        subject.AllowedProtocols.Should().Be(SslStreamSettings.SslProtocolsTls13 | SslProtocols.Tls12);
        subject.AllowInsecure.Should().BeFalse();
        subject.CheckCertificateRevocation.Should().BeFalse();
        subject.ClientCertificates.Should().BeNull();
        subject.ClientCertificateSelectionCallback.Should().BeNull();
        subject.Enabled.Should().BeFalse();
        subject.ServerCertificateValidationCallback.Should().BeNull();
    }

    [Fact]
    public void ClientCertificates_should_be_a_snapshot_of_the_assigned_value()
    {
        var certificates = new List<X509Certificate>();
        var subject = new TlsBuilder { ClientCertificates = certificates };

        certificates.Add(CreateCertificate());

        subject.ClientCertificates.Should().BeEmpty();
    }

    [Fact]
    public void ClientCertificates_should_keep_the_assigned_certificates()
    {
        var certificate = CreateCertificate();
        var subject = new TlsBuilder();

        subject.ClientCertificates = [certificate];

        subject.ClientCertificates.Should().Equal([certificate]);
    }

    [Fact]
    public void ClientCertificates_should_accept_null_after_being_set()
    {
        var subject = new TlsBuilder { ClientCertificates = [CreateCertificate()] };

        subject.ClientCertificates = null;

        subject.ClientCertificates.Should().BeNull();
    }

    [Theory]
    [InlineData("mongodb://host1", false, false)]
    [InlineData("mongodb://host1/?tls=true", true, false)]
    [InlineData("mongodb://host1/?tls=false", false, false)]
    [InlineData("mongodb://host1/?tls=true&tlsInsecure=true", true, true)]
    public void ApplyConnectionString_should_apply_the_options(string connectionString, bool expectedEnabled, bool expectedAllowInsecure)
    {
        var subject = new TlsBuilder();

        subject.ApplyConnectionString(new ConnectionString(connectionString));

        subject.Enabled.Should().Be(expectedEnabled);
        subject.AllowInsecure.Should().Be(expectedAllowInsecure);
    }

    [Fact]
    public void ApplyConnectionString_should_enable_tls_for_the_srv_scheme()
    {
        var subject = new TlsBuilder();
        var resolver = new Mock<IDnsResolver>(MockBehavior.Strict);
        resolver
            .Setup(r => r.ResolveTxtRecords(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns([]);
        var connectionString = new ConnectionString("mongodb+srv://cluster0.example.com", false, resolver.Object)
            .Resolve(resolveHosts: false);

        subject.ApplyConnectionString(connectionString);

        subject.Enabled.Should().BeTrue();
    }

    private static X509Certificate CreateCertificate()
    {
        using var key = ECDsa.Create();
        var request = new CertificateRequest("CN=test", key, HashAlgorithmName.SHA256);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }
}
