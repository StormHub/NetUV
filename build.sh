#!/usr/bin/env bash

#exit if any command fails
set -ev

echo $TRAVIS_OS_NAME
echo $CONFIGURATION

dotnet restore

dotnet build -c $CONFIGURATION -f netstandard2.0 ./src/NetUV.Core/NetUV.Core.csproj

dotnet test -c $CONFIGURATION -f netcoreapp2.0 ./test/NetUV.Core.Tests/NetUV.Core.Tests.csproj

dotnet run -c $CONFIGURATION -f netcoreapp2.0 -p ./test/NetUV.Core.Tests.Performance/NetUV.Core.Tests.Performance.csproj