# AtlasSeedExtractor

One-shot tool that extracts a curated subset of documents from an Atlas
deployment's `sample_training`, `sample_mflix`, and `sample_airbnb` databases
and emits them as C# literals in
`tests/MongoDB.Driver.Tests/Search/AtlasSearchFixtureSeedData.cs`.

The generated file is consumed by `AtlasSearchFixture` so that the Atlas
Search test suite no longer depends on pre-populated sample data on the test
cluster.

## When to rerun

Whenever new tests need additional source documents — extend the `MovieTitles`,
`EmbeddedMovieTitles`, or `AirbnbListingNames` arrays in `Program.cs` and rerun.

## Usage

```
dotnet run --project tests/Tools/AtlasSeedExtractor -- \
    --uri "mongodb://localhost:56669/?directConnection=true" \
    --out  tests/MongoDB.Driver.Tests/Search/AtlasSearchFixtureSeedData.cs
```

`--uri` falls back to the `ATLAS_SEARCH_URI` env var, then to the default
Atlas Local connection string.

The same source data plus the same arguments produce a byte-identical output
file (titles are sorted lexicographically, embeddings are emitted with `R`
round-trip formatting), so re-running on a fresh Atlas Local is safe.
