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

namespace MongoDB.Driver.Internal
{
    internal enum MessageOpcode
    {
        Reply = 1,
        Message = 1000,
        Update = 2001,
        Insert = 2002,
        Query = 2004,
        GetMore = 2005,
        Delete = 2006,
        KillCursors = 2007
    }
}
