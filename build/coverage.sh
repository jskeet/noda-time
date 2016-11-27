#!/bin/bash

set -e

# cd to repository root
cd $(dirname $0)/..

nuget install -Verbosity quiet -OutputDirectory packages -Version 4.6.519 OpenCover
nuget install -Verbosity quiet -OutputDirectory packages -Version 0.7.0 coveralls.net
nuget install -Verbosity quiet -OutputDirectory packages -Version 2.4.5.0 ReportGenerator

dotnet restore src
dotnet build -c Release src/NodaTime.Test

# Use the legacy JIT to avoid getting InvalidProgramException.
export COMPLUS_useLegacyJit=1
packages/OpenCover.4.6.519/tools/OpenCover.Console.exe \
  -register:user \
  -oldStyle \
  -target:"c:\Program Files\dotnet\dotnet.exe" \
  -targetargs:"test -f net451 -c Release src/NodaTime.Test -where=cat!=Slow" \
  -output:coverage.xml \
  -filter:"+[NodaTime]NodaTime.*" \
  -searchdirs:NodaTime/bin/Release/net451/win7-x64

packages/ReportGenerator.2.4.5.0/tools/ReportGenerator.exe \
  -reports:coverage.xml \
  -targetdir:coverage \
  -verbosity:Error
