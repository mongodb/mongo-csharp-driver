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
                Thread.Sleep(10);
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