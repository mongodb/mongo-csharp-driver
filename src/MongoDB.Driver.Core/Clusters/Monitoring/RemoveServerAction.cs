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
using System.Net;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Clusters.Monitoring
{
    public class RemoveServerAction : TransitionAction, IEquatable<RemoveServerAction>
    {
        // fields
        private readonly EndPoint _endPoint;

        // constructors
        public RemoveServerAction(EndPoint endPoint)
            : base(TransitionActionType.RemoveServer)
        {
            _endPoint = Ensure.IsNotNull(endPoint, "endPoint");
        }

        // properties
        public EndPoint EndPoint
        {
            get { return _endPoint; }
        }

        // methods
        public override bool Equals(object obj)
        {
            return Equals(obj as RemoveServerAction);
        }

        public bool Equals(RemoveServerAction rhs)
        {
            if (object.ReferenceEquals(rhs, null) || rhs.GetType() != typeof(RemoveServerAction))
            {
                return false;
            }

            return _endPoint.Equals(rhs._endPoint);
        }

        public override int GetHashCode()
        {
            return _endPoint.GetHashCode();
        }
    }
}
