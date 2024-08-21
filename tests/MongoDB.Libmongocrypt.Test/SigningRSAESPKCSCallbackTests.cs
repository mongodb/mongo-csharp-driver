/*
 * Copyright 2020–present MongoDB, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Text;
using FluentAssertions;
using Xunit;

namespace MongoDB.Libmongocrypt.Test.Callbacks
{
    public class SigningRSAESPKCSCallbackTests
    {
        private static string DataToSign =  "data to sign";

        public static string PrivateKey = "MIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQC4JOyv5z05cL18ztpknRC7CFY2gYol4DAKerdVUoDJ"
            + "xCTmFMf39dVUEqD0WDiw/qcRtSO1/FRut08PlSPmvbyKetsLoxlpS8lukSzEFpFK7+L+R4miFOl6HvECyg7lbC1H/WGAhIz9yZRlXhRo9qmO/fB6PV9IeYtU+1xY"
            + "uXicjCDPp36uuxBAnCz7JfvxJ3mdVc0vpSkbSb141nWuKNYR1mgyvvL6KzxO6mYsCo4hRAdhuizD9C4jDHk0V2gDCFBk0h8SLEdzStX8L0jG90/Og4y7J1b/cPo/"
            + "kbYokkYisxe8cPlsvGBf+rZex7XPxc1yWaP080qeABJb+S88O//LAgMBAAECggEBAKVxP1m3FzHBUe2NZ3fYCc0Qa2zjK7xl1KPFp2u4CU+9sy0oZJUqQHUdm5CM"
            + "prqWwIHPTftWboFenmCwrSXFOFzujljBO7Z3yc1WD3NJl1ZNepLcsRJ3WWFH5V+NLJ8Bdxlj1DMEZCwr7PC5+vpnCuYWzvT0qOPTl9RNVaW9VVjHouJ9Fg+s2DrS"
            + "hXDegFabl1iZEDdI4xScHoYBob06A5lw0WOCTayzw0Naf37lM8Y4psRAmI46XLiF/Vbuorna4hcChxDePlNLEfMipICcuxTcei1RBSlBa2t1tcnvoTy6cuYDqqIm"
            + "RYjp1KnMKlKQBnQ1NjS2TsRGm+F0FbreVCECgYEA4IDJlm8q/hVyNcPe4OzIcL1rsdYN3bNm2Y2O/YtRPIkQ446ItyxD06d9VuXsQpFp9jNACAPfCMSyHpPApqlx"
            + "dc8z/xATlgHkcGezEOd1r4E7NdTpGg8y6Rj9b8kVlED6v4grbRhKcU6moyKUQT3+1B6ENZTOKyxuyDEgTwZHtFECgYEA0fqdv9h9s77d6eWmIioP7FSymq93pC4u"
            + "mxf6TVicpjpMErdD2ZfJGulN37dq8FOsOFnSmFYJdICj/PbJm6p1i8O21lsFCltEqVoVabJ7/0alPfdG2U76OeBqI8ZubL4BMnWXAB/VVEYbyWCNpQSDTjHQYs54"
            + "qa2I0dJB7OgJt1sCgYEArctFQ02/7H5Rscl1yo3DBXO94SeiCFSPdC8f2Kt3MfOxvVdkAtkjkMACSbkoUsgbTVqTYSEOEc2jTgR3iQ13JgpHaFbbsq64V0QP3TAx"
            + "bLIQUjYGVgQaF1UfLOBv8hrzgj45z/ST/G80lOl595+0nCUbmBcgG1AEWrmdF0/3RmECgYAKvIzKXXB3+19vcT2ga5Qq2l3TiPtOGsppRb2XrNs9qKdxIYvHmXo/"
            + "9QP1V3SRW0XoD7ez8FpFabp42cmPOxUNk3FK3paQZABLxH5pzCWI9PzIAVfPDrm+sdnbgG7vAnwfL2IMMJSA3aDYGCbF9EgefG+STcpfqq7fQ6f5TBgLFwKBgCd7"
            + "gn1xYL696SaKVSm7VngpXlczHVEpz3kStWR5gfzriPBxXgMVcWmcbajRser7ARpCEfbxM1UJyv6oAYZWVSNErNzNVb4POqLYcCNySuC6xKhs9FrEQnyKjyk8wI4V"
            + "nrEMGrQ8e+qYSwYk9Gh6dKGoRMAPYVXQAO0fIsHF/T0a";

        private static string ExpectedSignature = "VocBRhpMmQ2XCzVehWSqheQLnU889gf3dhU4AnVnQTJjsKx/CM23qKDPkZDd2A/BnQsp99SN7ksIX5Raj0TPw"
            + "yN5OCN/YrNFNGoOFlTsGhgP/hyE8X3Duiq6sNO0SMvRYNPFFGlJFsp1Fw3Z94eYMg4/Wpw5s4+Jo5Zm/qY7aTJIqDKDQ3CNHLeJgcMUOc9sz01/GzoUYKDVODHSx"
            + "rYEk5ireFJFz9vP8P7Ha+VDUZuQIQdXer9NBbGFtYmWprY3nn4D3Dw93Sn0V0dIqYeIo91oKyslvMebmUM95S2PyIJdEpPb2DJDxjvX/0LLwSWlSXRWy9gapWoBk"
            + "b4ynqZBsg==";

        [Fact]
        public void GetSignatureTest()
        {
            byte[] privateKeyBytes = Convert.FromBase64String(PrivateKey);
            var dataBytes = Encoding.ASCII.GetBytes(DataToSign);
#if NETCOREAPP3_0
            byte[] signature = SigningRSAESPKCSCallback.HashAndSignBytes(dataBytes, privateKeyBytes);
            string output = Convert.ToBase64String(signature);

            output.Should().Be(ExpectedSignature);
#else
            var ex = Record.Exception(() => SigningRSAESPKCSCallback.HashAndSignBytes(dataBytes, privateKeyBytes));
            ex.Should().BeOfType<PlatformNotSupportedException>();
#endif
        }
    }
}
