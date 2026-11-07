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
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Encryption;
using Xunit;

namespace MongoDB.Driver.Tests.Encryption;

public class KmsTlsOptionsTests
{
    [Fact]
    public void constructor_should_initialize_instance()
    {
        var subject = new KmsTlsOptions();

        subject.AllowedProtocols.Should().Be(SslStreamSettings.SslProtocolsTls13 | SslProtocols.Tls12);
        subject.CheckCertificateRevocation.Should().BeFalse();
        subject.ClientCertificates.Should().BeNull();
    }

    [Fact]
    public void ClientCertificates_should_be_a_snapshot_of_the_assigned_value()
    {
        var certificates = new System.Collections.Generic.List<X509Certificate>();
        var subject = new KmsTlsOptions { ClientCertificates = certificates };

        certificates.Add(CreateCertificate());

        subject.ClientCertificates.Should().BeEmpty();
    }

    [Fact]
    public void ToSslSettings_should_map_all_values()
    {
        var certificate = CreateCertificate();
        var subject = new KmsTlsOptions
        {
            AllowedProtocols = SslProtocols.Tls12,
            CheckCertificateRevocation = true,
            ClientCertificates = new[] { certificate }
        };

        var result = subject.ToSslSettings();

        result.EnabledSslProtocols.Should().Be(SslProtocols.Tls12);
        result.CheckCertificateRevocation.Should().BeTrue();
        result.ClientCertificates.Should().Equal(certificate);
        result.ServerCertificateValidationCallback.Should().BeNull();
    }

    [Fact]
    public void ToSslSettings_should_return_a_new_instance_on_every_call()
    {
        var subject = new KmsTlsOptions();

        subject.ToSslSettings().Should().NotBeSameAs(subject.ToSslSettings());
    }

    [Fact]
    public void Copy_constructor_should_copy_all_properties()
    {
        var certificate = CreateCertificate();
        var subject = new KmsTlsOptions
        {
            AllowedProtocols = SslProtocols.Tls12,
            CheckCertificateRevocation = true,
            ClientCertificates = new[] { certificate }
        };

        var result = new KmsTlsOptions(subject);

        result.AllowedProtocols.Should().Be(SslProtocols.Tls12);
        result.CheckCertificateRevocation.Should().BeTrue();
        result.ClientCertificates.Should().Equal(new[] { certificate }.AsEnumerable());
        typeof(KmsTlsOptions).GetProperties().Should().HaveCount(3, "a new property needs to be copied and mapped by ToSslSettings");
    }

    private static X509Certificate CreateCertificate()
    {
        using var key = ECDsa.Create();
        var request = new CertificateRequest("CN=test", key, HashAlgorithmName.SHA256);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }
}
