Scripts to build library from the source after cloning : https://github.com/google/snappy

$ git submodule update --init
$ mkdir build
$ cd build && cmake ../ && make
$ cmake -DBUILD_SHARED_LIBS=ON ..
$ cmake --build . --target snappy

Note related to issues during building at the current time:
- The latest release (1.1.9) cannot be built on macOS and linux due some building bugs (see the CSHARP-2813 PR and CSHARP-3819 for details),
so use the previous stable 1.1.8 release instead.
- Windows version can be built with 1.1.9 version, but it requires additional investigation how to configure options like x32 processor architecture.

