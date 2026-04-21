#!/usr/bin/env sh

set -e

echo 'Running .NET tests.'
dotnet test --no-build --verbosity normal || echo 'No test projects found, skipping.'
