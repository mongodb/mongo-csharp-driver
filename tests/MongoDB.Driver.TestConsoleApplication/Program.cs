/* Copyright 2010-2016 MongoDB Inc.
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
using System.IO;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events.Diagnostics;

namespace MongoDB.Driver.TestConsoleApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            //FilterMeasuring.TestAsync().GetAwaiter().GetResult();
            int numConcurrentWorkers = 50;
            //new CoreApi().Run(numConcurrentWorkers, ConfigureCluster);
            new CoreApiSync().Run(numConcurrentWorkers, ConfigureCluster);

            new Api().Run(numConcurrentWorkers, ConfigureCluster);

            //new LegacyApi().Run(numConcurrentWorkers, ConfigureCluster);
        }

        private static void ConfigureCluster(ClusterBuilder cb)
        {
#if NET45
            cb.UsePerformanceCounters("test", true);
#endif
        }
    }
}