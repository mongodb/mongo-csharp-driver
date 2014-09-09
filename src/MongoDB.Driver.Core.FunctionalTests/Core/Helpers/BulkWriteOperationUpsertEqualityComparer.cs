using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver.Core.Helpers
{
    public static class BulkWriteOperationUpsertEqualityComparer
    {
        public static bool Equals(BulkWriteOperationUpsert x, BulkWriteOperationUpsert y)
        {
            return
                object.Equals(x.Id, x.Id) &&
                x.Index == y.Index;
        }
    }
}
