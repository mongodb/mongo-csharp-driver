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

using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Authentication.Oidc
{
    internal sealed class FileOidcCallback : IOidcCallback
    {
        private readonly IFileSystemProvider _fileSystemProvider;

        #region static
        public static FileOidcCallback CreateFromEnvironmentVariable(
            IEnvironmentVariableProvider environmentVariableProvider,
            IFileSystemProvider fileSystemProvider,
            string[] environmentVariableNames,
            string defaultPath = null)
        {
            Ensure.IsNotNull(environmentVariableProvider, nameof(environmentVariableProvider));
            Ensure.IsNotNull(fileSystemProvider, nameof(fileSystemProvider));
            Ensure.IsNotNullOrEmpty(environmentVariableNames, nameof(environmentVariableNames));

            string filePath = null;
            foreach (var variableName in environmentVariableNames)
            {
                filePath = environmentVariableProvider.GetEnvironmentVariable(variableName);
                if (!string.IsNullOrEmpty(filePath))
                {
                    break;
                }
            }

            filePath ??= defaultPath;
            return new FileOidcCallback(fileSystemProvider, filePath);
        }
        #endregion

        public FileOidcCallback(IFileSystemProvider fileSystemProvider, string filePath)
        {
            _fileSystemProvider = Ensure.IsNotNull(fileSystemProvider, nameof(fileSystemProvider));
            FilePath = Ensure.IsNotNullOrEmpty(filePath, nameof(filePath));
        }

        public string FilePath { get; }

        public OidcAccessToken GetOidcAccessToken(OidcCallbackParameters parameters, CancellationToken cancellationToken)
        {
            var accessToken = _fileSystemProvider.File.ReadAllText(FilePath);
            return new(accessToken, expiresIn: null);
        }

        public async Task<OidcAccessToken> GetOidcAccessTokenAsync(OidcCallbackParameters parameters, CancellationToken cancellationToken)
        {
            var accessToken = await _fileSystemProvider.File.ReadAllTextAsync(FilePath).ConfigureAwait(false);
            return new(accessToken, expiresIn: null);
        }
    }
}
