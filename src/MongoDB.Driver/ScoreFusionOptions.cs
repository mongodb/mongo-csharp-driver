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

using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver;

/// <summary>
/// Specifies how raw scores from each input pipeline of a $scoreFusion stage are normalized before combination.
/// </summary>
public enum ScoreFusionNormalization
{
    /// <summary>
    /// Use the raw input scores without normalization.
    /// </summary>
    None,

    /// <summary>
    /// Apply the sigmoid function to map each score to a value in (0, 1).
    /// </summary>
    Sigmoid,

    /// <summary>
    /// Rescale each score linearly to the range [0, 1] based on the minimum and maximum scores in the result set.
    /// </summary>
    MinMaxScaler
}

/// <summary>
/// Specifies how the (normalized) scores from each input pipeline of a $scoreFusion stage are combined into a final score.
/// </summary>
public enum ScoreFusionCombinationMethod
{
    /// <summary>
    /// Average the (weighted) input pipeline scores. This is the server default when no method is specified.
    /// </summary>
    Avg,

    /// <summary>
    /// Combine input pipeline scores using a user-supplied aggregation expression.
    /// </summary>
    Expression
}

/// <summary>
/// Represents options for the $scoreFusion stage.
/// </summary>
/// <typeparam name="TOutput">The type of the output documents.</typeparam>
public sealed class ScoreFusionOptions<TOutput>
{
    /// <summary>
    /// Gets or sets the output serializer.
    /// </summary>
    public IBsonSerializer<TOutput> OutputSerializer { get; set; }

    /// <summary>
    /// Flag that specifies whether to make a detailed breakdown of the score for each document available as metadata.
    /// Setting this to true adds score information accessible via $meta, which can then be optionally projected in results.
    /// </summary>
    public bool ScoreDetails { get; set; }

    /// <summary>
    /// Gets or sets the method used to combine scores from each input pipeline.
    /// When null, no method is emitted and the server defaults to <see cref="ScoreFusionCombinationMethod.Avg"/>.
    /// </summary>
    public ScoreFusionCombinationMethod? CombinationMethod { get; set; }

    /// <summary>
    /// Gets or sets the aggregation expression used to combine scores when
    /// <see cref="CombinationMethod"/> is <see cref="ScoreFusionCombinationMethod.Expression"/>.
    /// Pipeline scores are referenced by name as <c>$$pipelineName</c> variables.
    /// </summary>
    public BsonDocument CombinationExpression { get; set; }
}
