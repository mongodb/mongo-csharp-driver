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
using System.Net;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Logging
{
    internal static partial class StructuredLogTemplateProviders
    {
        public const string Awaited = nameof(Awaited);
        public const string Command = nameof(Command);
        public const string CommandName = nameof(CommandName);
        public const string DatabaseName = nameof(DatabaseName);
        public const string Description = nameof(Description);
        public const string DriverConnectionId = nameof(DriverConnectionId);
        public const string DurationMS = nameof(DurationMS);
        public const string Failure = nameof(Failure);
        public const string Error = nameof(Error);
        public const string MaxConnecting = nameof(MaxConnecting);
        public const string MaxIdleTimeMS = nameof(MaxIdleTimeMS);
        public const string MaxPoolSize = nameof(MaxPoolSize);
        public const string Message = nameof(Message);
        public const string MinPoolSize = nameof(MinPoolSize);
        public const string NewDescription = nameof(NewDescription);
        public const string Operation = nameof(Operation);
        public const string OperationId = nameof(OperationId);
        public const string PreviousDescription = nameof(PreviousDescription);
        public const string RequestId = nameof(RequestId);
        public const string Reply = nameof(Reply);
        public const string Reason = nameof(Reason);
        public const string Selector = nameof(Selector);
        public const string ServerHost = nameof(ServerHost);
        public const string ServerPort = nameof(ServerPort);
        public const string ServerConnectionId = nameof(ServerConnectionId);
        public const string ServiceId = nameof(ServiceId);
        public const string SharedLibraryVersion = nameof(SharedLibraryVersion);
        public const string TopologyDescription = nameof(TopologyDescription);
        public const string TopologyId = nameof(TopologyId);
        public const string WaitQueueTimeoutMS = nameof(WaitQueueTimeoutMS);
        public const string WaitQueueSize = nameof(WaitQueueSize);

        public const string DriverConnectionId_Message = $"{{{DriverConnectionId}}} {{{Message}}}";
        public const string ServerId_Message = $"{{{TopologyId}}} {{{ServerHost}}} {{{ServerPort}}} {{{Message}}}";
        public const string ServerId_Message_Description = $"{{{TopologyId}}} {{{ServerHost}}} {{{ServerPort}}} {{{Message}}} {{{Description}}}";
        public const string TopologyId_Message = $"{{{TopologyId}}} {{{Message}}}";
        public const string TopologyId_Message_SharedLibraryVersion = $"{{{TopologyId}}} {{{Message}}} {{{SharedLibraryVersion}}}";

        private readonly static LogTemplateProvider[] __eventTemplateProviders;

        static StructuredLogTemplateProviders()
        {
            var eventTypesCount = Enum.GetValues(typeof(EventType)).Length;
            __eventTemplateProviders = new LogTemplateProvider[eventTypesCount];

            AddClusterTemplates();
            AddCmapTemplates();
            AddCommandTemplates();
            AddConnectionTemplates();
            AddSdamTemplates();
        }

        public static LogTemplateProvider GetTemplateProvider(EventType eventType) => __eventTemplateProviders[(int)eventType];

        public static object[] GetParams(ClusterId clusterId, object arg1)
        {
            return new[] { clusterId.Value, arg1 };
        }

        public static object[] GetParams(ClusterId clusterId, object arg1, object arg2)
        {
            return new[] { clusterId.Value, arg1, arg2 };
        }

        public static object[] GetParams(ClusterId clusterId, object message, ClusterDescription oldDescription, ClusterDescription newDescription)
        {
            return new[] { clusterId.Value, message, oldDescription.ToString(), newDescription.ToString() };
        }

        public static object[] GetParams(ClusterId clusterId, EndPoint endPoint, object arg1)
        {
            var (host, port) = endPoint.GetHostAndPort();

            return new[] { clusterId.Value, host, port, arg1 };
        }

        public static object[] GetParams(ClusterId clusterId, object arg1, object arg2, object arg3, object arg4, object arg5)
        {
            return new object[] { clusterId.Value, arg1, arg2, arg3, arg4, arg5 };
        }

        public static object[] GetParams(ClusterId clusterId, object arg1, object arg2, object arg3, object arg4, object arg5, EndPoint endPoint)
        {
            var (host, port) = endPoint.GetHostAndPort();
            return new object[] { clusterId.Value, arg1, arg2, arg3, arg4, arg5, host, port };
        }

        public static object[] GetParams(ClusterId clusterId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
        {
            return new object[] { clusterId.Value, arg1, arg2, arg3, arg4, arg5, arg6 };
        }

        public static object[] GetParams(ServerId serverId, object arg1)
        {
            var (host, port) = serverId.EndPoint.GetHostAndPort();

            return new[] { serverId.ClusterId.Value, host, port, arg1 };
        }

        public static object[] GetParams(ServerId serverId, object arg1, object arg2)
        {
            var (host, port) = serverId.EndPoint.GetHostAndPort();

            return new[] { serverId.ClusterId.Value, host, port, arg1, arg2 };
        }

        public static object[] GetParams(ServerId serverId, object arg1, object arg2, object arg3)
        {
            var (host, port) = serverId.EndPoint.GetHostAndPort();

            return new[] { serverId.ClusterId.Value, host, port, arg1, arg2, arg3 };
        }

        public static object[] GetParams(ServerId serverId, object arg1, object arg2, object arg3, object arg4)
        {
            var (host, port) = serverId.EndPoint.GetHostAndPort();

            return new[] { serverId.ClusterId.Value, host, port, arg1, arg2, arg3, arg4 };
        }

        public static object[] GetParams(ServerId serverId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
        {
            var (host, port) = serverId.EndPoint.GetHostAndPort();

            return new[] { serverId.ClusterId.Value, host, port, arg1, arg2, arg3, arg4, arg5, arg6 };
        }

        public static object[] GetParams(ServerId serverId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7)
        {
            var (host, port) = serverId.EndPoint.GetHostAndPort();

            return new[] { serverId.ClusterId.Value, host, port, arg1, arg2, arg3, arg4, arg5, arg6, arg7 };
        }

        public static object[] GetParams(ConnectionId connectionId, object arg1)
        {
            var (host, port) = connectionId.ServerId.EndPoint.GetHostAndPort();

            return new[] { connectionId.ServerId.ClusterId.Value, connectionId.LongLocalValue, host, port, connectionId.LongServerValue, arg1 };
        }

        public static object[] GetParams(ConnectionId connectionId, object arg1, object arg2)
        {
            var (host, port) = connectionId.ServerId.EndPoint.GetHostAndPort();

            return new[] { connectionId.ServerId.ClusterId.Value, connectionId.LongLocalValue, host, port, connectionId.LongServerValue, arg1, arg2 };
        }

        public static object[] GetParams(ConnectionId connectionId, object arg1, object arg2, object arg3, object arg4)
        {
            var (host, port) = connectionId.ServerId.EndPoint.GetHostAndPort();

            return new[] { connectionId.ServerId.ClusterId.Value, connectionId.LongLocalValue, host, port, connectionId.LongServerValue, arg1, arg2, arg3, arg4 };
        }

        public static object[] GetParamsOmitNull(ConnectionId connectionId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object ommitableParam)
        {
            var (host, port) = connectionId.ServerId.EndPoint.GetHostAndPort();

            if (ommitableParam == null)
                return new[] { connectionId.ServerId.ClusterId.Value, connectionId.LongLocalValue, host, port, connectionId.LongServerValue, arg1, arg2, arg3, arg4, arg5, arg6 };
            else
                return new[] { connectionId.ServerId.ClusterId.Value, connectionId.LongLocalValue, host, port, connectionId.LongServerValue, arg1, arg2, arg3, arg4, arg5, arg6, ommitableParam };
        }

        public static object[] GetParamsOmitNull(ConnectionId connectionId, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object ommitableParam)
        {
            var (host, port) = connectionId.ServerId.EndPoint.GetHostAndPort();

            if (ommitableParam == null)
                return new[] { connectionId.ServerId.ClusterId.Value, connectionId.LongLocalValue, host, port, connectionId.LongServerValue, arg1, arg2, arg3, arg4, arg5, arg6, arg7, };
            else
                return new[] { connectionId.ServerId.ClusterId.Value, connectionId.LongLocalValue, host, port, connectionId.LongServerValue, arg1, arg2, arg3, arg4, arg5, arg6, arg7, ommitableParam };
        }

        private static void AddTemplateProvider<TEvent>(LogLevel logLevel, string template, Func<TEvent, EventLogFormattingOptions, object[]> extractor) where TEvent : struct, IEvent =>
            AddTemplateProvider<TEvent>(new LogTemplateProvider(
                logLevel,
                new[] { template },
                extractor));

        private static void AddTemplateProvider<TEvent>(LogLevel logLevel, string[] templates, Func<TEvent, EventLogFormattingOptions, object[]> extractor, Func<TEvent, LogTemplateProvider, string> templateExtractor) where TEvent : struct, IEvent =>
            AddTemplateProvider<TEvent>(new LogTemplateProvider(
                logLevel,
                templates,
                extractor,
                templateExtractor));

        private static void AddTemplate<TEvent, TArg>(LogLevel logLevel, string template, Func<TEvent, EventLogFormattingOptions, TArg, object[]> extractor) where TEvent : struct, IEvent =>
            AddTemplateProvider<TEvent>(new LogTemplateProvider(
                logLevel,
                new[] { template },
                extractor));

        private static void AddTemplateProvider<TEvent>(LogTemplateProvider templateProvider) where TEvent : struct, IEvent
        {
            var index = (int)(new TEvent().Type);

            if (__eventTemplateProviders[index] != null)
            {
                throw new InvalidOperationException($"Template already registered for {typeof(TEvent)} event.");
            }

            __eventTemplateProviders[index] = templateProvider;
        }

        private static string Concat(params string[] parameters) =>
            string.Join(" ", parameters.Select(p => $"{{{p}}}"));

        private static string Concat(string[] parameters, params string[] additionalParameters) =>
            string.Join(" ", parameters.Concat(additionalParameters).Select(p => $"{{{p}}}"));

        private static string DocumentToString(BsonDocument document, EventLogFormattingOptions eventLogFormattingOptions)
        {
            if (document == null)
            {
                return null;
            }

            return TruncateIfNeeded(document.ToString(), eventLogFormattingOptions.MaxDocumentSize);
        }

        private static string FormatException(Exception exception, EventLogFormattingOptions eventLogFormattingOptions)
        {
            if (exception == null)
            {
                return null;
            }

            return TruncateIfNeeded(exception.ToString(), eventLogFormattingOptions.MaxDocumentSize);
        }

        private static string TruncateIfNeeded(string str, int length) =>
             str.Length > length ? str.Substring(0, length) + "..." : str;

        internal sealed class LogTemplateProvider
        {
            public LogLevel LogLevel { get; }
            public string[] Templates { get; }
            public Delegate ParametersExtractor { get; }
            public Delegate TemplateExtractor { get; }

            public LogTemplateProvider(LogLevel logLevel, string[] templates, Delegate parametersExtractor, Delegate templateExtractor = null)
            {
                LogLevel = logLevel;
                Templates = templates;
                ParametersExtractor = parametersExtractor;
                TemplateExtractor = templateExtractor;
            }

            public string GetTemplate<TEvent>(TEvent @event) where TEvent : struct, IEvent =>
                TemplateExtractor != null ? ((Func<TEvent, LogTemplateProvider, string>)TemplateExtractor)(@event, this) : Templates.First();

            public object[] GetParams<TEvent>(TEvent @event, EventLogFormattingOptions eventLogFormattingOptions) where TEvent : struct, IEvent =>
                (ParametersExtractor as Func<TEvent, EventLogFormattingOptions, object[]>)(@event, eventLogFormattingOptions);

            public object[] GetParams<TEvent, TArg>(TEvent @event, EventLogFormattingOptions eventLogFormattingOptions, TArg arg) where TEvent : struct, IEvent =>
                (ParametersExtractor as Func<TEvent, EventLogFormattingOptions, TArg, object[]>)(@event, eventLogFormattingOptions, arg);
        }
    }
}
