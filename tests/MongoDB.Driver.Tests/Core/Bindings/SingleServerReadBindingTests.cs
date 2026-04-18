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
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Servers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Bindings;

public class SingleServerReadBindingTests
{
    [Fact]
    public void constructor_should_initialize_instance()
    {
        var cluster = new Mock<IClusterInternal>().Object;
        var server = CreateMockServer().Object;
        var readPreference = ReadPreference.Primary;
        var session = new Mock<ICoreSessionHandle>().Object;

        var result = new SingleServerReadBinding(cluster, server, readPreference, session);

        result._cluster().Should().BeSameAs(cluster);
        result._disposed().Should().BeFalse();
        result.ReadPreference.Should().BeSameAs(readPreference);
        result.Session.Should().BeSameAs(session);
    }

    [Fact]
    public void constructor_should_throw_when_cluster_is_null()
    {
        var server = CreateMockServer().Object;
        var readPreference = ReadPreference.Primary;
        var session = new Mock<ICoreSessionHandle>().Object;

        var exception = Record.Exception(() => new SingleServerReadBinding(null, server, readPreference, session));

        var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
        e.ParamName.Should().Be("cluster");
    }

    [Fact]
    public void constructor_should_throw_when_server_is_null()
    {
        var cluster = new Mock<IClusterInternal>().Object;
        var readPreference = ReadPreference.Primary;
        var session = new Mock<ICoreSessionHandle>().Object;

        var exception = Record.Exception(() => new SingleServerReadBinding(cluster, null, readPreference, session));

        var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
        e.ParamName.Should().Be("server");
    }

    [Fact]
    public void constructor_should_throw_when_readPreference_is_null()
    {
        var cluster = new Mock<IClusterInternal>().Object;
        var server = CreateMockServer().Object;
        var session = new Mock<ICoreSessionHandle>().Object;

        var exception = Record.Exception(() => new SingleServerReadBinding(cluster, server, null, session));

        var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
        e.ParamName.Should().Be("readPreference");
    }

    [Fact]
    public void constructor_should_throw_when_session_is_null()
    {
        var cluster = new Mock<IClusterInternal>().Object;
        var server = CreateMockServer().Object;
        var readPreference = ReadPreference.Primary;

        var exception = Record.Exception(() => new SingleServerReadBinding(cluster, server, readPreference, null));

        var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
        e.ParamName.Should().Be("session");
    }

    [Fact]
    public void ReadPreference_should_return_expected_result()
    {
        var readPreference = ReadPreference.Secondary;
        var subject = CreateSubject(readPreference: readPreference);

        var result = subject.ReadPreference;

        result.Should().BeSameAs(readPreference);
    }

    [Fact]
    public void Session_should_return_expected_result()
    {
        var session = new Mock<ICoreSessionHandle>().Object;
        var subject = CreateSubject(session: session);

        var result = subject.Session;

        result.Should().BeSameAs(session);
    }

    [Fact]
    public void Dispose_should_have_expected_result()
    {
        var mockSession = new Mock<ICoreSessionHandle>();
        var subject = CreateSubject(session: mockSession.Object);

        subject.Dispose();

        subject._disposed().Should().BeTrue();
        mockSession.Verify(m => m.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_can_be_called_more_than_once()
    {
        var mockSession = new Mock<ICoreSessionHandle>();
        var subject = CreateSubject(session: mockSession.Object);

        subject.Dispose();
        subject.Dispose();

        mockSession.Verify(m => m.Dispose(), Times.Once);
    }

    [Theory]
    [ParameterAttributeData]
    public async Task GetReadChannelSource_should_throw_when_disposed(
        [Values(false, true)] bool async)
    {
        var subject = CreateDisposedSubject();

        var exception = async ?
            await Record.ExceptionAsync(() => subject.GetReadChannelSourceAsync(OperationContext.NoTimeout)) :
            Record.Exception(() => subject.GetReadChannelSource(OperationContext.NoTimeout));

        var e = exception.Should().BeOfType<ObjectDisposedException>().Subject;
        e.ObjectName.Should().Be(subject.GetType().FullName);
    }

    [Theory]
    [ParameterAttributeData]
    public async Task GetReadChannelSource_should_call_cluster_SelectServer(
        [Values(false, true)] bool async)
    {
        var clusterMock = new Mock<IClusterInternal>();
        clusterMock.Setup(c => c.SelectServer(It.IsAny<OperationContext>(), It.IsAny<IServerSelector>())).Returns(Mock.Of<IServer>());
        clusterMock.Setup(c => c.SelectServerAsync(It.IsAny<OperationContext>(), It.IsAny<IServerSelector>())).ReturnsAsync(Mock.Of<IServer>());

        var endpoint = new DnsEndPoint("localhost", 27017);
        var serverMock = new Mock<IServer>();
        serverMock.Setup(s => s.EndPoint).Returns(endpoint);
        var subject = CreateSubject(cluster: clusterMock.Object, server: serverMock.Object);

        if (async)
        {
            _ = await subject.GetReadChannelSourceAsync(OperationContext.NoTimeout);
            clusterMock.Verify(c => c.SelectServerAsync(It.IsAny<OperationContext>(), It.Is<EndPointServerSelector>(s => s.EndPoint == endpoint)));
        }
        else
        {
            _ = subject.GetReadChannelSource(OperationContext.NoTimeout);
            clusterMock.Verify(c => c.SelectServer(It.IsAny<OperationContext>(), It.Is<EndPointServerSelector>(s => s.EndPoint == endpoint)));
        }
    }

    // private methods
    private SingleServerReadBinding CreateDisposedSubject()
    {
        var subject = CreateSubject();
        subject.Dispose();
        return subject;
    }

    private SingleServerReadBinding CreateSubject(IClusterInternal cluster = null, IServer server = null, ReadPreference readPreference = null, ICoreSessionHandle session = null)
    {
        if (session == null)
        {
            var sessionMock = new Mock<ICoreSessionHandle>();
            sessionMock.Setup(m => m.Fork()).Returns(sessionMock.Object);
            session = sessionMock.Object;
        }

        return new SingleServerReadBinding(
            cluster ?? new Mock<IClusterInternal>().Object,
            server ?? CreateMockServer().Object,
            readPreference ?? ReadPreference.Primary,
            session);
    }

    private Mock<IServer> CreateMockServer()
    {
        var endpoint = new DnsEndPoint("localhost", 27017);
        var mockServer = new Mock<IServer>();
        mockServer.Setup(s => s.Description).Returns(new ServerDescription(
            new ServerId(new Clusters.ClusterId(), endpoint),
            endpoint,
            state: ServerState.Connected));
        mockServer.Setup(s => s.EndPoint).Returns(endpoint);

        return mockServer;
    }
}

internal static class SingleServerReadBindingReflector
{
    public static bool _disposed(this SingleServerReadBinding obj)
        => (bool)Reflector.GetFieldValue(obj, "_disposed");

    public static IClusterInternal _cluster(this SingleServerReadBinding obj)
        => (IClusterInternal)Reflector.GetFieldValue(obj, "_cluster");
}
