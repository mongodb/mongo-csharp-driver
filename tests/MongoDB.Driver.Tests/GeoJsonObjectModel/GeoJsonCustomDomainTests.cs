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

using System.Globalization;
using System.IO;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.GeoJsonObjectModel;
using Xunit;

namespace MongoDB.Driver.Tests.GeoJsonObjectModel
{
    public class GeoJsonCustomDomainTests
    {
        // A custom 2D-coordinates serializer that writes the values prefixed with "X:"/"Y:"
        // strings instead of plain doubles, so we can detect at runtime which inner serializer
        // a GeoJSON serializer is using.
        private sealed class TaggedGeoJson2DCoordinatesSerializer : ClassSerializerBase<GeoJson2DCoordinates>
        {
            protected override GeoJson2DCoordinates DeserializeValue(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                var reader = context.Reader;
                reader.ReadStartArray();
                var xRaw = reader.ReadString();
                var yRaw = reader.ReadString();
                reader.ReadEndArray();
                var x = double.Parse(xRaw.Substring(2), CultureInfo.InvariantCulture);
                var y = double.Parse(yRaw.Substring(2), CultureInfo.InvariantCulture);
                return new GeoJson2DCoordinates(x, y);
            }

            protected override void SerializeValue(BsonSerializationContext context, BsonSerializationArgs args, GeoJson2DCoordinates value)
            {
                var writer = context.Writer;
                writer.WriteStartArray();
                writer.WriteString("X:" + value.X.ToString(CultureInfo.InvariantCulture));
                writer.WriteString("Y:" + value.Y.ToString(CultureInfo.InvariantCulture));
                writer.WriteEndArray();
            }
        }

        [Fact]
        public void GeoJsonPointSerializer_resolved_under_custom_domain_uses_domains_inner_coordinates_serializer()
        {
            var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("GeoJsonCustomDomainTests-Point");
            customDomain.RegisterSerializer(new TaggedGeoJson2DCoordinatesSerializer());

            var point = GeoJson.Point(GeoJson.Position(1.0, 2.0));
            var json = SerializeToJson(customDomain, point);

            json.Should().Contain("\"X:1\"").And.Contain("\"Y:2\"");
        }

        [Fact]
        public void Default_domain_unaffected_by_custom_domain_registration()
        {
            var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("GeoJsonCustomDomainTests-Isolation");
            customDomain.RegisterSerializer(new TaggedGeoJson2DCoordinatesSerializer());

            var point = GeoJson.Point(GeoJson.Position(1.0, 2.0));
            var json = SerializeToJson(BsonSerializationDomain.Default, point);

            json.Should().NotContain("X:").And.NotContain("Y:");
            json.Should().Contain("[1.0, 2.0]");
        }

        [Fact]
        public void GeoJsonLineStringSerializer_resolved_under_custom_domain_uses_domains_inner_coordinates_serializer()
        {
            var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("GeoJsonCustomDomainTests-LineString");
            customDomain.RegisterSerializer(new TaggedGeoJson2DCoordinatesSerializer());

            var lineString = GeoJson.LineString(
                GeoJson.Position(1.0, 2.0),
                GeoJson.Position(3.0, 4.0));
            var json = SerializeToJson(customDomain, lineString);

            json.Should().Contain("\"X:1\"").And.Contain("\"Y:2\"");
            json.Should().Contain("\"X:3\"").And.Contain("\"Y:4\"");
        }

        [Fact]
        public void GeoJsonGeometryCollectionSerializer_resolved_under_custom_domain_uses_domains_inner_coordinates_serializer()
        {
            var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("GeoJsonCustomDomainTests-GeometryCollection");
            customDomain.RegisterSerializer(new TaggedGeoJson2DCoordinatesSerializer());

            var geometryCollection = GeoJson.GeometryCollection(
                GeoJson.Point(GeoJson.Position(7.0, 8.0)));
            var json = SerializeToJson(customDomain, geometryCollection);

            json.Should().Contain("\"X:7\"").And.Contain("\"Y:8\"");
        }

        [Fact]
        public void GeoJsonFeatureSerializer_resolved_under_custom_domain_uses_domains_inner_coordinates_serializer()
        {
            var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("GeoJsonCustomDomainTests-Feature");
            customDomain.RegisterSerializer(new TaggedGeoJson2DCoordinatesSerializer());

            var feature = GeoJson.Feature(
                GeoJson.Point(GeoJson.Position(9.0, 10.0)));
            var json = SerializeToJson(customDomain, feature);

            json.Should().Contain("\"X:9\"").And.Contain("\"Y:10\"");
        }

        [Fact]
        public void GeoJsonFeatureCollectionSerializer_resolved_under_custom_domain_uses_domains_inner_coordinates_serializer()
        {
            var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("GeoJsonCustomDomainTests-FeatureCollection");
            customDomain.RegisterSerializer(new TaggedGeoJson2DCoordinatesSerializer());

            var featureCollection = GeoJson.FeatureCollection(
                GeoJson.Feature(GeoJson.Point(GeoJson.Position(11.0, 12.0))));
            var json = SerializeToJson(customDomain, featureCollection);

            json.Should().Contain("\"X:11\"").And.Contain("\"Y:12\"");
        }

        private static string SerializeToJson<T>(IBsonSerializationDomain domain, T value)
        {
            using var stringWriter = new StringWriter();
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                domain.Serialize(jsonWriter, value);
            }
            return stringWriter.ToString();
        }
    }
}
