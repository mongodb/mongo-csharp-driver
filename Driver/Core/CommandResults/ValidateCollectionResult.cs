/* Copyright 2010-2011 10gen Inc.
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
using System.Text.RegularExpressions;
using System.Xml;

using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents the results of a validate collection command.
    /// </summary>
    [Serializable]
    public class ValidateCollectionResult : CommandResult
    {
        // private fields
        private string[] _errors;
        private ExtentDetails _firstExtentDetails;
        private Dictionary<string, long> _keysPerIndex;

        // constructors
        /// <summary>
        /// Initializes a new instance of the ValidateCollectionResult class.
        /// </summary>
        public ValidateCollectionResult()
        {
        }

        // public properties
        /// <summary>
        /// Gets the data size of the collection.
        /// </summary>
        public long DataSize
        {
            get
            {
                if (_response.Contains("result") && !_response.Contains("datasize"))
                {
                    var match = Regex.Match(_response["result"].AsString, @"datasize\?\:(?<value>\d+)");
                    return XmlConvert.ToInt64(match.Groups["value"].Value);
                }
                else
                {
                    return _response["datasize"].ToInt64();
                }
            }
        }

        /// <summary>
        /// Gets the number of documents that have been deleted from the collection.
        /// </summary>
        public long DeletedCount
        {
            get
            {
                if (_response.Contains("result") && !_response.Contains("deletedCount"))
                {
                    var match = Regex.Match(_response["result"].AsString, @"deleted\: n\: (?<value>\d+)");
                    return XmlConvert.ToInt64(match.Groups["value"].Value);
                }
                else
                {
                    return _response["deletedCount"].ToInt64();
                }
            }
        }

        /// <summary>
        /// Gets the number of documents that have been deleted from the collection.
        /// </summary>
        public long DeletedSize
        {
            get
            {
                if (_response.Contains("result") && !_response.Contains("deletedSize"))
                {
                    var match = Regex.Match(_response["result"].AsString, @"deleted\: n\: \d+ size\: (?<value>\d+)");
                    return XmlConvert.ToInt64(match.Groups["value"].Value);
                }
                else
                {
                    return _response["deletedSize"].ToInt64();
                }
            }
        }

        /// <summary>
        /// Gets the errors returned by validate (or an empty array if there were no errors).
        /// </summary>
        public string[] Errors
        {
            get
            {
                if (_errors == null)
                {
                    if (_response.Contains("errors"))
                    {
                        _errors = _response["errors"].AsBsonArray.Select(e => e.ToString()).ToArray();
                    }
                    else
                    {
                        _errors = new string[0];
                    }
                }
                return _errors;
            }
        }

        /// <summary>
        /// Gets the number of extents in the collection.
        /// </summary>
        public long ExtentCount
        {
            get
            {
                if (_response.Contains("result") && !_response.Contains("extentCount"))
                {
                    var match = Regex.Match(_response["result"].AsString, @"# extents\:(?<value>\d+)");
                    return XmlConvert.ToInt64(match.Groups["value"].Value);
                }
                else
                {
                    return _response["extentCount"].ToInt64();
                }
            }
        }

        /// <summary>
        /// Gets the first extent of the collection.
        /// </summary>
        public string FirstExtent
        {
            get
            {
                if (_response.Contains("result") && !_response.Contains("firstExtent"))
                {
                    var match = Regex.Match(_response["result"].AsString, @"firstExtent\:(?<value>.+)");
                    return match.Groups["value"].Value;
                }
                else
                {
                    return _response["firstExtent"].AsString;
                }
            }
        }

        /// <summary>
        /// Gets details of the first extent of the collection.
        /// </summary>
        public ExtentDetails FirstExtentDetails
        {
            get
            {
                if (_firstExtentDetails == null)
                {
                    if (_response.Contains("result") && !_response.Contains("firstExtentDetails"))
                    {
                        var match = Regex.Match(_response["result"].AsString, @"first extent:\n(?<details>(    .+\n)+)");
                        var detailsString = match.Groups["details"].Value;
                        _firstExtentDetails = new ExtentDetails(null, detailsString);
                    }
                    else
                    {
                        var detailsDocument = _response["firstExtentDetails"].AsBsonDocument;
                        _firstExtentDetails = new ExtentDetails(detailsDocument, null);
                    }
                }
                return _firstExtentDetails;
            }
        }

        /// <summary>
        /// Gets the number of indexes in the collection.
        /// </summary>
        public int IndexCount
        {
            get
            {
                if (_response.Contains("result") && !_response.Contains("nIndexes"))
                {
                    var match = Regex.Match(_response["result"].AsString, @"nIndexes\:(?<value>\d+)");
                    return XmlConvert.ToInt32(match.Groups["value"].Value);
                }
                else
                {
                    return _response["nIndexes"].ToInt32();
                }
            }
        }

        /// <summary>
        /// Gets whether the collection is valid.
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (_response.Contains("result") && !_response.Contains("errors") && !_response.Contains("valid"))
                {
                    // this somewhat odd method of determining whether the collection is valid or not was copied from the mongo shell
                    var json = _response.ToJson();
                    return !(json.Contains("exception") || json.Contains("corrupt"));
                }
                else if (_response.Contains("errors") && !_response.Contains("valid"))
                {
                    return _response["errors"].AsBsonArray.Count == 0;
                }
                else
                {
                    return _response["valid"].ToBoolean();
                }
            }
        }

        /// <summary>
        /// Gets a dictionary containing the number of keys per index.
        /// </summary>
        public Dictionary<string, long> KeysPerIndex
        {
            get
            {
                if (_keysPerIndex == null)
                {
                    var dictionary = new Dictionary<string, long>();
                    var prefixLength = Namespace.Length + 1; // allow for "."
                    if (_response.Contains("result") && !_response.Contains("keysPerIndex"))
                    {
                        var match = Regex.Match(_response["result"].AsString, @"nIndexes\:\d+\n(?<value>(    .+\n)+)");
                        var indexStrings = match.Groups["value"].Value.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var indexString in indexStrings)
                        {
                            var trimmedIndexString = indexString.Substring(4 + prefixLength); // lines start with 4 blanks
                            match = Regex.Match(trimmedIndexString, @"(?<indexName>.+) keys\:(?<keys>\d+)");
                            var indexName = match.Groups["indexName"].Value;
                            var keys = XmlConvert.ToInt64(match.Groups["keys"].Value);
                            dictionary.Add(indexName, keys);
                        }
                    }
                    else
                    {
                        foreach (var element in _response["keysPerIndex"].AsBsonDocument)
                        {
                            var indexName = element.Name.Substring(prefixLength);
                            var keys = element.Value.ToInt64();
                            dictionary.Add(indexName, keys);
                        }
                    }
                    _keysPerIndex = dictionary;
                }
                return _keysPerIndex;
            }
        }

        /// <summary>
        /// Gets the last extent of the collection.
        /// </summary>
        public string LastExtent
        {
            get
            {
                if (_response.Contains("result") && !_response.Contains("lastExtent"))
                {
                    var match = Regex.Match(_response["result"].AsString, @"lastExtent\:(?<value>.+)");
                    return match.Groups["value"].Value;
                }
                else
                {
                    return _response["lastExtent"].AsString;
                }
            }
        }

        /// <summary>
        /// Gets the size of the last extent of the collection.
        /// </summary>
        public long LastExtentSize
        {
            get
            {
                if (_response.Contains("result") && !_response.Contains("lastExtentSize"))
                {
                    var match = Regex.Match(_response["result"].AsString, @"lastExtentSize\:(?<value>\d+)");
                    return XmlConvert.ToInt64(match.Groups["value"].Value);
                }
                else
                {
                    return _response["lastExtentSize"].ToInt64();
                }
            }
        }

        /// <summary>
        /// Gets the namespace.
        /// </summary>
        public string Namespace
        {
            get { return _response["ns"].AsString; }
        }

        /// <summary>
        /// Gets the padding factor of the collection.
        /// </summary>
        public double Padding
        {
            get
            {
                if (_response.Contains("result") && !_response.Contains("padding"))
                {
                    var match = Regex.Match(_response["result"].AsString, @"padding\:(?<value>.+)");
                    return XmlConvert.ToDouble(match.Groups["value"].Value);
                }
                else
                {
                    return _response["padding"].ToDouble();
                }
            }
        }

        /// <summary>
        /// Gets the number of records in the collection.
        /// </summary>
        public long RecordCount
        {
            get
            {
                if (_response.Contains("result") && !_response.Contains("nrecords"))
                {
                    var match = Regex.Match(_response["result"].AsString, @"nrecords\?\:(?<value>\d+)");
                    return XmlConvert.ToInt64(match.Groups["value"].Value);
                }
                else
                {
                    return _response["nrecords"].ToInt64();
                }
            }
        }

        /// <summary>
        /// Gets the result string.
        /// </summary>
        public string ResultString
        {
            get { return _response["result"].AsString; }
        }

        /// <summary>
        /// Gets any warning returned by the validate command (or null if there is no warning).
        /// </summary>
        public string Warning
        {
            get
            {
                if (_response.Contains("warning"))
                {
                    return _response["warning"].AsString;
                }
                else
                {
                    return null;
                }
            }
        }

        // nested classes
        /// <summary>
        /// Represents the details of the first extent of the collection.
        /// </summary>
        public class ExtentDetails
        {
            private BsonDocument _detailsDocument;
            private string _detailsString;

            internal ExtentDetails(BsonDocument detailsDocument, string detailsString)
            {
                _detailsDocument = detailsDocument;
                _detailsString = detailsString;
            }

            /// <summary>
            /// Gets the location of the extent.
            /// </summary>
            public string Loc
            {
                get
                {
                    if (_detailsDocument == null)
                    {
                        var match = Regex.Match(_detailsString, @"loc\:(?<value>.+) xnext");
                        return match.Groups["value"].Value;
                    }
                    else
                    {
                        return _detailsDocument["loc"].AsString;
                    }
                }
            }

            /// <summary>
            /// Gets the location of the first record of the extent.
            /// </summary>
            public string FirstRecord
            {
                get
                {
                    if (_detailsDocument == null)
                    {
                        var match = Regex.Match(_detailsString, @"firstRecord\:(?<value>.+) lastRecord");
                        return match.Groups["value"].Value;
                    }
                    else
                    {
                        return _detailsDocument["firstRecord"].AsString;
                    }
                }
            }

            /// <summary>
            /// Gets the location of the last record of the extent.
            /// </summary>
            public string LastRecord
            {
                get
                {
                    if (_detailsDocument == null)
                    {
                        var match = Regex.Match(_detailsString, @"lastRecord\:(?<value>.+)");
                        return match.Groups["value"].Value;
                    }
                    else
                    {
                        return _detailsDocument["lastRecord"].AsString;
                    }
                }
            }

            /// <summary>
            /// Gets the nsdiag value of the extent.
            /// </summary>
            public string NSDiag
            {
                get
                {
                    if (_detailsDocument == null)
                    {
                        var match = Regex.Match(_detailsString, @"nsdiag\:(?<value>.+)");
                        return match.Groups["value"].Value;
                    }
                    else
                    {
                        return _detailsDocument["nsdiag"].AsString;
                    }
                }
            }

            /// <summary>
            /// Gets the size of the extent.
            /// </summary>
            public long Size
            {
                get
                {
                    if (_detailsDocument == null)
                    {
                        var match = Regex.Match(_detailsString, @"size\:(?<value>\d+)");
                        return XmlConvert.ToInt64(match.Groups["value"].Value);
                    }
                    else
                    {
                        return _detailsDocument["size"].ToInt64();
                    }
                }
            }

            /// <summary>
            /// Gets the next extent.
            /// </summary>
            public string XNext
            {
                get
                {
                    if (_detailsDocument == null)
                    {
                        var match = Regex.Match(_detailsString, @"xnext\:(?<value>.+) xprev");
                        return match.Groups["value"].Value;
                    }
                    else
                    {
                        return _detailsDocument["xnext"].AsString;
                    }
                }
            }

            /// <summary>
            /// Gets the prev extent.
            /// </summary>
            public string XPrev
            {
                get
                {
                    if (_detailsDocument == null)
                    {
                        var match = Regex.Match(_detailsString, @"xprev\:(?<value>.+)");
                        return match.Groups["value"].Value;
                    }
                    else
                    {
                        return _detailsDocument["xprev"].AsString;
                    }
                }
            }
        }
    }
}
