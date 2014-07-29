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
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Events
{
    public class NoOpMessageListener : IMessageListener
    {
        #region static
        // static fields
        private static readonly Task __completedTask = Task.FromResult(true);
        #endregion

        // methods
        public void ReceivedMessage(ReceivedMessageEventArgs args)
        {
        }

        public void SendingMessage(SendingMessageEventArgs args)
        {
        }

        public void SentMessage(SentMessageEventArgs args)
        {
        }
    }
}
