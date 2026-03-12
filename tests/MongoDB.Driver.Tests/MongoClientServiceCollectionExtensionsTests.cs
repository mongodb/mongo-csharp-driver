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
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests;

public class MongoClientServiceCollectionExtensionsTests
{
    [Theory]
    [InlineData(ServiceLifetime.Singleton, MethodOverload.ConnectionString)]
    [InlineData(ServiceLifetime.Scoped, MethodOverload.ConnectionString)]
    [InlineData(ServiceLifetime.Transient, MethodOverload.ConnectionString)]
    [InlineData(ServiceLifetime.Singleton, MethodOverload.Url)]
    [InlineData(ServiceLifetime.Scoped, MethodOverload.Url)]
    [InlineData(ServiceLifetime.Transient, MethodOverload.Url)]
    [InlineData(ServiceLifetime.Singleton, MethodOverload.MongoClientSettings)]
    [InlineData(ServiceLifetime.Scoped, MethodOverload.MongoClientSettings)]
    [InlineData(ServiceLifetime.Transient, MethodOverload.MongoClientSettings)]
    [InlineData(ServiceLifetime.Singleton, MethodOverload.ConnectionStringAndConfigAction)]
    [InlineData(ServiceLifetime.Scoped, MethodOverload.ConnectionStringAndConfigAction)]
    [InlineData(ServiceLifetime.Transient, MethodOverload.ConnectionStringAndConfigAction)]
    [InlineData(ServiceLifetime.Singleton, MethodOverload.UrlAndConfigAction)]
    [InlineData(ServiceLifetime.Scoped, MethodOverload.UrlAndConfigAction)]
    [InlineData(ServiceLifetime.Transient, MethodOverload.UrlAndConfigAction)]
    [InlineData(ServiceLifetime.Singleton, MethodOverload.ClientSettingsAndConfigAction)]
    [InlineData(ServiceLifetime.Scoped, MethodOverload.ClientSettingsAndConfigAction)]
    [InlineData(ServiceLifetime.Transient, MethodOverload.ClientSettingsAndConfigAction)]
    [InlineData(ServiceLifetime.Singleton, MethodOverload.Everything)]
    [InlineData(ServiceLifetime.Scoped, MethodOverload.Everything)]
    [InlineData(ServiceLifetime.Transient, MethodOverload.Everything)]
    public void AddMongoClient_should_register_with_lifetime(ServiceLifetime lifetime, MethodOverload overload)
    {
        var services = new ServiceCollection();

        CallAddMongoClientOverload(overload, services, lifetime: lifetime);

        var descriptor = services.Single(d => d.ServiceType == typeof(IMongoClient));
        descriptor.Lifetime.Should().Be(lifetime);

        using var scope = services.BuildServiceProvider().CreateScope();

        var client = scope.ServiceProvider.GetService<IMongoClient>();
        client.Should().NotBeNull();
        client.Should().BeOfType<MongoClient>();

        var client2 = scope.ServiceProvider.GetService<IMongoClient>();

        if (lifetime == ServiceLifetime.Singleton
            || lifetime == ServiceLifetime.Scoped)
        {
            client.Should().BeSameAs(client2);
        }
        else
        {
            client.Should().NotBeSameAs(client2);
        }
    }

    [Theory]
    [InlineData(MethodOverload.ConnectionString)]
    [InlineData(MethodOverload.Url)]
    [InlineData(MethodOverload.MongoClientSettings)]
    [InlineData(MethodOverload.ConnectionStringAndConfigAction)]
    [InlineData(MethodOverload.UrlAndConfigAction)]
    [InlineData(MethodOverload.ClientSettingsAndConfigAction)]
    [InlineData(MethodOverload.Everything)]
    public void AddMongoClient_should_throw_when_services_is_null(MethodOverload overload)
        => Record.Exception(() => CallAddMongoClientOverload(overload, null))
            .Should().BeOfType<ArgumentNullException>();

    [Theory]
    [InlineData(MethodOverload.ConnectionString)]
    [InlineData(MethodOverload.ConnectionStringAndConfigAction)]
    public void AddMongoClient_should_throw_when_connection_string_is_null(MethodOverload overload)
        => Record.Exception(() => CallAddMongoClientOverload(overload, new ServiceCollection(), nullConnectionString: true))
            .Should().BeOfType<ArgumentNullException>();

    [Theory]
    [InlineData(MethodOverload.Url)]
    [InlineData(MethodOverload.UrlAndConfigAction)]
    public void AddMongoClient_should_throw_when_url_is_null(MethodOverload overload)
        => Record.Exception(() => CallAddMongoClientOverload(overload, new ServiceCollection(), nullUrl: true))
            .Should().BeOfType<ArgumentNullException>();

    [Theory]
    [InlineData(MethodOverload.MongoClientSettings)]
    [InlineData(MethodOverload.ClientSettingsAndConfigAction)]
    [InlineData(MethodOverload.Everything)]
    public void AddMongoClient_should_throw_when_client_settings_is_null(MethodOverload overload)
        => Record.Exception(() => CallAddMongoClientOverload(overload, new ServiceCollection(), nullClientSettings: true))
            .Should().BeOfType<ArgumentNullException>();


    [Theory]
    [InlineData(MethodOverload.ConnectionStringAndConfigAction)]
    [InlineData(MethodOverload.UrlAndConfigAction)]
    [InlineData(MethodOverload.ClientSettingsAndConfigAction)]
    [InlineData(MethodOverload.Everything)]
    public void AddMongoClient_calls_configuration_delegate(MethodOverload overload)
    {
        var services = new ServiceCollection();
        var client = new MongoClient("mongodb://localhost");

        var actionCalled = false;

        CallAddMongoClientOverload(overload, services, clientFactory: (sp, key, s) =>
        {
            sp.Should().NotBeNull();
            key.Should().BeNull();
            actionCalled = true;
            return client;
        });

        actionCalled.Should().BeFalse();

        var resolvedClient = services.BuildServiceProvider().GetService<IMongoClient>();

        actionCalled.Should().BeTrue();
        resolvedClient.Should().BeSameAs(client);
    }

    [Fact]
    public void AddMongoClient_generic_can_register_MongoClient_directly()
    {
        var services = new ServiceCollection();

        services.AddMongoClient<MongoClient>(new MongoClientSettings(), null);

        var client = services.BuildServiceProvider().GetService<MongoClient>();

        client.Should().NotBeNull();
        client.Should().BeOfType<MongoClient>();
    }

    [Fact]
    public void AddMongoClient_generic_should_register_custom_type()
    {
        var services = new ServiceCollection();

        services.AddMongoClient<IMyMongoClient>(new MongoClientSettings(), (sp, key, s) => new MyMongoClient(s));

        var client = services.BuildServiceProvider().GetService<IMyMongoClient>();

        client.Should().NotBeNull();
        client.Should().BeOfType<MyMongoClient>();
    }

    private interface IMyMongoClient
    {
    }

    private class MyMongoClient(MongoClientSettings mongoClientSettings) : IMyMongoClient
    {
        public MongoClientSettings MongoClientSettings { get; } = mongoClientSettings;
    }

    [Theory]
    [InlineData(MethodOverload.ConnectionString)]
    [InlineData(MethodOverload.Url)]
    [InlineData(MethodOverload.MongoClientSettings)]
    [InlineData(MethodOverload.ConnectionStringAndConfigAction)]
    [InlineData(MethodOverload.UrlAndConfigAction)]
    [InlineData(MethodOverload.ClientSettingsAndConfigAction)]
    [InlineData(MethodOverload.Everything)]
    public void AddMongoClient_should_configure_logging_if_not_set(MethodOverload overload)
    {
        var services = new ServiceCollection();
        var loggerFactory = new ListLoggerFactory();
        services.AddSingleton<ILoggerFactory>(loggerFactory);

        CallAddMongoClientOverload(overload, services);

        var client = services.BuildServiceProvider().GetRequiredService<IMongoClient>();

        client.Settings.LoggingSettings.Should().NotBeNull();
        client.Settings.LoggingSettings.LoggerFactory.Should().BeSameAs(loggerFactory);

        loggerFactory.Log.Should().NotBeEmpty();
    }

#if NET8_0_OR_GREATER
    [Theory]
    [InlineData(MethodOverload.ConnectionString)]
    [InlineData(MethodOverload.Url)]
    [InlineData(MethodOverload.MongoClientSettings)]
    [InlineData(MethodOverload.ConnectionStringAndConfigAction)]
    [InlineData(MethodOverload.UrlAndConfigAction)]
    [InlineData(MethodOverload.ClientSettingsAndConfigAction)]
    [InlineData(MethodOverload.Everything)]
    public void AddMongoClient_keyed_should_work_on_supported_platforms(MethodOverload overload)
    {
        var key1 = "client1";
        var key2 = "client2";

        var services = new ServiceCollection();

        CallAddMongoClientOverload(overload, services, serviceKey: key1);
        CallAddMongoClientOverload(overload, services, serviceKey: key2);

        var provider = services.BuildServiceProvider();
        var client1 = provider.GetRequiredKeyedService<IMongoClient>(key1);
        var client2 = provider.GetRequiredKeyedService<IMongoClient>(key2);

        client1.Should().NotBeNull();
        client2.Should().NotBeNull();
        client1.Should().NotBeSameAs(client2);
    }
#else
    [Theory]
    [InlineData(MethodOverload.ConnectionString)]
    [InlineData(MethodOverload.Url)]
    [InlineData(MethodOverload.MongoClientSettings)]
    [InlineData(MethodOverload.ConnectionStringAndConfigAction)]
    [InlineData(MethodOverload.UrlAndConfigAction)]
    [InlineData(MethodOverload.ClientSettingsAndConfigAction)]
    [InlineData(MethodOverload.Everything)]
    public void AddMongoClient_keyed_should_throw_on_unsupported_platforms(MethodOverload overload)
    {
        var services = new ServiceCollection();

        var exception = Record.Exception(() => CallAddMongoClientOverload(overload, services, serviceKey: "key"));

        exception.Should().BeOfType<NotSupportedException>();
    }
#endif

    public enum MethodOverload
    {
        ConnectionString,
        Url,
        MongoClientSettings,
        ConnectionStringAndConfigAction,
        UrlAndConfigAction,
        ClientSettingsAndConfigAction,
        Everything,
    }

    private static readonly string __mongoUri = Environment.GetEnvironmentVariable("MONGODB_URI") ??
                                                Environment.GetEnvironmentVariable("MONGO_URI") ??
                                                "mongodb://localhost";

    private static IServiceCollection CallAddMongoClientOverload(
        MethodOverload overload,
        IServiceCollection services,
        object serviceKey = null,
        Func<IServiceProvider, object, MongoClientSettings, IMongoClient> clientFactory = null,
        bool nullConnectionString = false,
        bool nullUrl = false,
        bool nullClientSettings = false,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        var connectionString = nullConnectionString ? null : __mongoUri;
        var mongoUrl = nullUrl ? null : new MongoUrl(__mongoUri);
        var clientSettings = nullClientSettings ? null : new MongoClientSettings();

        return overload switch
        {
            MethodOverload.ConnectionString => services.AddMongoClient(connectionString, lifetime, serviceKey),
            MethodOverload.Url => services.AddMongoClient(mongoUrl, lifetime, serviceKey),
            MethodOverload.MongoClientSettings => services.AddMongoClient(clientSettings, lifetime, serviceKey),
            MethodOverload.ConnectionStringAndConfigAction => services.AddMongoClient(connectionString, clientFactory, lifetime, serviceKey),
            MethodOverload.UrlAndConfigAction => services.AddMongoClient(mongoUrl, clientFactory, lifetime, serviceKey),
            MethodOverload.ClientSettingsAndConfigAction => services.AddMongoClient(clientSettings, clientFactory, lifetime, serviceKey),
            MethodOverload.Everything => services.AddMongoClient<IMongoClient>(clientSettings, clientFactory, lifetime, serviceKey),
            _ => throw new ArgumentOutOfRangeException(nameof(overload), overload, null)
        };
    }
}
