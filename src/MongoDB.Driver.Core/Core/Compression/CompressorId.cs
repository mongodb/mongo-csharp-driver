/* Copyright 2013-present MongoDB Inc.
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

namespace MongoDB.Driver.Core.Compression
{
	/// <summary>
	/// Represents the compressor id.
	/// </summary>
	public enum CompressorId
	{
		/// <summary>
		/// No compression.
		/// </summary>
		noop = 0,
		///// <summary>
		///// Compression using snappy algorithm. NOT SUPPORTED YET.
		///// </summary>
		//snappy = 1, 
		/// <summary>
		/// Compression using zlib algorithm. 
		/// </summary>
		zlib = 2
	}
}