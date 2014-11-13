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
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using MongoDB.Driver.Core.Configuration;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Configuration
{
    [TestFixture]
    public class SslStreamSettingsTests
    {
        [Test]
        public void Constructor_initializes_instance()
        {
            var subject = new SslStreamSettings();
            subject.ClientCertificates.Should().BeEmpty();
            subject.CheckCertificateRevocation.Should().Be(true);
            subject.ClientCertificateSelectionCallback.Should().BeNull();
            subject.EnabledSslProtocols.Should().Be(SslProtocols.Default);
            subject.ServerCertificateValidationCallback.Should().BeNull();
        }

        [Test]
        public void With_returns_a_new_instance()
        {
            var subject1 = new SslStreamSettings();
            var subject2 = subject1.With(checkCertificateRevocation: false);
            subject2.Should().NotBeSameAs(subject1);
            subject1.CheckCertificateRevocation.Should().BeTrue();
            subject2.CheckCertificateRevocation.Should().BeFalse();
        }
    }
}