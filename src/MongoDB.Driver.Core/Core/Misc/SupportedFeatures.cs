/* Copyright 2015 MongoDB Inc.
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

namespace MongoDB.Driver.Core.Misc
{
    /// <summary>
    /// Represents which features are supported.
    /// </summary>
    internal static class SupportedFeatures
    {
        // private static fields
        private static readonly SemanticVersion __versionSupportingAggregateCursorResult = new SemanticVersion(2, 6, 0);
        private static readonly SemanticVersion __versionSupportingAggregateOut = new SemanticVersion(2, 6, 0);
        private static readonly SemanticVersion __versionSupportingBypassDocumentValidation = new SemanticVersion(3, 1, 3);
        private static readonly SemanticVersion __versionSupportingCreateIndexesCommand = new SemanticVersion(2, 7, 6);
        private static readonly SemanticVersion __versionSupportingCurrentOpCommand = new SemanticVersion(3, 1, 2);
        private static readonly SemanticVersion __versionSupportingFailPoints = new SemanticVersion(2, 4, 0);
        private static readonly SemanticVersion __versionSupportingFindAndModifyWriteConcern = new SemanticVersion(3, 1, 1);
        private static readonly SemanticVersion __versionSupportingFindCommand = new SemanticVersion(3, 1, 5);
        private static readonly SemanticVersion __versionSupportingListCollectionsCommand = new SemanticVersion(2, 7, 6);
        private static readonly SemanticVersion __versionSupportingListIndexesCommand = new SemanticVersion(2, 7, 6);
        private static readonly SemanticVersion __versionSupportingMaxTime = new SemanticVersion(2, 6, 0);
        private static readonly SemanticVersion __versionSupportingReadConcern = new SemanticVersion(3, 1, 7);
        private static readonly SemanticVersion __versionSupportingScramSha1Authentication = new SemanticVersion(2, 7, 5);
        private static readonly SemanticVersion __versionSupportingWriteCommands = new SemanticVersion(2, 6, 0);
        private static readonly SemanticVersion __versionSupportingUserManagementCommands = new SemanticVersion(2, 6, 0);

        // public static methods
        /// <summary>
        /// Determines whether fail points are supported.
        /// </summary>
        /// <param name="serverVersion">The server version.</param>
        /// <returns>True if fail points are supported.</returns>
        public static bool AreFailPointsSupported(SemanticVersion serverVersion)
        {
            return serverVersion >= __versionSupportingFailPoints;
        }

        /// <summary>
        /// Determines whether user management commands are supported.
        /// </summary>
        /// <param name="serverVersion">The server version.</param>
        /// <returns>True if user management commands are supported.</returns>
        public static bool AreUserManagementCommandsSupported(SemanticVersion serverVersion)
        {
            return serverVersion >= __versionSupportingUserManagementCommands;
        }

        /// <summary>
        /// Determines whether write commands are supported.
        /// </summary>
        /// <param name="serverVersion">The server version.</param>
        /// <returns>True if write commands are supported.</returns>
        public static bool AreWriteCommandsSupported(SemanticVersion serverVersion)
        {
            return serverVersion >= __versionSupportingWriteCommands;
        }

        /// <summary>
        /// Determines whether aggregate cursor result is supported.
        /// </summary>
        /// <param name="serverVersion">The server version.</param>
        /// <returns>True if aggregate cursor result is supported.</returns>
        public static bool IsAggregateCursorResultSupported(SemanticVersion serverVersion)
        {
            return serverVersion >= __versionSupportingAggregateCursorResult;
        }

        /// <summary>
        /// Determines whether aggregate $out is supported.
        /// </summary>
        /// <param name="serverVersion">The server version.</param>
        /// <returns>True if aggregate $out is supported.</returns>
        public static bool IsAggregateOutSupported(SemanticVersion serverVersion)
        {
            return serverVersion >= __versionSupportingAggregateOut;
        }

        /// <summary>
        /// Determines whether BypassDocumentValidation is supported.
        /// </summary>
        /// <param name="serverVersion">The server version.</param>
        /// <returns>True if BypassDocumentValidation is supported.</returns>
        public static bool IsBypassDocumentValidationSupported(SemanticVersion serverVersion)
        {
            return serverVersion >= __versionSupportingBypassDocumentValidation;
        }

        /// <summary>
        /// Determines whether the CreateIndexes command is supported.
        /// </summary>
        /// <param name="serverVersion">The server version.</param>
        /// <returns>True if the CreateIndexes comamnd is supported.</returns>
        public static bool IsCreateIndexesCommandSupported(SemanticVersion serverVersion)
        {
            return serverVersion >= __versionSupportingCreateIndexesCommand;
        }

        /// <summary>
        /// Determines whether the CurrentOp command is supported.
        /// </summary>
        /// <param name="serverVersion">The server version.</param>
        /// <returns>True if the CurrentOp comamnd is supported.</returns>
        public static bool IsCurrentOpCommandSupported(SemanticVersion serverVersion)
        {
            return serverVersion >= __versionSupportingCurrentOpCommand;
        }

        /// <summary>
        /// Determines whether FindAndModify WriteConcern is supported.
        /// </summary>
        /// <param name="serverVersion">The server version.</param>
        /// <returns>True FindAndModify WriteConcern is supported.</returns>
        public static bool IsFindAndModifyWriteConcernSupported(SemanticVersion serverVersion)
        {
            return serverVersion >= __versionSupportingFindAndModifyWriteConcern;
        }


        /// <summary>
        /// Determines whether the Find command is supported.
        /// </summary>
        /// <param name="serverVersion">The server version.</param>
        /// <returns>True if the Find comamnd is supported.</returns>
        public static bool IsFindCommandSupported(SemanticVersion serverVersion)
        {
            return serverVersion >= __versionSupportingFindCommand;
        }

        /// <summary>
        /// Determines whether the ListCollections command is supported.
        /// </summary>
        /// <param name="serverVersion">The server version.</param>
        /// <returns>True if the ListCollections comamnd is supported.</returns>
        public static bool IsListCollectionsCommandSupported(SemanticVersion serverVersion)
        {
            return serverVersion >= __versionSupportingListCollectionsCommand;
        }

        /// <summary>
        /// Determines whether the ListIndexes command is supported.
        /// </summary>
        /// <param name="serverVersion">The server version.</param>
        /// <returns>True if the ListIndexes comamnd is supported.</returns>
        public static bool IsListIndexesCommandSupported(SemanticVersion serverVersion)
        {
            return serverVersion >= __versionSupportingListIndexesCommand;
        }

        /// <summary>
        /// Determines whether ReadConcern is supported.
        /// </summary>
        /// <param name="serverVersion">The server version.</param>
        /// <returns>True if ReadConcern is supported.</returns>
        public static bool IsReadConcernSupported(SemanticVersion serverVersion)
        {
            return serverVersion >= __versionSupportingReadConcern;
        }

        /// <summary>
        /// Determines whether MaxTime is supported.
        /// </summary>
        /// <param name="serverVersion">The server version.</param>
        /// <returns>True if MaxTime is supported.</returns>
        public static bool IsMaxTimeSupported(SemanticVersion serverVersion)
        {
            return serverVersion >= __versionSupportingMaxTime;
        }

        /// <summary>
        /// Determines whether SCRAM SHA1 authentication is supported.
        /// </summary>
        /// <param name="serverVersion">The server version.</param>
        /// <returns>True if SCRAM SHA1 authentication is supported.</returns>
        public static bool IsScramSha1AuthenticationSupported(SemanticVersion serverVersion)
        {
            return serverVersion >= __versionSupportingScramSha1Authentication;
        }
    }
}
