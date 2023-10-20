#!/bin/bash
cd tests/Hornbill.Tests.CSharp || exit
dotnet run
cd ../Hornbill.Tests.FSharp || exit
dotnet run
cd ../..
dotnet build
