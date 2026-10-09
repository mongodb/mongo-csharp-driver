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
using FluentAssertions;
using Xunit;

namespace MongoDB.Driver.Tests;

public class MongoClientBuilderTests
{
    public static IEnumerable<object[]> ConfigureMethods =>
    [
        [nameof(MongoClientBuilder.Authentication), new Action<MongoClientBuilder>(b => b.Authentication(null))],
        [nameof(MongoClientBuilder.AutoEncryption), new Action<MongoClientBuilder>(b => b.AutoEncryption(null))],
        [nameof(MongoClientBuilder.ClientMetadata), new Action<MongoClientBuilder>(b => b.ClientMetadata(null))],
        [nameof(MongoClientBuilder.Cluster), new Action<MongoClientBuilder>(b => b.Cluster(null))],
        [nameof(MongoClientBuilder.ConnectionPool), new Action<MongoClientBuilder>(b => b.ConnectionPool(null))],
        [nameof(MongoClientBuilder.Diagnostics), new Action<MongoClientBuilder>(b => b.Diagnostics(null))],
        [nameof(MongoClientBuilder.Network), new Action<MongoClientBuilder>(b => b.Network(null))],
        [nameof(MongoClientBuilder.Operations), new Action<MongoClientBuilder>(b => b.Operations(null))],
        [nameof(MongoClientBuilder.ServerMonitoring), new Action<MongoClientBuilder>(b => b.ServerMonitoring(null))],
        [nameof(MongoClientBuilder.Tls), new Action<MongoClientBuilder>(b => b.Tls(null))],
        [nameof(MongoClientBuilder.Translation), new Action<MongoClientBuilder>(b => b.Translation(null))]
    ];

    [Theory]
    [MemberData(nameof(ConfigureMethods))]
    public void Configure_method_should_throw_when_configure_is_null(string methodName, Action<MongoClientBuilder> callWithNull)
    {
        var subject = new MongoClientBuilder();

        var exception = Record.Exception(() => callWithNull(subject));

        exception.Should().BeOfType<ArgumentNullException>(because: methodName)
            .Subject.ParamName.Should().Be("configure");
    }
}
