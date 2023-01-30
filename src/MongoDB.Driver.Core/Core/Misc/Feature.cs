/* Copyright 2016-present MongoDB Inc.
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

namespace MongoDB.Driver.Core.Misc
{
    /// <summary>
    /// Represents a feature that is not supported by all versions of the server.
    /// </summary>
    public class Feature
    {
        #region static
        private static readonly Feature __aggregate = new Feature("Aggregate", WireVersion.Zero);
        private static readonly Feature __aggregateAccumulator = new Feature("AggregateAccumulator", WireVersion.Server44);
        private static readonly Feature __aggregateAddFields = new Feature("AggregateAddFields", WireVersion.Server34);
        private static readonly Feature __aggregateAllowDiskUse = new Feature("AggregateAllowDiskUse", WireVersion.Server26);
        private static readonly Feature __aggregateBucketStage = new Feature("AggregateBucketStage", WireVersion.Server34);
        private static readonly Feature __aggregateComment = new Feature("AggregateComment", WireVersion.Server36);
        private static readonly Feature __aggregateCountStage = new Feature("AggregateCountStage", WireVersion.Server34);
        private static readonly Feature __aggregateCursorResult = new Feature("AggregateCursorResult", WireVersion.Server26);
        private static readonly Feature __aggregateExplain = new Feature("AggregateExplain", WireVersion.Server26);
        private static readonly Feature __aggregateFacetStage = new Feature("AggregateFacetStage", WireVersion.Server34);
        private static readonly Feature __aggregateFunction = new Feature("AggregateFunction", WireVersion.Server44);
        private static readonly Feature __aggregateGraphLookupStage = new Feature("AggregateGraphLookupStage", WireVersion.Server34);
        private static readonly Feature __aggregateHint = new Feature("AggregateHint", WireVersion.Server36);
        private static readonly Feature __aggregateOptionsLet = new Feature("AggregateOptionsLet", WireVersion.Server50);
        private static readonly Feature __aggregateLet = new Feature("AggregateLet", WireVersion.Server36);
        private static readonly Feature __aggregateMerge = new Feature("AggregateMerge", WireVersion.Server42);
        private static readonly Feature __aggregateOut = new Feature("AggregateOut", WireVersion.Server26);
        private static readonly Feature __aggregateOutOnSecondary = new Feature("AggregateOutOnSecondary", WireVersion.Server50);
        private static readonly Feature __aggregateOutToDifferentDatabase = new Feature("AggregateOutToDifferentDatabase", WireVersion.Server44);
        private static readonly Feature __aggregateToString = new Feature("AggregateToString", WireVersion.Server40);
        private static readonly Feature __aggregateUnionWith = new Feature("AggregateUnionWith", WireVersion.Server44);
        private static readonly Feature __arrayFilters = new Feature("ArrayFilters", WireVersion.Server36);
        private static readonly Feature __bypassDocumentValidation = new Feature("BypassDocumentValidation", WireVersion.Server32);
        private static readonly Feature __changeStreamStage = new Feature("ChangeStreamStage", WireVersion.Server36);
        private static readonly Feature __changeStreamPostBatchResumeToken = new Feature("ChangeStreamPostBatchResumeToken", WireVersion.Server40);
        private static readonly Feature __changeStreamPrePostImages = new Feature("ChangeStreamPrePostImages", WireVersion.Server60);
        private static readonly Feature __clientSideEncryption = new Feature("ClientSideEncryption", WireVersion.Server42);
        private static readonly Feature __clusteredIndexes = new Feature("ClusteredIndexes", WireVersion.Server53);
        private static readonly Feature __collation = new Feature("Collation", WireVersion.Server34);
        private static readonly Feature __commandMessage = new Feature("CommandMessage", WireVersion.Server36);
        private static readonly Feature __commandsThatWriteAcceptWriteConcern = new Feature("CommandsThatWriteAcceptWriteConcern", WireVersion.Server34);
        private static readonly Feature __createIndexCommitQuorum = new Feature("CreateIndexCommitQuorum", WireVersion.Server44);
        private static readonly Feature __createIndexesCommand = new Feature("CreateIndexesCommand", WireVersion.Server26);
        private static readonly Feature __createIndexesUsingInsertOperations = new Feature("CreateIndexesUsingInsertOperations", WireVersion.Zero, WireVersion.Server42);
        private static readonly Feature __csfleRangeAlgorithm = new Feature("CsfleRangeAlgorithm", WireVersion.Server62);
        private static readonly Feature __csfle2 = new Feature("Csfle2", WireVersion.Server60);
        private static readonly Feature __currentOpCommand = new Feature("CurrentOpCommand", WireVersion.Server32);
        private static readonly Feature __dateOperatorsNewIn50 = new Feature("DateOperatorsNewIn50", WireVersion.Server50);
        private static readonly Feature __densifyStage = new Feature("DensifyStage", WireVersion.Server51);
        private static readonly Feature __documentsStage = new Feature("DocumentsStage", WireVersion.Server51);
        private static readonly Feature __documentValidation = new Feature("DocumentValidation", WireVersion.Server32);
        private static readonly Feature __directConnectionSetting = new Feature("DirectConnectionSetting", WireVersion.Server44);
        private static readonly Feature __electionIdPriorityInSDAM = new Feature("ElectionIdPriorityInSDAM ", WireVersion.Server60);
        private static readonly Feature __eval = new Feature("Eval", WireVersion.Zero, WireVersion.Server42);
        private static readonly Feature __explainCommand = new Feature("ExplainCommand", WireVersion.Server30);
        private static readonly Feature __failPoints = new Feature("FailPoints", WireVersion.Zero);
        private static readonly Feature __failPointsBlockConnection = new Feature("FailPointsBlockConnection", WireVersion.Server42);
        private static readonly Feature __failPointsFailCommand = new Feature("FailPointsFailCommand", WireVersion.Server40);
        private static readonly Feature __failPointsFailCommandForSharded = new Feature("FailPointsFailCommandForSharded", WireVersion.Server42);
        private static readonly Feature __filterLimit = new Feature("FilterLimit", WireVersion.Server60);
        private static readonly Feature __findAllowDiskUse = new Feature("FindAllowDiskUse", WireVersion.Server44);
        private static readonly Feature __findAndModifyWriteConcern = new Feature("FindAndModifyWriteConcern", WireVersion.Server32);
        private static readonly Feature __findCommand = new Feature("FindCommand", WireVersion.Server32);
        private static readonly Feature __findProjectionExpressions = new Feature("FindProjectionExpressions", WireVersion.Server44);
        private static readonly Feature __geoNearCommand = new Feature("GeoNearCommand", WireVersion.Zero, WireVersion.Server42);
        private static readonly Feature __getField = new Feature("GetField", WireVersion.Server50);
        private static readonly Feature __getMoreComment = new Feature("GetMoreComment", WireVersion.Server44);
        private static readonly Feature __groupCommand = new Feature("GroupCommand", WireVersion.Zero, WireVersion.Server42);
        private static readonly Feature __hedgedReads = new Feature("HedgedReads", WireVersion.Server44);
        private static readonly Feature __hiddenIndex = new Feature("HiddenIndex", WireVersion.Server44);
        private static readonly Feature __hintForDeleteOperations = new Feature("HintForDeleteOperations", WireVersion.Server44);
        private static readonly HintForFindAndModifyFeature __hintForFindAndModifyFeature = new HintForFindAndModifyFeature("HintForFindAndModify", WireVersion.Server44);
        private static readonly Feature __hintForUpdateAndReplaceOperations = new Feature("HintForUpdateAndReplaceOperations", WireVersion.Server42);
        private static readonly Feature __keepConnectionPoolWhenNotPrimaryConnectionException = new Feature("KeepConnectionPoolWhenNotWritablePrimaryConnectionException", WireVersion.Server42);
        private static readonly Feature __keepConnectionPoolWhenReplSetStepDown = new Feature("KeepConnectionPoolWhenReplSetStepDown", WireVersion.Server42);
        private static readonly Feature __killAllSessions = new Feature("KillAllSessions", WireVersion.Server36);
        private static readonly Feature __killCursorsCommand = new Feature("KillCursorsCommand", WireVersion.Server32);
        private static readonly Feature __legacyWireProtocol = new Feature("LegacyWireProtocol", WireVersion.Zero, WireVersion.Server51);
        private static readonly Feature __listCollectionsCommand = new Feature("ListCollectionsCommand", WireVersion.Server30);
        private static readonly Feature __listDatabasesAuthorizedDatabases = new Feature("ListDatabasesAuthorizedDatabases", WireVersion.Server40);
        private static readonly Feature __listDatabasesFilter = new Feature("ListDatabasesFilter", WireVersion.Server34);
        private static readonly Feature __listDatabasesNameOnlyOption = new Feature("ListDatabasesNameOnlyOption", WireVersion.Server34);
        private static readonly Feature __listIndexesCommand = new Feature("ListIndexesCommand", WireVersion.Server30);
        private static readonly Feature __loadBalancedMode = new Feature("LoadBalancedMode", WireVersion.Server50);
        private static readonly Feature __indexOptionsDefaults = new Feature("IndexOptionsDefaults", WireVersion.Server32);
        private static readonly Feature __maxStaleness = new Feature("MaxStaleness", WireVersion.Server34);
        private static readonly Feature __maxTime = new Feature("MaxTime", WireVersion.Server26);
        private static readonly Feature __mmapV1StorageEngine = new Feature("MmapV1StorageEngine", WireVersion.Zero, WireVersion.Server42);
        private static readonly Feature __partialIndexes = new Feature("PartialIndexes", WireVersion.Server32);
        private static readonly Feature __pickAccumulatorsNewIn52 = new Feature("PickAccumulatorsNewIn52", WireVersion.Server52);
        private static readonly Feature __readConcern = new Feature("ReadConcern", WireVersion.Server32);
        private static readonly Feature __retryableReads = new Feature("RetryableReads", WireVersion.Server36);
        private static readonly Feature __retryableWrites = new Feature("RetryableWrites", WireVersion.Server36);
        private static readonly Feature __scramSha1Authentication = new Feature("ScramSha1Authentication", WireVersion.Server30);
        private static readonly Feature __scramSha256Authentication = new Feature("ScramSha256Authentication", WireVersion.Server40);
        private static readonly Feature __serverExtractsUsernameFromX509Certificate = new Feature("ServerExtractsUsernameFromX509Certificate", WireVersion.Server34);
        private static readonly Feature __serverReturnsResumableChangeStreamErrorLabel = new Feature("ServerReturnsResumableChangeStreamErrorLabel", WireVersion.Server44);
        private static readonly Feature __serverReturnsRetryableWriteErrorLabel = new Feature("ServerReturnsRetryableWriteErrorLabel", WireVersion.Server44);
        private static readonly Feature __setWindowFields = new Feature("SetWindowFields", WireVersion.Server50);
        private static readonly Feature __setWindowFieldsLocf = new Feature("SetWindowFieldsLocf", WireVersion.Server52);
        private static readonly Feature __shardedTransactions = new Feature("ShardedTransactions", WireVersion.Server42);
        private static readonly Feature __snapshotReads = new Feature("SnapshotReads", WireVersion.Server50, notSupportedMessage: "Snapshot reads require MongoDB 5.0 or later");
        private static readonly Feature __sortArrayOperator = new Feature("SortArrayOperator", WireVersion.Server52);
        private static readonly Feature __speculativeAuthentication = new Feature("SpeculativeAuthentication", WireVersion.Server44);
        private static readonly Feature __streamingHello = new Feature("StreamingHello", WireVersion.Server44);
        private static readonly Feature __tailableCursor = new Feature("TailableCursor", WireVersion.Server32);
        private static readonly Feature __toConversionOperators = new Feature("ToConversionOperators", WireVersion.Server40);
        private static readonly Feature __trigOperators = new Feature("TrigOperators", WireVersion.Server42);
        private static readonly Feature __transactions = new Feature("Transactions", WireVersion.Server40);
        private static readonly Feature __updateWithAggregationPipeline = new Feature("UpdateWithAggregationPipeline", WireVersion.Server42);
        private static readonly Feature __userManagementCommands = new Feature("UserManagementCommands", WireVersion.Server26);
        private static readonly Feature __views = new Feature("Views", WireVersion.Server34);
        private static readonly Feature __wildcardIndexes = new Feature("WildcardIndexes", WireVersion.Server42);
        private static readonly Feature __writeCommands = new Feature("WriteCommands", WireVersion.Server26);

        /// <summary>
        /// Gets the aggregate feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature Aggregate => __aggregate;

        /// <summary>
        /// Gets the aggregate accumulato feature.
        /// </summary>
        public static Feature AggregateAccumulator => __aggregateAccumulator;

        /// <summary>
        /// Gets the aggregate AddFields feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature AggregateAddFields => __aggregateAddFields;

        /// <summary>
        /// Gets the aggregate allow disk use feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature AggregateAllowDiskUse => __aggregateAllowDiskUse;

        /// <summary>
        /// Gets the aggregate bucket stage feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature AggregateBucketStage => __aggregateBucketStage;

        /// <summary>
        /// Gets the aggregate comment feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature AggregateComment => __aggregateComment;

        /// <summary>
        /// Gets the aggregate count stage feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature AggregateCountStage => __aggregateCountStage;

        /// <summary>
        /// Gets the aggregate cursor result feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature AggregateCursorResult => __aggregateCursorResult;

        /// <summary>
        /// Gets the aggregate explain feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature AggregateExplain => __aggregateExplain;

        /// <summary>
        /// Gets the aggregate $facet stage feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature AggregateFacetStage => __aggregateFacetStage;

        /// <summary>
        /// Gets the aggregate $function stage feature.
        /// </summary>
        public static Feature AggregateFunction => __aggregateFunction;

        /// <summary>
        /// Gets the aggregate $graphLookup stage feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature AggregateGraphLookupStage => __aggregateGraphLookupStage;

        /// <summary>
        /// Gets the aggregate hint feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature AggregateHint => __aggregateHint;

        /// <summary>
        /// Gets the aggregate let feature.
        /// </summary>
        public static Feature AggregateOptionsLet => __aggregateOptionsLet;

        /// <summary>
        /// Gets the aggregate lookup stage let feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature AggregateLet => __aggregateLet;

        /// <summary>
        /// Gets the aggregate merge feature.
        /// </summary>
        public static Feature AggregateMerge => __aggregateMerge;

        /// <summary>
        /// Gets the aggregate out feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature AggregateOut => __aggregateOut;

        /// <summary>
        /// Gets the aggregate out on secondary feature,
        /// </summary>
        public static Feature AggregateOutOnSecondary => __aggregateOutOnSecondary;

        /// <summary>
        /// Gets the aggregate out to a different database feature.
        /// </summary>
        public static Feature AggregateOutToDifferentDatabase => __aggregateOutToDifferentDatabase;

        /// <summary>
        /// Gets the aggregate toString feature.
        /// </summary>
        public static Feature AggregateToString => __aggregateToString;

        /// <summary>
        /// Gets the aggregate unionWith feature.
        /// </summary>
        public static Feature AggregateUnionWith => __aggregateUnionWith;

        /// <summary>
        /// Gets the arrayFilters feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature ArrayFilters => __arrayFilters;

        /// <summary>
        /// Gets the bypass document validation feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature BypassDocumentValidation => __bypassDocumentValidation;

        /// <summary>
        /// Gets the aggregate $changeStream stage feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature ChangeStreamStage => __changeStreamStage;

        /// <summary>
        /// Gets the change stream post batch resume token feature.
        /// </summary>
        public static Feature ChangeStreamPostBatchResumeToken => __changeStreamPostBatchResumeToken;

        /// <summary>
        /// Gets the change stream pre post images feature.
        /// </summary>
        public static Feature ChangeStreamPrePostImages => __changeStreamPrePostImages;

        /// <summary>
        /// Gets the client side encryption feature.
        /// </summary>
        public static Feature ClientSideEncryption => __clientSideEncryption;


        /// <summary>
        /// Gets the clustered indexes feature.
        /// </summary>
        public static Feature ClusteredIndexes => __clusteredIndexes;

        /// <summary>
        /// Gets the collation feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature Collation => __collation;

        /// <summary>
        /// Gets the command message feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature CommandMessage => __commandMessage;

        /// <summary>
        /// Gets the commands that write accept write concern feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature CommandsThatWriteAcceptWriteConcern => __commandsThatWriteAcceptWriteConcern;

        /// <summary>
        /// Gets the create index commit quorum feature.
        /// </summary>
        public static Feature CreateIndexCommitQuorum => __createIndexCommitQuorum;

        /// <summary>
        /// Gets the create indexes command feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature CreateIndexesCommand => __createIndexesCommand;

        /// <summary>
        /// Gets the create indexes using insert operations feature.
        /// </summary>
        public static Feature CreateIndexesUsingInsertOperations => __createIndexesUsingInsertOperations;

        /// <summary>
        /// Gets the csfle range algorithm feature.
        /// </summary>
        public static Feature CsfleRangeAlgorithm => __csfleRangeAlgorithm;

        /// <summary>
        /// Gets the client side field level encryption 2 feature.
        /// </summary>
        public static Feature Csfle2 => __csfle2;

        /// <summary>
        /// Gets the current op command feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature CurrentOpCommand => __currentOpCommand;

        /// <summary>
        /// Gets the date operators added in 5.0 feature.
        /// </summary>
        public static Feature DateOperatorsNewIn50 => __dateOperatorsNewIn50;

        /// <summary>
        /// Gets the aggregate $densify stage feature.
        /// </summary>
        public static Feature DensifyStage => __densifyStage;

        /// <summary>
        /// Gets the documents stage feature.
        /// </summary>
        public static Feature DocumentsStage => __documentsStage;

        /// <summary>
        /// Gets the document validation feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature DocumentValidation => __documentValidation;

        /// <summary>
        /// Gets the directConnection setting feature.
        /// </summary>
        public static Feature DirectConnectionSetting => __directConnectionSetting;

        /// <summary>
        /// Gets the electionIdPriorityInSDAM feature.
        /// </summary>
        public static Feature ElectionIdPriorityInSDAM => __electionIdPriorityInSDAM;

        /// <summary>
        /// Gets the eval feature.
        /// </summary>
        public static Feature Eval => __eval;

        /// <summary>
        /// Gets the explain command feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature ExplainCommand => __explainCommand;

        /// <summary>
        /// Gets the fail points feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature FailPoints => __failPoints;

        /// <summary>
        /// Gets the fail points block connection feature.
        /// </summary>
        public static Feature FailPointsBlockConnection => __failPointsBlockConnection;

        /// <summary>
        /// Gets the fail points fail command feature.
        /// </summary>
        public static Feature FailPointsFailCommand => __failPointsFailCommand;

        /// <summary>
        /// Gets the fail points fail command for sharded feature.
        /// </summary>
        public static Feature FailPointsFailCommandForSharded => __failPointsFailCommandForSharded;


        /// <summary>
        /// Gets filter limit feature.
        /// </summary>
        public static Feature FilterLimit => __filterLimit;

        /// <summary>
        /// Gets the find allowDiskUse feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature FindAllowDiskUse => __findAllowDiskUse;

        /// <summary>
        /// Gets the find and modify write concern feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature FindAndModifyWriteConcern => __findAndModifyWriteConcern;

        /// <summary>
        /// Gets the find command feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature FindCommand => __findCommand;

        /// <summary>
        /// Gets the find projection expressions feature.
        /// </summary>
        public static Feature FindProjectionExpressions => __findProjectionExpressions;

        /// <summary>
        /// Gets the geoNear command feature.
        /// </summary>
        public static Feature GeoNearCommand => __geoNearCommand;

        /// <summary>
        /// Gets the getField feature.
        /// </summary>
        public static Feature GetField => __getField;

        /// <summary>
        /// Gets the getMore comment feature.
        /// </summary>
        public static Feature GetMoreComment => __getMoreComment;

        /// <summary>
        /// Gets the group command feature.
        /// </summary>
        public static Feature GroupCommand => __groupCommand;

        /// <summary>
        /// Gets the hedged reads feature.
        /// </summary>
        public static Feature HedgedReads => __hedgedReads;

        /// <summary>
        /// Gets the hidden index feature.
        /// </summary>
        public static Feature HiddenIndex => __hiddenIndex;

        /// <summary>
        /// Gets the hint for delete operations feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature HintForDeleteOperations => __hintForDeleteOperations;

        /// <summary>
        /// Gets the hint for find and modify operations feature.
        /// </summary>
        public static HintForFindAndModifyFeature HintForFindAndModifyFeature => __hintForFindAndModifyFeature;

        /// <summary>
        /// Gets the hint for update and replace operations feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature HintForUpdateAndReplaceOperations => __hintForUpdateAndReplaceOperations;

        /// <summary>
        /// Gets the keep connection pool when NotPrimary connection exception feature.
        /// </summary>
        public static Feature KeepConnectionPoolWhenNotPrimaryConnectionException => __keepConnectionPoolWhenNotPrimaryConnectionException;

        /// <summary>
        /// Gets the keep connection pool when NotPrimary connection exception feature.
        /// </summary>
        [Obsolete("Use KeepConnectionPoolWhenNotPrimaryConnectionException instead.")]
        public static Feature KeepConnectionPoolWhenNotMasterConnectionException => __keepConnectionPoolWhenNotPrimaryConnectionException;

        /// <summary>
        /// Gets the keep connection pool when replSetStepDown feature.
        /// </summary>
        public static Feature KeepConnectionPoolWhenReplSetStepDown => __keepConnectionPoolWhenReplSetStepDown;

        /// <summary>
        /// Get the killAllSessions feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature KillAllSessions => __killAllSessions;

        /// <summary>
        /// Get the killCursors command feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature KillCursorsCommand => __killCursorsCommand;

        /// <summary>
        /// Gets the index options defaults feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature IndexOptionsDefaults => __indexOptionsDefaults;

        /// <summary>
        /// Gets the legacy wire protocol feature.
        /// </summary>
        public static Feature LegacyWireProtocol => __legacyWireProtocol;

        /// <summary>
        /// Get the list databases authorizedDatabases feature.
        /// </summary>
        public static Feature ListDatabasesAuthorizedDatabases => __listDatabasesAuthorizedDatabases;

        /// <summary>
        /// Gets the list databases filter feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature ListDatabasesFilter => __listDatabasesFilter;

        /// <summary>
        /// Get the list databases nameOnly feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature ListDatabasesNameOnlyOption => __listDatabasesNameOnlyOption;

        /// <summary>
        /// Gets the list collections command feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature ListCollectionsCommand => __listCollectionsCommand;

        /// <summary>
        /// Gets the list indexes command feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature ListIndexesCommand => __listIndexesCommand;

        /// <summary>
        /// Gets the load balanced mode feature.
        /// </summary>
        public static Feature LoadBalancedMode => __loadBalancedMode;

        /// <summary>
        /// Gets the maximum staleness feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature MaxStaleness => __maxStaleness;

        /// <summary>
        /// Gets the maximum time feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature MaxTime => __maxTime;

        /// <summary>
        /// Gets the mmapv1 storage engine feature.
        /// </summary>
        public static Feature MmapV1StorageEngine => __mmapV1StorageEngine;

        /// <summary>
        /// Gets the partial indexes feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature PartialIndexes => __partialIndexes;

        /// <summary>
        /// Gets the pick accumulators new in 5.2 feature.
        /// </summary>
        public static Feature PickAccumulatorsNewIn52 => __pickAccumulatorsNewIn52;

        /// <summary>
        /// Gets the read concern feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature ReadConcern => __readConcern;

        /// <summary>
        /// Gets the retryable reads feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature RetryableReads => __retryableReads;

        /// <summary>
        /// Gets the retryable writes feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature RetryableWrites => __retryableWrites;

        /// <summary>
        /// Gets the scram sha1 authentication feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature ScramSha1Authentication => __scramSha1Authentication;

        /// <summary>
        /// Gets the scram sha256 authentication feature.
        /// </summary>
        public static Feature ScramSha256Authentication => __scramSha256Authentication;

        /// <summary>
        /// Gets the server extracts username from X509 certificate feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature ServerExtractsUsernameFromX509Certificate => __serverExtractsUsernameFromX509Certificate;

        /// <summary>
        /// Gets the server returns resumableChangeStream label feature.
        /// </summary>
        public static Feature ServerReturnsResumableChangeStreamErrorLabel => __serverReturnsResumableChangeStreamErrorLabel;

        /// <summary>
        /// Gets the server returns retryable writeError label feature.
        /// </summary>
        public static Feature ServerReturnsRetryableWriteErrorLabel => __serverReturnsRetryableWriteErrorLabel;

        /// <summary>
        /// Gets the set window fields feature.
        /// </summary>
        public static Feature SetWindowFields => __setWindowFields;

        /// <summary>
        /// Gets the set window fields $locf feature.
        /// </summary>
        public static Feature SetWindowFieldsLocf => __setWindowFieldsLocf;

        /// <summary>
        /// Gets the sharded transactions feature.
        /// </summary>
        public static Feature ShardedTransactions => __shardedTransactions;

        /// <summary>
        /// Gets the snapshot reads feature.
        /// </summary>
        public static Feature SnapshotReads => __snapshotReads;

        /// <summary>
        /// Gets the $sortArray operator feature.
        /// </summary>
        public static Feature SortArrayOperator => __sortArrayOperator;

        /// <summary>
        /// Gets the speculative authentication feature.
        /// </summary>
        public static Feature SpeculativeAuthentication => __speculativeAuthentication;

        /// <summary>
        /// Gets the streaming hello feature.
        /// </summary>
        public static Feature StreamingHello => __streamingHello;

        /// <summary>
        /// Gets the streaming hello feature.
        /// </summary>
        [Obsolete("Use StreamingHello instead.")]
        public static Feature StreamingIsMaster => __streamingHello;

        /// <summary>
        /// Gets the tailable cursor feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature TailableCursor => __tailableCursor;

        /// <summary>
        /// Gets the $toXyz conversion operators feature ($toDouble etc.).
        /// </summary>
        public static Feature ToConversionOperators => __toConversionOperators;

        /// <summary>
        /// Gets the transactions feature.
        /// </summary>
        public static Feature Transactions => __transactions;

        /// <summary>
        /// Gets the trig operators feature.
        /// </summary>
        public static Feature TrigOperators => __trigOperators;

        /// <summary>
        /// Gets the user management commands feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature UserManagementCommands => __userManagementCommands;

        /// <summary>
        /// Gets the update with aggregation pipeline feature.
        /// </summary>
        public static Feature UpdateWithAggregationPipeline => __updateWithAggregationPipeline;

        /// <summary>
        /// Gets the views feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature Views => __views;

        /// <summary>
        /// Gets the wildcard indexes feature.
        /// </summary>
        public static Feature WildcardIndexes => __wildcardIndexes;

        /// <summary>
        /// Gets the write commands feature.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public static Feature WriteCommands => __writeCommands;
        #endregion

        private readonly string _name;
        private readonly int _firstSupportedWireVersion;
        private readonly int? _supportRemovedWireVersion;
        private readonly string _notSupportedMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="Feature" /> class.
        /// </summary>
        /// <param name="name">The name of the feature.</param>
        /// <param name="firstSupportedWireVersion">The first wire version that supports the feature.</param>
        /// <param name="supportRemovedWireVersion">The wire version that stops support the feature.</param>
        /// <param name="notSupportedMessage">The not supported error message.</param>
        public Feature(string name,
            int firstSupportedWireVersion,
            int? supportRemovedWireVersion = null,
            string notSupportedMessage = null)
        {
            _name = name;
            _firstSupportedWireVersion = Ensure.IsGreaterThanOrEqualToZero(firstSupportedWireVersion, nameof(firstSupportedWireVersion));
            _supportRemovedWireVersion = Ensure.IsNullOrGreaterThanOrEqualToZero(supportRemovedWireVersion, nameof(supportRemovedWireVersion));
            _notSupportedMessage = notSupportedMessage;
        }

        /// <summary>
        /// Gets the name of the feature.
        /// </summary>
        public string Name => _name;

        internal int FirstSupportedWireVersion => _firstSupportedWireVersion;

        internal int LastNotSupportedWireVersion
        {
            get
            {
                return _firstSupportedWireVersion > 0 ? _firstSupportedWireVersion - 1 : throw new InvalidOperationException("There is no wire version before 0.");
            }
        }

        /// <summary>
        /// Gets the error message to be used by the feature support checks.
        /// </summary>
        public string NotSupportedMessage => _notSupportedMessage;

        internal bool IsSupported(int wireVersion)
        {
            return _supportRemovedWireVersion.HasValue
                ? _firstSupportedWireVersion <= wireVersion && _supportRemovedWireVersion > wireVersion
                : _firstSupportedWireVersion <= wireVersion;
        }

        internal void ThrowIfNotSupported(int wireVersion)
        {
            if (!IsSupported(wireVersion))
            {
                string errorMessage;
                if (_notSupportedMessage != null)
                {
                    errorMessage = _notSupportedMessage; ;
                }
                else
                {
                    errorMessage = $"Server version {WireVersion.GetServerVersionForErrorMessage(wireVersion)} does not support the {_name} feature.";
                }
                throw new NotSupportedException(errorMessage);
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var message = $"{_name} feature added in {_firstSupportedWireVersion} wire protocol";
            if (_supportRemovedWireVersion != null)
            {
                message += $" and removed in {_supportRemovedWireVersion} wire protocol";
            }
            return $"{message}.";
        }
    }
}
