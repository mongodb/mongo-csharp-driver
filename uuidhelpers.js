// Javascript helper functions for parsing and displaying UUIDs in the MongoDB shell.
// This is a temporary solution until SERVER-3153 is implemented.
// To create BinData values corresponding to the various driver encodings use:
//      var s = "{00112233-4455-6677-8899-aabbccddeeff}";
//      var uuid = UUID(s); // new Standard encoding
//      var juuid = JUUID(s); // JavaLegacy encoding
//      var csuuid = CSUUID(s); // CSharpLegacy encoding
//      var pyuuid = PYUUID(s); // PythonLegacy encoding
// To convert the various BinData values back to human readable UUIDs use:
//      uuid.toUUID()     => 'UUID("00112233-4455-6677-8899-aabbccddeeff")'
//      juuid.ToJUUID()   => 'JUUID("00112233-4455-6677-8899-aabbccddeeff")'
//      csuuid.ToCSUUID() => 'CSUUID("00112233-4455-6677-8899-aabbccddeeff")'
//      pyuuid.ToPYUUID() => 'PYUUID("00112233-4455-6677-8899-aabbccddeeff")'
// With any of the UUID variants you can use toHexUUID to echo the raw BinData with subtype and hex string:
//      uuid.toHexUUID()   => 'HexData(4, "00112233-4455-6677-8899-aabbccddeeff")'
//      juuid.toHexUUID()  => 'HexData(3, "77665544-3322-1100-ffee-ddccbbaa9988")'
//      csuuid.toHexUUID() => 'HexData(3, "33221100-5544-7766-8899-aabbccddeeff")'
//      pyuuid.toHexUUID() => 'HexData(3, "00112233-4455-6677-8899-aabbccddeeff")'

function HexToBase64(hex) {
    var base64Digits = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    var base64 = "";
    var group;
    for (var i = 0; i < 30; i += 6) {
        group = parseInt(hex.substr(i, 6), 16);
        base64 += base64Digits[(group >> 18) & 0x3f];
        base64 += base64Digits[(group >> 12) & 0x3f];
        base64 += base64Digits[(group >> 6) & 0x3f];
        base64 += base64Digits[group & 0x3f];
    }
    group = parseInt(hex.substr(30, 2), 16);
    base64 += base64Digits[(group >> 2) & 0x3f];
    base64 += base64Digits[(group << 4) & 0x3f];
    base64 += "==";
    return base64;
}

function Base64ToHex(base64) {
    var base64Digits = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";
    var hexDigits = "0123456789abcdef";
    var hex = "";
    for (var i = 0; i < 24; ) {
        var e1 = base64Digits.indexOf(base64[i++]);
        var e2 = base64Digits.indexOf(base64[i++]);
        var e3 = base64Digits.indexOf(base64[i++]);
        var e4 = base64Digits.indexOf(base64[i++]);
        var c1 = (e1 << 2) | (e2 >> 4);
        var c2 = ((e2 & 15) << 4) | (e3 >> 2);
        var c3 = ((e3 & 3) << 6) | e4;
        hex += hexDigits[c1 >> 4];
        hex += hexDigits[c1 & 15];
        if (e3 != 64) {
            hex += hexDigits[c2 >> 4];
            hex += hexDigits[c2 & 15];
        }
        if (e4 != 64) {
            hex += hexDigits[c3 >> 4];
            hex += hexDigits[c3 & 15];
        }
    }
    return hex;
}

function UUID(uuid) {
    var hex = uuid.replace(/[{}-]/g, ""); // remove extra characters
    var base64 = HexToBase64(hex);
    return new BinData(4, base64); // new subtype 4
}

function JUUID(uuid) {
    var hex = uuid.replace(/[{}-]/g, ""); // remove extra characters
    var msb = hex.substr(0, 16);
    var lsb = hex.substr(16, 16);
    msb = msb.substr(14, 2) + msb.substr(12, 2) + msb.substr(10, 2) + msb.substr(8, 2) + msb.substr(6, 2) + msb.substr(4, 2) + msb.substr(2, 2) + msb.substr(0, 2);
    lsb = lsb.substr(14, 2) + lsb.substr(12, 2) + lsb.substr(10, 2) + lsb.substr(8, 2) + lsb.substr(6, 2) + lsb.substr(4, 2) + lsb.substr(2, 2) + lsb.substr(0, 2);
    hex = msb + lsb;
    var base64 = HexToBase64(hex);
    return new BinData(3, base64);
}

function CSUUID(uuid) {
    var hex = uuid.replace(/[{}-]/g, ""); // remove extra characters
    var a = hex.substr(6, 2) + hex.substr(4, 2) + hex.substr(2, 2) + hex.substr(0, 2);
    var b = hex.substr(10, 2) + hex.substr(8, 2);
    var c = hex.substr(14, 2) + hex.substr(12, 2);
    var d = hex.substr(16, 16);
    hex = a + b + c + d;
    var base64 = HexToBase64(hex);
    return new BinData(3, base64);
}

function PYUUID(uuid) {
    var hex = uuid.replace(/[{}-]/g, ""); // remove extra characters
    var base64 = HexToBase64(hex);
    return new BinData(3, base64);
}

BinData.prototype.toUUID = function () {
    var hex = Base64ToHex(this.base64()); // don't use BinData's hex function because it has bugs in older versions of the shell
    var uuid = hex.substr(0, 8) + '-' + hex.substr(8, 4) + '-' + hex.substr(12, 4) + '-' + hex.substr(16, 4) + '-' + hex.substr(20, 12);
    return 'UUID("' + uuid + '")';
}

BinData.prototype.toJUUID = function () {
    var hex = Base64ToHex(this.base64()); // don't use BinData's hex function because it has bugs in older versions of the shell
    var msb = hex.substr(0, 16);
    var lsb = hex.substr(16, 16);
    msb = msb.substr(14, 2) + msb.substr(12, 2) + msb.substr(10, 2) + msb.substr(8, 2) + msb.substr(6, 2) + msb.substr(4, 2) + msb.substr(2, 2) + msb.substr(0, 2);
    lsb = lsb.substr(14, 2) + lsb.substr(12, 2) + lsb.substr(10, 2) + lsb.substr(8, 2) + lsb.substr(6, 2) + lsb.substr(4, 2) + lsb.substr(2, 2) + lsb.substr(0, 2);
    hex = msb + lsb;
    var uuid = hex.substr(0, 8) + '-' + hex.substr(8, 4) + '-' + hex.substr(12, 4) + '-' + hex.substr(16, 4) + '-' + hex.substr(20, 12);
    return 'JUUID("' + uuid + '")';
}

BinData.prototype.toCSUUID = function () {
    var hex = Base64ToHex(this.base64()); // don't use BinData's hex function because it has bugs in older versions of the shell
    var a = hex.substr(6, 2) + hex.substr(4, 2) + hex.substr(2, 2) + hex.substr(0, 2);
    var b = hex.substr(10, 2) + hex.substr(8, 2);
    var c = hex.substr(14, 2) + hex.substr(12, 2);
    var d = hex.substr(16, 16);
    hex = a + b + c + d;
    var uuid = hex.substr(0, 8) + '-' + hex.substr(8, 4) + '-' + hex.substr(12, 4) + '-' + hex.substr(16, 4) + '-' + hex.substr(20, 12);
    return 'CSUUID("' + uuid + '")';
}

BinData.prototype.toPYUUID = function () {
    var hex = Base64ToHex(this.base64()); // don't use BinData's hex function because it has bugs
    var uuid = hex.substr(0, 8) + '-' + hex.substr(8, 4) + '-' + hex.substr(12, 4) + '-' + hex.substr(16, 4) + '-' + hex.substr(20, 12);
    return 'PYUUID("' + uuid + '")';
}


BinData.prototype.toHexUUID = function () {
    var hex = Base64ToHex(this.base64()); // don't use BinData's hex function because it has bugs
    var uuid = hex.substr(0, 8) + '-' + hex.substr(8, 4) + '-' + hex.substr(12, 4) + '-' + hex.substr(16, 4) + '-' + hex.substr(20, 12);
    return 'HexData(' + this.subtype() + ', "' + uuid + '")';
}

function TestUUIDHelperFunctions() {
    var s = "{00112233-4455-6677-8899-aabbccddeeff}";
    var uuid = UUID(s);
    var juuid = JUUID(s);
    var csuuid = CSUUID(s);
    var pyuuid = PYUUID(s);
    print(uuid.toUUID());
    print(juuid.toJUUID());
    print(csuuid.toCSUUID());
    print(pyuuid.toPYUUID());
    print(uuid.toHexUUID());
    print(juuid.toHexUUID());
    print(csuuid.toHexUUID());
    print(pyuuid.toHexUUID());
}
