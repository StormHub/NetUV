#!/usr/bin/env bash

#exit if any command fails
set -e

echo $CONFIGURATION

dotnet restore

dotnet build -c $CONFIGURATION ./src/NetUV.Core

echo $TRAVIS_OS_NAME

if test "$TRAVIS_OS_NAME" != "osx"; then
  dotnet test -c $CONFIGURATION ./test/NetUV.Tests -verbose
fi

dotnet run -c $CONFIGURATION -p ./test/NetUV.Tests.Performance