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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MongoDB.Driver.Tests.Tools.AtlasSeedExtractor;

internal static class Program
{
    // ---------------------------------------------------------------------
    // Title lists. These drive the queries against Atlas Local. Order does
    // not matter; the extractor sorts by `title` for deterministic output.
    // ---------------------------------------------------------------------

    // Titles referenced by AtlasSearchTests.cs (sample_mflix.movies).
    private static readonly string[] MovieTitles =
    [
        // EqualsArrayField (top-3 with genre Family, alpha order from index)
        "The Poor Little Rich Girl", "Robin Hood", "Peter Pan",
        // EqualsStringField
        "A Corner in Wheat",
        // In (runtime 31 and 231)
        "Kung Fury", "Home from Home: Chronicle of a Vision",
        // RangeString — alpha range "city" < title < "country"
        "Civilization", "Clash of the Wolves", "City Lights", "Comradeship", "Come and Get It",
        // SearchSequenceToken — title:"flower"
        "Equinox Flower", "Flower Drum Song", "Cactus Flower", "The Flower of My Secret",
        // Sort_MetaSearchScore — text "dance"
        "Invitation to the Dance",
        // PhraseSynonym — plot:"automobile race" via transportSynonyms
        "The Great Race", "The Cannonball Run", "National Mechanics", "Speedway Junky", "Jo pour Jonathan",
        // Synonyms — title:"automobile" / "boat" via transportSynonyms
        "Blue Car", "And the Ship Sails On",
        // TextMatchCriteria — plot:"attire" via attireSynonyms
        "The Royal Tailor", "La guerre des tuques", "The Dress", "The Club", "The Triple Echo",
        // Rerank — plot:"apes"
        "Tarzan the Ape Man", "Storm Over Asia"
    ];

    // Titles referenced by AtlasSearchTests.cs + VectorSearchTests.cs (sample_mflix.embedded_movies).
    // Every top-K assertion in vector-search tests is satisfied as long as the corresponding 5 (or 3)
    // movies are present with their real plot_embedding; we seed only those.
    private static readonly string[] EmbeddedMovieTitles =
    [
        // VectorSearch / VectorSearch_Limit / VectorSearch_Exact (magical 5)
        "Willy Wonka & the Chocolate Factory", "Pinocchio", "Time Bandits",
        "Harry Potter and the Sorcerer's Stone", "The Witches",
        // VectorSearch_Filter / ProjectAndScore / ProjectAndCount (text "time" filter)
        "Oz the Great and Powerful", "Mr India", "Down to Earth", "Mr. Magorium's Wonder Emporium",
        // RankFusion (ape-themed)
        "Tarzan the Ape Man", "King Kong", "Battle for the Planet of the Apes", "King Kong Lives",
        "Mighty Joe Young",
        // VectorSearchTests.cs VectorSearchExact (war-themed)
        "Red Dawn", "Sands of Iwo Jima", "White Tiger", "P-51 Dragon Fighter", "When Trumpets Fade"
    ];

    // Airbnb listings — Ribeira Charming Duplex + 4 other Porto-Ribeira docs within the
    // tests' Lisbon-ish polygon (-8.6131..-8.60308, 41.14..41.145) + House close to station
    // (Range test bedrooms=3 beds=14 — incidentally located in Australia).
    private static readonly string[] AirbnbListingNames =
    [
        "Ribeira Charming Duplex",
        "Pury Apartments",
        "PORTO DOWNTOWN FLATS-RIBEIRA STUDIO",
        "DB RIBEIRA - Grey Apartment",
        "The Porto Concierge - Casa D. Olga - Riverview",
        "House close to station & direct to opera house...."
    ];

    public static int Main(string[] args)
    {
        try
        {
            var (uri, outPath) = ParseArgs(args);
            Console.WriteLine($"Connecting to {uri}");
            Console.WriteLine($"Output:  {outPath}");

            var client = new MongoClient(uri);

            var historicalDocs = ExtractHistoricalDocuments(client);
            Console.WriteLine($"  historical_documents: {historicalDocs.Count}");
            var movies = ExtractMovies(client);
            Console.WriteLine($"  movies:               {movies.Count}");
            var embeddedMovies = ExtractEmbeddedMovies(client);
            Console.WriteLine($"  embedded_movies:      {embeddedMovies.Count}");
            var airbnb = ExtractAirbnbListings(client);
            Console.WriteLine($"  airbnb_listings:      {airbnb.Count}");

            var output = Render(historicalDocs, movies, embeddedMovies, airbnb);
            File.WriteAllText(outPath, output, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            Console.WriteLine("Done.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    // ---------------------------------------------------------------------
    // Argument parsing
    // ---------------------------------------------------------------------

    private static (string uri, string outPath) ParseArgs(string[] args)
    {
        string? uri = null;
        string? outPath = null;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--uri" when i + 1 < args.Length:
                    uri = args[++i];
                    break;
                case "--out" when i + 1 < args.Length:
                    outPath = args[++i];
                    break;
                default:
                    throw new ArgumentException($"Unknown or incomplete argument: {args[i]}");
            }
        }

        uri ??= Environment.GetEnvironmentVariable("ATLAS_SEARCH_URI")
            ?? "mongodb://localhost:56669/?directConnection=true";

        if (outPath == null)
        {
            throw new ArgumentException(
                "--out <path> is required (e.g. tests/MongoDB.Driver.Tests/Search/AtlasSearchFixtureSeedData.cs).");
        }

        return (uri, outPath);
    }

    // ---------------------------------------------------------------------
    // Extraction queries (sorted by title/_id for determinism)
    // ---------------------------------------------------------------------

    private static IReadOnlyList<BsonDocument> ExtractHistoricalDocuments(IMongoClient client)
    {
        var posts = client.GetDatabase("sample_training").GetCollection<BsonDocument>("posts");

        // sample_training.posts contains hundreds of duplicate titles. Take exactly one per title for
        // the named posts, then 3 additional unique-title posts that have a "Corliss Zuk" comment.
        var declaration = posts.Find(Builders<BsonDocument>.Filter.Eq("title", "Declaration of Independence"))
            .Sort(Builders<BsonDocument>.Sort.Ascending("_id"))
            .Limit(1)
            .First();

        var constitution = posts.Find(Builders<BsonDocument>.Filter.Eq("title", "US Constitution"))
            .Sort(Builders<BsonDocument>.Sort.Ascending("_id"))
            .Limit(1)
            .First();

        var titlesToExclude = new[] { "Declaration of Independence", "US Constitution" };
        var corlissDocs = posts
            .Find(Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("comments.author", "Corliss Zuk"),
                Builders<BsonDocument>.Filter.Nin("title", titlesToExclude)))
            .Sort(Builders<BsonDocument>.Sort.Ascending("_id"))
            .ToList()
            .GroupBy(d => d["title"].AsString) // one per distinct title
            .Select(g => g.First())
            .Take(3)
            .ToList();

        return new[] { declaration, constitution }
            .Concat(corlissDocs)
            .OrderBy(d => d["title"].AsString, StringComparer.Ordinal)
            .ThenBy(d => d["_id"].AsObjectId)
            .ToList();
    }

    private static IReadOnlyList<BsonDocument> ExtractMovies(IMongoClient client)
    {
        var movies = client.GetDatabase("sample_mflix").GetCollection<BsonDocument>("movies");

        var filter = new BsonDocument("title", new BsonDocument("$in", new BsonArray(MovieTitles)));

        var projection = Builders<BsonDocument>.Projection
            .Include("title").Include("plot").Include("fullplot")
            .Include("year").Include("runtime").Include("genres")
            .Exclude("_id");

        // For each title, take the first match (some titles like "Robin Hood" map to several films).
        var byTitle = movies.Find(filter)
            .Project<BsonDocument>(projection)
            .Sort(Builders<BsonDocument>.Sort.Ascending("title").Ascending("year"))
            .ToList()
            .GroupBy(d => d["title"].AsString)
            .Select(g => g.First())
            .OrderBy(d => d["title"].AsString, StringComparer.Ordinal)
            .ToList();

        return byTitle;
    }

    private static IReadOnlyList<BsonDocument> ExtractEmbeddedMovies(IMongoClient client)
    {
        var coll = client.GetDatabase("sample_mflix").GetCollection<BsonDocument>("embedded_movies");

        var filter = new BsonDocument("title", new BsonDocument("$in", new BsonArray(EmbeddedMovieTitles)));

        var projection = Builders<BsonDocument>.Projection
            .Include("title").Include("fullplot")
            .Include("year").Include("runtime")
            .Include("plot_embedding")
            .Exclude("_id");

        var byTitle = coll.Find(filter)
            .Project<BsonDocument>(projection)
            .Sort(Builders<BsonDocument>.Sort.Ascending("title").Ascending("year"))
            .ToList()
            .GroupBy(d => d["title"].AsString)
            .Select(g => g.First(d => d.Contains("plot_embedding"))) // require embedding
            .OrderBy(d => d["title"].AsString, StringComparer.Ordinal)
            .ToList();

        return byTitle;
    }

    private static IReadOnlyList<BsonDocument> ExtractAirbnbListings(IMongoClient client)
    {
        var coll = client.GetDatabase("sample_airbnb").GetCollection<BsonDocument>("listingsAndReviews");

        var filter = new BsonDocument("name", new BsonDocument("$in", new BsonArray(AirbnbListingNames)));

        var projection = Builders<BsonDocument>.Projection
            .Include("name").Include("address").Include("bedrooms").Include("beds")
            .Include("description").Include("space")
            .Exclude("_id");

        return coll.Find(filter)
            .Project<BsonDocument>(projection)
            .Sort(Builders<BsonDocument>.Sort.Ascending("name"))
            .ToList()
            .GroupBy(d => d["name"].AsString)
            .Select(g => g.First())
            .OrderBy(d => d["name"].AsString, StringComparer.Ordinal)
            .ToList();
    }

    // ---------------------------------------------------------------------
    // C# code emission
    // ---------------------------------------------------------------------

    private static string Render(
        IReadOnlyList<BsonDocument> historicalDocs,
        IReadOnlyList<BsonDocument> movies,
        IReadOnlyList<BsonDocument> embeddedMovies,
        IReadOnlyList<BsonDocument> airbnbListings)
    {
        var sb = new StringBuilder();
        EmitHeader(sb);

        EmitBsonDocArray(sb, "HistoricalDocuments", historicalDocs);
        EmitBsonDocArray(sb, "Movies", movies);
        EmitAirbnbListings(sb, airbnbListings);
        EmitEmbeddedMovies(sb, embeddedMovies);

        EmitFooter(sb);
        return sb.ToString();
    }

    private static void EmitHeader(StringBuilder sb)
    {
        sb.AppendLine("""
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

            // <auto-generated>
            //   Generated by tests/Tools/AtlasSeedExtractor.
            //   Re-run with: dotnet run --project tests/Tools/AtlasSeedExtractor -- \
            //                  --uri "<atlas-uri>" --out "<this-file>"
            //   Do not edit manually.
            // </auto-generated>

            using MongoDB.Bson;
            using MongoDB.Bson.Serialization;

            namespace MongoDB.Driver.Tests.Search
            {
                internal static class AtlasSearchFixtureSeedData
                {
            """);
    }

    private static void EmitFooter(StringBuilder sb)
    {
        sb.AppendLine("""
                }
            }
            """);
    }

    private static void EmitBsonDocArray(StringBuilder sb, string name, IReadOnlyList<BsonDocument> docs)
    {
        sb.AppendLine();
        sb.AppendLine($"        public static readonly BsonDocument[] {name} =");
        sb.AppendLine("        [");
        for (var i = 0; i < docs.Count; i++)
        {
            EmitBsonDocAsParse(sb, docs[i], i < docs.Count - 1);
        }
        sb.AppendLine("        ];");
    }

    // Each Airbnb doc has a nested address.location that's a GeoJSON Point. Use BsonDocument.Parse
    // with canonical extended JSON to round-trip cleanly.
    private static void EmitAirbnbListings(StringBuilder sb, IReadOnlyList<BsonDocument> docs)
    {
        EmitBsonDocArray(sb, "AirbnbListings", docs);
    }

    // Embedded movies are emitted via a builder method that constructs the BsonDocument from
    // the literal fields + a separate float[] embedding so the binary vector subtype is preserved.
    private static void EmitEmbeddedMovies(StringBuilder sb, IReadOnlyList<BsonDocument> docs)
    {
        sb.AppendLine();
        sb.AppendLine("        public static readonly BsonDocument[] EmbeddedMovies =");
        sb.AppendLine("        [");
        for (var i = 0; i < docs.Count; i++)
        {
            var d = docs[i];
            var title = d["title"].AsString;
            var fullplot = d.TryGetValue("fullplot", out var fp) ? fp.AsString : "";
            var runtime = d.TryGetValue("runtime", out var r) ? r.ToInt32() : 0;
            var year = d.TryGetValue("year", out var y) ? y.ToInt32() : 0;
            var embedding = ReadFloat32Vector(d["plot_embedding"]);

            sb.AppendLine($"            BuildEmbeddedMovie(");
            sb.AppendLine($"                title:    {EscapeStringLiteral(title)},");
            sb.AppendLine($"                fullplot: {EscapeStringLiteral(fullplot)},");
            sb.AppendLine($"                runtime:  {runtime.ToString(CultureInfo.InvariantCulture)},");
            sb.AppendLine($"                year:     {year.ToString(CultureInfo.InvariantCulture)},");
            sb.AppendLine($"                embedding: new float[]");
            sb.AppendLine($"                {{");
            EmitFloatArray(sb, embedding, indent: "                    ", perLine: 6);
            sb.AppendLine($"                }}){(i < docs.Count - 1 ? "," : "")}");
        }
        sb.AppendLine("        ];");

        sb.AppendLine();
        sb.AppendLine("""
                    private static BsonDocument BuildEmbeddedMovie(
                        string title, string fullplot, int runtime, int year, float[] embedding) =>
                        new BsonDocument
                        {
                            { "title", title },
                            { "fullplot", fullplot },
                            { "runtime", runtime },
                            { "year", year },
                            { "plot_embedding", new BinaryVectorFloat32(embedding).ToBsonBinaryData() }
                        };
            """);
    }

    // Decodes Atlas's binary-vector subtype-9 format directly so the tool doesn't need access to
    // MongoDB.Bson's internal BinaryVectorReader. Layout: [dtype(1)][padding(1)][little-endian payload].
    private static float[] ReadFloat32Vector(BsonValue value)
    {
        if (value is BsonBinaryData bin && bin.SubType == BsonBinarySubType.Vector)
        {
            var bytes = bin.Bytes;
            if (bytes.Length < 2 || bytes[0] != 0x27)
            {
                throw new InvalidOperationException(
                    $"Expected float32 vector (dtype 0x27); got dtype 0x{bytes[0]:X2}.");
            }
            var payload = (bytes.Length - 2) / 4;
            var result = new float[payload];
            Buffer.BlockCopy(bytes, 2, result, 0, payload * 4);
            return result;
        }
        if (value is BsonArray arr)
        {
            return arr.Select(v => (float)v.ToDouble()).ToArray();
        }
        throw new InvalidOperationException($"Cannot read plot_embedding of type {value.GetType().Name}");
    }

    private static void EmitFloatArray(StringBuilder sb, float[] values, string indent, int perLine)
    {
        for (var i = 0; i < values.Length; i += perLine)
        {
            sb.Append(indent);
            for (var j = i; j < Math.Min(i + perLine, values.Length); j++)
            {
                sb.Append(values[j].ToString("R", CultureInfo.InvariantCulture));
                sb.Append("f");
                if (j < values.Length - 1)
                {
                    sb.Append(", ");
                }
            }
            sb.AppendLine();
        }
    }

    private static void EmitBsonDocAsParse(StringBuilder sb, BsonDocument doc, bool trailingComma)
    {
        var json = doc.ToJson(new JsonWriterSettings
        {
            Indent = true,
            IndentChars = "    ",
            NewLineChars = "\n",
            OutputMode = JsonOutputMode.CanonicalExtendedJson
        });

        // Indent every line of the JSON by 16 spaces so it sits inside the array initializer.
        var indented = string.Join("\n",
            json.Split('\n').Select((line, idx) => idx == 0 ? line : "                " + line));

        // Choose a fence longer than any """ sequence in the JSON so the raw string literal closes correctly.
        var fence = "\"\"\"";
        while (indented.Contains(fence))
        {
            fence += "\"";
        }

        sb.AppendLine($"            BsonDocument.Parse({fence}");
        sb.AppendLine("                " + indented);
        sb.Append("                " + fence + ")");
        sb.AppendLine(trailingComma ? "," : "");
    }

    private static string EscapeStringLiteral(string s)
    {
        var sb = new StringBuilder("\"");
        foreach (var c in s)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20)
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture, "\\u{0:X4}", (int)c);
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        sb.Append('"');
        return sb.ToString();
    }
}
