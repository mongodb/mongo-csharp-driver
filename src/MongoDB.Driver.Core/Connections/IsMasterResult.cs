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
    public sealed class IsMasterResult : IEquatable<IsMasterResult>
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
            get { return ServerType == ServerType.ReplicaSetArbiter; }
        }

        public bool IsReplicaSetMember
        {
            get { return ServerType.IsReplicaSetMember(); }
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
                if (!_wrapped.GetValue("ok", false).ToBoolean())
                {
                    return ServerType.Unknown;
                }

                if (_wrapped.GetValue("isreplicaset", false).ToBoolean())
                {
                    return ServerType.ReplicaSetGhost;
                }

                if (_wrapped.Contains("setName"))
                {
                    if (_wrapped.GetValue("ismaster", false).ToBoolean())
                    {
                        return ServerType.ReplicaSetPrimary;
                    }
                    if (_wrapped.GetValue("secondary", false).ToBoolean())
                    {
                        return ServerType.ReplicaSetSecondary;
                    }
                    if (_wrapped.GetValue("passive", false).ToBoolean() || _wrapped.GetValue("hidden", false).ToBoolean())
                    {
                        return ServerType.ReplicaSetPassive;
                    }
                    if (_wrapped.GetValue("arbiterOnly", false).ToBoolean())
                    {
                        return ServerType.ReplicaSetArbiter;
                    }

                    return ServerType.ReplicaSetOther;
                }

                if ((string)_wrapped.GetValue("msg", null) == "isdbgrid")
                {
                    return ServerType.ShardRouter;
                }

                return ServerType.Standalone;
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

        public int MaxWireVersion
        {
            get { return _wrapped.GetValue("maxWireVersion", 0).ToInt32(); }
        }

        public int MinWireVersion
        {
            get { return _wrapped.GetValue("minWireVersion", 0).ToInt32(); }
        }

        public BsonDocument Wrapped
        {
            get { return _wrapped; }
        }

        // methods
        public bool Equals(IsMasterResult other)
        {
            if (other == null)
            {
                return false;
            }

            return _wrapped.Equals(other._wrapped);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IsMasterResult);
        }

        public override int GetHashCode()
        {
            return _wrapped.GetHashCode();
        }

        private List<EndPoint> GetMembers()
        {
            var hosts = GetMembers("hosts");
            var passives = GetMembers("passives");
            var arbiters = GetMembers("arbiters");
            return hosts.Concat(passives).Concat(arbiters).ToList();
        }

        private IEnumerable<EndPoint> GetMembers(string elementName)
        {
            if (!_wrapped.Contains(elementName))
            {
                return new EndPoint[0];
            }

            return ((BsonArray)_wrapped[elementName]).Select(v => EndPointParser.Parse((string)v));
        }

        private EndPoint GetPrimary()
        {
            BsonValue primary;
            if (_wrapped.TryGetValue("primary", out primary))
            {
                // TODO: what does primary look like when there is no current primary (null, empty string)?
                return EndPointParser.Parse((string)primary);
            }

            return null;
        }

        public ReplicaSetConfig GetReplicaSetConfig()
        {
            if (!IsReplicaSetMember)
            {
                return null;
            }

            var members = GetMembers();
            var name = (string)_wrapped.GetValue("setName", null);
            var primary = GetPrimary();
            var version = _wrapped.Contains("version") ? (int?)_wrapped["version"].ToInt32() : null;

            return new ReplicaSetConfig(members, name, primary, version);
        }
    }
}
