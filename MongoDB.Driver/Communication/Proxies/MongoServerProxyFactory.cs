/* Copyright 2010-2013 10gen Inc.
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

using System.Collections.Generic;
using System.Linq;

namespace MongoDB.Driver.Internal
{
    /// <summary>
    /// Creates a MongoServerInstanceManager based on the settings.
    /// </summary>
    internal class MongoServerProxyFactory
    {
        // private static fields
        private static readonly MongoServerProxyFactory __instance = new MongoServerProxyFactory();

        // private fields
        private readonly object _lock = new object();
        private readonly Dictionary<MongoServerProxySettings, IMongoServerProxy> _proxies = new Dictionary<MongoServerProxySettings, IMongoServerProxy>();

        private int _nextSequentialId = 1;

        // public static properties
        /// <summary>
        /// Gets the default instance.
        /// </summary>
        /// <value>
        /// The default instance.
        /// </value>
        public static MongoServerProxyFactory Instance
        {
            get { return __instance; }
        }

        // public properties
        /// <summary>
        /// Gets the proxy count.
        /// </summary>
        /// <value>
        /// The proxy count.
        /// </value>
        public int ProxyCount
        {
            get
            {
                lock (_lock)
                {
                    return _proxies.Count;
                }
            }
        }

        // public methods
        /// <summary>
        /// Creates an IMongoServerProxy of some type that depends on the settings (or returns an existing one if one has already been created with these settings).
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <returns>An IMongoServerProxy.</returns>
        public IMongoServerProxy Create(MongoServerProxySettings settings)
        {
            lock (_lock)
            {
                IMongoServerProxy proxy;
                if (!_proxies.TryGetValue(settings, out proxy))
                {
                    proxy = CreateInstance(_nextSequentialId++, settings);
                    _proxies.Add(settings, proxy);
                }
                return proxy;
            }
        }

        // private methods
        private IMongoServerProxy CreateInstance(int sequentialId, MongoServerProxySettings settings)
        {
            var connectionMode = settings.ConnectionMode;
            if (settings.ConnectionMode == ConnectionMode.Automatic)
            {
                if (settings.ReplicaSetName != null)
                {
                    connectionMode = ConnectionMode.ReplicaSet;
                }
                else if (settings.Servers.Count() == 1)
                {
                    connectionMode = ConnectionMode.Direct;
                }
            }

            switch (connectionMode)
            {
                case ConnectionMode.Direct:
                    return new DirectMongoServerProxy(sequentialId, settings);
                case ConnectionMode.ReplicaSet:
                    return new ReplicaSetMongoServerProxy(sequentialId, settings);
                case ConnectionMode.ShardRouter:
                    return new ShardedMongoServerProxy(sequentialId, settings);
                default:
                    return new DiscoveringMongoServerProxy(sequentialId, settings);
            }
        }
    }
}