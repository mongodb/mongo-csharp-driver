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
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders
{
    /// <summary>
    /// Represents a message encoder.
    /// </summary>
    public interface IMessageEncoder
    {
        MongoDBMessage ReadMessage();
        void WriteMessage(MongoDBMessage message);
    }

    /// <summary>
    /// Represents a message encoder.
    /// </summary>
    public interface IMessageEncoder<TMessage> : IMessageEncoder where TMessage : MongoDBMessage
    {
        new TMessage ReadMessage();
        void WriteMessage(TMessage message);
    }
}
