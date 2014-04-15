/* Copyright 2010-2014 MongoDB Inc.
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
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;

namespace MongoDB.Driver.Communication.Security.Mechanisms.Gsasl
{
    internal static class Gsasl
    {
        // public constants
        public const int GSASL_OK = 0;
        public const int GSASL_NEEDS_MORE = 1;
        public const int GSASL_UNKNOWN_MECHANISM = 2;
        public const int GSASL_MECHANISM_CALLED_TOO_MANY_TIMES = 3;
        public const int GSASL_MALLOC_ERROR = 7;
        public const int GSASL_BASE64_ERROR = 8;
        public const int GSASL_CRYPTO_ERROR = 9;
        public const int GSASL_SASLPREP_ERROR = 29;
        public const int GSASL_MECHANISM_PARSE_ERROR = 30;
        public const int GSASL_AUTHENTICATION_ERROR = 31;
        public const int GSASL_INTEGRITY_ERROR = 33;
        public const int GSASL_NO_CLIENT_CODE = 35;
        public const int GSASL_NO_SERVER_CODE = 36;
        public const int GSASL_NO_CALLBACK = 51;
        public const int GSASL_NO_ANONYMOUS_TOKEN = 52;
        public const int GSASL_NO_AUTHID = 53;
        public const int GSASL_NO_AUTHZID = 54;
        public const int GSASL_NO_PASSWORD = 55;
        public const int GSASL_NO_PASSCODE = 56;
        public const int GSASL_NO_PIN = 57;
        public const int GSASL_NO_SERVICE = 58;
        public const int GSASL_NO_HOSTNAME = 59;
        public const int GSASL_NO_CB_TLS_UNIQUE = 65;
        public const int GSASL_NO_SAML20_IDP_IDENTIFIER = 66;
        public const int GSASL_NO_SAML20_REDIRECT_URL = 67;
        public const int GSASL_NO_OPENID20_REDIRECT_URL = 68;

        // public static methods
        /// <summary>
        /// Gets the description of an error code.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <returns>The description.</returns>
        public static string GetError(int error)
        {
            var messagePtr = gsasl_strerror(error);
            return Marshal.PtrToStringAnsi(messagePtr);
        }

        /// <summary>
        /// Begins a GsaslSession.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="mechanism">The mechanism.</param>
        /// <param name="session">The session.</param>
        /// <returns>A result code.</returns>
        [DllImport("libgsasl-7.dll", CharSet = CharSet.Ansi)]
        public static extern int gsasl_client_start(
            GsaslContext context,
            [MarshalAs(UnmanagedType.LPStr)]string mechanism,
            out GsaslSession session);

        /// <summary>
        /// Frees a GsaslContext.
        /// </summary>
        /// <param name="context">The context.</param>
        [DllImport("libgsasl-7.dll", CharSet = CharSet.Ansi)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static extern void gsasl_done(IntPtr context);

        /// <summary>
        /// Frees a GsaslSession.
        /// </summary>
        /// <param name="session">The session.</param>
        [DllImport("libgsasl-7.dll", CharSet = CharSet.Ansi)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static extern void gsasl_finish (IntPtr session);

        /// <summary>
        /// Frees memory allocated by libgsasl.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        [DllImport("libgsasl-7.dll", CharSet = CharSet.Ansi)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static extern void gsasl_free(IntPtr ptr);

        /// <summary>
        /// Initiates a GsaslContext.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        [DllImport("libgsasl-7.dll", CharSet = CharSet.Ansi)]
        public static extern int gsasl_init(out GsaslContext context);

        /// <summary>
        /// Sets a property on a GsaslSession.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="prop">The prop.</param>
        /// <param name="value">The value.</param>
        [DllImport("libgsasl-7.dll", CharSet = CharSet.Ansi)]
        public static extern void gsasl_property_set(
            GsaslSession session,
            GsaslProperty prop,
            [MarshalAs(UnmanagedType.LPStr)]string value);

        /// <summary>
        /// Steps through the state machine.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="input">The input.</param>
        /// <param name="input_len">The input_len.</param>
        /// <param name="output">The output.</param>
        /// <param name="output_len">The output_len.</param>
        /// <returns></returns>
        [DllImport("libgsasl-7.dll", CharSet = CharSet.Ansi)]
        public static extern int gsasl_step(
            GsaslSession session,
            IntPtr input, 
            int input_len,
            out IntPtr output, 
            out int output_len);

        /// <summary>
        /// Gets a description for the error code.
        /// </summary>
        /// <param name="err">The err.</param>
        /// <returns>A string describing the error.</returns>
        [DllImport("libgsasl-7.dll", CharSet = CharSet.Ansi)]
        public static extern IntPtr gsasl_strerror(int err);
    }
}