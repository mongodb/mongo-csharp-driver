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
using MongoDB.Driver.Core.Bindings;

namespace MongoDB.Driver.Core.Misc;

/// <summary>
/// Represents a feature that is not supported by all versions of the server.
/// </summary>
public class Feature
{
    #region static

    /// <summary>
    /// Gets the aggregate accumulator feature.
    /// </summary>
    public static Feature AggregateAccumulator { get; } = new("AggregateAccumulator", WireVersion.Server44);

    /// <summary>
    /// Gets the aggregate $function stage feature.
    /// </summary>
    public static Feature AggregateFunction { get; } = new("AggregateFunction", WireVersion.Server44);

    /// <summary>
    /// Gets the aggregate let feature.
    /// </summary>
    public static Feature AggregateOptionsLet { get; } = new("AggregateOptionsLet", WireVersion.Server50);

    /// <summary>
    /// Gets the aggregate out on secondary feature.
    /// </summary>
    public static Feature AggregateOutOnSecondary { get; } = new("AggregateOutOnSecondary", WireVersion.Server50);

    /// <summary>
    /// Gets the aggregate out to time series feature.
    /// </summary>
    public static Feature AggregateOutTimeSeries { get; } = new("AggregateOutTimeSeries", WireVersion.Server70);

    /// <summary>
    /// Gets the aggregate out to a different database feature.
    /// </summary>
    public static Feature AggregateOutToDifferentDatabase { get; } = new("AggregateOutToDifferentDatabase", WireVersion.Server44);

    /// <summary>
    /// Gets the aggregate unionWith feature.
    /// </summary>
    public static Feature AggregateUnionWith { get; } = new("AggregateUnionWith", WireVersion.Server44);

    /// <summary>
    /// Gets the arrayIndexAs feature for $map, $filter and $reduce.
    /// </summary>
    public static Feature ArrayIndexAs { get; } = new("ArrayIndexAs", WireVersion.Server83);

    /// <summary>
    /// Gets the bitwise operators feature.
    /// </summary>
    public static Feature BitwiseOperators { get; } = new("BitwiseOperators", WireVersion.Server63);

    /// <summary>
    /// Gets the change stream pre post images feature.
    /// </summary>
    public static Feature ChangeStreamPrePostImages { get; } = new("ChangeStreamPrePostImages", WireVersion.Server60);

    /// <summary>
    /// Gets the change stream splitEvent stage feature.
    /// </summary>
    public static Feature ChangeStreamSplitEventStage { get; } = new("ChangeStreamSplitEventStage", WireVersion.Server70);

    /// <summary>
    /// Gets the client bulk write feature.
    /// </summary>
    public static Feature ClientBulkWrite { get; } = new("ClientBulkWrite", WireVersion.Server80);

    /// <summary>
    /// Gets the clustered indexes feature.
    /// </summary>
    public static Feature ClusteredIndexes { get; } = new("ClusteredIndexes", WireVersion.Server53);

    /// <summary>
    /// Gets the $concatArrays and $setUnion accumulators feature.
    /// </summary>
    public static Feature ConcatArraysAndSetUnionAccumulators { get; } = new("ConcatArraysAndSetUnionAccumulators", WireVersion.Server81);

    /// <summary>
    /// Gets the conversion of any type to string feature.
    /// </summary>
    public static Feature ConvertOperatorAnyToString { get; } = new("ConvertOperatorAnyToString", WireVersion.Server83);

    /// <summary>
    /// Gets the base conversion in $convert feature.
    /// </summary>
    public static Feature ConvertOperatorBaseConversion { get; } = new("ConvertOperatorBaseConversion", WireVersion.Server83);

    /// <summary>
    /// Gets the conversion of binary data to/from numeric types feature.
    /// </summary>
    public static Feature ConvertOperatorBinDataToFromNumeric { get; } = new("ConvertOperatorBinDataToFromNumeric", WireVersion.Server81);

    /// <summary>
    /// Gets the conversion of binary data to/from string feature.
    /// </summary>
    public static Feature ConvertOperatorBinDataToFromString { get; } = new("ConvertOperatorBinDataToFromString", WireVersion.Server80);

    /// <summary>
    /// Gets the conversion of string to object or array feature.
    /// </summary>
    public static Feature ConvertOperatorStringToObjectOrArray { get; } = new("ConvertOperatorStringToObjectOrArray", WireVersion.Server83);

    /// <summary>
    /// Gets the create index commit quorum feature.
    /// </summary>
    public static Feature CreateIndexCommitQuorum { get; } = new("CreateIndexCommitQuorum", WireVersion.Server44);

    /// <summary>
    /// Represents support for the $createObjectId operator feature.
    /// </summary>
    public static Feature CreateObjectIdExpression { get; } = new("CreateObjectIdExpression", WireVersion.Server83);

    /// <summary>
    /// Gets the csfle range algorithm feature.
    /// </summary>
    public static Feature CsfleRangeAlgorithm { get; } = new("CsfleRangeAlgorithm", WireVersion.Server62);

    /// <summary>
    /// Gets the client side field level encryption 2 feature.
    /// </summary>
    public static Feature Csfle2 { get; } = new("Csfle2", WireVersion.Server60);

    /// <summary>
    /// Gets the client side field level encryption 2 queryable encryption v2 feature.
    /// </summary>
    public static Feature Csfle2QEv2 { get; } = new("Csfle2Qev2", WireVersion.Server70, notSupportedMessage: "Driver support of Queryable Encryption is incompatible with server. Upgrade server to use Queryable Encryption.");

    /// <summary>
    /// Gets the csfle2 $lookup support feature.
    /// </summary>
    public static Feature Csfle2QEv2Lookup { get; } = new("csfle2Qev2Lookup", WireVersion.Server81);

    /// <summary>
    /// Gets the csfle2 $lookup support feature for mixing a queryable encryption schema with a non-CSFLE JSON schema validator.
    /// </summary>
    public static Feature Csfle2QEv2LookupNonCsfleSchema => new("csfle2Qev2LookupNonCsfleSchema", WireVersion.Server82);

    /// <summary>
    /// Gets the csfle2 range algorithm feature.
    /// </summary>
    public static Feature Csfle2QEv2RangeAlgorithm { get; } = new("csfle2Qev2RangeAlgorithm", WireVersion.Server80);

    /// <summary>
    /// Gets the csfle2 string algorithm feature.
    /// </summary>
    public static Feature Csfle2QEv2StringAlgorithm { get; } = new("csfle2Qev2StringAlgorithm", WireVersion.Server90);

    /// <summary>
    /// Gets the csfle2 string (preview) query types feature (prefixPreview, suffixPreview, substringPreview).
    /// </summary>
    public static Feature Csfle2QEv2StringPreviewAlgorithm { get; } = new("csfle2Qev2StringPreviewAlgorithm", WireVersion.Server82);

    /// <summary>
    /// Gets the date operators added in 5.0 feature.
    /// </summary>
    public static Feature DateOperatorsNewIn50 { get; } = new("DateOperatorsNewIn50", WireVersion.Server50);

    /// <summary>
    /// Gets the aggregate $densify stage feature.
    /// </summary>
    public static Feature DensifyStage { get; } = new("DensifyStage", WireVersion.Server51);

    /// <summary>
    /// Gets the $deserializeEJSON operator feature.
    /// </summary>
    public static Feature DeserializeEJsonOperator { get; } = new("DeserializeEJsonOperator", WireVersion.Server83);

    /// <summary>
    /// Gets the documents stage feature.
    /// </summary>
    public static Feature DocumentsStage { get; } = new("DocumentsStage", WireVersion.Server51);

    /// <summary>
    /// Gets the directConnection setting feature.
    /// </summary>
    public static Feature DirectConnectionSetting { get; } = new("DirectConnectionSetting", WireVersion.Server44);

    /// <summary>
    /// Gets the electionIdPriorityInSDAM feature.
    /// </summary>
    public static Feature ElectionIdPriorityInSDAM { get; } = new("ElectionIdPriorityInSDAM ", WireVersion.Server60);

    /// <summary>
    /// Gets filter limit feature.
    /// </summary>
    public static Feature FilterLimit { get; } = new("FilterLimit", WireVersion.Server60);

    /// <summary>
    /// Gets the find allowDiskUse feature.
    /// </summary>
    public static Feature FindAllowDiskUse { get; } = new("FindAllowDiskUse", WireVersion.Server44);

    /// <summary>
    /// Gets the find projection expressions feature.
    /// </summary>
    public static Feature FindProjectionExpressions { get; } = new("FindProjectionExpressions", WireVersion.Server44);

    /// <summary>
    /// Gets the getField feature.
    /// </summary>
    public static Feature GetField { get; } = new("GetField", WireVersion.Server50);

    /// <summary>
    /// Gets the getMore comment feature.
    /// </summary>
    public static Feature GetMoreComment { get; } = new("GetMoreComment", WireVersion.Server44);

    /// <summary>
    /// Gets the $hash operator feature.
    /// </summary>
    public static Feature HashOperator { get; } = new("HashOperator", WireVersion.Server83);

    /// <summary>
    /// Gets the hedged reads feature.
    /// </summary>
    public static Feature HedgedReads { get; } = new("HedgedReads", WireVersion.Server44);

    /// <summary>
    /// Gets the hidden index feature.
    /// </summary>
    public static Feature HiddenIndex { get; } = new("HiddenIndex", WireVersion.Server44);

    /// <summary>
    /// Gets the hint for delete operations feature.
    /// </summary>
    public static Feature HintForDeleteOperations { get; } = new("HintForDeleteOperations", WireVersion.Server44);

    /// <summary>
    /// Gets the hint for find and modify operations feature.
    /// </summary>
    public static Feature HintForFindAndModifyOperations { get; } = new("HintForFindAndModifyOperations", WireVersion.Server44);

    /// <summary>
    /// Gets the legacy wire protocol feature.
    /// </summary>
    public static Feature LegacyWireProtocol { get; } = new("LegacyWireProtocol", WireVersion.Zero, WireVersion.Server51);

    /// <summary>
    /// Gets the load balanced mode feature.
    /// </summary>
    public static Feature LoadBalancedMode { get; } = new("LoadBalancedMode", WireVersion.Server50);

    /// <summary>
    /// Gets the lookup concise syntax feature.
    /// </summary>
    public static Feature LookupConciseSyntax { get; } = new("LookupConciseSyntax", WireVersion.Server50);

    /// <summary>
    /// Gets the lookup documents feature.
    /// </summary>
    public static Feature LookupDocuments { get; } = new("LookupDocuments", WireVersion.Server60);

    /// <summary>
    /// Gets the $median operator added in 7.0
    /// </summary>
    public static Feature MedianOperator { get; } = new("MedianOperator", WireVersion.Server70);

    /// <summary>
    /// Gets the $minMaxScaler window operator added in 8.2
    /// </summary>
    public static Feature MinMaxScalerOperator { get; } = new("MinMaxScalerOperator", WireVersion.Server82);

    /// <summary>
    /// Gets the $percentile operator added in 7.0
    /// </summary>
    public static Feature PercentileOperator { get; } = new("PercentileOperator", WireVersion.Server70);

    /// <summary>
    /// Gets the pick accumulators new in 5.2 feature.
    /// </summary>
    public static Feature PickAccumulatorsNewIn52 { get; } = new("PickAccumulatorsNewIn52", WireVersion.Server52);

    /// <summary>
    /// Gets the $rankFusion feature.
    /// </summary>
    public static Feature RankFusionStage { get; } = new("RankFusionStage", WireVersion.Server81);

    /// <summary>
    /// Gets the $replaceAll feature.
    /// </summary>
    public static Feature ReplaceAll { get; } = new("ReplaceAll", WireVersion.Server44);

    /// <summary>
    /// Gets the feature to support $replaceAll with regex as a find parameter.
    /// </summary>
    public static Feature ReplaceAllWithRegex { get; } = new("ReplaceAllWithRegex", WireVersion.Server82);

    /// <summary>
    /// Gets the $scoreFusion feature.
    /// </summary>
    public static Feature ScoreFusionStage { get; } = new("ScoreFusionStage", WireVersion.Server82);

    /// <summary>
    /// Gets the server returns resumableChangeStream label feature.
    /// </summary>
    public static Feature ServerReturnsResumableChangeStreamErrorLabel { get; } = new("ServerReturnsResumableChangeStreamErrorLabel", WireVersion.Server44);

    /// <summary>
    /// Gets the server returns retryable writeError label feature.
    /// </summary>
    public static Feature ServerReturnsRetryableWriteErrorLabel { get; } = new("ServerReturnsRetryableWriteErrorLabel", WireVersion.Server44);

    /// <summary>
    /// Gets the $serializeEJSON operator feature.
    /// </summary>
    public static Feature SerializeEJsonOperator { get; } = new("SerializeEJsonOperator", WireVersion.Server83);

    /// <summary>
    /// Gets the set window fields feature.
    /// </summary>
    public static Feature SetWindowFields { get; } = new("SetWindowFields", WireVersion.Server50);

    /// <summary>
    /// Gets the set window fields $locf feature.
    /// </summary>
    public static Feature SetWindowFieldsLocf { get; } = new("SetWindowFieldsLocf", WireVersion.Server52);

    /// <summary>
    /// Gets the $sigmoid operator feature.
    /// </summary>
    public static Feature SigmoidOperator { get; } = new("SigmoidOperator", WireVersion.Server81);

    /// <summary>
    /// Gets the similarity functions feature for $similarityDotProduct, $similarityEuclidean,
    /// and $similarityCosine.
    /// </summary>
    public static Feature SimilarityFunctions { get; } = new("SimilarityFunctions", WireVersion.Server82);

    /// <summary>
    /// Gets the snapshot reads feature.
    /// </summary>
    public static Feature SnapshotReads { get; } = new("SnapshotReads", WireVersion.Server50, notSupportedMessage: "Snapshot reads require MongoDB 5.0 or later");

    /// <summary>
    /// Gets the $sortArray operator feature.
    /// </summary>
    public static Feature SortArrayOperator { get; } = new("SortArrayOperator", WireVersion.Server52);

    /// <summary>
    /// Gets the speculative authentication feature.
    /// </summary>
    public static Feature SpeculativeAuthentication { get; } = new("SpeculativeAuthentication", WireVersion.Server44);

    /// <summary>
    /// Gets the feature to support $split with regex as a delimiter parameter.
    /// </summary>
    public static Feature SplitWithRegex { get; } = new("SplitWithRegex", WireVersion.Server82);

    /// <summary>
    /// Gets the speculative authentication feature.
    /// </summary>
    public static Feature StableApi { get; } = new("StableAPI", WireVersion.Server50);

    /// <summary>
    /// Gets the streaming hello feature.
    /// </summary>
    public static Feature StreamingHello { get; } = new("StreamingHello", WireVersion.Server44);

    /// <summary>
    /// Gets the $subtype operator feature.
    /// </summary>
    public static Feature SubtypeOperator { get; } = new("SubtypeOperator", WireVersion.Server83);

    #endregion

    private readonly int? _supportRemovedWireVersion;

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
        Name = name;
        FirstSupportedWireVersion = Ensure.IsGreaterThanOrEqualToZero(firstSupportedWireVersion, nameof(firstSupportedWireVersion));
        _supportRemovedWireVersion = Ensure.IsNullOrGreaterThanOrEqualToZero(supportRemovedWireVersion, nameof(supportRemovedWireVersion));
        NotSupportedMessage = notSupportedMessage;
    }

    /// <summary>
    /// Gets the name of the feature.
    /// </summary>
    public string Name { get; }

    internal int FirstSupportedWireVersion { get; }

    internal int LastNotSupportedWireVersion
    {
        get
        {
            return FirstSupportedWireVersion > 0 ? FirstSupportedWireVersion - 1 : throw new InvalidOperationException("There is no wire version before 0.");
        }
    }

    /// <summary>
    /// Gets the error message to be used by the feature support checks.
    /// </summary>
    public string NotSupportedMessage { get; }

    /// <summary>
    /// Throws an exception if the feature is not supported in the server used by the client.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public void ThrowIfNotSupported(IMongoClient client, CancellationToken cancellationToken = default)
    {
        var cluster = client.GetClusterInternal();
        using var session = NoCoreSession.NewHandle();
        using var operationContext = new OperationContext(session, cancellationToken: cancellationToken);
        using var binding = new ReadWriteBindingHandle(new WritableServerBinding(cluster));
        using var channelSource = binding.GetWriteChannelSource(operationContext);
        using var channel = channelSource.GetChannel(operationContext);
        // Use WireVersion from a connection since server level value may be null
        ThrowIfNotSupported(channel.ConnectionDescription.MaxWireVersion);
    }

    /// <summary>
    /// Throws an exception if the feature is not supported in the server used by the client.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task ThrowIfNotSupportedAsync(IMongoClient client, CancellationToken cancellationToken = default)
    {
        var cluster = client.GetClusterInternal();
        using var session = NoCoreSession.NewHandle();
        using var operationContext = new OperationContext(session, cancellationToken: cancellationToken);
        using var binding = new ReadWriteBindingHandle(new WritableServerBinding(cluster));
        using var channelSource = await binding.GetWriteChannelSourceAsync(operationContext).ConfigureAwait(false);
        using var channel = await channelSource.GetChannelAsync(operationContext).ConfigureAwait(false);
        // Use WireVersion from a connection since server level value may be null
        ThrowIfNotSupported(channel.ConnectionDescription.MaxWireVersion);
    }

    internal bool IsSupported(int wireVersion)
    {
        return _supportRemovedWireVersion.HasValue
            ? FirstSupportedWireVersion <= wireVersion && _supportRemovedWireVersion > wireVersion
            : FirstSupportedWireVersion <= wireVersion;
    }

    internal void ThrowIfNotSupported(int wireVersion)
    {
        if (!IsSupported(wireVersion))
        {
            string errorMessage;
            if (NotSupportedMessage != null)
            {
                errorMessage = NotSupportedMessage;
            }
            else
            {
                errorMessage = $"Server version {WireVersion.GetServerVersionForErrorMessage(wireVersion)} does not support the {Name} feature.";
            }
            throw new NotSupportedException(errorMessage);
        }
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var message = $"{Name} feature added in {FirstSupportedWireVersion} wire protocol";
        if (_supportRemovedWireVersion != null)
        {
            message += $" and removed in {_supportRemovedWireVersion} wire protocol";
        }
        return $"{message}.";
    }
}
