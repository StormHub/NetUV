#!/usr/bin/env bash

#exit if any command fails
set -ev

echo $TRAVIS_OS_NAME
echo $CONFIGURATION

dotnet restore

dotnet build -c $CONFIGURATION -f netstandard1.6 ./src/NetUV.Core/NetUV.Core.csproj

dotnet test -c $CONFIGURATION -f netcoreapp1.1 ./test/NetUV.Core.Tests/NetUV.Core.Tests.csproj

dotnet run -c $CONFIGURATION -f netcoreapp1.1 -p ./test/NetUV.Core.Tests.Performance/NetUV.Core.Tests.Performance.csproj