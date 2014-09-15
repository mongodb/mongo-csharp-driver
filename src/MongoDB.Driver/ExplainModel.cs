/* Copyright 2010-2014 MongoDB Inc.
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

using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Model for explaining a command.
    /// </summary>
    public class ExplainModel
    {
        // fields
        private readonly IExplainableModel _command;
        private ExplainVerbosity _verbosity;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ExplainModel"/> class.
        /// </summary>
        /// <param name="command">The command.</param>
        public ExplainModel(IExplainableModel command)
        {
            _command = Ensure.IsNotNull(command, "command");
        }

        // properties
        /// <summary>
        /// The command to explain.
        /// </summary>
        public IExplainableModel Command
        {
            get { return _command; }
        }

        /// <summary>
        /// Gets or sets the verbosity.
        /// </summary>
        public ExplainVerbosity Verbosity
        {
            get { return _verbosity; }
            set { _verbosity = value; }
        }
    }
}
