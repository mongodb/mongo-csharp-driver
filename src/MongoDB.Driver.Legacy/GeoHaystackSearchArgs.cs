/* Copyright 2010-2015 MongoDB Inc.
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
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver.Linq.Utils;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents the arguments to the GeoHaystackSearch method.
    /// </summary>
    public class GeoHaystackSearchArgs
    {
        // private fields
        private string _additionalFieldName;
        private BsonValue _additionalFieldValue;
        private int? _limit;
        private double? _maxDistance;
        private TimeSpan? _maxTime;
        private XYPoint _near;

        // public properties
        /// <summary>
        /// Gets or sets the name of the additional field.
        /// </summary>
        public string AdditionalFieldName
        {
            get { return _additionalFieldName; }
            set { _additionalFieldName = value; }
        }

        /// <summary>
        /// Gets or sets the additional field value.
        /// </summary>
        public BsonValue AdditionalFieldValue
        {
            get { return _additionalFieldValue; }
            set { _additionalFieldValue = value; }
        }

        /// <summary>
        /// Gets or sets the limit.
        /// </summary>
        public int? Limit
        {
            get { return _limit; }
            set { _limit = value; }
        }

        /// <summary>
        /// Gets or sets the max distance.
        /// </summary>
        public double? MaxDistance
        {
            get { return _maxDistance; }
            set { _maxDistance = value; }
        }

        /// <summary>
        /// Gets or sets the max time.
        /// </summary>
        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = value; }
        }

        /// <summary>
        /// Gets or sets the location near which to search.
        /// </summary>
        public XYPoint Near
        {
            get { return _near; }
            set { _near = value; }
        }

        // methods
        /// <summary>
        /// Sets the name and value of the additional field.
        /// </summary>
        /// <param name="name">Name of the additional field.</param>
        /// <param name="value">The value.</param>
        /// <returns>The args so calls can be chained.</returns>
        public GeoHaystackSearchArgs SetAdditionalField(string name, BsonValue value)
        {
            _additionalFieldName = name;
            _additionalFieldValue = value;
            return this;
        }

        /// <summary>
        /// Sets the name and value of the additional field.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <typeparam name="TMember">The type of the member.</typeparam>
        /// <param name="memberExpression">The member expression.</param>
        /// <param name="value">The value.</param>
        /// <returns>The args so calls can be chained.</returns>
        public GeoHaystackSearchArgs SetAdditionalField<TDocument, TMember>(Expression<Func<TDocument, TMember>> memberExpression, TMember value)
        {
            var serializationInfoHelper = new BsonSerializationInfoHelper();
            var serializationInfo = serializationInfoHelper.GetSerializationInfo(memberExpression);
            var serializedValue = serializationInfoHelper.SerializeValue(serializationInfo, value);
            SetAdditionalField(serializationInfo.ElementName, serializedValue);
            return this;
        }
    }
}
