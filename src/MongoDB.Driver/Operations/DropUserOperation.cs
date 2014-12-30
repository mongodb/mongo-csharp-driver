﻿/* Copyright 2010-2014 MongoDB Inc.
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

using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Operations
{
    internal class DropUserOperation : IWriteOperation<bool>
    {
        #region static
        // static fields
        private static readonly SemanticVersion __serverVersionSupportingUserManagementCommands = new SemanticVersion(2, 6, 0);
        #endregion

        // fields
        private readonly DatabaseNamespace _databaseNamespace;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly string _username;

        // constructors
        public DropUserOperation(
            DatabaseNamespace databaseNamespace,
            string username,
            MessageEncoderSettings messageEncoderSettings)
        {
            _databaseNamespace = databaseNamespace;
            _username = username;
            _messageEncoderSettings = messageEncoderSettings;
        }

        // methods
        public async Task<bool> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken)
        {
            using (var channelSource = await binding.GetWriteChannelSourceAsync(cancellationToken).ConfigureAwait(false))
            {
                IWriteOperation<bool> operation;
                if (channelSource.ServerDescription.Version >= __serverVersionSupportingUserManagementCommands)
                {
                    operation = new DropUserUsingUserManagementCommandsOperation(_databaseNamespace, _username, _messageEncoderSettings);
                }
                else
                {
                    operation = new DropUserUsingSystemUsersCollectionOperation(_databaseNamespace, _username, _messageEncoderSettings);
                }

                return await operation.ExecuteAsync(channelSource, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
