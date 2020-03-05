/* Copyright 2019–present MongoDB Inc.
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

using MongoDB.Driver.Core.NativeLibraryLoader;
using System;

namespace MongoDB.Driver.Core.Compression.Snappy
{
    internal static class SnappyAdapter
    {
        public static SnappyStatus snappy_compress(byte[] input, int input_offset, int input_length, byte[] output, int output_offset, ref int output_length)
        {
            using (var pinnedInput = new PinnedBuffer(input, input_offset))
            using (var pinnedOutput = new PinnedBuffer(output, output_offset))
            {
                return SnappyNativeMethodsAdapter.snappy_compress(pinnedInput.IntPtr, input_length, pinnedOutput.IntPtr, ref output_length);
            }
        }

        public static int snappy_max_compressed_length(int input_length)
        {
            return SnappyNativeMethodsAdapter.snappy_max_compressed_length(input_length);
        }

        public static SnappyStatus snappy_uncompress(byte[] input, int input_offset, int input_length, byte[] output, int output_offset, ref int output_length)
        {
            using (var pinnedInput = new PinnedBuffer(input, input_offset))
            using (var pinnedOutput = new PinnedBuffer(output, output_offset))
            {
                return SnappyNativeMethodsAdapter.snappy_uncompress(pinnedInput.IntPtr, input_length, pinnedOutput.IntPtr, ref output_length);
            }
        }

        public static SnappyStatus snappy_uncompressed_length(byte[] input, int input_offset, int input_length, out int output_length)
        {
            using (var pinnedInput = new PinnedBuffer(input, input_offset))
            {
                return SnappyNativeMethodsAdapter.snappy_uncompressed_length(pinnedInput.IntPtr, input_length, out output_length);
            }
        }

        public static SnappyStatus snappy_validate_compressed_buffer(byte[] input, int input_offset, int input_length)
        {
            using (var pinnedInput = new PinnedBuffer(input, input_offset))
            {
                return SnappyNativeMethodsAdapter.snappy_validate_compressed_buffer(pinnedInput.IntPtr, input_length);
            }
        }

        // nested types
        private static class SnappyNativeMethodsAdapter
        {
            private static bool __is64BitProcess = Is64BitProcess();

            public static SnappyStatus snappy_compress(IntPtr input, int input_length, IntPtr output, ref int output_length)
            {
                SnappyStatus status;
                if (__is64BitProcess)
                {
                    var ulongOutput_length = (ulong)output_length;
                    status = Snappy64NativeMethods.snappy_compress(input, (ulong)input_length, output, ref ulongOutput_length);
                    output_length = (int)ulongOutput_length;
                }
                else
                {
                    var uintOutput_length = (uint)output_length;
                    status = Snappy32NativeMethods.snappy_compress(input, (uint)input_length, output, ref uintOutput_length);
                    output_length = (int)uintOutput_length;
                }

                return status;
            }

            public static int snappy_max_compressed_length(int input_length)
            {
                if (__is64BitProcess)
                {
                    return (int)Snappy64NativeMethods.snappy_max_compressed_length((ulong)input_length);
                }
                else
                {
                    return (int)Snappy32NativeMethods.snappy_max_compressed_length((uint)input_length);
                }
            }

            public static SnappyStatus snappy_uncompress(IntPtr input, int input_length, IntPtr output, ref int output_length)
            {
                SnappyStatus status;
                if (__is64BitProcess)
                {
                    var ulongOutput_length = (ulong)output_length;
                    status = Snappy64NativeMethods.snappy_uncompress(input, (ulong)input_length, output, ref ulongOutput_length);
                    output_length = (int)ulongOutput_length;
                }
                else
                {
                    var uintOutput_length = (uint)output_length;
                    status = Snappy32NativeMethods.snappy_uncompress(input, (uint)input_length, output, ref uintOutput_length);
                    output_length = (int)uintOutput_length;
                }
                return status;
            }

            public static SnappyStatus snappy_uncompressed_length(IntPtr input, int input_length, out int output_length)
            {
                SnappyStatus status;
                if (__is64BitProcess)
                {
                    status = Snappy64NativeMethods.snappy_uncompressed_length(input, (ulong)input_length, out var ulongOutput_length);
                    output_length = (int)ulongOutput_length;
                }
                else
                {
                    status = Snappy32NativeMethods.snappy_uncompressed_length(input, (uint)input_length, out var uintOutput_length);
                    output_length = (int)uintOutput_length;
                }
                return status;
            }

            public static SnappyStatus snappy_validate_compressed_buffer(IntPtr input, int input_length)
            {
                if (__is64BitProcess)
                {
                    return Snappy64NativeMethods.snappy_validate_compressed_buffer(input, (ulong)input_length);
                }
                else
                {
                    return Snappy32NativeMethods.snappy_validate_compressed_buffer(input, (uint)input_length);
                }
            }

            private static bool Is64BitProcess()
            {
#if NET452 || NETSTANDARD2_0
                var is64Bit = Environment.Is64BitProcess;
#else
                var is64Bit = IntPtr.Size == 8;
#endif
                return is64Bit;
            }
        }
    }
}
