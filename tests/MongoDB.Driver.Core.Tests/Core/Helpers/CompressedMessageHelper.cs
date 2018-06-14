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

using System.IO;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;

namespace MongoDB.Driver.Core.Helpers
{
	internal static class CompressedMessageHelper
	{
		public static byte[] CreateCompressedMessageBytes(byte[] commandMessageBytes, ICompressor compressor, byte compressorId = (byte)CompressorId.zlib)
		{
			var compressedCommandMessage = compressor.Compress(commandMessageBytes, 16);

			return CreateCompressedMessageBytes(commandMessageBytes, compressedCommandMessage, compressorId);
		}

		public static byte[] CreateCompressedMessageBytes(byte[] commandMesage, byte[] compressedCommandMessage, byte compressorId)
		{
			using (var memStream = new MemoryStream())
			using (var writer = new BinaryWriter(memStream))
			{
				writer.Write(25 + compressedCommandMessage.Length);
				writer.Write(0);
				writer.Write(0);
				writer.Write((int)Opcode.Compressed);
				writer.Write((int)Opcode.OpMsg);
				writer.Write(commandMesage.Length - 16);
				writer.Write(compressorId);
				writer.Write(compressedCommandMessage);
				
				return memStream.ToArray();
			}
		}
	}
}