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

using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Driver.Linq3.Ast.Stages
{
    public sealed class AstCurrentOpStage : AstPipelineStage
    {
        private bool? _allUsers;
        private bool? _idleConnections;
        private bool? _idleCursors;
        private bool? _idleSessions;
        private bool? _localOps;

        public AstCurrentOpStage(
            bool? allUsers = null,
            bool? idleConnections = null,
            bool? idleCursors = null,
            bool? idleSessions = null,
            bool? localOps = null)
        {
            _allUsers = allUsers;
            _idleConnections = idleConnections;
            _idleCursors = idleCursors;
            _idleSessions = idleSessions;
            _localOps = localOps;
        }

        public bool? AllUsers => _allUsers;
        public bool? IdleConnections => _idleConnections;
        public bool? IdleCursors => _idleCursors;
        public bool? IdleSessions => _idleSessions;
        public bool? LocalOps => _localOps;
        public override AstNodeType NodeType => AstNodeType.CurrentOpStage;

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$currentOp", new BsonDocument
                    {
                        { "allUsers", () => _allUsers.Value, _allUsers.HasValue },
                        { "idleConnections", () => _idleConnections.Value, _idleConnections.HasValue },
                        { "idleCursors", () => _idleCursors.Value, _idleCursors.HasValue },
                        { "idleSessions", () => _idleSessions.Value, _idleSessions.HasValue },
                        { "localOps", () => _localOps.Value, _localOps.HasValue },
                    }
                }
            };
        }
    }
}
