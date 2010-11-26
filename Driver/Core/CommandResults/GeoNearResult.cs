/* Copyright 2010 10gen Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver
{
    [Serializable]
    public class GeoNearResult<TDocument> : CommandResult
    {
        public readonly GeoNearStats Stats;
        private IEnumerable<Result> _results;

        public GeoNearResult()
        {
            Stats = new GeoNearStats(this);
        }

        public string Near
        {
            get { return this["near"].AsString; }
        }

        public string NameSpace
        {
            get { return this["ns"].AsString; }
        }

        public IEnumerable<Result> Results
        {
            get
            {
                if (_results == null)
                    _results = _getResults();

                return _results;
            }
        }

        private IEnumerable<Result> _getResults()
        {
            var resultDocument = this["results"].AsBsonArray;

            return resultDocument.Select(_convertResultDocument).ToList();
        }

        private static Result _convertResultDocument(BsonValue resultEntry)
        {
            var resultEntryDocument = resultEntry.AsBsonDocument;

            //todo: is this the right way to deserialize
            var distance = resultEntryDocument.GetValue("dis").AsDouble;
            var objDocument = resultEntryDocument.GetValue("obj").AsBsonDocument;
            var obj = _deserializeDocument(objDocument);

            return new Result(distance, obj);
        }

        private static TDocument _deserializeDocument(BsonDocument objDocument)
        {
            var buffer = objDocument.ToBson();
            return BsonSerializer.Deserialize<TDocument>(buffer);
        }

        #region Nested type: GeoNearStats

        public class GeoNearStats
        {
            // todo: Maybe this should just deserialize a Stats object?

            private readonly GeoNearResult<TDocument> _parent;
            private BsonDocument _statsDocument;

            public GeoNearStats(GeoNearResult<TDocument> parent)
            {
                _parent = parent;
            }

            private BsonDocument StatsDocument
            {
                get
                {
                    if (_statsDocument == null)
                        _statsDocument = _parent["stats"].AsBsonDocument;

                    return _statsDocument;
                }
            }

            public int objectsLoaded
            {
                get { return StatsDocument.GetValue("objectsLoaded").AsInt32; }
            }

            public int time
            {
                get { return StatsDocument.GetValue("time").AsInt32; }
            }

            public int btreelocs
            {
                get { return StatsDocument.GetValue("btreelocs").AsInt32; }
            }

            public int nscanned
            {
                get { return StatsDocument.GetValue("nscanned").AsInt32; }
            }

            public Double avgDistance
            {
                get { return StatsDocument.GetValue("avgDistance").AsDouble; }
            }

            public Double maxDistance
            {
                get { return StatsDocument.GetValue("maxDistance").AsDouble; }
            }
        }

        #endregion

        #region Nested type: Result

        public class Result
        {
            public readonly double Distance;
            public readonly TDocument Value;

            public Result(double distance, TDocument value)
            {
                Distance = distance;
                Value = value;
            }
        }

        #endregion
    }
}