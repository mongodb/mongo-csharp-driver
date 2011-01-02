﻿/* Copyright 2010 10gen Inc.
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
using System.Linq;
using System.Text;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MongoDB.Driver.Builders {
    public static class GeoNearOptions {
        #region public static properties
        public static IMongoGeoNearOptions Null {
            get { return null; }
        }
        #endregion

        #region public static methods
        public static GeoNearOptionsBuilder SetDistanceMultiplier(
            double value
        ) {
            return new GeoNearOptionsBuilder().SetDistanceMultiplier(value);
        }

        public static GeoNearOptionsBuilder SetMaxDistance(
            double value
        ) {
            return new GeoNearOptionsBuilder().SetMaxDistance(value);
        }

        public static IMongoGeoNearOptions Wrap(
            object options
        ) {
            return GeoNearOptionsWrapper.Create(options);
        }
        #endregion
    }

    [Serializable]
    public class GeoNearOptionsBuilder : BuilderBase, IMongoGeoNearOptions {
        #region private fields
        private BsonDocument document;
        #endregion

        #region constructors
        public GeoNearOptionsBuilder() {
            document = new BsonDocument();
        }
        #endregion

        #region public methods
        public GeoNearOptionsBuilder SetDistanceMultiplier(
            double value
        ) {
            document["distanceMultiplier"] = value;
            return this;
        }

        public GeoNearOptionsBuilder SetMaxDistance(
            double value
        ) {
            document["maxDistance"] = value;
            return this;
        }
        public override BsonDocument ToBsonDocument() {
            return document;
        }
        #endregion

        #region protected methods
        protected override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            document.Serialize(bsonWriter, nominalType, options);
        }
        #endregion
    }
}
