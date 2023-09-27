using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Search;

/// <summary>
///  A synonym mapping definition for search queries
/// </summary>
public sealed class SearchSynonymMappingDefinition
{
    private readonly string _synonymsMapName;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchSynonymMappingDefinition"/> class.
    /// </summary>
    /// <param name="synonymsMapName">The name of the synonym mapping definition</param>
    public SearchSynonymMappingDefinition(string synonymsMapName)
    {
        _synonymsMapName = Ensure.IsNotNullOrEmpty(synonymsMapName, nameof(synonymsMapName));
    }

    /// <summary>
    /// Performs an implicit conversion from a string to <see cref="SearchSynonymMappingDefinition"/>.
    /// </summary>
    /// <param name="synonymsMapName">The name of the synonym mapping definition</param>
    /// <returns>
    /// The result of the conversion.
    /// </returns>
    public static implicit operator SearchSynonymMappingDefinition(string synonymsMapName) =>
        new(synonymsMapName);

    /// <summary>
    /// Renders the synonym mapping definition to a <see cref="BsonValue"/>.
    /// </summary>
    /// <returns>A <see cref="BsonValue"/>.</returns>
    public BsonValue Render() => new BsonString(_synonymsMapName);
}
