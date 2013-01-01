/* Copyright 2010-2013 10gen Inc.
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

namespace MongoDB.Bson.IO
{
    /// <summary>
    /// Represents the output mode of a JsonWriter.
    /// </summary>
    public enum JsonOutputMode
    {
        /// <summary>
        /// Output strict JSON.
        /// </summary>
        Strict,
        /// <summary>
        /// Use JavaScript data types for some values.
        /// </summary>
        JavaScript,
        /// <summary>
        /// Use JavaScript and 10gen data types for some values.
        /// </summary>
        TenGen,
        /// <summary>
        /// Use a format that can be pasted in to the MongoDB shell.
        /// </summary>
        Shell
    }
}
