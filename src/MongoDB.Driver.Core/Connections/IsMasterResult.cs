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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Connections
{
    public class IsMasterResult
    {
        // fields
        private readonly BsonDocument _wrapped;

        // constructors
        public IsMasterResult(BsonDocument wrapped)
        {
            _wrapped = Ensure.IsNotNull(wrapped, "wrapped");
        }

        // properties
        public bool IsArbiter
        {
            get
            {
                return _wrapped.GetValue("arbiterOnly", false).ToBoolean();
            }
        }

        public bool IsReplicaSetMember
        {
            get
            {
                return _wrapped.GetValue("isreplicaset", false).ToBoolean() || _wrapped.Contains("setName");
            }
        }

        public int MaxBatchCount
        {
            get
            {
                BsonValue value;
                if (_wrapped.TryGetValue("maxWriteBatchSize", out value))
                {
                    return value.ToInt32();
                }

                return 1000;
            }
        }

        public int MaxDocumentSize
        {
            get
            {
                BsonValue value;
                if (_wrapped.TryGetValue("maxBsonObjectSize", out value))
                {
                    return value.ToInt32();
                }

                return 4 * 1024 * 1024;
            }
        }

        public int MaxMessageSize
        {
            get
            {
                BsonValue value;
                if (_wrapped.TryGetValue("maxMessageSizeBytes", out value))
                {
                    return value.ToInt32();
                }

                return Math.Max(MaxDocumentSize + 1024, 16000000);
            }
        }

        public ServerType ServerType
        {
            get
            {
                if (IsReplicaSetMember)
                {
                    if (_wrapped.GetValue("ismaster", false).ToBoolean())
                    {
                        return ServerType.Primary;
                    }
                    if (_wrapped.GetValue("secondary", false).ToBoolean())
                    {
                        return ServerType.Secondary;
                    }
                    if (_wrapped.GetValue("passive", false).ToBoolean() || _wrapped.GetValue("hidden", false).ToBoolean())
                    {
                        return ServerType.Passive;
                    }
                    if (_wrapped.GetValue("arbiterOnly", false).ToBoolean())
                    {
                        return ServerType.Arbiter;
                    }
                    if (_wrapped.Contains("setName"))
                    {
                        return ServerType.Other;
                    }
                    return ServerType.Ghost;
                }

                if ((string)_wrapped.GetValue("msg", null) == "isdbgrid")
                {
                    return ServerType.ShardRouter;
                }

                if (_wrapped.GetValue("ismaster", false).ToBoolean())
                {
                    return ServerType.Standalone;
                }

                return ServerType.Unknown;
            }
        }

        public TagSet Tags
        {
            get
            {
                BsonValue tags;
                if (_wrapped.TryGetValue("tags", out tags))
                {
                    return new TagSet(tags.AsBsonDocument.Select(e => new Tag(e.Name, (string)e.Value)));
                }

                return null;
            }
        }

        public BsonDocument Wrapped
        {
            get { return _wrapped; }
        }

        // methods
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(IsMasterResult)) { return false; }
            var rhs = (IsMasterResult)obj;
            return _wrapped.Equals(rhs._wrapped);
        }

        public override int GetHashCode()
        {
            return _wrapped.GetHashCode();
        }

        private List<DnsEndPoint> GetMembers(AddressFamily addressFamily)
        {
            var hosts = GetMembers(addressFamily, "hosts");
            var passives = GetMembers(addressFamily, "passives");
            var arbiters = GetMembers(addressFamily, "arbiters");
            return hosts.Concat(passives).Concat(arbiters).ToList();
        }

        private IEnumerable<DnsEndPoint> GetMembers(AddressFamily addressFamily, string elementName)
        {
            if (!_wrapped.Contains(elementName))
            {
                return new DnsEndPoint[0];
            }

            return ((BsonArray)_wrapped[elementName]).Select(v => DnsEndPointParser.Parse((string)v, addressFamily));
        }

        private DnsEndPoint GetPrimary(AddressFamily addressFamily)
        {
            BsonValue primary;
            if (_wrapped.TryGetValue("primary", out primary))
            {
                // TODO: what does primary look like when there is no current primary (null, empty string)?
                return DnsEndPointParser.Parse((string)primary, addressFamily);
            }

            return null;
        }

        public ReplicaSetConfig GetReplicaSetConfig(AddressFamily addressFamily)
        {
            if (!IsReplicaSetMember)
            {
                return null;
            }

            var members = GetMembers(addressFamily);
            var name = (string)_wrapped.GetValue("setName", null);
            var primary = GetPrimary(addressFamily);
            var version = _wrapped.Contains("version") ? (int?)_wrapped["version"].ToInt32() : null;

            return new ReplicaSetConfig(members, name, primary, version);
        }
    }
}
