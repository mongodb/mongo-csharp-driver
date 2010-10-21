NUnit 2.5.7.10213
=================

You must install NUnit before you can build the MongoDB C# Driver. The file NUnit-2.5.7.10213.msi is a copy of the file of the same name available at:

http://nunit.org/?p=download

If Visual Studio is unable to resolve the references to nunit.framework you may have to drop the references and add them back. On a 64-bit Windows 7 machine nunit.framework.dll is installed at "C:\Program Files (x86)\NUnit 2.5.7\bin\net-2.0\framework".

If NUnit was not installed at the above location you will also have to adjust the path to nunit.exe in the "Start external program" setting of the Debug tab of the project settings for BsonUnitTests and DriverUnitTests.
