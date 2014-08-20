using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver.Core.FunctionalTests.Helpers
{
    public static class BulkWriteUpsertEqualityComparer
    {
        public static bool Equals(BulkWriteUpsert x, BulkWriteUpsert y)
        {
            return
                object.Equals(x.Id, x.Id) &&
                x.Index == y.Index;
        }
    }
}
