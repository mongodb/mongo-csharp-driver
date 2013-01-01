/* Copyright 2010-2013 10gen Inc.
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

using System.IO;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Jira
{
    [TestFixture]
    public class CSharp71Tests
    {
        [Test]
        public void TestWithArrayOfDocument()
        {
            // this is the C# version of the json-with-docarray.txt file attached to the bug report
            // document initializer
            var document = new BsonDocument
            {
                { "_id", "5d37e102e4a297f4156e0000" },
                { "Name", "IMG_2962.JPG" },
                { "AccountId", "94bb3e04e4a297080c000000" },
                { "Properties", new BsonArray
                    {
                        new BsonDocument
                        {
                            { "Name", "Make" },
                            { "Value", "Canon" },
                            { "Tag", 100271 }
                        },
                        new BsonDocument
                        {
                            { "Name", "Model" },
                            { "Value", "Canon DIGITAL IXUS 950 IS" },
                            { "Tag", 100272 }
                        },
                        new BsonDocument
                        {
                            { "Name", "Orientation" },
                            { "Value", "Normal" },
                            { "Tag", 100274 }
                        },
                        new BsonDocument
                        {
                            { "Name", "XResolution" },
                            { "Value", "180/1" },
                            { "Tag", 100282 }
                        },
                        new BsonDocument
                        {
                            { "Name", "YResolution" },
                            { "Value", "180/1" },
                            { "Tag", 100283 }
                        },
                        new BsonDocument
                        {
                            { "Name", "ResolutionUnit" },
                            { "Value", "Inches" },
                            { "Tag", 100296 }
                        },
                        new BsonDocument
                        {
                            { "Name", "DateTime" },
                            { "Value", "03-09-2010 23:20:26" },
                            { "Tag", 100306 }
                        },
                        new BsonDocument
                        {
                            { "Name", "YCbCrPositioning" },
                            { "Value", "Centered" },
                            { "Tag", 100531 }
                        },
                        new BsonDocument
                        {
                            { "Name", "EXIFIFDPointer" },
                            { "Value", "196" },
                            { "Tag", 134665 }
                        },
                        new BsonDocument
                        {
                            { "Name", "ExposureTime" },
                            { "Value", "1/60" },
                            { "Tag", 233434 }
                        },
                        new BsonDocument
                        {
                            { "Name", "FNumber" },
                            { "Value", "14/5" },
                            { "Tag", 233437 }
                        },
                        new BsonDocument
                        {
                            { "Name", "ISOSpeedRatings" },
                            { "Value", "200" },
                            { "Tag", 234855 }
                        },
                        new BsonDocument
                        {
                            { "Name", "ExifVersion" },
                            { "Value", "0220" },
                            { "Tag", 236864 }
                        },
                        new BsonDocument
                        {
                            { "Name", "DateTimeOriginal" },
                            { "Value", "03-09-2010 23:20:26" },
                            { "Tag", 236867 }
                        },
                        new BsonDocument
                        {
                            { "Name", "DateTimeDigitized" },
                            { "Value", "03-09-2010 23:20:26" },
                            { "Tag", 236868 }
                        },
                        new BsonDocument
                        {
                            { "Name", "ComponentsConfiguration" },
                            { "Value", "System.Byte[]" },
                            { "Tag", 237121 }
                        },
                        new BsonDocument
                        {
                            { "Name", "CompressedBitsPerPixel" },
                            { "Value", "5/1" },
                            { "Tag", 237122 }
                        },
                        new BsonDocument
                        {
                            { "Name", "ShutterSpeedValue" },
                            { "Value", "189/32" },
                            { "Tag", 237377 }
                        },
                        new BsonDocument
                        {
                            { "Name", "ApertureValue" },
                            { "Value", "95/32" },
                            { "Tag", 237378 }
                        },
                        new BsonDocument
                        {
                            { "Name", "ExposureBiasValue" },
                            { "Value", "0/1" },
                            { "Tag", 237380 }
                        },
                        new BsonDocument
                        {
                            { "Name", "MaxApertureValue" },
                            { "Value", "95/32" },
                            { "Tag", 237381 }
                        },
                        new BsonDocument
                        {
                            { "Name", "MeteringMode" },
                            { "Value", "Pattern" },
                            { "Tag", 237383 }
                        },
                        new BsonDocument
                        {
                            { "Name", "Flash" },
                            { "Value", "FlashFired, CompulsoryFlashMode, AutoMode, RedEyeReductionMode" },
                            { "Tag", 237385 }
                        },
                        new BsonDocument
                        {
                            { "Name", "FocalLength" },
                            { "Value", "29/5" },
                            { "Tag", 237386 }
                        },
                        new BsonDocument
                        {
                            { "Name", "MakerNote" },
                            { "Value", "System.Byte[]" },
                            { "Tag", 237500 }
                        },
                        new BsonDocument
                        {
                            { "Name", "UserComment" },
                            { "Value", "" },
                            { "Tag", 237510 }
                        },
                        new BsonDocument
                        {
                            { "Name", "FlashpixVersion" },
                            { "Value", "0100" },
                            { "Tag", 240960 }
                        },
                        new BsonDocument
                        {
                            { "Name", "ColorSpace" },
                            { "Value", "sRGB" },
                            { "Tag", 240961 }
                        },
                        new BsonDocument
                        {
                            { "Name", "PixelXDimension" },
                            { "Value", "3264" },
                            { "Tag", 240962 }
                        },
                        new BsonDocument
                        {
                            { "Name", "PixelYDimension" },
                            { "Value", "1832" },
                            { "Tag", 240963 }
                        },
                        new BsonDocument
                        {
                            { "Name", "InteroperabilityIFDPointer" },
                            { "Value", "3334" },
                            { "Tag", 240965 }
                        },
                        new BsonDocument
                        {
                            { "Name", "FocalPlaneXResolution" },
                            { "Value", "43520/3" },
                            { "Tag", 241486 }
                        },
                        new BsonDocument
                        {
                            { "Name", "FocalPlaneYResolution" },
                            { "Value", "1832000/169" },
                            { "Tag", 241487 }
                        },
                        new BsonDocument
                        {
                            { "Name", "FocalPlaneResolutionUnit" },
                            { "Value", "Inches" },
                            { "Tag", 241488 }
                        },
                        new BsonDocument
                        {
                            { "Name", "SensingMethod" },
                            { "Value", "OneChipColorAreaSensor" },
                            { "Tag", 241495 }
                        },
                        new BsonDocument
                        {
                            { "Name", "FileSource" },
                            { "Value", "DSC" },
                            { "Tag", 241728 }
                        },
                        new BsonDocument
                        {
                            { "Name", "CustomRendered" },
                            { "Value", "NormalProcess" },
                            { "Tag", 241985 }
                        },
                        new BsonDocument
                        {
                            { "Name", "ExposureMode" },
                            { "Value", "Auto" },
                            { "Tag", 241986 }
                        },
                        new BsonDocument
                        {
                            { "Name", "WhiteBalance" },
                            { "Value", "Auto" },
                            { "Tag", 241987 }
                        },
                        new BsonDocument
                        {
                            { "Name", "DigitalZoomRatio" },
                            { "Value", "1/1" },
                            { "Tag", 241988 }
                        },
                        new BsonDocument
                        {
                            { "Name", "SceneCaptureType" },
                            { "Value", "Standard" },
                            { "Tag", 241990 }
                        },
                        new BsonDocument
                        {
                            { "Name", "InteroperabilityIndex" },
                            { "Value", "R98" },
                            { "Tag", 400001 }
                        },
                        new BsonDocument
                        {
                            { "Name", "InteroperabilityVersion" },
                            { "Value", "0100" },
                            { "Tag", 400002 }
                        },
                        new BsonDocument
                        {
                            { "Name", "RelatedImageWidth" },
                            { "Value", "3264" },
                            { "Tag", 404097 }
                        },
                        new BsonDocument
                        {
                            { "Name", "RelatedImageHeight" },
                            { "Value", "1832" },
                            { "Tag", 404098 }
                        },
                        new BsonDocument
                        {
                            { "Name", "ThumbnailCompression" },
                            { "Value", "JPEGCompression" },
                            { "Tag", 500259 }
                        },
                        new BsonDocument
                        {
                            { "Name", "ThumbnailXResolution" },
                            { "Value", "180/1" },
                            { "Tag", 500282 }
                        },
                        new BsonDocument
                        {
                            { "Name", "ThumbnailYResolution" },
                            { "Value", "180/1" },
                            { "Tag", 500283 }
                        },
                        new BsonDocument
                        {
                            { "Name", "ThumbnailResolutionUnit" },
                            { "Value", "Inches" },
                            { "Tag", 500296 }
                        },
                        new BsonDocument
                        {
                            { "Name", "ThumbnailJPEGInterchangeFormat" },
                            { "Value", "5108" },
                            { "Tag", 500513 }
                        },
                        new BsonDocument
                        {
                            { "Name", "ThumbnailJPEGInterchangeFormatLength" },
                            { "Value", "4963" },
                            { "Tag", 500514 }
                        }
                    }
                }
            };

            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }

        [Test]
        public void Test20KDocument()
        {
            // manufacture an approximately 20K document using 200 strings each 100 characters long
            // it's enough to cause the document to straddle a chunk boundary
            var document = new BsonDocument();
            var value = new string('x', 100);
            for (int i = 0; i < 200; i++)
            {
                var name = i.ToString();
                document.Add(name, value);
            }

            // round trip tests
            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));

            // test failure mode when 20 bytes are truncated from the buffer
            using (var buffer = new BsonBuffer())
            {
                buffer.LoadFrom(new MemoryStream(bson));
                buffer.Length -= 20;
                using (var bsonReader = BsonReader.Create(buffer))
                {
                    Assert.Throws<EndOfStreamException>(() => BsonSerializer.Deserialize<BsonDocument>(bsonReader));
                }
            }
        }

        [Test]
        public void TestNameStraddlesBoundary()
        {
            // manufacture an approximately 20K document using 200 elements with long names of 100+ characters
            // it's enough to cause the document to straddle a chunk boundary
            var document = new BsonDocument();
            var prefix = new string('x', 100);
            for (int i = 0; i < 200; i++)
            {
                var name = prefix + i.ToString();
                document.Add(name, "x");
            }

            // round trip tests
            var bson = document.ToBson();
            var rehydrated = BsonSerializer.Deserialize<BsonDocument>(bson);
            Assert.IsTrue(bson.SequenceEqual(rehydrated.ToBson()));
        }
    }
}
