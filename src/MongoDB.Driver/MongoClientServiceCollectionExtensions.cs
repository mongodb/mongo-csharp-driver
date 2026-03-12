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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver;

/// <summary>
/// Extension methods for adding a <see cref="MongoClient"/> to an <see cref="IServiceCollection"/>.
/// </summary>
[CLSCompliant(false)]
public static class MongoClientServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="MongoClient"/> as a <see cref="IMongoClient"/> service in the
    /// <see cref="IServiceCollection"/> using the specified connection string.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="connectionString">The connection string of the database to connect to.</param>
    /// <param name="lifetime">The registration lifetime, or <see cref="ServiceLifetime.Singleton"/> by default.</param>
    /// <param name="serviceKey">An optional service key for the registration, on .NET 8 or higher.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddMongoClient(
        this IServiceCollection services,
        string connectionString,
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        object serviceKey = null)
        => AddMongoClient(services, connectionString, null, lifetime, serviceKey);

    /// <summary>
    /// Registers a <see cref="MongoClient"/> as a <see cref="IMongoClient"/> service in the
    /// <see cref="IServiceCollection"/> using the specified connection string.
    /// </summary>
    /// <remarks>
    /// The <paramref name="clientFactory"/> is called when the service is resolved from DI and allows customization
    /// of the client based on the passed in <see cref="IServiceProvider"/> container, client key, and settings.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="connectionString">The connection string of the database to connect to.</param>
    /// <param name="clientFactory">An action to customize the creation of the client in DI.</param>
    /// <param name="lifetime">The registration lifetime, or <see cref="ServiceLifetime.Singleton"/> by default.</param>
    /// <param name="serviceKey">An optional service key for the registration, on .NET 8 or higher.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddMongoClient(
        this IServiceCollection services,
        string connectionString,
        Func<IServiceProvider, object, MongoClientSettings, IMongoClient> clientFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        object serviceKey = null)
        => AddMongoClient(
            services,
            MongoClientSettings.FromConnectionString(Ensure.IsNotNull(connectionString, nameof(connectionString))),
            clientFactory,
            lifetime,
            serviceKey);

    /// <summary>
    /// Registers a <see cref="MongoClient"/> as a <see cref="IMongoClient"/> service in the
    /// <see cref="IServiceCollection"/> using the specified <see cref="MongoUrl"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="mongoUrl">The <see cref="MongoUrl"/> of the database to connect to.</param>
    /// <param name="lifetime">The registration lifetime, or <see cref="ServiceLifetime.Singleton"/> by default.</param>
    /// <param name="serviceKey">An optional service key for the registration, on .NET 8 or higher.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddMongoClient(
        this IServiceCollection services,
        MongoUrl mongoUrl,
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        object serviceKey = null)
        => AddMongoClient(services, mongoUrl, null, lifetime, serviceKey);

    /// <summary>
    /// Registers a <see cref="MongoClient"/> as a <see cref="IMongoClient"/> service in the
    /// <see cref="IServiceCollection"/> using the specified <see cref="MongoUrl"/>.
    /// </summary>
    /// <remarks>
    /// The <paramref name="clientFactory"/> is called when the service is resolved from DI and allows customization
    /// of the client based on the passed in <see cref="IServiceProvider"/> container, client key, and settings.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="mongoUrl">The <see cref="MongoUrl"/> of the database to connect to.</param>
    /// <param name="clientFactory">An action to customize the creation of the client in DI.</param>
    /// <param name="lifetime">The registration lifetime, or <see cref="ServiceLifetime.Singleton"/> by default.</param>
    /// <param name="serviceKey">An optional service key for the registration, on .NET 8 or higher.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddMongoClient(
        this IServiceCollection services,
        MongoUrl mongoUrl,
        Func<IServiceProvider, object, MongoClientSettings, IMongoClient> clientFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        object serviceKey = null)
        => AddMongoClient(
            services,
            MongoClientSettings.FromUrl(Ensure.IsNotNull(mongoUrl, nameof(mongoUrl))),
            clientFactory,
            lifetime,
            serviceKey);

    /// <summary>
    /// Registers a <see cref="MongoClient"/> as a <see cref="IMongoClient"/> service in the
    /// <see cref="IServiceCollection"/> using the specified <see cref="MongoClientSettings"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="mongoClientSettings">The <see cref="MongoClientSettings"/> to use.</param>
    /// <param name="lifetime">The registration lifetime, or <see cref="ServiceLifetime.Singleton"/> by default.</param>
    /// <param name="serviceKey">An optional service key for the registration, on .NET 8 or higher.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddMongoClient(
        this IServiceCollection services,
        MongoClientSettings mongoClientSettings,
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        object serviceKey = null)
        => AddMongoClient(services, mongoClientSettings, null, lifetime, serviceKey);

    /// <summary>
    /// Registers a <see cref="MongoClient"/> as a <see cref="IMongoClient"/> service in the
    /// <see cref="IServiceCollection"/> using the specified <see cref="MongoClientSettings"/>.
    /// </summary>
    /// <remarks>
    /// The <paramref name="clientFactory"/> is called when the service is resolved from DI and allows customization
    /// of the client based on the passed in <see cref="IServiceProvider"/> container, client key, and settings.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="mongoClientSettings">The <see cref="MongoClientSettings"/> to use.</param>
    /// <param name="clientFactory">An action to customize the creation of the client in DI.</param>
    /// <param name="lifetime">The registration lifetime, or <see cref="ServiceLifetime.Singleton"/> by default.</param>
    /// <param name="serviceKey">An optional service key for the registration, on .NET 8 or higher.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddMongoClient(
        this IServiceCollection services,
        MongoClientSettings mongoClientSettings,
        Func<IServiceProvider, object, MongoClientSettings, IMongoClient> clientFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        object serviceKey = null)
        => AddMongoClient<IMongoClient>(services, mongoClientSettings, clientFactory, lifetime, serviceKey);

    /// <summary>
    /// Registers a <see cref="MongoClient"/> as the specified service type in the
    /// <see cref="IServiceCollection"/> using the specified <see cref="MongoClientSettings"/>.
    /// </summary>
    /// <remarks>
    /// The <paramref name="clientFactory"/> is called when the service is resolved from DI and allows customization
    /// of the client based on the passed in <see cref="IServiceProvider"/> container, client key, and settings.
    /// </remarks>
    /// <typeparam name="TClient">
    /// The type of the service to register, which is usually <see cref="IMongoClient"/> or <see cref="MongoClient"/>,
    /// but may be some other type to register the client under a custom interface.
    /// </typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="mongoClientSettings">The <see cref="MongoClientSettings"/> to use.</param>
    /// <param name="clientFactory">An action to customize the creation of the client in DI.</param>
    /// <param name="lifetime">The registration lifetime, or <see cref="ServiceLifetime.Singleton"/> by default.</param>
    /// <param name="serviceKey">An optional service key for the registration, on .NET 8 or higher.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddMongoClient<TClient>(
        this IServiceCollection services,
        MongoClientSettings mongoClientSettings,
        Func<IServiceProvider, object, MongoClientSettings, TClient> clientFactory,
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        object serviceKey = null)
    {
        Ensure.IsNotNull(services, nameof(services));
        Ensure.IsNotNull(mongoClientSettings, nameof(mongoClientSettings));

#if NET8_0_OR_GREATER
        services.TryAdd(
            new ServiceDescriptor(
                typeof(TClient),
                serviceKey,
                (sp, key) => CreateMongoClient(sp, key, mongoClientSettings, clientFactory),
                lifetime));
#else
        if (serviceKey != null)
        {
            throw new NotSupportedException(
                "Keyed DI services are only supported on .NET 8 and later. Null must be passed for the serviceKey argument on all other platforms.");
        }

        services.TryAdd(
            new ServiceDescriptor(
                typeof(TClient),
                sp => CreateMongoClient(sp, null, mongoClientSettings, clientFactory),
                lifetime));
#endif
        return services;
    }

    private static TClient CreateMongoClient<TClient>(IServiceProvider serviceProvider,
        object key,
        MongoClientSettings mongoClientSettings,
        Func<IServiceProvider, object, MongoClientSettings, TClient> clientFactory)
    {
        if (mongoClientSettings.LoggingSettings == null)
        {
            mongoClientSettings = mongoClientSettings.Clone();
            mongoClientSettings.LoggingSettings = new(serviceProvider.GetService<ILoggerFactory>());
        }

        var mongoClient = clientFactory != null
            ? clientFactory(serviceProvider, key, mongoClientSettings)
            : (object)new MongoClient(mongoClientSettings);

        return (TClient)mongoClient;
    }
}
