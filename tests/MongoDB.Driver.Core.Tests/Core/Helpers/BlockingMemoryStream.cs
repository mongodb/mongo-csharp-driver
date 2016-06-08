/* Copyright 2015 MongoDB Inc.
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Helpers
{
    public class BlockingMemoryStream : MemoryStream
    {
        public override int Read(byte[] buffer, int offset, int count)
        {
            var numRead = base.Read(buffer, offset, count);
            if (numRead == 0)
            {
                Thread.Sleep(100); // 10 isn't enough, tests fail with EndOfStreamException
                numRead = base.Read(buffer, offset, count);
            }

            return numRead;
        }

        public async override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int numRead = await base.ReadAsync(buffer, offset, count, cancellationToken);
            while (numRead == 0)
            {
                await Task.Delay(10);
                numRead = await base.ReadAsync(buffer, offset, count, cancellationToken);
            }

            return numRead;
        }
    }
}