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
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Events.Diagnostics
{
    public class LogEnricher
    {
        public virtual string Enrich(LogLevel level, string message)
        {
            return level.ToString().PadRight(5) + " " + DateTime.UtcNow.ToString() + " " + Thread.CurrentThread.ManagedThreadId.ToString().PadRight(4) + message;
        }
    }
}