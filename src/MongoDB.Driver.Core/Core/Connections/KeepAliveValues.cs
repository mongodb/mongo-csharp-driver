/* Copyright 2018-present MongoDB Inc.
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

namespace MongoDB.Driver.Core.Connections
{
    // see the tcp_keepalive struct at the following page for documentation of the buffer layout
    // https://msdn.microsoft.com/en-us/library/windows/desktop/dd877220(v=vs.85).aspx
    // note: a u_long in C is 4 bytes so the C# equivalent is uint

    internal struct KeepAliveValues
    {
        public uint OnOff { get; set; }
        public uint KeepAliveTime { get; set; }
        public uint KeepAliveInterval { get; set; }

        public byte[] ToBytes()
        {
            var bytes = new byte[12];
            Array.Copy(BitConverter.GetBytes(OnOff), 0, bytes, 0, 4);
            Array.Copy(BitConverter.GetBytes(KeepAliveTime), 0, bytes, 4, 4);
            Array.Copy(BitConverter.GetBytes(KeepAliveInterval), 0, bytes, 8, 4);
            return bytes;
        }
    }
}
