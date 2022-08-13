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

namespace MongoDB.Driver.Core.Logging
{
    internal static class StructuredLogsTemplates
    {
        public const string IdParameter = "Id";

        public const string Id_Message = "{Id} {Message}";
        public const string Id_Message_ConnectionId = "{Id} {Message} {ConnectionId}";
        public const string Id_Message_Description = "{Id} {Message} {Description}";
        public const string Id_Message_Information = "{Id} {Message} {Information}";
        public const string Id_Message_OperationId = "{Id} {Message} {OperationId}";
        public const string Id_Message_ServerId = "{Id} {Message} {ServerId}";
        public const string Id_Message_ServerId_Reason_Duration = "{Id} {Message} {ServerId} {Reason} {Duration}";
        public const string Id_Message_Reason_Duration = "{Id} {Message} {Reason} {Duration}";
        public const string Id_Message_Reason = "{Id} {Message} {ConnectionId}";
        public const string Id_Message_RequestId_CommandName = "{Id} {Message} {RequestId} {CommandName}";
        public const string Id_Message_RequestId_CommandName_Command = "{Id} {Message} {RequestId} {CommandName} {Command}";

        public const string Message_ConnectionId = "{Message} {ConnectionId}";
    }
}
