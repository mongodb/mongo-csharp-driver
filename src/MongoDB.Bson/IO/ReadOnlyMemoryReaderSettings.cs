namespace MongoDB.Bson.IO;

/// <summary>
/// Represents settings for a <see cref="ReadOnlyMemoryBsonReader"/>.
/// </summary>
public sealed class ReadOnlyMemoryReaderSettings : BsonBinaryReaderSettings
{
    // constructors
    /// <summary>
    /// Initializes a new instance of the ReadOnlyMemoryReaderSettings class.
    /// </summary>
    public ReadOnlyMemoryReaderSettings()
    {
    }

    internal ReadOnlyMemoryReaderSettings(BsonBinaryReaderSettings readerSettings)
    {
        Encoding = readerSettings.Encoding;
        FixOldBinarySubTypeOnInput = readerSettings.FixOldBinarySubTypeOnInput;
        FixOldDateTimeMaxValueOnInput = readerSettings.FixOldDateTimeMaxValueOnInput;
        MaxDocumentSize = readerSettings.MaxDocumentSize;
    }

    // public static properties
    /// <summary>
    /// Gets the default settings for a <see cref="ReadOnlyMemoryBsonReader"/>
    /// </summary>
    public static new ReadOnlyMemoryReaderSettings Defaults { get; } = new();

    // public methods
    /// <summary>
    /// Creates a clone of the settings.
    /// </summary>
    /// <returns>A clone of the settings.</returns>
    public new ReadOnlyMemoryReaderSettings Clone() => (ReadOnlyMemoryReaderSettings)CloneImplementation();

    // protected methods
    /// <summary>
    /// Creates a clone of the settings.
    /// </summary>
    /// <returns>A clone of the settings.</returns>
    protected override BsonReaderSettings CloneImplementation() =>
        new ReadOnlyMemoryReaderSettings
        {
            Encoding = Encoding,
            FixOldBinarySubTypeOnInput = FixOldBinarySubTypeOnInput,
            FixOldDateTimeMaxValueOnInput = FixOldDateTimeMaxValueOnInput,
            MaxDocumentSize = MaxDocumentSize
        };
}
