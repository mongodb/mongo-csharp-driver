# Requirements

__All__

CMake 3.12 or later

__Windows__

Visual Studio 2017 15.9+

__Linux, macOS__

dotnet 2.1+


# Quick Instructions

*Requires:* Cygwin
```
1. bash ./libmongocrypt/.evergreen/compile.sh
```
*Note*: You must call this from the parent directory of the libmongocrypt repo. It will not work within the repo directory


## Developer Instructions

### Windows
To build libmongocrypt on Windows. This example assumes kms-message and the c driver are installed to "d:/usr"

```
1. mkdir build
2. "C:\Program Files\CMake\bin\cmake.exe" -Thost=x64 -G "Visual Studio 15 2017 Win64" -DCMAKE_INSTALL_PREFIX=d:/usr -DCMAKE_PREFIX_PATH=d:/usr -DCMAKE_BUILD_TYPE=Debug "-DCMAKE_C_FLAGS=-Id:/usr/include" ..
3. msbuild libmongocrypt.sln
4. cd bindings/cs
5. msbuild cs.sln
```

### Troubleshooting

If you see `Windows Error: 126` during tests, like the example below, it means that `libbson-1.0.dll` is not in your path.

```
 System.TypeInitializationException : The type initializer for 'MongoDB.Libmongocrypt.Library' threw an exception.
---- System.IO.FileNotFoundException : D:\repo\libmongocrypt\build\bindings\cs\MongoDB.Libmongocrypt.Test\bin\x64\Debug\netcoreapp2.1\mongocrypt.dll, Windows Error: 126
```


### Linux and macOS

*Note* Only building from the cmake build directory is supported

```
1. Build libmongocrypt with CMake
2. cd <build>/bindings/cs
3. dotnet build cs.build
```
*Note*: You can use the ```LIBMONGOCRYPT_PATH``` environment variable to load a locally installed
libmongocrypt build. You should specify the absolute path to the libmongocrypt library itself, not just the containing folder. For example on Linux:
```$ export LIBMONGOCRYPT_PATH='/path/to/libmongocrypt.so'```.

# Testing
Do not modify xunit.runner.json
- Be wary of https://github.com/xunit/xunit/issues/1654

### Debugging on Linux
To attach to a unit test with lldb, print the PID in the process and then attach.

Tests always run in child processes and lldb, as of 7.0, cannot follow child processes.

