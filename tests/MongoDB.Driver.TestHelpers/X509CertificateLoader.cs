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

using System;
using System.Security.Cryptography.X509Certificates;

namespace MongoDB.Driver.TestHelpers;

#if !NET8_0_OR_GREATER

public static class X509CertificateLoader
{
    public static X509Certificate2 LoadCertificate(ReadOnlySpan<byte> certificateData) =>
        new(certificateData.ToArray());

    public static X509Certificate2 LoadCertificateFromFile(string certificateFilename) =>
        new (certificateFilename);

    public static X509Certificate2 LoadPkcs12FromFile(string certificateFilename, string password) =>
        new(certificateFilename, password);
}

#endif
