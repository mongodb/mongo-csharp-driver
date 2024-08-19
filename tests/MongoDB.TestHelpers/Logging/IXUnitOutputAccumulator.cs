using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace MongoDB.TestHelpers.Logging
{
    public interface IXUnitOutputAccumulator
    {
        void Log(LogLevel logLevel,
            string category,
            IEnumerable<KeyValuePair<string, object>> state,
            Exception exception,
            Func<object, Exception, string> formatter);
    }
}
