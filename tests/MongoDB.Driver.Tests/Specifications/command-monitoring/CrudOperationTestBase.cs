/* Copyright 2010-2015 MongoDB Inc.
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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Tests.Specifications.command_monitoring
{
    public abstract class CrudOperationTestBase : ICrudOperationTest
    {
        protected ClusterDescription ClusterDescription { get; private set; }

        public virtual bool CanExecute(ClusterDescription clusterDescription, BsonDocument arguments, out string reason)
        {
            reason = null;
            return true;
        }

        public void Execute(ClusterDescription clusterDescription, IMongoDatabase database, IMongoCollection<BsonDocument> collection, BsonDocument arguments, bool async)
        {
            ClusterDescription = clusterDescription;

            foreach (var argument in arguments.Elements)
            {
                if (!TrySetArgument(argument.Name, argument.Value))
                {
                    throw new NotImplementedException("The argument " + argument.Name + " has not been implemented in " + GetType());
                }
            }

            Execute(collection, async);
        }

        public virtual BsonDocument MassageReply(BsonDocument reply)
        {
            var massagedReply = new BsonDocument(reply);
            BsonValue ok;
            if (massagedReply.TryGetValue("ok", out ok))
            {
                // have to force every ok value into a double since the server
                // hasn't chosen a consistent representation.
                massagedReply["ok"] = ok.ToDouble();
            }

            return massagedReply;
        }

        protected abstract bool TrySetArgument(string name, BsonValue value);

        protected abstract void Execute(IMongoCollection<BsonDocument> collection, bool async);
    }
}
