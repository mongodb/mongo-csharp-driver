#!/usr/bin/env bash

dotnet test -r netstandard15 tests/MongoDB.Bson.Tests.Dotnet/
dotnet test -r netstandard15 tests/MongoDB.Driver.Core.Tests.Dotnet/
dotnet test -r netstandard15 tests/MongoDB.Driver.Tests.Dotnet/
dotnet test -r netstandard15 tests/MongoDB.Driver.GridFS.Tests.Dotnet/
dotnet test -r netstandard15 tests/MongoDB.Driver.Legacy.Tests.Dotnet/