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

namespace MongoDB.Driver
{
    /// <summary>
    /// Change stream FullDocumentBeforeChange option.
    /// </summary>
    public enum ChangeStreamFullDocumentBeforeChangeOption
    {
        /// <summary>
        /// Do not send this option to the server.
        /// Server's default is to not return the full document before change.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Do not return the full document before change.
        /// </summary>
        Off,

        /// <summary>
        /// Returns the pre-image of the modified document for replace, update and delete
        /// change events if the pre-image for this event is available.
        /// </summary>
        WhenAvailable,

        /// <summary>
        /// Same behavior as 'whenAvailable' except that
        /// an error is raised if the pre-image is not available.
        /// </summary>
        Required
    }
}
