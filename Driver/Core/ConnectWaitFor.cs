/* Copyright 2010-2012 10gen Inc.
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

namespace MongoDB.Driver
{
    /// <summary>
    /// Used with the Connect method when connecting to a replica set to specify what subset of the replica set must be connected before returning.
    /// </summary>
    public enum ConnectWaitFor
    {
        /// <summary>
        /// Wait for all members of the replica set to be connected.
        /// </summary>
        All,
        /// <summary>
        /// Wait for the primary member of the replica set to be connected.
        /// </summary>
        Primary,
        /// <summary>
        /// Wait for any slaveOk member of the replica set to be connected (primary or any secondary).
        /// </summary>
        AnySlaveOk
    }
}
