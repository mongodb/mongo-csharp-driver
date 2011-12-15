/* Copyright 2010-2011 10gen Inc.
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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using MongoDB.Bson;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver
{
    /// <summary>
    /// Server connection mode.
    /// </summary>
    [Serializable]
    public enum ConnectionMode
    {
        /// <summary>
        /// Connect directly to a server.
        /// </summary>
        Direct,
        /// <summary>
        /// Connect to a replica set.
        /// </summary>
        ReplicaSet
    }

    /// <summary>
    /// Represents an immutable URL style connection string. See also MongoUrlBuilder.
    /// </summary>
    [Serializable]
    public class MongoUrl : IEquatable<MongoUrl>
    {
        // private static fields
        private static object staticLock = new object();
        private static Dictionary<string, MongoUrl> cache = new Dictionary<string, MongoUrl>();

        // private fields
        private MongoServerSettings serverSettings;
        private double waitQueueMultiple;
        private int waitQueueSize;
        private string databaseName;
        private string url;

        // constructors
        /// <summary>
        /// Creates a new instance of MongoUrl.
        /// </summary>
        /// <param name="url">The URL containing the settings.</param>
        public MongoUrl(string url)
        {
            var builder = new MongoUrlBuilder(url); // parses url
            serverSettings = builder.ToServerSettings().FrozenCopy();
            this.waitQueueMultiple = builder.WaitQueueMultiple;
            this.waitQueueSize = builder.WaitQueueSize;
            this.databaseName = builder.DatabaseName;
            this.url = builder.ToString(); // keep canonical form
        }

        // public properties
        /// <summary>
        /// Gets the actual wait queue size (either WaitQueueSize or WaitQueueMultiple x MaxConnectionPoolSize).
        /// </summary>
        public int ComputedWaitQueueSize
        {
            get
            {
                if (waitQueueMultiple == 0.0)
                {
                    return waitQueueSize;
                }
                else
                {
                    return (int)(waitQueueMultiple * serverSettings.MaxConnectionPoolSize);
                }
            }
        }

        /// <summary>
        /// Gets the connection mode.
        /// </summary>
        public ConnectionMode ConnectionMode
        {
            get { return serverSettings.ConnectionMode; }
        }

        /// <summary>
        /// Gets the connect timeout.
        /// </summary>
        public TimeSpan ConnectTimeout
        {
            get { return serverSettings.ConnectTimeout; }
        }

        /// <summary>
        /// Gets the optional database name.
        /// </summary>
        public string DatabaseName
        {
            get { return databaseName; }
        }

        /// <summary>
        /// Gets the default credentials.
        /// </summary>
        public MongoCredentials DefaultCredentials
        {
            get { return serverSettings.DefaultCredentials; }
        }

        /// <summary>
        /// Gets the representation to use for Guids.
        /// </summary>
        public GuidRepresentation GuidRepresentation
        {
            get { return serverSettings.GuidRepresentation; }
        }

        /// <summary>
        /// Gets whether to use IPv6.
        /// </summary>
        public bool IPv6
        {
            get { return serverSettings.IPv6; }
        }

        /// <summary>
        /// Gets the max connection idle time.
        /// </summary>
        public TimeSpan MaxConnectionIdleTime
        {
            get { return serverSettings.MaxConnectionIdleTime; }
        }

        /// <summary>
        /// Gets the max connection life time.
        /// </summary>
        public TimeSpan MaxConnectionLifeTime
        {
            get { return serverSettings.MaxConnectionLifeTime; }
        }

        /// <summary>
        /// Gets the max connection pool size.
        /// </summary>
        public int MaxConnectionPoolSize
        {
            get { return serverSettings.MaxConnectionPoolSize; }
        }

        /// <summary>
        /// Gets the min connection pool size.
        /// </summary>
        public int MinConnectionPoolSize
        {
            get { return serverSettings.MinConnectionPoolSize; }
        }

        /// <summary>
        /// Gets the name of the replica set.
        /// </summary>
        public string ReplicaSetName
        {
            get { return serverSettings.ReplicaSetName; }
        }

        /// <summary>
        /// Gets the SafeMode to use.
        /// </summary>
        public SafeMode SafeMode
        {
            get { return serverSettings.SafeMode; }
        }

        /// <summary>
        /// Gets the address of the server (see also Servers if using more than one address).
        /// </summary>
        public MongoServerAddress Server
        {
            get { return serverSettings.Server; }
        }

        /// <summary>
        /// Gets the list of server addresses (see also Server if using only one address).
        /// </summary>
        public IEnumerable<MongoServerAddress> Servers
        {
            get { return serverSettings.Servers; }
        }

        /// <summary>
        /// Gets whether queries should be sent to secondary servers.
        /// </summary>
        public bool SlaveOk
        {
            get { return serverSettings.SlaveOk; }
        }

        /// <summary>
        /// Gets the socket timeout.
        /// </summary>
        public TimeSpan SocketTimeout
        {
            get { return serverSettings.SocketTimeout; }
        }

        /// <summary>
        /// Gets the URL (in canonical form).
        /// </summary>
        public string Url
        {
            get { return url; }
        }

        /// <summary>
        /// Gets the wait queue multiple (the actual wait queue size will be WaitQueueMultiple x MaxConnectionPoolSize).
        /// </summary>
        public double WaitQueueMultiple
        {
            get { return waitQueueMultiple; }
        }

        /// <summary>
        /// Gets the wait queue size.
        /// </summary>
        public int WaitQueueSize
        {
            get { return waitQueueSize; }
        }

        /// <summary>
        /// Gets the wait queue timeout.
        /// </summary>
        public TimeSpan WaitQueueTimeout
        {
            get { return serverSettings.WaitQueueTimeout; }
        }

        // public operators
        /// <summary>
        /// Compares two MongoUrls.
        /// </summary>
        /// <param name="lhs">The first URL.</param>
        /// <param name="rhs">The other URL.</param>
        /// <returns>True if the two URLs are equal (or both null).</returns>
        public static bool operator ==(MongoUrl lhs, MongoUrl rhs)
        {
            return object.Equals(lhs, rhs);
        }

        /// <summary>
        /// Compares two MongoUrls.
        /// </summary>
        /// <param name="lhs">The first URL.</param>
        /// <param name="rhs">The other URL.</param>
        /// <returns>True if the two URLs are not equal (or one is null and the other is not).</returns>
        public static bool operator !=(MongoUrl lhs, MongoUrl rhs)
        {
            return !(lhs == rhs);
        }

        // public static methods
        /// <summary>
        /// Clears the URL cache. When a URL is parsed it is stored in the cache so that it doesn't have to be
        /// parsed again. There is rarely a need to call this method.
        /// </summary>
        public static void ClearCache()
        {
            cache.Clear();
        }

        /// <summary>
        /// Creates an instance of MongoUrl (might be an existing existence if the same URL has been used before).
        /// </summary>
        /// <param name="url">The URL containing the settings.</param>
        /// <returns>An instance of MongoUrl.</returns>
        public static MongoUrl Create(string url)
        {
            // cache previously seen urls to avoid repeated parsing
            lock (staticLock)
            {
                MongoUrl mongoUrl;
                if (!cache.TryGetValue(url, out mongoUrl))
                {
                    mongoUrl = new MongoUrl(url);
                    var canonicalUrl = mongoUrl.ToString();
                    if (canonicalUrl != url)
                    {
                        if (cache.ContainsKey(canonicalUrl))
                        {
                            mongoUrl = cache[canonicalUrl]; // use existing MongoUrl
                        }
                        else
                        {
                            cache[canonicalUrl] = mongoUrl; // cache under canonicalUrl also
                        }
                    }
                    cache[url] = mongoUrl;
                }
                return mongoUrl;
            }
        }

        // public methods
        /// <summary>
        /// Compares two MongoUrls.
        /// </summary>
        /// <param name="rhs">The other URL.</param>
        /// <returns>True if the two URLs are equal.</returns>
        public bool Equals(MongoUrl rhs)
        {
            if (object.ReferenceEquals(rhs, null) || GetType() != rhs.GetType()) { return false; }
            return url == rhs.url; // this works because URL is in canonical form
        }

        /// <summary>
        /// Compares two MongoUrls.
        /// </summary>
        /// <param name="obj">The other URL.</param>
        /// <returns>True if the two URLs are equal.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as MongoUrl); // works even if obj is null or of a different type
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return url.GetHashCode(); // this works because URL is in canonical form
        }

        /// <summary>
        /// Creates a new instance of MongoServerSettings based on the settings in this MongoUrlBuilder.
        /// </summary>
        /// <returns>A new instance of MongoServerSettings.</returns>
        public MongoServerSettings ToServerSettings()
        {
            return serverSettings;
        }

        /// <summary>
        /// Returns the canonical URL based on the settings in this MongoUrlBuilder.
        /// </summary>
        /// <returns>The canonical URL.</returns>
        public override string ToString()
        {
            return url;
        }
    }
}
