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
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
    public class EvalOperation : IReadOperation<BsonValue>
    {
        // fields
        private readonly string _databaseName;
        private readonly BsonJavaScript _javaScript;
        private TimeSpan? _maxTime;
        private readonly bool? _nolock;

        // constructors
        public EvalOperation(
            string databaseName,
            BsonJavaScript javaScript)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _javaScript = Ensure.IsNotNull(javaScript, "javaScript");
        }

        private EvalOperation(
            string databaseName,
            BsonJavaScript javaScript,
            bool? nolock)
        {
            _databaseName = databaseName;
            _javaScript = javaScript;
            _nolock = nolock;
        }

        // properties
        public string DatabaseName
        {
            get { return _databaseName; }
        }

        public BsonJavaScript JavaScript
        {
            get { return _javaScript; }
        }

        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = value; }
        }

        public bool? Nolock
        {
            get { return _nolock; }
        }

        // methods
        public BsonDocument CreateCommand()
        {
            var javaScriptWithScope = _javaScript as BsonJavaScriptWithScope;
            return new BsonDocument
            {
                { "$eval", _javaScript.Code },
                { "args", () => javaScriptWithScope.Scope, javaScriptWithScope != null },
                { "nolock", () => _nolock.Value, _nolock.HasValue },
                { "maxTimeMS", () => _maxTime.Value.TotalMilliseconds, _maxTime.HasValue }
            };
        }

        public async Task<BsonValue> ExecuteAsync(IReadBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var operation = new ReadCommandOperation(_databaseName, command);
            var result = await operation.ExecuteAsync(binding, timeout, cancellationToken);
            return result["retval"];
        }

        public EvalOperation WithDatabaseName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (_databaseName == value) ? this : new EvalOperation(value, _javaScript, _nolock);
        }

        public EvalOperation WithJavaScript(BsonJavaScript value)
        {
            Ensure.IsNotNull(value, "value");
            return (_javaScript == value) ? this : new EvalOperation(_databaseName, value, _nolock);
        }

        public EvalOperation WithNoLock(bool? value)
        {
            return (_nolock == value) ? this : new EvalOperation(_databaseName, _javaScript, value);
        }
    }
}
