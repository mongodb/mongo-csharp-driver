/* Copyright 2010-present MongoDB Inc.
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

using System.IO;

namespace MongoDB.Driver.Core.Misc
{
    /// <summary>
    /// Abstraction for static methods in <see cref="System.IO.File" />.
    /// </summary>
    internal interface IFile
    {
        bool Exists(string name);
    }

    internal sealed class FileWrapper : IFile
    {
        public bool Exists(string name)
        {
            return File.Exists(name);
        }
    }
}
