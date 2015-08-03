/* Copyright 2010-2015 MongoDB Inc.
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
using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents arguments for the Eval command helper method.
    /// </summary>
    public class EvalArgs
    {
        // private fields
        private IEnumerable<BsonValue> _args;
        private BsonJavaScript _code;
        private bool? _lock;
        private TimeSpan? _maxTime;

        // public properties
        /// <summary>
        /// Gets or sets the arguments to the JavaScript code.
        /// </summary>
        public IEnumerable<BsonValue> Args
        {
            get { return _args; }
            set { _args = value.ToList(); }
        }

        /// <summary>
        /// Gets or sets the JavaScript code.
        /// </summary>
        public BsonJavaScript Code
        {
            get { return _code; }
            set { _code = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server should take a global write lock when executing the JavaScript code.
        /// </summary>
        public bool? Lock
        {
            get { return _lock; }
            set { _lock = value; }
        }

        /// <summary>
        /// Gets or sets the max time.
        /// </summary>
        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = value; }
        }
    }
}
