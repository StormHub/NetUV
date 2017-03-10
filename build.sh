#!/usr/bin/env bash

#exit if any command fails
set -e

echo $CONFIGURATION

dotnet restore

dotnet build -c $CONFIGURATION ./src/NetUV.Core

dotnet test -c $CONFIGURATION ./test/NetUV.Tests

dotnet run -c $CONFIGURATION -p ./test/NetUV.Tests.Performance