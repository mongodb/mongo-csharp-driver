﻿/* Copyright 2018-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Authentication;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Configuration
{
    public class ClusterBuilderTests
    {
        [Theory]
        [InlineData(0, 0, 30000, 30000)]
        [InlineData(-1, -1, 30000, 30000)]
        [InlineData(20000, 0, 20000, 20000)]
        [InlineData(20000, -1, 20000, 20000)]
        [InlineData(20000, 10000, 20000, 10000)]
        public void CreateServerMonitorFactory_should_return_expected_result(int connectTimeoutMilliseconds, int heartbeatTimeoutMilliseconds, int expectedServerMonitorConnectTimeoutMilliseconds, int expectedServerMonitorSocketTimeoutMilliseconds)
        {
            var connectTimeout = TimeSpan.FromMilliseconds(connectTimeoutMilliseconds);
            var authenticatorFactory = new AuthenticatorFactory(() => Mock.Of<IAuthenticator>());
            var heartbeatTimeout = TimeSpan.FromMilliseconds(heartbeatTimeoutMilliseconds);
            var serverMonitoringMode = ServerMonitoringMode.Stream;
            var expectedServerMonitorConnectTimeout = TimeSpan.FromMilliseconds(expectedServerMonitorConnectTimeoutMilliseconds);
            var expectedServerMonitorSocketTimeout = TimeSpan.FromMilliseconds(expectedServerMonitorSocketTimeoutMilliseconds);
            var subject = new ClusterBuilder()
                .ConfigureTcp(s => s.With(connectTimeout: connectTimeout))
                .ConfigureConnection(s => s.WithInternal(authenticatorFactory: authenticatorFactory))
                .ConfigureServer(s => s.With(heartbeatTimeout: heartbeatTimeout, serverMonitoringMode: serverMonitoringMode));

            var result = (ServerMonitorFactory)subject.CreateServerMonitorFactory();

            var serverMonitorConnectionFactory = (BinaryConnectionFactory)result._connectionFactory();
            var serverMonitorConnectionSettings = serverMonitorConnectionFactory._settings();
            serverMonitorConnectionSettings.AuthenticatorFactory.Should().BeNull();

            var serverMonitorStreamFactory = (TcpStreamFactory)serverMonitorConnectionFactory._streamFactory();
            var serverMonitorTcpStreamSettings = serverMonitorStreamFactory._settings();
            serverMonitorTcpStreamSettings.ConnectTimeout.Should().Be(expectedServerMonitorConnectTimeout);
            serverMonitorTcpStreamSettings.ReadTimeout.Should().Be(null);
            serverMonitorTcpStreamSettings.WriteTimeout.Should().Be(null);

            var serverSettings = result._serverMonitorSettings();
            serverSettings.ServerMonitoringMode.Should().Be(ServerMonitoringMode.Stream);
            serverSettings.ConnectTimeout.Should().Be(expectedServerMonitorConnectTimeout);
            serverSettings.HeartbeatTimeout.Should().Be(expectedServerMonitorSocketTimeout);
        }

        [Fact]
        public void StreamFactoryWrappers_should_be_called_in_the_correct_order()
        {
            var subject = new ClusterBuilder();
            var calls = new List<int>();
            subject.RegisterStreamFactory(factory => { calls.Add(1); return factory; });
            subject.RegisterStreamFactory(factory => { calls.Add(2); return factory; });
            subject.RegisterStreamFactory(factory => { calls.Add(3); return factory; });

            subject._streamFactoryWrapper()(Mock.Of<IStreamFactory>());

            calls.Should().Equal(1, 2, 3);
        }
    }

    public static class ClusterBuilderReflector
    {
        internal static IServerMonitorFactory CreateServerMonitorFactory(this ClusterBuilder obj) => (IServerMonitorFactory)Reflector.Invoke(obj, nameof(CreateServerMonitorFactory));

        internal static Func<IStreamFactory, IStreamFactory> _streamFactoryWrapper(this ClusterBuilder obj) => (Func<IStreamFactory, IStreamFactory>)Reflector.GetFieldValue(obj, nameof(_streamFactoryWrapper));
    }
}
