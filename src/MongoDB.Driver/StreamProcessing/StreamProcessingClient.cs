/* Copyright 2026-present MongoDB Inc.
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

namespace MongoDB.Driver.StreamProcessing
{
    /// <summary>
    /// Client for an Atlas Stream Processing workspace.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="MongoClient"/> so that connection intent is
    /// explicit and ASP commands cannot be accidentally routed to a standard
    /// <c>mongod</c>. The underlying <see cref="MongoClient"/> is still
    /// available via <see cref="Client"/> for advanced uses such as
    /// <c>RunCommand</c> against admin.
    ///
    /// Workspace endpoints share the <c>mongodb://</c> URI scheme with
    /// standard MongoDB clusters but follow a distinct hostname pattern:
    ///
    /// <c>mongodb://atlas-stream-&lt;workspaceId&gt;-&lt;suffix&gt;.&lt;region&gt;.a.query.mongodb.net/</c>
    ///
    /// Atlas staging endpoints use <c>.a.query.mongodb-stage.net</c>; both are
    /// accepted. Per the ASP spec, TLS is required and the authentication
    /// source defaults to "admin".
    /// </remarks>
    public sealed class StreamProcessingClient : IDisposable
    {
        private readonly MongoClient _client;
        private readonly IMongoDatabase _admin;
        private readonly string _uri;
        private bool _disposed;

        /// <summary>
        /// Initializes a new <see cref="StreamProcessingClient"/>.
        /// </summary>
        /// <param name="uri">Workspace connection string.</param>
        public StreamProcessingClient(string uri)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (!IsWorkspaceUri(uri))
            {
                throw new ArgumentException(
                    "StreamProcessingClient requires a workspace endpoint URI " +
                    "(atlas-stream-*.a.query.mongodb.net or .mongodb-<env>.net). " +
                    "For standard MongoDB clusters, use MongoClient instead.",
                    nameof(uri));
            }

            if (uri.StartsWith("mongodb+srv://", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "mongodb+srv:// is not supported for workspace endpoints; use mongodb://.",
                    nameof(uri));
            }

            var settings = MongoClientSettings.FromConnectionString(uri);

            // TLS is required and MUST NOT be disabled.
            if (!settings.UseTls)
            {
                settings.UseTls = true;
            }

            // authSource defaults to "admin" but MAY be overridden by the caller.
            if (settings.Credential != null && string.IsNullOrEmpty(settings.Credential.Source))
            {
                settings.Credential = settings.Credential.WithMechanismProperty("SOURCE", "admin");
            }

            _uri = uri;
            _client = new MongoClient(settings);
            _admin = _client.GetDatabase("admin");
        }

        /// <summary>The underlying <see cref="MongoClient"/>.</summary>
        public MongoClient Client => _client;

        /// <summary>The workspace URI passed to the constructor.</summary>
        public string Uri => _uri;

        /// <summary>Returns a handle for managing stream processors in this workspace.</summary>
        public StreamProcessors StreamProcessorsView => new StreamProcessors(_admin);

        /// <summary>Disposes the underlying <see cref="MongoClient"/>.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _client.Dispose();
            _disposed = true;
        }

        /// <summary>
        /// Returns true when the supplied URI targets an Atlas Stream
        /// Processing workspace endpoint.
        /// </summary>
        /// <remarks>
        /// Matches hostnames that begin with <c>atlas-stream-</c> and end with
        /// <c>.a.query.mongodb.net</c> (production) or
        /// <c>.a.query.mongodb-&lt;env&gt;.net</c> (e.g. <c>mongodb-stage.net</c>
        /// for Atlas staging).
        /// </remarks>
        public static bool IsWorkspaceUri(string uri)
        {
            if (string.IsNullOrEmpty(uri)) return false;

            var lower = uri.ToLowerInvariant();
            if (!lower.StartsWith("mongodb://", StringComparison.Ordinal)) return false;

            var afterScheme = lower.Substring("mongodb://".Length);

            // Strip userinfo: if "@" appears before any path/query, take everything after.
            var pathOrQueryIdx = IndexOfAny(afterScheme, '/', '?');
            var searchEnd = pathOrQueryIdx == -1 ? afterScheme.Length : pathOrQueryIdx;
            var atIdx = afterScheme.LastIndexOf('@', searchEnd > 0 ? searchEnd - 1 : 0);
            var hostSection = atIdx >= 0 ? afterScheme.Substring(atIdx + 1) : afterScheme;

            // Strip path/query/port.
            var endIdx = IndexOfAny(hostSection, '/', '?', ':');
            var host = endIdx == -1 ? hostSection : hostSection.Substring(0, endIdx);

            if (!host.StartsWith("atlas-stream-", StringComparison.Ordinal)) return false;
            if (host.EndsWith(".a.query.mongodb.net", StringComparison.Ordinal)) return true;

            // Accept .a.query.mongodb-<env>.net
            var envMarker = ".a.query.mongodb-";
            var markerIdx = host.LastIndexOf(envMarker, StringComparison.Ordinal);
            if (markerIdx >= 0 && host.EndsWith(".net", StringComparison.Ordinal))
            {
                var envStart = markerIdx + envMarker.Length;
                var envLength = host.Length - envStart - ".net".Length;
                if (envLength > 0)
                {
                    for (var i = envStart; i < envStart + envLength; i++)
                    {
                        var c = host[i];
                        var ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-';
                        if (!ok) return false;
                    }
                    return true;
                }
            }

            return false;
        }

        private static int IndexOfAny(string s, params char[] chars)
        {
            return s.IndexOfAny(chars);
        }
    }
}
